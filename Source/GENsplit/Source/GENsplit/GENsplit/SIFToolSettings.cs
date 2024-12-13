// GENsplit is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENsplit.
// 
// GENsplit is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENsplit is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENsplit. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.GENsplit
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        // Note: because of rounding, squaring and square root operations the distance can have an error of almost 1.0
        public const double DistanceTolerance = 0.01;

        /// <summary>
        /// String to use for NoData-values in resulting GEN-file
        /// </summary>
        public const string NoDataValueString = "-9999";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public bool SkipOverwrite { get; set; }
        public float MaxSnapDistance { get; set; }
        public float MinSplitDistance { get; set; }
        public string DatSplitColumnString { get; set; }
        public string SplitValuePrefix { get; set; }
        public string IPFFilename { get; set; }
        public int AddedIPFColNr { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            SkipOverwrite = false;
            MaxSnapDistance = 100.0f;
            MinSplitDistance = 1.0f;
            DatSplitColumnString = null;
            SplitValuePrefix = null;
            IPFFilename = null;
            AddedIPFColNr = -1;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for GEN-file(s)", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.GEN)", "*.GEN");
            AddToolParameterDescription("outPath", "path for output GEN-files", "C:\\Test\\Output");
            AddToolOptionDescription("o", "Do not overwrite existing target GEN-files; existing files will be skipped", "/o",
                                          "Existing output files are overwritten");
            AddToolOptionDescription("c", "Split GEN-features by values in specified column c1 of DAT-file and create seperate GEN-files\n" +
                                          "Column can be specified by (one-based) number or column name, optionally use split prefix", "/c:5", 
                                          "Columns are split by values in column: {0}", new string[] { "c1" }, new string[] { "c2" });
            AddToolOptionDescription("i", "Split GEN-lines at points in specified IPF-file\n" +
                                          "Optionally specify (one-based) column index c in IPF-file to add to split GEN-file", "/i:split.IPF", 
                                          "IPF-file for splitting GEN-lines is: {0}", new string[] { "i1" }, new string[] { "i2" });
            AddToolOptionDescription("s", "Specify settings for snapping/splitting with option i:\n" +
                                          " - max snap distance for snapping (default: 100)\n" + 
                                          " - min split distance to existing GEN-points for splitting (default: 1); to avoid very short lines", "/s:100,0.5",
                                          "Max snap distance: {0}; Min split distance: {1}", new string[] { "s1" }, new string[] { "s2" }, new string[] { "1.0"});
        }

        /// <summary>
        /// Format specified option parameter value in logstring with a new (readable) string
        /// </summary>
        /// <param name="optionName">name of option for which a formatted parameter value is required</param>
        /// <param name="parameter">name of option parameter for which a formatted parameter value is required</param>
        /// <param name="parameterValue">the parameter value that has to be formatted</param>
        /// <param name="parameterValues">for reference, all specified parameter values for this options</param>
        /// <returns>a readable form of specified parameter value</returns>
        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            return parameterValue;
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 3)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                OutputPath = parameters[2];
                groupIndex = 0;
            }
            else
            {
                HandleInvalidParameterCount(parameters, out groupIndex);
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
            if (optionName.ToLower().Equals("o"))
            {
                SkipOverwrite = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    if (optionParameters.Length >= 1)
                    {
                        try
                        {
                            MaxSnapDistance = float.Parse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo);
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse max snap distance for option '" + optionName + "':" + optionParametersString);
                        }
                    }
                    if (optionParameters.Length >= 2)
                    {
                        try
                        {
                            MinSplitDistance = float.Parse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo);
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse min split distance for option '" + optionName + "':" + optionParametersString);
                        }
                    }
                    if ((optionParameters.Length < 1) || (optionParameters.Length > 2))
                    {
                        throw new ToolException("Option s requires 1 or 2 arguments: " + optionParametersString);
                    }

                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    DatSplitColumnString = optionParameters[0];
                    if (optionParameters.Length > 1)
                    {
                        SplitValuePrefix = optionParameters[1];
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Please specify parameters after 'i:':" + optionParametersString);
                    }
                    IPFFilename = optionParameters[0];
                    if (optionParameters.Length > 1)
                    {
                        int colNr;
                        if (!int.TryParse(optionParameters[1], out colNr))
                        {
                            throw new ToolException("Could not parse value for option 'i':" + optionParameters[1]);
                        }
                        AddedIPFColNr = colNr;
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
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
                InputFilter = "*.GEN";
            }
            else if (!InputFilter.ToLower().EndsWith(".gen"))
            {
                throw new ToolException("Input filter should have GEN extension: " + InputFilter);
            }

            if ((IPFFilename != null) && !File.Exists(IPFFilename))
            {
                throw new ToolException("IPF-file does not exist: " + IPFFilename);
            }

            if (AddedIPFColNr < -1)
            {
                throw new ToolException("Value 1 or larger expected for option i");
            }

        }
    }
}
