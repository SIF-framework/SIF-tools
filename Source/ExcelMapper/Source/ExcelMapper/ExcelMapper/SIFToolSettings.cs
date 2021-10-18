// ExcelMapper is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ExcelMapper.
// 
// ExcelMapper is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ExcelMapper is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ExcelMapper. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.ExcelMapper
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string ExcelFilename { get; set; }
        public string BaseString { get; set; }
        public string OutputFilename { get; set; }

        public bool IsMerged { get; set; }
        public bool IsVarExpanded { get; set; }
        public bool IsBottomUp { get; set; }
        public bool IsDoubleEmptyLineRemoved { get; set; }
        public string InsertedString { get; set; }
        public string AppendedString { get; set; }
        public int StartRow { get; set; }
        public int SheetNumber { get; set; }
        public List<string> ReplacedCellStrings { get; set; }
        public List<string> ReplacementCellStrings { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            ExcelFilename = null;
            BaseString = null;
            OutputFilename = null;

            StartRow = 0;
            SheetNumber = 0;
            IsMerged = false;
            IsVarExpanded = false;
            IsBottomUp = false;
            IsDoubleEmptyLineRemoved = false;
            InsertedString = null;
            AppendedString = null;
            ReplacedCellStrings = new List<string>();
            ReplacementCellStrings = new List<string>();
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("xlsx", "Filename of Excelfile with input to process", "C:\\Test\\Input.xslx");
            AddToolParameterDescription("bstr", "Base string, or filename with base string(s), to write to the outputfile for each row in the Excelsheet\n"
                + "- '{i}'-substrings with i an integer (one based) or an alphabetic column character, are replaced with \n"
                + "  the row value in column i\n"
                + "- '{A1}'-substrings with A1 an absolute cell address, are replaced with the cell value\n" 
                + "- '{A1x}'-substrings with A1 an absolute cell address and x one of '^','v','>','<', are replaced with\n" 
                + "  the last non-empty cell in the direction specified (i.e. up, down, right, left), starting from cell {A1}\n" 
                + "- '{=FUNC(x)}'-substrings with FUNC any simple Excel-function with one parameter and x one of the above \n" 
                + "  cell/column references, are replaced with the result of this function applied to the specified cellvalue.\n" 
                + "- when any of the specified {}-substrings in a line refers to an empty cell, the line for that row is skipped\n" 
                + "- when str is an empty string, the contents of the first column are simply exported\n" 
                + "- when a filename is not used: add newlines with \\n",
                "\"ECHO Input{1} {2}.IDF Output{3}.IDF >> info.txt\"");
            AddToolParameterDescription("outf", "Name of output textfile to create or add to", "Test\\Script.bat");

            AddToolOptionDescription("a", "Define appended string, or filename with appended string(s), to add to output after processing Excel-rows", null, "Append string: {0}", new string[] { "a1" });
            AddToolOptionDescription("i", "Define inserted string, or filename with inserted string(s), to add to output before processing Excel-rows", null, "Insert string: {0}", new string[] { "i1" });
            AddToolOptionDescription("r", "Start processing at row r1 of input sheet (one based, default is 1)", "/r:4", "Start processing at (one-based) row: {0}", new string[] { "r1" });
            AddToolOptionDescription("s", "Use Excelsheet s1 for input (default 1)", "/s:2", "Process data from (one-based) sheet: {0}", new string[] { "s1" });
            AddToolOptionDescription("m", "Do not overwrite an existing output file, but merge to existing outputfile", "/m", "Result is merged with existing output file");
            AddToolOptionDescription("p", "Replace substrings si in selected cells with other strings ri (use english notation for values)", "/p:typeA,1,typeB,2", "The following search/replacement strings are used for cellvalues: {...}", new string[] { "s1", "r1", "..." });
            AddToolOptionDescription("x", "Expansd enviroment variables in output strings. Note: variables between {}-symbols are always expanded.", "/x", "Environment variables are expanded");
            AddToolOptionDescription("u", "Process rows from bottom upwards, upto startrow (default is downwards from startrow)", "/u", "Rows are processed from bottom upwards");
            AddToolOptionDescription("e", "Remove empty lines below an other empty line in result file", "/e", "Consequtive empty lines are removed");

            AddToolUsageOptionPostRemark("Note: Surround with double quotes in case of spaces in command or option");
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
                ExcelFilename = parameters[0];
                BaseString = parameters[1];
                OutputFilename = parameters[2];
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
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int intValue))
                    {
                        throw new ToolException("Could not parse value for option 'r':" + optionParametersString);
                    }
                    StartRow = intValue;
                }
                else
                {
                    throw new ToolException("Please specify row number after 'r:': " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("m"))
            {
                IsMerged = true;
            }
            else if (optionName.ToLower().Equals("x"))
            {
                IsVarExpanded = true;
            }
            else if (optionName.ToLower().Equals("u"))
            {
                IsBottomUp = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                IsDoubleEmptyLineRemoved = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int intValue))
                    {
                        throw new ToolException("Could not parse value for option 's':" + optionParametersString);
                    }
                    SheetNumber = intValue;
                }
                else
                {
                    throw new ToolException("Please specify sheet number index after 's:': " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("a"))
            {
                if (hasOptionParameters)
                {
                    AppendedString = optionParametersString;
                }
                else
                {
                    throw new ToolException("Please specify string to append after 'a:'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                if (hasOptionParameters)
                {
                    InsertedString = optionParametersString;
                }
                else
                {
                    throw new ToolException("Please specify string to insert after 'i:'");
                }
            }
            else if (optionName.ToLower().Equals("p"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if ((optionParameters.Length % 2) == 0)
                    {
                        for (int i = 0; i < optionParameters.Length; i += 2)
                        {
                            ReplacedCellStrings.Add(optionParameters[i]);
                            if (ReplacedCellStrings.Equals(string.Empty))
                            {
                                throw new ToolException("Replaced string cannot be empty: " + optionParametersString);
                            }
                            ReplacementCellStrings.Add(optionParameters[i + 1]);
                        }
                    }
                    else
                    {
                        throw new ToolException("Parameter count should be a multiple of two for option 'p:': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Please specify parameters for option 'p:'");
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
            if (ExcelFilename != null)
            {
                ExcelFilename = ExpandPathArgument(ExcelFilename);
                if (!File.Exists(ExcelFilename))
                {
                    throw new ToolException("Input file does not exist: " + ExcelFilename);
                }
            }

            OutputFilename = ExpandPathArgument(OutputFilename);

            // Create output path if not yet existing
            if (!Directory.Exists(Path.GetDirectoryName(OutputFilename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFilename));
            }

            if (StartRow < 0)
            {
                throw new ToolException("Value 1 or larger expected for option r");
            }
            if (SheetNumber < 0)
            {
                throw new ToolException("Value 1 or larger expected for option s");
            }

            // Set default values
            if (StartRow == 0)
            {
                StartRow = 1;
            }
            if (SheetNumber == 0)
            {
                SheetNumber = 1;
            }
        }
    }
}
