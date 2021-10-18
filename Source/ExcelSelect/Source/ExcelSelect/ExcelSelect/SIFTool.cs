// ExcelSelect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ExcelSelect.
// 
// ExcelSelect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ExcelSelect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ExcelSelect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.ExcelSelect
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
            ToolPurpose = "SIF-tool for selection of rows and/or sheets in Excel file(s)";
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

            SpreadsheetManager excelManager = null;
            IWorkbook workbook = null;
            int selectedRowCount = -1;
            int totalRowCount = -1;
            try
            {
                excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);

                Log.AddInfo("Processing Excel workbook '" + Path.GetFileName(settings.InputFilename) + "' ...");
                workbook = excelManager.OpenWorkbook(settings.InputFilename);

                if (!IsRowSelection(settings))
                {
                    // No rows have to be selected, only select sheets that were specified, other sheets are deleted

                    // Retrieve sheetnames for specified sheet indices
                    List<string> selectedSheetNames = new List<string>();
                    for (int sheetIdx = 0; sheetIdx < workbook.Sheets.Count; sheetIdx++)
                    {
                        if (settings.SheetIndices.Contains(sheetIdx))
                        {
                            selectedSheetNames.Add(workbook.Sheets[sheetIdx].GetSheetname());
                        }
                    }

                    // Note: EPPlus can't delete first sheet. Use workaround.
                    bool isFirstSheetDeleted = !settings.SheetIndices.Contains(0);
                    if (isFirstSheetDeleted)
                    {
                        // First sheet is not selected and has to be deleted. Use workaround: move first selected sheet before current first sheet
                        IWorksheet sheet0 = workbook.GetSheet(0);
                        IWorksheet firstSelSheet = workbook.GetSheet(selectedSheetNames[0]);
                        workbook.MoveSheetBefore(firstSelSheet, sheet0);
                    }

                    int sheetIdx2 = 0;
                    int selSheetCount = workbook.Sheets.Count;
                    while (sheetIdx2 < workbook.Sheets.Count)
                    {
                        string sheetname = workbook.Sheets[sheetIdx2].GetSheetname();
                        if (!selectedSheetNames.Contains(sheetname))
                        {
                            if (sheetIdx2 > 0)
                            {
                                Log.AddInfo("Deleting sheet " + (isFirstSheetDeleted ? sheetIdx2 : (sheetIdx2 + 1)) + ": " + sheetname + "...", 1);
                                workbook.DeleteSheet(sheetIdx2);
                                selSheetCount--;
                            }
                            else
                            {
                                Log.AddWarning("Sheet 1 cannot be deleted and is skipped (not deleted): " + sheetname, 1);
                                sheetIdx2++;
                            }
                        }
                        else
                        {
                            sheetIdx2++;
                        }
                    }
                    Log.AddInfo("Selected " + selSheetCount + " out of " + workbook.Sheets.Count + " sheets", 1);
                }
                else
                {
                    // Row selection was requested
                    if (settings.SheetIndices.Count > 1)
                    {
                        throw new ToolException("For row selection only one sheet can be specified");
                    }
                    int sheetIdx = settings.SheetIndices[0];

                    IWorksheet sheet = workbook.Sheets[sheetIdx];

                    Log.AddInfo("Selecting rows from sheet " + (sheetIdx + 1) + " ...", 1);
                    int rowIdx = settings.StartRowIdx;
                    int sourceRowIdx = rowIdx;
                    int lastRowIdx = sheet.End(new Cell(sheet, sheet.LastRowIdx, 1), CellDirection.Up).RowIdx;
                    totalRowCount = 0;
                    selectedRowCount = 0;
                    try
                    {
                        while (sourceRowIdx <= lastRowIdx)
                        {
                            bool isRowSelected = IsRowSelected(sheet, rowIdx, settings);
                            if (!isRowSelected)
                            {
                                sheet.DeleteRow(rowIdx);
                            }
                            else
                            {
                                selectedRowCount++;
                                rowIdx++;
                            }
                            sourceRowIdx++;
                            totalRowCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Unexpected error in row " + (sourceRowIdx + 1) + " while reading " + Path.GetFileName(settings.InputFilename), ex);
                    }

                    Log.AddInfo("Selected " + selectedRowCount + " out of " + totalRowCount + " rows", 1);
                }

                // Finished selection, save workbook to output path
                workbook.Save(settings.OutputFilename);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error", ex);
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

        protected virtual bool IsRowSelected(IWorksheet sheet, int rowIdx, SIFToolSettings settings)
        {
            bool isRowSelected = true;

            if (isRowSelected && (settings.FilenameColIdx >= 0))
            {
                bool isFileSelected = false;
                string filename = sheet.GetCellValue(rowIdx, settings.FilenameColIdx);
                if ((filename != null) && !filename.Equals(string.Empty))
                {
                    string extension = Path.GetExtension(filename);
                    if ((extension == null) || extension.Equals(string.Empty))
                    {
                        if (settings.DefaultFileExtension != null)
                        {
                            filename = filename + (settings.DefaultFileExtension.StartsWith(".") ? "" : ".") + settings.DefaultFileExtension;
                        }
                    }
                    string basePath = settings.BasePath;
                    if ((basePath == null) && (settings.BasePathColIdx >= 0))
                    {
                        basePath = sheet.GetCellValue(rowIdx, settings.BasePathColIdx + 1);
                    }
                    if (basePath != null)
                    {
                        filename = Path.Combine(basePath, filename);
                    }
                    if (File.Exists(filename))
                    {
                        isFileSelected = true;
                    }
                }
                isRowSelected = isFileSelected;
            }

            return isRowSelected;
        }

        /// <summary>
        /// Check if rows are selected or sheets
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual bool IsRowSelection(SIFToolSettings settings)
        {
            return (settings.FilenameColIdx >= 0);
        }
    }
}
