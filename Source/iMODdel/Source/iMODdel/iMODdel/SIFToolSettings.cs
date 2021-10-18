// iMODdel is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODdel.
// 
// iMODdel is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODdel is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODdel. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.iMODdel
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public bool IsRecursive { get; set; }
        public bool IsZeroDeleted { get; set; }
        public float ZeroValue { get; set; }
        public float ZeroMargin { get; set; }
        public bool IsRecycleBinUsed { get; set; }
        public bool IsListMode { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            IsRecursive = false;
            IsRecycleBinUsed = true;
            IsZeroDeleted = false;
            ZeroValue = 0;
            ZeroMargin = 0;
            IsListMode = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for iMOD-files", "C:\\Test\\SomeFiles");
            AddToolParameterDescription("filter", "Filter to select iMOD-files", "*_L*.XXX");
            AddToolOptionDescription("r", "process input path recursively", "/a:45,3", "Subdirectories are processed recursively");
            AddToolOptionDescription("b", "do not delete to recycle bin, but delete permanently (which is faster)", null, "Files are NOT sent to recycle bin, but deleted permanently");
            AddToolOptionDescription("0", "also delete IDF-files with only 0-values (or NoData-values)\n" +
                                          "if margin 'm' is specified values less than 'm' around 0 are handled as 0\n" +
                                          "if value 'v' is specified, v is used instead of 0", "/0:0.01,1", "Files with NoData- or zero-values are deleted; margin (m): {0}, value (v): {1}", null, new string[] { "m", "v" }, new string[] { "0", "0" });
            AddToolOptionDescription("l", "do not delete, but just log/show list of empty iMOD-files", null, "Files are not deleted, just logged");
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
                IsRecycleBinUsed = false;
            }
            else if (optionName.ToLower().Equals("l"))
            {
                IsListMode = true;
            }
            else if (optionName.ToLower().Equals("0"))
            {
                IsZeroDeleted = true;
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    try
                    {
                        // Parse substrings for this option
                        if (optionParameters.Length >= 1)
                        {
                            ZeroMargin = float.Parse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo);
                        }
                        if (optionParameters.Length >= 2)
                        {
                            ZeroValue = float.Parse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo);
                        }
                        if (optionParameters.Length >= 3)
                        {
                            throw new ToolException("Maximum of two paramters is expected for option '0': " + optionParametersString);
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                    }
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
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex"></param>
        /// <returns>the index for the argument group for these parameters, 0 if only a single group is defined</returns>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 2)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
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
                    throw new ToolException("inPath does not exist: " + InputPath);
                }
            }

            // Check tool parameters
            if ((InputFilter == null) || (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.*";
            }

            // Check tool option values
            if (ZeroMargin < 0)
            {
                throw new ToolException("Value 0 or larger expected for parameter m of option '0': " + ZeroMargin);
            }
        }
    }
}
