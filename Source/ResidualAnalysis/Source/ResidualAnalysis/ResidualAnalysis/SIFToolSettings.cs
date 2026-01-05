// ResidualAnalysis is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ResidualAnalysis.
// 
// ResidualAnalysis is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ResidualAnalysis is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ResidualAnalysis. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.ResidualAnalysis
{
    /// <summary>
    /// Enums that define columns in statistic summary sheet
    /// </summary>
    public enum ResidualStatistic
    {
        ME,
        MAE,
        SDE,
        RMSE,
        SSE
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultDiffResIPFPath = "DiffResIPF";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string IdColString { get; set; }
        public string LayerColString { get; set; }
        public string ObservedColString { get; set; }
        public string SimulatedColString { get; set; }
        public string ResidualColString { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public bool IsBackupSkipped { get; set; }
        public List<string> ColStrings { get; set; }
        public List<string> ColNames { get; set; }
        public string ResSetName { get; set; }
        public string GroupName { get; set; }
        public Extent Extent { get; set; }
        public bool IsDiffIPFCreated { get; set; }
        public string DiffIPFResultPath { get; set; }
        public bool IsOverwrite { get; set; }
        public List<float> SkippedValues { get; set; }
        public bool UseWeights { get; set; }
        public string WeightColString { get; set; }

        public string MaskIDFFilename { get; set; }
        public float[] MaskIDFValues { get; set; }

        public int PercentileCount { get; set; }

        public bool SkipIDMismatches { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            IdColString = null;
            LayerColString = null;
            ObservedColString = null;
            SimulatedColString = null;
            ResidualColString = null;
            OutputPath = null;
            OutputFilename = null;

            IsBackupSkipped = false;
            ColStrings = new List<string>();
            ColNames = new List<string>();
            ResSetName = null;
            GroupName = null;
            Extent = null;
            IsDiffIPFCreated = false;
            DiffIPFResultPath = DefaultDiffResIPFPath;
            IsOverwrite = false;
            SkippedValues = new List<float>();
            UseWeights = false;
            WeightColString = null;

            MaskIDFFilename = null;
            MaskIDFValues = null;

            PercentileCount = 0;

            SkipIDMismatches = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IPF-files of some residual set (e.g. a calibration set)", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files of the residual set (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("idColNr", "name or number (one-based) of column with ID in IPF-files", "3");
            AddToolParameterDescription("layColNr", "name or number (one-based) of column with layer in IPF-files\n" +
                                        "use 0 to take the last filename digits (after '_L')", "4");
            AddToolParameterDescription("obsColNr", "name or number (one-based) of column with observed values in IPF-files. Use 0 to skip.", "5");
            AddToolParameterDescription("simColNr", "name or number (one-based) of column with computed values in IPF-files. Use 0 to skip.", "6");
            AddToolParameterDescription("resColNr", "name or number (one-based) of column with residuals in IPF-files. Use 0 to calculate.\n" +
                                        "use negative obs/sim/resColNr to get relative to last column \n"+
                                        "use zero obs/simColNr to skip observation and/or simulation column \n" +
                                        "note: x- and y-coordinates should be in IPF-columns 1 an 2", "7");
            AddToolParameterDescription("outPath", "Path or Excel-filename (XLSX) to write results", "C:\\Test\\Output");

            AddToolOptionDescription("b", "Skip backup when (re)adding summary to existing Excelfile", "/b", "Backup is skipped when adding to existing Excelfile");
            AddToolOptionDescription("c", "Comma seperated list of (1-based) column numbers or names to add", "/c:5", "The following columns are added: {...} ", new string[] { "..." });
            AddToolOptionDescription("n", "Comma seperated list of names of the added columns (in option c)", "/n:Weight", "The following column names are added: {...}", new string[] { "..." });
            AddToolOptionDescription("d", "Define name n for of current residual set and group name g that results should be added to in summary sheet.\n"+
                                          "If not specified, these are derived from the filenames", "/d:ORG_BAS,Test1", "Residual set is named '{0}' and grouped under '{1}'", new string[] {"n", "g" });
            AddToolOptionDescription("e", "Extent (within specified IPF-files) for residual analysis", "/e:184000,352500,200500,371000", "Extent is defined as: {0},{1},{2},{3}",
                                     new string[] { "e1", "e2", "e3", "e4" });
            AddToolOptionDescription("i", "Create IPF-files with residual-differences per residual set under (relative) path p (default: " + DefaultDiffResIPFPath + ")\n" +
                                          "Note: IPF-files are written in subdirectory with name of group as defined by option 'd'\n" +
                                          "The following columns in the resulting IPF-file can be used for visualization:\n" +
                                          "- ID:         the ID of the point for which the residual difference is calculated\n" +
                                          "- RES(1):     residual from first/reference residual set\n" +
                                          "- RES(2):     residual from second/modified residual set\n" +
                                          "- dABSRES1-2: ABS(res1) - ABS(res2), with resi the residual of residual set i\n" +
                                          "                a positive value indicates an improvement by residual set 2\n" +
                                          "- CLASS:      number of class in legend of absolute residuals (which currently cannot be changed)\n" +
                                          "                5: > 1.0; 4: > 0.5; 3: > 0.2; 2: > 0.1; 1: <= 0.1\n" +
                                          "- dSGN        difference in sign: -1 indicates the sign has changed\n" +
                                          "note: in case of mismatches, only the matched points of residual set 1 are reported",
                                     "/i:DiffRes", "IPF-file(s) created with residual-differences in subdir: {0}", null, new string[] { "p" }, new string[] { DefaultDiffResIPFPath });
            AddToolOptionDescription("k", "Use mask IDF-file (k1) and (comma-seperated) mask values (kx) \n" +
                                     "IPF-points in cells with IDF-value equal to any ki-value (default NoData) are skipped in results",
                                     "/k", "Mask IDF-file is used with mask values: {...}", new string[] { "k1" }, new string[] { "..." });
            AddToolOptionDescription("o", "Overwrite existing outputfiles, if not specified, statistics are added and compared to existing output",
                                     "/o", "Outputfiles are overwritten");
            AddToolOptionDescription("s", "Comma seperated list of values (observed, simulated or residual value) to skip (e.g. NoData-values) \n" +
                                     "use \"\" to skip missing (or empty string) values", "/s:NoData", "The following values are skipped: {0}", new string[] { "..." });
            AddToolOptionDescription("w", "Use weights as defined in column number", "/w:3", "Weights are defined by column number {0}", new string[] { "w1" });
            AddToolOptionDescription("p", "Add number of percentile classes to be used in result statistics \n" +
                                     "i.e. 4 classes give percentile 25, 50, 75 and 100.", "/p:4", "Number of percentile classes: {0}", new string[] { "p1" });
            AddToolOptionDescription("sim", "Skip ID mismatches (for option i): skip points that do not match an ID in previous results.\n" +
                                     "Note: the number of points (N) in the summary sheet may not be correct for skipped points, but the\n" +
                                     "other statistics will be correct and values are copied from the individual sheets.", "/sim", "Points without matching ID are skipped");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 8)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                IdColString = parameters[2];
                LayerColString = parameters[3];
                ObservedColString = parameters[4];
                SimulatedColString = parameters[5];
                ResidualColString = parameters[6];
                OutputPath = parameters[7];
                if (Path.GetExtension(OutputPath).ToLower().Equals(".xlsx"))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null for now
                }
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        /// <summary>
        /// Parse and process tool option
        /// </summary>
        /// <param name="optionName">the character(s) that identify this option</param>
        /// <param name="hasOptionParameters">true if this option has parameters</param>
        /// <param name="optionParametersString">a string with optional comma seperated parameters for this option</param>
        /// <returns>true if recognized and processed</returns>
        protected override bool ParseOption(string optionName, bool hasOptionParameters, string optionParametersString = null)
        {

            if (optionName.ToLower().Equals("b"))
            {
                IsBackupSkipped = true;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    ColStrings.AddRange(optionParameters);
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("n"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    ColNames.AddRange(optionParameters);
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("d"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    if (optionParameters.Length == 2)
                    {
                        ResSetName = optionParameters[0];
                        GroupName = optionParameters[1];
                    }
                    else
                    {
                        throw new ToolException("Invalid number of arguments for option 'd': " + optionParametersString);
                    }          
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    // Parse substrings for this option
                    if (optionParameters.Length == 4)
                    {
                        float[] extentCoordinates = new float[4];
                        for (int i = 0; i < 4; i++)
                        {
                            string coordinateString = optionParameters[i];
                            float coordinate;
                            if (!float.TryParse(coordinateString, NumberStyles.Float, EnglishCultureInfo, out coordinate))
                            {
                                throw new ToolException("Invalid extent coordinate: " + coordinateString);
                            }
                            extentCoordinates[i] = coordinate;
                        }
                        Extent = new Extent(extentCoordinates[0], extentCoordinates[1], extentCoordinates[2], extentCoordinates[3]);
                    }
                    else
                    {
                        throw new ToolException("Invalid extent: 4 coordinates expected: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                IsDiffIPFCreated = true;
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    // Parse substrings for this option
                    if (optionParameters.Length == 1)
                    {
                        DiffIPFResultPath = optionParameters[0];
                    }
                    else
                    {
                        throw new ToolException("Only one parameter is allowed for option i, you provided: " + optionParameters.Length);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("k"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    // Parse substrings for this option
                    MaskIDFFilename = optionParameters[0];
                    if (optionParameters.Length > 1)
                    {
                        MaskIDFValues = new float[optionParameters.Length - 1];
                        for (int idx = 1; idx < optionParameters.Length; idx++)
                        {
                            if (float.TryParse(optionParameters[idx], out float maskValue))
                            {
                                MaskIDFValues[idx - 1] = maskValue;
                            }
                            else
                            {
                                throw new ToolException("Could not parse maskValue for option k: " + optionParameters[idx]);
                            }

                        }
                    }

                    if (!File.Exists(MaskIDFFilename))
                    {
                        throw new ToolException("Pointerfile not found: " + MaskIDFFilename);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {

                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    try
                    {
                        for (int idx2 = 0; idx2 < optionParameters.Length; idx2++)
                        {
                            if (!optionParameters[idx2].Equals(string.Empty))
                            {
                                float skippedValue = float.Parse(optionParameters[idx2], NumberStyles.Float, EnglishCultureInfo);
                                SkippedValues.Add(skippedValue);
                            }
                            else
                            {
                                if (!SkippedValues.Contains(float.NaN))
                                {
                                    SkippedValues.Add(float.NaN);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("w"))
            {

                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    if (optionParameters.Length == 1)
                    {
                        WeightColString = optionParameters[0];
                        try
                        {
                            int weightColIdx = int.Parse(optionParameters[0]);
                            if (weightColIdx < 0)
                            {
                                throw new ToolException("The weight colidx should be zero (for no weight) or above: " + weightColIdx);
                            }
                            UseWeights = true;
                        }
                        catch (Exception)
                        {
                            // ignore string can be a column name
                            // throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("Only one parameter is allowed for option w, you provided: " + optionParameters.Length);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("p"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    // Parse substrings for this option
                    if (optionParameters.Length == 1)
                    {
                        if (int.TryParse(optionParameters[0], out int percentileCount))
                        {
                            PercentileCount = percentileCount;
                        }
                        else
                        {
                            throw new ToolException("Could not parse percentileCount for option " + optionName + "':" + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("Only one parameter is allowed for option p, you provided: " + optionParameters.Length);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("sim"))
            {
                SkipIDMismatches = true;
            }
            else
            {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check the number of parsed arguments against the number of expected arguments. Override to check actual values.
        /// </summary>
        public override void CheckSettings()
        {
            // Perform syntax checks 
            base.CheckSettings();

            // Retrieve full paths and check existance
            if (InputPath != null)
            {
                InputPath = ExpandPathArgument(InputPath);
                if (!Directory.Exists(InputPath))
                {
                    throw new ToolException("Input path does not exist: " + InputPath);
                }
            }

            // Check tool parameters
            if ((InputFilter != null) && (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.IPF";
            }

            if (SkipIDMismatches && (IdColString == null))
            {
                throw new ToolException("For sim-option it is required that an ID-column is defined via option i");
            }

            if (ObservedColString.Equals("0") && SimulatedColString.Equals("0") && ResidualColString.Equals("0"))
            {
                throw new ToolException("Either observed/simulated column references or residual column reference can be zero, but not all.");
            }
        }
    }
}
