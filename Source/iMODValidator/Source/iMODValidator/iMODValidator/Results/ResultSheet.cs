// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Common.Properties;
using Sweco.SIF.GIS;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Sweco.SIF.iMODValidator.Results
{
    public class ResultSheet : ResultTable
    {
        protected const string LogIssuesSheetname = "Logissues";

        protected SpreadsheetManager sheetManager;
        public SpreadsheetManager SheetManager
        {
            get { return sheetManager; }
            set { sheetManager = value; }
        }

        /// <summary>
        /// Final message that is shown when nothing is found
        /// </summary>
        public string NoIssuesMessage { get; set; } = "No issues found";

        protected const int RunfilePart1RowIdx = 4;
        protected const int RunfilePart2RowIdx = 5;
        protected const int ValidationDateRowIdx = 6;
        protected const int ValidationExtentRowIdx = 7;
        protected const int LogMessagesHyperlinkRowIdx = 8;
        protected const string ValidationExtentPrefix = "Extent: ";
        protected const string ValidationRunfileNamePrefix = "Statistics for model: ";
        protected const string ValidationRunfilePathPrefix = "Modelpath: ";
        protected const int CheckSummaryTableHeaderRowIdx = 11;
        protected const int CheckSummaryTableHeaderColIdx = 2;
        protected const string CheckSummaryTitle = "Summary per check";
        protected const int CheckSummaryCheckNameRelColIdx = 0;
        protected const int CheckSummaryPackageRelColIdx = 2;
        protected const int CheckSummaryStressPeriodRelColIdx = 3;
        protected const int CheckSummaryLayerRelColIdx = 4;
        protected const int CheckSummaryFirstResultTypeRelColIdx = 5;
        protected const string CheckSummaryResultTotalCountPrefix = "Total\n";
        protected const int MsgSummaryPackageRelColIdx = 0;
        protected const int MsgSummaryLayerRelColIdx = 1;
        protected const int MsgSummaryFileRelColIdx = 2;
        protected const int MsgSummaryMessageRelColIdx = 5;
        protected const int MsgSummaryResultCountRelColIdx = 8;
        protected const string MsgSummaryTitlePrefix = "Summary per ";
        protected const string MsgSummaryTitlePostfix = "message";

        protected bool isResultShown;
        protected Log log;

        public ResultSheet(SpreadsheetManager sheetManager) : base()
        {
            this.sheetManager = sheetManager;
            this.isResultShown = false;
            this.log = null;
        }

        public ResultSheet(SpreadsheetManager sheetManager, string baseModelFilename, Extent extent) : base(baseModelFilename, extent)
        {
            this.sheetManager = sheetManager;
            this.isResultShown = false;
            this.log = null;
            this.tableCreator = null;
        }

        public override void Export(Log log)
        {
            try
            {
                CreateResultWorksheet(null, log);
            }
            catch (Exception ex)
            {
                log.AddInfo("Export to " + sheetManager.ApplicationName + " didn't succeed, exporting to ASCII instead...");
                base.Export(log);
                if (!(ExceptionHandler.ExceptionChainContains(ex, "Could not load file or assembly")))
                {
                    throw new Exception("Unknown error while exporting to " + sheetManager.ApplicationName, ex);
                }
            }
        }

        public override void Export(string exportFilename, Log log, bool isResultShown = true)
        {
            try
            {
                exportFilename = Path.Combine(Path.GetDirectoryName(exportFilename), Path.GetFileNameWithoutExtension(exportFilename) + ".xlsx");
                CreateResultWorksheet(exportFilename, log, isResultShown);
            }
            catch (Exception ex)
            {
                log.AddInfo("Export to " + sheetManager.ApplicationName + " didn't succeed, exporting to ASCII instead...");
                base.Export(log);
                if (!(ExceptionHandler.ExceptionChainContains(ex, "Could not load file or assembly")))
                {
                    throw new Exception("Unknown error while exporting to " + sheetManager.ApplicationName, ex);
                }
            }
        }

        protected virtual void CreateResultWorksheet(string exportFilename, Log log, bool isSheetShown = true)
        {
            this.isResultShown = isSheetShown;
            this.log = log;

            try
            {
                if (sheetManager == null)
                {
                    throw new Exception("No SpreadsheetManager was defined, ResultSheet cannot be created");
                }

                sheetManager.SetVisibility(isSheetShown);
                sheetManager.SetScreenUpdating(false);
                IWorkbook workbook = sheetManager.CreateWorkbook(true, "iMODValidatorReport");
                IWorksheet worksheet = workbook.Sheets[0];
                worksheet.Zoom(80);
                worksheet.SetBorderColor(new Range(worksheet, 0, 0, 2047,64), Color.White, BorderWeight.Thin, true);
                // worksheet.SetBorderColor(Color.White);
                Color headerInteriorColor = Color.FromArgb(0, 112, 192);

                // Get the unique resultTypes over all rows
                List<string> resultTypes = GetResultTypeList();
                List<int> minColumnWidths = new List<int>();
                if (resultTypes.Count > 0)
                {
                    // Set the column headers as defined
                    worksheet.MergeCells(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryCheckNameRelColIdx,
                        CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryPackageRelColIdx - 1);
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryCheckNameRelColIdx, mainTypeColumnName);
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryPackageRelColIdx, subTypeColumnName);
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryStressPeriodRelColIdx, "Stress\nperiod");
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryLayerRelColIdx, "Layer\nnr");

                    // Add LayerStatistics-specific columns
                    string[] resultColumnHeaders = layerStatisticsList[0].GetResultColumnHeaders(CheckSummaryResultTotalCountPrefix, resultTypes);
                    for (int resultSubColIdx = 0; resultSubColIdx < resultColumnHeaders.Count(); resultSubColIdx++)
                    {
                        string columnHeader = resultColumnHeaders[resultSubColIdx];
                        int colIdx = CheckSummaryTableHeaderColIdx + CheckSummaryFirstResultTypeRelColIdx + resultSubColIdx;
                        worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, colIdx, columnHeader);
                        SetColumnWidth(worksheet, minColumnWidths, colIdx, columnHeader);
                    }

                    Range headerRange = null;
                    headerRange = new Range(worksheet, CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx,
                        CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx + CheckSummaryFirstResultTypeRelColIdx + resultColumnHeaders.Length - 1);
                    worksheet.SetFontBold(headerRange, true);
                    worksheet.SetInteriorColor(headerRange, headerInteriorColor);
                    worksheet.SetFontColor(headerRange, Color.White);
                    worksheet.SetBorderColor(headerRange, Color.Black, BorderWeight.Thin);
                    worksheet.SetVerticalAlignmentTop(headerRange);
                    worksheet.SetHorizontalAlignmentLeft(headerRange);
                    worksheet.SetWrapText(headerRange, true);
                    // worksheet.AutoFitColumns(headerRange);

                    layerStatisticsList.Sort();
                    // Set the actual values
                    int firstRowIdx = CheckSummaryTableHeaderRowIdx + 1;
                    for (int i = 0; i < layerStatisticsList.Count; i++)
                    {
                        worksheet.MergeCells(firstRowIdx + i, CheckSummaryTableHeaderColIdx + 0, firstRowIdx + i, CheckSummaryTableHeaderColIdx + 1);
                        worksheet.SetCellValue(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryCheckNameRelColIdx, layerStatisticsList[i].MainType);
                        worksheet.SetCellValue(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryPackageRelColIdx, layerStatisticsList[i].SubType);
                        worksheet.SetHorizontalAlignmentLeft(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryStressPeriodRelColIdx);
                        worksheet.SetCellValue(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryStressPeriodRelColIdx, layerStatisticsList[i].StressperiodString);
                        worksheet.SetCellValue(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryLayerRelColIdx, layerStatisticsList[i].Ilay.ToString());

                        long[] resultValues = layerStatisticsList[i].GetResultValues(resultTypes);
                        for (int resultSubColIdx = 0; resultSubColIdx < resultValues.Count(); resultSubColIdx++)
                        {
                            long resultValue = resultValues[resultSubColIdx];
                            worksheet.SetCellValue(firstRowIdx + i, CheckSummaryTableHeaderColIdx + CheckSummaryFirstResultTypeRelColIdx + resultSubColIdx, resultValue);
                        }
                    }

                    // Do table layout
                    int lastRowIdx = CheckSummaryTableHeaderRowIdx + layerStatisticsList.Count;
                    int lastColIdx = CheckSummaryTableHeaderColIdx + CheckSummaryFirstResultTypeRelColIdx + resultColumnHeaders.Length - 1;
                    Range tableValuesRange = new Range(worksheet, firstRowIdx, CheckSummaryTableHeaderColIdx, lastRowIdx, lastColIdx);
                    worksheet.SetBorderEdgeBottomColor(tableValuesRange, Color.Gray);

                    // Add title of summarytable
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx - 1, CheckSummaryTableHeaderColIdx, CheckSummaryTitle);
                    worksheet.SetFontItalic(CheckSummaryTableHeaderRowIdx - 1, CheckSummaryTableHeaderColIdx, true);

                    //////////////////////////////
                    // Add tables with messages //
                    //////////////////////////////

                    foreach (string resultType in resultTypes)
                    {
                        if (GetTotalMessageCount(resultType) != 0)
                        {
                            // Set the column header
                            int resultTableHeaderRowIdx = lastRowIdx + 3;
                            int resultTableHeaderColIdx = CheckSummaryTableHeaderColIdx;

                            worksheet.SetCellValue(resultTableHeaderRowIdx, resultTableHeaderColIdx, messageTypeColumnName);
                            SetColumnWidth(worksheet, minColumnWidths, resultTableHeaderColIdx, messageTypeColumnName, 1.2f);

                            worksheet.SetCellValue(resultTableHeaderRowIdx, resultTableHeaderColIdx + 1, "Layer\nnr");
                            worksheet.MergeCells(resultTableHeaderRowIdx, resultTableHeaderColIdx + 2, resultTableHeaderRowIdx, resultTableHeaderColIdx + 4);
                            worksheet.SetCellValue(resultTableHeaderRowIdx, resultTableHeaderColIdx + 2, resultType + "file");
                            worksheet.MergeCells(resultTableHeaderRowIdx, resultTableHeaderColIdx + 5, resultTableHeaderRowIdx, resultTableHeaderColIdx + 7);
                            worksheet.SetCellValue(resultTableHeaderRowIdx, resultTableHeaderColIdx + 5, resultType + "message");

                            string columnHeader = "Total " + resultType.ToLower() + "s";
                            int colIdx = resultTableHeaderColIdx + 8;
                            worksheet.SetCellValue(resultTableHeaderRowIdx, colIdx, columnHeader);
                            SetColumnWidth(worksheet, minColumnWidths, colIdx, columnHeader);

                            headerRange = new Range(worksheet, resultTableHeaderRowIdx, resultTableHeaderColIdx, resultTableHeaderRowIdx, resultTableHeaderColIdx + 8);
                            worksheet.SetFontBold(headerRange, true);
                            worksheet.SetInteriorColor(headerRange, headerInteriorColor);
                            worksheet.SetFontColor(headerRange, Color.White);
                            worksheet.SetBorderColor(headerRange, Color.Black, BorderWeight.Thin);
                            worksheet.SetVerticalAlignmentTop(headerRange);

                            // Resize columns to fit the data up to now
                            // worksheet.AutoFitColumns(headerRange);

                            // Set the actual values of the errormessage table
                            firstRowIdx = resultTableHeaderRowIdx + 1;
                            lastRowIdx = resultTableHeaderRowIdx;
                            for (int i = 0; i < layerStatisticsList.Count; i++)
                            {
                                if (layerStatisticsList[i].ResultLayerStatisticsDictionary.ContainsKey(resultType))
                                {
                                    SortedDictionary<string, long> resultTypeMsgCountDictionary = layerStatisticsList[i].ResultLayerStatisticsDictionary[resultType].MessageCountDictionary;
                                    foreach (string errorMessage in resultTypeMsgCountDictionary.Keys)
                                    {
                                        lastRowIdx++;
                                        long count = resultTypeMsgCountDictionary[errorMessage];
                                        worksheet.SetCellValue(lastRowIdx, resultTableHeaderColIdx + 0, layerStatisticsList[i].MessageType);
                                        worksheet.SetHorizontalAlignmentLeft(lastRowIdx, resultTableHeaderColIdx + 1);
                                        worksheet.SetCellValue(lastRowIdx, resultTableHeaderColIdx + 1, layerStatisticsList[i].Ilay);
                                        worksheet.MergeCells(lastRowIdx, resultTableHeaderColIdx + 2, lastRowIdx, resultTableHeaderColIdx + 4);
                                        string resultFilename = layerStatisticsList[i].GetResultTypeStatistics(resultType).ResultFilename;
                                        worksheet.SetCellValue(lastRowIdx, resultTableHeaderColIdx + 2, Path.GetFileName(resultFilename));
                                        worksheet.MergeCells(lastRowIdx, resultTableHeaderColIdx + 5, lastRowIdx, resultTableHeaderColIdx + 7);
                                        worksheet.SetCellValue(lastRowIdx, resultTableHeaderColIdx + 5, errorMessage);
                                        worksheet.SetCellValue(lastRowIdx, resultTableHeaderColIdx + 8, count.ToString());
                                    }
                                }
                            }

                            // Add title of errortable
                            worksheet.SetCellValue(resultTableHeaderRowIdx - 1, CheckSummaryTableHeaderColIdx, MsgSummaryTitlePrefix + resultType.ToLower() + MsgSummaryTitlePostfix);
                            worksheet.SetFontItalic(resultTableHeaderRowIdx - 1, CheckSummaryTableHeaderColIdx, true);
                        }
                    }

                    // Do table layout
                    lastColIdx = CheckSummaryTableHeaderColIdx + 8;
                    tableValuesRange = new Range(worksheet, firstRowIdx, CheckSummaryTableHeaderColIdx, lastRowIdx, lastColIdx);
                    worksheet.SetBorderEdgeBottomColor(tableValuesRange, Color.Gray);
                }
                else
                {
                    worksheet.SetCellValue(CheckSummaryTableHeaderRowIdx, CheckSummaryTableHeaderColIdx, NoIssuesMessage);
                }

                // Now add other items since columnresize has been done now
                worksheet.SetCellValue(1, 1, tableTitle);
                worksheet.SetFontBold(1, 1, true);
                worksheet.SetFontSize(1, 1, 14);
                worksheet.SetCellValue(2, 1, (tableCreator != null) ? "Author: " + tableCreator : string.Empty);
                worksheet.SetFontBold(1, 1, true);
                worksheet.SetFontSize(1, 1, 14);

                worksheet.SetColumnWidth(0, 2);
                worksheet.SetColumnWidth(1, 2);

                worksheet.SetCellValue(RunfilePart1RowIdx, 1, GetModelDescription1());
                worksheet.SetCellValue(RunfilePart2RowIdx, 1, GetModelDescription2());

                worksheet.SetCellValue(ValidationDateRowIdx, 1, "Validationdate: " + DateTime.Now.ToShortDateString());
                worksheet.SetCellValue(ValidationExtentRowIdx, 1, ValidationExtentPrefix + ((extent != null) ? extent.ToString() : string.Empty));
                Range someRange = new Range(worksheet, RunfilePart1RowIdx, 1, ValidationExtentRowIdx, 1);
                worksheet.SetFontBold(someRange, true);

                if ((logErrors.Count > 0) || (log.Warnings.Count > 0))
                {
                    SheetManager.SetScreenUpdating(true);

                    IWorksheet worksheet2 = workbook.AddSheet(LogIssuesSheetname);
                    worksheet2.Activate();
                    worksheet2.SetSheetname(LogIssuesSheetname);
                    worksheet2.Zoom(80);
                    worksheet2.SetColumnWidth(0, 2);
                    worksheet2.SetColumnWidth(1, 10);
                    worksheet2.SetColumnWidth(2, 10);
                    worksheet2.SetColumnWidth(3, 30);
                    worksheet2.SetColumnWidth(4, 150);

                    worksheet2.SetCellValue(1, 1, "Issues from logfile that ask for attention");
                    worksheet2.SetCellValue(2, 1, "Type");
                    worksheet2.SetCellValue(2, 2, "Package");
                    worksheet2.SetCellValue(2, 3, "Filename");
                    worksheet2.SetCellValue(2, 4, "Message");
                    Range headerRange = new Range(worksheet2, 2, 1, 2, 4);
                    worksheet2.SetFontBold(headerRange, true);
                    worksheet2.SetInteriorColor(headerRange, headerInteriorColor);
                    worksheet2.SetFontColor(headerRange, Color.White);
                    worksheet2.SetBorderColor(headerRange, Color.Black, BorderWeight.Thin);
                    worksheet2.SetVerticalAlignmentTop(headerRange);

                    int startRowIdx = 3;
                    for (int idx = 0; idx < logErrors.Count; idx++)
                    {
                        LogMessage logEntry = logErrors[idx];
                        worksheet2.SetCellValue(idx + startRowIdx, 1, "Error");
                        worksheet2.SetCellValue(idx + startRowIdx, 2, (logEntry.Category != null) ? logEntry.Category : string.Empty);
                        worksheet2.SetCellValue(idx + startRowIdx, 3, (logEntry.Filename != null) ? Path.GetFileName(logEntry.Filename) : string.Empty);
                        worksheet2.SetCellValue(idx + startRowIdx, 4, logEntry.Message);
                    }
                    startRowIdx += logErrors.Count;
                    for (int idx = 0; idx < logWarnings.Count; idx++)
                    {
                        LogMessage logEntry = logWarnings[idx];
                        worksheet2.SetCellValue(idx + startRowIdx, 1, "Warning");
                        worksheet2.SetCellValue(idx + startRowIdx, 2, (logEntry.Category != null) ? logEntry.Category : string.Empty);
                        worksheet2.SetCellValue(idx + startRowIdx, 3, (logEntry.Filename != null) ? Path.GetFileName(logEntry.Filename) : string.Empty);
                        worksheet2.SetCellValue(idx + startRowIdx, 4, logEntry.Message);
                    }

                    worksheet.Activate();
                    string logIssuesString = "Additional issues reported in the logfile: " + logWarnings.Count + " warnings and " + logErrors.Count + " errors";
                    worksheet.MergeCells(LogMessagesHyperlinkRowIdx, 1, LogMessagesHyperlinkRowIdx, 8);
                    worksheet.SetHyperlink(LogMessagesHyperlinkRowIdx, 1, null, LogIssuesSheetname + "!A1", null, logIssuesString);
                }
                else
                {
                    string logIssuesString = "No issues reported in the logfile";
                    worksheet.SetCellValue(LogMessagesHyperlinkRowIdx, 1, logIssuesString);
                }

                sheetManager.SetScreenUpdating(false);
                // worksheet.SetPageLayoutView();
                // worksheet.ApplyPageSetup("iMODValidator Report", "Sweco", Resources.SwecoIcon.ToBitmap()); // retrieve logo from resources
                // worksheet.SetPageOrientationLandscape();
                // worksheet.SetPapersizeA4();
                worksheet.Zoom(80);

                if (log != null)
                {
                    log.AddInfo("Saving statistics ...");
                }
                workbook.Save(exportFilename);
                if (log != null)
                {
                    log.AddInfo("Statistics have been saved in output path to: " + Path.GetFileName(exportFilename), 1);
                }
                workbook.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while creating result sheet for '" + Path.GetFileName(exportFilename) + "'", ex);
            }
            finally
            {
                sheetManager.Cleanup();
            }

            try
            {
                if (isResultShown)
                {
                    System.Diagnostics.Process.Start(exportFilename);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while opening Excelsheet", ex);
            }
        }

        /// <summary>
        /// Set column width of specified column to width as specified in list with column width for all columns.
        /// As a minimum the width of the column name is used (for a specified/default symbol width) 
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="columnWidths">list of (current) widths; this is updated with the new width</param>
        /// <param name="colIdx">the index of the updated column width</param>
        /// <param name="columnHeader">the column name of the updated column, to calculate width for specified symbol width</param>
        /// <param name="avgSymbolWidth">the average symbol width to calculate column width from number of symbols in column header</param>
        private void SetColumnWidth(IWorksheet worksheet, List<int> columnWidths, int colIdx, string columnHeader, float avgSymbolWidth = 1.4f)
        {
            ExtendList(columnWidths, colIdx, 0);
            int minColWidth = CommonUtils.Max(columnWidths[colIdx], (int)GetMinColumnWidth(columnHeader, avgSymbolWidth));
            columnWidths[colIdx] = minColWidth;
            worksheet.SetColumnWidth(colIdx, minColWidth);
        }

        private void ExtendList<T>(List<T> list, int minCount, T newValue)
        {
            for (int idx = list.Count; idx <= minCount; idx++)
            {
                list.Add(newValue);
            }

        }

        private float GetMinColumnWidth(string columnHeader, float avgSymbolWidth = 1.4f)
        {
            string[] lineStrings = columnHeader.Split('\n');
            int maxCharCount = 0;
            for (int lineIdx = 0; lineIdx < lineStrings.Length; lineIdx++)
            {
                if (lineStrings[lineIdx].Length > maxCharCount)
                {
                    maxCharCount = lineStrings[lineIdx].Length;
                }
            }
            return maxCharCount * avgSymbolWidth;
        }

        protected virtual string GetModelDescription1()
        {
            return ValidationRunfileNamePrefix + Path.GetFileName(baseModelFilename);
        }

        protected virtual string GetModelDescription2()
        {
            return ValidationRunfilePathPrefix + Path.GetDirectoryName(baseModelFilename);
        }

        public void Import(string resultSheetFilename, Log log)
        {
            this.log = log;
            try
            {
                sheetManager.SetVisibility(false);
                sheetManager.SetScreenUpdating(false);
                IWorkbook workbook = sheetManager.OpenWorkbook(resultSheetFilename);
                IWorksheet worksheet = workbook.Sheets[0];

                // Check that sheet contains a validation result
                string runfileCellValue = worksheet.GetCellValue(RunfilePart1RowIdx, 1);
                if (!runfileCellValue.Contains(ValidationRunfileNamePrefix))
                {
                    throw new Exception("Sheet doesn't contain validation results: " + resultSheetFilename);
                }
                this.baseModelFilename = resultSheetFilename;
                string runfileName = runfileCellValue.Replace(ValidationRunfileNamePrefix, string.Empty);
                string runfilePath = worksheet.GetCellValue(4, 1).Replace(ValidationRunfilePathPrefix, string.Empty);
                string extentString = worksheet.GetCellValue(6, 1).Replace(ValidationExtentPrefix, string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
                string[] coordinates = extentString.Substring(1, extentString.Length - 2).Split(',');
                try
                {
                    this.extent = new Extent(int.Parse(coordinates[0]), int.Parse(coordinates[1]), int.Parse(coordinates[2]), int.Parse(coordinates[3]));
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not parse extentstring in sheetfile: " + extentString, ex);
                }

                this.BaseRunfilename = Path.Combine(runfilePath, runfileName);

                Cell summaryTitleCell = worksheet.FindCell(CheckSummaryTitle);
                if (summaryTitleCell == null)
                {
                    throw new Exception("Invalid format of ResultSheet file, title not found: '" + CheckSummaryTitle + "'");
                }
                int rowIdx = summaryTitleCell.RowIdx + 1;
                int firstColIdx = summaryTitleCell.ColIdx;

                // Retrieve available resultypes
                List<string> resultTypes = new List<string>();
                int resultTypeColIdx = firstColIdx + CheckSummaryFirstResultTypeRelColIdx;
                while ((worksheet.GetCellValue(rowIdx, resultTypeColIdx) != null) && !worksheet.GetCellValue(rowIdx, resultTypeColIdx).Equals(string.Empty))
                {
                    string resultType = worksheet.GetCellValue(rowIdx, resultTypeColIdx).Replace(CheckSummaryResultTotalCountPrefix, string.Empty);
                    resultTypes.Add(resultType.Substring(0, 1).ToUpper() + resultType.Substring(1, resultType.Length - 1).ToLower());
                    resultTypeColIdx += 2;
                }

                // Retrieve (total) results per check
                rowIdx += 1;
                while ((worksheet.GetCellValue(rowIdx, firstColIdx) != null) && !worksheet.GetCellValue(rowIdx, firstColIdx).Equals(string.Empty))
                {
                    string checkName = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryCheckNameRelColIdx);
                    string packageName = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryPackageRelColIdx);
                    string stressPeriodString = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryStressPeriodRelColIdx);
                    string layerNumberString = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryLayerRelColIdx);
                    int layerNumber;
                    if (!int.TryParse(layerNumberString, out layerNumber))
                    {
                        throw new Exception("Unexpected layernumber: " + layerNumberString);
                    }
                    LayerStatistics layerStat = new LayerStatistics(null, checkName, packageName, packageName, layerNumber, stressPeriodString);

                    for (int resultTypeIdx = 0; resultTypeIdx < resultTypes.Count(); resultTypeIdx++)
                    {
                        string resultType = resultTypes[resultTypeIdx];
                        string totalResultTypeCount = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryFirstResultTypeRelColIdx + 2 * resultTypeIdx);
                        string totalResultTypeLocCount = worksheet.GetCellValue(rowIdx, firstColIdx + CheckSummaryFirstResultTypeRelColIdx + 2 * resultTypeIdx + 1);
                        ResultLayerStatistics resultTypeStatistic = new ResultLayerStatistics(resultType, null, int.Parse(totalResultTypeCount), int.Parse(totalResultTypeLocCount));
                        layerStat.ResultLayerStatisticsDictionary.Add(resultType, resultTypeStatistic);
                    }
                    AddRow(layerStat);
                    rowIdx++;
                }

                // Retrieve (message) results per ResultType
                rowIdx++;
                while ((worksheet.GetCellValue(rowIdx, firstColIdx) != null) && !worksheet.GetCellValue(rowIdx, firstColIdx).Equals(string.Empty))
                {
                    string title = worksheet.GetCellValue(rowIdx, firstColIdx);
                    string resultType = title.Replace(MsgSummaryTitlePrefix, string.Empty).Replace(MsgSummaryTitlePostfix, string.Empty);
                    resultType = resultType.Substring(0, 1).ToUpper() + resultType.Substring(1, resultType.Length - 1).ToLower();
                    rowIdx += 2;
                    while ((worksheet.GetCellValue(rowIdx, firstColIdx) != null) && !worksheet.GetCellValue(rowIdx, firstColIdx).Equals(string.Empty))
                    {
                        string packageName = worksheet.GetCellValue(rowIdx, firstColIdx + MsgSummaryPackageRelColIdx);
                        string layerNumberString = worksheet.GetCellValue(rowIdx, firstColIdx + MsgSummaryLayerRelColIdx);
                        string filename = worksheet.GetCellValue(rowIdx, firstColIdx + MsgSummaryFileRelColIdx);
                        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                        string stressPeriodString = filenameWithoutExtension.Substring(filenameWithoutExtension.Length - 8, 8);
                        long stressPeriodValue = 0;
                        if (stressPeriodString != null)
                        {
                            if (!long.TryParse(stressPeriodString, out stressPeriodValue))
                            {
                                stressPeriodString = null;
                            }
                        }
                        string message = worksheet.GetCellValue(rowIdx, firstColIdx + MsgSummaryMessageRelColIdx);
                        string resultCountString = worksheet.GetCellValue(rowIdx, firstColIdx + MsgSummaryResultCountRelColIdx);
                        int layerNumber;
                        if (!int.TryParse(layerNumberString, out layerNumber))
                        {
                            throw new Exception("Unexpected layernumber: " + layerNumberString);
                        }
                        int resultCount = int.Parse(resultCountString);

                        LayerStatistics layerStatistics = GetLayerStatistics(packageName, layerNumber, stressPeriodString);
                        if (layerStatistics == null)
                        {
                            throw new Exception("Layerstatistics not found for " + resultType + "message of package " + packageName + ", layer " + layerNumber + ", stress period " + stressPeriodString);
                        }
                        ResultLayerStatistics resultLayerStatistics = layerStatistics.ResultLayerStatisticsDictionary[resultType];
                        resultLayerStatistics.ResultFilename = filename;
                        resultLayerStatistics.AddMessage(message, resultCount);
                        rowIdx++;
                    }
                    rowIdx++;
                }

                workbook.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while creating iMODValidator " + sheetManager.ApplicationName + " file", ex);
            }
            finally
            {
                sheetManager.Cleanup();
            }
        }
    }
}
