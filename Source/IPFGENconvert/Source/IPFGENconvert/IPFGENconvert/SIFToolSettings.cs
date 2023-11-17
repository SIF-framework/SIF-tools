// IPFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFGENconvert.
// 
// IPFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.GIS;

namespace Sweco.SIF.IPFGENconvert
{
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
        public int Method { get; set; }
        public double MethodParameter { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            OutputFilename = null;

            IsRecursive = false;
            Method = 0;
            MethodParameter = double.NaN;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IPF- or GEN-files (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("outPath", "Path to write results; or full filename if input is a single file", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Recursively process subdirectories in input path", null, "Input path is processed recursively");
            AddToolOptionDescription("m", "Method for conversion of IPF- or GEN-file\n" + 
                                          "when converting IPF-file to GEN-file: \n" +
                                          "  use m1=1 for squares and specify edge length m2\n" +
                                          "  use m1=2 for convex hull around all points, m2 is ignored\n" +
                                          "when converting GEN-file to an IPF-file: \n" +
                                          "  use m1=1 for centerpoint, m2 is ignored\n",
                                          "/m:1,100", "Conversion method m1: {0}, m2: {1}", new string[] { "m1" }, new string[] { "m2" }, new string[] { "NA" });
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
                if (Path.GetExtension(OutputPath).ToLower().Equals(".gen") || Path.GetExtension(OutputPath).ToLower().Equals(".ipf"))
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
            if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("m"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if(optionParameters.Length >= 1)
                    {
                        if (int.TryParse(optionParameters[0], out int method))
                        {
                            Method = method;
                        }
                        else
                        {
                            throw new ToolException("Invalid method specified: " + optionParameters[0]);
                        }
                    }

                    if (optionParameters.Length >= 2)
                    {
                        if (double.TryParse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo, out double methodValue))
                        {
                            MethodParameter = methodValue;
                        }
                        else
                        {
                            throw new ToolException("Invalid method value specified: " + optionParameters[1]);
                        }
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
                throw new ToolException ("Inputfilter is missing");
            }
        }
    }
}
