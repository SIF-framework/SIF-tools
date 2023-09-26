// NumberRounder is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of NumberRounder.
// 
// NumberRounder is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// NumberRounder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with NumberRounder. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.NumberRounder
{
    public enum ScientificMode
    {
        RemoveRound,
        KeepRound,
        Ignore
    }
    
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public int DecimalCount { get; set; }
        public string DecimalSeperator { get; set; }
        public string ListSeperator { get; set; }

        public bool IsDirectoryInput { get; set; }
        public bool IsBackedUp { get; set; }
        public string BackupPath { get; set; }
        public ScientificMode ScientificMode { get; set; }
        public string ThousandsSeperator { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            DecimalCount = -1;
            DecimalSeperator = null;
            ListSeperator = null;

            IsDirectoryInput = false;
            IsBackedUp = false;
            BackupPath = "backup";
            ScientificMode = ScientificMode.RemoveRound;
            ThousandsSeperator = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input text files, wildcard are allowed", "*.txt");
            AddToolParameterDescription("decimalcount", "Number of decimals to round to", "*.XXX");
            AddToolParameterDescription("decimalseperator", "Decimal seperator", ",");
            AddToolParameterDescription("listseperator", "List seperator between values; surround with quotes if seperator is a space", ";");

            AddToolOptionDescription("b", "Backup of input file(s) should be made to specified path", "/b:C:\\Test\\Backuppath", "Original files are backed up to: {0}", new string[] { "b1" });
            AddToolOptionDescription("e", "Define scientific notation mode: 0) Remove notation and round (default without option 'e');\n" + 
                                          "1) Keep notation and round (default for option 'e'; 2) Ignore (and keep notation)", 
                                          "/e:2", "Scientific notation mode: {0}", null, new string[] { "m" }, new string[] { "1" });
            AddToolOptionDescription("t", "Specify thousands seperator t1", "/t:.", "Thousands are seperated with: {0}", new string[] { "t1" });
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
            // As a default, do not use special formatting and simply return parameter value
            return parameterValue;
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 5)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                if (!int.TryParse(parameters[2], out int decimalCount))
                {
                    throw new ToolException("Invalid decimalCount argument: " + parameters[2]);
                }
                DecimalCount = decimalCount;
                DecimalSeperator = parameters[3];
                ListSeperator = parameters[4];
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
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 1)
                    {
                        BackupPath = optionParameters[0];
                    }
                    else
                    {
                        throw new ToolException("Only one parameter is expected for option '" + optionName + "':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                ScientificMode = ScientificMode.KeepRound;
                if (hasOptionParameters)
                {
                    switch (optionParametersString)
                    {
                        case "0":
                            ScientificMode = ScientificMode.RemoveRound;
                            break;
                        case "1":
                            ScientificMode = ScientificMode.KeepRound;
                            break;
                        case "2":
                            ScientificMode = ScientificMode.Ignore;
                            break;
                        default:
                            throw new ToolException("Invalid mode for option '" + optionName + ": " + optionParametersString);
                                 
                    }
                }
            }
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 1)
                    {
                        ThousandsSeperator = optionParameters[0];
                    }
                    else
                    {
                        throw new ToolException("Only one parameter is expected for option '" + optionName + "':" + optionParametersString);
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
            if (DecimalCount < 0)
            {
                throw new ToolException("Decimal count should equal to or larger than zero: " + DecimalCount);
            }
        }
    }
}
