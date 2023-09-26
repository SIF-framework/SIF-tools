// GENselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENselect.
// 
// GENselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENselect. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.GENselect
{
    public enum SizeOperator
    {
        Undefined,
        Equal,
        Min,
        Max,
        GreaterThan,
        GreaterThanOrEqual,
        LesserThan,
        LesserThanOrEqual,
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

        public SizeOperator SizeOperator { get; set; }
        public float SizeValue { get; set; }

        public List<ColumnExpressionDef> ColumnExpressionDefs { get; set; }

        public string ExpColReference { get; set; }
        public ValueOperator ExpOperator { get; set; }
        public string ExpValue { get; set; }

        public bool UseRegExp { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            SizeOperator = SizeOperator.Undefined;
            SizeValue = float.NaN;

            ColumnExpressionDefs = null;

            ExpColReference = null;
            ExpOperator = ValueOperator.Undefined;
            ExpValue = null;

            UseRegExp = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.XXX)", "*.XXX");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Use regular expressions for strings values and (un)equal operator \n" +
                                          "expressions are embedded between Regex-symbols (^$) to get an exact match",
                                          "/r", "Regular expressions are used for string values and operators");
            AddToolOptionDescription("s", "Specify selection criterium (operator and value) based on size of feature (length for line, area for polygon)\n" + 
                                          "Depending on the operator value v1 must be defined. Size operator op can be one of the following:\n" +
                                          "'min'  - retrieves feature with minimum size; v1 is ignored\n" +
                                          "'min'  - retrieves feature with maximum size; v1 is ignored\n" +
                                          "'eq'   - retrieves features with size equal to value v1\n" +
                                          "'gt'   - retrieves features with size greater than value v1\n" +
                                          "'gteq' - retrieves features with size greater than or equal to value v1\n" +
                                          "'lt'   - retrieves features with size lesser than value v1\n" +
                                          "'lteq' - retrieves features with size lesser than or equal to value v1",
                                          "/s:gt,10000", "Selection criterium: {0} {1}", new string[] { "op" }, new string[] { "v1" }, new string[] { string.Empty } );
            AddToolOptionDescription("x", "Specify select expression on values of column with (one based) column number or name c and\n" +
                                          "operator o against specified value v. Supported logical operators:\n" +
                                          "'eq' - tests if column value = value v\n" +
                                          "'gt' - tests if column value > value v\n" +
                                          "'gteq' - tests if column value >= to value v\n" +
                                          "'lt' - tests if column value < value v\n" +
                                          "'lteq' - tests if column value <= value v\n" +
                                          "'uneq' - tests if column value <> value v (unequality)\n" +
                                          "NoData- or string-values are valid for (un)equality, otherwise result in false", "/x:PEIL,gt,1.5", "Expression for selection: {0} {1} {2}", new string[] { "c", "o", "v" });
            AddToolOptionDescription("c", "Change columnvalues of selected points\n" +
                                          "one or more column/exp-definitions can be specified, seperated by commas.;\n" +
                                          "each column/exp-definition is specified by 'c1;c2;c3', where:\n" +
                                          "  'c1' is a (one-based) column index or a column name. If a column name is not found \n" +
                                          "    it is added, where non - selected points will receive an empty string as a value. \n" +
                                          "  'c2' is a constant value, a mathetical expression or a string expression. \n" +
                                          "    a mathemetical expression is defined as an operator and value \n" +
                                          "      valid operators are: '*', '/', '+' and '-'. E.g. \"/c:3;*2.5,TOP;-1\" \n" +
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
        }

        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            switch (optionName)
            {
                case "x":
                    switch (parameter)
                    {
                        case "c":
                            return "column(" + parameterValue + ")";
                        default:
                            return parameterValue;
                    }
                default:
                    return parameterValue;
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
                UseRegExp = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    try
                    {
                        // Parse substrings for this option
                        if (optionParameters.Length >= 1)
                        {
                            switch (optionParameters[0].ToLower())
                            {
                                case "min":
                                    SizeOperator = SizeOperator.Min;
                                    break;
                                case "max":
                                    SizeOperator = SizeOperator.Max;
                                    break;
                                case "eq":
                                    SizeOperator = SizeOperator.Equal;
                                    break;
                                case "gt":
                                    SizeOperator = SizeOperator.GreaterThan;
                                    break;
                                case "gteq":
                                    SizeOperator = SizeOperator.GreaterThanOrEqual;
                                    break;
                                case "lt":
                                    SizeOperator = SizeOperator.LesserThan;
                                    break;
                                case "lteq":
                                    SizeOperator = SizeOperator.LesserThanOrEqual ;
                                    break;
                                default:
                                    throw new ToolException("Unknown size operator: " + optionParameters[0]);
                            }
                        }
                        if (optionParameters.Length >= 2)
                        {
                            if (!float.TryParse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                            {
                                throw new ToolException("Invalid value for option '" + optionName + "': " + optionParameters[0]);
                            }
                            SizeValue = value;
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter operator and optional value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
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
                        string[] defValues = optionParameters[defIdx].Split(new char[] { ';' });
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

            if ((OutputFilename != null) && !Path.GetExtension(OutputFilename).ToLower().Equals(".gen"))
            {
                throw new ToolException("When an output filename is specified it should have extension .GEN:" + OutputFilename);
            }

            // Create output path if not yet existing
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            if (SizeOperator != SizeOperator.Undefined)
            {
                switch (SizeOperator)
                {
                    case SizeOperator.Min:
                    case SizeOperator.Max:
                        if (!SizeValue.Equals(float.NaN))
                        {
                            throw new ToolException("No value is expeced for size operators min, max");
                        }
                        break;
                    case SizeOperator.Equal:
                    case SizeOperator.GreaterThan:
                    case SizeOperator.GreaterThanOrEqual:
                    case SizeOperator.LesserThan:
                    case SizeOperator.LesserThanOrEqual:
                        if (SizeValue.Equals(float.NaN))
                        {
                            throw new ToolException("A value is expeced for size operators gt,gteq,lt,lteq,eq");
                        }
                        break;
                    default:
                        throw new Exception("Undefined size operator: " + SizeOperator);
                }
            }
        }

        /// <summary>
        /// Combine seperate '{' and '}'-parts between that were incorrectly split 
        /// </summary>
        /// <param name="defValues"></param>
        /// <returns></returns>
        private string[] FixSeperators(string[] defValues)
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
        private string FixSeperators(string parString)
        {
            return parString.Replace("[,]", "[###COMMA###]").Replace("[;]", "[###SEMICOLON###]").Replace("[,]", "[###COMMA###]").Replace("[;]", "[###SEMICOLON###]");
        }

        /// <summary>
        /// Place back '[,]' and '[;]' after splitting on ',' and ';'
        /// </summary>
        /// <param name="expString"></param>
        /// <returns></returns>
        private string UnFixSeperators(string expString)
        {
            return expString.Replace("[###COMMA###]", "[,]").Replace("[###SEMICOLON###]", "[;]");
        }
    }

    public enum OperatorEnum
    {
        None,
        Multiply,
        Divide,
        Add,
        Substract
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
        public double ExpDoubleValue;
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
