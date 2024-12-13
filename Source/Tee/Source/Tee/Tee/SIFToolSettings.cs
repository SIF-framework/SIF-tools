// Tee is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Tee.
// 
// Tee is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Tee is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Tee. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.Tee
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string ErrorMessagePrefix = "ERROR:";

        public string OutputFilename { get; set; }
        public bool IsOutputAppended { get; set; }
        public bool IsCharacterMode { get; set; }
        public bool IsErrorLevelSet { get; set; }
        public bool IsQuestionEchoed { get; set; }
        public int MaxQuestionLines { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            OutputFilename = null;
            IsOutputAppended = false;
            IsCharacterMode = false;
            IsErrorLevelSet = true;
            IsQuestionEchoed = false;

            // Set to zero to turn off maximum limit
            MaxQuestionLines = 0;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("outFile", "Full path and filename of outputfile", "C:\\Test\\Output\\SomeTool.log");
            AddToolOptionDescription("a", "Append output to outputfile. Otherwise an existing outputfile is overwritten", "/a", "Output is appended to outputfile");
            AddToolOptionDescription("c", "Run in charactermode: read and write characters until EOL or question marks\n" +
                                          "Note: this ensures that lines are shown that end with a question mark and wait for user input", "/c", "Processing in charactermode");
            AddToolOptionDescription("e", "Turn off setting of ERRORLEVEL environment variable\n" +
                                          "Default ERRORLEVEL is set to 1 when one or more lines start with '" + ErrorMessagePrefix + "'.", null, "Errorlevel-setting for '" + ErrorMessagePrefix + "'-strings is turned off");
            AddToolOptionDescription("?", "Only echo lines to console that end with a question mark. All previous lines upto first empty line are\n" +
                                          "shown (default), limited by maximum number of lines c. Use c=1 to show only line with question mark.", null, null, null, new string[] { "c" }, new string[] { "0" });
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 1)
            {
                // Parse syntax 1:
                OutputFilename = parameters[0];
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
            if (optionName.ToLower().Equals("a"))
            {
                IsOutputAppended = true;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsCharacterMode = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                IsErrorLevelSet = false;
            }
            else if (optionName.ToLower().Equals("?"))
            {
                IsQuestionEchoed = true;
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int count))
                    {
                        throw new ToolException("Invalid maximum number of question lines for option '" + optionName + "':" + optionParametersString);
                    }
                    MaxQuestionLines = count;
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

            // Check output filename
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (Path.GetFileName(OutputFilename).Contains(c))
                {
                    throw new ToolException("Output filename constains invalid character '" + c + "': " + Path.GetFileName(OutputFilename));
                }
            }
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (Path.GetDirectoryName(OutputFilename).Contains(c))
                {
                    throw new ToolException("Output path constains invalid character '" + c + "': " + Path.GetDirectoryName(OutputFilename));
                }
            }

            // Ensure output path exists
            string path = Path.GetDirectoryName(OutputFilename);
            if ((path != null) && !path.Equals(string.Empty) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFilename));
            }
        }
    }
}
