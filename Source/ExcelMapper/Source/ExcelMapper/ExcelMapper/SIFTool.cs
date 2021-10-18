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
using Sweco.SIF.Common;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.ExcelMapper
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        protected static IWorksheet FormulaSheet { get; set; }
        protected string InsertedString { get; set; }
        protected string BaseString { get; set; }
        protected string AppendedString { get; set; }

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for mapping data from Excel rows to textfile";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            StringBuilder outputString = new StringBuilder();

            ExcelManager excelManager = null;
            IWorkbook workbook = null;
            try
            {
                excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);

                // Retrieve insert string
                InsertedString = null;
                bool isInsertStringFile = false;
                if (settings.InsertedString != null)
                {
                    InsertedString = RetrieveString(settings.InsertedString);
                    if (!InsertedString.Equals(settings.InsertedString))
                    {
                        Log.AddInfo("Inserted string from file:");
                        Log.AddInfo(InsertedString);
                        isInsertStringFile = true;
                    }
                }

                // Retrieve base string(s)
                BaseString = RetrieveString(settings.BaseString);
                bool isBaseStringFile = false;
                if (!BaseString.Equals(settings.BaseString))
                {
                    Log.AddInfo("Base string from file:");
                    Log.AddInfo(BaseString);
                    isBaseStringFile = true;
                }

                // Retrieve append string(s)
                AppendedString = null;
                bool isAppendStringFile = false;
                if (settings.AppendedString != null)
                {
                    AppendedString = RetrieveString(settings.AppendedString);
                    if (!AppendedString.Equals(settings.AppendedString))
                    {
                        Log.AddInfo("Appended string from file:");
                        Log.AddInfo(AppendedString);
                        isAppendStringFile = true;
                    }
                }

                // Open Excelsheet
                Log.AddInfo("Reading Excelsheet '" + Path.GetFileName(settings.ExcelFilename) + "' ...");
                workbook = excelManager.OpenWorkbook(settings.ExcelFilename);
                IWorksheet sheet = workbook.GetSheet(settings.SheetNumber - 1);

                // Split base string line in seperate lines
                List<List<int>> colIndicesList = new List<List<int>>();
                List<List<string>> colFormulasList = new List<List<string>>();
                if (!isBaseStringFile)
                {
                    BaseString = BaseString.Replace("\\n", "\n").Replace("\\r", "\r");
                }
                string[] baseStringLines = BaseString.Replace("\r\n", "\n").Split(new char[] { '\n', '\r' });

                // Parse and correct base string lines
                int maxColIdx = -1;
                string[] corrBaseStringLines;
                List<string> corrBaseStringLinesList = new List<string>();
                for (int lineIdx = 0; lineIdx < baseStringLines.Length; lineIdx++)
                {
                    string corrBaseStringLine = ParseMappingString(baseStringLines[lineIdx], sheet, ref maxColIdx, out List<int> colIndices, out List<string> colFormulas);
                    if (corrBaseStringLine != null)
                    {
                        corrBaseStringLinesList.Add(corrBaseStringLine);
                        colIndicesList.Add(colIndices);
                        colFormulasList.Add(colFormulas);
                    }
                }
                corrBaseStringLines = corrBaseStringLinesList.ToArray();
                List<int> colMapping = RetrieveColumnMapping(settings, maxColIdx);

                Log.AddInfo("Processing rows from Excelsheet ...");

                // Determine startrow
                int startRowIdx = settings.StartRow - 1;
                int rowIdx = startRowIdx;
                if (settings.IsBottomUp)
                {
                    rowIdx = startRowIdx;
                    while (!IsEmptyRow(sheet, rowIdx, colMapping))
                    {
                        rowIdx++;
                    }
                    rowIdx--;
                }

                // Parse all Excel rows until a row is found with empty cells for all cells in specified columns
                string prevOutputLine = string.Empty;
                while (!IsEmptyRow(sheet, rowIdx, colMapping) && (rowIdx >= startRowIdx))
                {
                    for (int lineIdx = 0; lineIdx < corrBaseStringLines.Length; lineIdx++)
                    {
                        string corrBasestringLine = corrBaseStringLines[lineIdx];
                        List<int> colIndices = colIndicesList[lineIdx];
                        List<string> colFormulas = colFormulasList[lineIdx];

                        // Retrieve column values for current row based on specified column mapping and formulas
                        string[] colValues = new string[colIndices.Count];
                        bool hasEmptyValue = false;
                        for (int i = 0; i < colIndices.Count; i++)
                        {
                            int colIdx = colMapping[colIndices[i]];
                            string cellValue = sheet.GetCellValue(rowIdx, colIdx);
                            if ((cellValue == null) || cellValue.Equals(string.Empty))
                            {
                                // one of the columns specified in the base string is empty
                                hasEmptyValue = true;
                            }
                            if ((cellValue != null) && (colFormulas[i] != null))
                            {
                                cellValue = ProcessFormula(cellValue, colFormulas[i]);
                            }
                            colValues[i] = cellValue;
                        }

                        string outputLine = string.Format(corrBasestringLine, colValues).Trim();
                        if (!hasEmptyValue)
                        {
                            // When none of the requested column values have empty values for this row and base string line, write base string line with column values from this row

                            if (settings.IsVarExpanded)
                            {
                                outputLine = Environment.ExpandEnvironmentVariables(outputLine);
                            }
                            if (settings.ReplacedCellStrings.Count > 0)
                            {
                                for (int i = 0; i < settings.ReplacedCellStrings.Count; i++)
                                {
                                    outputLine = outputLine.Replace(settings.ReplacedCellStrings[i], settings.ReplacementCellStrings[i]);
                                }
                            }
                            if (!settings.IsDoubleEmptyLineRemoved || !(outputLine.Equals(string.Empty) && prevOutputLine.Equals(string.Empty)))
                            {
                                outputString.AppendLine(outputLine);
                                prevOutputLine = outputLine;
                            }
                        }
                        else
                        {
                            Log.AddInfo("Row " + (rowIdx + 1) + ", line " + (lineIdx + 1) + " is skipped because of empty values: " + outputLine, 1);
                        }
                    }

                    if (settings.IsBottomUp)
                    {
                        rowIdx--;
                    }
                    else
                    {
                        rowIdx++;
                    }
                }

                // Write result to file
                StreamWriter sw = null;
                try
                {
                    Log.AddInfo("Writing output file '" + Path.GetFileName(settings.OutputFilename) + "' ...");
                    sw = new StreamWriter(settings.OutputFilename, settings.IsMerged);

                    // Write insert string if specified 
                    if (InsertedString != null)
                    {
                        if (!isInsertStringFile)
                        {
                            InsertedString = InsertedString.Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        string[] insertedStringLines = InsertedString.Replace("\r\n", "\n").Split(new char[] { '\n', '\r' });
                        for (int lineIdx = 0; lineIdx < insertedStringLines.Length; lineIdx++)
                        {
                            maxColIdx = -1;
                            string corrInsertedStringLine = ParseMappingString(insertedStringLines[lineIdx], sheet, ref maxColIdx, out List<int> colIndices, out List<string> colFormulas);
                            if (maxColIdx != -1)
                            {
                                throw new ToolException("Column indices are not allowed in inserted string, only cell references: " + insertedStringLines[lineIdx]);
                            }
                            if (corrInsertedStringLine != null)
                            {
                                sw.WriteLine(corrInsertedStringLine.Replace("\\n", "\n"));
                            }
                        }
                    }

                    // Write evaluated row strings
                    sw.Write(outputString.ToString());

                    // Write append string if specified 
                    if (AppendedString != null)
                    {
                        if (!isAppendStringFile)
                        {
                            AppendedString = AppendedString.Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        string[] appendedStringLines = AppendedString.Replace("\r\n", "\n").Split(new char[] { '\n', '\r' });
                        for (int lineIdx = 0; lineIdx < appendedStringLines.Length; lineIdx++)
                        {
                            maxColIdx = -1;
                            string corrAppendedString = ParseMappingString(appendedStringLines[lineIdx], sheet, ref maxColIdx, out List<int> colIndices, out List<string> colFormulas);
                            if (maxColIdx != -1)
                            {
                                throw new ToolException("Column indices are not allowed in inserted string, only cell references: " + appendedStringLines[lineIdx]);
                            }

                            sw.WriteLine(corrAppendedString.Replace("\\n", "\n"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not write output file: " + settings.OutputFilename, ex);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }
            finally
            {
                if (workbook != null)
                {
                    workbook.Close();
                }
                if (excelManager != null)
                {
                    excelManager.Cleanup();
                }
            }

            ToolSuccessMessage = "Finished processing";

            return exitcode;
        }

        /// <summary>
        /// Retrieve column mapping between column indices in template and column indices in Excelsheet. Currently this is a default mapping to same column indices.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual List<int> RetrieveColumnMapping(SIFToolSettings settings, int maxColIdx)
        {
            // define default columm mapping
            List<int> colMapping = colMapping = new List<int>();
            for (int colIdx = 0; colIdx <= maxColIdx; colIdx++)
            {
                colMapping.Add(colIdx);
            }

            return colMapping;
        }

        /// <summary>
        /// Process specified column formulas with specified values
        /// </summary>
        /// <param name="colValues"></param>
        /// <param name="colFormulas"></param>
        protected virtual void ProcessFormulas(string[] colValues, List<string> colFormulas)
        {
            for (int colIdx = 0; colIdx < colValues.Length; colIdx++)
            {
                colValues[colIdx] = ProcessFormula(colValues[colIdx], colFormulas[colIdx]);
            }
        }

        /// <summary>
        /// Checks if all cells from specified columns are empty in given row
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="rowIdx"></param>
        /// <param name="colMapping"></param>
        /// <returns></returns>
        protected bool IsEmptyRow(IWorksheet sheet, int rowIdx, List<int> colMapping)
        {
            for (int parIdx = 0; parIdx < colMapping.Count; parIdx++)
            {
                int colIdx = colMapping[parIdx];
                if (!sheet.IsEmpty(rowIdx, colIdx))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Parse a single basestring template line and fill in values from specified sheet
        /// </summary>
        /// <param name="baseString"></param>
        /// <param name="sheet"></param>
        /// <param name="maxParIdx"></param>
        /// <param name="colIndices"></param>
        /// <param name="colFormulas"></param>
        /// <returns></returns>
        protected virtual string ParseMappingString(string baseString, IWorksheet sheet, ref int maxParIdx, out List<int> colIndices, out List<string> colFormulas)
        {
            string corrBaseString = string.Empty; // copy of baseString but with lowered colIndices (zero based instead of one based)
            int idx1 = baseString.IndexOf('{');
            int idx2 = -1;
            colIndices = new List<int>();
            colFormulas = new List<string>();
            while (idx1 >= 0)
            {
                corrBaseString += baseString.Substring(idx2 + 1, idx1 - idx2 - 1);
                idx2 = baseString.IndexOf('}', idx1 + 1);
                string parIdxString = baseString.Substring(idx1 + 1, idx2 - idx1 - 1);
                int parIdx = -1;

                // Always expand environment variables in column/cell definitions
                parIdxString = Environment.ExpandEnvironmentVariables(parIdxString);

                // First handle formula part if present
                string formulaString = null;
                if (parIdxString.StartsWith("="))
                {
                    int parenthesisIdx1 = baseString.IndexOf('(', idx1 + 2);
                    if ((parenthesisIdx1 < 0) || (parenthesisIdx1 >= idx2))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", missing opening parenthesis '(': " + parIdxString);
                    }
                    int parenthesisIdx2 = baseString.IndexOf(')', idx1 + 2);
                    if ((parenthesisIdx2 < 0) || (parenthesisIdx2 >= idx2))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", missing closing parenthesis ')': " + parIdxString);
                    }
                    if (parenthesisIdx2 <= parenthesisIdx1)
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", invalid parenthesis order '()': " + parIdxString);
                    }

                    if (idx2 != (parenthesisIdx2 + 1))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", closing parenthesis expected immediately before closing brace: " + parIdxString);
                    }

                    formulaString = baseString.Substring(idx1 + 2, parenthesisIdx1 - idx1 - 2);
                    parIdxString = baseString.Substring(parenthesisIdx1 + 1, parenthesisIdx2 - parenthesisIdx1 - 1).Trim();

                    if (parIdxString.Equals(string.Empty))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", column index between parenthesis is empty: " + parIdxString);
                    }

                    // check that there are no more parenthesis characters within parIdxString
                    parenthesisIdx1 = baseString.IndexOf('(', parenthesisIdx1 + 1);
                    parenthesisIdx2 = baseString.IndexOf(')', parenthesisIdx2 + 1);
                    if ((parenthesisIdx1 >= 0) && (parenthesisIdx1 < idx2))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", not more than one parenthesis '(' is allowed per cell value: " + parIdxString);
                    }
                    if ((parenthesisIdx2 >= 0) && (parenthesisIdx2 < idx2))
                    {
                        throw new ToolException("Invalid formula at position " + (idx1 + 1) + ", not more than one parenthesis ')' is allowed per cell value: " + parIdxString);
                    }
                }

                if (!int.TryParse(parIdxString, out parIdx))
                {
                    // check for A1-cell format: retrieve column part with letters
                    int charIdx = 0;
                    string columnString = string.Empty;
                    while ((charIdx < parIdxString.Length) && (((parIdxString[charIdx] >= 'a') && (parIdxString[charIdx] <= 'z')) || ((parIdxString[charIdx] >= 'A') && (parIdxString[charIdx] <= 'Z'))))
                    {
                        columnString += parIdxString[charIdx++];
                    }
                    int cellColNr = Sweco.SIF.Spreadsheets.SpreadsheetUtils.NumberFromExcelColumn(columnString);
                    if (cellColNr <= 0)
                    {
                        throw new ToolException("Invalid column index at position " + (idx1 + 1) + ", cell reference or positive integer expected: " + parIdxString);
                    }

                    if (charIdx < parIdxString.Length)
                    {
                        // Check for row number, part before optional End-symbols
                        string rowIdxString = string.Empty;
                        while ((charIdx < parIdxString.Length) && (parIdxString[charIdx] >= '0') && (parIdxString[charIdx] <= '9'))
                        {
                            rowIdxString += parIdxString[charIdx++];
                        }

                        if (!int.TryParse(rowIdxString, out int cellRowNr))
                        {
                            throw new ToolException("Invalid cell reference at position " + (idx1 + 1) + ", cell reference or positive integer expected: " + parIdxString);
                        }

                        string cellValue = null;
                        if (charIdx < parIdxString.Length)
                        {
                            string endSymbol = parIdxString.Substring(charIdx);
                            if (endSymbol.Equals(">"))
                            {
                                cellValue = sheet.GetCell(cellRowNr - 1, cellColNr - 1).End(CellDirection.ToRight).Value;
                            }
                            else if (endSymbol.Equals("<"))
                            {
                                cellValue = sheet.GetCell(cellRowNr - 1, cellColNr - 1).End(CellDirection.ToLeft).Value;
                            }
                            else if (endSymbol.Equals("v"))
                            {
                                cellValue = sheet.GetCell(cellRowNr - 1, cellColNr - 1).End(CellDirection.Down).Value;
                            }
                            else if (endSymbol.Equals("^"))
                            {
                                cellValue = sheet.GetCell(cellRowNr - 1, cellColNr - 1).End(CellDirection.Up).Value;
                            }
                            else
                            {
                                throw new ToolException("Invalid cell reference at position " + (idx1 + 1) + ", one of '<','>','v','^' expected: " + parIdxString);
                            }
                        }
                        else
                        {
                            cellValue = sheet.GetCellValue(cellRowNr - 1, cellColNr - 1);
                        }
                        if ((cellValue != null) && !cellValue.Equals(string.Empty))
                        {
                            if (formulaString != null)
                            {
                                cellValue = ProcessFormula(cellValue, formulaString);
                            }
                            corrBaseString += cellValue;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        // no row number present after alphabetic column characters, parse as a column parameter
                        parIdx = cellColNr - 1;
                        corrBaseString += "{" + colIndices.Count + "}";
                        colIndices.Add(parIdx);
                        colFormulas.Add(formulaString);
                        if (parIdx > maxParIdx)
                        {
                            maxParIdx = parIdx;
                        }
                    }
                }
                else if (parIdx > 0)
                {
                    // Par between braces was an integer; convert from one based to zero based
                    parIdx--;
                    corrBaseString += "{" + colIndices.Count + "}";

                    colIndices.Add(parIdx);
                    colFormulas.Add(formulaString);
                    if (parIdx > maxParIdx)
                    {
                        maxParIdx = parIdx;
                    }
                }
                else
                {
                    throw new ToolException("Invalid column index at position " + (idx1 + 1) + ", cell reference or positive integer expected: " + parIdxString);
                }

                idx1 = baseString.IndexOf('{', idx2 + 1);
            }
            corrBaseString += baseString.Substring(idx2 + 1);

            return corrBaseString;
        }

        /// <summary>
        /// Process any simple Excel-function with one parameter value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="excelFunctionName"></param>
        /// <returns></returns>
        protected virtual string ProcessFormula(string value, string excelFunctionName)
        {
            if (FormulaSheet == null)
            {
                ExcelManager excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
                IWorkbook workbook = excelManager.CreateWorkbook();
                FormulaSheet = workbook.Sheets[0];
            }

            string formula = null;
            if (double.TryParse(value, out double testValue) || DateTime.TryParse(value, out DateTime testDate))
            {
                formula = excelFunctionName + "(" + value + ")";
            }
            else
            {
                formula = excelFunctionName + "(\"" + value + "\")";
            }
            FormulaSheet.SetCellFormula(0, 0, formula);
            FormulaSheet.Calculate();
            string resultValue = FormulaSheet.GetCellValue(0, 0);
            return resultValue;
        }

        /// <summary>
        /// Retrieves string from a file or if the file does not exist simply return the specified string
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected string RetrieveString(string filenameOrString)
        {
            string fullString;
            if (File.Exists(filenameOrString))
            {
                Stream stream = null;
                StreamReader sr = null;
                string fileString = string.Empty;
                try
                {
                    stream = File.Open(filenameOrString, FileMode.Open, FileAccess.Read);
                    sr = new StreamReader(stream);
                    fileString = sr.ReadToEnd();
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not open file: " + filenameOrString, ex);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    if (sr != null)
                    {
                        sr.Close();
                    }
                }

                fullString = fileString;
            }
            else
            {
                fullString = filenameOrString;
            }

            return fullString;
        }
    }
}
