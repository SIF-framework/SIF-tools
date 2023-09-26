// GENselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENselect.
// 
// GENselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENselect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.iMOD.Values;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENselect
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
        /// Currently processed GEN-file
        /// </summary>
        protected GENFile inputGENFile;

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
            ToolPurpose = "SIF-tool for selecting features in GEN-files";
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


            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one GEN-file is found for current filter: " + settings.InputFilter);
            }

            int logIndentLevel = 1;
            foreach (string inputGENFilename in inputFilenames)
            {
                ReadInputFiles(inputGENFilename, settings, Log, logIndentLevel);
                Metadata resultGENMetadata = CreateMetadata(inputGENFilename, settings);

                Log.AddInfo("Selecting features ...", logIndentLevel + 1);

                // Create list to keep track of indices of selected features from source GEN-file; initially all features are selected
                List<int> selSourceFeatureIndices = new List<int>();
                for (int idx = 0; idx < inputGENFile.Count; idx++)
                {
                    selSourceFeatureIndices.Add(idx);
                }

                // Actually select features
                GENFile resultGENFile = SelectFeatures(inputGENFile, ref selSourceFeatureIndices, settings, Log, logIndentLevel + 2);

                // Check if selected points should be modified instead of selected
                if (settings.ColumnExpressionDefs != null)
                {
                    // Do not select points, but modify selected columnvalues
                    resultGENFile = ModifyFeatures(inputGENFile, selSourceFeatureIndices, settings, Log, logIndentLevel + 2);
                }

                // Calculated number of selected/modified points
                Log.AddInfo("Selected " + selSourceFeatureIndices.Count + " / " + inputGENFile.Count + " features", logIndentLevel + 1);

                // Write results
                string outputFilename = Path.Combine(settings.OutputPath, settings.OutputFilename ?? Path.GetFileName(inputGENFilename));
                Log.AddInfo("Writing GEN-file '" + Path.GetFileName(outputFilename) + "' ...", logIndentLevel);
                resultGENFile.WriteFile(outputFilename);

                fileCount++;
            }

            Log.AddInfo();
            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Reads input files depending on specified settings
        /// </summary>
        /// <param name="inputGENFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ReadInputFiles(string inputGENFilename, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            Log.AddInfo("Reading GEN-file " + Path.GetFileName(inputGENFilename) + " ...", logIndentLevel);
            inputGENFile = GENFile.ReadFile(inputGENFilename);
        }

        /// <summary>
        /// Select features from specified GENFile object based on specified settings. Fea
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="selSourceFeatureIndices">list with indices of selected features from source GEN-file</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual GENFile SelectFeatures(GENFile genFile, ref List<int> selSourceFeatureIndices, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            // Select points based on specified expression
            if (!settings.ExpOperator.Equals(ValueOperator.Undefined))
            {
                if (!genFile.HasDATFile())
                {
                    throw new ToolException("GEN-file has no DAT-file, selection on column values is not possible: " + Path.GetFileName(genFile.Filename));
                }

                string expColName = settings.ExpColReference;
                int colIdx = genFile.DATFile.FindColumnIndex(settings.ExpColReference);
                if (colIdx >= 0)
                {
                    expColName = genFile.DATFile.ColumnNames[colIdx];
                }
                else
                {
                    throw new ToolException("Invalid column reference for GEN-file: " + settings.ExpColReference);
                }

                if ((colIdx >= 0) && (colIdx < genFile.DATFile.ColumnNames.Count))
                {
                    log.AddInfo("Selecting features with " + (settings.UseRegExp ? "RegExp-" : string.Empty) + "expression: " + genFile.DATFile.ColumnNames[colIdx] + " "
                        + settings.ExpOperator.ToString() + " " + settings.ExpValue + " ...", logIndentLevel);
                    List<int> tmpFeatureIndices = new List<int>();
                    genFile = genFile.Select(colIdx, settings.ExpOperator, settings.ExpValue, tmpFeatureIndices, settings.UseRegExp);
                    selSourceFeatureIndices = ParseUtils.RetrieveIndexItems(tmpFeatureIndices, selSourceFeatureIndices);
                }
                else
                {
                    throw new ToolException("Invalid expression column index: " + settings.ExpColReference);
                }
            }

            if (settings.SizeOperator != SizeOperator.Undefined)
            {
                log.AddInfo("Selecting features with size " + settings.SizeOperator.ToString() + " " + settings.SizeValue, logIndentLevel);
                List<int> tmpFeatureIndices = new List<int>();
                genFile = Select(genFile, settings.SizeOperator, settings.SizeValue, tmpFeatureIndices);
                selSourceFeatureIndices = ParseUtils.RetrieveIndexItems(tmpFeatureIndices, selSourceFeatureIndices);
            }

            return genFile;
        }

        /// <summary>
        /// Selects features with specified size
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="sizeOperator"></param>
        /// <param name="sizeValue"></param>
        /// <param name="srcFeatureIndices">optional (empty, non-null) list to store indices to selected features in source GEN-file</param>
        /// <returns></returns>
        private GENFile Select(GENFile genFile, SizeOperator sizeOperator, float sizeValue, List<int> srcFeatureIndices)
        {
            if (genFile == null)
            {
                return null;
            }

            List<GENFeature> features = genFile.Features;
            GENFile newGENFile = new GENFile();
            if (genFile.HasDATFile())
            {
                newGENFile.AddDATFile();
                newGENFile.DATFile.ColumnNames = new List<string>(genFile.DATFile.ColumnNames);
            }

            if (genFile.Features.Count == 0)
            {
                return newGENFile;
            }

            if (srcFeatureIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcFeatureIndices = new List<int>();
            }

            switch (sizeOperator)
            {
                case SizeOperator.Min:
                    double minSize = double.MaxValue;
                    GENFeature minFeature = null;
                    int minFeatureIdx = -1;
                    for (int featureIdx = 0; featureIdx < features.Count; featureIdx++)
                    {
                        GENFeature genFeature = features[featureIdx];
                        double size = genFeature.CalculateMeasure();
                        if (size < minSize)
                        {
                            minSize = size;
                            minFeature = genFeature;
                            minFeatureIdx = featureIdx;
                        }
                    }
                    if (minFeature != null)
                    {
                        newGENFile.AddFeature(minFeature);
                    }
                    if ((minFeature != null) && !srcFeatureIndices.Contains(minFeatureIdx))
                    {
                        newGENFile.AddFeature(minFeature);
                        srcFeatureIndices.Add(minFeatureIdx);
                    }
                    break;
                case SizeOperator.Max:
                    double maxSize = double.MinValue;
                    GENFeature maxFeature = null;
                    int maxFeatureIdx = -1;
                    for (int featureIdx = 0; featureIdx < features.Count; featureIdx++)
                    {
                        GENFeature genFeature = features[featureIdx];
                        double size = Math.Abs(genFeature.CalculateMeasure());
                        if (size > maxSize)
                        {
                            maxSize = size;
                            maxFeature = genFeature;
                            maxFeatureIdx = featureIdx;
                        }
                    }
                    if ((maxFeature != null) && !srcFeatureIndices.Contains(maxFeatureIdx))
                    {
                        newGENFile.AddFeature(maxFeature);
                        srcFeatureIndices.Add(maxFeatureIdx);
                    }
                    break;
                default:
                    for (int featureIdx = 0; featureIdx < features.Count; featureIdx++)
                    {
                        bool isSelected = false;
                        GENFeature genFeature = features[featureIdx];
                        double size = genFeature.CalculateMeasure();
                        switch (sizeOperator)
                        {
                            case SizeOperator.Equal:
                                isSelected = size.Equals(sizeValue);
                                break;
                            case SizeOperator.GreaterThan:
                                isSelected = size > sizeValue;
                                break;
                            case SizeOperator.GreaterThanOrEqual:
                                isSelected = size >= sizeValue;
                                break;
                            case SizeOperator.LesserThan:
                                isSelected = size < sizeValue;
                                break;
                            case SizeOperator.LesserThanOrEqual:
                                isSelected = size <= sizeValue;
                                break;
                            default:
                                throw new Exception("Undefined size operator: " + sizeOperator);
                        }
                        if (isSelected && !srcFeatureIndices.Contains(featureIdx))
                        {
                            newGENFile.AddFeature(genFeature);
                            srcFeatureIndices.Add(featureIdx);
                        }
                    }
                    break;
            }

            return newGENFile;
        }

        /// <summary>
        /// Select features from a list of features with specified size
        /// </summary>
        /// <param name="features"></param>
        /// <param name="sizeOperator"></param>
        /// <param name="sizeValue"></param>
        /// <returns>list of selected features</returns>
        protected List<GENFeature> SelectFeatures(List<GENFeature> features, SizeOperator sizeOperator, float sizeValue)
        {
            List<GENFeature> selectedFeatures = new List<GENFeature>();

            return selectedFeatures;
        }

        /// <summary>
        /// Modify columnvalues of selected features in specified GENFile object
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="selSourceFeatureIndices"></param>
        /// <param name="settings"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        private GENFile ModifyFeatures(GENFile genFile, List<int> selSourceFeatureIndices, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            log.AddInfo("Modifying column values for selected points ... ", logIndentLevel);

            DATFile datFile = genFile.HasDATFile() ? genFile.DATFile : null;
            if (datFile == null)
            {
                throw new ToolException("No DAT-file defined for GEN-file, modification of feature is not allowed: " + Path.GetFileName(genFile.Filename));
            }

            // First add new columns if requested and check for invalid column definitions
            for (int defIdx = 0; defIdx < settings.ColumnExpressionDefs.Count; defIdx++)
            {
                ColumnExpressionDef colExpDef = settings.ColumnExpressionDefs[defIdx];
                int colNr = datFile.FindColumnNumber(colExpDef.ColumnDefinition, true, false);
                if (colNr <= 0)
                {
                    if (int.TryParse(colExpDef.ColumnDefinition, out colNr))
                    {
                        throw new ToolException("Specified column not found for column/value-definition " + (defIdx + 1) + ": " + colExpDef.ToString());
                    }

                    // Column specified by a non-existing columnname, add it
                    datFile.AddColumn(colExpDef.ColumnDefinition, colExpDef.NoDataString);
                }
                else
                {
                    if ((colNr <= 0) || (colNr > genFile.Count))
                    {
                        throw new ToolException("Invalid column number for column/value-definition " + (defIdx + 1) + ": " + colExpDef.ToString());
                    }
                }
            }

            for (int selFeatureIdx = 0; selFeatureIdx < selSourceFeatureIndices.Count; selFeatureIdx++)
            {
                int featureIdx = selSourceFeatureIndices[selFeatureIdx];
                GENFeature genFeature = genFile.Features[featureIdx];
                for (int defIdx = 0; defIdx < settings.ColumnExpressionDefs.Count; defIdx++)
                {
                    ColumnExpressionDef colExpDef = settings.ColumnExpressionDefs[defIdx];
                    int colNr = datFile.FindColumnNumber(colExpDef.ColumnDefinition, true, false);
                    DATRow datRow = datFile.GetRow(genFeature.ID);
                    if (datRow != null)
                    {
                        datRow[colNr - 1] = EvaluateExpression(datRow[colNr - 1], colExpDef, inputGENFile, datRow);
                    }
                }
            }
            return genFile;
        }

        /// <summary>
        /// Creates metadata object based on specified source GEN-file and settings
        /// </summary>
        /// <param name="genFilename"></param>
        /// <param name="settings"></param>
        protected virtual Metadata CreateMetadata(string genFilename, SIFToolSettings settings)
        {
            Metadata genMetadata = new Metadata(genFilename);

            string metadataDescription = "GEN-selection";
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
            genMetadata.Description = metadataDescription;
            genMetadata.ProcessDescription = metadataProcessDescription;
            genMetadata.Source = genFilename;

            return genMetadata;
        }

        private string EvaluateExpression(string currentValueString, ColumnExpressionDef colExpDef, GENFile inputGENFile, List<string> columnValues)
        {
            if (colExpDef.ExpOperator == OperatorEnum.None)
            {
                // Try to parse string expression
                if (inputGENFile.HasDATFile())
                {
                    return inputGENFile.DATFile.EvaluateStringExpression(colExpDef.ExpressionString, columnValues);
                }
                else
                {
                    throw new Exception("Columns string expression cannot be evaluated for missing DAT-file: " + Path.GetFileName(inputGENFile.Filename));
                }
            }
            else
            {
                try
                {
                    double resultValue;
                    if ((currentValueString == null) || currentValueString.Equals(string.Empty))
                    {
                        currentValueString = "0";
                    }
                    double currentValue;
                    if (colExpDef.ExpDoubleValue.Equals(double.NaN) || !double.TryParse(currentValueString, NumberStyles.Float, EnglishCultureInfo, out currentValue))
                    {
                        if (colExpDef.ExpOperator == OperatorEnum.Add)
                        {
                            return currentValueString + colExpDef.ExpStringValue;
                        }
                        else if (colExpDef.ExpOperator == OperatorEnum.Substract)
                        {
                            return currentValueString.Replace(colExpDef.ExpStringValue, string.Empty);
                        }
                        else
                        {
                            throw new ToolException("Expression is not defined for operator " + colExpDef.ExpOperator + " and current value: " + currentValueString);
                        }
                    }
                    switch (colExpDef.ExpOperator)
                    {
                        case OperatorEnum.Multiply:
                            resultValue = currentValue * colExpDef.ExpDoubleValue;
                            break;
                        case OperatorEnum.Divide:
                            resultValue = currentValue / colExpDef.ExpDoubleValue;
                            break;
                        case OperatorEnum.Add:
                            resultValue = currentValue + colExpDef.ExpDoubleValue;
                            break;
                        case OperatorEnum.Substract:
                            resultValue = currentValue - colExpDef.ExpDoubleValue;
                            break;
                        default:
                            throw new Exception("Unexpected operator: " + colExpDef.ExpOperator);
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

    }
}
