// ReplaceLine is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ReplaceLine.
// 
// ReplaceLine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ReplaceLine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ReplaceLine. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.ReplaceLine
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultSubdirName = "backup";

        public string Filename { get; set; }
        public int Linenumber { get; set; }
        public string NewLine { get; set; }
        public bool IsBackedUp { get; set; }
        public string BackupSubdirname { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            Filename = null;
            Linenumber = -1;
            NewLine = null;
            IsBackedUp = false;
            BackupSubdirname = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("file", "Path and filename of file to modeify", "C:\\Test\\Input\\RJ_STAT_HUIDIG3.RUN");
            AddToolParameterDescription("linenr", "Number of line to modify", "1");
            AddToolParameterDescription("line", "New line to replace line at specified linenumber", "C:\\TEMP\\RJ_STAT\\RESULTS\\HUIDIG3");
            AddToolOptionDescription("b", "Make backup of sourcefile in subdirectory with specified or default name (" + DefaultSubdirName + ")", "/b", "Sourcefile is backed up to: {0}", null, new string[] { "subdir" });
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
                Filename = parameters[0];
                string lineNrString = parameters[1];
                int nr;
                if (!int.TryParse(lineNrString, out nr))
                {
                    throw new ToolException("Invalid linenumber, a positive integer value (one-based) is expected: " + lineNrString);
                }
                Linenumber = nr;
                NewLine = parameters[2];
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
                IsBackedUp = true;
                if (hasOptionParameters)
                {
                    BackupSubdirname = optionParametersString;
                    if ((BackupSubdirname.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) || (BackupSubdirname.IndexOfAny(Path.GetInvalidPathChars()) >= 0))
                    {
                        throw new ToolException("Name of backup subdirectory contains invalid characters: " + BackupSubdirname);
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
        /// Check the number of parsed arguments against the number of expected arguments. Override to check actual values.
        /// </summary>
        public override void CheckSettings()
        {
            // Perform syntax checks 
            base.CheckSettings();

            if (!File.Exists(Filename))
            {
                throw new ToolException("Filename should refer to an existing file: '" + Filename + "'");
            }

            if (Linenumber <= 0)
            {
                throw new ToolException("Linenumber is one-based and should one or larger: " + Linenumber);
            }
        }
    }
}
