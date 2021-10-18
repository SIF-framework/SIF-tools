// IPFreorder is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFreorder.
// 
// IPFreorder is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFreorder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFreorder. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IPFreorder
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string ColumnSeparator = ";";
        public const string NewColumnPrefix = "+";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }

        public bool IsOverwrite { get; set; }
        public bool IsRecursive { get; set; }
        public string[] SourceColumnStrings { get; set; }       // columnindices or names of source IPF-file
        public string[] TargetColumnNames { get; set; }         // new column names in target IPF-file for constant values
        public string[] TargetColumnExpressions { get; set; }   // constant values
        public int AssociatedFileColumnIndex { get; set; }      

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
            SourceColumnStrings = null;
            TargetColumnNames = null;
            TargetColumnExpressions = null;
            AssociatedFileColumnIndex = -1;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to input IPF-file", "C:\\Test\\Input", new int[] { 0, 1 });
            AddToolParameterDescription("filter", "filter (e.q. *_L?.IPF) or single filename, for IPF-file(s) to process", "*_L?.IPF", new int[] { 0, 1 });
            AddToolParameterDescription("outPath", "Path for output IPF-file (including filename in case of a single IPF-filename)", "C:\\Test\\Output", new int[] { 0, 1 });
            AddToolParameterDescription("colDef", "A column (re)definition for each column in the result IPF-file, seperated by spaces, defined as follows:\n"
                                                + "col             : column number (one-based) or columnname in the source IPF-file\n"
                                                + "col" + ColumnSeparator + "colname     : column number/name in the source IPF-file and a new columnname for the target IPF-file\n"
                                                + NewColumnPrefix + "colname" + ColumnSeparator + "colval : column name for a new column in the target IPF-file, and a\n" 
                                                + "                  single string for the column value of all existing rows.\n"
                                                + "Notes: when no column definitions are specified, all columns are copied without reordering.\n"
                                                + "       environment variables are replaced before evaluating column definitions.",
                                                "1 2 3;ID +ColumnA;\"some value\"", true, new int[] { 0 });
            AddToolOptionDescription("a", "define column number (in new IPF-file) of associated files, use 0 for IPF-files without associated files\n"
                                        + "if not specified, for columns with existing associated files, the index is corrected for the new order", "/a:3", "Column index of associated files: {0}", new string[] { "i" }, null, null, new int[] { 0, 1 } );
            AddToolOptionDescription("o", "Overwrite existing target IPF-files; if not specified, existing files will be skipped", "/o", "Existing output files are overwritten", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is processed recursively", null, null, null, new int[] { 0, 1 });
            AddToolUsageOptionPostRemark("Note: all parameters can be (partly) surrounded by \"-characters to include spaces", 1);
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length >= 3)
            {
                // Parse syntax 1:
                groupIndex = 0;
                InputPath = parameters[0];
                InputFilter = parameters[1];
                OutputPath = parameters[2];

                if (parameters.Length > 3)
                {
                    // Parse column definitions
                    ParseColumnArguments(parameters, 3, out string[] sourceColumnStrings, out string[] targetColumnNames, out string[] targetColumnExpressions);
                    this.SourceColumnStrings = sourceColumnStrings;
                    this.TargetColumnNames = targetColumnNames;
                    this.TargetColumnExpressions = targetColumnExpressions;
                }
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
            if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("a"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    if (!int.TryParse(optionParametersString, out int idx))
                    {
                        throw new ToolException("Invalid column number for option 'a': " + optionParametersString);
                    }
                    AssociatedFileColumnIndex = idx;
                }
                else
                {
                    throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                }
            }
            else
            {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        protected virtual void ParseColumnArguments(string[] parameters, int startIdx, out string[] sourceColumnStrings, out string[] targetColumnNames, out string[] targetColumnExpressions)
        {
            // Parse columnn arguments
            sourceColumnStrings = new string[parameters.Length - startIdx];
            targetColumnNames = new string[parameters.Length - startIdx];
            targetColumnExpressions = new string[parameters.Length - startIdx];

            for (int idx = startIdx; idx < parameters.Length; idx++)
            {
                string columnString = parameters[idx].Replace("\"", string.Empty);
                sourceColumnStrings[idx - startIdx] = null;
                targetColumnNames[idx - startIdx] = null;
                targetColumnExpressions[idx - startIdx] = null;
                if (columnString.StartsWith(NewColumnPrefix))
                {
                    if ((columnString.Length >= 4) && columnString.Contains(ColumnSeparator))
                    {
                        string namevalueString = columnString.Substring(1);
                        string[] partsString = namevalueString.Split(new string[] { ColumnSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        if (partsString.Length == 2)
                        {
                            targetColumnNames[idx - startIdx] = partsString[0];
                            targetColumnExpressions[idx - startIdx] = partsString[1];
                        }
                        else
                        {
                            throw new ToolException("Invalid name-value pair (" + NewColumnPrefix + "<colname>;<colval>): " + parameters[idx]);
                        }
                    }
                    else
                    {
                        throw new ToolException("Invalid name-value pair (" + NewColumnPrefix + "<colname>;<colval>): " + parameters[idx]);
                    }
                }
                else
                {
                    if (columnString.Contains(ColumnSeparator))
                    {
                        string[] partsString = columnString.Split(new string[] { ColumnSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        if (partsString.Length == 2)
                        {
                            if ((partsString[0] == null) || partsString[0].Equals(string.Empty))
                            {
                                throw new ToolException("Invalid col-colname pair (<col>;<colname>): " + parameters[idx]);
                            }
                            sourceColumnStrings[idx - startIdx] = partsString[0];
                            targetColumnNames[idx - startIdx] = partsString[1];
                        }
                        else
                        {
                            throw new ToolException("Invalid col-colname pair (<col>;<colname>): " + parameters[idx]);
                        }
                    }
                    else
                    {
                        sourceColumnStrings[idx - startIdx] = parameters[idx];
                        if ((parameters[idx] == null) || parameters[idx].Equals(string.Empty))
                        {
                            throw new ToolException("argument " + idx + " (" + parameters[idx] + ") is not valid column indicator");
                        }
                    }
                }
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
                    throw new ToolException("Input path does not exist: " + InputPath);
                }
            }

            // Check tool parameters
            if ((InputFilter != null) && (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.IPF";
            }

            // Retrieve full output path 
            try
            {
                OutputPath = Path.GetFullPath(OutputPath);
            }
            catch (Exception ex)
            {
                throw new ToolException("Specified output path is not a valid file- or directoryname: " + ex.GetBaseException().Message);
            }

            if (AssociatedFileColumnIndex < -1)
            {
                throw new ToolException("Value 0 or larger expected for option 'a': " + AssociatedFileColumnIndex);
            }
        }
    }
}
