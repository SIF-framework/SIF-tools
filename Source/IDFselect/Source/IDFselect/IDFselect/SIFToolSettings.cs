// IDFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFselect.
// 
// IDFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFselect. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Values;
using Sweco.SIF.iMODPlus;

namespace Sweco.SIF.IDFselect
{
    public enum LogicalOperator
    {
        None,
        AND,
        OR
    }

    /// <summary>
    /// Unit for checks that use a dimension, distance, size or length, which can be either cells or meters
    /// </summary>
    public enum Unit
    {
        Cells,
        Meters
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const int CONNECTIONOPTION_NONE = -1;

        /// <summary>
        /// Select all connected cells
        /// </summary>
        public const int CONNECTIONOPTION_ALL = 0;

        /// <summary>
        /// Only connected cells with equal non-NoData values are selected
        /// </summary>
        public const int CONNECTIONOPTION_BYVALUE = 1;

        /// <summary>
        /// Diagonally connected cells are also selected
        /// </summary>
        public const int CONNECTIONOPTION_DIAGONAL = 2;

        /// <summary>
        /// Values from selection IDF-file are used for connection ID-values instead of input IDF-file
        /// </summary>
        public const int CONNECTIONOPTION_SELVALUE = 4;

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }
        public bool IsRecursive { get; set; }
        public string SelectionString { get; set; }
        public List<float> ExcludedValues { get; set; }
        public ValueOperator ConditionalOperator { get; set; }
        public string ConditionalIDFString { get; set; }
        public int ConnectionFlags { get; set; }
        public int MinConnectedWidth { get; set; }
        public int MinConnectedHeight{ get; set; }
        public Unit MinConnectedUnit { get; set; }
        public bool SkipEmptyResult { get; set; }

        /// <summary>
        /// An integer number (which is stored as a negative value) that defines minimum number of overlapping, connected cells with selection IDF-file, for selection of connected cells
        /// </summary>
        public int MinConnectedOverlapCount { get; set; }

        /// <summary>
        /// A fraction (floating point value between 0 and 1) that defines fraction of of overlapping, connected cells with selection IDF-file, for selection of connected cells
        /// </summary>
        public float MinConnectedOverlapFraction { get; set; }

        /// <summary>
        /// A logical operator to define the combination of a minimum connection count and/or fraction
        /// </summary>
        public LogicalOperator MinConnectedOverlapOperator { get; set; }

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
            IsRecursive = false;
            SelectionString = null;
            ConnectionFlags = CONNECTIONOPTION_NONE;
            ExcludedValues = null;
            ConditionalOperator = ValueOperator.Undefined;
            ConditionalIDFString = null;
            SkipEmptyResult = false;

            MinConnectedOverlapCount = -1;
            MinConnectedOverlapFraction = -1;
            MinConnectedOverlapOperator = LogicalOperator.None;

