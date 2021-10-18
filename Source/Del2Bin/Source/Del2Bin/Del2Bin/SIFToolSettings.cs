// Del2Bin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Del2Bin.
// 
// Del2Bin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Del2Bin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Del2Bin. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.Del2Bin
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }

        public bool IsRecursive { get; set; }
        public bool IsErrorLevelReturned { get; set; }
        public bool IsReadOnlyDeleted { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            IsErrorLevelReturned = false;
            IsRecursive = false;
            IsReadOnlyDeleted = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("pathfilter", "path and/or filter for file(s) or subdirectory to delete", "C:\\Test\\*_L?.*", 0);
            AddToolParameterDescription("path", "path for file(s), specified by filter, to delete", "C:\\Test\\Input", 1);
            AddToolParameterDescription("filter", "filter (e.q. *_L?.*) or single filename for file(s) to delete", "*_L?.*", 1);
            AddToolOptionDescription("e", "return errorlevel instead of number of deletions (0 for success, 1 in case of errors)", "/e", "errorlevel is returned (0 for success, 1 for errors) instead of number of deletions", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("f", "force deletion of read-only files", "/f", "read-only files are also deleted", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("s", "delete specified files recursively from all subdirectories", "/s", "specified files are deleted recursively from all subdirectories", null, null, null, new int[] { 0, 1 });

            // Settings are similar to MSDOS DEL command:
            //  /P            Prompts for confirmation before deleting each file.
            //  /F            Force deleting of read-only files.
            //  /S            Delete specified files from all subdirectories.
            //  /Q            Quiet mode, do not ask if ok to delete on global wildcard
            //  /A            Selects files to delete based on attributes
            //  attributes    R  Read-only files            S  System files
            //                H  Hidden files               A  Files ready for archiving
            //                I  Not content indexed Files  L  Reparse Points
            //                O  Offline files              -  Prefix meaning not
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
                InputPath = parameters[0];

                if (!Directory.Exists(InputPath))
                {
                    // filter is part of input path
                    InputFilter = Path.GetFileName(InputPath);
                    try
                    {
                        InputPath = Path.GetDirectoryName(InputPath);
                    }
                    catch (Exception)
                    {
                        // Assume just a filter and no path has been specified
                        InputPath = null;
                    }
                }
                else
                {
                    InputFilter = null;
                }

                groupIndex = 0;
            }
            else if (parameters.Length == 2)
            {
                InputPath = parameters[0];
                InputFilter = parameters[1];
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
            if (optionName.ToLower().Equals("e"))
            {
                IsErrorLevelReturned = true;
            }
            else if (optionName.ToLower().Equals("f"))
            {
                IsReadOnlyDeleted = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                IsRecursive = true;
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
            if ((InputPath == null) || InputPath.Equals(string.Empty))
            {
                // Use current path
                InputPath = Directory.GetCurrentDirectory();
            }
            InputPath = ExpandPathArgument(InputPath);
            if (!Directory.Exists(InputPath))
            {
                throw new ToolException("Input path does not exist: " + InputPath);
            }

            // Check restrictions
            CheckRootPath();
            CheckNetworkPath();
        }

        /// <summary>
        /// Perform checks for root input path
        /// </summary>
        protected virtual void CheckRootPath()
        {
            bool isRootPath = FileUtils.IsRootPath(InputPath);
            if (InputFilter != null)
            {
                // A filter or filename was specified
                if (isRootPath && (InputFilter.Contains("?") || InputFilter.Contains("*")))
                {
                    throw new ToolException("Deleting files with a wildcard filter is not allowed in the root path");
                }
            }
            else
            {
                // A directory was specified
                if (isRootPath)
                {
                    throw new ToolException("Deleting rootfolder is not allowed");
                }
            }
        }

        /// <summary>
        /// Perform checks for network input path
        /// </summary>
        protected virtual void CheckNetworkPath()
        {
            bool isNetworkPath = FileUtils.IsNetworkPath(InputPath);
            if (isNetworkPath)
            {
                throw new ToolException("Deleting to recycle bin on a network path is not possible: " + InputPath);
            }
        }
    }
}
