// iMODstats is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODstats.
// 
// iMODstats is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODstats is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODstats. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats
{
    /// <summary>
    /// Class for storing raster statistics in a spreadsheet
    /// </summary>
    public class StatisticssResult
    {
        protected const string NoResultsMessage = "No results found";

        protected SIFToolSettings settings;
        protected SpreadsheetManager spreadsheetManager;
        protected List<string> statColumnnames;
        protected List<string> statNumberFormats;
        protected List<string> statComments;
        protected IWorkbook workbook;
        protected IWorksheet worksheet1;
        protected int firstRowIdx1;
        protected int firstStatColIdx1;
        protected int headerRowIdx1;
        protected int rowIdx1;
        protected Log log;

        protected Dictionary<string, List<ZoneStatistics>> fileStats;

        /// <summary>
        /// Create empty StatsResult object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="fileStats"></param>
        /// <param name="log"></param>
        public StatisticssResult(SIFToolSettings settings, Dictionary<string, List<ZoneStatistics>> fileStats, Log log)
        {
            this.settings = settings;
            this.fileStats = fileStats;
            this.log = log;

            spreadsheetManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
        }

        /// <summary>
        /// Initialize results
        /// </summary>
        public virtual void Initialize()
        {
            firstRowIdx1 = 1;
            firstStatColIdx1 = 1;
            headerRowIdx1 = 4;

            if (File.Exists(settings.OutputFile) && !settings.IsOverwrite)
            {
                throw new ToolException("Outputfile exists and option 'overwrite' is not used: " + settings.OutputFile);
            }

            // Retrieve statistic columnnames and corresponding number formats and comments
            List<ZoneStatistics> fileStats0 = fileStats.First().Value;
            statColumnnames = (fileStats0.Count > 0) ? fileStats0[0].StatColumnnames : null;
            statNumberFormats = (fileStats0.Count > 0) ? fileStats0[0].StatNumberFormats : null;
            statComments = (fileStats0.Count > 0) ? fileStats0[0].StatComments : null;

            workbook = spreadsheetManager.CreateWorkbook(true, "iMOD-stats");
            worksheet1 = workbook.Sheets[0];

            worksheet1.SetCellValue(headerRowIdx1, 0, "Filename");
            if (statColumnnames != null)
            {
                for (int idx = 0; idx < statColumnnames.Count; idx++)
                {
                    worksheet1.SetCellValue(headerRowIdx1, idx + firstStatColIdx1, statColumnnames[idx]);
                    worksheet1.SetComment(headerRowIdx1, idx + firstStatColIdx1, statComments[idx]);
                }
                worksheet1.SetFontBold(new Range(worksheet1, headerRowIdx1, 0, headerRowIdx1, statColumnnames.Count + firstStatColIdx1), true);
            }

            rowIdx1 = headerRowIdx1 + firstRowIdx1;
        }

        /// <summary>
        /// Add zone statistics for specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="stats"></param>
        public virtual void AddZoneStats(string filename, ZoneStatistics stats)
        {
            worksheet1.SetCellValue(rowIdx1, 0, Path.GetFileNameWithoutExtension(filename));

            for (int statIdx = 0; statIdx < stats.StatList.Count; statIdx++)
            {
                worksheet1.SetCellValue(rowIdx1, statIdx + firstStatColIdx1, stats.StatList[statIdx]);
            }

            rowIdx1++;
        }

        /// <summary>
        /// Write result table to specified Excel file (use extension .xlsx)
        /// </summary>
        /// <param name="filename"></param>
        public void WriteFile(string filename)
        {
            if (workbook != null)
            {
                workbook.Save(filename);
                workbook.Close();
            }
        }

        /// <summary>
        /// Add headers, characteristics of statistics and add layout
        /// </summary>
        public virtual void ProcessLayout()
        {
            if (statColumnnames != null)
            {
                for (int statIdx = 0; statIdx < statColumnnames.Count; statIdx++)
                {
                    worksheet1.SetNumberFormat(new Range(worksheet1, headerRowIdx1 + 1, statIdx + firstStatColIdx1, rowIdx1 - 1, statIdx + statColumnnames.Count - 1), statNumberFormats[statIdx]);
                }
                worksheet1.SetBorderColor(new Range(worksheet1, headerRowIdx1, 0, rowIdx1 - 1, firstStatColIdx1 + statColumnnames.Count - 1), System.Drawing.Color.Black, BorderWeight.Thin, true);
            }
            else
            {
                if (firstRowIdx1 == 1)
                {
                    // When no other results are present write warning message
                    worksheet1.SetCellValue(headerRowIdx1 + 1, 0, NoResultsMessage);
                }
            }

            if (!Path.GetExtension(settings.OutputFile.ToLower()).Equals("xlsx"))
            {
                settings.OutputFile = Path.Combine(Path.GetDirectoryName(settings.OutputFile), Path.GetFileNameWithoutExtension(settings.OutputFile) + ".xlsx");
            }

            // Set titles
            worksheet1.SetCellValue(0, 0, "iMOD statistics for directory: " + settings.InputPath);
            worksheet1.SetCellValue(1, 0, "Filter filter: " + settings.InputFilter);
            worksheet1.SetCellValue(2, 0, "Extent: " + ((settings.Extent != null) ? settings.Extent.ToString() : "full extent of IDF is used"));
            worksheet1.FreezeRow(headerRowIdx1);
        }

        /// <summary>
        /// Cleans up underlying result table structures
        /// </summary>
        public void Cleanup()
        {
            if (spreadsheetManager != null)
            {
                spreadsheetManager.Cleanup();
            }
        }
    }
}
