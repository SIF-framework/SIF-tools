// GENbuffer is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENbuffer.
// 
// GENbuffer is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENbuffer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENbuffer. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.GENbuffer
{
    public enum BufferJoinType
    {
        Round,
        Miter,
        Square
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

        public bool IsRecursive { get; set; }
        public double BufferSize { get; set; }
        public BufferJoinType BufferJoinType { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            IsRecursive = false;

            BufferSize = float.NaN;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.XXX)", "*.XXX");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is processed recursively");
            AddToolOptionDescription("b", "Specify buffer size b1, negative values will shrink", "/b:100", "Buffer size: {0}", new string[] { "b1" });
            AddToolOptionDescription("e", "Specify method e1 for rounding edges:\n" +
                                          "1) rounded joins between edges (default)\n" +
                                          "2) mitered joins (sharp angle between edges, squared for narrow spikes)\n" +
                                          "3) squared (flattened) joins", "/e:2", "Method for rounding edges: {0}", new string[] { "e1" });
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
            switch (optionName)
            {
                case "e":
                    switch (parameter)
                    {
                        case "e1":
                            switch (parameterValue)
                            {
                                case "1": return "round";
                                case "2": return "miter";
                                case "3": return "square";
                                default: return parameterValue;
                            }
                        default: return parameterValue;
                    }
 
                default:
                    // As a default, do not use special formatting and simply return parameter value
                    return parameterValue;
            }
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

                // Split output path in path and filename if both are specified
                if (Path.HasExtension(OutputPath))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null for now
                    OutputFilename = null;
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
            if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("b"))
            {
                if (hasOptionParameters)
                {
                    if (!double.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out double bufferSize))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    BufferSize = bufferSize;
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
                    if (!int.TryParse(optionParametersString, out int bufferJoinType))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    switch (bufferJoinType)
                    {
                        case 1:
                            BufferJoinType = BufferJoinType.Round;
                            break;
                        case 2:
                            BufferJoinType = BufferJoinType.Miter;
                            break;
                        case 3:
                            BufferJoinType = BufferJoinType.Square;
                            break;
                        default:
                            throw new ToolException("Invalid method for rounding edges, integer value 1-3 expected: " + optionParametersString);
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

            // Check tool option values
            if (BufferSize.Equals(float.NaN))
            {
                throw new ToolException("A buffersize needs to be specified via option 'b'");
            }
        }
    }
}
