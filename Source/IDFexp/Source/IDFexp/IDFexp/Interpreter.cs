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
using System.Threading.Tasks;

namespace Sweco.SIF.IDFexp
{
    /// <summary>
    /// Class for parsing and evaluating a script file with IDF-expressions
    /// </summary>
    public class Interpreter
    {
        public string InputFilename { get; set; }

        protected IDFExpParser IDFExpParser { get; set; }

        /// <summary>
        /// Dictionary with all variable names (and corresponding IDF-files) that are defined up to the current line
        /// If IDFexpVariable is null for some variable, the referred result/file does not exist. 
        /// </summary>
        protected Dictionary<string, IDFExpVariable> VariableDictionary { get; set; }

        /// <summary>
        /// Dictionary with all 
        /// </summary>
        protected Dictionary<string, string> ResultPathDictionary { get; set; }
        
        /// <summary>
        /// Input files are read relative to this path
        /// </summary>
        public static string BasePath
        {
            get { return IDFExpParser.BasePath; }
            set { IDFExpParser.BasePath = value; }
        }

        /// <summary>
        /// Results are written relative to this path
        /// </summary>
        public static string OutputPath
        {
            get { return IDFExpParser.OutputPath; }
            set { IDFExpParser.OutputPath = value; }
        }

        /// <summary>
        /// Defines if parser should be run in debug mode
        /// </summary>
        public static bool IsDebugMode
        {
            get { return IDFExpParser.IsDebugMode; }
            set { IDFExpParser.IsDebugMode = value; }
        }

        /// <summary>
        /// Defines if parser should write IDF-files that result from intermediate expressions
        /// </summary>
        public static bool IsIntermediateResultWritten
        {
            get { return IDFExpParser.IsIntermediateResultWritten; }
            set { IDFExpParser.IsIntermediateResultWritten = value; }
        }

        /// <summary>
        /// Defines if NoData-value should be used as a value in expressions
        /// </summary>
        public static bool UseNoDataAsValue
        {
            get { return IDFExpParser.UseNoDataAsValue; }
            set { IDFExpParser.UseNoDataAsValue = value; }
        }

        /// <summary>
        /// When UseNoDataAsValue is true, this defines the actual value to be used instead of NoData
        /// </summary>
        public static float NoDataValue
        {
            get { return IDFExpParser.NoDataValue; }
            set { IDFExpParser.NoDataValue = value; }
        }

        /// <summary>
        /// Define expression extent (xll,yll,xur,yur). IDF-files are enlarged (with NoData-values) when smaller or clipped to this extent.
        /// </summary>
        public static Extent Extent
        {
            get { return IDFExpParser.Extent; }
            set { IDFExpParser.Extent = value; }
        }

        /// <summary>
        ///  A SIF Log object to write logmessages to
        /// </summary>
        public static Log Log
        {
            get { return IDFExpParser.Log; }
            set { IDFExpParser.Log = value; }
        }

        /// <summary>
        /// Quiet mode that shoud be used in case a missing file is referred
        /// </summary>
        public static QuietMode QuietMode { get; set; }

        /// <summary>
        /// Defines if metadata should be added to result IDF-files
        /// </summary>
        public static bool IsMetadataAdded { get; set; }

        /// <summary>
        /// Defines number of decimals to round cell values in result IDF-file
        /// </summary>
        public static int DecimalCount { get; set; }

        /// <summary>
        /// Stack datastructure to keep track of (nested) for loops
        /// </summary>
        private Stack<ForLoopDef> forLoopStack = new Stack<ForLoopDef>();

        public Interpreter()
        {
            VariableDictionary = null;
        }

        /// <summary>
        /// Ininialize interpreter and ExpParser with specified settings
        /// </summary>
        public static void Initialize()
        {
            IDFExpParser.Initialize();
        }

