// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Settings;

namespace Sweco.SIF.iMODValidator
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string RUNFilename { get; set; }
        public string RUNFilename2 { get; set; }

        public string OutputPath { get; set; }
        public string SettingsFilename { get; set; }
        public bool PreventExternalAppStart { get; set; }

        public bool IsValidated { get; set; }
        public bool IsCompared { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            RUNFilename = null;
            OutputPath = null;
            SettingsFilename = null;
            PreventExternalAppStart = false;

            IsValidated = false;
            IsCompared = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("runfile", "iMOD RUN/PRJ-file with references to files to check", "C:\\Test\\Input", 1);
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output", 1);
            AddToolOptionDescription("s", "specify XML-file to retrieve iMODValidator settings from", "/s:Test\\Input\\iMODValidator.xml", "Settings are retrieved from: {0}", new string[] { "s1" }, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("i", "prevent starting of iMOD and Excel (settings are overruled)", "/i", "Starting of iMOD/Excel is prevented", null, null, null, 1);
            AddToolOptionDescription("c", "Compare model defined by runfile parameter with model defined by second RUN/PRJ-file r2", "/c:Test\\Input\\Model2.RUN", "Model is compared with RUN/PRJ-file: {0}", new string[] { "r2" }, null, null, 1);
            AddToolOptionDescription("v", "validate model defined by runfile parameter (default action)", "/v", "Model defined by runfile is validated", null, null, null, 1);
            AddToolUsageFinalRemark("When run without arguments, the iMODValidator user interface version is started");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 0)
            {
                groupIndex = 0;
            }
            else if (parameters.Length == 2)
            {
                // Parse syntax 1:
                RUNFilename = parameters[0];
                OutputPath = parameters[1];
                groupIndex = 1;
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
                if (hasOptionParameters)
                {
                    SettingsFilename = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                PreventExternalAppStart = true;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsCompared = true;
                if (hasOptionParameters)
                {
                    RUNFilename = optionParametersString;
                    RUNFilename2 = optionParametersString;
                }
                else
                {
                    throw new ToolException("RUN/PRJ-filename expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                IsValidated = true;
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

            // Retrieve full paths and check existancex
            if (RUNFilename != null)
            {
                RUNFilename = ExpandPathArgument(RUNFilename);
                if (!File.Exists(RUNFilename))
                {
                    throw new ToolException("RUN/PRJ-file does not exist: " + RUNFilename);
                }
            }
            if (RUNFilename2 != null)
            {
                RUNFilename2 = ExpandPathArgument(RUNFilename2);
                if (!File.Exists(RUNFilename2))
                {
                    throw new ToolException("RUN/PRJ-file for option 'c' does not exist: " + RUNFilename2);
                }
            }

            if (OutputPath != null)
            {
                OutputPath = ExpandPathArgument(OutputPath);
            }

            // Check tool option values
            if (!IsCompared)
            {
                IsValidated = true;
            }
        }
    }
}
