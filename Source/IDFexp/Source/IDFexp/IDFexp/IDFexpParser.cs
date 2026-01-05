// IDFexp is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFexp.
// 
// IDFexp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFexp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFexp. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Sweco.SIF.IDFexp
{
    /// <summary>
    /// Available expression types for an INI-line
    /// </summary>
    public enum IDFExpressionType
    {
        Undefined,
        Constant,
        File,
        Variable,
        Function,
        IfThenElse,
        Arithmetic,
        Complex,
    }

    /// <summary>
    /// IDFExpParser based on parser developed by Kaplan, see: https://msdn.microsoft.com/nl-nl/magazine/mt573716.aspx. Modified for parsing IDF-expressions.
    /// V. Kaplan, “Split and Merge Algorithm for Parsing Mathematical Expressions,” CVu, 27-2, May 2015, http://bit.ly/1Jb470l
    /// V. Kaplan, “Split and Merge Revisited,” CVu, 27-3, July 2015, http://bit.ly/1UYHmE9
    /// Modified for usage in IDFexp.
    /// </summary>
    public class IDFExpParser
    {
        public const char START_ARG = '(';
        public const char SEP_ARG = ',';
        public const char END_ARG = ')';
        public const char END_LINE = '\n';

        public static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public static string BasePath = null;
        public static string OutputPath = null;
        public static bool IsDebugMode = false;
        public static int DebugDelay = 0;
        public static bool IsIntermediateResultWritten = false;
        public static bool UseNoDataAsValue = false;
        public static float NoDataValue = float.NaN;
        public static Extent Extent = null;
        public static Log Log = new Log();

        protected static long ExpressionCount { get; set; } = 0;

        /// <summary>
        /// Initialize ExpParser class
        /// </summary>
        public static void Initialize()
        {
            RegisterParserFunctions();
        }

        /// <summary>
        /// Register the IDFFile ParserFunction, which can evaluate a filename of IDF-file in an expression
        /// </summary>
        /// <param name="idfFileFunction"></param>
        protected static void RegisterIDFFileFunction(IDFFileFunction idfFileFunction)
        {
            IDFFileFunction.idfFileFunction = idfFileFunction;
        }

        /// <summary>
        /// Register all allowed functions with parser 
        /// </summary>
        protected static void RegisterParserFunctions()
        {
            ParserFunction.RemoveAllFunctions();
            ParserFunction.RegisterFunction(IfThenElseFunction.Name, new IfThenElseFunction());
            ParserFunction.RegisterFunction(MinFunction.Name, new MinFunction());
            ParserFunction.RegisterFunction(MaxFunction.Name, new MaxFunction());
            ParserFunction.RegisterFunction(RoundFunction.Name, new RoundFunction());
            ParserFunction.RegisterFunction(EnlargeFunction.Name, new EnlargeFunction());
            ParserFunction.RegisterFunction(ClipFunction.Name, new ClipFunction());
            ParserFunction.RegisterFunction(ScaleFunction.Name, new ScaleFunction());
            ParserFunction.RegisterFunction(BoundingBoxFunction.Name, new BoundingBoxFunction());
            ParserFunction.RegisterFunction(CellsizeFunction.Name, new CellsizeFunction());
            ParserFunction.RegisterFunction(NDFunction.Name, new NDFunction());
        }

        /// <summary>
        /// Parse IDF-expression. Note: operators (e.g. +, -) are not allowed in file and/or variable names
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <returns></returns>
        public static IDFFile Parse(string expression, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType)
        {
            // Get rid of spaces and check parenthesis
            string cleanedExpression = Preprocess(expression);

            // Simplify expression before and temporarily replace existing variable names, which enables mathematical operators in variable names
            Dictionary<string, IDFExpVariable> tmpVarDictionary;
            string tmpExpressionString;
            PreprocessVars(cleanedExpression, variableDictionary, out tmpExpressionString, out tmpVarDictionary); 

            int from = 0;
            return SplitAndMerge(tmpExpressionString, tmpVarDictionary, out expressionType, ref from, END_LINE);
        }

        protected static void PreprocessVars(string expression, Dictionary<string, IDFExpVariable> variableDictionary, out string tmpExpression, out Dictionary<string, IDFExpVariable> tmpVarDictionary)
        {
            tmpVarDictionary = new Dictionary<string, IDFExpVariable>();
            int index = 1;
            tmpExpression = expression;
            // Loop through current variable names reversely sorted by string length to avoid conflicts with substrings (e.g. )variable XXX05 is recognized as XXX instead of XXX05).
            foreach (string varName in variableDictionary.Keys.OrderByDescending(x => x.Length))
            {
                if (expression.Contains(varName))
                {
                    string tmpKey = "#" + index++;
                    tmpExpression = tmpExpression.Replace(varName, tmpKey);
                    tmpVarDictionary.Add(tmpKey, variableDictionary[varName]);
                }
            }
        }

        /// <summary>
        /// Preprocess expression string: get rid of spaces and check parenthesis
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected static string Preprocess(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("Parsed empty expression (sub)string");
            }

            int parentheses = 0;
            StringBuilder result = new StringBuilder(data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                char ch = data[i];
                switch (ch)
                {
                    case ' ':
                    case '\t':
                    case '\n': continue;
                    case END_ARG:
                        parentheses--;
                        break;
                    case START_ARG:
                        parentheses++;
                        break;
                }
                result.Append(ch);
            }

            if (parentheses != 0)
            {
                throw new ToolException("Uneven number of parenthesis in expression");
            }

            return result.ToString();
        }

        /// <summary>
        /// Write specified IDFFile object to file and include metadata with expression details
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="expressionId">some Id that indentifies the expression</param>
        /// <param name="expression"></param>
        /// <param name="log"></param>
        public static void WriteExpressionResult(IDFFile idfFile, string expressionId, string expression, Log log)
        {
            Metadata metadata = new Metadata("result of intermediate logical expression " + expressionId + ": " + expression);
            metadata.ProcessDescription = "Automatically generated with debug mode of Sweco's IDFexp-tool";
            string debugIDFFilename = Path.Combine((OutputPath == null) ? expressionId : (Path.Combine(OutputPath, "debug")), expressionId + ".IDF");
            if (idfFile is ConstantIDFFile)
            {
                log.AddInfo("Result is a constantvalue: " + ((ConstantIDFFile)idfFile).ConstantValue, 1);
            }
            else
            {
                if (IsDebugMode)
                {
                    idfFile.IsDebugMode = true;
                }
                idfFile.WriteFile(debugIDFFilename, metadata);
            }
        }

        /// <summary>
        /// Parse specified part of an expression string
        /// </summary>
        /// <param name="data">expression string</param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static IDFFile SplitAndMerge(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from, char to = END_LINE)
        {
            if (from >= data.Length || data[from] == to)
            {
                throw new ArgumentException("Invalid expression (sub)string: " + data);
            }

            // The split and merge algorithm (Kaplan, 2015)
            // In the first step, the string containing an expression is split into a list of so-called “cells”.

            // In the first step, the expression is split into tokens that are then converted into cells. All tokens are
            // separated by one of the mathematical expressions or a parenthesis. A token can be either a real
            // number or a string with the name of a function.The ParserFunction class defined later takes care of
            // all of the functions in the string to be parsed, or for parsing a string to a number.It may also call the
            // whole string parsing algorithm, recursively.If there are no functions and no parentheses in the
            // string to parse, there will be no recursion.

            // Each cell consists, primarily, of a number, or a reference to an IDF-file by a IDF-variable or a string and an action that must be applied to it. 
            // For numbers and IDFFile variables, the actions can be all possible operations we can do with IDF-files, for example, +, –, *, / or ^
            // In an expression with an IF-function, in the first parameter numeric comparisons (==, !=, >=, >, <, <=) and boolean operators (&&, ||) are allowed.
            // Each action is related to the token on the left side of it and each action has a specific priority. 
            // The action of the last variable in the list is defined as as ). It could’ve been any character, but ) is chosen to be sure that it doesn’t represent any other action. This special has the lowest priority.
            // The separation criteria for splitting the string into tokens are mathematical and logical operators or parentheses. Note that the priority of the operators does not matter in the first step.
            // An example of applying the first step to some expressions:
            //  Split(“3 + 5”) = { Variable(3, “+“), Variable(5, “)“)}
            //  Split(“11 - 2 * 5”) = { Variable(11, “-“), Variable(2, “*“), Variable(5, “)“)}
            // As soon as an opening parentheses if found in the expression string, the whole Split-and-Merge algorithm is applied recursively to the expression in parentheses and replace the expression in parentheses with the calculated result. For example:
            //  Split(“3 * (2 + 2)”) = { Variable(3, “*“),Variable(SplitAndMerge(“2 + 2”), “)“)} ={ Variable(3, “*“), Variable(4, “)“)}

            // The split-and-merge algorithm has O(n) complexity if n is the number of characters in the expression string. 
            // This is so because each token will be read only once during the splitting step and then, in the worst case, 
            // there will be at most 2(m - 1) – 1 comparisons in the merging step, where m is the number of cells created in the first step.

            List<IDFExpressionCell> listToMerge = Split(data, ref from, to, variableDictionary, out expressionType);
            if (listToMerge.Count == 0)
            {
                throw new Exception("Unexpected error, couldn't parse expression: " + data.Substring(from));
            }

            // In the second step, all the cells are merged together. 
            int index = 1;
            IDFExpressionCell baseCell = listToMerge[0];
            return MergeList(baseCell, ref index, listToMerge, variableDictionary, ref expressionType);
        }

        /// <summary>
        /// Split specified part of an expression string into individual variables with corresponding operators
        /// </summary>
        /// <param name="data"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <returns></returns>
        private static List<IDFExpressionCell> Split(string data, ref int from, char to, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType)
        {
            // Splitting an Expression into a List of Cells
            // The first part of the algorithm splits an expression into a list of cells. Mathematical operator precedence isn’t taken into account in this step.First, the expression is split into a list of tokens. 
            // All tokens are separated by any mathematical operator or by an open or close parenthesis. The parentheses may, but don’t have to, have an associated function; for example, “1 - sin(1 - 2)” has an associated function, but “1 - (1 - 2)” doesn’t.

            // The StringBuilder item will hold the current token, adding characters to it one by one as soon as they’re read from the expression string.
            StringBuilder item = new StringBuilder();
            List<IDFExpressionCell> listToMerge = new List<IDFExpressionCell>(16);
            expressionType = IDFExpressionType.Undefined;

            do
            { // Main processing cycle of the first part.
                char ch = data[from++];

                // The StillCollecting method checks if the characters for the current token are still being collected.
                // This isn’t the case if the current character is END_ARG or any other special “to” character (such as a comma 
                // if the parsing arguments are separated by a comma; I’ll provide an example of this using the power function later). 
                // Also, the token isn’t being collected anymore if the current character is a valid action or a START_ARG:
                if (IsStillCollecting(item.ToString(), ch, to))
                { // The char still belongs to the previous operand.
                    item.Append(ch);
                    if (from < data.Length && data[from] != to)
                    {
                        continue;
                    }
                }

                // We are done getting the next token. The GetIDFFile() call in the ParserFunction class below may recursively call SplitAndMerge(). 
                // This will happen if extracted item is a function or if the next item is starting with a START_ARG '('.

                // The actual parsing of the extracted token happens here
                ParserFunction func = new ParserFunction(data, variableDictionary, ref from, item.ToString(), ch);

                IncreaseExpressionCount();
                IDFFile idfFile = func.GetIDFFile(data, variableDictionary, out expressionType, ref from);
                if (idfFile == null)
                {
                    throw new NullReferenceException("Null reference for variable in expression " + data);
                }

                // At the end of the splitting step, all of the subexpressions in parentheses and all of the function calls
                // are eliminated via the recursive calls to the whole algorithm evaluation. But the resulting actions of
                // these recursive calls will always have the END_ARG action, which won’t be correct in the global
                // expression scope if the calculated expression isn’t at the end of the expression to be evaluated. This
                // is why the action needs to be updated after each recursive call.
                string action;
                if (IsValidShortAction(ch))
                {
                    action = ch.ToString();
                }
                else if (IsValidLongAction(data, ref from, to, out action))
                {
                    // action has been read
                }
                else
                {
                    action = UpdateAction(data, ref from, ch, to).ToString();
                }

                string expId = GetCurrentExpressionId();
                IDFExpressionCell cell = null;
                if ((expressionType != IDFExpressionType.Constant) && (expressionType != IDFExpressionType.File) && (expressionType != IDFExpressionType.Variable))
                {
                    // For complex expressions use current expressionId instead of value/filename/variablename
                    cell = new IDFExpressionCell(idfFile, expId, action, expId, 1, expressionType); ;
                }
                else
                {
                    cell = new IDFExpressionCell(idfFile, item.ToString(), action, expId, 1, expressionType);
                }
                listToMerge.Add(cell);
                item.Clear();

            } while (from < data.Length && data[from] != to);

            if (from < data.Length && (data[from] == END_ARG || data[from] == to))
            {
                // This happens when called recursively: move one char forward.
                from++;
            }

            return listToMerge;
        }

        public static string GetCurrentExpressionId()
        {
            return "Exp" + ExpressionCount;
        }

        internal static long GetCurrentExpressionCount()
        {
            return ExpressionCount;
        }

        public static void IncreaseExpressionCount()
        {
            ExpressionCount++;
        }

        private static bool IsStillCollecting(string item, char ch, char to)
        {
            // Collecting the current token is finished as soon as a mathematical operator is found as described in the ValidAction method, or parentheses defined by 
            // the START_ARG or END_ARG constants. There’s also a special case involving a “-” token, which is used to denote a number starting with a negative sign.

            // Stop collecting if either got END_ARG ')' or to char, e.g. SEP_ARG ','.
            char stopCollecting = (to == END_ARG || to == END_LINE) ? END_ARG : to;

            return (item.Length == 0 && (ch == '-' || ch == END_ARG)) 
                || ((item.Length > 0) && IsDigit(item[0]) && item.EndsWith("E") && ((ch == '-') || (ch == '+'))) 
                || !(IsValidShortAction(ch) || IsValidLongAction(ch) || ch == START_ARG || ch == stopCollecting);
        }

        private static bool IsDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        private static bool IsValidShortAction(char ch)
        {
            return ch == '*' || ch == '/' || ch == '+' || ch == '-' || ch == '^';
        }

        private static bool IsValidLongAction(char ch)
        {
            return ch == '!' || ch == '=' || ch == '>' || ch == '<' || ch == '&' || ch == '|';
        }

        private static bool IsValidLongAction(string data, ref int from, char to, out string action)
        {
            action = null;
            if (from >= data.Length - 1)
            {
                return false;
            }

            switch (data[from - 1])
            {
                case '!':
                    if (data[from].Equals('='))
                    {
                        action = "!=";
                        from++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case '=':
                    if (data[from].Equals('='))
                    {
                        action = "==";
                        from++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case '>':
                    if (data[from].Equals('='))
                    {
                        action = ">=";
                        from++;
                        return true;
                    }
                    else
                    {
                        action = ">";
                        return true;
                    }
                case '<':
                    if (data[from].Equals('='))
                    {
                        action = "<=";
                        from++;
                        return true;
                    }
                    else
                    {
                        action = "<";
                        return true;
                    }
                case '&':
                    if (data[from].Equals('&'))
                    {
                        action = "&&";
                        from++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case '|':
                    if (data[from].Equals('|'))
                    {
                        action = "||";
                        from++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        private static char UpdateAction(string item, ref int from, char ch, char to)
        {
            if (from >= item.Length || item[from] == END_ARG || item[from] == to)
            {
                return END_ARG;
            }

            int index = from;
            char res = ch;
            while (!IsValidShortAction(res) && index < item.Length)
            { // Look for the next character in string until a valid action is found.
                res = item[index++];
            }

            from = IsValidShortAction(res) ? index
                                    : index > from ? index - 1
                                                   : from;
            return res;
        }

        /// <summary>
        /// The list of cells is merged one by one according to the priorities of the actions; that is, the mathematical operators. See the GetPriority() method.
        /// From outside this function is called with mergeOneOnly = false. It also calls itself recursively with mergeOneOnly = true, meaning that it will return after only one merge.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="index"></param>
        /// <param name="listToMerge"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <param name="mergeOneOnly"></param>
        /// <returns></returns>
        private static IDFFile MergeList(IDFExpressionCell current, ref int index, List<IDFExpressionCell> listToMerge, Dictionary<string, IDFExpVariable> variableDictionary, ref IDFExpressionType expressionType, bool mergeOneOnly = false)
        {
            // Two cells can be merged if and only if the priority of the action of the cell on the left isn’t lower than the priority of the action of the cell next to it:
            // Merging cells means applying the action of the left cell to the values of the left cell and the right cell.The new cell will have the same action as the right cell.
            // For example, merging Cell(8, ‘-’) and Cell(5, ‘+’) will lead to a new Cell(8 – 5, ‘+’) = Cell (3, ‘+’).
            // If two cells can’t be merged because the priority of the left cell is lower than the right cell, a temporary move to the next, right cell happens, 
            // in order to try to merge it with the cell next to it, and so on, recursively. As soon as the right cell has been merged with the cell next to it, 
            // the original, left cell is returned, which is tried to remerge with the newly created right cell.

            // Note that, from the outside, this method is called with the mergeOneOnly parameter set to false, so it won’t return before completing the whole merge. 
            // In contrast, when the merge method is called recursively (when the left and the right cells can’t be merged because of their priorities), 
            // mergeOneOnly will be set to true because I want to return to where I was as soon as I complete an actual merge in the MergeCells method.
            // Also note that the value returned from the Merge method is the actual result of the expression.


            while (index < listToMerge.Count)
            {
                IDFExpressionCell next = listToMerge[index++];

                while (!CanMergeCells(current, next))
                {
                    // If we cannot merge cells yet, go to the next cell and merge next cells first. 
                    // E.g. if we have 1+2*3, we first merge next cells, i.e. 2*3, getting 6, and then we can merge 1+6.
                    MergeList(next, ref index, listToMerge, variableDictionary, ref expressionType, true /* mergeOneOnly */);
                }
                MergeCells(current, next, variableDictionary);
                expressionType = IDFExpressionType.Complex;
                if (mergeOneOnly)
                {
                    return current.IDFFile;
                }
            }

            return current.IDFFile;
        }

        private static void MergeCells(IDFExpressionCell leftCell, IDFExpressionCell rightCell, Dictionary<string, IDFExpVariable> variableDictionary)
        {
            IncreaseExpressionCount();
            string newExpId = GetCurrentExpressionId();

            string expression = leftCell.IDFId + leftCell.Action + rightCell.IDFId;
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + newExpId + " = " + IDFExpParser.ExpandExpressionKeys(expression, variableDictionary) + "'", leftCell.ExpDepth);
            }

            IDFFile leftCellIDFFile = leftCell.IDFFile;
            IDFFile rightCellIDFFile = rightCell.IDFFile;

            if (rightCellIDFFile == null)
            {
                throw new NullReferenceException("Null reference for IDFFile object of rightCell " + rightCell.IDFId);
            }

            // Check and correct for NoData-values, use NoData-value of leftcell expression
            if (leftCell.IDFId.Equals("NoData"))
            {
                leftCellIDFFile = new ConstantIDFFile(rightCell.IDFFile.NoDataValue);
                leftCellIDFFile.NoDataValue = rightCell.IDFFile.NoDataValue;

            }
            if (rightCell.IDFId.Equals("NoData"))
            {
                rightCellIDFFile = new ConstantIDFFile(leftCell.IDFFile.NoDataValue);
                rightCellIDFFile.NoDataValue = leftCell.IDFFile.NoDataValue;
            }

            Extent expressionExtent = null;
            if (IDFExpParser.Extent != null)
            {
                // Enlarge/clip IDF-files to specified extent
                expressionExtent = IDFExpParser.Extent;

                // Clip/enlarge IDF-files 
                IDFFile idfFile1Tmp = leftCell.IDFFile;
                IDFFile idfFile2Tmp = rightCell.IDFFile;
                if (idfFile1Tmp is ConstantIDFFile)
                {
                    idfFile1Tmp = idfFile1Tmp.ClipIDF(expressionExtent);
                }
                else
                {
                    if (!idfFile2Tmp.Extent.Equals(expressionExtent))
                    {
                        if (!idfFile2Tmp.Extent.Contains(expressionExtent))
                        {
                            if (IDFExpParser.IsDebugMode)
                            {
                                IDFExpParser.Log.AddInfo("Enlarging IDF-file2 to specified extent: " + expressionExtent.ToString(), 1);
                            }
                            idfFile2Tmp = idfFile2Tmp.EnlargeIDF(expressionExtent);
                        }
                        if (!expressionExtent.Contains(idfFile2Tmp.Extent))
                        {
                            if (IDFExpParser.IsDebugMode)
                            {
                                IDFExpParser.Log.AddInfo("Clipping IDF-file2 to specified extent: " + expressionExtent.ToString(), 1);
                            }
                            idfFile2Tmp = idfFile2Tmp.ClipIDF(expressionExtent);
                        }
                    }

                    if (!idfFile1Tmp.Extent.Equals(expressionExtent))
                    {
                        if (!idfFile1Tmp.Extent.Contains(expressionExtent))
                        {
                            if (IDFExpParser.IsDebugMode)
                            {
                                IDFExpParser.Log.AddInfo("Enlarging IDF-file1 to specified extent: " + expressionExtent.ToString(), 1);
                            }
                            idfFile1Tmp = idfFile1Tmp.EnlargeIDF(expressionExtent);
                        }
                        if (!expressionExtent.Contains(idfFile1Tmp.Extent))
                        {
                            if (IDFExpParser.IsDebugMode)
                            {
                                IDFExpParser.Log.AddInfo("Clipping IDF-file1 to specified extent: " + expressionExtent.ToString(), 1);
                            }
                            idfFile1Tmp = idfFile1Tmp.ClipIDF(expressionExtent);
                        }
                    }

                    leftCellIDFFile = idfFile1Tmp;
                    rightCellIDFFile = idfFile2Tmp;
                }
            }

            switch (leftCell.Action)
            {
                case "^":
                    leftCell.IDFFile = leftCellIDFFile ^ rightCellIDFFile;
                    break;
                case "*":
                    leftCell.IDFFile = leftCellIDFFile * rightCellIDFFile;
                    break;
                case "/":
                    leftCell.IDFFile = leftCellIDFFile / rightCellIDFFile;
                    break;
                case "+":
                    leftCell.IDFFile = leftCellIDFFile + rightCellIDFFile;
                    break;
                case "-":
                    leftCell.IDFFile = leftCellIDFFile - rightCellIDFFile;
                    break;
                case "==":
                    leftCell.IDFFile = leftCellIDFFile.IsEqual(rightCellIDFFile);
                    break;
                case "!=":
                    leftCell.IDFFile = leftCellIDFFile.IsNotEqual(rightCellIDFFile);
                    break;
                case "<":
                    leftCell.IDFFile = leftCellIDFFile.IsLesser(rightCellIDFFile);
                    break;
                case "<=":
                    leftCell.IDFFile = leftCellIDFFile.IsLesserEqual(rightCellIDFFile);
                    break;
                case ">":
                    leftCell.IDFFile = leftCellIDFFile.IsGreater(rightCellIDFFile);
                    break;
                case ">=":
                    leftCell.IDFFile = leftCellIDFFile.IsGreaterEqual(rightCellIDFFile);
                    break;
                case "&&":
                    leftCell.IDFFile = leftCellIDFFile.LogicalAnd(rightCellIDFFile);
                    break;
                case "||":
                    leftCell.IDFFile = leftCellIDFFile.LogicalOr(rightCellIDFFile);
                    break;
            }

            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                WriteExpressionResult(leftCell.IDFFile, newExpId, expression, IDFExpParser.Log);
            }

            leftCell.IDFId = newExpId;
            leftCell.ExpId = newExpId;
            leftCell.Action = rightCell.Action;
            leftCell.ExpDepth = rightCell.ExpDepth; // TODO increase/decrease to use expression level for indentation of logmessages
        }

        private static bool CanMergeCells(IDFExpressionCell leftCell, IDFExpressionCell rightCell)
        {
            return GetPriority(leftCell.Action) >= GetPriority(rightCell.Action);
        }

        private static int GetPriority(string action)
        {
            switch (action)
            {
                case "^": return 4;
                case "*":
                case "/": return 3;
                case "+":
                case "-": return 2;
                case "<":
                case "<=":
                case ">":
                case ">=":
                case "!=":
                case "==": return 1;
            }
            return 0;
        }

        /// <summary>
        /// Parse expression for IDF-filename and return corresponding IDF-file or null if file does not exist
        /// </summary>
        /// <param name="idfFilenameExpression"></param>
        /// <param name="expressionType"></param>
        /// <returns></returns>
        public static IDFFile ParseIDFFilename(string idfFilenameExpression, out IDFExpressionType expressionType)
        {
            string idfFilename = GetIDFFilenameFromExpression(idfFilenameExpression, out expressionType);
            if (idfFilename != null)
            {
                return ReadIDFFile(idfFilename);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve full filename from IDF-filename expression and set expressionType
        /// </summary>
        /// <param name="idfFilenameExpression"></param>
        /// <param name="expressionType">type 'File' if file exists, otherwise 'Undefined'</param>
        /// <returns></returns>
        protected static string GetIDFFilenameFromExpression(string idfFilenameExpression, out IDFExpressionType expressionType)
        {
            string idfFilename = idfFilenameExpression;
            if (!Path.IsPathRooted(idfFilename) && (BasePath != null))
            {
                idfFilename = Path.Combine(BasePath, idfFilenameExpression);
            }
            if (!File.Exists(idfFilename))
            {
                expressionType = IDFExpressionType.Undefined;
                return null;
            }
            expressionType = IDFExpressionType.File;

            return idfFilename;
        }

        /// <summary>
        /// Read an IDF-file with specified filename, using lazy-loading. NoData is replaced with NoDataCalculation if specified by UseNoDataAsValue-field.
        /// </summary>
        /// <param name="idfFilename"></param>
        /// <returns></returns>
        protected static IDFFile ReadIDFFile(string idfFilename)
        {
            IDFFile resultIDF = IDFFile.ReadFile(idfFilename, true, null, 0, Interpreter.Extent);
            if (IsDebugMode)
            {
                resultIDF.IsDebugMode = true;
            }
            // IDFFile resultIDF = (isSparseIDFUsed) ? SparseIDFFile.ReadIDFFile(idfFilename, true) : IDFFile.ReadFile(idfFilename, true);
            if (UseNoDataAsValue)
            {
                resultIDF.NoDataCalculationValue = (NoDataValue.Equals(float.NaN)) ? resultIDF.NoDataValue : NoDataValue;
            }
            if (resultIDF.NoDataValue.Equals(float.NaN) && (Log != null))
            {
                Log.AddWarning("NoData-value of IDF-file '" + Path.GetFileName(idfFilename) + "' is NaN, this may produce unexpected results!");
            }
            if ((resultIDF.NCols == 0) || (resultIDF.NRows == 0))
            {
                Log.AddWarning("Empty IDF-file (number of rows/columns is 0), this may produce unexpected results!");
            }
            return resultIDF;
        }

        /// <summary>
        /// Utility method for expanding variable keys to variable names in specified expression string
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="variableDictionary"></param>
        /// <returns></returns>
        public static string ExpandExpressionKeys(string expression, Dictionary<string, IDFExpVariable> variableDictionary)
        {
            string result = expression;
            foreach (string key in variableDictionary.Keys)
            {
                if (result.Contains(key))
                {
                    result = result.Replace(key, variableDictionary[key].Name);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Base class for parser functions: override in subclass to create a specific function that can be used in expressions.
    /// Users of the parser can add as many functions as they wish by calling the following method on the base ParserFunction class
    /// </summary>
    public class ParserFunction
    {
        // The function factory is implemented using the virtual constructor idiom that was first published by James Coplien. 
        // In C#, this is often implemented using a factory method that uses an extra factory class to produce the needed object. 
        // But Coplien’s older design pattern doesn’t need an extra factory façade class and instead just constructs a new object 
        // on the fly using the implementation member m_impl that’s derived from the same class
        protected ParserFunction impl;

        // Two special “standard” functions are used in the ParserFunction constructor: 
        // - The first is the identity function; it will be called to parse any expression in parentheses that doesn’t have an associated function
        // - The second function is a “catchall” that’s called when no function is found that corresponds to the last extracted token.
        //   This will happen when the extracted token is neither a real number nor an implemented function. In the latter case, an exception will be thrown.
        internal static IDFFileFunction idfFileFunction = new IDFFileFunction();
        internal static IdentityFunction identityFunction = new IdentityFunction();

        // A dictionary is used to hold all of the parser functions. This dictionary maps the string name of the
        // function (such as “min”) to the actual object implementing this function
        protected static Dictionary<string, ParserFunction> functions = new Dictionary<string, ParserFunction>();

        /// <summary>
        /// Constructor for the function implementation classes, deriving from the ParserFunction class
        /// </summary>
        public ParserFunction()
        {
            // The function implementation classes, deriving from the ParserFunction class, won’t be using the
            // internal constructor. Instead, they’ll use the following constructor of the base class

            this.impl = this;
        }

        /// <summary>
        /// A "virtual" Constructor: an implementation of the “virtual constructor” idiom for the function factory implementation. This idiom was introduced in C++ by James Coplien
        /// </summary>
        /// <param name="data"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="from"></param>
        /// <param name="item"></param>
        /// <param name="ch"></param>
        internal ParserFunction(string data, Dictionary<string, IDFExpVariable> variableDictionary, ref int from, string item, char ch)
        {
            // The special internal constructor initializes this member with the appropriate class. The actual class of
            // the created implementation object m_impl depends on the input parameters

            if (item.Length == 0 && ch == IDFExpParser.START_ARG)
            {
                // There is no function, just an expression in parentheses
                impl = identityFunction;
                return;
            }

            if ((ch == '(') && functions.TryGetValue(item.ToLower(), out impl))
            {
                // Function exists and is registered (e.g. pi, exp, etc.)
                return;
            }

            // No function, check if it's a variable
            if (variableDictionary.ContainsKey(item))
            {
                idfFileFunction.Item = item;
                impl = idfFileFunction;
                return;
            }

            // Function not found, will try to parse this as a number.
            idfFileFunction.Item = item;
            impl = idfFileFunction;
        }

        /// <summary>
        /// Remove all regitered functions
        /// </summary>
        public static void RemoveAllFunctions()
        {
            functions = new Dictionary<string, ParserFunction>();
        }

        /// <summary>
        /// Register a new ParserFunction with specified name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="function"></param>
        public static void RegisterFunction(string name, ParserFunction function)
        {
            functions[name.ToLower()] = function;
        }

        /// <summary>
        /// Retrieves the IDF-file that results for an expression that uses a ParserFunction-subclass
        /// </summary>
        /// <param name="data"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        internal IDFFile GetIDFFile(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            // The GetIDFFile method is called on the created ParserFunction, but the real work is done in the
            // implementation function, which will override the evaluate method of the base class
            return impl.Evaluate(data, variableDictionary, out expressionType, ref from);
        }

        /// <summary>
        /// Method for doing the actual work for evaluating a ParserFunction
        /// </summary>
        /// <param name="data"></param>
        /// <param name="variableDictionary"></param>
        /// <param name="expressionType"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        protected virtual IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            // The real implementation will be in the derived classes.
            expressionType = IDFExpressionType.Undefined;
            return new ConstantIDFFile(0);
        }

        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public static string Name 
        { 
            get { throw new NotImplementedException(); } // To be redefined in subclass
        }
    }

    /// <summary>
    /// Class for parsing a simple expression with just an IDF-file reference (a variable, a filename or a value, which is handled as a ConstantIDFFile object).
    /// </summary>
    public class IDFFileFunction : ParserFunction
    {
        public string Item { protected get; set; }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            if (float.TryParse(Item, NumberStyles.Float, IDFExpParser.englishCultureInfo, out float num))
            {
                expressionType = IDFExpressionType.Constant;
                IDFFile resultIDF = new ConstantIDFFile(num);
                if (IDFExpParser.UseNoDataAsValue)
                {
                    resultIDF.NoDataCalculationValue = (IDFExpParser.NoDataValue.Equals(float.NaN)) ? resultIDF.NoDataValue : IDFExpParser.NoDataValue;
                }
                return resultIDF;
            }
            else if (variableDictionary.ContainsKey(Item))
            {
                IDFExpVariable idfExpVariable = variableDictionary[Item];
                IDFFile idfFile = idfExpVariable.IDFFile;
                expressionType = (idfFile is ConstantIDFFile) ? IDFExpressionType.Constant : IDFExpressionType.Variable;

                if (idfFile != null)
                {
                    // Force values to be loaded now, since it is used in an expression, the values are needed (also to allow redefined variables to be written to file)
                    idfFile.EnsureLoadedValues();
                }

                return idfFile;
            }
            else if (Item.ToLower().EndsWith(".idf"))
            {
                IDFFile resultIDFFile = IDFExpParser.ParseIDFFilename(Item, out expressionType);
                if (resultIDFFile == null)
                {
                    throw new ToolException("IDF-file not found: " + Environment.ExpandEnvironmentVariables(IDFExpParser.ExpandExpressionKeys(Item, variableDictionary)));
                }
                return resultIDFFile;
            }
            else
            {
                throw new ToolException("Could not parse token '" + IDFExpParser.ExpandExpressionKeys(Item, variableDictionary) + "'");
            }
        }
    }

    /// <summary>
    /// Special class that’s used when no function is found that corresponds to the last extracted token, which will happen when the extracted token is neither a real number nor an implemented function. In the latter case, an exception will be thrown.
    /// </summary>
    public class IdentityFunction : ParserFunction
    {
        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            return IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
        }
    }

    /// <summary>
    /// Class for parsing an if-function to handle an if-then-else expression with syntax if(cond,thenIDF,elseIDF) 
    /// </summary>
    class IfThenElseFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "if"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            // This is how to write a function requiring multiple arguments separated by a comma
            int startIdx = from;
            IDFFile conditionIDFFile = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string condExpression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile thenIDFFile = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            if (thenIDFFile is ConstantIDFFile)
            {
                thenIDFFile = ((ConstantIDFFile)thenIDFFile).Allocate(conditionIDFFile);

                // For constant IDF-files ITB-level is not defined, remove if it was copied from the condition IDF-file
                thenIDFFile.ClearITBLevels();
            }
            string thenExpression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile elseIDFFile = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            if (elseIDFFile is ConstantIDFFile)
            {
                elseIDFFile = ((ConstantIDFFile)elseIDFFile).Allocate(conditionIDFFile);

                // For constant IDF-files ITB-level is not defined, remove if it was copied from the condition IDF-file
                elseIDFFile.ClearITBLevels();
            }
            string elseExpression = data.Substring(startIdx, from - startIdx - 1);
            string ifExpression = Name+ "(" + condExpression + "," + thenExpression + "," + elseExpression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(ifExpression, variableDictionary) + "'", 1);
            }

            // If all input IDF-files are constant IDF-files perform simple direct if-then-else
            if ((conditionIDFFile is ConstantIDFFile) && (thenIDFFile is ConstantIDFFile) && (elseIDFFile is ConstantIDFFile))
            {
                return new ConstantIDFFile((((ConstantIDFFile)conditionIDFFile).ConstantValue != 0) ? ((ConstantIDFFile)thenIDFFile).ConstantValue : ((ConstantIDFFile)elseIDFFile).ConstantValue);
            }

            Extent expressionExtent = null;
            string expressionExtentDescription = null;
            if (IDFExpParser.Extent != null)
            {
                // Enlarge/clip IDF-files to specified extent
                expressionExtent = IDFExpParser.Extent;
                expressionExtentDescription = "specified";
            }
            else
            {
                // Enlarge IDF-files to union of IDF-files
                if (!(conditionIDFFile is ConstantIDFFile))
                {
                    expressionExtent = conditionIDFFile.Extent;
                }
                else if (!(thenIDFFile is ConstantIDFFile))
                {
                    expressionExtent = thenIDFFile.Extent;
                }
                else if (!(elseIDFFile is ConstantIDFFile))
                {
                    expressionExtent = elseIDFFile.Extent;
                }
                else
                {
                    // Use ConstantIDF extent, since all inputs are ConstantIDFFiles
                    expressionExtent = conditionIDFFile.Extent;
                }

                expressionExtentDescription = "union";
                if (!(conditionIDFFile is ConstantIDFFile) && !(thenIDFFile is ConstantIDFFile))
                {
                    expressionExtent = conditionIDFFile.Extent.Union(elseIDFFile.Extent);
                }
                if (!(conditionIDFFile is ConstantIDFFile) && !(elseIDFFile is ConstantIDFFile))
                {
                    expressionExtent = expressionExtent.Union(elseIDFFile.Extent);
                }

            }

            if (!conditionIDFFile.Extent.Equals(expressionExtent))
            {
                if (!conditionIDFFile.Extent.Contains(expressionExtent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Enlarging condition-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    conditionIDFFile = conditionIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(conditionIDFFile.Extent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Clipping condition-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    conditionIDFFile = conditionIDFFile.ClipIDF(expressionExtent);
                }
            }
            if (!thenIDFFile.Extent.Equals(expressionExtent))
            {
                if (!thenIDFFile.Extent.Contains(expressionExtent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Enlarging then-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    thenIDFFile = thenIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(thenIDFFile.Extent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Clipping then-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    thenIDFFile = thenIDFFile.ClipIDF(expressionExtent);
                }
            }
            if (!elseIDFFile.Extent.Equals(expressionExtent))
            {
                if (!elseIDFFile.Extent.Contains(expressionExtent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Enlarging else-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    elseIDFFile = elseIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(elseIDFFile.Extent))
                {
                    if (IDFExpParser.IsDebugMode)
                    {
                        IDFExpParser.Log.AddInfo("Clipping else-IDF file to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                    }
                    elseIDFFile = elseIDFFile.ClipIDF(expressionExtent);
                }
            }

            // Now evaluatie IF-expression
            float minXCellsize = Math.Min(Math.Min(conditionIDFFile.XCellsize, thenIDFFile.XCellsize), elseIDFFile.XCellsize);
            float minYCellsize = Math.Min(Math.Min(conditionIDFFile.YCellsize, thenIDFFile.YCellsize), elseIDFFile.YCellsize);
            IDFFile resultIDFFile = new IDFFile(expId + ".IDF", conditionIDFFile.Extent, minXCellsize, minYCellsize, conditionIDFFile.NoDataValue);
            if (!thenIDFFile.TOPLevel.Equals(float.NaN))
            {
                resultIDFFile.SetITBLevels(thenIDFFile.TOPLevel, thenIDFFile.BOTLevel);
            }
            else if (!elseIDFFile.TOPLevel.Equals(float.NaN))
            {
                resultIDFFile.SetITBLevels(elseIDFFile.TOPLevel, elseIDFFile.BOTLevel);
            }

            resultIDFFile.ReplaceValues(elseIDFFile);
            resultIDFFile.ReplaceValues(conditionIDFFile, 1, thenIDFFile);
            expressionType = IDFExpressionType.IfThenElse;

            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), ifExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing a round-function to handle rounding the values of an IDF-file with syntax round(IDF,d), with d the number of decimals
    /// </summary>
    class RoundFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "round"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string roundExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(roundExpression, variableDictionary) + "'", 1);
            }

            if (idfFile1 is ConstantIDFFile)
            {
                throw new ToolException("Argument 1 of round-function should be an IDF-file: " + roundExpression);
            }
            if (!(idfFile2 is ConstantIDFFile))
            {
                throw new ToolException("Argument 2 of round-function should be an constant integer value: " + roundExpression);
            }

            // Now evaluate expression
            int decimalCount = (int)((ConstantIDFFile)idfFile2).ConstantValue;

            IDFFile resultIDFFile = idfFile1.CopyIDF(expId + ".IDF");
            resultIDFFile.RoundValues(decimalCount);
            expressionType = IDFExpressionType.Function;

            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), roundExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing a max-function to get the maximum value per cell for two IDF-files, with syntax max(IDF1,IDF2)
    /// </summary>
    class MaxFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "max"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string maxExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(maxExpression, variableDictionary) + "'", 1);
            }

            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                float constValue1 = ((ConstantIDFFile)idfFile1).ConstantValue;
                float constValue2 = ((ConstantIDFFile)idfFile2).ConstantValue;
                return new ConstantIDFFile((constValue1 > constValue2) ? constValue1 : constValue2);
            }
            else if (idfFile1 is ConstantIDFFile)
            {
                // swap idfFile1 and 2
                IDFFile tmp = idfFile1;
                idfFile1 = idfFile2;
                idfFile2 = tmp;
            }

            Extent expressionExtent = null;
            string expressionExtentDescription = null;
            if (IDFExpParser.Extent != null)
            {
                // Enlarge/clip IDF-files to specified extent
                expressionExtent = IDFExpParser.Extent;
                expressionExtentDescription = "specified";
            }
            else
            {
                // Clip/enlarge IDF-files to first IDF-file
                expressionExtent = idfFile1.Extent;
                expressionExtentDescription = "IDF-file1";
            }

            // Clip/enlarge IDF-files 
            IDFFile idfFile1Tmp = idfFile1;
            IDFFile idfFile2Tmp = idfFile2;
            if (idfFile1 is ConstantIDFFile)
            {
                idfFile1Tmp = idfFile1Tmp.ClipIDF(expressionExtent);
            }
            else
            {
                if (!idfFile2Tmp.Extent.Equals(expressionExtent))
                {
                    if (!idfFile2Tmp.Extent.Contains(expressionExtent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Enlarging IDF-file2 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile2Tmp = idfFile2Tmp.EnlargeIDF(expressionExtent);
                    }
                    if (!expressionExtent.Contains(idfFile2Tmp.Extent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Clipping IDF-file2 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile2Tmp = idfFile2Tmp.ClipIDF(expressionExtent);
                    }
                }

                if (!idfFile1Tmp.Extent.Equals(expressionExtent))
                {
                    if (!idfFile1Tmp.Extent.Contains(expressionExtent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Enlarging IDF-file1 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile1Tmp = idfFile1Tmp.EnlargeIDF(expressionExtent);
                    }
                    if (!expressionExtent.Contains(idfFile1Tmp.Extent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Clipping IDF-file1 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile1Tmp = idfFile1Tmp.ClipIDF(expressionExtent);
                    }
                }
            }

            // Now evaluate expression
            IDFFile resultIDFFile = idfFile1Tmp.CopyIDF(expId + ".IDF");
            IDFFile greaterThanIDFFile = idfFile2Tmp.IsGreater(resultIDFFile);
            resultIDFFile.ReplaceValues(greaterThanIDFFile, 1, idfFile2Tmp);
            expressionType = IDFExpressionType.Function;

            if (!resultIDFFile.TOPLevel.Equals(float.NaN))
            {
                resultIDFFile.SetITBLevels(idfFile2Tmp.TOPLevel, idfFile2Tmp.BOTLevel);
            }

            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), maxExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing a min-function to get the minimum value per cell for two IDF-files, with syntax min(IDF1,IDF2)
    /// </summary>
    class MinFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "min"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string maxExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(maxExpression, variableDictionary) + "'", 1);
            }

            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                float constValue1 = ((ConstantIDFFile)idfFile1).ConstantValue;
                float constValue2 = ((ConstantIDFFile)idfFile2).ConstantValue;
                return new ConstantIDFFile((constValue1 < constValue2) ? constValue1 : constValue2);
            }
            else if (idfFile1 is ConstantIDFFile)
            {
                // swap idfFile1 and 2
                IDFFile tmp = idfFile1;
                idfFile1 = idfFile2;
                idfFile2 = tmp;
            }

            Extent expressionExtent = null;
            string expressionExtentDescription = null;
            if (IDFExpParser.Extent != null)
            {
                // Enlarge/clip IDF-files to specified extent
                expressionExtent = IDFExpParser.Extent;
                expressionExtentDescription = "specified";
            }
            else
            {
                // Clip/enlarge IDF-files to first IDF-file
                expressionExtent = idfFile1.Extent;
                expressionExtentDescription = "IDF-file1";
            }

            // Clip/enlarge IDF-files 
            IDFFile idfFile1Tmp = idfFile1;
            IDFFile idfFile2Tmp = idfFile2;
            if (idfFile1 is ConstantIDFFile)
            {
                idfFile1Tmp = idfFile1.ClipIDF(expressionExtent);
            }
            else
            {
                if (!idfFile2Tmp.Extent.Equals(expressionExtent))
                {
                    if (!idfFile2Tmp.Extent.Contains(expressionExtent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Enlarging IDF-file2 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile2Tmp = idfFile2Tmp.EnlargeIDF(expressionExtent);
                    }
                    if (!expressionExtent.Contains(idfFile2Tmp.Extent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Clipping IDF-file2 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile2Tmp = idfFile2Tmp.ClipIDF(expressionExtent);
                    }
                }

                if (!idfFile1Tmp.Extent.Equals(expressionExtent))
                {
                    if (!idfFile1Tmp.Extent.Contains(expressionExtent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Enlarging IDF-file1 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile1Tmp = idfFile1Tmp.EnlargeIDF(expressionExtent);
                    }
                    if (!expressionExtent.Contains(idfFile1Tmp.Extent))
                    {
                        if (IDFExpParser.IsDebugMode)
                        {
                            IDFExpParser.Log.AddInfo("Clipping IDF-file1 to " + expressionExtentDescription + " extent: " + expressionExtent.ToString(), 1);
                        }
                        idfFile1Tmp = idfFile1Tmp.ClipIDF(expressionExtent);
                    }
                }
            }

            // Now evaluate expression
            IDFFile resultIDFFile = idfFile1Tmp.CopyIDF(expId + ".IDF");
            IDFFile lesserThanIDFFile = idfFile2Tmp.IsLesser(resultIDFFile);
            resultIDFFile.ReplaceValues(lesserThanIDFFile, 1, idfFile2Tmp);
            expressionType = IDFExpressionType.Function;

            if (!resultIDFFile.TOPLevel.Equals(float.NaN))
            {
                resultIDFFile.SetITBLevels(idfFile2Tmp.TOPLevel, idfFile2Tmp.BOTLevel);
            }

            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), maxExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing a clip-function to clip an IDF-file to the extent of another IDF-file, with syntax clip(IDF1,IDF2)
    /// </summary>
    class ClipFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "clip"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string clipExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(clipExpression, variableDictionary) + "'", 1);
            }

            if (idfFile1 is ConstantIDFFile)
            {
                if (IDFExpParser.IsDebugMode)
                {
                    IDFExpParser.Log.AddInfo("Keeping extent of constant IDF-file1: " + idfFile2.Extent.ToString(), 1);
                }
                return idfFile1;
            }

            if (idfFile2 is ConstantIDFFile)
            {
                throw new ToolException("Argument 2 of clip-function should be an IDF-file: " + clipExpression);
            }

            // Clip first IDF-file to second IDF-file (unless second IDF has a larger extent)
            IDFFile idfFile1Tmp = idfFile1;
            IDFFile resultIDFFile = null;
            if (!idfFile2.Extent.Contains(idfFile1.Extent) && !idfFile1.Extent.Equals(idfFile2.Extent))
            {
                if (IDFExpParser.IsDebugMode)
                {
                    IDFExpParser.Log.AddInfo("Clipping IDF-file1 to extent of IDF-file2: " + idfFile2.Extent.ToString(), 1);
                }
                resultIDFFile = CorrectEmptyIDF(idfFile1.ClipIDF(idfFile2.Extent), idfFile2.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
            }
            else
            {
                resultIDFFile = idfFile1.CopyIDF(FileUtils.AddFilePostFix(idfFile1.Filename, "_clip"));
            }

            return resultIDFFile;
        }

        private IDFFile CorrectEmptyIDF(IDFFile idfFile, Extent extent, float xCellsize, float yCellsize, float noData)
        {
            if (idfFile != null)
            {
                return idfFile;
            }
            else
            {
                IDFExpParser.Log.AddWarning("Empty IDF-file after clipping, dummy 1x1 grid created", 1);
                // float[][] values = new float[0][];
                float[][] values = new float[1][];
                values[0] = new float[1];
                values[0][0] = 1;

                IDFFile emptyIDFFile = new IDFFile("Empty.IDF", new Extent(extent.llx, extent.lly, extent.llx, extent.lly), xCellsize, yCellsize, noData);
                emptyIDFFile.values = values;

                return emptyIDFFile;
            }
        }
    }

    /// <summary>
    /// Class for parsing an enlarge-function to enlarge an IDF-file to the extent of another IDF-file, with syntax clip(IDF1,IDF2)
    /// New cells are filled with NoData-values.
    /// </summary>
    class EnlargeFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "enlarge"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string enlargeExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(enlargeExpression, variableDictionary) + "'", 1);
            }

            if (idfFile1 is ConstantIDFFile)
            {
                if (IDFExpParser.IsDebugMode)
                {
                    IDFExpParser.Log.AddInfo("Keeping extent of constant IDF-file1: " + idfFile2.Extent.ToString(), 1);
                }
                return idfFile1;
            }

            if (idfFile2 is ConstantIDFFile)
            {
                throw new ToolException("Argument 2 of enlarge-function should be an IDF-file: " + enlargeExpression);
            }

            // Enlarge first IDF-file to second IDF-file
            IDFFile idfFile1Tmp = idfFile1;
            IDFFile resultIDFFile = null;
            if (!idfFile1.Extent.Contains(idfFile2.Extent) && !idfFile1.Extent.Equals(idfFile2.Extent))
            {
                if (IDFExpParser.IsDebugMode)
                {
                    IDFExpParser.Log.AddInfo("Enlarging IDF-file1 to extent of IDF-file2: " + idfFile2.Extent.ToString(), 1);
                }
                resultIDFFile = idfFile1.EnlargeIDF(idfFile2.Extent);
            }
            else
            {
                resultIDFFile = idfFile1.CopyIDF(FileUtils.AddFilePostFix(idfFile1.Filename, "_enlarge"));
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing a scale-function to scale an IDF-file (IDF1) to the specified cellsize (IDF2) using specified method, with syntax scale(IDF1,IDF2,method), scale(IDF1,IDF2, methodDown,methodUp). For methods see UpscaleMethodEnum and DownscaleMethodEnum.
    /// </summary>
    class ScaleFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "scale"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            // Parse first argument
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;

            // Parse second argument, check if a third argument is present
            bool hasThirdArg = false;
            char searchedSymbol = IDFExpParser.END_ARG;
            if (data.Substring(startIdx).Contains(IDFExpParser.SEP_ARG))
            {
                hasThirdArg = true;
                searchedSymbol = IDFExpParser.SEP_ARG;
            }
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, searchedSymbol);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);

            // Parse third argument if present and check for fourth argument
            IDFFile idfFile3 = null;
            string idfFile3Expression = null;
            bool hasFourthArg = false;
            if (hasThirdArg)
            {
                startIdx = from;
                searchedSymbol = IDFExpParser.END_ARG;
                if (data.Substring(startIdx).Contains(IDFExpParser.SEP_ARG))
                {
                    hasFourthArg = true;
                    searchedSymbol = IDFExpParser.SEP_ARG;
                }
                idfFile3 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, searchedSymbol);
                idfFile3Expression = data.Substring(startIdx, from - startIdx - 1);
            }

            // Parse fourth argument if present
            IDFFile idfFile4 = null;
            string idfFile4Expression = null;
            if (hasFourthArg)
            {
                startIdx = from;
                idfFile4 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
                idfFile4Expression = data.Substring(startIdx, from - startIdx - 1);
            }

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();

            string scaleExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ((idfFile3Expression != null) ? ("," + idfFile3Expression) : string.Empty) + ((idfFile4Expression != null) ? ("," + idfFile4Expression) : string.Empty) + ")";
            if (idfFile1 is ConstantIDFFile)
            {
                throw new ToolException("Argument 1 of scale-function should be an IDF-file: " + scaleExpression);
            }
            if ((idfFile3 != null) && !(idfFile3 is ConstantIDFFile))
            {
                throw new ToolException("Argument 3 of round-function should be an constant integer value: " + idfFile3Expression);
            }
            if ((idfFile4 != null) && !(idfFile4 is ConstantIDFFile))
            {
                throw new ToolException("Argument 4 of round-function should be an constant integer value: " + idfFile4Expression);
            }

            // Retrieve cellsize;
            float cellSize;
            if (idfFile2 is ConstantIDFFile)
            {
                cellSize = ((ConstantIDFFile)idfFile2).ConstantValue;
            }
            else
            {
                cellSize = idfFile2.XCellsize;
            }

            // Now evaluate expression
            IDFFile resultIDFFile = null;
            string scaleMethodString = "Unknown";
            string scaleTypeString = "Unknown";
            if (cellSize < idfFile1.XCellsize)
            {
                // Downscale
                scaleTypeString = "downscaling";
                DownscaleMethodEnum downscaleMethod = DownscaleMethodEnum.Block;
                scaleMethodString = downscaleMethod.ToString();
                if (hasThirdArg)
                {
                    downscaleMethod = GetDownscaleMethod((int)((ConstantIDFFile)idfFile3).ConstantValue);
                    scaleMethodString = downscaleMethod.ToString();
                }
                resultIDFFile = idfFile1.ScaleDown(cellSize, downscaleMethod);
            }
            else if (cellSize > idfFile1.XCellsize)
            {
                // Upscale
                scaleTypeString = "upscaling";
                UpscaleMethodEnum upscaleMethod = UpscaleMethodEnum.Mean;
                scaleMethodString = upscaleMethod.ToString();
                if (hasThirdArg)
                {
                    upscaleMethod = GetUpscaleMethod((int)((ConstantIDFFile)(idfFile4 != null ? idfFile4 : idfFile3)).ConstantValue);
                    scaleMethodString = upscaleMethod.ToString();
                }
                // Scale and try to keep (align to) extent of source IDF-file
                resultIDFFile = idfFile1.ScaleUp(cellSize, upscaleMethod, null, idfFile1.Extent);
            }
            else
            {
                scaleMethodString = "Copy";
                scaleTypeString = "copying";
                resultIDFFile = idfFile1.CopyIDF(expId + ".IDF");
            }

            expressionType = IDFExpressionType.Function;

            if (hasThirdArg)
            {
                scaleExpression = "scale(" + idfFile1Expression + "," + idfFile2Expression + "," + scaleMethodString + ") [" + scaleTypeString + "]";
            }
            else
            {
                scaleExpression = "scale(" + idfFile1Expression + "," + idfFile2Expression + ") [" + scaleTypeString + " with " + scaleMethodString + "-method]";
            }
            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.Log.AddInfo("Evaluated expression '" + expId + " = " + scaleExpression + "'", 1);
            }
            else
            {
                IDFExpParser.Log.AddInfo("Evaluated expression: '" + scaleExpression + "'", 1);
            }

            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), scaleExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Retrieve DownscaleMethodEnum for integer method value: 0=Block
        /// </summary>
        /// <param name="methodValue"></param>
        /// <returns>an enum value or a ToolException for invalid number</returns>
        private DownscaleMethodEnum GetDownscaleMethod(int methodValue)
        {
            switch (methodValue)
            {
                case 0:
                    return DownscaleMethodEnum.Block;
                case 1:
                    return DownscaleMethodEnum.Divide;
                default:
                    throw new ToolException("Invalid method number for downscale: " + methodValue);
            }
        }

        /// <summary>
        /// Retrieve UpscaleMethodEnum for integer method value.
        /// 0=Mean, 1=Median, 2=Minimum, 3=Maximum, 4=Most occurring, 5=Boundary, 6=Sum
        /// </summary>
        /// <param name="methodValue"></param>
        /// <returns>an enum value or a ToolException for invalid number</returns>
        private UpscaleMethodEnum GetUpscaleMethod(int methodValue)
        {
            switch (methodValue)
            {
                case 0:
                    return UpscaleMethodEnum.Mean;
                case 1:
                    return UpscaleMethodEnum.Median;
                case 2:
                    return UpscaleMethodEnum.Minimum;
                case 3:
                    return UpscaleMethodEnum.Maximum;
                case 4:
                    return UpscaleMethodEnum.MostOccurring;
                case 5:
                    return UpscaleMethodEnum.Boundary;
                case 6:
                    return UpscaleMethodEnum.Sum;
                case 7:
                    return UpscaleMethodEnum.MostOccurringNoData;
                default:
                    throw new ToolException("Invalid method number for downscale: " + methodValue);
            }
        }
    }

    /// <summary>
    /// Class for parsing bbox-function to find minimal extent (bounding box) with all non-NoData-values, with syntax bbox(IDF1). Cellsize and alignment are not changed.
    /// When no non-NoData-cells are present, the extent is not modified.
    /// </summary>
    class BoundingBoxFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "bbox"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFileExpression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            string bboxExpression = Name + "(" + idfFileExpression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(bboxExpression, variableDictionary) + "'", 1);
            }

            IDFFile resultIDFFile = idfFile.Clip();
            resultIDFFile.Filename = expId + ".IDF";
            expressionType = IDFExpressionType.Function;

            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), bboxExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing cellsize-function to retrieve X-cellsize for specified IDF-file. A constant IDF-file is returned
    /// </summary>
    class CellsizeFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "cellsize"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.END_ARG);
            string idfFileExpression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            string cellsizeExpression = Name + "(" + idfFileExpression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(cellsizeExpression, variableDictionary) + "'", 1);
            }

            ConstantIDFFile resultIDFFile = new ConstantIDFFile(idfFile.XCellsize);

            if (IDFExpParser.IsDebugMode)
            {
                IDFExpParser.WriteExpressionResult(resultIDFFile, IDFExpParser.GetCurrentExpressionId(), cellsizeExpression, IDFExpParser.Log);
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for parsing an enlarge-function to enlarge an IDF-file to the extent of another IDF-file, with syntax clip(IDF1,IDF2)
    /// New cells are filled with NoData-values.
    /// </summary>
    class NDFunction : ParserFunction
    {
        /// <summary>
        /// Returns the name of this function as used for calling
        /// </summary>
        public new static string Name
        {
            get { return "nd"; }
        }

        protected override IDFFile Evaluate(string data, Dictionary<string, IDFExpVariable> variableDictionary, out IDFExpressionType expressionType, ref int from)
        {
            int startIdx = from;
            IDFFile idfFile1 = IDFExpParser.SplitAndMerge(data, variableDictionary, out expressionType, ref from, IDFExpParser.SEP_ARG);
            string idfFile1Expression = data.Substring(startIdx, from - startIdx - 1);
            startIdx = from;
            IDFFile idfFile2 = IDFExpParser.SplitAndMerge(data, variableDictionary, out IDFExpressionType expressionType2, ref from, IDFExpParser.END_ARG);
            string idfFile2Expression = data.Substring(startIdx, from - startIdx - 1);
            string ndExpression = Name + "(" + idfFile1Expression + "," + idfFile2Expression + ")";

            IDFExpParser.IncreaseExpressionCount();
            string expId = IDFExpParser.GetCurrentExpressionId();
            if (IDFExpParser.IsDebugMode || IDFExpParser.IsIntermediateResultWritten)
            {
                IDFExpParser.Log.AddInfo("Evaluating expression '" + expId + " = " + IDFExpParser.ExpandExpressionKeys(ndExpression, variableDictionary) + "'", 1);
            }

            float noDataValue = float.NaN;
            if (idfFile1 is ConstantIDFFile)
            {
                if (idfFile2 is ConstantIDFFile)
                {
                    noDataValue = ((ConstantIDFFile) idfFile2).ConstantValue;
                }
                else
                {
                    noDataValue = idfFile2.NoDataValue;
                }
            }
            else if (idfFile2 is ConstantIDFFile)
            {
                noDataValue = ((ConstantIDFFile)idfFile2).ConstantValue;
            }

            // Redefine NoData-value of first IDF-file to second IDF-file or constant value
            IDFFile resultIDFFile = null;
            if (noDataValue.Equals(idfFile1.NoDataValue))
            {
                resultIDFFile = idfFile1;
            }
            else
            {
                resultIDFFile = idfFile1.CopyIDF(FileUtils.AddFilePostFix(idfFile1.Filename, "_nd"));
                resultIDFFile.ReplaceValues(resultIDFFile.NoDataValue, noDataValue);
                resultIDFFile.NoDataValue = noDataValue;
            }

            return resultIDFFile;
        }
    }

    /// <summary>
    /// Class for storing the result of the Split-method which splits an expression into a list of so-called “cells”.
    /// Each cell consists, primarily, of token (a number, a reference to an IDF-file by a IDF-variable or a string) and an action that must be applied to it. 
    /// </summary>
    public class IDFExpressionCell
    {
        internal IDFFile IDFFile { get; set; }
        internal string IDFId { get; set; }

        /// <summary>
        /// The action is a single/double character that can be any of the mathematical operators: or a special character denoting the end of an expression, which is hardcoded as ‘).’ 
        /// The last element in the list of cells to be merged will always have the special action ‘),’ that is, no action.
        /// </summary>
        internal string Action { get; set; }
        internal string ExpId { get; set; }

        // Currently expression depth is just a unique number for the expression, but it could be used for the depth or level of an expression that defines the indentation of logmessages
        internal int ExpDepth { get; set; }
        internal IDFExpressionType ExpressionType { get; set; }

        /// <summary>
        /// Creates an IDFExpressionCell object that consistens of a token (refering to a (constant) IDF-file) and an action.
        /// </summary>
        /// <param name="idfFile">the IDF-file (token) on the left side of an operator (action)</param>
        /// <param name="idfId">an Id-string for the IDF-file (ie. the filename)</param>
        /// <param name="action">the operator that is applied to the IDF-file on the left of it</param>
        /// <param name="expId">an Id-string that represents the expression</param>
        /// <param name="expDepth">a number for this expression</param>
        /// <param name="expressionType"></param>
        public IDFExpressionCell(IDFFile idfFile, string idfId, string action, string expId, int expDepth, IDFExpressionType expressionType)
        {
            this.IDFFile = idfFile;
            this.Action = action;
            this.IDFId = idfId;
            this.ExpId = expId;
            this.ExpDepth = expDepth;
            this.ExpressionType = expressionType;
        }
    }
}