        /// <summary>
        /// Parses script file with IDF-expressions
        /// </summary>
        /// <returns></returns>
        public virtual int ProcessFile(string inputFilename)
        {
            this.InputFilename = inputFilename;

            // Read input text file
            StreamReader sr = null;
            string script = null;
            try
            {
                sr = new StreamReader(InputFilename);
                script = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading input file: " + InputFilename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            // Process input file
            return ProcessScript(script);
        }

        /// <summary>
        /// Parse a (multiline) string from a file with expressions
        /// </summary>
        /// <param name="inputFileString"></param>
        /// <returns></returns>
        protected virtual int ProcessScript(string inputFileString)
        {
            int exitCode;
            switch (Path.GetExtension(InputFilename).ToLower())
            {
                case ".ini":
                    exitCode = ProcessINIScript(inputFileString);
                    break;
                default:
                    throw new ToolException("Unknown input file extension: " + Path.GetExtension(InputFilename));
            }

            return exitCode;
        }

        /// <summary>
        /// Parse a (multiline) string from an INI-file with IDF-expressions
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="iniScript"></param>
        /// <returns></returns>
        protected virtual int ProcessINIScript(string iniScript)
        {
            int exitcode = 0;
            int lineIdx = 0;
            string wholeLine = string.Empty;
            if (VariableDictionary == null)
            {
                VariableDictionary = new Dictionary<string, IDFExpVariable>();
            }

            // Split string in single lines
            string[] iniLines = iniScript.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (iniLines == null)
            {
                throw new ToolException("Empty INI-file");
            }

            // Define a ConstantIDFFile that corresponds to the NoData string in expressions
            ConstantIDFFile noDataIDFFile = new ConstantIDFFile(-9999);
            noDataIDFFile.NoDataValue = -9999;
            if (UseNoDataAsValue)
            {
                noDataIDFFile.NoDataCalculationValue = (IDFExpParser.NoDataValue.Equals(float.NaN)) ? noDataIDFFile.NoDataValue : IDFExpParser.NoDataValue;
            }

            RegisterVariable(VariableDictionary, "NoData", noDataIDFFile, IDFExpressionType.Constant);
            RegisterVariable(VariableDictionary, "NaN", new ConstantIDFFile(float.NaN), IDFExpressionType.Constant);

            if (IsDebugMode)
            {
                // In debug mode, write the input INI-file with all environment variables expanded
                WriteExpandedINIFile(iniLines);
            }

            // Check input INI-file for syntax errors at a level higher scale than a single line 
            INIChecker iniScriptChecker = new INIChecker(iniLines);
            iniScriptChecker.Check();

            try
            {
                // Process all lines in INI-file
                while (lineIdx < iniLines.Length)
                {
                    wholeLine = iniLines[lineIdx++].Trim();
                    while (wholeLine.EndsWith("_") && (lineIdx < iniLines.Length))
                    {
                        wholeLine = wholeLine.Substring(0, wholeLine.Length - 1) + iniLines[lineIdx++].Trim();
                    }

                    ParseINILine(wholeLine, iniLines, ref lineIdx);
                }

                if (Log.Warnings.Count > 0)
                {
                    Log.AddInfo("Finished processing INI-file (with warnings)");
                }
                else
                {
                    Log.AddInfo("Finished processing INI-file");
                }
            }
            catch (QuietException ex)
            {
                throw ex;
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error in line " + lineIdx + ": " + ex.GetBaseException().Message); //  + "\nLine " + lineIdx + ": " + wholeLine);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading INI-file", ex);
            }

            return exitcode;
        }

        /// <summary>
        /// Add an IDFExpVariable with specified property sto the variable dictionary. If already existing it is replaced with specified properties.
        /// </summary>
        /// <param name="variableDictionary"></param>
        /// <param name="name"></param>
        /// <param name="idfFile"></param>
        /// <param name="expressionType"></param>
        /// <param name="prefix"></param>
        /// <param name="metadata"></param>
        protected virtual void RegisterVariable(Dictionary<string, IDFExpVariable> variableDictionary, string name, IDFFile idfFile, IDFExpressionType expressionType, string prefix = null, Metadata metadata = null)
        {
            if (idfFile != null)
            {
                idfFile.UseLazyLoading = SIFToolSettings.UseLazyLoading;
            }

            IDFExpVariable idfExpVariable = CreateIDFExpVariable(name, idfFile, expressionType, prefix, metadata);
            if (variableDictionary.ContainsKey(name))
            {
                // Replace current value
                variableDictionary[name] = idfExpVariable;
            }
            else
            {
                variableDictionary.Add(name, idfExpVariable);
            }
        }

