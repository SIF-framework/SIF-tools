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

namespace Sweco.SIF.IPFSHPconvert
{
    public class SIFTool : SIFToolBase
    {
        /// <summary>
        /// If true, a warning for NULL-values has been issued already
        /// </summary>
        protected bool isWarnedForNullValue;

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

                isWarnedForNullValue = false;
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
                // Create shapefilewriter, without XY-columns
                sfw = CreateShapeFileWriter(shapeFilename, ShapeType.Point, fieldDefinitions, ipfFile, 2, Log, logIndentLevel + 1);
                List<FieldDefinition> fieldDefinitions2 = fieldDefinitions.GetRange(2, fieldDefinitions.Count - 2);
                for (int pointIdx = 0; pointIdx < ipfPoints.Count; pointIdx++)
                {
                    IPFPoint point = ipfFile.GetPoint(pointIdx);
                    List<string> columnValues = point.ColumnValues.GetRange(2, point.ColumnValues.Count - 2);
                    string[] fieldData = columnValues.ToArray();
                    CorrectSHPFieldData(fieldData, fieldDefinitions2);
                    sfw.AddRecord(new PointD[] { new PointD( point.X, point.Y ) }, 1, fieldData);
                }
                sfw.Close();
            }
        }

        private void CorrectSHPFieldData(string[] fieldData, List<FieldDefinition> fieldDefinitions)
        {
            for (int colIdx = 0; colIdx < fieldDefinitions.Count; colIdx++)
            {
                string fieldValue = fieldData[colIdx];
                switch (fieldDefinitions[colIdx].Type)
                {
                    case DbfFieldType.Logical:
                        if (fieldValue.Equals(string.Empty) || fieldValue.ToUpper().Equals("NULL"))
                        {
                            fieldData[colIdx] = "?";
                        }
                        else if (!fieldData[colIdx].Equals("?"))
                        {
                            bool boolValue = bool.Parse(fieldValue);
                            fieldData[colIdx] = (boolValue) ? "T" : "F";
                        }
                        break;
                    case DbfFieldType.Date:
                        if (fieldValue.Equals(string.Empty))
                        {
                            fieldData[colIdx] = SIFToolSettings.NoDataSHPDateValue;
                        }
                        else
                        {
                            DateTime date = DateTime.Parse(fieldValue);
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
                        else if (fieldData[colIdx].Length > fieldDefinitions[colIdx].FieldLength)
                        {
                            fieldData[colIdx] = fieldValue.Substring(fieldDefinitions[colIdx].FieldLength);
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
                DbfFieldType[] fieldTypes = new DbfFieldType[fieldDescs.Length];
                for (int fieldIdx = 0; fieldIdx < fieldDescs.Length; fieldIdx++)
                {
                    DbfFieldDesc fieldDesc = fieldDescs[fieldIdx];

                    // Floating point values are stored/retrieved as Number with EGIS; overrule with FloatingPoint type if decimals are present
                    fieldTypes[fieldIdx] = fieldDesc.FieldType;
                    if ((fieldDesc.FieldType == DbfFieldType.Number) && (fieldDesc.DecimalCount > 0))
                    {
                        // Override and use floating point when decimals are defined
                        fieldTypes[fieldIdx] = DbfFieldType.FloatingPoint;
                    }
                }

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

                        if (settings.ShpNullNumericChars == null)
                        {
                            for (int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++)
                            {
                                columnValues.Add(fields[fieldIdx].Trim());
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
                                    if (!value.Equals(string.Empty))
                                    {
                                        // Check if valuestring starts with any of the specified NULL-characters
                                        for (int charIdx = 0; charIdx < settings.ShpNullNumericChars.Length; charIdx++)
                                        {
                                            if (value[0].Equals(settings.ShpNullNumericChars[charIdx]))
                                            {
                                                string orgValue = value;

                                                if (fieldType == DbfFieldType.FloatingPoint)
                                                {
                                                    value = settings.ShpNullDblReplacementString;
                                                }
                                                else if (fieldType == DbfFieldType.Number)
                                                {
                                                    value = settings.ShpNullIntReplacementString;
                                                }

                                                if (!isWarnedForNullValue)
                                                {
                                                    Log.AddWarning("Native NULL-value '" + orgValue + "' found in row number " + featureIdx + " for " + fieldTypes[fieldIdx] + "-column '" + fieldDescs[fieldIdx].FieldName + "'", 3);
                                                    Log.AddInfo("Value is replaced with '" + value + "'; Warnings for more native NULL-values in this shapefile are not shown", 3);
                                                    isWarnedForNullValue = true;
                                                }

                                                // Stop trying other NULL-characters in this for-loop 
                                                break;
                                            }
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

                                value = CorrectIPFStringValue(value);

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

        /// <summary>
        /// Correct string that will be stored in an IPF-file
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string CorrectIPFStringValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            // Remove end-of-line characters
            if (value.IndexOfAny(new char[] { '\r', '\n' }) >= 0)
            {
                value = value.Replace("\r", string.Empty).Replace("\n", string.Empty);
            }

            // Replace double quotes
            if (value.Contains("\""))
            {
                if (value.StartsWith("\"") && value.EndsWith("\n"))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                value = value.Replace("\"", "'");
            }

            return value;
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

        protected List<FieldDefinition> GetFieldDefinitions(IPFFile ipfFile)
        {
            // Initialize fieldTypes to some initial value
            List<FieldDefinition> fieldDefinitions = new List<FieldDefinition>();
            List<bool> isFieldTypeDefined = new List<bool>();
            bool hasScientificNotations = false;
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
                    value = CorrectSHPStringValue(value);

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
                                fieldLength = valueLength;
                            }

                            // value is a long (or an integer, etc.)
                            if (!isFieldTypeDefined[colIdx])
                            {
                                fieldDefinitions[colIdx] = new FieldDefinition(columnName, DbfFieldType.Number, fieldLength);
                                isFieldTypeDefined[colIdx] = true;
                            }
                            else if (fieldDefinition.Type.Equals(DbfFieldType.FloatingPoint))
                            {
                                // a long-value was found, leave fieldType to FloatingPoint, but redefine length:
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
                                value = doubleValue.ToString("0." + new string('#', 339), EnglishCultureInfo); // doubleValue.ToString("G", EnglishCultureInfo);
                                hasScientificNotations = true;
                            }

                            int fieldLength = valueLength;
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
                            int fieldLength = valueLength;
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
                    fieldDefinitions[colIdx] = new FieldDefinition(ipfFile.ColumnNames[colIdx], DbfFieldType.Character);
                }

                if (fieldDefinitions[colIdx].FieldLength > 254)
                {
                    // The shapefile maximum field width is 254. It is a limitation of the dBase format.
                    fieldDefinitions[colIdx].FieldLength = 254;
                }
            }


            if (hasScientificNotations)
            {
                Log.AddWarning("Scientific notations were found and converted to standard numeric format, which may have effected accuracy");
            }

            return fieldDefinitions;
        }

        /// <summary>
        /// Correct string that will be stored in a shapefile
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string CorrectSHPStringValue(string value)
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

        protected ShapeFileWriter CreateShapeFileWriter(string shapeFilename, ShapeType shapeType, List<FieldDefinition> fieldDefinitions, IPFFile ipfFile, int startColIdx, Log log, int logIndentLevel = 0)
        {
            ShapeFileWriter sfw = null;
            if (ipfFile != null)
            {
                List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
                List<string> columnNames = new List<string>();
                for (int colIdx = startColIdx; colIdx < ipfFile.ColumnNames.Count; colIdx++)
                {
                    FieldDefinition fieldDefinition = fieldDefinitions[colIdx];
                    try
                    {
                        if (fieldDefinition.Name.Length > 10)
                        {
                            fieldDefinition.Name = fieldDefinition.Name.Remove(10);
                            log.AddWarning("Columnname shortenend to 10 characters: " + fieldDefinition.Name, logIndentLevel);
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
