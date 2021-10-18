// IDFresample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFresample.
// 
// IDFresample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFresample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFresample. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IDFresample
{
    public enum ConflictMethod
    {
        MinimumValue,
        MaximumValue,
        ArithmeticAverage,
        HarmonicAverage
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public string ZoneFilename { get; set; }
        public ConflictMethod ConflictMethod { get; set; }
        public bool isSplitByZoneValues { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            ConflictMethod = ConflictMethod.ArithmeticAverage;
            isSplitByZoneValues = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input value file(s) (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outPath", "Path to write results or  filename of resultfile when filter refers to a single value file", "C:\\Test\\Output");
            AddToolOptionDescription("z", "define zonefile to define resampled zone. Each zone value is resampled individually", "/z:zonefile.IDF", "Zonefile is defined for resampling zone: {0}", new string[] { "z1" });
            AddToolOptionDescription("c", "define method to handle multiple neighbor cells:\n1: arithmetic average (default); 2: harmonic average; 3: minimum value; 4: maximum value", "/c:3", "Method number used to handle neighbors with equal distance: {0}", new string[] { "c1" });
            AddToolOptionDescription("s", "split result IDF-file(s) for zones with unique zone values to handle multiple neighbor cells.\nThe zone number z is added as a postfix '_zone<z>'", "/s", "Result IDF-file is split by unique zone values");

            AddToolHelpArgString("Resulting IDF-file will get extent of zone IDF-file and cellsize and NoData-value of value IDF-file.");
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
                if (OutputPath.ToLower().EndsWith(".idf"))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
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
            if (optionName.ToLower().Equals("s"))
            {
                isSplitByZoneValues = true;
            }
            else if (optionName.ToLower().Equals("z"))
            {
                if (hasOptionParameters)
                {
                    ZoneFilename = optionParametersString;
                }
                else
                {
                    throw new ToolException("Please specify an zone IDF-file for option 'z'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    int methodNumber;
                    if (int.TryParse(optionParametersString, out methodNumber))
                    {
                        switch (methodNumber)
                        {
                            case 1:
                                ConflictMethod = ConflictMethod.ArithmeticAverage;
                                break;
                            case 2:
                                ConflictMethod = ConflictMethod.HarmonicAverage;
                                break;
                            case 3:
                                ConflictMethod = ConflictMethod.MinimumValue;
                                break;
                            case 4:
                                ConflictMethod = ConflictMethod.MaximumValue;
                                break;
                            default:
                                throw new ToolException("Please specify a valid conflict method number (1-4) for option 'c':" + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("Please specify a valid conflict method number (1-4) for option 'c':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Please specify a conflict method number (1-4) for option 'c'");
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
            if ((InputFilter != null) && InputFilter.Equals(string.Empty))
            {
                // Specify default
                InputFilter = "*.IDF";
            }
            if ((InputFilter != null) && !Path.GetExtension(InputFilter).ToLower().Equals(".idf"))
            {
                throw new ToolException("Input filter for value file(s) should have IDF-extension: " + InputFilter);
            }

            // Check tool option values
            if ((ZoneFilename != null) && !File.Exists(ZoneFilename))
            {
                throw new ToolException("Zone IDF-file does not exist: " + ZoneFilename);
            }
            if ((ZoneFilename != null) && !Path.GetExtension(ZoneFilename).ToLower().Equals(".idf"))
            {
                throw new ToolException("Zone file should be an IDF-file: " + ZoneFilename);
            }
        }
    }
}
