// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.LayerManager.LayerModels;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sweco.SIF.iMOD.IDF.IDFLog;

namespace Sweco.SIF.LayerManager
{
    /// <summary>
    /// Class for writing IDFLog to files and Excelsheet
    /// </summary>
    public class IDFLogStatsWriter
    {
        IDFLog log;

        public IDFLogStatsWriter(IDFLog log)
        {
            this.log = log;
        }

        public void WriteExcelFile()
        {
            IWorkbook workbook = null;
            ExcelManager excelManager = null;
            string excelFilename = Path.Combine(Path.GetDirectoryName(log.BaseFilename), Path.GetFileNameWithoutExtension(log.BaseFilename) + ".xlsx");
            try
            {
                excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
                workbook = excelManager.CreateWorkbook(false);

                if ((log.WarningCount > 0) || (log.ErrorCount > 0))
                {
                    IWorksheet worksheet = workbook.AddSheet("IDFLog statistics");
                    AddIssueStatsHeader(worksheet, 0);
                    int issueRowIdx = 1;

                    if (log.WarningCount > 0)
                    {
                        foreach (string warningGroupID in log.GetWarningGroupIDs())
                        {
                            IDFLogStatistics issueStats = log.GetWarningStatistics(warningGroupID);
                            WriteIssueStats(worksheet, warningGroupID, issueStats, Layer.RetrieveShortWarningLabel, ref issueRowIdx);
                        }
                    }
                    if (log.ErrorCount > 0)
                    {
                        foreach (string errorGroupID in log.GetErrorGroupIDs())
                        {
                            IDFLogStatistics issueStats = log.GetErrorStatistics(errorGroupID);
                            WriteIssueStats(worksheet, errorGroupID, issueStats, Layer.RetrieveShortErrorLabel, ref issueRowIdx);
                        }
                    }
                    worksheet.SetBorderColor(new Range(worksheet, new Cell(worksheet, 0, 0), new Cell(worksheet, issueRowIdx - 1, 7)), Color.Black, BorderWeight.Thin, true);
                    worksheet.AutoFitColumns(new Range(worksheet, 0, 0, issueRowIdx - 1, 7));
                    worksheet.SetNumberFormat(new Range(worksheet, 1, 4, issueRowIdx - 1, 7), "0.000");

                    workbook.Save(excelFilename);
                }
            }
            catch (ToolException ex)
            {
                // throw further up
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not process Excelsheet", ex);
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

        }

        private void AddIssueStatsHeader(IWorksheet worksheet, int rowIdx)
        {
            worksheet.SetCellValue(rowIdx, 0, "Base IDF-file");
            worksheet.SetCellValue(rowIdx, 1, "Postfix");
            worksheet.SetCellValue(rowIdx, 2, "Issue");
            worksheet.SetCellValue(rowIdx, 3, "Count");
            worksheet.SetCellValue(rowIdx, 4, "Min");
            worksheet.SetCellValue(rowIdx, 5, "Avg");
            worksheet.SetCellValue(rowIdx, 6, "SD");
            worksheet.SetCellValue(rowIdx, 7, "Max");
            worksheet.SetFontBold(new Range(worksheet, rowIdx, 0, rowIdx, 7), true);
        }

        private void WriteIssueStats(IWorksheet worksheet, string postfix, IDFLogStatistics issueStats, Func<int, string> retrieveShortIssueLabelMethod, ref int issueRowIdx)
        {
            IDFFile issueIDFFile = issueStats.IssueIDFFile;
            if (issueIDFFile.HasValueLargerThan(0))
            {
                List<float> uniqueIssueCodes = issueIDFFile.RetrieveUniqueValues();
                foreach (float issueCodeSum in uniqueIssueCodes)
                {
                    List<int> issueCodes = Layer.SplitIssueCodeSum((int)issueCodeSum);
                    foreach (int issueCode in issueCodes)
                    {
                        if (issueCode > 0)
                        {
                            worksheet.SetCellValue(issueRowIdx, 0, Path.GetFileNameWithoutExtension(issueIDFFile.Filename));
                            worksheet.SetCellValue(issueRowIdx, 1, postfix);
                            worksheet.SetCellValue(issueRowIdx, 2, retrieveShortIssueLabelMethod((int)issueCode));
                            worksheet.SetCellValue(issueRowIdx, 3, issueStats.GetIssueCount(issueCode));

                            List<float> issueValueList = issueStats.GetValueList(issueCode);
                            if (issueValueList != null)
                            {
                                Statistics.Statistics statistics = new Statistics.Statistics(issueValueList);
                                statistics.ComputeBasicStatistics();
                                if (statistics.Count > 0)
                                {
                                    worksheet.SetCellValue(issueRowIdx, 4, statistics.Min);
                                    worksheet.SetCellValue(issueRowIdx, 5, statistics.Mean);
                                    worksheet.SetCellValue(issueRowIdx, 6, statistics.SD);
                                    worksheet.SetCellValue(issueRowIdx, 7, statistics.Max);
                                }
                            }

                            issueRowIdx++;
                        }
                    }
                }
            }
        }
    }
}
