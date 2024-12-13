// URLdownload is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of URLdownload.
// 
// URLdownload is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// URLdownload is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with URLdownload. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.URLdownload
{
    public enum SecurityProtocol
    {
        Undefined,
        SSL,
        TLS
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string URL { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }
        public string Accountname { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public SecurityProtocol SecurityProtocol { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            URL = null;
            OutputPath = null;
            OutputFilename = null;
            Accountname = null;
            Password = null;
            Domain = null;
            SecurityProtocol = SecurityProtocol.Undefined;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("url", "URL to download file from.", "ftp://www.xxx.nl/download.zip");
            AddToolParameterDescription("outPath", "Path and filename to write downloaded file", "C:\\Test\\Output\\download.zip");
            AddToolOptionDescription("c", "Add credentials with accountname (an) and password (pw). Optionally a domain (d) can be specified.\n" +
                                          "Note: both are NOT reported/stored by tool, but are send as text via URL", "/c:p.ersoon@hier.nl,X#$!21C&", "Credentials have been specified", new string[] { "an", "pw" }, new string[] { "d" }, new string[] { "N/A" } );
            AddToolOptionDescription("sp", "Define security protocol p with one of the following strings: 'ssl' for SSL 3.0, 'tls' for TLS1.2", "/sp:ssl", "Security protocol has been specified: {0}", new string[] { "p" });
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 2)
            {
                // Parse syntax 1:
                URL = parameters[0];
                this.SplitPathArgument(parameters[1], out string outputPath, out string outputFilename);
                OutputPath = outputPath;
                OutputFilename = outputFilename;
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
            if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    try
                    {
                        // Parse substrings for this option
                        if (optionParameters.Length >= 1)
                        {
                            Accountname = optionParameters[0];
                        }
                        if (optionParameters.Length >= 2)
                        {
                            Password = optionParameters[1];
                        }
                        if (optionParameters.Length >= 3)
                        {
                            throw new ToolException("Too much parameters defined for option '" + optionName + "':" + optionParametersString);
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
            else if (optionName.ToLower().Equals("sp"))
            {
                if (hasOptionParameters)
                {
                    switch (optionParametersString.ToLower())
                    {
                        case "ssl":
                            SecurityProtocol = SecurityProtocol.SSL;
                            break;
                        case "tls":
                            SecurityProtocol = SecurityProtocol.TLS;
                            break;
                        default:
                            throw new ToolException("Undefined security protocol:" + optionParametersString);
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

            //if (Directory.Exists(Filename))
            //{
            //    throw new ToolException("Output filename cannot be equal to an existing directoryname: " + Filename);
            //}
        }
    }
}
