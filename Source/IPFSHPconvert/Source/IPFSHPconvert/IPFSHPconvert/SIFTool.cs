// IPFSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFSHPconvert.
// 
// IPFSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using EGIS.ShapeFileLib;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Sweco.SIF.IPFSHPconvert
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
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
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for converting IPF-file(s) to shapefile(s) or vice versa";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            // Create output path if not yet existing
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
            
            foreach (string inputFilename in inputFilenames)
            {
                string outputSubdirectory = Path.GetDirectoryName(FileUtils.GetRelativePath(inputFilename, settings.InputPath));
                string outputFilename = Path.GetFullPath(Path.Combine(settings.OutputPath, Path.Combine(outputSubdirectory, settings.OutputFilename != null ? settings.OutputFilename : Path.GetFileName(inputFilename))));
                FileUtils.EnsureFolderExists(outputFilename);

                if (Path.GetExtension(inputFilename).ToLower().Equals(".ipf"))
                {
                    outputFilename = Path.ChangeExtension(outputFilename, ".shp");
                    ConvertIPFToShapefile(inputFilename, outputFilename, settings, 1);
                    fileCount++;
                }
                else if (Path.GetExtension(inputFilename).ToLower().Equals(".shp"))
                {
                    outputFilename = Path.ChangeExtension(outputFilename, ".IPF");
                    ConvertShapefileToIPF(inputFilename, outputFilename, settings, 1);
                    fileCount++;
                }
                else
                {
                    continue;
                }
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected void ConvertIPFToShapefile(string ipfFilename, string shapeFilename, SIFToolSettings settings, int logIndentLevel)
        {
            Log.AddInfo("Processing IPF-file '" + FileUtils.GetRelativePath(ipfFilename, settings.InputPath) + "' ... ", logIndentLevel, false);
            IPFFile ipfFile = null;
            try
            {
                ipfFile = ReadIPFFile(ipfFilename, settings, logIndentLevel);
            }
            catch (Exception ex)
            {
                // Add newline and throw exeception further up
                Log.AddInfo();
                throw ex;
            }
            Log.AddInfo(ipfFile.PointCount + " points found");

            ShapeFileWriter sfw;
            List<FieldDefinition> fieldDefinitions = GetFieldDefinitions(ipfFile);
            List<IPFPoint> ipfPoints = ipfFile.Points;
            if (!settings.IsOverwrite && File.Exists(shapeFilename))
            {
                throw new ToolException("Outputfile already exists, use 'overwrite' option to overwrite: " + shapeFilename);
            }
            else
            {
                sfw = CreateShapeFileWriter(shapeFilename, ShapeType.Point, fieldDefinitions, ipfFile, Log, logIndentLevel + 1);
                for (int pointIdx = 0; pointIdx < ipfPoints.Count; pointIdx++)
                {
                    IPFPoint point = ipfFile.GetPoint(pointIdx);
                    sfw.AddRecord(new PointD[] { new PointD( point.X, point.Y ) }, 1, point.ColumnValues.ToArray());
                }
                sfw.Close();
            }
        }

        protected virtual IPFFile ReadIPFFile(string ipfFilename, SIFToolSettings settings, int logIndentLevel)
        {
            return IPFFile.ReadFile(ipfFilename);
        }

        private void ConvertShapefileToIPF(string shapeFilename, string ipfFilename, SIFToolSettings settings, int logIndentLevel)
        {
            Log.AddInfo("Processing shapefile " + FileUtils.GetRelativePath(shapeFilename, settings.InputPath) + " ... ", logIndentLevel, false);
            ShapeFile sf = null;
            DbfReader dbfReader = null;
            try
            {
                sf = new EGIS.ShapeFileLib.ShapeFile(shapeFilename);
                string[] record = sf.GetRecords(0);
                if (record != null)
                {
                    Log.AddInfo(record.Length + " features found");
                }
                else
                {
                    Log.AddInfo("no features found");
                }

                Log.AddInfo(string.Format("Shape Type: {0}", sf.ShapeType), logIndentLevel);

                dbfReader = new DbfReader(System.IO.Path.ChangeExtension(shapeFilename, ".dbf"));
                ShapeType shapeType = sf.ShapeType;
                if (sf.ShapeType == ShapeType.PolygonZ)
                {
                    // Currently ignore PolgonZ 
                    shapeType = ShapeType.Polygon;
                }

                DbfFieldDesc[] fieldDescs = dbfReader.DbfRecordHeader.GetFieldDescriptions();

                IPFFile ipfFile = new IPFFile();
                AddColumns(ipfFile, fieldDescs, settings, logIndentLevel);

                // Calculate number of points between 5% logmessages, use multiple of 50
                int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(sf.RecordCount, 5);

                bool hasFeatureParts = false;
                for (int featureIdx = 0; featureIdx < sf.RecordCount; ++featureIdx)
                {
                    if (featureIdx % logSnapPointMessageFrequency == 0)
                    {
                        Log.AddInfo("Reading features " + (featureIdx + 1) + "-" + (int)Math.Min(sf.RecordCount, (featureIdx + logSnapPointMessageFrequency)) + " of " + sf.RecordCount + " ...", logIndentLevel +1);
                    }

                    // Get the DBF record
                    string[] fields = sf.GetAttributeFieldValues(featureIdx);

                    // Get and process points
                    IReadOnlyCollection<PointD[]> pds = sf.GetShapeDataD(featureIdx);
                    hasFeatureParts |= (pds.Count > 1);
                    for (int partIdx = 0; partIdx < pds.Count; partIdx++)
                    {
                        PointD[] pd = pds.ElementAt(partIdx);
                        List<IPFPoint> pointList = new List<IPFPoint>();

                        List<string> columnValues = new List<string>();
                        for (int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++)
                        {
                            string value = fields[fieldIdx].Trim();
                            if (value.StartsWith(SIFToolSettings.ShpNoData1Prefix))
                            {
                                columnValues.Add(SIFToolSettings.ShpNoData1ReplacementString);
                            }
                            else if (value.StartsWith(SIFToolSettings.ShpNoData2Prefix))
                            {
                                columnValues.Add(SIFToolSettings.ShpNoData2ReplacementString);
                            }
                            else
                            {
                                columnValues.Add(value);
                            }
                        }
                        if (settings.IsFeatureIdxAdded)
                        {
                            columnValues.Add(featureIdx.ToString());
                        }

                        // Add points to ipffile and assign attribute values/record to point
                        if (partIdx >= 0)
                        {
                            foreach (PointD point in pd)
                            {
                                FloatPoint dummyFloatPoint = new FloatPoint((float)point.X, (float)point.Y);
                                IPFPoint ipfPoint = new IPFPoint(ipfFile, dummyFloatPoint, columnValues);
                                pointList.Add(ipfPoint);
                            }
                        }
                        else
                        {
                            for (int pointIdx = pd.Length - 1; pointIdx >= 0; pointIdx--)
                            {
                                PointD point = pd[pointIdx];
                                FloatPoint dummyFloatPoint = new FloatPoint((float)point.X, (float)point.Y);
                                IPFPoint ipfPoint = new IPFPoint(ipfFile, dummyFloatPoint, columnValues);

                                pointList.Add(ipfPoint);
                            }
                        }

                        ipfFile.AddPoints(pointList);
                    }
                }

                if (!settings.IsOverwrite && File.Exists(ipfFilename))
                {
                    throw new ToolException("Outputfile already exists, use 'overwrite' option to overwrite: " + Path.GetFileName(ipfFilename));
                }
                else
                {
                    WriteIPFFile(ipfFilename, ipfFile, settings, logIndentLevel);
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

        protected virtual void WriteIPFFile(string ipfFilename, IPFFile ipfFile, SIFToolSettings settings, int logIndentLevel)
        {
            Log.AddInfo("Creating IPF-file " + Path.GetFileName(ipfFilename) + " ...", logIndentLevel);
            ipfFile.WriteFile(ipfFilename);
        }

        protected virtual void AddColumns(IPFFile ipfFile, DbfFieldDesc[] fieldDescs, SIFToolSettings settings, int logIndentLevel)
        {
            ipfFile.AddXYColumns();

            for (int fieldIdx = 0; fieldIdx < fieldDescs.Length; fieldIdx++)
            {
                DbfFieldDesc fieldDesc = fieldDescs[fieldIdx];
                string fieldName = fieldDesc.FieldName;
                fieldName = ipfFile.FindUniqueColumnName(fieldName);
                ipfFile.AddColumn(fieldName);
            }

            if (settings.IsFeatureIdxAdded)
            {
                ipfFile.AddColumn(settings.FeatureIdxColumnName);
            }
        }


        protected static List<FieldDefinition> GetFieldDefinitions(IPFFile ipfFile)
        {
            // Initialize fieldTypes to some initial valuee
            List<FieldDefinition> fieldDefinitions = new List<FieldDefinition>();
            List<bool> isFieldTypeDefined = new List<bool>();
            for (int colIdx = 0; colIdx < ipfFile.ColumnNames.Count; colIdx++)
            {
                isFieldTypeDefined.Add(false);
                fieldDefinitions.Add(new FieldDefinition(ipfFile.ColumnNames[colIdx], DbfFieldType.Logical));
            }

            for (int pointIdx = 0; pointIdx < ipfFile.PointCount; pointIdx++)
            {
                IPFPoint point = ipfFile.GetPoint(pointIdx);
                
                for (int colIdx = 0; colIdx < ipfFile.ColumnNames.Count; colIdx++)
                {
                    string columnName = ipfFile.ColumnNames[colIdx];
                    string value = point.ColumnValues[colIdx];

                    value = CorrectStringValue(value);

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
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, value.Length) : value.Length;

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
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, value.Length) : value.Length;
                        int decimalCount = value.Length - value.IndexOf(".") - 1;
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
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, value.Length) : value.Length;

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
                        int length = (fieldDefinitions[colIdx].Length != 0) ? Math.Max((int)fieldDefinitions[colIdx].Length, value.Length) : value.Length;
                        fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Character, length);
                    }
                }
            }
            return fieldDefinitions;
        }

        private static string CorrectStringValue(string value)
        {
            if (value.Equals("Infinity"))
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                value = double.PositiveInfinity.ToString(EnglishCultureInfo);
            }
            else if (value.Equals("-Infinity"))
            {
                // For infinity, try Inf which is string that is recognized as a floating point value
                value = double.NegativeInfinity.ToString(EnglishCultureInfo);
            }

            return value;
        }

        protected ReadOnlyCollection<PointD[]> GetShapeFilePointData(List<IPFPoint> ipfPoints)
        {
            List<PointD[]> pointDArrays = new List<PointD[]>();
            PointD[] pointDArray = new PointD[ipfPoints.Count()];
            for (int pointIdx = 0; pointIdx < ipfPoints.Count(); pointIdx++)
            {
                Point point = ipfPoints[pointIdx];
                pointDArray[pointIdx] = new PointD(point.X, point.Y);
            }
            pointDArrays.Add(pointDArray);

            return new ReadOnlyCollection<PointD[]>(pointDArrays);
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

        protected ShapeFileWriter CreateShapeFileWriter(string shapeFilename, ShapeType shapeType, List<FieldDefinition> fieldDefinitions, IPFFile ipfFile, Log log, int logIndentLevel = 0)
        {
            ShapeFileWriter sfw = null;
            if (ipfFile != null)
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                List<string> columNames = new List<string>();
                for (int colIdx = 0; colIdx < ipfFile.ColumnNames.Count; colIdx++)
                {
                    FieldDefinition fieldDefinition = fieldDefinitions[colIdx];
                    try
                    {
                        if (fieldDefinition.Name.Length > 10)
                        {
                            fieldDefinition.Name = fieldDefinition.Name.Remove(10);


                            log.AddWarning("Columnname shortenend to 10 characters: " + fieldDefinition.Name, logIndentLevel);
                        }

                        fieldDefinition.Name = GetUniqueColumnName(fieldDefinition.Name, columNames, 10);

                        DbfFieldDesc fieldDesc = fieldDefinition.ToDbfFieldDesc();
                        fieldDescs.Add(fieldDesc);
                        columNames.Add(fieldDefinition.Name);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not create shapefile " + Path.GetFileName(shapeFilename), ex);
                    }
                }
                sfw = ShapeFileWriter.CreateWriter(Path.GetDirectoryName(shapeFilename), Path.GetFileNameWithoutExtension(shapeFilename), shapeType, fieldDescs.ToArray());
            }
            else
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                DbfFieldDesc idFieldDesc = new DbfFieldDesc();
                idFieldDesc.FieldName = "ID";
                idFieldDesc.FieldType = DbfFieldType.Character;
                idFieldDesc.DecimalCount = 0;
                fieldDescs.Add(idFieldDesc);

                sfw = ShapeFileWriter.CreateWriter(Path.GetDirectoryName(shapeFilename), Path.GetFileNameWithoutExtension(shapeFilename), shapeType, fieldDescs.ToArray());
            }

            return sfw;
        }
    }
}
