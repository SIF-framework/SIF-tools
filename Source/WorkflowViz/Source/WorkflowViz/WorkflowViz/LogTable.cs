// WorkflowViz is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of WorkflowViz.
// 
// WorkflowViz is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// WorkflowViz is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with WorkflowViz. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using Sweco.SIF.WorkflowViz.Status;
using Sweco.SIF.WorkflowViz.Workflows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz
{
    public class LogEntry
    {
        public Batchfile Batchfile { get; set; }

        public LogEntry(Batchfile batchfile)
        {
            Batchfile = batchfile;
        }
    }

    public class LogTable
    {
        protected const int BatchFilePathColIdx = 0;
        protected const int BatchFileNameColIdx = 1;
        protected const int BatchFileDateColIdx = 2;
        protected const int BatchLogDateColIdx = 3;
        protected const int BatchFileStatusColIdx = 4;
        protected const int BatchFileChecksumColIdx = 5;

        protected const int FilePathColIdx = 0;
        protected const int FileNameColIdx = 1;
        protected const int FileDateColIdx = 2;
        protected const int FileSizeColIdx = 3;
        protected const int FileChecksumColIdx = 4;

        public List<LogEntry> LogList { get; protected set; }

        public LogTable()
        {
            LogList = new List<LogEntry>();
        }

        public int Count
        {
            get { return LogList.Count; }
        }

        public void Add(LogEntry entry)
        {
            LogList.Add(entry);
        }

        public LogEntry GetEntry(int idx)
        {
            return LogList[idx];
        }

        public void CreateExcelFile(SIFToolSettings settings)
        {
            SpreadsheetManager excelManager = ExcelManagerFactory.CreateExcelManager();
            IWorkbook workbook = excelManager.CreateWorkbook(false);

            AddWorkflowSheet(workbook, settings);
            if (settings.FileListPaths != null)
            {
                foreach (string fileListPath in settings.FileListPaths)
                {
                    AddFileList(fileListPath, workbook, settings);
                }
            }

            workbook.Save(Path.Combine(settings.OutputPath, "LogTable.xlsx"));
            workbook.Close();
        }

        private void AddFileList(string fileListPath, IWorkbook workbook, SIFToolSettings settings)
        {
            IWorksheet worksheet = workbook.AddSheet(Path.GetFileName(fileListPath));

            int logRowHeaderIdx = 4;
            int lastColIdx = FileChecksumColIdx;

            // Add Title
            worksheet.SetCellValue(0, 0, "File list for directory:");
            worksheet.SetFontBold(0, 0, true);
            worksheet.SetCellValue(0, 1, fileListPath);
            worksheet.MergeCells(0, 1, 0, lastColIdx);

            worksheet.SetCellValue(1, 0, "File list created with:");
            worksheet.SetFontBold(1, 0, true);
            worksheet.SetCellValue(1, 1, "SIF-tool WorkflowViz");
            worksheet.MergeCells(1, 1, 1, lastColIdx);

            worksheet.SetCellValue(2, 0, "File list created at:");
            worksheet.SetCellValue(2, 1, DateTime.Now);
            worksheet.SetNumberFormat(2, 1, "dd-MM-yyyy HH:mm");
            worksheet.SetFontBold(2, 0, true);
            worksheet.MergeCells(2, 1, 2, lastColIdx);
            worksheet.SetHorizontalAlignmentLeft(2, 1);

            // Add header
            worksheet.SetCellValue(logRowHeaderIdx, 0, "Nr");
            worksheet.SetCellValue(logRowHeaderIdx, FilePathColIdx, "Path");
            worksheet.SetCellValue(logRowHeaderIdx, FileNameColIdx, "Filename");
            worksheet.SetCellValue(logRowHeaderIdx, FileDateColIdx, "File date");
            worksheet.SetCellValue(logRowHeaderIdx, FileSizeColIdx, "File size (b)");
            worksheet.SetCellValue(logRowHeaderIdx, FileChecksumColIdx, "Checksum (MD5)");

            worksheet.SetFontBold(new Range(worksheet, new Cell(worksheet, logRowHeaderIdx, 0), new Cell(worksheet, logRowHeaderIdx, 0).End(CellDirection.ToRight)), true);

            string[] filenames = Directory.GetFiles(fileListPath, "*", SearchOption.AllDirectories);

            for (int fileIdx = 0; fileIdx < filenames.Length; fileIdx++)
            {
                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, 0, fileIdx + 1);
                string filename = filenames[fileIdx];

                string relFilePath = FileUtils.GetRelativePath(filename, settings.InputPath).Length < filename.Length ? FileUtils.GetRelativePath(filename, settings.InputPath) : filename;
                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, FilePathColIdx, Path.GetDirectoryName(relFilePath));
                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, FileNameColIdx, Path.GetFileName(relFilePath));

                DateTime lastWriteTime = File.GetLastWriteTime(filename);
                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, FileDateColIdx, lastWriteTime.ToShortDateString());
                worksheet.SetNumberFormat(logRowHeaderIdx + 1 + fileIdx, FileDateColIdx, "dd-MM-yyyy");

                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, FileSizeColIdx, new FileInfo(filename).Length);

                string checksum = FileUtils.CalculateMD5Checksum(filename);
                worksheet.SetCellValue(logRowHeaderIdx + 1 + fileIdx, lastColIdx, checksum);
            }

            Range tableRange = new Range(worksheet, logRowHeaderIdx, 0, logRowHeaderIdx + filenames.Length, lastColIdx);
            worksheet.SetInteriorColor(new Range(worksheet, logRowHeaderIdx, 0, logRowHeaderIdx, lastColIdx), Color.FromArgb(242, 242, 242));
            worksheet.SetBorderColor(tableRange, Color.Black, BorderWeight.Thin, true);
            worksheet.AutoFitColumns(tableRange);
            worksheet.SetAutoFilter(tableRange);
        }

        private void AddWorkflowSheet(IWorkbook workbook, SIFToolSettings settings)
        {
            int logRowHeaderIdx = 4;
            int lastColIdx = BatchFileChecksumColIdx;

            IWorksheet worksheet = workbook.AddSheet("Workflows");

            // Add Title
            worksheet.SetCellValue(0, 0, "LogTable for workflow:");
            worksheet.SetFontBold(0, 0, true);
            worksheet.SetCellValue(0, 1, settings.InputPath);
            worksheet.MergeCells(0, 1, 0, lastColIdx);

            worksheet.SetCellValue(1, 0, "LogTable created with:");
            worksheet.SetFontBold(1, 0, true);
            worksheet.SetCellValue(1, 1, "SIF-tool WorkflowViz");
            worksheet.MergeCells(1, 1, 1, lastColIdx);

            worksheet.SetCellValue(2, 0, "LogTable created at:");
            worksheet.SetCellValue(2, 1, DateTime.Now);
            worksheet.SetNumberFormat(2, 1, "dd-MM-yyyy HH:mm");
            worksheet.SetFontBold(2, 0, true);
            worksheet.MergeCells(2, 1, 2, lastColIdx);
            worksheet.SetHorizontalAlignmentLeft(2, 1);

            // Add header
            worksheet.SetCellValue(logRowHeaderIdx, 0, "Nr");
            worksheet.SetCellValue(logRowHeaderIdx, BatchFilePathColIdx, "Path");
            worksheet.SetCellValue(logRowHeaderIdx, BatchFileNameColIdx, "Filename");
            worksheet.SetCellValue(logRowHeaderIdx, BatchFileDateColIdx, "BatchDate");
            worksheet.SetCellValue(logRowHeaderIdx, BatchLogDateColIdx, "LogDate");
            worksheet.SetCellValue(logRowHeaderIdx, BatchFileStatusColIdx, "Status");
            worksheet.SetCellValue(logRowHeaderIdx, BatchFileChecksumColIdx, "Checksum (MD5)");

            worksheet.SetFontBold(new Range(worksheet, new Cell(worksheet, logRowHeaderIdx, 0), new Cell(worksheet, logRowHeaderIdx, 0).End(CellDirection.ToRight)), true);

            for (int entryIdx = 0; entryIdx < Count; entryIdx++)
            {
                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, 0, entryIdx + 1);
                LogEntry logEntry = GetEntry(entryIdx);
                Batchfile batchfile = logEntry.Batchfile;

                string relBatchFilePath = FileUtils.GetRelativePath(batchfile.Filename, settings.InputPath);
                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchFilePathColIdx, Path.GetDirectoryName(relBatchFilePath));
                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchFileNameColIdx, Path.GetFileName(relBatchFilePath));
                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchFileDateColIdx, batchfile.LastWriteTime.ToShortDateString());
                worksheet.SetNumberFormat(logRowHeaderIdx + 1 + entryIdx, BatchFileDateColIdx, "dd-MM-yyyy");

                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchLogDateColIdx, (batchfile.Logfile != null) ? batchfile.Logfile.LastWriteTime.ToShortDateString() : null);
                worksheet.SetNumberFormat(logRowHeaderIdx + 1 + entryIdx, BatchLogDateColIdx, "dd-MM-yyyy");

                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchFileStatusColIdx, batchfile.RunStatus.ToString());
                worksheet.SetInteriorColor(logRowHeaderIdx + 1 + entryIdx, BatchFileStatusColIdx, GetStatusColor(batchfile.RunStatus));

                string checksum = FileUtils.CalculateMD5Checksum(batchfile.Filename);
                worksheet.SetCellValue(logRowHeaderIdx + 1 + entryIdx, BatchFileChecksumColIdx, checksum);
            }

            Range tableRange = new Range(worksheet, logRowHeaderIdx, 0, logRowHeaderIdx + Count, lastColIdx);
            worksheet.SetInteriorColor(new Range(worksheet, logRowHeaderIdx, 0, logRowHeaderIdx, lastColIdx), Color.FromArgb(242, 242, 242));
            worksheet.SetBorderColor(tableRange, Color.Black, BorderWeight.Thin, true);
            worksheet.AutoFitColumns(tableRange);
            worksheet.SetAutoFilter(tableRange);
        }

        /// <summary>
        /// Get the dot-colorname for the specified WorkflowViz-status 
        /// </summary>
        /// <param name="runStatus"></param>
        /// <returns></returns>
        protected Color GetStatusColor(RunStatus runStatus)
        {
            Color color;
            switch (runStatus)
            {
                case RunStatus.Undefined:
                    color = Color.LightGray; // "lightgrey";
                    break;
                case RunStatus.Completed:
                    color = Color.FromArgb(162, 205, 90); // "darkolivegreen3";
                    break;
                case RunStatus.None:
                    color = Color.FromArgb(192, 192, 192); // "grey";
                    break;
                case RunStatus.Ignored:
                    color = Color.FromArgb(242, 242, 242);  //"grey95";
                    break;
                case RunStatus.Outdated:
                    color = Color.FromArgb(255, 193, 37); // "goldenrod1";
                    break;
                case RunStatus.Error:
                    color = Color.FromArgb(255, 128, 128); // "#ff8080"; // light red
                    break;
                case RunStatus.CompletedPartially:
                    color = Color.FromArgb(255, 246, 143); // "khaki1";
                    break;
                case RunStatus.Unknown:
                    color = Color.FromArgb(255, 0, 0); // Red / "firebrick1";
                    break;
                default:
                    throw new Exception("Invalid status: " + runStatus);
            }
            return color;
        }

    }
}
