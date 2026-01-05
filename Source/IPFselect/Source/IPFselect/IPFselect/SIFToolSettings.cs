// IPFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFselect.
// 
// IPFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFselect. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.IPFselect
{
    public enum OperatorEnum
    {
        None,
        Multiply,
        Divide,
        Add,
        Substract
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public static CultureInfo DutchCultureInfo = new CultureInfo("nl-NL", false);

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public int XColIdx { get; set; }
        public int YColIdx { get; set; }
        public int ZColIdx { get; set; }
        public string TopLevelString { get; set; }
        public string BotLevelString { get; set; }
        public Extent Extent { get; set; }
        public string ZoneFilename { get; set; }
        public List<float> ZoneValues { get; set; }
        public SelectPointMethod SelectPointMethod { get; set; }
        public string ExpColReference { get; set; }
        public ValueOperator ExpOperator { get; set; }
        public string ExpValue { get; set; }
        public bool UseRegExp { get; set; }
        public List<ColumnExpressionDef> ColumnExpressionDefs { get; set; }
        public string Postfix { get; set; }
        public bool IsMetadataAdded { get; set; }

        public bool IsTSSkipped { get; set; }
        public bool IsEmptyTSPointRemoved { get; set; }
        public DateTime? TSPeriodStartDate { get; set; }
        public DateTime? TSPeriodEndDate { get; set; }
        public bool IsTSPeriodSelected { get; set; }
        public bool IsTSPeriodClipped { get; set; }
        public int TSPeriodValueColIndex { get; set; }
        public List<int> TSValueColIndices { get; set; }
        public List<float> TSValueColFactors { get; set; }
        public bool IsTSNoDataRemoved { get; set; }
        public int TSNoDataValColIdx { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            OutputFilename = null;

            XColIdx = 0;
            YColIdx = 1;
            ZColIdx = -2; // try column index 2 for z-column, if name is equal to XY-columns

            TopLevelString = null;
            BotLevelString = null;
            Extent = null;
            ZoneFilename = null;
            ZoneValues = null;
            SelectPointMethod = SelectPointMethod.Inside;

            ExpColReference = null;
            ExpOperator = ValueOperator.Undefined;
            ExpValue = null;
            UseRegExp = false;

            ColumnExpressionDefs = null;

            Postfix = null;
            IsMetadataAdded = false;

            IsTSSkipped = false;
            TSPeriodStartDate = null;
            TSPeriodEndDate = null;
            IsTSPeriodSelected = true;
            IsTSPeriodClipped = false;
            IsEmptyTSPointRemoved = false;
            TSPeriodValueColIndex = -1;
            TSValueColIndices = new List<int>();
            TSValueColFactors = new List<float>();
            IsTSNoDataRemoved = false;
            TSNoDataValColIdx = -1;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IPF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("outPath", "Path or IPF-filename to write results", "C:\\Test\\Output");
            AddToolOptionDescription("c", "Change columnvalues of selected points\n" +
                                          "one or more column/exp-definitions can be specified, seperated by commas;\n" +
                                          "each column/exp-definition is specified by 'c1;c2;c3', where:\n" +
                                          "  'c1' is a (one-based) column index or a column name. If a column name is not found \n" +
                                          "    it is added, where non - selected points will receive an empty string as a value. \n" +
                                          "  'c2' is a constant value, a mathetical expression or a string expression:\n" +
                                          "    a mathemetical expression is defined as an operator and value \n" +
                                          "      valid operators are: '*', '/', '+' and '-'. E.g. \"/c:3;*2.5,TOP;-1\" \n" +
                                          "      for the value a floating point value or a columnname can be specified. \n" +
                                          "        a columnname must be surrounded with curly braces, e.g. '-{TOP}' \n" +
                                          "      for stringvalues only operators '+' and '-' are valid (to concatenate or remove a substring). \n" + 
                                          "    a string expression is a combination of (one-based) column numbers/names and/or \n" +
                                          "    constant values/strings; column numbers/names must be surrounded by {}-brackets and \n" +
                                          "      {<col>}-substrings are replaced with the corresponding column values in that row; \n" +
                                          "      use[ID](including brackets) for the (one-based) rownumber of the processed row. \n" +
                                          "      for string manipulation two forms are allowed:\n" +
                                          "      - {<col>:~idx,len} to select substring at index idx with length len (see batchfile-syntax)\n" +
                                          "      - {<col>:A=B} to replace substrings A by B\n" +
                                          "      Note: string expression results are trimmed after applying expressions.\n" +
                                          "  'c3' is an optional NoData-value for new columns and rows that were not selected.",
                                          "/c:3;*2.5", "Changes are made to column with expressions: {...}", new string[] { "c1" }, new string[] { "..." });
            AddToolOptionDescription("l", "Selection volume below specified value/IDFFile l1 or \n" +
                                          "between top/bot-levels by values/IDFFile l1 and l2",
                                          "/l:3,5", "Analysis volume levels defined: {0},{1}", new string[] { "l1" }, new string[] { "l2" });
            AddToolOptionDescription("m", "Add metadata and including existing metadata from source file", "/m", "Metadata is added");
            AddToolOptionDescription("p", "Add postfix p1 to result filename", "/p:_sel", "Postfix for result filenames: {0}", new string[] { "p1" });
            AddToolOptionDescription("r", "Use regular expressions for strings values and (un)equal operator \n" +
                                          "expressions are embedded between Regex-symbols (^$) to get an exact match",
                                          "/r", "Regular expressions are used for string values and operators");
            AddToolOptionDescription("v", "Specify volume/area method, v1: 1=inside (default); 2=outside specified volume/area",
                                          "/v:1", "Volume/area method is: {0}", new string[] { "v1" });
            AddToolOptionDescription("x", "Select expression on values of column with (one based) column number or name x1; with logical\n" +
                                          "operator x2 against specified value x3 (constant or string expression as defined for option c).\n" + 
                                          "Supported logical operators: eq: equal; gt: greater than; gteq: greater than or equal;\n" + 
                                          "                             lt: lower than; lteq: lower than or equal; uneq: unequal\n" +
                                          "NoData- or string-values are valid for (un)equality, otherwise result in false",
                                          "/x:3,eq,5.5", "Condition for selection: {0} {1} {2}", new string[] { "x1", "x2", "x3" });
            AddToolOptionDescription("y", "Specify (one based) columnindices for x-, y- and (optionally) z-coordinates",
                                          "/y:1,2,4", "x, y (and z) columnindices are redefined to: {0}, {1}, {2}", new string[] { "y1", "y2" }, new string[] { "y3" }, new string[] { "NA" });
            AddToolOptionDescription("z", "Selection volume within zone(s) in IDF/GEN-file z1\n" +
                                          "For IDF-file also specify (semicolon-seperated) zone-values z2", "/p:somecountour.GEN",
                                          "Analysis volume zone defined within: {...}", new string[] { "z1" }, new string[] { "z2" }, new string[] { "NA" });
            AddToolOptionDescription("tsc", "Select value columns in associated files. Specify comma-seperated list of (one-based) numbers.\n" +
                                             "First value column has numnber 1. When option c1 is not used, all value columns are selected.\n" +
                                             "Optionally for each column, after a semicolon, specify a factor f1 to multiply all column values with.",
                                             "/tsc:1;0.1,3;100", "Selected timeseries value column numbers (and optional factor): {...}", new string[] { "v1" }, new string[] { "..." });
            AddToolOptionDescription("tse", "Remove IPF-points without timeseries or with empty timeseries (without any values)",
                                            "/tse", "IPF-points with empty timeseries are removed");
            AddToolOptionDescription("tsn", "Remove timestamps from timeseries with NoData-value in value column with (one-based) column number v\n" +
                                             "Use 0 to only remove timestamp if all row values have a NoData-value; check is done before other selections",
                                            "/tsn:1", "Timestamps with NoData-values in value column number {0} are removed", new string[] { "v" });
            AddToolOptionDescription("tsp", "Select points that have values in timeseries within specified period p1 - p2\n" +
                                            "Use format yyyymmdd[hhmmss] for p1 and p2. With p3 selection/clipping can be defined:\n" +
                                            "p3 = 0: remove points without dates in specified period; do not clip timeseries (default)\n" +
                                            "p3 = 1: remove points without dates in specified period; clip timeseries\n" +
                                            "p3 = 2: do not remove points without dates in specified period; clip timeseries\n" +
                                            "Optionally, specify (one-based) number p4 of value column to check for values.\n" +
                                            "Use p4=0 (default) if all value columns should contain values for points to be selected.",
                                            "/tsp:20070101,20201231", "Period for timeseries selection: {0}-{1}, with options (clip/NoData-columnnr): {...}", new string[] { "p1", "p2" }, new string[] { "p3", "p4" }, new string[] { "0", "0" } );
            AddToolOptionDescription("tss", "Skip reading/writing IPF-timeseries (and keep non-existing timeseries references in input file).",
                                            "/tss", "IPF-timeseries are not read/written");
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

        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            return base.FormatLogStringParameter(optionName, parameter, parameterValue, parameterValues);
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
                    string tmpOptionParametersString = FixSeperators(optionParametersString);
                    string[] tmpOptionParameterStrings = GetOptionParameters(tmpOptionParametersString);
                    string[] optionParameters = FixSeperators(tmpOptionParameterStrings);
                    // string[] optionParameters = CommonUtils.SplitQuoted2(tmpOptionParametersString, ',', '{', '}');
                    ColumnExpressionDefs = new List<ColumnExpressionDef>();
                    for (int defIdx = 0; defIdx < optionParameters.Length; defIdx++)
                    {
                        string[] defValues = CommonUtils.SplitQuoted(optionParameters[defIdx], ';', '{', '}');
                        defValues = FixSeperators(defValues);
                        if (defValues.Length == 2)
                        {
                            ColumnExpressionDefs.Add(new ColumnExpressionDef(defValues[0], UnFixSeperators(defValues[1])));
                        }
                        else if (defValues.Length == 3)
                        {
                            ColumnExpressionDefs.Add(new ColumnExpressionDef(defValues[0], UnFixSeperators(defValues[1]), defValues[2]));
                        }
                        else
                        {
                            throw new ToolException("Invalid column/value-definition at definition index " + (defIdx + 1) + " for option '" + optionName + "': " + CommonUtils.ToString(optionParameters.ToList()));
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    try
                    {
                        Extent = Extent.ParseExtent(optionParametersString);
                    }
                    catch
                    {
                        throw new ToolException("Could not parse extent: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("l"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    TopLevelString = optionParameters[0];
                    if (optionParameters.Length > 1)
                    {
                        BotLevelString = optionParameters[1];
                    }
                    else if (optionParameters.Length > 2)
                    {
                        throw new ToolException("For option 'l' not more than two parameters are allowed: " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("m"))
            {
                IsMetadataAdded = true;
            }
            else if (optionName.ToLower().Equals("p"))
            {
                if (hasOptionParameters)
                {
                    Postfix = optionParametersString;
                }
                else
                {
                    throw new ToolException("Postfix string expected for option '" + optionName + "' : " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("r"))
            {
                UseRegExp = true;
            }
            else if (optionName.ToLower().Equals("v"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    int s1Value;
                    if (int.TryParse(optionParameters[0], out s1Value))
                    {
                        switch (s1Value)
                        {
                            case 1:
                                SelectPointMethod = SelectPointMethod.Inside;
                                break;
                            case 2:
                                SelectPointMethod = SelectPointMethod.Outside;
                                break;
                            default:
                                throw new ToolException("Invalid parameter value for option v, parameter v1: " + optionParameters);
                        }
                    }
                    else
                    {
                        throw new ToolException("Parameter for option 'v' should be an integer value 1 or 2: " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("x"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 3)
                    {
                        ExpColReference = optionParameters[0];

                        try
                        {
                            ExpOperator = ValueOperatorUtils.ParseString(optionParameters[1]);
                        }
                        catch
                        {
                            throw new ToolException("Parameter x2 for option x is not a valid operator:" + optionParameters[1]);
                        }
                        ExpValue = optionParameters[2];
                    }
                    else
                    {
                        throw new ToolException("Please specify x1, x2, x3 parameters for option 'x':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("y"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if ((optionParameters.Length != 2) && (optionParameters.Length != 3))
                    {
                        throw new ToolException("Please specify x,y (and optionally z) column indices after 'y:':" + optionParameters);
                    }
                    else
                    {
                        try
                        {
                            // Parse substrings for this option
                            XColIdx = int.Parse(optionParameters[0]) - 1;
                            YColIdx = int.Parse(optionParameters[1]) - 1;
                            if (optionParameters.Length == 3)
                            {
                                ZColIdx = int.Parse(optionParameters[2]) - 1;
                            }
                        }
                        catch (Exception)
                        {
                            throw new ToolException("xyz-values should be integers for option 'y': " + optionParametersString);
                        }
                    }
                }
            }
            else if (optionName.ToLower().Equals("z"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    ZoneFilename = optionParameters[0];
                    if (optionParameters.Length == 2)
                    {
                        ZoneValues = new List<float>();
                        foreach (string zonevalueString in optionParameters[1].Split(new char[] { ';' }))
                        {
                            if (!float.TryParse(zonevalueString, NumberStyles.Float, EnglishCultureInfo, out float zoneValue))
                            {
                                throw new ToolException("Invalid zone value, float value(s) expected for option '" + optionName + "': " + optionParametersString);
                            }
                            ZoneValues.Add(zoneValue);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tse"))
            {
                IsEmptyTSPointRemoved = true;
            }
            else if (optionName.ToLower().Equals("tss"))
            {
                IsTSSkipped = true;
            }
            else if (optionName.ToLower().Equals("tsp"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length > 0)
                    {
                        try
                        {
                            // force date time strings into dutch culture format (although this doesn't seem to be necessary if dd-MM-yyyy format is used)
                            string startDateString = optionParameters[0].Trim();
                            if (startDateString.Length == 8)
                            {
                                // No time is present
                                startDateString = startDateString.Substring(6, 2) + "-" + startDateString.Substring(4, 2) + "-" + startDateString.Substring(0, 4);
                                TSPeriodStartDate = optionParameters[0].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(startDateString, DutchCultureInfo);
                            }
                            else
                            {
                                // Assume time is present
                                startDateString = startDateString.Substring(6, 2) + "-" + startDateString.Substring(4, 2) + "-" + startDateString.Substring(0, 4)
                                    + " " + startDateString.Substring(8, 2) + ":" + startDateString.Substring(10, 2) + ":" + startDateString.Substring(12, 2);
                                TSPeriodStartDate = optionParameters[0].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(startDateString, DutchCultureInfo);
                            }
                            if (optionParameters.Length >= 2)
                            {
                                string endDateString = optionParameters[1].Trim();
                                if (endDateString.Length == 8)
                                {
                                    // No time is present
                                    endDateString = endDateString.Substring(6, 2) + "-" + endDateString.Substring(4, 2) + "-" + endDateString.Substring(0, 4);
                                    TSPeriodEndDate= optionParameters[0].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(endDateString, DutchCultureInfo);
                                }
                                else
                                {
                                    // Assume time is present
                                    endDateString = endDateString.Substring(6, 2) + "-" + endDateString.Substring(4, 2) + "-" + endDateString.Substring(0, 4)
                                        + " " + endDateString.Substring(8, 2) + ":" + endDateString.Substring(10, 2) + ":" + endDateString.Substring(12, 2);
                                    TSPeriodEndDate = optionParameters[0].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(endDateString, DutchCultureInfo);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new ToolException("Could not parse dates for option 'tsp':" + optionParametersString, ex);
                        }
                        if (optionParameters.Length >= 3)
                        {
                            switch (optionParameters[2].Trim())
                            {
                                case "0":
                                    IsTSPeriodSelected = true;
                                    IsTSPeriodClipped = false;
                                    break;
                                case "1":
                                    IsTSPeriodSelected = true;
                                    IsTSPeriodClipped = true;
                                    break;
                                case "2":
                                    IsTSPeriodSelected = false;
                                    IsTSPeriodClipped = true;
                                    break;
                                default:
                                    throw new ToolException("Undefined value for tsp3 of option '/tsp'");
                            }
                        }
                        if (optionParameters.Length >= 4)
                        {
                            if (!int.TryParse(optionParameters[3].Trim(), out int valueColNr))
                            {
                                throw new ToolException("Value column number could not be parsed as integer for option 'tsp': " + optionParameters[3]);
                            }
                            TSPeriodValueColIndex = (valueColNr - 1);
                        }
                    }
                    else
                    {
                        throw new ToolException("Please specify start- and enddate after 'tsp': " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsc"))
            {
                TSValueColIndices.Clear();
                TSValueColFactors.Clear();
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    for (int idx = 0; idx < optionParameters.Length; idx++)
                    {
                        string colfctPairString = optionParameters[idx];
                        string[] colfctStrings = colfctPairString.Split(new char[] { ';' });

                        if (!int.TryParse(colfctStrings[0], out int colnr))
                        {
                            throw new ToolException("Could not parse column number v1 for option '" + optionName + "':" + optionParameters[0]);
                        }
                        TSValueColIndices.Add(colnr - 1);

                        if (colfctStrings.Length > 1)
                        {
                            if (!float.TryParse(colfctStrings[1], NumberStyles.Float, EnglishCultureInfo, out float factor))
                            {
                                throw new ToolException("Could not parse factor f1 for option '" + optionName + "':" + optionParameters[0]);
                            }
                            TSValueColFactors.Add(factor);
                        }
                        else
                        {
                            TSValueColFactors.Add(1f);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsn"))
            {
                IsTSNoDataRemoved = true;
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int colNr))
                    {
                        throw new ToolException("Invalid value column number specified for option '" + optionName + "'");
                    }
                    TSNoDataValColIdx = (colNr - 1);
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
                InputFilter = "*.IPF";
            }

            if ((OutputFilename != null) && !Path.GetExtension(OutputFilename).ToLower().Equals(".ipf"))
            {
                throw new ToolException("When an output filename is specified it should have extension .IPF:" + OutputFilename);
            }

            // Create output path if not yet existing
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            if (TopLevelString != null)
            {
                if (!IsValidLevelString(TopLevelString))
                {
                    throw new ToolException("Level1 is not a floating point or existing IDF-file: " + TopLevelString);
                }
            }
            if (BotLevelString != null)
            {
                if (!IsValidLevelString(BotLevelString))
                {
                    throw new ToolException("Level2 is not a floating point or existing IDF-file: " + BotLevelString);
                }
            }
            if (ZoneFilename != null)
            {
                if (!File.Exists(ZoneFilename))
                {
                    throw new ToolException("Specified GEN-file does not exist: " + ZoneFilename);
                }
            }
            if ((XColIdx < 0) || (YColIdx < 0) || (ZColIdx < -2))
            {
                throw new ToolException("xyz-column indices should be positive numbers: " + XColIdx.ToString() + "," + YColIdx.ToString() + "," + ZColIdx.ToString());
            }

            if (IsTSNoDataRemoved && ((TSNoDataValColIdx + 1) < 0))
            {
                throw new ToolException("Invalid (negative) value column number for tsn-option: " + (TSNoDataValColIdx + 1));
            }

            if ((TSPeriodStartDate != null) && ((TSPeriodValueColIndex  + 1) < 0))
            {
                throw new ToolException("Invalid (negative) value column number for tsp-option: " + (TSPeriodValueColIndex + 1));
            }
        }

        /// <summary>
        /// Combine seperate '{' and '}'-parts between that were incorrectly split 
        /// </summary>
        /// <param name="defValues"></param>
        /// <returns></returns>
        protected string[] FixSeperators(string[] defValues)
        {
            List<string> corrDefValues = new List<string>();

            string prevDefValue = null;
            for (int idx = 0; idx < defValues.Length; idx++)
            {
                string defValue = defValues[idx];

                if (defValue.Contains('{') && !defValue.Contains('}'))
                {
                    prevDefValue = defValue;
                }
                else if ((defValue.Contains('}')) && (prevDefValue != null))
                {
                    corrDefValues.Add(prevDefValue + "," + defValue);
                    prevDefValue = null;
                }
                else
                {
                    if (prevDefValue != null)
                    {
                        corrDefValues.Add(prevDefValue);
                    }
                    corrDefValues.Add(defValue);
                }
            }

            return corrDefValues.ToArray();
        }

        /// <summary>
        /// Temporarily replace '[,]' and '[;]' to prevent splitting on ',' and ';'
        /// </summary>
        /// <param name="parString"></param>
        /// <returns></returns>
        protected string FixSeperators(string parString)
        {
            return parString.Replace("[,]", "[###COMMA###]").Replace("[;]", "[###SEMICOLON###]").Replace("[,]", "[###COMMA###]").Replace("[;]", "[###SEMICOLON###]");
        }

        /// <summary>
        /// Place back '[,]' and '[;]' after splitting on ',' and ';'
        /// </summary>
        /// <param name="expString"></param>
        /// <returns></returns>
        protected string UnFixSeperators(string expString)
        {
            return expString.Replace("[###COMMA###]", "[,]").Replace("[###SEMICOLON###]", "[;]");
        }

        protected bool IsValidLevelString(string levelString)
        {
            float value;
            if (!float.TryParse(levelString, NumberStyles.Float, EnglishCultureInfo, out value))
            {
                if (!File.Exists(levelString))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class ColumnExpressionDef
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// Name of column index (one based)
        /// </summary>
        public string ColumnDefinition;
        public string ExpressionString;
        public OperatorEnum ExpOperator;

        /// <summary>
        /// Either a valid, numeric value or NaN, which indicates that the expression value is textual
        /// </summary>
        public double ExpDoubleValue;

        /// <summary>
        /// The string represention of specified expression value, which be textual or numeric
        /// </summary>
        public string ExpStringValue;   
        public string NoDataString;

        /// <summary>
        /// Define Column Expression
        /// </summary>
        /// <param name="columnDefinition">name or column index (one based)</param>
        /// <param name="expression"></param>
        /// <param name="noDataString"></param>
        public ColumnExpressionDef(string columnDefinition, string expression, string noDataString = "")
        {
            this.ColumnDefinition = columnDefinition;
            this.ExpressionString = expression;
            this.NoDataString = noDataString;
            ParseOperator();
            ParseValue();
        }

        private void ParseValue()
        {
            string strValue = ExpressionString.Trim();
            if (ExpOperator != OperatorEnum.None)
            {
                strValue = strValue.Substring(1);
            }
            double dblValue;
            if (!double.TryParse(strValue, NumberStyles.Float, englishCultureInfo, out dblValue))
            {
                // throw new ToolException("Invalid value in column/value-expression: " + expressionString); 
                dblValue = double.NaN;
            }
            ExpDoubleValue = dblValue;
            ExpStringValue = strValue;

            if ((ExpOperator == OperatorEnum.Divide) && dblValue.Equals(0d))
            {
                throw new ToolException("Invalid expression: division by zero is not allowed");
            }
        }

        private void ParseOperator()
        {
            if (ExpressionString.Length > 0)
            {
                switch (ExpressionString[0])
                {
                    case '*':
                        ExpOperator = OperatorEnum.Multiply;
                        break;
                    case '/':
                        ExpOperator = OperatorEnum.Divide;
                        break;
                    case '+':
                        ExpOperator = OperatorEnum.Add;
                        break;
                    case '-':
                        ExpOperator = OperatorEnum.Substract;
                        break;
                    default:
                        ExpOperator = OperatorEnum.None;
                        break;
                }
            }
            else
            {
                ExpOperator = OperatorEnum.None;
            }
        }
    }
}
