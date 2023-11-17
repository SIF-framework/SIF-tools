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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENSHPconvert
{
    public class SIFTool : SIFToolBase
    {
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

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            DATFile.IsErrorOnDuplicateID = !settings.IgnoreDuplicateIDs;
            GENFile.IsErrorOnDuplicateID = !settings.IgnoreDuplicateIDs;

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                if (Path.GetExtension(inputFilename).ToLower().Equals(".gen"))
                {
                    ConvertGENToShapefile(inputFilename, outputPath, settings);
                }
                else if (Path.GetExtension(inputFilename).ToLower().Equals(".shp"))
                {
                    ConvertShapefileToGEN(inputFilename, outputPath, settings);
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

                dbfReader = new DbfReader(System.IO.Path.ChangeExtension(shapeFilename, ".dbf"));
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
                for (int fieldIdx = 0; fieldIdx < fieldDescs.Length; fieldIdx++)
                {
                    DbfFieldDesc fieldDesc = fieldDescs[fieldIdx];
                    string fieldName = fieldDesc.FieldName;

                    // Fix existing ID or ID2 columnnames
                    if ((fieldName).ToUpper().Equals("ID"))
                    {
                        string newIdColName = "ID_ORG";
                        int idx = 2;
                        while (datFile.GetColIdx(newIdColName) >= 0)
                        {
                            newIdColName = "ID_ORG" + idx;
                        }
                        fieldName = newIdColName;
                    }
                    if ((fieldName).ToUpper().Equals("ID2"))
                    {
                        string newIdColName = "ID2_ORG";
                        int idx = 2;
                        while (datFile.GetColIdx(newIdColName) >= 0)
                        {
                            newIdColName = "ID2_ORG" + idx;
                        }
                        fieldName = newIdColName;
                    }
                    datFile.AddColumn(fieldName);
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

                    string[] fields = sf.GetAttributeFieldValues(featureIdx);

                    // Get the DBF record
                    IReadOnlyCollection<PointD[]> pds = sf.GetShapeDataD(featureIdx);
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

                        AddDatFileRow(datFile, fields, id, id2, false);
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

                for (int fileIdx = 0; fileIdx < genFiles.Count; fileIdx++)
                {
                    genFile = genFiles[fileIdx];
                    datFile = datFiles[fileIdx];
                    string genFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(shapeFilename) + ((genFiles.Count > 1) ? " #" + (fileIdx + 1).ToString() : string.Empty) + ".GEN");
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
                string shapeFilename = CreateShapeFilename(outputPath, genFilename, "_p", (genLines.Count > 0) || (genPolygons.Count > 0));
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.Point, fieldDefinitions, datFile);
                for (int i = 0; i < genPoints.Count; i++)
                {
                    ReadOnlyCollection<PointD[]> pointDArrays = pointDArrays = GetShapeFilePointData(genPoints[i]);
                    List<string> fieldData = datFile.GetRow(genPoints[i].ID);
                    sfw.AddRecord(pointDArrays, fieldData.ToArray());
                }
                sfw.Close();
            }
            if (genLines.Count > 0)
            {
                string shapeFilename = CreateShapeFilename(outputPath, genFilename, "_l", (genPoints.Count > 0) || (genPolygons.Count > 0));
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.PolyLine, fieldDefinitions, datFile);
                for (int i = 0; i < genLines.Count; i++)
                {
                    ReadOnlyCollection<PointD[]> pointDArrays = pointDArrays = GetShapeFilePointData(genLines[i]);
                    List<string> fieldData = datFile.GetRow(genLines[i].ID);
                    sfw.AddRecord(pointDArrays, fieldData.ToArray());
                }
                sfw.Close();
            }
            if (genPolygons.Count > 0)
            {
                string shapeFilename = CreateShapeFilename(outputPath, genFilename, "_v", (genPoints.Count > 0) || (genLines.Count > 0));
                sfw = CreateShapeFileWriter(outputPath, shapeFilename, ShapeType.Polygon, fieldDefinitions, datFile);
                for (int i = 0; i < genPolygons.Count; i++)
                {
                    GENPolygon genPolygon = genPolygons[i];
                    if (settings.IsClockwiseOrderForced && !GISUtils.IsClockwise(genPolygon.Points))
                    {
                        genPolygon.ReversePoints();
                    }
                      
                    ReadOnlyCollection<PointD[]> pointDArrays = pointDArrays = GetShapeFilePointData(genPolygon);
                    List<string> fieldData = datFile.GetRow(genPolygons[i].ID);
                    string[] fieldDataArray = CreateStringArray(fieldData);
                    sfw.AddRecord(pointDArrays, fieldDataArray);
                }
                sfw.Close();
            }
        }

        private string[] CreateStringArray(List<string> stringList)
        {
            string[] array = new string[stringList.Count];
            for (int idx = 0; idx < stringList.Count; idx++)
            {
                string item = stringList[idx];
                array[idx] = (item != null) ? item : string.Empty;
            }
            return array;
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

        protected void AddDatFileRow(DATFile datFile, string[] fields, int id, string id2, bool checkDuplicateIDs = true)
        {
            List<string> rowValues = new List<string>();
            rowValues.Add(id.ToString());
            rowValues.Add(id2);
            for (int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++)
            {
                string value = fields[fieldIdx].Trim();
                if (value.StartsWith(SIFToolSettings.ShpNoData1Prefix))
                {
                    rowValues.Add(SIFToolSettings.ShpNoData1ReplacementString);
                }
                else if (value.StartsWith(SIFToolSettings.ShpNoData2Prefix))
                {
                    rowValues.Add(SIFToolSettings.ShpNoData2ReplacementString);
                }
                else
                {
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

        protected ShapeFileWriter CreateShapeFileWriter(string outputPath, string shapeFilename, ShapeType shapeType, List<FieldDefinition> fieldDefinitions, DATFile datFile)
        {
            ShapeFileWriter sfw = null;
            if (datFile != null)
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                {
                    FieldDefinition fieldDefinition = fieldDefinitions[colIdx];
                    if (fieldDefinition.Name.Length > 10)
                    {
                        throw new Exception("Column name too long for shapefile (max. 10 characters): " + fieldDefinition.Name);
                    }

                    try
                    {
                        DbfFieldDesc fieldDesc = fieldDefinition.ToDbfFieldDesc();
                        fieldDescs.Add(fieldDesc);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Could not create shapefile " + Path.GetFileName(shapeFilename));
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

                sfw = ShapeFileWriter.CreateWriter(outputPath, shapeFilename, shapeType, fieldDescs.ToArray());
            }

            return sfw;
        }

        protected static List<FieldDefinition> GetFieldDefinitions(DATFile datFile)
        {
            // Initialize fieldTypes to some initial valuee
            List<FieldDefinition> fieldDefinitions = new List<FieldDefinition>();
            List<bool> isFieldTypeDefined = new List<bool>();
            for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
            {
                isFieldTypeDefined.Add(false);
                fieldDefinitions.Add(new FieldDefinition(datFile.ColumnNames[colIdx], DbfFieldType.Logical));
            }

            for (int rowIdx = 0; rowIdx < datFile.Rows.Count; rowIdx++)
            {
                List<string> row = datFile.Rows[rowIdx];
                for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                {
                    string columnName = datFile.ColumnNames[colIdx];
                    string value = row[colIdx];
                    int valueLength = (value != null) ? value.Length : 0;

                    if (bool.TryParse(value, out bool boolValue))
                    {
                        // value is a boolean
                        if (!isFieldTypeDefined[colIdx])
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Logical, 1);
                            isFieldTypeDefined[colIdx] = true;
                        }
                        else if (!fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Logical))
                        {
                            int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, Boolean.FalseString.Length) : Boolean.FalseString.Length;
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                        }
                        else
                        {
                            // leave fieldType to shpBool
                        }
                    }
                    else if (long.TryParse(value, out long longValue))
                    {
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, valueLength) : valueLength;

                        // value is a long (or an integer, etc.)
                        if (!isFieldTypeDefined[colIdx])
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Number, length);
                            isFieldTypeDefined[colIdx] = true;
                        }
                        else if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.FloatingPoint))
                        {
                            // leave fieldType to shpDouble, but redefine length
                            int decimalCount = length - value.IndexOf(".") - 1;
                            length = Math.Max(length, decimalCount + 8);
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length, decimalCount);
                        }
                        else if (!fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Number))
                        {
                            // If the current type is other than double or long, a string will be used for this column
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                        }
                        else
                        {
                            // leave fieldType to shpLong, but redefine length
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Number, length);
                        }
                    }
                    else if (double.TryParse(value, NumberStyles.Float, EnglishCultureInfo, out double doubleValue))
                    {
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, valueLength) : valueLength;
                        int decimalCount = valueLength - value.IndexOf(".") - 1;
                        if (decimalCount < 0)
                        {
                            decimalCount = 0;
                        }
                        length = Math.Max(length, decimalCount + 8);

                        // value is a double (or a float)
                        if (!isFieldTypeDefined[colIdx])
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, length, decimalCount);
                            isFieldTypeDefined[colIdx] = true;
                        }
                        else if (fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Number))
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, length, decimalCount);
                        }
                        else if (!fieldDefinitions[colIdx].Type.Equals(DbfFieldType.FloatingPoint))
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                        }
                        else
                        {
                            // leave fieldType to shpDouble, but redefine length
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.FloatingPoint, length, decimalCount);
                        }
                    }
                    else if (DateTime.TryParse(value, out DateTime dateValue))
                    {
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, valueLength) : valueLength;

                        // value is a date
                        if (!isFieldTypeDefined[colIdx])
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Date);
                            isFieldTypeDefined[colIdx] = true;
                        }
                        else if (!fieldDefinitions[colIdx].Type.Equals(DbfFieldType.Date))
                        {
                            fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                        }
                        else
                        {
                            // leave fieldType to shpDate
                        }
                    }
                    else
                    {
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, valueLength) : valueLength;
                        fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                    }
                }
            }
            return fieldDefinitions;
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

        protected object ParseValue(string value, DbfFieldType fieldType)
        {
            try
            {
                switch (fieldType)
                {
                    case DbfFieldType.Logical:
                        return bool.Parse(value);
                    case DbfFieldType.Date:
                        return DateTime.Parse(value);
                    case DbfFieldType.Number:
                        return long.Parse(value);
                    case DbfFieldType.FloatingPoint:
                        return double.Parse(value, EnglishCultureInfo);
                    case DbfFieldType.Character:
                        return value;
                   default:
                        throw new Exception("Unexpected fieldType: " + fieldType.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected value type in GEN-file row. Expected type: " + fieldType.ToString() + ", value: " + value, ex);
            }
        }

    }
}