            MinConnectedWidth = 0;
            MinConnectedHeight = 0;
            MinConnectedUnit = Unit.Cells;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is search resursively for input files");
            AddToolOptionDescription("x", "Exclude specified values xi from selection", "/x:1,2", "Excluded values: {0}", new string[] { "x1" }, new string[] { "..." });
            AddToolOptionDescription("s", "Skip empty result IDF-files", "/s", "Empty result files are skipped");
            AddToolOptionDescription("z", "Define selection zones, defined by all non-NoData-cells in specified zone IDF-file z1 (or constant value)", "/z1:Zones.IDF", "Zone IDF-file is specified: {0}", new string[] { "s1" });
            AddToolOptionDescription("c", "Select cells that are connected in input IDF-file and have overlap with any of the specified zones from\n" + 
                                          "option z or, when option z is not used: all non-NoData-cells in input IDF-file.\n" + 
                                          "Connectivity is defined by parameter f, a combination/sum of the following flags:\n" +
                                          "  0 - select all connected non-NoData cells that have overlap with specified zone(s) regardless of value\n" +
                                          "  1 - only select connected cells with equal non-NoData values in input IDF-file\n" +
                                          "  2 - also select diagonally connected cells\n" +
                                          "  4 - use values from selection IDF-file for result instead of values from input IDF-file\n" +
                                          "Optionally specify conditions for selection via optional parameters o (min. overlap) and d (min. dimension):\n" + 
                                          " - o defines minimum number of overlapping cells and/or percentage of overlapping out of all connected cells\n" +
                                          "   To define both, seperate by either 'OR' or 'AND' to define logical combination of both (e.g. 50OR20%).\n" +
                                          " - d defines minimal dimensions WxH of a rectangle that should be contained by the connected cells, where\n" +
                                          "   W is width and H height of rectangle, unit is cells (default) or meters (add m), e.g. '4x4' or '4mx4m'",
                                          "/c:7,50OR20%,3x3", "Selecting connected cells with parameter flag(s) '{0}', minimum overlap '{1}' and minimum inner dimension '{2}'", new string[] { "f" }, new string[] { "o", "s" }, new string[] { "0%", "1x1" });
            AddToolOptionDescription("v", "select cells for which value expression 'currIDF op val', evaluates to true, where\n" +
                                          "currIDF is the current input IDF-file or current result IDF-file (when another option is used)\n" + 
                                          "val is a filename for an existing IDF-file or a constant value and the operator op is one of:\n" +
                                          "  EQU - equal\n" +
                                          "  NEQ - not equal\n" +
                                          "  LSS - less than\n" +
                                          "  LEQ - less than or equal\n" +
                                          "  GTR - greater than\n" +
                                          "  GEQ - greather than or equal", null, "Value expression for selected cells: {0} {1}", new string[] { "op", "idf" });
            AddToolUsageFinalRemark("Notes:\n" +
                " - options are executed in following order: x, c, s, l\n" + 
                " - without any option all cells are selected and the input IDF-files are just copied");
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
            else if (optionName.ToLower().Equals("s"))
            {
                SkipEmptyResult = true;
            }
            else if (optionName.ToLower().Equals("z"))
            {
                if (hasOptionParameters)
                {
                    SelectionString = optionParametersString;
                    if (!SelectionString.ToUpper().EndsWith(".IDF") && !float.TryParse(SelectionString, NumberStyles.Float, EnglishCultureInfo, out float tmpValue))
                    {
                        throw new ToolException("Invalid zone value (IDF-file or floating point value) for option '" + optionName + "': " + SelectionString);
                    }
                }
                else
                {
                    throw new ToolException("Zone parameter expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameterStrings = GetOptionParameters(optionParametersString);
                    if (!int.TryParse(optionParameterStrings[0], out int parValue))
                    {
                        throw new ToolException("Could not parse integer value for option 'c': " + optionParametersString);
                    }
                    ConnectionFlags = parValue;
                    if (ConnectionFlags > (CONNECTIONOPTION_BYVALUE | CONNECTIONOPTION_DIAGONAL | CONNECTIONOPTION_SELVALUE))
                    {
                        throw new ToolException("Undefined value for option 'c': " + optionParametersString);
                    }

                    if (optionParameterStrings.Length > 1)
                    {
                        string minConnectionString = optionParameterStrings[1];
                        string[] minConnectionStrings = null;
                        if (minConnectionString.ToUpper().Contains("OR"))
                        {
                            minConnectionStrings = minConnectionString.ToUpper().Split(new string[] { "OR" }, StringSplitOptions.None);
                            MinConnectedOverlapOperator = LogicalOperator.OR;
                        }
                        else if (minConnectionString.ToUpper().Contains("AND"))
                        {
                            minConnectionStrings = minConnectionString.ToUpper().Split(new string[] { "AND" }, StringSplitOptions.None);
                            MinConnectedOverlapOperator = LogicalOperator.AND;
                        }
                        else
                        {
                            minConnectionStrings = new string[] { minConnectionString };
                            MinConnectedOverlapOperator = LogicalOperator.None;
                        }

                        if (minConnectionStrings.Length > 2)
                        {
                            throw new ToolException("Max two min connection values expected: " + optionParametersString);
                        }

                        foreach (string minConnectionSubString in minConnectionStrings)
                        {
                            if (minConnectionSubString.Contains("%"))
                            {
                                if (!float.TryParse(minConnectionSubString.Replace("%", string.Empty), NumberStyles.Float, EnglishCultureInfo, out float percentage))
                                {
                                    throw new ToolException("Could not parse minimum percentage for option 'c': " + optionParametersString);
                                }
                                if ((percentage < 0) || (percentage > 100))
                                {
                                    throw new ToolException("Minimum percentage should lie betweeen 0 and 100% for option 'c': " + optionParametersString);
                                }
                                MinConnectedOverlapFraction = percentage / 100f;
                            }
                            else
                            {
                                if (!int.TryParse(minConnectionSubString, out int number))
                                {
                                    throw new ToolException("Could not parse minimum number for option 'c': " + optionParametersString);
                                }
                                MinConnectedOverlapCount = number;
                            }
                        }
                    }

                    if (optionParameterStrings.Length > 2)
                    {
                        string minDimensionString = optionParameterStrings[2];
                        if (!minDimensionString.Contains("x"))
                        {
                            throw new ToolException("Missing 'x' in dimension string: " + minDimensionString);
                        }

                        minDimensionString = minDimensionString.ToLower();
                        string[] dimensions = minDimensionString.Split(new char[] { 'x' });
                        if (dimensions.Length != 2)
                        {
                            throw new ToolException("Invalid dimension, expected WxH: " + minDimensionString);
                        }

                        // Check for optional unit
                        int mUnitCount = 0;
                        mUnitCount += dimensions[0].EndsWith("m") ? 1 : 0;
                        mUnitCount += dimensions[1].EndsWith("m") ? 1 : 0;
                        if (mUnitCount == 1)
                        {
                            throw new ToolException("For only one of width/height is unit meters used, ensure units are equal: " + minDimensionString);
                        }
                        else
                        { 
                            MinConnectedUnit = Unit.Meters;
                            dimensions[0] = dimensions[0].Replace("m", string.Empty);
                            dimensions[1] = dimensions[0].Replace("m", string.Empty);
                        }
                        if (!int.TryParse(dimensions[0], out int width))
                        {
                            throw new ToolException("Invalid width in dimensions: " + dimensions[0]);
                        }
                        if (!int.TryParse(dimensions[1], out int height))
                        {
                            throw new ToolException("Invalid height in dimensions: " + dimensions[1]);
                        }
                        MinConnectedWidth = width;
                        MinConnectedHeight = height;
                    }
                }
                else
                {
                    throw new ToolException("Please specify connection type with an integer value after 'c:': " + optionParametersString);
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                if (hasOptionParameters)
                {
                    // retrieve part after colon
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length != 2)
                    {
                        throw new ToolException("Please specify conditional operator after '" + optionName + ":':" + optionParametersString);
                    }
                    else
                    {
                        string operatorString = optionParameters[0];
                        switch (operatorString.ToUpper())
                        {
                            case "EQU":
                                ConditionalOperator = ValueOperator.Equal;
                                break;
                            case "NEQ":
                                ConditionalOperator = ValueOperator.Unequal;
                                break;
                            case "LSS":
                                ConditionalOperator = ValueOperator.LessThan;
                                break;
                            case "LEQ":
                                ConditionalOperator = ValueOperator.LessThanOrEqual;
                                break;
                            case "GTR":
                                ConditionalOperator = ValueOperator.GreaterThan;
                                break;
                            case "GEQ":
                                ConditionalOperator = ValueOperator.GreaterThanOrEqual;
                                break;
                            default:
                                throw new ToolException("Unknown conditional operator: " + operatorString);
                        }

                        // Read operand which is either an IDF-file or a constant value
                        ConditionalIDFString = optionParameters[1];
                        if (!float.TryParse(ConditionalIDFString, NumberStyles.Float, EnglishCultureInfo, out float constantValue))
                        {
                            // operand is not a constant value
                            if (!File.Exists(ConditionalIDFString))
                            {
                                throw new ToolException("Operand of option '" + optionName + "' should be a constant float value or an existing IDF-file: " + ConditionalIDFString);
                            }
                        }
                    }
                }
                else
                {
                    throw new ToolException("Please specify a valid conditional operator after option '" + optionName + ":'");
                }
            }
            else if (optionName.ToLower().Equals("x"))
            {
                if (hasOptionParameters)
                {
                    ExcludedValues = new List<float>();
                    // retrieve part after colon
                    // split part after colon to strings seperated by a comma
                    string[] valueStrings = GetOptionParameters(optionParametersString);
                    if (valueStrings.Length == 0)
                    {
                        throw new ToolException("Please specify parameters after 'x:', or use only 'x' for defaults:" + optionParametersString);
                    }
                    else
                    {
                        for (int valueIdx = 0; valueIdx < valueStrings.Length; valueIdx++)
                        {
                            try
                            {
                                float value = float.Parse(valueStrings[valueIdx], NumberStyles.Float, EnglishCultureInfo);
                                ExcludedValues.Add(value);
                            }
                            catch (Exception)
                            {
                                throw new ToolException("Could not parse value for option 'x':" + valueStrings[valueIdx]);
                            }
                        }
                    }
                }
                else
                {
                    throw new ToolException("Please specify values after 'x'");
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
                InputFilter = "*.IDF";
            }

            // Create output path if not yet existing, check if specified outputpath is a filename
            OutputFilename = null;
            if (Path.HasExtension(OutputPath))
            {
                if (Path.GetExtension(OutputPath).ToLower().Equals(".idf"))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                    OutputPath = ExpandPathArgument(OutputPath);
                    if (!Directory.Exists(OutputPath))
                    {
                        Directory.CreateDirectory(OutputPath);
                    }
                }
                else
                {
                    throw new ToolException("Extension of output filename is not supported: " + OutputPath);
                }
            }
            else
            {
                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }
            }

            // Check tool option values
        }
    }
}
