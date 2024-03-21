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
using Sweco.SIF.GIS;
using Sweco.SIF.iMODstats.Zones;
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
    public class ResultTable
    {
        protected const string NoResultsMessage = "No results found";

        protected ResultTableSettings settings;
        protected Dictionary<string, List<ZoneStatistics>> zoneStatsDictionary;
        protected SpreadsheetManager spreadsheetManager;
        protected Log log;

        protected List<string> columnnames;
        protected List<string> numberFormats;
        protected List<string> comments;
        protected IWorkbook workbook;
        protected IWorksheet worksheet;
        protected int firstRowIdx;
        protected int firstStatColIdx;
        protected int headerRowIdx;
        protected int rowIdx;

        protected Dictionary<string, string> loggedSettingDictionary;

        /// <summary>
        /// Create empty StatsResult object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="zoneStatsDictionary"></param>
        /// <param name="log"></param>
        public ResultTable(ResultTableSettings settings, Dictionary<string, List<ZoneStatistics>> zoneStatsDictionary, Log log)
        {
            this.settings = settings;
            this.zoneStatsDictionary = zoneStatsDictionary;
            this.log = log;

            loggedSettingDictionary = null;

            spreadsheetManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
        }

        /// <summary>
        /// Initialize results
        /// </summary>
        public virtual void Initialize(Dictionary<string, string> loggedSettingDictionary = null)
        {
            this.loggedSettingDictionary = loggedSettingDictionary;

            firstRowIdx = 1;                        // Relative index to first available data row
            firstStatColIdx = 1;
            headerRowIdx = 4;                       // The first four rows before the headers are always filled with path, filter, extent and an empty row.
            headerRowIdx += (loggedSettingDictionary != null) ? loggedSettingDictionary.Count : 0;   // Add other, extra settings

            if (File.Exists(settings.OutputFile) && !settings.IsOverwrite)
            {
                throw new ToolException("Outputfile exists and option 'overwrite' is not used: " + settings.OutputFile);
            }

            // Retrieve statistic columnnames and corresponding number formats and comments
            List<ZoneStatistics> zoneStats0 = zoneStatsDictionary.First().Value;
            columnnames = (zoneStats0.Count > 0) ? zoneStats0[0].StatColumnnames : null;
            numberFormats = (zoneStats0.Count > 0) ? zoneStats0[0].StatNumberFormats : null;
            comments = (zoneStats0.Count > 0) ? zoneStats0[0].StatComments : null;

            workbook = spreadsheetManager.CreateWorkbook(true, "iMOD-stats");
            worksheet = workbook.Sheets[0];

            worksheet.SetCellValue(headerRowIdx, 0, "Filename");
            if (settings.IsRecursive)
            {
                worksheet.SetCellValue(headerRowIdx, 1, "Subdir");
                firstStatColIdx++;
            }
            if (columnnames != null)
            {
                for (int idx = 0; idx < columnnames.Count; idx++)
                {
                    worksheet.SetCellValue(headerRowIdx, idx + firstStatColIdx, columnnames[idx]);
                    worksheet.SetComment(headerRowIdx, idx + firstStatColIdx, comments[idx]);
                }
                worksheet.SetFontBold(new Range(worksheet, headerRowIdx, 0, headerRowIdx, columnnames.Count + firstStatColIdx), true);
            }

            rowIdx = headerRowIdx + firstRowIdx;
        }

        /// <summary>
        /// Add zone statistics for specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="zoneStatistics"></param>
        public virtual void AddZoneStatistics(string filename, ZoneStatistics zoneStatistics)
        {
            worksheet.SetCellValue(rowIdx, 0, Path.GetFileNameWithoutExtension(filename));
            if (settings.IsRecursive)
            {
                worksheet.SetCellValue(rowIdx, 1, FileUtils.GetRelativePath(Path.GetDirectoryName(filename), settings.InputPath));
            }

            for (int statIdx = 0; statIdx < zoneStatistics.StatValues.Count; statIdx++)
            {
                worksheet.SetCellValue(rowIdx, statIdx + firstStatColIdx, zoneStatistics.StatValues[statIdx]);
            }

            rowIdx++;
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
            if (columnnames != null)
            {
                for (int statIdx = 0; statIdx < columnnames.Count; statIdx++)
                {
                    worksheet.SetNumberFormat(new Range(worksheet, headerRowIdx + 1, statIdx + firstStatColIdx, rowIdx - 1, statIdx + columnnames.Count - 1), numberFormats[statIdx]);
                }
                worksheet.SetBorderColor(new Range(worksheet, headerRowIdx, 0, rowIdx - 1, firstStatColIdx + columnnames.Count - 1), System.Drawing.Color.Black, BorderWeight.Thin, true);
            }
            else
            {
                if (firstRowIdx == 1)
                {
                    // When no results are present write warning message
                    worksheet.SetCellValue(headerRowIdx + 1, 0, NoResultsMessage);
                }
            }

            if (!Path.GetExtension(settings.OutputFile.ToLower()).Equals("xlsx"))
            {
                settings.OutputFile = Path.Combine(Path.GetDirectoryName(settings.OutputFile), Path.GetFileNameWithoutExtension(settings.OutputFile) + ".xlsx");
            }

            // Set titles
            worksheet.SetCellValue(0, 0, "iMOD statistics for directory: " + settings.InputPath);
            worksheet.SetCellValue(1, 0, "Filter filter: " + settings.InputFilter);
            worksheet.SetCellValue(2, 0, "Extent: " + ((settings.Extent != null) ? settings.Extent.ToString() : "full extent of IDF is used"));
            if (loggedSettingDictionary != null)
            {
                int idx = 0;
                foreach (string settingKey in loggedSettingDictionary.Keys)
                {
                    string settingsValue = loggedSettingDictionary[settingKey];
                    worksheet.SetCellValue(3 + idx++, 0, settingKey + ": " + settingsValue);
                }
            }
            worksheet.FreezeRow(headerRowIdx);
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

    public class ResultTableSettings
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputFile { get; set; }

        public bool IsRecursive { get; set; }
        public bool IsOverwrite { get; set; }
        public Extent Extent { get; set; }
    }
}
