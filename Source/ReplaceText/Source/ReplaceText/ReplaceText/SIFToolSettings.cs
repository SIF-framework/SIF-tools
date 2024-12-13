// ReplaceText is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ReplaceText.
// 
// ReplaceText is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ReplaceText is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ReplaceText. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.ReplaceText
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultLogFilename = "ReplaceText.log";

        public string BasePath { get; set; }
        public string Filter { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }

        public bool IsFirstOnly { get; set; }
        public bool IsFindOnly { get; set; }
        public bool IsRecursive { get; set; }
        public bool IsRegExp { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool HasLogfile { get; set; }
        public bool IsMatchCountReturned { get; set; }
        public bool IsErrorNoMatch { get; set; }
        public bool IsDateTimeReset { get; set; }
        public string LogFilename { get; set; }
        public bool IsBinarySkipped { get; set; }
        public List<string> ExcludePatterns { get; set; }
        public bool IsMatchesShown { get; set; }
        public bool IsMatchedStringShown { get; set; }

        /// <summary>
        /// If true, no text1/text2 are defined and only environment variables are replaced in the input file
        /// </summary>
        public bool IsOnlyEnvVarsExpanded { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            BasePath = null;
            Filter = null;
            Text1 = null;
            Text2 = null;

            IsFirstOnly = false;
            IsFindOnly = false;
            IsRecursive = false;
            IsRegExp = false;
            IsCaseSensitive = false;
            HasLogfile = false;
            IsMatchCountReturned = false;
            IsErrorNoMatch = false;
            IsDateTimeReset = false;
            LogFilename = null;
            IsBinarySkipped = true;
            ExcludePatterns = null;
            IsMatchesShown = false;
            IsMatchedStringShown = true;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("path", "Path to directory to start search, leave empty (\"\") to use filter as a filename", "C:\\Test\\Input", new int[] { 0, 1 });
            AddToolParameterDescription("filter", "Filter to select files in path (e.g. *.RUN) or specify a filename in combination with empty path", "*.RUN", new int[] { 0, 1 });
            AddToolParameterDescription("text1", "The replaced text", "\".:\\\\.+\\DBASE\"", new int[] { 0 });
            AddToolParameterDescription("text2", "The new text", "\"C:\\XXX\\Model\\DBASE\"", new int[] { 0 });
            AddToolUsageOptionPreRemark("Note: When text1/text2 are not defined (syntax 2), all environment variables in specified inputfiles are expanded.\n");

            AddToolOptionDescription("1", "Replace only first occurrance of text1", null, "Only first occurance of text1 is replaced.");
            AddToolOptionDescription("b", "Process 'binary' files (or actually: files containing nul-characters)", null, "Binary files are also processed", null, null, null, new int[] { 0, 1 } );
            AddToolOptionDescription("c", "Match case", "/c", "Replace/find is case-sensitive");
            AddToolOptionDescription("d", "Reset original (create, access and write) date and time of modified files", "/d", "Date and time of modified files are reset to original date/time", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("e", "exclude all matches that contain any of the specified patterns", "/e:E:\\\\Program,F:\\\\System", "Exclude matches for one of the following patterns: {...}", new string[] { "p1" }, new string[] { "..." });
            AddToolOptionDescription("f", "Only find and return number of matches, but don't modify files (includes option m).\nIn this case some text2 has to be supplied, but is not used.", null, "Text1 is searched instead of replaced");
            AddToolOptionDescription("l", "Append log messages to file instead of writing to console. Only replacement messages are written to console.", "/l", "Logmessages are written to logfile: {0}", null, new string[] { "f" }, new string[] { DefaultLogFilename }, new int[] { 0, 1 });
            AddToolOptionDescription("n", "Throw error when searchtext has no match", null, "No match results in an error");
            AddToolOptionDescription("m", "Return matchcount instead of errorcode: \nA negative number indicates an error, a positive number equals the total number of matches", null, "Matchcount is returned instead of errorcode");
            AddToolOptionDescription("r", "Process subfolders in path recursively", "/r", "Subdirectories are searched recursively", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("x", "Use regular expressions (check internet for a description) for text1, text2 and all patterns", "/x", "Using regular expressions");
            AddToolOptionDescription("sum", "Show summary of matched patterns/strings per file after finishing", "/sum", "Summary of matched patterns/strings is shown", null, null, null, new int[] { 0, 1 });

            AddToolUsageOptionPostRemark("Note: use an @-character before text1 and/or text2 to parse escape characters,\n" +
                                         "      otherwise the character will be read exactly as specified.");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 4)
            {
                // Parse syntax 1:
                BasePath = parameters[0];
                Filter = parameters[1];
                Text1 = parameters[2];
                Text2 = parameters[3];
                groupIndex = 0;
            }
            else if (parameters.Length == 2)
            {
                // Parse syntax 2:
                BasePath = parameters[0];
                Filter = parameters[1];
                Text1 = null;
                Text2 = null;
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
            if (optionName.ToLower().Equals("1"))
            {
                IsFirstOnly = true;
            }
            else if (optionName.ToLower().Equals("b"))
            {
                IsBinarySkipped = false;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsCaseSensitive = true;
            }
            else if (optionName.ToLower().Equals("d"))
            {
                IsDateTimeReset = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    ExcludePatterns = new List<string>(optionParametersString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else if (optionName.ToLower().Equals("f"))
            {
                IsFindOnly = true;
                IsMatchCountReturned = true;
            }
            else if (optionName.ToLower().Equals("l"))
            {
                HasLogfile = true;
                if (hasOptionParameters)
                {
                    LogFilename = optionParametersString;
                }
                if (LogFilename == null)
                {
                    LogFilename = DefaultLogFilename;
                }
            }
            else if (optionName.ToLower().Equals("n"))
            {
                IsErrorNoMatch = true;
            }
            else if (optionName.ToLower().Equals("m"))
            {
                IsMatchCountReturned = true;
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("sum"))
            {
                IsMatchesShown = true;
            }
            else if (optionName.ToLower().Equals("x"))
            {
                IsRegExp = true;
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

            if (!BasePath.Equals(string.Empty) && !Directory.Exists(BasePath))
            {
                throw new ToolException("First parameter should be an existing directory: '" + BasePath + "'");
            }

            if (BasePath.Equals(string.Empty))
            {
                BasePath = Path.GetDirectoryName(Filter);
                Filter = Path.GetFileName(Filter);
            }

            if (BasePath.Equals(string.Empty))
            {
                BasePath = Directory.GetCurrentDirectory();
            }

            BasePath = Path.GetFullPath(BasePath);

            if (Filter.Contains(Path.DirectorySeparatorChar))
            {
                throw new ToolException("Filter should not contain path separator(s): " + Filter);
            }

            // Check that Text1 is not empty
            if ((Text1 != null) && Text1.Equals(string.Empty))
            {
                throw new ToolException("Please specify a non-empty string for text1");
            }

            // Check that when either Text1 or Text2 is specified, Text2/Text1 is also specified
            if ((Text1 != null) && !Text1.Equals(string.Empty))
            {
                if (Text2 == null)
                {
                    throw new ToolException("Please specify a non-null string for text2");
                }
            }

            if ((Text2 != null) && !Text2.Equals(string.Empty))
            {
                if ((Text1 == null) || Text1.Equals(string.Empty))
                {
                    throw new ToolException("Please specify a non-empty string for text1");
                }
            }

            // Check if only environments variables should be expanded in input files
            IsOnlyEnvVarsExpanded = (Text1 == null) && (Text2 == null);
        }
    }
}
