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

        protected IDFExpParser IDFExpressionParser { get; set; }

        /// <summary>
        /// Dictionary with all variable names (and corresponding IDF-files) that are defined up to the current line
        /// </summary>
        protected Dictionary<string, IDFFile> VariableDictionary { get; set; }

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
        /// Defines if quiet mode shoud be used: end without raising an error for missing IDF-files
        /// </summary>
        public static bool IsQuietMode { get; set; }

        /// <summary>
        /// Defines if metadata should be added to result IDF-files
        /// </summary>
        public static bool IsMetadataAdded { get; set; }

        /// <summary>
        /// Defines if cell values in result IDF-file should be rounded to defined number of decimals
        /// </summary>
        public static bool IsResultRounded { get; set; }

        /// <summary>
        /// Defines number of decimals to round to when <para>IsResultRounded</para> is true
        /// </summary>
        public static int DecimalCount { get; set; }

        public Interpreter()
        {
            VariableDictionary = null;
            ResultPathDictionary = null;
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
        protected int ProcessINIScript(string iniScript)
        {
            int exitcode = 0;
            int lineIdx = 0;
            string wholeLine = string.Empty;
            VariableDictionary = new Dictionary<string, IDFFile>();
            ResultPathDictionary = new Dictionary<string, string>();

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
            VariableDictionary.Add("NoData", noDataIDFFile);

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

                    ParseINILine(wholeLine, iniLines, lineIdx);
                }

                Log.AddInfo("Finished processing INI-file");
            }
            catch (QuietException ex)
            {
                throw ex;
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error in line " + lineIdx + ": " + ex.GetBaseException().Message + ": " + wholeLine);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading INI-file", ex);
            }

            return exitcode;
        }

        /// <summary>
        /// Parse a single INI-line with IDF-expressions
        /// </summary>
        /// <param name="wholeLine"></param>
        /// <param name="iniLines"></param>
        /// <param name="lineIdx"></param>
        protected virtual void ParseINILine(string wholeLine, string[] iniLines, int lineIdx)
        {
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
                        resultPath = Path.GetDirectoryName(variableName);
                        variableName = Path.GetFileName(variableName);
                    }

                    // Restore equality symbols
                    string expression = lineValues[1].Trim();
                    expression = expression.Replace("#@1#", "==");
                    expression = expression.Replace("#@2#", "!=");
                    expression = expression.Replace("#@3#", ">=");
                    expression = expression.Replace("#@4#", "<=");

                    Log.AddInfo("Evaluating expression at line " + lineIdx + ": '" + wholeLine + "' ...");
                    if (IsDebugMode)
                    {
                        Log.AddMessage(LogLevel.Debug, "Expanded expression: '" + Environment.ExpandEnvironmentVariables(wholeLine) + "'", 1);
                    }

                    IDFExpressionType expressionType;
                    string orgExpression = expression;
                    expression = System.Environment.ExpandEnvironmentVariables(orgExpression);
                    if (IsDebugMode && !expression.Equals(orgExpression))
                    {
                        Log.AddInfo("Expression '" + orgExpression + "' evaluated to: " + expression, 1);
                    }

                    bool isIDFFilenameVariable = false;
                    IDFFile expResultIDFFile = null;
                    if (expression.ToLower().EndsWith(".idf"))
                    {
                        // Parse a single IDF-filename which is assigned to a variable seperately and prevent writing this IDF-file to results path
                        expResultIDFFile = ParseIDFFilename(expression, out expressionType);
                        if (expResultIDFFile == null)
                        {
                            if (IsQuietMode)
                            {
                                throw new QuietException("IDF-file not found: " + expression);
                            }
                            else
                            {
                                throw new ToolException("IDF-file not found: " + expression);
                            }
                        }
                        isIDFFilenameVariable = true;
                    }
                    else
                    {
                        // Parse IDF-expression (part after the '='-symbol and assign to IDF-variable
                        expResultIDFFile = IDFExpParser.Parse(expression, VariableDictionary, out expressionType);
                    }

                    // When an expression was successfully evaluated to an IDF-file, write IDF-file to disk
                    if ((expressionType != IDFExpressionType.Undefined) && (expressionType != IDFExpressionType.Constant) && (expressionType != IDFExpressionType.File))
                    {
                        string currentOutputPath = OutputPath;
                        if (resultPath != null)
                        {
                            if (Path.IsPathRooted(resultPath))
                            {
                                currentOutputPath = resultPath;
                            }
                            else
                            {
                                currentOutputPath = Path.Combine(currentOutputPath, resultPath);
                            }
                        }
                        string outputIDFFilename = Path.Combine(currentOutputPath, variableName + ".IDF");
                        Metadata outputMetadata = new Metadata("Expression evaluation using IDF files: " + expression);
                        outputMetadata.ProcessDescription = "Automatically generated with Sweco's IDFexp-tool";
                        outputMetadata.Source = InputFilename;

                        if (!isIDFFilenameVariable)
                        {
                            if (IsResultRounded)
                            {
                                IDFFile roundedExpResultIDFFile = expResultIDFFile.CopyIDF(outputIDFFilename);
                                roundedExpResultIDFFile.RoundValues(DecimalCount);
                                if (IsMetadataAdded)
                                {
                                    roundedExpResultIDFFile.WriteFile(outputIDFFilename, outputMetadata);
                                }
                                else
                                {
                                    roundedExpResultIDFFile.WriteFile(outputIDFFilename);
                                }
                            }
                            else
                            {
                                if (IsMetadataAdded)
                                {
                                    expResultIDFFile.WriteFile(outputIDFFilename, outputMetadata);
                                }
                                else
                                {
                                    expResultIDFFile.WriteFile(outputIDFFilename);
                                }
                            }

                            // Ensure saved filename is stored, to be able to reload released values memory later
                            expResultIDFFile.Filename = outputIDFFilename;

                            Log.AddInfo("Expression file has been written to: " + Path.GetFileName(outputIDFFilename), 1);
                        }
                    }

                    // Check if a new variable was defined. If so, add it to variable dictionary, otherwise replace existing variable
                    if (VariableDictionary.ContainsKey(variableName))
                    {
                        // Replace current value
                        VariableDictionary[variableName] = expResultIDFFile;
                    }
                    else
                    {
                        VariableDictionary.Add(variableName, expResultIDFFile);
                        ResultPathDictionary.Add(variableName, resultPath);
                    }

                    // Release values from memory for all currently defined/read IDF-files
                    if (IsDebugMode)
                    {
                        long usedMemory = GC.GetTotalMemory(true) / 1000000;
                        Log.AddInfo("Allocated memory after evaluating line " + lineIdx + ": " + usedMemory + "Mb", 1);
                    }
                    foreach (IDFFile idfFile in VariableDictionary.Values)
                    {
                        idfFile.ReleaseMemory(false);
                    }
                    GC.Collect();
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

    }
}
