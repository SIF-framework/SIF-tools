// IPFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFselect.
// 
// IPFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFselect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.iMOD.Values;
using Sweco.SIF.iMODPlus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFselect
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
        /// Currently processed IPF-file
        /// </summary>
        protected IPFFile inputIPFFile;

        /// <summary>
        /// IDFFile object with top of volume used for selection
        /// </summary>
        protected IDFFile topIDFFile;

        /// <summary>
        /// IDFFile object with bottom of volume used for selection
        /// </summary>
        protected IDFFile botIDFFile;

        /// <summary>
        /// IDF- or GENFile object with zones used for selection
        /// </summary>
        protected IMODFile zoneFile;

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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            AddAuthor("Koen Jansen");
            ToolPurpose = "SIF-tool for selecting IPF-points with a conditional expression";
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

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            string[] inputFilenames = GetInputFiles(settings);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one IPF-file is found for current filter: " + settings.InputFilter);
            }

            int logIndentLevel = 1;
            foreach (string inputIPFFilename in inputFilenames)
            {
                ReadInputFiles(inputIPFFilename, settings, Log, logIndentLevel);

                Metadata resultIPFMetadata = settings.IsMetadataAdded ? CreateMetadata(inputIPFFile, settings) : null;

                string outputIPFFilename = null;
                if (settings.OutputFilename == null)
                {
                    outputIPFFilename = Path.Combine(settings.OutputPath, Path.GetFileName(inputIPFFilename));
                }
                else
                {
                    outputIPFFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                }

                if (!settings.IsTSSkipped)
                {
                    // Check for and remove invalid timeseries references (i.e. non-existing TS-files)
                    Log.AddInfo("Checking existance of associated timeseries files ...", logIndentLevel + 1);                    
                    foreach (IPFPoint ipfPoint in inputIPFFile.Points)
                    {
                        if (ipfPoint.HasTimeseriesDefined() && !ipfPoint.HasTimeseries())
                        {
                            Log.AddWarning("Removing reference to unexisting TS-file for point " + ipfPoint.ToString() + ": " + ipfPoint.ColumnValues[inputIPFFile.AssociatedFileColIdx], (logIndentLevel + 2));
                            ipfPoint.ColumnValues[inputIPFFile.AssociatedFileColIdx] = null;
                            ipfPoint.Timeseries = null;
                        }
                        else
                        {
                            ipfPoint.EnsureTimeseriesIsLoaded();
                        }
                    }
                }

                Log.AddInfo("Selecting points ...", logIndentLevel + 1);

                // Create list to keep track of indices of selected points from source IPF-file; initially all points are selected
                List<int> selSourcePointIndices = new List<int>();
                for (int idx = 0; idx < inputIPFFile.PointCount; idx++)
                {
                    selSourcePointIndices.Add(idx);
                }

                // Actually select points
                IPFFile outputIPFFile = SelectPoints(inputIPFFile, ref selSourcePointIndices, settings, Log, logIndentLevel + 2);

                // Check if selected points should be modified instead of selected
                if (settings.ColumnExpressionDefs != null)
                {
                    // Do not select points, but modify selected columnvalues
                    outputIPFFile = ModifyPoints(inputIPFFile, selSourcePointIndices, settings, Log, logIndentLevel + 2);
                }

                // Calculated number of selected/modified points
                Log.AddInfo("Selected " + selSourcePointIndices.Count + " / " + inputIPFFile.PointCount + " points", logIndentLevel + 1);

                // Write results
                if (outputIPFFile.AssociatedFileColIdx < -1)
                {
                    outputIPFFile.AssociatedFileColIdx = 0;
                }

                Log.AddInfo("Writing IPF-file '" + Path.GetFileName(outputIPFFilename) + "' ...", logIndentLevel);
                outputIPFFile.WriteFile(outputIPFFilename, resultIPFMetadata, !settings.IsTSSkipped);

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Retrieve array of filenames for specified input 
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual string[] GetInputFiles(SIFToolSettings settings)
        {
            return Directory.GetFiles(settings.InputPath, settings.InputFilter);
        }

        /// <summary>
        /// Reads input files depending on specified settings
        /// </summary>
        /// <param name="inputIPFFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ReadInputFiles(string inputIPFFilename, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            Log.AddInfo("Reading IPF-file " + Path.GetFileName(inputIPFFilename) + " ...", logIndentLevel);
            inputIPFFile = IPFFile.ReadFile(inputIPFFilename, settings.XColIdx, settings.YColIdx, settings.ZColIdx, false, true, null, logIndentLevel); //  !settings.IsTSSkipped
            if ((settings.IsMetadataAdded) && File.Exists(Path.ChangeExtension(inputIPFFilename, "MET")))
            {
                inputIPFFile.Metadata = Metadata.ReadMetaFile(inputIPFFilename);
            }

            if ((settings.XColIdx != 0) || (settings.YColIdx != 0) || (settings.ZColIdx >= 0))
            {
                string xyzMessage = "used x,y" + ((settings.ZColIdx >= 0) ? ",z" : string.Empty) + "-columns: " + inputIPFFile.ColumnNames[settings.XColIdx] + ", " + inputIPFFile.ColumnNames[settings.YColIdx] + ((settings.ZColIdx >= 0) ? (" and " + inputIPFFile.ColumnNames[settings.ZColIdx]) : string.Empty);
                log.AddInfo(xyzMessage, logIndentLevel + 1);
            }

            if (settings.TopLevelString != null)
            {
                float topValue;
                if (float.TryParse(settings.TopLevelString, NumberStyles.Float, EnglishCultureInfo, out topValue))
                {
                    topIDFFile = new ConstantIDFFile(topValue);
                }
                else
                {
                    log.AddInfo("Reading top IDF-file " + Path.GetFileName(settings.TopLevelString) + " ...", logIndentLevel);
                    topIDFFile = IDFFile.ReadFile(settings.TopLevelString);
                }
            }

            if (settings.BotLevelString != null)
            {
                float botValue;
                if (float.TryParse(settings.BotLevelString, NumberStyles.Float, EnglishCultureInfo, out botValue))
                {
                    botIDFFile = new ConstantIDFFile(botValue);
                }
                else
                {
                    log.AddInfo("Reading bottom IDF-file " + Path.GetFileName(settings.BotLevelString) + " ...", logIndentLevel);
                    botIDFFile = IDFFile.ReadFile(settings.BotLevelString);
                }
            }

            if (settings.ZoneFilename != null)
            {
                zoneFile = null;
                switch (Path.GetExtension(settings.ZoneFilename).ToLower())
                {
                    case ".idf":
                        log.AddInfo("Reading zone IDF-file " + Path.GetFileName(settings.ZoneFilename) + " ...", logIndentLevel);
                        zoneFile = IDFFile.ReadFile(settings.ZoneFilename);
                        break;
                    case ".gen":
                        log.AddInfo("Reading zone GEN-file " + Path.GetFileName(settings.ZoneFilename) + " ...", logIndentLevel);
                        zoneFile = GENFile.ReadFile(settings.ZoneFilename);
                        break;
                    default:
                        throw new Exception("Unknown extension for zone-file:" + Path.GetFileName(settings.ZoneFilename));
                }
            }
        }

        /// <summary>
        /// Select points from specified IPFFile object based on specified settings. 
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="selSourcePointIndices">list with indices of selected points from source IPF-file</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns>new IPF-file object with selected points</returns>
        protected virtual IPFFile SelectPoints(IPFFile ipfFile, ref List<int> selSourcePointIndices, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            // Select points based on specified expression
            if (!settings.ExpOperator.Equals(ValueOperator.Undefined))
            {
                string expColName = settings.ExpColReference;

                int colIdx = ipfFile.FindColumnIndex(settings.ExpColReference);
                if (colIdx >= 0)
                {
                    expColName = ipfFile.ColumnNames[colIdx];
                }
                else
                {
                    throw new ToolException("Invalid column reference c1 for IPF-file: " + settings.ExpColReference);
                }

                if ((colIdx >= 0) && (colIdx < ipfFile.ColumnCount))
                {
                    log.AddInfo("Selecting points with " + (settings.UseRegExp ? "RegExp-" : string.Empty) + "expression: " + ipfFile.ColumnNames[colIdx] + " " 
                        + settings.ExpOperator.ToString() + " " + settings.ExpValue + " ...", logIndentLevel);

                    // Check if column is a string column with string anomalies: values that can be parsed as another type
                    FieldType fieldType = ipfFile.GetFieldType(colIdx, out bool hasNullValues, out bool hasScientificNotation, out 
                        bool hasStringAnomalies,  out int stringAnomalies, out string stringAnomaly);
                    if ((fieldType == FieldType.String) && (stringAnomalies > 0))
                    {
                        if (stringAnomalies > (ipfFile.PointCount / 2))
                        {
                            log.AddWarning("Column '" + ipfFile.ColumnNames[colIdx] + "' is parsed as string column, but has more than 50% string anomalies (" + stringAnomalies + "/" + ipfFile.PointCount + " points)!", logIndentLevel);
                        }
                        else if (stringAnomalies > (ipfFile.PointCount / 4))
                        {
                            log.AddInfo("Column '" + ipfFile.ColumnNames[colIdx] + "' is parsed as string column, but has more than 25% string anomalies (" + stringAnomalies + "/" + ipfFile.PointCount + " points)", logIndentLevel);
                        }
                        log.AddInfo("An example of an anomaly that was found is: " + stringAnomaly, logIndentLevel);
                    }

                    if (hasNullValues)
                    {
                        log.AddInfo("Note: Column '" + ipfFile.ColumnNames[colIdx] + "' contains empty and/or null-values", logIndentLevel);
                    }

                    List<int> tmpPointIndices = new List<int>();
                    ipfFile = ipfFile.Select(colIdx, settings.ExpOperator, settings.ExpValue, tmpPointIndices, settings.UseRegExp);
                    selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
                }
                else
                {
                    throw new ToolException("Invalid expression column reference: " + settings.ExpColReference);
                }
            }

            // Select points in relation to specified extent
            if (settings.Extent != null)
            {
                log.AddInfo("Selecting points with extent ...", logIndentLevel);
                List<int> tmpPointIndices = new List<int>();
                ipfFile = IPFUtils.SelectPoints(ipfFile, settings.Extent, settings.SelectPointMethod, tmpPointIndices);
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
            }

            // Select points in relation to volume specified by TOP-and BOT-levels
            if (settings.BotLevelString != null || settings.TopLevelString != null)
            {
                log.AddInfo("Selecting points with TOP- and BOT-levels ...", logIndentLevel);

                // First check for and remove points NaN z-values
                int prevPointCount = ipfFile.PointCount;
                List<int> tmpPointIndices = new List<int>();
                ipfFile = ipfFile.Select(ipfFile.ZColIdx, ValueOperator.Unequal, "NaN", tmpPointIndices);
                int zValueNaNCount = prevPointCount - ipfFile.PointCount;
                if (zValueNaNCount > 0)
                {
                    log.AddWarning(zValueNaNCount + " point(s) with NaN-values in Z-column cannot be evaluated and were not selected", logIndentLevel);
                }
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);

                tmpPointIndices.Clear();
                ipfFile = IPFUtils.SelectPoints(ipfFile, topIDFFile, botIDFFile, settings.SelectPointMethod, tmpPointIndices);
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
            }

            // Select points in relation to specified zone(s)
            if (settings.ZoneFilename != null)
            {
                log.AddInfo("Selecting points in zone(s) ...", logIndentLevel);
                List<int> tmpPointIndices = new List<int>();
                if (zoneFile is GENFile)
                {
                    ipfFile = IPFUtils.SelectPoints(ipfFile, (GENFile)zoneFile, settings.SelectPointMethod, tmpPointIndices);
                }
                else if (zoneFile is IDFFile)
                {
                    ipfFile = IPFUtils.SelectPoints(ipfFile, (IDFFile)zoneFile, settings.ZoneValues, settings.SelectPointMethod, tmpPointIndices);
                }
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
            }

            if (settings.TSPeriodStartDate != null)
            {
                log.AddInfo("Selecting points with timeseries in period: " + ((DateTime)settings.TSPeriodStartDate).ToString("dd-MM-yyyy HH:mm:ss") 
                    + ((settings.TSPeriodEndDate != null) ? (" - " + ((DateTime)settings.TSPeriodEndDate).ToString("dd-MM-yyyy HH:mm:ss")) : string.Empty) + " ...", logIndentLevel);
                List<int> tmpPointIndices = new List<int>();
                ipfFile = IPFUtils.SelectPoints(ipfFile, settings.TSPeriodStartDate, settings.TSPeriodEndDate, settings.TSPeriodValueColIndex, settings.IsTSPeriodClipped, tmpPointIndices);
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
            }

            if (settings.IsEmptyTSPointRemoved)
            {
                log.AddInfo("Removing points without (non-empty) timeseries ...", logIndentLevel);
                List<int> tmpPointIndices = new List<int>();
                ipfFile = RemoveEmptyTSPoints(ipfFile, tmpPointIndices, log, logIndentLevel);
                selSourcePointIndices = ParseUtils.RetrieveIndexItems(tmpPointIndices, selSourcePointIndices);
            }

            return ipfFile;
        }

        /// <summary>
        /// Modify columnvalues of selected points in specified IPFFile object
        /// </summary>
        /// <param name="inputIPFFile"></param>
        /// <param name="selSourcePointIndices"></param>
        /// <param name="settings"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        private IPFFile ModifyPoints(IPFFile ipfFile, List<int> selSourcePointIndices, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            log.AddInfo("Modifying column values for selected points ... ", logIndentLevel);

            // First add new columns if requested and check for invalid column definitions
            for (int defIdx = 0; defIdx < settings.ColumnExpressionDefs.Count; defIdx++)
            {
                ColumnExpressionDef colExpDef = settings.ColumnExpressionDefs[defIdx];
                int colNr = ipfFile.FindColumnNumber(colExpDef.ColumnDefinition, true, false);
                if (colNr <= 0)
                {
                    if (int.TryParse(colExpDef.ColumnDefinition, out colNr))
                    {
                        throw new ToolException("Specified column not found for column/value-definition " + (defIdx + 1) + ": " + colExpDef.ToString());
                    }

                    // Column specified by a non-existing columnname, add it
                    ipfFile.AddColumn(colExpDef.ColumnDefinition, colExpDef.NoDataString);
                }
                else
                {
                    if ((colNr <= 0) || (colNr > ipfFile.ColumnCount))
                    {
                        throw new ToolException("Invalid column number for column/value-definition " + (defIdx + 1) + ": " + colExpDef.ToString());
                    }
                }
            }

            bool hasParseError = false;
            string parseErrorString = null;
            bool isParseErrorLogged = false;
            for (int selPointIdx = 0; selPointIdx < selSourcePointIndices.Count; selPointIdx++)
            {   
                int pointIdx = selSourcePointIndices[selPointIdx];
                IPFPoint ipfPoint = ipfFile.Points[pointIdx];
                for (int defIdx = 0; defIdx < settings.ColumnExpressionDefs.Count; defIdx++)
                {
                    ColumnExpressionDef colExpDef = settings.ColumnExpressionDefs[defIdx];
                    int colNr = ipfFile.FindColumnNumber(colExpDef.ColumnDefinition, true, false);
                    ipfPoint.ColumnValues[colNr - 1] = EvaluateExpression(ipfPoint.ColumnValues[colNr - 1], colExpDef, inputIPFFile, ipfPoint.ColumnValues, out hasParseError, out parseErrorString);
                    if (hasParseError && !isParseErrorLogged)
                    {
                        log.AddWarning("Expression value '" + (parseErrorString.Equals(string.Empty) ? "Empty" : parseErrorString) + "' in expression '" + colExpDef.ExpressionString + "' could not be parsed as a value for point " + ipfPoint.ToString(), logIndentLevel + 1);
                        log.AddInfo("Note: other parse errors are not logged.", logIndentLevel);
                        isParseErrorLogged = true;
                    }
                }
            }
            return ipfFile;
        }

        /// <summary>
        /// Remove empty points from specified IPF-file
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <returns></returns>
        private IPFFile RemoveEmptyTSPoints(IPFFile sourceIPFFile, List<int> srcPointIndices, Log log, int logIndentLevel)
        {
            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            List<IPFPoint> ipfPoints = sourceIPFFile.Points;
            for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
            {
                IPFPoint ipfPoint = ipfPoints[pointIdx];
                if (ipfPoint.HasTimeseries())
                {
                    if (ipfPoint.Timeseries.Values.Count() > 0)
                    {
                        newIPFFile.AddPoint(ipfPoint);
                        srcPointIndices.Add(pointIdx);
                    }
                    else
                    {
                        // skip point
                        log.AddWarning("Removing point " + ipfPoint.ToString() + " at row " + (pointIdx + 1) + " with empty timeseries", logIndentLevel);
                    }
                }
                else
                {
                    // skip point
                    log.AddWarning("Removing point " + ipfPoint.ToString() + " at row " + (pointIdx + 1) + " with empty timeseries", logIndentLevel);
                }
            }

            return newIPFFile;
        }

        private string EvaluateExpression(string currentValueString, ColumnExpressionDef colExpDef, IPFFile ipfInputFile, List<string> columnValues, out bool hasParseError, out string parseErrorString)
        {
            hasParseError = false;
            parseErrorString = null;
            if (colExpDef.ExpOperator == OperatorEnum.None)
            {
                // Try to parse string expression
                return ipfInputFile.EvaluateStringExpression(colExpDef.ExpressionString, columnValues);
            }
            else
            {
                // Parse mathematical expression
                try
                {
                    string expStringValue = colExpDef.ExpStringValue;
                    double expDoubleValue = colExpDef.ExpDoubleValue;

                    double resultValue;
                    if ((currentValueString == null) || currentValueString.Equals(string.Empty))
                    {
                        currentValueString = "0";
                    }
                    double currentValue;
                    if ( colExpDef.ExpStringValue.Contains("{"))
                    {
                        expStringValue = ipfInputFile.EvaluateStringExpression(colExpDef.ExpStringValue, columnValues);
                        if (!double.TryParse(expStringValue, NumberStyles.Float, EnglishCultureInfo, out expDoubleValue))
                        {
                            hasParseError = true;
                            parseErrorString = expStringValue;
                            expDoubleValue = double.NaN;
                        }
                    }
                    if (expDoubleValue.Equals(double.NaN) || !double.TryParse(currentValueString, NumberStyles.Float, EnglishCultureInfo, out currentValue))
                    {
                        // Process string expressions
                        if (colExpDef.ExpOperator == OperatorEnum.Add)
                        {
                            return currentValueString + expStringValue;
                        }
                        else if (colExpDef.ExpOperator == OperatorEnum.Substract)
                        {
                            // Remove substring currentValueString from existing string if existing
                            return ((expStringValue == null) || expStringValue.Equals(string.Empty)) ? currentValueString : currentValueString.Replace(expStringValue, string.Empty);
                        }
                        else
                        {
                            throw new ToolException("Expression is not defined for expression operator " + colExpDef.ExpOperator + ", NaN expression value and current value: " + currentValueString);
                        }
                    }
                    else
                    {
                        switch (colExpDef.ExpOperator)
                        {
                            case OperatorEnum.Multiply:
                                resultValue = currentValue * expDoubleValue;
                                break;
                            case OperatorEnum.Divide:
                                resultValue = currentValue / expDoubleValue;
                                break;
                            case OperatorEnum.Add:
                                resultValue = currentValue + expDoubleValue;
                                break;
                            case OperatorEnum.Substract:
                                resultValue = currentValue - expDoubleValue;
                                break;
                            default:
                                throw new Exception("Unexpected operator: " + colExpDef.ExpOperator);
                        }
                    }

                    // currentValueString = currentValue.ToString(englishCultureInfo);
                    // int decimalSeperatorIdx = currentValueString.IndexOf(".");
                    // int decimalCount = (decimalSeperatorIdx < 0) ? 0 : (currentValueString.Length - decimalSeperatorIdx - 1);
                    // return Math.Round(resultValue, decimalCount).ToString(englishCultureInfo);
                    return resultValue.ToString(EnglishCultureInfo);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Error during evaluation of expression: " + currentValueString + " " + colExpDef.ExpressionString, ex);
                }
            }
        }

        /// <summary>
        /// Creates metadata object based on specified source IPF-file and settings
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="settings"></param>
        protected virtual Metadata CreateMetadata(IPFFile ipfFile, SIFToolSettings settings)
        {
            Metadata newIPFMetadata = new Metadata(ipfFile.Filename);

            string metadataDescription = "IPF-selection";
            string metadataProcessDescription = "Created with " + ToolName + " version " + ToolVersion;
            if (settings.ExpColReference != null)
            {
                metadataProcessDescription += "; based on expression: column (" + settings.ExpColReference + ") " + settings.ExpOperator + " " + settings.ExpValue;
            }
            if (settings.ColumnExpressionDefs != null)
            {
                metadataProcessDescription += "; no selection, but modified columnvalues based on: '";
                foreach (ColumnExpressionDef columnExpressionDef in settings.ColumnExpressionDefs)
                {
                    metadataProcessDescription += columnExpressionDef.ColumnDefinition;
                    metadataProcessDescription += "," + columnExpressionDef.ExpressionString;
                    if ((columnExpressionDef.NoDataString != null) && !columnExpressionDef.NoDataString.Equals(""))
                    {
                        metadataProcessDescription += "," + columnExpressionDef.NoDataString;
                    }
                    metadataProcessDescription += ";";
                }
            }
            newIPFMetadata.Description = metadataDescription;
            newIPFMetadata.ProcessDescription = metadataProcessDescription;
            newIPFMetadata.Source = ipfFile.Filename;

            if (ipfFile.Metadata != null)
            {
                Metadata metadata = ipfFile.Metadata.Copy();
                metadata.MergeMetadata(newIPFMetadata);

                newIPFMetadata = metadata;
            }

            if (settings.TopLevelString != null)
            {
                if (topIDFFile is ConstantIDFFile)
                {
                    newIPFMetadata.ProcessDescription += "TOP-level: " + ((ConstantIDFFile)topIDFFile).ConstantValue + "; ";
                }
                else
                {
                    newIPFMetadata.ProcessDescription += "TOP IDF-file: " + topIDFFile.Filename + "; ";
                }
            }

            if (settings.BotLevelString != null)
            {
                if (botIDFFile is ConstantIDFFile)
                {
                    newIPFMetadata.ProcessDescription += "BOT-level: " + ((ConstantIDFFile)botIDFFile).ConstantValue + "; ";
                }
                else
                {
                    newIPFMetadata.ProcessDescription += "BOT IDF-file: " + botIDFFile.Filename + "; ";
                }
            }

            if (settings.ZoneFilename != null)
            {
                newIPFMetadata.ProcessDescription += "zone file: " + zoneFile.Filename + "; ";
            }

            if ((settings.XColIdx != 0) || (settings.YColIdx != 0) || (settings.ZColIdx >= 0))
            {
                string xyzMessage = "used x,y" + ((settings.ZColIdx >= 0) ? ",z" : string.Empty) + "-columns: " + ipfFile.ColumnNames[settings.XColIdx] + ", " + ipfFile.ColumnNames[settings.YColIdx] + ((settings.ZColIdx >= 0) ? (" and " + inputIPFFile.ColumnNames[settings.ZColIdx]) : string.Empty);
                newIPFMetadata.ProcessDescription += "; " + xyzMessage;
            }

            return newIPFMetadata;
        }
    }
}
