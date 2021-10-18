// iMODstats is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODstats.
// 
// iMODstats is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODstats is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODstats. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.iMODstats
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputFile { get; set; }

        public bool IsOverwrite { get; set; }
        public Extent Extent { get; set; }

        /// <summary>
        /// Number of percentile classes, e.g. 4 will result in 4 classes: 25, 50, 75 and 100.
        /// </summary>
        public int PercentileClassCount { get; set; }

        /// <summary>
        /// Number of decimals in resulting statistics 
        /// </summary>
        public int DecimalCount { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputFile = null;
            IsOverwrite = false;
            Extent = null;
            DecimalCount = 2;
            PercentileClassCount = 10;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IDF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IDF-files (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outFile", "Path and filename of Excel-file to write results to", "C:\\Test\\Output\\Statistics.xlsx");
            AddToolOptionDescription("d", "Define decimal count (default:3) for results or just use /d to show all decimals", "/d:1", "Number of decimals: {0}", null, new string[] { "d1" }, new string[] { "2" });
            AddToolOptionDescription("e", "extent (xll,yll,xur,yur) for processing", "/e:181500,407500,200500,426500", "Extent used for statistics: {0},{1},{2},{3}", new string[] { "xll", "yll", "xur", "yur" } );
            AddToolOptionDescription("o", "overwrite existing outputfile", "/o", "Existing outputfile is overwritten");
            AddToolOptionDescription("t", "Define number of percentile classes n (e.g. 4 gives classes of 25%)", "/t", "Number of percentile classes: {0}", new string[] { "n" });
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
                OutputFile = parameters[2];
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
            if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("d"))
            {
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int intValue))
                    {
                        throw new ToolException("Invalid parameter for option 'd', integer expected: " + optionParametersString);
                    }
                    DecimalCount = intValue;
                }
                else
                {
                    DecimalCount = int.MaxValue;
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    Extent = Extent.ParseExtent(optionParametersString);
                    if (Extent == null)
                    {
                        throw new ToolException("Could not parse extent:" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing extent coordinates for option '" + optionName + "'");
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
                InputFilter = "*.IDF";
            }

            if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
            }
        }
    }
}