        /// <summary>
        /// Create IDFExpVariable object with specified properties
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idfFile"></param>
        /// <param name="expressionType"></param>
        /// <param name="prefix"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        protected virtual IDFExpVariable CreateIDFExpVariable(string name, IDFFile idfFile, IDFExpressionType expressionType, string prefix, Metadata metadata)
        {
            return new IDFExpVariable(name, idfFile, expressionType, prefix, metadata);
        }

        /// <summary>
        /// Parse a single INI-line with IDF-expressions
        /// </summary>
        /// <param name="wholeLine"></param>
        /// <param name="iniLines"></param>
        /// <param name="lineIdx"></param>
        protected virtual void ParseINILine(string wholeLine, string[] iniLines, ref int lineIdx)
        {
            // Parse expressions with preconditions '#IF <cond>:' and effectively remove precondition part
            wholeLine = HandleForLoops(wholeLine, forLoopStack);
            wholeLine = ParsePreconditions(wholeLine, lineIdx, Log, 1);

            if (!wholeLine.Equals(string.Empty))
            {
                if (wholeLine.ToLower().StartsWith("rem ") || wholeLine.StartsWith("//") || wholeLine.StartsWith("'"))
                {
                    // Parse comment line, skip when not in debug mode
                    if (IsDebugMode)
                    {
                        // In debug mode, also log line with comments
                        if (wholeLine.ToLower().StartsWith("rem"))
                        {
                            wholeLine = wholeLine.Substring(3).Trim();
                        }
                        else if (wholeLine.StartsWith("//"))
                        {
                            wholeLine = wholeLine.Substring(2).Trim();
                        }
                        else
                        {
                            wholeLine = wholeLine.Substring(1).Trim();
                        }

                        Log.AddInfo("Remark: " + wholeLine);
                    }
                }
                else if (wholeLine.ToLower().StartsWith("for "))
                {
                    // parse start of new FOR-loop
                    string wholeLineExpanded = System.Environment.ExpandEnvironmentVariables(wholeLine);

                    ForLoopDef forLoopDef = ForLoopDef.Parse(wholeLineExpanded, lineIdx);
                    if (forLoopDef.Idx < forLoopDef.LoopValues.Count)
                    {
                        forLoopStack.Push(forLoopDef);
                        Log.AddInfo("FOR-loop '" + forLoopDef.VarName + "' started: " + wholeLine);
                    }
                    else
                    {
                        Log.AddInfo("FOR-loop skipped: " + wholeLine);
                        int forcount = 1;
                        do
                        {
                            wholeLine = iniLines[lineIdx++].Trim();
                            if (wholeLine.ToLower().StartsWith("for "))
                            {
                                forcount++;
                            }
                            else if (wholeLine.ToLower().StartsWith("endfor"))
                            {
                                forcount--;
                            }
                        } while ((forcount > 0) && (lineIdx < iniLines.Length));
                    }
                }
                else if (wholeLine.ToLower().Equals("endfor"))
                {
                    // Parse ENDFOR-statement to continue or finish the current FOR-loop
                    ForLoopDef forLoopDef = forLoopStack.Pop();
                    forLoopDef.Idx++;
                    if (forLoopDef.Idx < forLoopDef.LoopValues.Count)
                    {
                        // Continue this FOR-loop
                        forLoopStack.Push(forLoopDef);
                        lineIdx = forLoopDef.LineIdx;

                        if (IsDebugMode)
                        {
                            Log.AddInfo("FOR-loop '" + forLoopDef.VarName + "' continued: " + wholeLine + " (value: " + forLoopDef.LoopValues[forLoopDef.Idx] + ")");
                        }
                    }
                }
                else if (wholeLine.Contains("="))
                {
                    // Parse assignment of expression to IDF-variable

                    // Check for single equal signs, and ignore double equal signs (==) which are allowed: Note use temporary character sequences to be able to split on '='-symbol
                    if (wholeLine.Contains("#@1#") || wholeLine.Contains("#@2#") || wholeLine.Contains("#@3#") || wholeLine.Contains("#@4#"))
                    {
                        throw new ToolException("Line " + lineIdx + " is not allowed to contain '#@*#'-character succesions, with * any symbol: " + wholeLine);
                    }
                    string tmpWholeline = wholeLine.Replace("==", "#@1#").Replace("!=", "#@2#").Replace(">=", "#@3#").Replace("<=", "#@4#");
                    string[] lineValues = tmpWholeline.Split('=');
                    if (lineValues.Length != 2)
                    {
                        throw new ToolException("Exactly one equal sign is expected");
                    }

                    // Part before '='-symbol is seen as the name of the IDF-variable to which is assigned the result of the expression
                    string variableName = lineValues[0].Trim();
                    variableName = System.Environment.ExpandEnvironmentVariables(variableName);

                    // Check if a subdirectory was specified before the variable name
                    string resultPath = null;
                    if (variableName.Contains(Path.DirectorySeparatorChar))
                    {
                        // Allow preprocessing constructs at beginning of parsed lines that are ended by a colon
                        if (variableName.Contains(":"))
                        {
                            // Skip part before colon
                            int colonIdx = variableName.IndexOf(":");
                            variableName = variableName.Substring(colonIdx + 1).Trim();
                        }
                        resultPath = Path.GetDirectoryName(variableName);
                        variableName = Path.GetFileName(variableName);
                    }

                    // Remove optional IDF extension from variable name, all other substrings with a dot or extensions are left
                    if (Path.GetExtension(variableName.ToLower()).Equals(".idf"))
                    {
                        variableName = Path.GetFileNameWithoutExtension(variableName);
                    }

                    // Restore equality symbols
                    string expression = lineValues[1].Trim();
                    expression = expression.Replace("#@1#", "==");
                    expression = expression.Replace("#@2#", "!=");
                    expression = expression.Replace("#@3#", ">=");
                    expression = expression.Replace("#@4#", "<=");

                    if (IsDebugMode)
                    {
                        Log.AddInfo("Evaluating expression at line " + lineIdx + ": '" + wholeLine + "' ...");
                        Log.AddInfo("Expanded expression: '" + Environment.ExpandEnvironmentVariables(wholeLine) + "'", 1);
                    }
                    else
                    {
                        Log.AddInfo("Evaluating expression at line " + lineIdx + ": '" + Environment.ExpandEnvironmentVariables(wholeLine) + "' ...");
                    }

                    IDFExpressionType expressionType;
                    string orgExpression = expression;
                    expression = System.Environment.ExpandEnvironmentVariables(orgExpression);
                    if (IsDebugMode && !expression.Equals(orgExpression))
                    {
                        Log.AddInfo("Expression '" + orgExpression + "' evaluated to: " + expression, 1);
                    }

                    IDFFile expResultIDFFile = null;
                    Metadata expResultMetadata = null;
                    if (expression.ToLower().EndsWith(".idf"))
                    {
                        // Parse a single IDF-filename which is assigned to a variable seperately and prevent writing this IDF-file to results path
                        expResultIDFFile = IDFExpParser.ParseIDFFilename(expression, out expressionType);
                        if (expResultIDFFile == null)
                        {
                            if (QuietMode == QuietMode.SilentExit)
                            {
                                throw new QuietException("IDF-file not found: " + Environment.ExpandEnvironmentVariables(expression));
                            }
                            else if (QuietMode == QuietMode.SilentSkip)
                            {
                                Log.AddWarning("Silently skipping definition of variable '" + variableName + "' that refers to a missing IDF-file ...", 1);
                            }
                            else
                            {
                                throw new ToolException("IDF-file not found: " + Environment.ExpandEnvironmentVariables(expression));
                            }
                        }
                    }
                    else
                    {
                        // Parse IDF-expression (part after the '='-symbol and assign to IDF-variable
                        expressionType = IDFExpressionType.Undefined;
                        if (VariableDictionary.ContainsKey(expression))
                        {
                            // Expression is equal to an existing variable
                            expResultIDFFile = CopyIDFVariable(VariableDictionary, expression, out expressionType);
                        }
                        else
                        {
                            // Parse expression
                            try
                            {
                                expResultIDFFile = IDFExpParser.Parse(expression, VariableDictionary, out expressionType);
                            } 
                            catch (NullReferenceException ex)
                            {
                                if (QuietMode == QuietMode.SilentSkip)
                                {
                                    // ignore here, show warning in next block
                                }
                                else
                                {
                                    throw ex;
                                }
                            }
                        }

                        if (expResultIDFFile == null)
                        {
                            if (QuietMode == QuietMode.SilentSkip)
                            {
                                Log.AddWarning("Silently skipping definition of variable '" + variableName + "' that refers to a skipped variable ...", 1);
                            }
                            else
                            {
                                throw new Exception("Unexpected error in expression");
                            }
                        }
                        else if ((expressionType != IDFExpressionType.Undefined) && (expressionType != IDFExpressionType.Constant) && (expressionType != IDFExpressionType.File))
                        {
                            string currentOutputPath = OutputPath;
                            if (resultPath != null)
                            {
                                currentOutputPath = (Path.IsPathRooted(resultPath)) ? resultPath : Path.Combine(currentOutputPath, resultPath);
                            }
                            expResultIDFFile.Filename = Path.Combine(currentOutputPath, variableName + ".IDF");

                            if (DecimalCount >= 0)
                            {
                                expResultIDFFile.RoundValues(DecimalCount);
                            }

                            if (IsMetadataAdded)
                            {
                                expResultMetadata = new Metadata("Expression evaluation using IDF files: " + expression);
                                expResultMetadata.ProcessDescription = "Automatically generated with Sweco's IDFexp-tool";
                                if (DecimalCount >= 0)
                                {
                                    expResultMetadata.ProcessDescription += "; Values are rounded to " + DecimalCount + " decimals";
                                }
                                expResultMetadata.Source = InputFilename;
                            }
                        }
                    }

                    // (Re)save variable in dictionary: save new variable, replace existing variable
                    RegisterVariable(VariableDictionary, variableName, expResultIDFFile, expressionType, resultPath, expResultMetadata);

                    // Handle memory and persistance management: for all currently defined/read IDF-variables check if values should be released from memory 
                    if (IsDebugMode)
                    {
                        long usedMemory = GC.GetTotalMemory(true) / 1000000;
                        Log.AddInfo("Allocated memory after evaluating line " + lineIdx + ": " + usedMemory + "Mb", 1);
                    }
                    ManageMemory(VariableDictionary, Log);
                    if (IsDebugMode)
                    {
                        long usedMemory = GC.GetTotalMemory(true) / 1000000;
                        Log.AddInfo("Allocated memory after releasing memory: " + usedMemory + "Mb", 1);
                    }
                }
                else
                {
                    throw new ToolException("Invalid expression");
                }
            }
        }

