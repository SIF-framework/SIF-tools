// HFBmanager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HFBmanager.
// 
// HFBmanager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HFBmanager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HFBmanager. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.HFBManager
{
    public enum FaultAction
    {
        Split,
        CreateCSV
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public bool IsRecursive { get; set; }
        public string UnitColumnRef { get; set; }
        public List<FaultDefinition> FaultDefinitions { get; set; }
        public List<FaultAction> FaultActions = new List<FaultAction>();
        public float DefaultWeight { get; set; }
        public string MergedCSVFilename { get; set; }

        /// <summary>
        /// Path and filename of file that defines order of units
        /// </summary>
        public string OrderFilename { get; set; }
        /// <summary>
        /// Name or number of sheet to process in order file
        /// </summary>
        public string OrderFileSheetRef { get; set; }
        public int OrderFileRowNr { get; set; }
        public int OrderFileColNr { get; set; }

        public string FaultGENFilenameTemplateString {get; set;}

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

            UnitColumnRef = "EENHEID";
            FaultDefinitions = null;
            FaultActions = new List<FaultAction>() { FaultAction.CreateCSV };
            DefaultWeight = 100000f;

            OrderFilename = null;
            OrderFileSheetRef = "1"; // Note: this is both valid for XLSX and CSV
            OrderFileRowNr = 2;
            OrderFileColNr = 2;

            MergedCSVFilename = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input GEN-files (e.g. *.GEN)", "*.GEN");
            AddToolParameterDescription("outPath", "Path or filename (CSV or GEN) to write results. For multiple input files, CSV-file will be merged.", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is processed recursively");
            AddToolOptionDescription("c", "Specify column for geological unit (default: EENHEID) in input GEN-file: column number of name", "/c:5", "Column reference for unit in input GEN-file: {0}", new string[] { "c1" });
            AddToolOptionDescription("o", "Specify order file (xlsx or csv) for geological units. Expected order is top-down. The row with\n" + 
                                          "the first unit, a column number and a sheet name or number can be specified (default: 2,2,1)", "/o:regis_order.csv", "Order file: {...}", new string[] { "f" }, new string[] { "r", "c", "s" }, new string[] { "2", "2", "1" });
            AddToolOptionDescription("f", "Specify one or more (comma-seperated) fault definitions as a list of upper-units or (upper-unit;weight)-pairs\n" +
                                          "(semi-colon seperated) that indicate the upper unit of a fault section and (optionally) its weight (days);\n" +
                                          "the weight is supposed to apply all units up to and including this geological unit; the need for a weight\n" + 
                                          "depends on the specified action(s)", "/w:HLc;500,KIk1;100000", "Fault Definitions: {...}", new string[] { "w1" }, new string[] { "..." });
            AddToolOptionDescription("w", "Specify default weight which is used when fault definition(s) are missing (default: 100000)", "/w:10000", "Default weight: {0}", new string[] { "w1" });
            AddToolOptionDescription("a", "Specify one or more action numbers to perform in specified order (default:1):\n" + 
                                          "  1=Split faults (specify upper-units); 2=Create CSV (specify (upper-unit;weight)-pairs)\n" + 
                                          "  order '1,2' ensures resulting GEN-file of action 1 is used for action 2\n" + 
                                          "Specify optional action settings by adding semi-colon after number:\n" + 
                                          "- for action 2, specify input CSV-filename from which input lines are copied to (before) new results", "/a:1,2;C:\\Test\\faults.CSV\n", "Performed actions: {...}", new string[] { "a1" }, new string[] { "..." });
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
             switch (optionName)
             {
                case "a":
                    string[] optionParameterValues = parameterValue.Split(new char[] { ';' });
                    switch (optionParameterValues[0])
                    {
                        case "1": return "Split";
                        case "2": return "Create " + ((optionParameterValues.Length > 1) ? "merged " : string.Empty) + "CSV";

                        default: return parameterValue;
                    }
                //case "x":
                //    switch (parameter)
                //    {
                //        case "x1":
                //            switch (parameterValue)
                //            {
                //                case "Value1": return "Value1";
                //                default: return parameterValue;
                //            }
                //        default: return parameterValue;
                //    }
                default: return parameterValue;
             }
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
                InputPath = parameters[0];
                InputFilter = parameters[1];
                OutputPath = parameters[2];

                // Split output path in path and filename if both are specified
                if (Path.HasExtension(OutputPath))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null for now
                    OutputFilename = null;
                }

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
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("o"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    OrderFilename = optionParameters[0];

                    if (optionParameters.Length > 1)
                    {
                        if (int.TryParse(optionParameters[1], out int value))
                        {
                            OrderFileRowNr = value;
                        }
                        else
                        {
                            throw new ToolException("�nvalid row number for option '" + optionName + "':" + optionParameters[1]);
                        }
                    }

                    if (optionParameters.Length > 2)
                    {
                        if (int.TryParse(optionParameters[1], out int value))
                        {
                            OrderFileColNr = value;
                        }
                        else
                        {
                            throw new ToolException("�nvalid row number for option '" + optionName + "':" + optionParameters[1]);
                        }
                    }

                    if (optionParameters.Length > 3)
                    {
                        OrderFileSheetRef = optionParameters[3];
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("a"))
            {
                if (hasOptionParameters)
                {
                    FaultActions.Clear();

                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    foreach (string optionParameter in optionParameters)
                    {
                        string[] optionParameterValues = optionParameter.Split(new char[] { ';' });
                        switch (optionParameterValues[0])
                        {
                            case "1":
                                FaultActions.Add(FaultAction.Split);
                                break;
                            case "2":
                                FaultActions.Add(FaultAction.CreateCSV);

                                if (optionParameterValues.Length > 1)
                                {
                                    MergedCSVFilename = optionParameterValues[1];
                                }

                                break;
                            default:
                                throw new ToolException("Unknown value for option '" + optionName + "': "  + optionParameter);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter value(s) expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    UnitColumnRef = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("w"))
            {
                if (hasOptionParameters)
                {
                    if (float.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out float weight))
                    {
                        DefaultWeight = weight;
                    }
                    else
                    {
                        throw new ToolException("Invalid (floating point) value for weight: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("f"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    FaultDefinitions = new List<FaultDefinition>();
                    foreach (string hfbDef in optionParameters)
                    {
                        string[] hfbDefValues = hfbDef.Split(new char[] { ';' });
                        if (hfbDefValues.Length == 1)
                        {
                            FaultDefinitions.Add(new FaultDefinition(hfbDefValues[0], DefaultWeight));
                        }
                        else if (hfbDefValues.Length == 2)
                        {
                            if (!float.TryParse(hfbDefValues[1], NumberStyles.Float, EnglishCultureInfo, out float weight))
                            {
                                throw new ToolException("Invalid weight value in fault definition: " + hfbDef);
                            }
                            FaultDefinitions.Add(new FaultDefinition(hfbDefValues[0], weight));
                        }
                        else
                        {
                            throw new ToolException("Invalid fault definition: " + hfbDef);
                        }
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
            if ((InputFilter != null) && (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.GEN";
            }

            // Check tool option values
            if (UnitColumnRef == null)
            {
                throw new ToolException("Please define a unit column string via option 'c'");
            }
        }
    }
}
