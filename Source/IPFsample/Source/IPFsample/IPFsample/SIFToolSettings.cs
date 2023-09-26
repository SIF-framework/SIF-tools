// IPFsample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFsample.
// 
// IPFsample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFsample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFsample. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IPFsample
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string ValueGrid { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public int DecimalCount { get; set; }
        public bool IsSkippingOutsideIDFExtent { get; set; }
        public bool IsInterpolated { get; set; }
        public bool IsSkippingNoDataCells { get; set; }
        public bool IsOverwrite { get; set; }
        public string ObservationColString { get; set; }
        public string StatPrefix { get; set; }
        public string CSVStatsFilename { get; set; }
        public bool IsNaNPointExcluded { get; set; }

        public string ValueColumnname { get; set; }
        public bool IsCSVFileWritten { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            ValueGrid = null;
            OutputPath = null;
            OutputFilename = null;

            DecimalCount = 2;
            IsSkippingOutsideIDFExtent = false;
            IsInterpolated = false;
            IsSkippingNoDataCells = false;          
            IsOverwrite = false;
            ObservationColString = null;
            CSVStatsFilename = null;
            StatPrefix = null;
            IsNaNPointExcluded = false;
            
            ValueColumnname = null;
            IsCSVFileWritten = false;
    }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input ipf-file (e.g. *.ipf)", "*.ipf");
            AddToolParameterDescription("valueGrid", "Filename of valuegrid (ASC/IDF)", "C:\\Test\\Input\\valueGrid.idf");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");

            AddToolOptionDescription("c", "Specify columnname in result IPF-file with sampled valuegrid-values (default is filename of valuegrid)", "/c", "Columnname for sampled valuegrid values: {0}", null, new string[] { "c1" });
            AddToolOptionDescription("d", "Round resultvalues to n decimals (default: 3)", "/d:3", "Number of decimals for result values: {0}", new string[] { }, new string[] { "d1" });
            AddToolOptionDescription("e", "Skip points outside IDF-extent ", "/e", "Points outside IDF-extent are removed from results");
            AddToolOptionDescription("i", "Interpolate gridvalues to IPF-locations", "/i", "Gridvalues are interpolated");
            AddToolOptionDescription("n", "Skip points in NoData-cells", "/n", "Points in NoData-cells or outside IDF-extent are removed from results");
            AddToolOptionDescription("o", "Overwrite existing outputfile; if not specified, existing files will be skipped", "/o", "Existing output files are overwritten");
            AddToolOptionDescription("s", "Calculate residual statistics for value in column s1 in IPF-file. Use (one-based) number or column name.\n" +
                                     "The IDF-value, residual and absolute residual are added to the output IPF-file \n" +
                                     "Other statistics are written in CSV-filename s2 (if specified) or in CSV-file in outputpath." +
                                     "Optionally add substring s3 to use as prefix before residual column names.", "/s:6",
                                     "Residual statistics will be calculated relative to the values in column: {0}; CSV-filename: {1}", new string[] { "s1" }, new string[] { "s2", "s3" }, new string[] { "<IPF-file>_stats.csv", "\"\"" });
            AddToolOptionDescription("x", "Exclude points that have a NoData-value in the specified column for option s", "/x", "NoData values are excluded for option s");
            AddToolOptionDescription("csv", "Write output file in CSV-format instead of IPF-format", "/csv", "Output is written as csv-file");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 4)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                ValueGrid = parameters[2];
                OutputPath = parameters[3];
                if (Path.GetExtension(OutputPath).ToLower().Equals(".ipf"))
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
            if (optionName.ToLower().Equals("d"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    if (optionParameters.Length == 1)
                    {
                        if (int.TryParse(optionParameters[0], out int decimalCount))
                        {
                            DecimalCount = decimalCount;
                        }
                        else
                        {
                            throw new ToolException("Invalid parameter for option d: " + optionParameters[0] + ". Integer expected.");
                        }
                    }
                    else
                    {
                        throw new ToolException("More than one parameter is specified for option d: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                IsSkippingOutsideIDFExtent = true;
            }
            else if (optionName.ToLower().Equals("i"))
            {
                IsInterpolated = true;
            }
            else if (optionName.ToLower().Equals("n"))
            {
                IsSkippingNoDataCells = true;
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
                    ObservationColString = optionParameters[0];

                    if (optionParameters.Length > 1)
                    {
                        CSVStatsFilename = optionParameters[1];
                        CSVStatsFilename = Path.ChangeExtension(CSVStatsFilename, "csv");
                    }
                    if (optionParameters.Length > 2)
                    {
                        StatPrefix = optionParameters[2];
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("x"))
            {
                IsNaNPointExcluded = true;
            }

            //plus
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    if (optionParameters.Length == 1)
                    {
                        ValueColumnname = optionParameters[0];
                    }
                    else
                    {
                        throw new ToolException("Only one parameter can be specified for option c: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing columnname for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("csv"))
            {
                IsCSVFileWritten = true;
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

        }
    }
}