        /// <summary>
        /// Parse line with preconditions which always start with a '#'-symbol. Check tool usage for valid syntax.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineIdx"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        protected virtual string ParsePreconditions(string line, int lineIdx, Log log, int logIndentLevel)
        {
            string parsedLine = line;
            if (line.ToLower().StartsWith("#if"))
            {
                int colonIdx = FindLastPreconditionColonIndex(line, lineIdx);
                if (colonIdx >= 0)
                {
                    bool isInverseCondition = false;
                    string preconditionString = line.Substring(0, colonIdx);
                    parsedLine = line.Remove(0, colonIdx + 1).Trim();
                    string[] preconditionParts = CommonUtils.SplitQuoted(preconditionString, ' ', '"', true, true); //.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (preconditionParts.Length == 1)
                    {
                        throw new ToolException("Invalid precondition in line " + (lineIdx + 1) + ": " + preconditionString);
                    }
                    if (preconditionParts[1].ToLower().Equals("not"))
                    {
                        isInverseCondition = true;
                        preconditionParts = preconditionString.Replace(preconditionParts[1], string.Empty).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    parsedLine = ParsePrecondition(parsedLine, preconditionString, preconditionParts, isInverseCondition, lineIdx, log, logIndentLevel);
                }
                else
                {
                    throw new ToolException("Invalid precondition in line " + (lineIdx + 1) + ", missing colon-symbol (':'): " + line);
                }
            }

            if (parsedLine.ToLower().StartsWith("#if"))
            {
                parsedLine = ParsePreconditions(parsedLine, lineIdx, log, 1);
            }

            return parsedLine;
        }

        protected virtual string ParsePrecondition(string parsedLine, string preconditionString, string[] preconditionParts, bool isInverseCondition, int lineIdx, Log log, int logIndentLevel)
        {
            if (preconditionParts.Length == 3)
            {
                switch (preconditionParts[1].ToLower())
                {
                    case "exist":
                        // Check existance of directory or file
                        string path = preconditionParts[2].Replace("\"", string.Empty);
                        path = Environment.ExpandEnvironmentVariables(path);
                        if (!Path.IsPathRooted(path) && (BasePath != null))
                        {
                            path = Path.Combine(BasePath, path);
                        }
                        path = System.Environment.ExpandEnvironmentVariables(path);
                        if ((!isInverseCondition && (!Directory.Exists(path) && !File.Exists(path)))
                            || (isInverseCondition && !(!Directory.Exists(path) && !File.Exists(path))))
                        {
                            if (IsDebugMode)
                            {
                                log.AddInfo("Skipping line: " + parsedLine, logIndentLevel);
                                log.AddInfo("evaluated precondition '" + preconditionString + "': path " + (!isInverseCondition ? "not " : "") + "found: " + path, logIndentLevel);
                            }
                            parsedLine = string.Empty;
                        }
                        break;
                    default:
                        throw new ToolException("Unknown precondition keyword in line " + (lineIdx + 1) + ": " + preconditionParts[1]);
                }
            }
            else
            {
                throw new ToolException("Invalid precondition in line " + (lineIdx + 1) + ": " + preconditionString);
            }

            return parsedLine;
        }

        /// <summary>
        /// Retrieve index of last colon in a line with a precondition
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineIdx"></param>
        /// <returns></returns>
        protected int FindLastPreconditionColonIndex(string line, int lineIdx)
        {
            int colonIndx = -1;

            // Find first singl� '='-symbol
            string tmpLine = line.Replace("==", "XX");
            int equalSignIdx = tmpLine.IndexOf("=");
            if (equalSignIdx >= 0)
            {
                // Retrieve first part of line, before equal sign
                tmpLine = line.Substring(0, equalSignIdx);
                colonIndx = tmpLine.LastIndexOf(":");
            }
            else
            {
                throw new ToolException("Invalid expression in line " + (lineIdx + 1) + ", missing equal-symbol ('='): " + line);
            }

            return colonIndx;
        }

        protected virtual IDFFile CopyIDFVariable(Dictionary<string, IDFExpVariable> variableDictionary, string expression, out IDFExpressionType expressionType)
        {
            IDFExpVariable idfExpVariable = variableDictionary[expression];
            IDFFile idfFile = idfExpVariable.IDFFile;
            expressionType = IDFExpressionType.Variable;
            if (idfFile != null)
            {
                idfFile.EnsureLoadedValues();
                return idfFile.CopyIDF(null);
            }
            else
            {
                return null;
            }
        }

        protected virtual void ManageMemory(Dictionary<string, IDFExpVariable> variableDictionary, Log log)
        {
            foreach (IDFExpVariable idfExpVariable in variableDictionary.Values)
            {
                idfExpVariable.Persist(true, log);
                idfExpVariable.ReleaseMemory();
            }
            GC.Collect();
        }

        /// <summary>
        /// Parse an expression that consists of an IDF-filename and path
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="expressionType"></param>
        /// <returns></returns>
        protected virtual IDFFile ParseIDFFilename(string expression, out IDFExpressionType expressionType)
        {
            return IDFExpParser.ParseIDFFilename(expression, out expressionType);
        }

        /// <summary>
        /// Expand environment variables in specified strings and write to file in output path with same name as input filename, but with '_expanded' postfix
        /// </summary>
        /// <param name="iniLines"></param>
        protected void WriteExpandedINIFile(string[] iniLines)
        {
            string expandedINIFilename = Path.Combine(OutputPath, FileUtils.AddFilePostFix(Path.GetFileName(InputFilename), "_expanded"));

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(expandedINIFilename);
                foreach (string line in iniLines)
                {
                    sw.WriteLine(System.Environment.ExpandEnvironmentVariables(line));
                }

                Log.AddInfo("INI-file with expanded environment variables written to: " + expandedINIFilename);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing expanded INI-file: " + expandedINIFilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Check if line contains FOR-loop indices which will be replaced by the current iteration number of the corresponding FOR-loop
        /// </summary>
        /// <param name="wholeLine"></param>
        /// <param name="forLoopStack"></param>
        /// <returns></returns>
        private string HandleForLoops(string wholeLine, Stack<ForLoopDef> forLoopStack)
        {
            // Check FOR-loop variables to replace with current values
            for (int forLoopIdx = 0; forLoopIdx < forLoopStack.Count(); forLoopIdx++)
            {
                ForLoopDef forLoopDef = forLoopStack.ElementAt(forLoopIdx);
                string varName = forLoopDef.VarName;
                string varValueString = forLoopDef.LoopValues[forLoopDef.Idx];

                // Check for expressions with forloop indices such as '%%(i+1)'
                int indexExpIdx = wholeLine.IndexOf("%%(" + varName);
                while (indexExpIdx >= 0)
                {
                    int parenthesisEndIdx = wholeLine.IndexOf(")", indexExpIdx + 1);
                    if (parenthesisEndIdx > 0)
                    {
                        string indexExp = wholeLine.Substring(indexExpIdx, parenthesisEndIdx - indexExpIdx + 1);
                        // Check that loopvalue is an integer
                        if (int.TryParse(varValueString, out int varValue))
                        {
                            string indexExpValue = ParseIndexExp(indexExp, varName, varValue);
                            wholeLine = wholeLine.Replace(indexExp, indexExpValue);
                        }
                        else
                        {
                            throw new ToolException("Index expression in FOR-loop is not allowed for non-numeric loopvalues: " + varValueString);
                        }
                    }
                    indexExpIdx = wholeLine.IndexOf("%%(" + varName, indexExpIdx + 1);
                }

                // Check for expressions with forloop indices such as '%%00i', which will pad index value with zeroes
                indexExpIdx = wholeLine.IndexOf("%%0");
                while (indexExpIdx >= 0)
                {
                    int varNameIdx = indexExpIdx + 3;
                    // Find first non-zero character
                    while ((varNameIdx < wholeLine.Length) && (wholeLine[varNameIdx] == '0'))
                    {
                        varNameIdx++;
                    }
                    // int startIdx = indexExpIdx + 1;
                    if ((varNameIdx < wholeLine.Length) && (wholeLine[varNameIdx].ToString().Equals(varName)))
                    {
                        int digitCount = varNameIdx - indexExpIdx - 2;
                        string indexExp = wholeLine.Substring(indexExpIdx, varNameIdx - indexExpIdx + 1);
                        wholeLine = wholeLine.Replace(indexExp, varValueString.PadLeft(digitCount, '0'));
                        //    startIdx = indexExpIdx + digitCount;
                    }
                    indexExpIdx = wholeLine.IndexOf("%%0", indexExpIdx + 1);
                }

                // Replace (normal) forloop variable references
                wholeLine = wholeLine.Replace("%%" + forLoopDef.VarName, varValueString);
            }

            return wholeLine;
        }

        /// <summary>
        /// Parse simple expressions within FOR-loop indices, which is used to refer to values relative to current index
        /// </summary>
        /// <param name="indexExp"></param>
        /// <param name="varName"></param>
        /// <param name="varValue"></param>
        /// <returns></returns>
        private string ParseIndexExp(string indexExp, string varName, int varValue)
        {
            string simpleIndexExp = indexExp.Substring(3, indexExp.Length - 4);
            int opIdx = simpleIndexExp.IndexOfAny(new char[] { '+', '-', '*', '/' });
            string value2String = simpleIndexExp.Substring(opIdx + 1, simpleIndexExp.Length - opIdx - 1);
            if (!int.TryParse(value2String, out int value2))
            {
                throw new ToolException("Could not parse integer value in index expression: " + indexExp);
            }
            string operatorString = simpleIndexExp.Substring(opIdx, 1);
            int resultValue;
            switch (operatorString)
            {
                case "+":
                    resultValue = varValue + value2;
                    break;
                case "-":
                    resultValue = varValue - value2;
                    break;
                case "*":
                    resultValue = varValue * value2;
                    break;
                case "/":
                    resultValue = varValue + value2;
                    break;
                default:
                    throw new ToolException("Unknown operator (" + operatorString + ") in index expression: " + indexExp);
            }

            return resultValue.ToString();
        }
    }
}
