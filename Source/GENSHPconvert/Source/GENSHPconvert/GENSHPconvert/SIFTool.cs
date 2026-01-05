// GENSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENSHPconvert.
// 
// GENSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using EGIS.ShapeFileLib;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Sweco.SIF.GENSHPconvert
{
    public class SIFTool : SIFToolBase
    {
        /// <summary>
        /// If true, a warning for NULL-values has been issued already
        /// </summary>
        public bool isWarnedForNullValue;

        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for converting GEN-files to shapefiles or vice versa";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            DATFile.IsErrorOnDuplicateID = !settings.IgnoreDuplicateIDs;
            GENFile.IsErrorOnDuplicateID = !settings.IgnoreDuplicateIDs;

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one input file is found for current filter: " + settings.InputFilter);
            }

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                isWarnedForNullValue = false;
                string relativeOutputpath = Path.GetDirectoryName(FileUtils.GetRelativePath(inputFilename, settings.InputPath));
                string outputpath = Path.Combine(settings.OutputPath, relativeOutputpath);

                // Create output path if not yet existing
                if (!Directory.Exists(outputpath))
                {
                    Directory.CreateDirectory(outputpath);
                }

                if (Path.GetExtension(inputFilename).ToLower().Equals(".gen"))
                {
                    ConvertGENToShapefile(inputFilename, outputpath, settings);
                }
                else if (Path.GetExtension(inputFilename).ToLower().Equals(".shp"))
                {
                    ConvertShapefileToGEN(inputFilename, outputpath, settings);
                }
                else
                {
                    continue;
                }

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected void ConvertShapefileToGEN(string shapeFilename, string outputPath, SIFToolSettings settings)
        {
            Log.AddInfo("Reading shapefile " + FileUtils.GetRelativePath(shapeFilename, settings.InputPath) + " ... ", 1, false);
            ShapeFile sf = null;
            DbfReader dbfReader = null;
            try
            {
                sf = new EGIS.ShapeFileLib.ShapeFile(shapeFilename);

                sf.RenderSettings = new EGIS.ShapeFileLib.RenderSettings(shapeFilename, string.Empty, new System.Drawing.Font(System.Drawing.FontFamily.GenericSerif, 6f));
                // Note: encoding is read from shapefile's cpg-file, which contains just a text string with the name of the encoding; if not present, EGIS uses UTF8 as a default. This can be overruled with one of:
                // sf.DbfReader.StringEncoding = Encoding.UTF8; // Encoding.ASCII; Encoding.GetEncoding("ISO8859-1"); // Latin-1 // Encoding.GetEncoding("Windows-1252"); // SBCS // Encoding.GetEncoding("UTF-8");

                string[] record = sf.GetRecords(0);
                if (record != null)
                {
                    Log.AddInfo(record.Length + " features found.");
                }
                else
                {
                    Log.AddInfo("no features found.");
                }

                Log.AddInfo(string.Format("Shape Type: {0}", sf.ShapeType), 2);

                dbfReader = new DbfReader(Path.ChangeExtension(shapeFilename, ".dbf"));
                ShapeType shapeType = sf.ShapeType;
                if (sf.ShapeType == ShapeType.PolygonZ)
                {
                    // Currently ignore PolgonZ 
                    shapeType = ShapeType.Polygon;
                }

                DbfFieldDesc[] fieldDescs = dbfReader.DbfRecordHeader.GetFieldDescriptions();

                List<GENFile> genFiles = new List<GENFile>();
                List<DATFile> datFiles = new List<DATFile>();

                GENFile genFile = new GENFile(sf.RecordCount);
                DATFile datFile = new DATFile(genFile, sf.RecordCount);
                // Always add an ID-column, which holds GENFeature index to keep relation with DAT-file
                datFile.AddColumn("ID");
                // Find unique name for secondary id which relates to original FID of shapefile
                datFile.AddColumn("ID2");

                // Now add columns from shapefile
                DbfFieldType[] fieldTypes = new DbfFieldType[fieldDescs.Length];
                for (int fieldIdx = 0; fieldIdx < fieldDescs.Length; fieldIdx++)
                {
                    DbfFieldDesc fieldDesc = fieldDescs[fieldIdx];
                    string fieldName = fieldDesc.FieldName;

                    // Fix existing ID or ID2 columnnames
                    if ((fieldName).ToUpper().Equals("ID"))
                    {
                        fieldName = "ID_ORG";
                    }
                    if ((fieldName).ToUpper().Equals("ID2"))
                    {
                        fieldName = "ID2_ORG";
                    }
                    datFile.AddColumn(fieldName, string.Empty, false);

                    // Floating point values are stored/retrieved as Number with EGIS; overrule with FloatingPoint type if decimals are present
                    fieldTypes[fieldIdx] = fieldDesc.FieldType;
                    if ((fieldDesc.FieldType == DbfFieldType.Number) && (fieldDesc.DecimalCount > 0))
                    {
                        // Override and use floating point when decimals are defined
                        fieldTypes[fieldIdx] = DbfFieldType.FloatingPoint;
                    }
                }

                // Calculate number of points between 5% logmessages, use multiple of 50
                int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(sf.RecordCount, 5);

                int id = 0;
                string id2;
                bool hasFeatureParts = false;
                for (int featureIdx = 0; featureIdx < sf.RecordCount; ++featureIdx)
                {
                    if (featureIdx % logSnapPointMessageFrequency == 0)
                    {
                        Log.AddInfo("Reading features " + (featureIdx + 1) + "-" + (int)Math.Min(sf.RecordCount, (featureIdx + logSnapPointMessageFrequency)) + " of " + sf.RecordCount + " ...", 2);
                    }

                    if ((settings.MaxFeatureCount != 0) && (genFile.Count >= settings.MaxFeatureCount))
                    {
                        genFiles.Add(genFile);
                        datFiles.Add(datFile);

                        GENFile newGENFile = new GENFile(settings.MaxFeatureCount);
                        DATFile newDATFile = new DATFile(newGENFile, settings.MaxFeatureCount);
                        newDATFile.AddColumns(datFile.ColumnNames.ToList());

                        genFile = newGENFile;
                        datFile = newDATFile;
                    }

                    // Get the DBF record
                    string[] fieldValues = sf.GetAttributeFieldValues(featureIdx);

                    // Retrieve feature(parts)
                    IReadOnlyCollection<PointD[]> pds = sf.GetShapeDataD(featureIdx);
                    if (pds.Count == 0)
                    {
                        Log.AddWarning("Feature " + (featureIdx + 1) + " has no shape data and is skipped: " + CommonUtils.ToString(fieldValues.ToList(), ",", true), 3);
                    }

                    hasFeatureParts |= (pds.Count > 1);
                    for (int partIdx = 0; partIdx < pds.Count; partIdx++)
                    {
                        PointD[] pd = pds.ElementAt(partIdx);
                        List<Point> pointList = new List<Point>(pd.Length);

                        if (partIdx >= 0)
                        {
                            foreach (PointD point in pd)
                            {
                                pointList.Add(new DoublePoint(point.X, point.Y));
                            }
                        }
                        else
                        {
                            for (int pointIdx = pd.Length - 1; pointIdx >= 0; pointIdx--)
                            {
                                PointD point = pd[pointIdx];
                                pointList.Add(new DoublePoint(point.X, point.Y));
                            }
                        }

                        id2 = (pds.Count > 1) ? (featureIdx + "-" + (partIdx + 1).ToString()) : id.ToString();
                        genFile.AddFeature(pointList, id, false);
                        if (settings.IsClockwiseOrderForced)
                        {
                            GENFeature genFeature = genFile.Features[genFile.Features.Count - 1];
                            if ((genFeature is GENPolygon) && GISUtils.IsClockwise(genFeature.Points))
                            {
                                ((GENPolygon)genFeature).ReversePoints();
                            }
                        }

                        AddDATFileRow(datFile, fieldValues, fieldTypes, id, id2, false, settings);
                        id++;
                    }

                    //        Point point = null;
                    //        if (vertice.Z_Cord.Equals(0))
                    //        {
                    //            point = new DoublePoint(vertice.X_Cord, vertice.Y_Cord);
                    //        }
                    //        else
                    //        {
                    //            point = new DoublePoint3D(vertice.X_Cord, vertice.Y_Cord, vertice.Z_Cord);
                    //        }
                    //        pointList.Add(point);
                }
                if (genFile.Count > 0)
                {
                    genFiles.Add(genFile);
                    datFiles.Add(datFile);
                }

                if (!hasFeatureParts)
                {
                    // When no features were present with multiple parts, the secondary ID-column can be removed in DAT-file(s)
                    foreach (DATFile datFile2 in datFiles)
                    {
                        datFile2.RemoveColumn(1);
                    }
                }

                // Write GEN-file(s)
                for (int fileIdx = 0; fileIdx < genFiles.Count; fileIdx++)
                {
                    genFile = genFiles[fileIdx];
                    datFile = datFiles[fileIdx];
                    string baseOutputFilename;
                    if (settings.OutputFilename == null)
                    {
                        baseOutputFilename = Path.Combine(settings.OutputPath, Path.GetFileName(shapeFilename));
                    }
                    else
                    {
                        baseOutputFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                    }
                    string genFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(baseOutputFilename) + ((genFiles.Count > 1) ? " #" + (fileIdx + 1).ToString() : string.Empty) + ".GEN");
                    genFile.Filename = genFilename;
                    if (datFile.Rows.Count > 0)
                    {
                        genFile.DATFile = datFile;
                    }

                    genFile.WriteFile(genFilename, null, Log, 1);
                    if (genFiles.Count > 1)
                    {
                        WriteExtent(genFile, FileUtils.AddFilePostFix(genFilename, "_extent"));
                    }
                }
            }
            catch (Exception ex)
            {
                    throw new Exception("Unexpected error when converting shapefile " + Path.GetFileName(shapeFilename), ex);
            }
            finally
            {
                if (dbfReader != null)
                {
                    dbfReader.Close();
                }
                if (sf != null)
                {
                    sf.Close();
                }
            }
        }

        protected void ConvertGENToShapefile(string genFilename, string outputPath, SIFToolSettings settings)
        {
            Log.AddInfo("Processing file " + FileUtils.GetRelativePath(genFilename, settings.InputPath) + " ... ", 1, false);
            GENFile genFile = ReadGENFile(genFilename, settings);
            Log.AddInfo(genFile.Count + " features found.");

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            ShapeFileWriter sfw;
            if (!genFile.HasDATFile())
            {
                genFile.AddDATFile();
            }
            DATFile datFile = genFile.DATFile;
            List<FieldDefinition> fieldDefinitions = GetFieldDefinitions(datFile);
            genFile.DATFile.AddMissingRows(GetDefaultValues(fieldDefinitions));

            List<GENPoint> genPoints = genFile.RetrieveGENPoints();
            List<GENLine> genLines = genFile.RetrieveGENLines();
            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();

            if (genPoints.Count > 0)
            {
                string baseOutputFilename;
                if (settings.OutputFilename == null)
                {
                    baseOutputFilename = Path.Combine(settings.OutputPath, Path.GetFileName(genFilename));
                }
                else
                {
                    baseOutputFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                }
                string shapeFilename = CreateShapeFilename(outputPath, baseOutputFilename, "_p", (genLines.Count > 0) || (genPolygons.Count > 0));
                Log.AddInfo("Writing point shapefile '" + Path.GetFileName(shapeFilename) + "' ...", 2);
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.Point, fieldDefinitions, datFile, Log, 2);
                for (int i = 0; i < genPoints.Count; i++)
                {
                    List<string> fieldData = datFile.GetRow(genPoints[i].ID);
                    CorrectSHPFieldData(fieldData, fieldDefinitions);
                    sfw.AddRecord(new PointD[] { new PointD(genPoints[i].Point.X, genPoints[i].Point.Y) }, 1, fieldData.ToArray());
                }
                sfw.Close();
            }
            if (genLines.Count > 0)
            {
                string shapeFilename = CreateShapeFilename(outputPath, genFilename, "_l", (genPoints.Count > 0) || (genPolygons.Count > 0));
                Log.AddInfo("Writing line shapefile '" + Path.GetFileName(shapeFilename) + "' ...", 2);
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.PolyLine, fieldDefinitions, datFile, Log, 2);
                for (int i = 0; i < genLines.Count; i++)
                {
                    ReadOnlyCollection<PointD[]> pointDArrays = pointDArrays = GetShapeFilePointData(genLines[i]);
                    List<string> fieldData = datFile.GetRow(genLines[i].ID);
                    CorrectSHPFieldData(fieldData, fieldDefinitions);
                    sfw.AddRecord(pointDArrays, fieldData.ToArray());
                }
                sfw.Close();
            }
            if (genPolygons.Count > 0)
            {
                string shapeFilename = CreateShapeFilename(outputPath, genFilename, "_v", (genPoints.Count > 0) || (genLines.Count > 0));
                Log.AddInfo("Writing polygon shapefile '" + Path.GetFileName(shapeFilename) + "' ...", 2);
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.Polygon, fieldDefinitions, datFile, Log, 2);
                for (int i = 0; i < genPolygons.Count; i++)
                {
                    GENPolygon genPolygon = genPolygons[i];
                    if (settings.IsClockwiseOrderForced && !GISUtils.IsClockwise(genPolygon.Points))
                    {
                        genPolygon.ReversePoints();
                    }
                      
                    ReadOnlyCollection<PointD[]> pointDArrays = pointDArrays = GetShapeFilePointData(genPolygon);
                    List<string> fieldData = datFile.GetRow(genPolygons[i].ID);
                    CorrectSHPFieldData(fieldData, fieldDefinitions);
                    sfw.AddRecord(pointDArrays, fieldData.ToArray());
                }
                sfw.Close();
            }
        }

        private void CorrectSHPFieldData(List<string> fieldData, List<FieldDefinition> fieldDefinitions)
        {
            for (int colIdx = 0; colIdx < fieldDefinitions.Count; colIdx++)
            {
                switch (fieldDefinitions[colIdx].Type)
                {
                    case DbfFieldType.Logical:
                        if (!fieldData[colIdx].Equals("?"))
                        {
                            bool boolValue = bool.Parse(fieldData[colIdx]);
                            fieldData[colIdx] = (boolValue) ? "T" : "F";
                        }
                        break;
                    case DbfFieldType.Date:
                        if (fieldData[colIdx].Equals(string.Empty))
                        {
                            fieldData[colIdx] = SIFToolSettings.NoDataSHPDateValue;
                        }
                        else
                        {
                            DateTime date = DateTime.Parse(fieldData[colIdx]);
                            fieldData[colIdx] = date.ToString("yyyyMMdd");
                        }
                        break;
                    case DbfFieldType.Number:
                    case DbfFieldType.FloatingPoint:
                        if (fieldData[colIdx].Equals(string.Empty))
                        {
                            fieldData[colIdx] = "\0";
                        }
                        break;
                    case DbfFieldType.Character:
                        if (fieldData[colIdx].Equals(string.Empty))
                        {
                            // An empty string doesn't seem to be allowed in a shapefile Character field, this is replaced by NULL.
                            fieldData[colIdx] = "\0"; //string.Empty.PadLeft(fieldDefinitions[colIdx].FieldLength + 1);
                        }
                        break;
                    default:
                        if (fieldData[colIdx] == null)
                        {
                            fieldData[colIdx] = string.Empty;
                        }
                        else
                        {
                            // leave current string value
                        }
                        break;
                }
            }
        }

        protected virtual GENFile ReadGENFile(string genFilename, SIFToolSettings settings)
        {
            return GENFile.ReadFile(genFilename, false);
        }

        protected void WriteExtent(GENFile genFile, string extentGENFilename)
        {
            GENFile extentGENFile = new GENFile();
            GENPolygon extentPolygon = new GENPolygon(extentGENFile, 1, genFile.Extent.ToPointList());
            extentPolygon.Points.Add(extentPolygon.Points[0]);
            extentGENFile.AddFeature(extentPolygon);
            extentGENFile.WriteFile(extentGENFilename);
        }

        protected void AddDATFileRow(DATFile datFile, string[] fields, DbfFieldType[] fieldTypes, int id, string id2, bool checkDuplicateIDs, SIFToolSettings settings)
        {
            List<string> rowValues = new List<string>();
            rowValues.Add(id.ToString());
            rowValues.Add(id2);

            if (settings.ShpNullNumericChars == null)
            {
                for (int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++)
                {
                    rowValues.Add(fields[fieldIdx].Trim());
                }
            }
            else
            {
                for (int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++)
                {
                    string value = fields[fieldIdx].Trim();
                    DbfFieldType fieldType = fieldTypes[fieldIdx];

                    if ((fieldType == DbfFieldType.FloatingPoint) || (fieldType == DbfFieldType.Number))
                    {
                        for (int charIdx = 0; charIdx < settings.ShpNullNumericChars.Length; charIdx++)
                        {
                            if ((value.Length > 0) && value[0].Equals(settings.ShpNullNumericChars[charIdx]))
                            {
                                string orgValue = value;

                                if (fieldTypes[fieldIdx] == DbfFieldType.FloatingPoint)
                                {
                                    value = settings.ShpNullDblReplacementString;
                                }
                                else if (fieldTypes[fieldIdx] == DbfFieldType.Number)
                                {
                                    value = settings.ShpNullIntReplacementString;
                                }

                                if (!isWarnedForNullValue)
                                {
                                    Log.AddWarning("Native NULL-value '" + orgValue + "' found in row with id " + id + " for " + fieldTypes[fieldIdx] + "-column '" + datFile.ColumnNames[fieldIdx] + "'", 3);
                                    Log.AddInfo("Value is replaced with '" + value + "'; Warnings for more native NULL-values in this shapefile are not shown", 3);
                                    isWarnedForNullValue = true;
                                }

                                // Stop trying other NULL-characters in this for-loop 
                                break;
                            }
                        }
                    }
                    else if (fieldType == DbfFieldType.Date)
                    {
                        if (!value.Equals(string.Empty))
                        {
                            if (value.Equals(SIFToolSettings.NoDataSHPDateValue))
                            {
                                value = settings.ShpNullDateReplacementString;
                            }
                            else
                            {
                                DateTime date = DateTime.ParseExact(value, "yyyyMMdd", EnglishCultureInfo, DateTimeStyles.None);
                                value = date.ToString(settings.DateFormat);
                            }
                        }
                    }
                    else if (fieldType == DbfFieldType.Logical)
                    {
                        switch (value)
                        {
                            case "F":
                                value = "False";
                                break;
                            case "T":
                                value = "True";
                                break;
                            default:
                                // leave value (which can be ? if undefined)
                                break;
                        }
                    }
                    else if (fieldType == DbfFieldType.Character)
                    {
                        if (value.Equals("NULL"))
                        {
                            value = string.Empty;
                        }
                    }

                    rowValues.Add(value);
                }
            }

            datFile.AddRow(new DATRow(rowValues), checkDuplicateIDs);
        }

        protected ReadOnlyCollection<PointD[]> GetShapeFilePointData(GENFeature genFeature)
        {
            List<PointD[]> pointDArrays = new List<PointD[]>();

            PointD[] pointDArray = new PointD[genFeature.Points.Count];
            for (int pointIdx = 0; pointIdx < genFeature.Points.Count; pointIdx++)
            {
                Point point = genFeature.Points[pointIdx];
                pointDArray[pointIdx] = new PointD(point.X, point.Y);
            }
            pointDArrays.Add(pointDArray);

            return new ReadOnlyCollection<PointD[]>(pointDArrays);
        }

        protected string CreateShapeFilename(string path, string genFilename, string optionalPostFix, bool isPostfixUSed)
        {
            string postfix = string.Empty;
            // When lines or points are present as well, these are processed seperately. Add a postfix _p to show that the outputfile only has points
            if (isPostfixUSed)
            {
                postfix = optionalPostFix;
            }
            return Path.Combine(path, Path.GetFileNameWithoutExtension(genFilename) + postfix + ".shp");
        }

        protected ShapeFileWriter CreateShapeFileWriter(string outputPath, string shapeFilename, ShapeType shapeType, List<FieldDefinition> fieldDefinitions, DATFile datFile, Log log, int logIndentLevel = 0)
        {
            ShapeFileWriter sfw = null;
            if (datFile != null)
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                List<string> columnNames = new List<string>();
                for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                {
                    FieldDefinition fieldDefinition = fieldDefinitions[colIdx];
                    try
                    {
                        if (fieldDefinition.Name.Length > 10)
                        {
                            fieldDefinition.Name = fieldDefinition.Name.Remove(10);

                            Log.AddWarning("Columnname shortenend to 10 characters: " + fieldDefinition.Name, logIndentLevel);
                        }

                        fieldDefinition.Name = GetUniqueColumnName(fieldDefinition.Name, columnNames, 10);

                        if (fieldDefinition.Decimalcount > 15)
                        {
                            // Maximum number of decimals for shapefiles is 15
                            Log.AddWarning(fieldDefinition.Decimalcount + " decimals were found for field '" + fieldDefinition.Name + "', which is limited to 15, some accuracy may be lost");
                            fieldDefinition.Decimalcount = 15;
                        }
                        DbfFieldDesc fieldDesc = fieldDefinition.ToDbfFieldDesc();
                        fieldDescs.Add(fieldDesc);
                        columnNames.Add(fieldDefinition.Name);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not create shapefile " + Path.GetFileName(shapeFilename), ex);
                    }
                }
                sfw = ShapeFileWriter.CreateWriter(Path.GetFullPath(outputPath), Path.GetFileNameWithoutExtension(shapeFilename), shapeType, fieldDescs.ToArray());
            }
            else
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                DbfFieldDesc idFieldDesc = new DbfFieldDesc();
                idFieldDesc.FieldName = "ID";
                idFieldDesc.FieldType = DbfFieldType.Character;
                idFieldDesc.DecimalCount = 0;
                fieldDescs.Add(idFieldDesc);

                sfw = ShapeFileWriter.CreateWriter(Path.GetFullPath(outputPath), Path.GetFileNameWithoutExtension(shapeFilename), shapeType, fieldDescs.ToArray());
            }

            return sfw;
        }

        /// <summary>
        /// Adds postfix "i" to specified columnname if it exists already, where i is the lowest available index
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string GetUniqueColumnName(string columnName, List<string> columNames, int maxLength = 0)
        {
            int idx = 1;
            string corrColumnName = columnName;

            while (columNames.Contains(corrColumnName))
            {
                idx++;
                if (maxLength != 0)
                {
                    corrColumnName = corrColumnName.Remove(10 - idx.ToString().Length) + idx.ToString();
                }
                else
                {
                    corrColumnName = columnName + idx.ToString();
                }
            }
            return corrColumnName;
        }

        protected List<FieldDefinition> GetFieldDefinitions(DATFile datFile)
        {
            // Initialize fieldTypes to some initial value
            List<FieldDefinition> fieldDefinitions = new List<FieldDefinition>();
            List<bool> isFieldTypeDefined = new List<bool>();
            bool hasScientificNotations = false;
            for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
            {
                isFieldTypeDefined.Add(false);
                fieldDefinitions.Add(new FieldDefinition(datFile.ColumnNames[colIdx], DbfFieldType.Logical));
            }

            List<DATRow> datRows = datFile.RowList;
            for (int rowIdx = 0; rowIdx < datFile.Rows.Count; rowIdx++)
            {
                List<string> row = datRows[rowIdx];
                for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                {
                    string columnName = datFile.ColumnNames[colIdx];
                    string value = row[colIdx];
                    value = CorrectStringValue(value);

                    FieldDefinition fieldDefinition = fieldDefinitions[colIdx];
                    int currFieldDefLength = fieldDefinition.FieldLength;
                    int currFieldDefDecCount = fieldDefinition.Decimalcount;
                    int valueLength = value.Length;

                    if (!value.Equals(string.Empty) && !value.ToUpper().Equals("NULL"))
                    {
                        if (value.Equals("?") || bool.TryParse(value, out bool boolValue))
                        {
                            // value is a boolean
                            if (!isFieldTypeDefined[colIdx])
                            {
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Logical, 1);
                                isFieldTypeDefined[colIdx] = true;
                            }
                            else if (!fieldDefinition.Type.Equals(DbfFieldType.Logical))
                            {
                                int fieldLength = (currFieldDefLength != 0) ? Math.Max((int)currFieldDefLength, Boolean.FalseString.Length) : Boolean.FalseString.Length;
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, fieldLength);
                            }
                            else
                            {
                                // leave fieldType to shpBool
                            }
                        }
                        else if (long.TryParse(value, out long longValue))
                        {
                            int fieldLength;
                            if (value.Contains("E"))
                            {
                                // Scientific format, retrieve number of digits
                                fieldLength = (int)Math.Log10(longValue);
                                hasScientificNotations = true;
                            }
                            else
                            {
                                fieldLength = value.Length;
                            }

                            // value is a long (or an integer, etc.)
                            if (!isFieldTypeDefined[colIdx])
                            {
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Number, fieldLength);
                                isFieldTypeDefined[colIdx] = true;
                            }
                            else if (fieldDefinition.Type.Equals(DbfFieldType.FloatingPoint))
                            {
                                // a long-value was found, leave fieldType to FloatingPoint, but redefine length
                                fieldLength = Math.Max(fieldLength + currFieldDefDecCount + 1, currFieldDefLength);
                                if (fieldLength > currFieldDefLength)
                                {
                                    fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, fieldLength, currFieldDefDecCount);
                                }
                            }
                            else if (!fieldDefinition.Type.Equals(DbfFieldType.Number))
                            {
                                // If the current type is other than double or long, a string will be used for this column
                                if (currFieldDefLength > fieldLength)
                                {
                                    fieldLength = currFieldDefLength;
                                }
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, fieldLength);
                            }
                            else
                            {
                                // leave fieldType to Number, but redefine length if necessary
                                if (fieldLength > currFieldDefLength)
                                {
                                    fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Number, fieldLength);
                                }
                            }
                        }
                        else if (double.TryParse(value, NumberStyles.Float, EnglishCultureInfo, out double doubleValue))
                        {
                            if (value.Contains("E"))
                            {
                                // Scientific format, convert to standard format 
                                value = doubleValue.ToString("0." + new string('#', 339), EnglishCultureInfo);
                                hasScientificNotations = true;
                            }

                            int fieldLength = value.Length;
                            int decimalCount = fieldLength - value.IndexOf(".") - 1;

                            // value is a double (or a float)
                            if (!isFieldTypeDefined[colIdx])
                            {
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, fieldLength, decimalCount);
                                isFieldTypeDefined[colIdx] = true;
                            }
                            else if (fieldDefinition.Type.Equals(DbfFieldType.Number))
                            {
                                // Current type was a long or integer, so current fieldlength refers to an integer fraction and current decimal count is zero
                                // For new fieldlength check current fieldlength, including the new number of decimals (and decimal point)
                                fieldLength = Math.Max(fieldLength, currFieldDefLength + decimalCount + 1);
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, fieldLength, decimalCount);
                            }
                            else if (!fieldDefinition.Type.Equals(DbfFieldType.FloatingPoint))
                            {
                                if (currFieldDefLength > fieldLength)
                                {
                                    fieldLength = currFieldDefLength;
                                }
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, fieldLength);
                            }
                            else
                            {
                                // leave fieldType to FloatingPoint, but redefine length: note both new/old integer and decimal fraction have to be compared
                                int intFractionLength = fieldLength - decimalCount - 1;
                                int currIntFractionLength = currFieldDefLength - currFieldDefDecCount - 1;
                                if (currIntFractionLength > intFractionLength)
                                {
                                    intFractionLength = currIntFractionLength;
                                }
                                if (currFieldDefDecCount > decimalCount)
                                {
                                    decimalCount = currFieldDefDecCount;
                                }

                                if ((intFractionLength > currIntFractionLength) || (decimalCount > currFieldDefDecCount))
                                {
                                    fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, intFractionLength + decimalCount + 1, decimalCount);
                                }
                            }
                        }
                        else if (DateTime.TryParse(value, out DateTime dateValue))
                        {
                            int fieldLength = valueLength;

                            // value is a date
                            if (!isFieldTypeDefined[colIdx])
                            {
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Date, 8);
                                isFieldTypeDefined[colIdx] = true;
                            }
                            else if (!fieldDefinition.Type.Equals(DbfFieldType.Date))
                            {
                                if (currFieldDefLength > fieldLength)
                                {
                                    fieldLength = currFieldDefLength;
                                }
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, fieldLength);
                            }
                            else
                            {
                                // leave fieldType to Date
                            }
                        }
                        else
                        {
                            // Set (or leave) fieldType to character, but redefine length if necessary
                            int fieldLength = value.Length;
                            if (currFieldDefLength > fieldLength)
                            {
                                fieldLength = currFieldDefLength;
                            }
                            if (!isFieldTypeDefined[colIdx])
                            {
                                isFieldTypeDefined[colIdx] = true;
                            }
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, fieldLength);
                        }
                    }
                    else
                    {
                        // Empty/NULL-value, leave current FieldType
                    }
                }
            }

            // Check if there are still undefined columns (which can happen if only empty/NULL-values are present
            for (int colIdx = 0; colIdx < isFieldTypeDefined.Count; colIdx++)
            {
                if (!isFieldTypeDefined[colIdx])
                {
                    // Define simple character field with length 0
                    fieldDefinitions[colIdx] = new FieldDefinition(datFile.ColumnNames[colIdx], DbfFieldType.Character);
                }
            }

            if (hasScientificNotations)
            {
                Log.AddWarning("Scientific notations were found and converted to standard numeric format, which may have effected accuracy");
            }

            return fieldDefinitions;
        }

        private static string CorrectStringValue(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }    
            else if (value.ToLower().Equals("infinity") || value.ToLower().Equals("inf")) 
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                return double.PositiveInfinity.ToString(EnglishCultureInfo);
            }
            else if (value.ToLower().Equals("-infinity") || value.ToLower().Equals("-inf"))
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                return double.NegativeInfinity.ToString(EnglishCultureInfo);
            }
            else
            {
                return value;
            }
        }

        protected static List<string> GetDefaultValues(List<FieldDefinition> fieldDefinitions)
        {
            List<string> defaultValues = new List<string>();
            for (int colIdx = 0; colIdx < fieldDefinitions.Count; colIdx++)
            {
                if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Logical))
                {
                    defaultValues.Add(bool.FalseString);
                }
                else if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Number))
                {
                    defaultValues.Add("0");
                }
                else if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.FloatingPoint))
                {
                    defaultValues.Add("0.0");
                }
                else if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Date))
                {
                    defaultValues.Add("01-01-1900");
                }
                else
                {
                    defaultValues.Add(string.Empty);
                }
            }
            return defaultValues;
        }
    }
}
