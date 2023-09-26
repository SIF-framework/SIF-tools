// ExcelSelect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ExcelSelect.
// 
// ExcelSelect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ExcelSelect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ExcelSelect. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.ExcelSelect
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputFilename { get; set; }
        public string OutputFilename { get; set; }

        public bool IsColumnIndicesSpecified { get; set; }

        /// <summary>
        /// zero based column index for x-coordinate
        /// </summary>
        public int XColIdx { get; set; }
        /// <summary>
        /// zero based column index for y-coordinate
        /// </summary>
        public int YColIdx { get; set; }

        public List<int> SheetIndices { get; set; }
        public int StartRowIdx { get; set; }
        public int FilenameColIdx { get; set; }
        public int BasePathColIdx { get; set; }
        private string baseDefString;
        public string BasePath { get; set; }
        public string DefaultFileExtension { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputFilename = null;
            OutputFilename = null;

            SheetIndices = new List<int>();
            SheetIndices.Add(0);
            StartRowIdx = 1;
            IsColumnIndicesSpecified = false;
            XColIdx = 0;
            YColIdx = 1;
            FilenameColIdx = -1;
            BasePathColIdx = -1;
            BasePath = null;
            DefaultFileExtension = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path and filename of input Excel-file (.xlsx)", "C:\\Test\\Input");
            AddToolParameterDescription("outPath", "Path and filename of resulting Excel-file (.xlsx)", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Specify row index (one based) of first data row to process (default:1)", "/r:4", "First data row to process: {0}", new string[] { "r1" });
            AddToolOptionDescription("f", "select rows with existing files; specify (one based) column index (fc) for filename and optional column\n" +
                                          "index or string (p) for basepath (relative to Excelsheet) of filename and optional default extension (x)",
                                          "/f:5,4", "Select rows with existing files: {0}", new string[] { "fc" }, new string[] { "p", "x" }, new string[] { string.Empty, string.Empty });
            AddToolOptionDescription("s", "Specify selected sheet(s) (one based, default: 1). If an option to select rows is used, a single sheet\n" +
                                          "can be specified and the other sheets are copied. If no options are used to select rows, one or more\n" +
                                          "(comma-seperated) sheetnumbers can be specified for sheets to select. Other sheets are deleted.", "/s:2", "Selected sheetnumber(s): {0}", new string[] { "s1" });
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
                InputFilename = parameters[0];
                OutputFilename = parameters[1];
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
            if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    SheetIndices.Clear();
                    int sheetIdx;
                    string[] values = GetOptionParameters(optionParametersString);
                    foreach (string value in values)
                    {
                        if (int.TryParse(value, out sheetIdx))
                        {
                            sheetIdx = sheetIdx - 1;
                            SheetIndices.Add(sheetIdx);
                        }
                        else
                        {
                            throw new ToolException("Missing (one based) sheetnumber index for option '" + optionName + "': " + optionParametersString);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing sheetnumber for option '" + optionName + "': " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("r"))
            {
                if (hasOptionParameters)
                {
                    int rowIdx;
                    if (int.TryParse(optionParametersString, out rowIdx))
                    {
                        StartRowIdx = rowIdx - 1;
                    }
                    else
                    {
                        throw new ToolException("Missing valid (one based) rownumber for option '" + optionName + "': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing rownumber for option '" + optionName + "': " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("f"))
            {
                if (hasOptionParameters)
                {
                    int colIdx;
                    string[] values = GetOptionParameters(optionParametersString);
                    if (values.Length > 0)
                    {
                        if (int.TryParse(values[0], out colIdx))
                        {
                            FilenameColIdx = colIdx - 1;
                        }
                        else
                        {
                            throw new ToolException("Missing (one based) column index for option '" + optionName + "': " + optionParametersString);
                        }

                        if (values.Length > 1)
                        {
                            baseDefString = values[1].Replace("\"", string.Empty.Trim());
                        }

                        if (values.Length > 2)
                        {
                            DefaultFileExtension = values[2];
                        }
                    }
                    else
                    {
                        throw new ToolException("Missing (one based) column index of one or higher for option '" + optionName + "': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing sheetnumber for option '" + optionName + "': " + optionParametersString);
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

            if ((!File.Exists(InputFilename) || !Path.GetExtension(InputFilename).ToLower().Equals(".xlsx")))
            {
                throw new ToolException("Please specify an existing Excel workmap (XLSX): " + InputFilename);
            }

            // Check specified input
            InputFilename = Path.GetFullPath(InputFilename);
            OutputFilename = Path.GetFullPath(OutputFilename);

            if (!Path.GetExtension(OutputFilename).ToLower().Equals(".xlsx"))
            {
                OutputFilename = Path.ChangeExtension(OutputFilename, ".xlsx");
            }

            // Create output path if not yet existing
            if (!Directory.Exists(Path.GetDirectoryName(OutputFilename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFilename));
            }

            if (XColIdx < 0)
            {
                throw new ToolException("Column index for x-columnindex should be greather than or equal to zero:" + (XColIdx + 1));
            }
            if (YColIdx < 0)
            {
                throw new ToolException("Column index for y-columnindex should be greather than or equal to zero:" + (YColIdx + 1));
            }
            if (StartRowIdx < 0)
            {
                throw new ToolException("Starting row number should be greather than or equal to one: " + (StartRowIdx + 1));
            }
            for (int sheetIdx = 0; sheetIdx < SheetIndices.Count; sheetIdx++)
            {
                if (SheetIndices[sheetIdx] < 0)
                {
                    throw new ToolException("Sheet number should be greather than or equal to one:" + (SheetIndices[sheetIdx] + 1));
                }
                if ((sheetIdx > 0) && (SheetIndices[sheetIdx] < SheetIndices[sheetIdx - 1]))
                {
                    throw new ToolException("Sheetindices should be ordered from low to high:" + ToSheetNumberString(SheetIndices));
                }
            }

            if (baseDefString != null)
            {
                int colIdx;
                if (int.TryParse(baseDefString, out colIdx))
                {
                    BasePathColIdx = colIdx - 1;
                    if (BasePathColIdx < 0)
                    {
                        throw new ToolException("Please specify a column index of one or higher for option 'f'" + baseDefString);
                    }
                }
                else
                {
                    BasePath = baseDefString;
                    if (!Path.IsPathRooted(BasePath))
                    {
                        BasePath = Path.Combine(Path.GetDirectoryName(InputFilename), BasePath);
                    }
                    if (!Directory.Exists(BasePath))
                    {
                        throw new ToolException("Please specify either a (one based) column index or an existing base path as second parameter after 'f:': " + baseDefString);
                    }
                }
            }
        }

        /// <summary>
        /// Create readable string from list of sheet indices
        /// </summary>
        /// <param name="sheetIndices"></param>
        /// <returns></returns>
        private string ToSheetNumberString(List<int> sheetIndices)
        {
            string sheetsIndicesString = "";
            for (int sheetIdx = 0; sheetIdx < sheetIndices.Count; sheetIdx++)
            {
                sheetsIndicesString += (sheetIndices[sheetIdx] + 1) + ",";
            }
            return sheetsIndicesString.Substring(0, sheetsIndicesString.Length - 1);
        }
    }
}
