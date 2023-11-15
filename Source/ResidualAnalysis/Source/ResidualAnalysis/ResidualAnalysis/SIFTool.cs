// ResidualAnalysis is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ResidualAnalysis.
// 
// ResidualAnalysis is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ResidualAnalysis is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ResidualAnalysis. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using Sweco.SIF.Statistics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.ResidualAnalysis
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

        // Note: sheets use zero-based row/col-indices: 0 is the index of the first column/row
        protected const int SheetSummaryHeaderRowIdx = 4;
        protected const int SummarySheetHeaderRowIdx = 0;
        protected const int ComparisonSheetHeaderRowIdx = 0;
        protected const string SummarySheetname = "Comparison";
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);
        protected static int DecimalCount = 3;
        protected static int MaxBinCount = 100;
        protected static int MinBinCount = 4;

        /// <summary>
        /// Mask IDF-file with zones that correspond to masked values in settings
        /// </summary>
        protected IDFFile maskIDFFile = null;

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

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            AddAuthor("Koen Jansen");
            ToolPurpose = "SIF-tool for statistics and analysis of IPF-files with model residuals";
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
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            int logIndentLevel = 0;

            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);

            if (settings.OutputFilename == null)
            {
                settings.OutputFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputFilenames[0]) + ".xlsx");
            }
            else
            {
                settings.OutputFilename = Path.Combine(outputPath, settings.OutputFilename);
            }

            Log.AddInfo("Processing input files ...", logIndentLevel);
            int fileCount = 0;
            List<IPFFile> ipfFileList = new List<IPFFile>();

            logIndentLevel = 1;
            int layerColNr;
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Reading file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);

                if (!Path.GetExtension(inputFilename).ToLower().Equals(".ipf"))
                {
                    throw new ToolException("Invalid input filename (IPF-file expected): " + Path.GetFileName(inputFilename));
                }

                IPFFile ipfFile = IPFFile.ReadFile(inputFilename);
                layerColNr = ParseColString(settings.LayerColString, "Layer", ipfFileList);

                if (ipfFile.PointCount > 0)
                {
                    if (settings.Extent != null)
                    {
                        ipfFile = ipfFile.ClipIPF(settings.Extent);
                    }
                    if (ipfFile.PointCount > 0)
                    {
                        ipfFileList.Add(ipfFile);
                    }
                    else
                    {
                        Log.AddInfo("Clipped IPF-file has no points and is skipped: " + Path.GetFileName(ipfFile.Filename), logIndentLevel);
                    }
                }
                else
                {
                    Log.AddInfo("IPF-file has no points and is skipped: " + Path.GetFileName(ipfFile.Filename), logIndentLevel);
                }
            }

            ipfFileList.Sort(new IPFFileListComparer());

            // Retrieve one based columnindices from first IPF-file
            int idColNr = ParseColString(settings.IdColString, "Id", ipfFileList);
            layerColNr = ParseColString(settings.LayerColString, "Layer", ipfFileList);
            int observedColNr = ParseColString(settings.ObservedColString, "Observation", ipfFileList);
            int simulatedColNr = ParseColString(settings.SimulatedColString, "Simulation", ipfFileList);
            int residualColNr = ParseColString(settings.ResidualColString, "Residual", ipfFileList);
            int weightColNr = ParseColString(settings.WeightColString, "Weight", ipfFileList);
            List<int> colNrs = new List<int>();
            for (int idx = 0; idx < settings.ColStrings.Count; idx++)
            {
                int colNr = ParseColString(settings.ColStrings[idx], "ExtraCol" + (idx + 1), ipfFileList);
                colNrs.Add(colNr);
            }

            string modelname = settings.Modelname;
            if (modelname == null)
            {
                // Use as a default modelname the relative path of the residualfiles
                string commonPath = CommonUtils.LongestCommonSubstring(settings.InputPath, Directory.GetCurrentDirectory());
                modelname = FileUtils.GetRelativePath(settings.InputPath, commonPath);
                modelname = modelname.Replace("\\", "_");
                modelname = modelname.Replace("..", string.Empty);
                while (modelname.Contains("__"))
                {
                    modelname = modelname.Replace("__", "_");
                }
                if (modelname.StartsWith("_"))
                {
                    modelname = modelname.Substring(1);
                }
            }
            string calibrationsetname = settings.Calibrationsetname;
            if (settings.Calibrationsetname == null)
            {
                if (ipfFileList.Count > 1)
                {
                    // Use as a default caibrationset name the first, non-layer, part of the first residual filename 
                    calibrationsetname = Path.GetFileNameWithoutExtension(ipfFileList[0].Filename);
                    calibrationsetname = CommonUtils.LongestCommonSubstring(calibrationsetname, Path.GetFileNameWithoutExtension(ipfFileList[1].Filename));
                    calibrationsetname = RemoveLayerPostfix(calibrationsetname);
                }
            }

            string metadataString1 = "Path: " + settings.InputPath + ", filter: " + settings.InputFilter;
            string metadataString2 = "Model: " + modelname + ", calibrationset: " + calibrationsetname;

            processResidualFiles(
                modelname,
                calibrationsetname,
                ipfFileList,
                idColNr,
                layerColNr,
                observedColNr,
                simulatedColNr,
                residualColNr,
                weightColNr,
                colNrs,
                settings.ColNames,
                metadataString1,
                metadataString2,
                Log,
                logIndentLevel,
                settings);

            fileCount++;

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Replace string with column number for string with columnname 
        /// </summary>
        /// <param name="colString"></param>
        /// <param name="colName"></param>
        /// <param name="ipfFileList"></param>
        /// <returns>column number, or exception if columnname is not found</returns>
        protected static int ParseColString(string colString, string colName, List<IPFFile> ipfFileList)
        {
            int colNr = 0;
            if ((ipfFileList.Count > 0) && (colString != null))
            {
                IPFFile ipfFile1 = ipfFileList[0];
                if (!int.TryParse(colString, out colNr))
                {
                    colNr = ipfFile1.FindColumnName(colString) + 1;
                    if (colNr == 0)
                    {
                        throw new ToolException("Specified " + colName + "-column not found in IPF-file: " + colString);
                    }
                }
            }

            return colNr;
        }

        protected string RemoveLayerPostfix(string calibrationsetname)
        {
            if (calibrationsetname.ToLower().EndsWith("_l"))
            {
                calibrationsetname = calibrationsetname.Remove(calibrationsetname.Length - 2, 2);
            }
            else if (calibrationsetname.ToLower().EndsWith("_layer"))
            {
                calibrationsetname = calibrationsetname.Remove(calibrationsetname.Length - 6, 6);
            }
            else if (calibrationsetname.ToLower().EndsWith("_laag"))
            {
                calibrationsetname = calibrationsetname.Remove(calibrationsetname.Length - 5, 5);
            }
            return calibrationsetname;
        }

        protected void processResidualFiles(string modelname, string calibrationsetname, List<IPFFile> ipfFileList, int idColNr, int layerColNr, int observedColNr, int simulatedColNr, int residualColNr, int weightColNr, List<int> extraColumnNrs, List<string> extraColumnNames, string metadataString1, string metadataString2, Log log, int logIndentLevel, SIFToolSettings settings)
        {
            // Do general checks
            if (ipfFileList.Count == 0)
            {
                log.AddWarning("No IPF-files found.", logIndentLevel);
                return;
            }

            if ((extraColumnNames.Count > 0) && (extraColumnNames.Count != extraColumnNrs.Count))
            {
                throw new ToolException("Extra number of column names (" + extraColumnNames.Count + ") should be equal to number of extra column indices (" + extraColumnNrs.Count + ")");
            }

            // Create Excel Worksheet
            ExcelManager excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);

            IWorkbook workbook = null;
            IWorksheet sheet = null;

            string sheetname;
            if (calibrationsetname != null)
            {
                sheetname = SpreadsheetUtils.CorrectSheetname(
                SpreadsheetUtils.CorrectSheetname(modelname) + "-" + SpreadsheetUtils.CorrectSheetname(calibrationsetname));
            }
            else
            {
                sheetname = SpreadsheetUtils.CorrectSheetname(modelname);
            }

            bool isSummaryAdded = true; // always add summary sheet
            if (File.Exists(settings.OutputFilename) && !settings.IsOverwrite)
            {
                isSummaryAdded = true;
                if (!settings.IsBackupSkipped)
                {
                    string backupFilename = FileUtils.GetUniqueFilename(Path.Combine(Path.GetDirectoryName(settings.OutputFilename), Path.GetFileNameWithoutExtension(settings.OutputFilename) + "_backup" + Path.GetExtension(settings.OutputFilename)));
                    File.Copy(settings.OutputFilename, backupFilename, true);
                }
                workbook = excelManager.OpenWorkbook(settings.OutputFilename, false);

                // remove old overviewsheet
                int summarySheetIdx = workbook.FindSheetIndex(SummarySheetname);
                if (summarySheetIdx >= 0)
                {
                    try
                    {
                        workbook.DeleteSheet(summarySheetIdx);
                    }
                    catch (Exception ex)
                    {
                        log.AddWarning(ex.GetBaseException().Message, logIndentLevel);
                    }
                }

                int sheetNameIdx = 1;
                string baseSheetname = sheetname;
                while (workbook.GetSheet(sheetname) != null)
                {
                    sheetNameIdx++;
                    sheetname = SpreadsheetUtils.CorrectSheetname(baseSheetname + sheetNameIdx);
                }
                sheet = workbook.AddSheet(sheetname);
            }
            else
            {
                workbook = excelManager.CreateWorkbook(true, sheetname);
                sheet = workbook.Sheets[0];
            }

            try
            {
                AddResidualStatistics(sheet, modelname, calibrationsetname, ipfFileList, idColNr, layerColNr, observedColNr, simulatedColNr, residualColNr, weightColNr, extraColumnNrs, extraColumnNames, metadataString1, metadataString2, logIndentLevel, settings);
                if (isSummaryAdded)
                {
                    AddResidualSummarySheets(workbook, idColNr, observedColNr, simulatedColNr, (weightColNr > 0), log, logIndentLevel, settings);
                }
                else if (settings.IsDiffIPFCreated)
                {
                    log.AddInfo("No current Excelsheet found, residual difference IPF-files cannot be calculated for a single model and are skipped", logIndentLevel);
                }

                workbook.Save(settings.OutputFilename);
            }
            catch (Exception ex)
            {
                string baseMessage = ex.GetBaseException().Message;
                if (baseMessage.Contains("toegang") || baseMessage.Contains("access"))
                {
                    throw new ToolException("Excelfile cannot be written. Possibly it is open in another application. Please check, close and try again.");
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                workbook.Close();
                excelManager.Cleanup();
            }
        }

        protected void AddResidualSummarySheets(IWorkbook workbook, int idColNr, int observedColNr, int simulatedColNr, bool isWeighed, Log log, int logIndentLevel, SIFToolSettings settings)
        {
            log.AddInfo("Creating summary sheets ...", logIndentLevel);

            IWorksheet summarySheet = workbook.AddSheet(SummarySheetname);
            workbook.MoveSheetToStart(summarySheet);

            List<string> calibrationSets = new List<string>();
            Dictionary<string, List<CalSetModelDef>> calibrationSetDefs = new Dictionary<string, List<CalSetModelDef>>();
            int nextCalibrationsetRowIdx = ComparisonSheetHeaderRowIdx;
            for (int sheetIdx = 1; sheetIdx < workbook.Sheets.Count(); sheetIdx++)
            {
                IWorksheet residualSheet = workbook.Sheets[sheetIdx];

                // Retrieve modelname and calibrationsetname
                string metadataString2 = residualSheet.GetCellValue(2, 0);
                if ((metadataString2 == null) || metadataString2.Equals(string.Empty))
                {
                    throw new ToolException("\"Model:modelname,calibrationset:calibrationsetname\" expected, invalid empty cell in sheet " + residualSheet.GetSheetname() + ", cell (" + SheetSummaryHeaderRowIdx + "," + ")" + ": " + metadataString2 + ".");
                }
                string[] metadataStringValues = metadataString2.Split(',');
                if (metadataStringValues.Length != 2)
                {
                    throw new ToolException("\"Model:modelname,calibrationset:calibrationsetname\" expected, invalid cellvalue in sheet " + residualSheet.GetSheetname() + ", cell (" + SheetSummaryHeaderRowIdx + "," + ")" + ": " + metadataString2 + ".");
                }
                string[] modelnameKeyValues = metadataStringValues[0].Split(':');
                string[] calibrationsetKeyValues = metadataStringValues[1].Split(':');
                if (modelnameKeyValues.Length != 2)
                {
                    throw new ToolException("\"Model:modelname,calibrationset:calibrationsetname\" expected, invalid cellvalue in sheet " + residualSheet.GetSheetname() + ", cell (" + SheetSummaryHeaderRowIdx + "," + ")" + ": " + metadataString2 + ".");
                }
                if (metadataStringValues.Length != 2)
                {
                    throw new ToolException("\"Model:modelname,calibrationset:calibrationsetname\" expected, invalid cellvalue in sheet " + residualSheet.GetSheetname() + ", cell (" + SheetSummaryHeaderRowIdx + "," + ")" + ": " + metadataString2 + ".");
                }
                string modelname = modelnameKeyValues[1].Trim();
                string calibrationsetname = calibrationsetKeyValues[1].Trim();
                CalSetModelDef calSetModelDef = new CalSetModelDef(calibrationsetname, modelname, residualSheet);

                // Retrieve residual columnname
                string resColName = residualSheet.GetCellValue(SheetSummaryHeaderRowIdx, 2);
                if (resColName != null)
                {
                    resColName = resColName.Replace("M(", string.Empty);
                    resColName = resColName.Replace(")", string.Empty);
                }

                // Try to find calibrationset
                int residualRowIdx;
                int comparisonRowIdx;
                int headerRowIdx = SummarySheetHeaderRowIdx;
                int calsetLayerCount = 0; // number of layers in current calibration set
                int calSetModelCount = 0; // number of models in current calibration set
                int modelCount = 0; // maximum number of models over all calibration sets

                // Find residual columns in current comparison sheet
                int avgResStartColIdx = 3;
                int avgAbsResStartColIdx = RetrieveSheetColIdx(summarySheet, ResidualStatistic.MAE, 5, isWeighed, (sheetIdx > 1), settings);
                int sdAvgResStartColIdx = RetrieveSheetColIdx(summarySheet, ResidualStatistic.SDE, 7, isWeighed, (sheetIdx > 1), settings);
                int rmseStartColIdx = RetrieveSheetColIdx(summarySheet, ResidualStatistic.RMSE, 9, isWeighed, (sheetIdx > 1), settings);
                int sseStartColIdx = RetrieveSheetColIdx(summarySheet, ResidualStatistic.SSE, 11, isWeighed, (sheetIdx > 1), settings);

                // Calculate maximum model count over all current calibration sets
                modelCount = sdAvgResStartColIdx - avgAbsResStartColIdx - 1;

                Cell calibrationSetCell = summarySheet.FindCell(calibrationsetname);
                if (calibrationSetCell != null)
                {
                    // Calibrationset is already present
                    headerRowIdx = calibrationSetCell.RowIdx - 2;
                    calibrationSetDefs[calibrationsetname].Add(calSetModelDef);

                    // Determine current number of model columns
                    Cell cell1 = new Cell(summarySheet, headerRowIdx + 1, 1);
                    Cell cell2 = cell1.End(CellDirection.ToRight);
                    Cell cell3 = cell1.End(CellDirection.Down);
                    calSetModelCount = cell2.ColIdx - cell1.ColIdx - 1;
                    calsetLayerCount = cell3.RowIdx - cell1.RowIdx - 1;

                    if (calSetModelCount >= modelCount)
                    {
                        // Insert columns
                        summarySheet.InsertColumn(avgAbsResStartColIdx);
                        avgAbsResStartColIdx += 1;
                        sdAvgResStartColIdx += 1;
                        summarySheet.InsertColumn(sdAvgResStartColIdx);
                        sdAvgResStartColIdx += 1;
                        rmseStartColIdx += 2;
                        summarySheet.InsertColumn(rmseStartColIdx);
                        rmseStartColIdx += 1;
                        sseStartColIdx += 3;
                        summarySheet.InsertColumn(sseStartColIdx);
                        sseStartColIdx += 1;
                    }
                }
                else
                {
                    // Calibrationset is not yet present, add at it below last calibration set
                    calibrationSetDefs.Add(calibrationsetname, new List<CalSetModelDef>());
                    calibrationSetDefs[calibrationsetname].Add(calSetModelDef);

                    headerRowIdx = nextCalibrationsetRowIdx;
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx + 1, 0, "Set");
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx + 1, 1, "Layer");
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx + 1, 2, "N");
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx + 2, 0, calibrationsetname);
                    summarySheet.SetOrientation(new Range(summarySheet, nextCalibrationsetRowIdx + 2, 0, nextCalibrationsetRowIdx + 2, 0), 90);
                    calSetModelCount = 0;

                    summarySheet.SetCellValue(nextCalibrationsetRowIdx, avgResStartColIdx, RetrieveSheetColumnName(ResidualStatistic.ME, isWeighed, settings, resColName));
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx, avgAbsResStartColIdx, RetrieveSheetColumnName(ResidualStatistic.MAE, isWeighed, settings, resColName));
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx, sdAvgResStartColIdx, RetrieveSheetColumnName(ResidualStatistic.SDE, isWeighed, settings, resColName));
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx, rmseStartColIdx, RetrieveSheetColumnName(ResidualStatistic.RMSE, isWeighed, settings, resColName));
                    summarySheet.SetCellValue(nextCalibrationsetRowIdx, sseStartColIdx, RetrieveSheetColumnName(ResidualStatistic.SSE, isWeighed, settings, resColName));
                    if (sheetIdx == 1)
                    {
                        summarySheet.SetComment(nextCalibrationsetRowIdx, avgResStartColIdx, RetrieveSummaryColumnComment(ResidualStatistic.ME, isWeighed));
                        summarySheet.SetComment(nextCalibrationsetRowIdx, avgAbsResStartColIdx, RetrieveSummaryColumnComment(ResidualStatistic.MAE, isWeighed));
                        summarySheet.SetComment(nextCalibrationsetRowIdx, sdAvgResStartColIdx, RetrieveSummaryColumnComment(ResidualStatistic.SDE, isWeighed));
                        summarySheet.SetComment(nextCalibrationsetRowIdx, rmseStartColIdx, RetrieveSummaryColumnComment(ResidualStatistic.RMSE, isWeighed));
                        summarySheet.SetComment(nextCalibrationsetRowIdx, sseStartColIdx, RetrieveSummaryColumnComment(ResidualStatistic.SSE, isWeighed));
                    }

                    // Find number of layers and residuals
                    residualRowIdx = SheetSummaryHeaderRowIdx + 1;
                    comparisonRowIdx = nextCalibrationsetRowIdx + 2;
                    while (!residualSheet.IsEmpty(residualRowIdx, 1))
                    {
                        // Copy layer number
                        residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 0), new Cell(summarySheet, comparisonRowIdx, 1));
                        // Copy point count (N)
                        residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 1), new Cell(summarySheet, comparisonRowIdx, 2));
                        residualRowIdx++;
                        comparisonRowIdx++;
                        calsetLayerCount++;
                    }
                    // Remove comment, as workaround for epplus bug when deleting sheets with identical comments in different cells
                    summarySheet.DeleteComment(comparisonRowIdx - 1, 1);
                    // Correct number of layers, last row was total statitstic
                    calsetLayerCount--;
                    // Merge cells below calibrationset name
                    summarySheet.MergeCells(nextCalibrationsetRowIdx + 2, 0, nextCalibrationsetRowIdx + 2 + calsetLayerCount, 0);
                    summarySheet.SetVerticalAlignmentCenter(new Range(summarySheet, nextCalibrationsetRowIdx + 2, 0, nextCalibrationsetRowIdx + 2 + calsetLayerCount, 0));

                    // Set row for next calibration set
                    nextCalibrationsetRowIdx = comparisonRowIdx + 1;
                }

                // Now add model statistics to comparison table
                residualRowIdx = SheetSummaryHeaderRowIdx + 1;
                comparisonRowIdx = headerRowIdx + 2;
                int modellayerCount = 0;
                summarySheet.SetHyperlink(headerRowIdx + 1, avgResStartColIdx + calSetModelCount, null, "'" + residualSheet.GetSheetname() + "'!A1", null, modelname);
                summarySheet.SetHyperlink(headerRowIdx + 1, avgAbsResStartColIdx + calSetModelCount, null, "'" + residualSheet.GetSheetname() + "'!A1", null, modelname);
                summarySheet.SetHyperlink(headerRowIdx + 1, sdAvgResStartColIdx + calSetModelCount, null, "'" + residualSheet.GetSheetname() + "'!A1", null, modelname);
                summarySheet.SetHyperlink(headerRowIdx + 1, rmseStartColIdx + calSetModelCount, null, "'" + residualSheet.GetSheetname() + "'!A1", null, modelname);
                summarySheet.SetHyperlink(headerRowIdx + 1, sseStartColIdx + calSetModelCount, null, "'" + residualSheet.GetSheetname() + "'!A1", null, modelname);
                summarySheet.SetColumnWidth(sseStartColIdx + calSetModelCount, 12);
                while (!residualSheet.IsEmpty(residualRowIdx, 1))
                {
                    residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 2), new Cell(summarySheet, comparisonRowIdx, avgResStartColIdx + calSetModelCount));
                    residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 3), new Cell(summarySheet, comparisonRowIdx, avgAbsResStartColIdx + calSetModelCount));
                    residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 4), new Cell(summarySheet, comparisonRowIdx, sdAvgResStartColIdx + calSetModelCount));
                    residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 5), new Cell(summarySheet, comparisonRowIdx, rmseStartColIdx + calSetModelCount));
                    residualSheet.CopyCell(new Cell(residualSheet, residualRowIdx, 6), new Cell(summarySheet, comparisonRowIdx, sseStartColIdx + calSetModelCount));
                    residualRowIdx++;
                    comparisonRowIdx++;
                    modellayerCount++;
                }
                modellayerCount--;
                if (modellayerCount != calsetLayerCount)
                {
                    throw new ToolException("Unequal number of layers found for calibration set '" + calibrationsetname + "': " + calsetLayerCount + " vs " + modellayerCount + " for model " + modelname);
                }
                Range headerRange = new Range(summarySheet, headerRowIdx, 0, headerRowIdx + 1, sseStartColIdx + modelCount);
                summarySheet.SetFontBold(headerRange, true);

                if (settings.IsDiffIPFCreated)
                {
                    CalSetModelDef basicCalSetModelDef = calibrationSetDefs[calibrationsetname][0];
                    if (!basicCalSetModelDef.ModelName.Equals(calSetModelDef.ModelName))
                    {
                        CreateResidualDifferenceIPFFile(workbook, basicCalSetModelDef, calSetModelDef, idColNr, observedColNr, simulatedColNr, log, settings);
                    }
                }
            }
            summarySheet.AutoFitColumns();
            summarySheet.SetColumnWidth(0, 4);
            summarySheet.FreezeRow(1);
            summarySheet.Zoom(80);
        }

        protected virtual int RetrieveSheetColIdx(IWorksheet sheet, ResidualStatistic statistic, int defaultColIdx, bool isWeighed, bool isExceptionThrown, SIFToolSettings settings)
        {
            int colIdx = defaultColIdx;

            string columnName = RetrieveSheetColumnName(statistic, isWeighed, settings);
            Cell cell = sheet.FindCell(columnName, true, true);
            if (cell != null)
            {
                colIdx = cell.ColIdx;
            }
            else if (isExceptionThrown)
            {
                throw new ToolException(columnName + "-column not found in summary sheet");
            }

            return colIdx;
        }

        protected virtual string RetrieveSheetColumnName(ResidualStatistic statistic, bool isWeighed, SIFToolSettings settings, string resColName = null)
        {
            string columnname;
            switch (statistic)
            {
                case ResidualStatistic.ME:
                case ResidualStatistic.MAE:
                case ResidualStatistic.RMSE:
                case ResidualStatistic.SSE:
                    columnname = statistic.ToString();
                    break;
                case ResidualStatistic.SDE:
                    columnname = "SD(E)";
                    break;
                default:
                    throw new Exception("Unknown ResidualStatistic: " + statistic);
            }

            return isWeighed ? ("W" + columnname) : columnname;
        }

        protected virtual string RetrieveSummaryColumnComment(ResidualStatistic statistic, bool isWeighed)
        {
            switch (statistic)
            {
                case ResidualStatistic.ME:
                    return isWeighed ? "Weighted Mean Error (or weighted average residual)" : "Mean Error (or average residual)";
                case ResidualStatistic.MAE:
                    return isWeighed ? "Weighted Mean Absolute Error (or weighted average absolute residual)" : "Mean Absolute Error (or average absolute residual)";
                case ResidualStatistic.SDE:
                    return isWeighed ? "Standard deviation of Weighted Mean Error" : "Standard deviation of Mean Error";
                case ResidualStatistic.RMSE:
                    return isWeighed ? "Weighted Root Mean Squared Error" : "Root Mean Squared Error";
                case ResidualStatistic.SSE:
                    return isWeighed ? "Weighted Sum Squared Error" : "Sum Squared Error";
                default:
                    throw new Exception("Unknown ResidualStatistic: " + statistic);
            }
        }

        protected virtual string RetrieveSheetColumnComment(ResidualStatistic statistic, bool isWeighed)
        {
            switch (statistic)
            {
                case ResidualStatistic.ME:
                    return isWeighed ? "Weighed Mean Error or average weighed residual per layer: sum of residual times weight, divided by sum of all weights" : "Mean Error or average residual per layer";
                case ResidualStatistic.MAE:
                    return isWeighed ? "Weighthed Mean Absolute Error or average absolute weigthed residual per layer: sum of absolute residuals times weight, divided by sum of all weights" :
                                       "Mean Absolute Error or average absolute residual per layer";
                case ResidualStatistic.SDE:
                    return isWeighed ? "Weighted Standard deviation of Weighted Mean Error per layer" : "Standard deviation of Mean Error per layer";
                case ResidualStatistic.RMSE:
                    return isWeighed ? "Weighted Root Mean Squared Error" : "Root Mean Squared Error";
                case ResidualStatistic.SSE:
                    return isWeighed ? "Weighted Sum Squared Error" : "Sum Squared Error";
                default:
                    throw new Exception("Unknown ResidualStatistic: " + statistic);
            }
        }

        protected void CreateResidualDifferenceIPFFile(IWorkbook workbook, CalSetModelDef basicCalSetModelDef, CalSetModelDef calSetModelDef, int idColNr, int observedColNr, int simulatedColNr, Log log, SIFToolSettings settings)
        {
            string ipfFilename = Path.Combine(settings.IpfPath, FileUtils.EnsureTrailingSlash(basicCalSetModelDef.CalibrationsetName) + calSetModelDef.ModelName + "-" + basicCalSetModelDef.ModelName + ".IPF");
            int resColIdx = 3;
            int lastSingleColIdx = 1; // X,Y or X,Y,ID
            if (idColNr > 0)
            {
                resColIdx++;
                lastSingleColIdx++;
            }
            if (observedColNr != 0)
            {
                resColIdx++;
            }
            if (simulatedColNr != 0)
            {
                resColIdx++;
            }

            IWorksheet sheet1 = basicCalSetModelDef.Worksheet;
            IWorksheet sheet2 = calSetModelDef.Worksheet;

            // Find residual header of filters in sheet1
            // skip values rows of summary
            int headerIdx1 = SheetSummaryHeaderRowIdx;
            while (!sheet1.IsEmpty(new Cell(sheet1, headerIdx1, 0)))
            {
                headerIdx1++;
            }
            // skip empty rows
            while (sheet1.IsEmpty(new Cell(sheet1, headerIdx1, 0)) && (headerIdx1 < 1000))
            {
                headerIdx1++;
            }
            if (sheet1.IsEmpty(headerIdx1, 0))
            {
                throw new Exception("Invalid residualsheet, filters header not found: " + sheet1.GetSheetname());
            }
            if (!sheet1.GetCellValue(headerIdx1, 0).ToLower().Equals("x"))
            {
                throw new Exception("Invalid residualsheet, filters header with columnname 'X' not found: " + sheet1.GetSheetname());
            }
            if (!sheet1.GetCellValue(headerIdx1, 1).ToLower().Equals("y"))
            {
                throw new Exception("Invalid residualsheet, filters header with columnname 'Y' not found: " + sheet1.GetSheetname());
            }

            // Find residual header of filters in sheet2
            // skip values rows of summary
            int headerIdx2 = SheetSummaryHeaderRowIdx;
            while (!sheet2.IsEmpty(new Cell(sheet2, headerIdx2, 0)))
            {
                headerIdx2++;
            }
            // skip empty rows
            while (sheet2.IsEmpty(new Cell(sheet2, headerIdx2, 0)) && (headerIdx2 < 2000))
            {
                headerIdx2++;
            }
            if (sheet2.IsEmpty(headerIdx2, 0))
            {
                throw new Exception("Invalid residualsheet, filters header not found: " + sheet2.GetSheetname());
            }
            if (!sheet2.GetCellValue(headerIdx2, 0).ToLower().Equals("x"))
            {
                throw new Exception("Invalid residualsheet, filters header with columnname 'X' not found: " + sheet2.GetSheetname());
            }
            if (!sheet2.GetCellValue(headerIdx2, 1).ToLower().Equals("y"))
            {
                throw new Exception("Invalid residualsheet, filters header with columnname 'Y' not found: " + sheet2.GetSheetname());
            }

            if (headerIdx1 != headerIdx2)
            {
                throw new Exception("Invalid residualsheet, filters header in different rows (" + headerIdx1 + " vs " + headerIdx2 + ") for sheets: '" + sheet1.GetSheetname() + "' and '" + sheet2.GetSheetname() + "'");
            }

            // Add IPF column names
            IPFFile ipfFile = new IPFFile();
            ipfFile.AddXYColumns();
            int colIdx = 0;
            while (!sheet1.IsEmpty(headerIdx1, colIdx))
            {
                string colName1 = sheet1.GetCellValue(headerIdx1, colIdx);
                string colName2 = sheet2.GetCellValue(headerIdx1, colIdx);
                if (colName1 == null)
                {
                    colName2 = string.Empty;
                }
                //if (!colName1.Equals(colName2))
                //{
                //    throw new Exception("Columnname mismatch ('" + colName1 + "' vs '" + colName2 + "') in sheets: '" + sheet1.GetSheetname() + "' and '" + sheet2.GetSheetname() + "'");
                //}
                if (colIdx <= lastSingleColIdx)
                {
                    // XY-columns are already added for new IPF
                    if (colIdx > 1)
                    {
                        ipfFile.AddColumn(colName1);
                    }
                }
                else
                {
                    ipfFile.AddColumn(colName1 + "(1)");
                    ipfFile.AddColumn(colName1 + "(2)");
                }
                colIdx++;
            }
            ipfFile.AddColumn("dRES2-1(dH2-1)"); // Dit geeft de verandering van de grondwaterstand door model2 tov model1 ter plaatse van filter
            ipfFile.AddColumn("dABSRES1-2"); // Dit geeft de verbetering in het absolute residu: geeft aan of het model absoluut gezien beter wordt. 
            ipfFile.AddColumn("ABS(dABSRES1-2)"); // De absolute waarden van dABSRES1-2
            ipfFile.AddColumn("CLASS"); // De absolute waarden van dABSRES1-2
            ipfFile.AddColumn("dSGN");  // Verschil in sign: van -1 naar 1 of van 1 naar -1

            // Add IPF points
            int rowIdx = headerIdx1 + 1;
            while (!sheet1.IsEmpty(rowIdx, 1))
            {
                colIdx = 0;
                List<string> columnValues = new List<string>();

                // For each point in sheet loop through columns: 1) check equality of x/y and id for both models and 2) calculate differences in residual and absolute residual
                string x = null;
                string y = null;
                double diff_res = double.NaN;
                double diff_absres = double.NaN;
                int diff_sign = 0;
                while (!sheet1.IsEmpty(headerIdx1, colIdx))
                {
                    string value1String = sheet1.GetCellValue(rowIdx, colIdx);
                    string value2String = sheet2.GetCellValue(rowIdx, colIdx);
                    if (value1String == null)
                    {
                        value1String = string.Empty;
                    }
                    if (value2String == null)
                    {
                        value2String = string.Empty;
                    }

                    double value1;
                    double value2;
                    if (double.TryParse(value1String.Replace(",", "."), NumberStyles.Any, englishCultureInfo, out value1)
                        && double.TryParse(value2String.Replace(",", "."), NumberStyles.Any, englishCultureInfo, out value2))
                    {
                        // If it's a number round and use english notation
                        long value1Long;
                        if (long.TryParse(value1String, out value1Long))
                        {
                            value1String = value1.ToString();
                        }
                        else
                        {
                            value1String = Math.Round(value1, 5).ToString(englishCultureInfo);
                        }
                        long value2Long;
                        if (long.TryParse(value2String, out value2Long))
                        {
                            value2String = value2Long.ToString();
                        }
                        else
                        {
                            value2String = Math.Round(value2, 5).ToString(englishCultureInfo);
                        }
                    }

                    if (colIdx == 0)
                    {
                        x = value1String;
                    }
                    else if (colIdx == 1)
                    {
                        y = value1String;
                    }
                    else if (colIdx == resColIdx)
                    {
                        double res1;
                        if (!double.TryParse(value1String, NumberStyles.Any, englishCultureInfo, out res1))
                        {
                            throw new Exception("Invalid residual value in cell (" + rowIdx + "," + colIdx + ") in sheet " + sheet1.GetSheetname() + ": " + value1String);
                        }
                        double res2;
                        if (!double.TryParse(value2String, NumberStyles.Any, englishCultureInfo, out res2))
                        {
                            throw new Exception("Invalid residual value in cell (" + rowIdx + "," + colIdx + ") in sheet " + sheet2.GetSheetname() + ": " + value2String);
                        }
                        diff_res = res2 - res1; // res = sim1 - meting; res2 = sim2 - meting; res2 - res1 = sim2 - sim1 ( = effect van aanpassing voor model2)
                        diff_sign = Math.Sign(res2) * Math.Sign(res1);
                    }
                    else if (colIdx == resColIdx + 1)
                    {
                        double absres1;
                        if (!double.TryParse(value1String, NumberStyles.Float, englishCultureInfo, out absres1))
                        {
                            throw new Exception("Invalid residual value in cell (" + rowIdx + "," + colIdx + ") in sheet " + sheet1.GetSheetname() + ": " + value1String);
                        }
                        double absres2;
                        if (!double.TryParse(value2String, NumberStyles.Float, englishCultureInfo, out absres2))
                        {
                            throw new Exception("Invalid residual value in cell (" + rowIdx + "," + colIdx + ") in sheet " + sheet2.GetSheetname() + ": " + value2String);
                        }
                        diff_absres = absres1 - absres2;
                    }

                    if (colIdx <= lastSingleColIdx)
                    {
                        if (!value1String.Equals(value2String))
                        {
                            string row1String = GetRowString(sheet1, rowIdx, colIdx);
                            string row2String = GetRowString(sheet2, rowIdx, colIdx);
                            throw new ToolException("Value mismatch ('" + value1String + "' vs '" + value2String + "') in cell " + SpreadsheetUtils.ExcelColumnFromNumber(colIdx + 1) + (rowIdx + 1) + " in sheets: '" + sheet1.GetSheetname() + "' and '" + sheet2.GetSheetname() + "'" + "\r\nsheet1: " + row1String + "\r\nsheet2: " + row2String + "\r\n" + "Check that calibratiesets and extents match!");
                        }
                        columnValues.Add(value1String);
                    }
                    else
                    {
                        columnValues.Add(value1String);
                        columnValues.Add(value2String);
                    }
                    colIdx++;
                }
                columnValues.Add(diff_res.ToString("F3", englishCultureInfo));
                columnValues.Add(diff_absres.ToString("F3", englishCultureInfo));
                columnValues.Add(Math.Abs(diff_absres).ToString("F3", englishCultureInfo));
                int baseClass = 1;
                columnValues.Add((baseClass + GetLegendClassIdx(diff_absres)).ToString());
                columnValues.Add(diff_sign.ToString());
                ipfFile.AddPoint(new IPFPoint(ipfFile, new FloatPoint(x, y), columnValues));
                rowIdx++;
            }
            ipfFile.WriteFile(ipfFilename);
            //            IPFLegend ipfLegend = (IPFLegend) ipfFile.CreateDifferenceLegend();
            //            ipfLegend.WriteLegendFile(Path.Combine(Path.GetDirectoryName(ipfFilename), Path.GetFileNameWithoutExtension(ipfFilename) + ".leg"));
        }

        protected static string GetRowString(IWorksheet sheet, int rowIdx, int colIdx)
        {
            string rowString = sheet.GetCellValue(rowIdx, colIdx++);
            while (!sheet.IsEmpty(rowIdx, colIdx))
            {
                rowString += ";" + sheet.GetCellValue(rowIdx, colIdx++);
            }
            return rowString;
        }

        protected static int ParseCellIntValue(IWorksheet sheet, int rowIdx, int colIdx)
        {
            string valueString = sheet.GetCellValue(SheetSummaryHeaderRowIdx, 1);
            int value;
            if (!int.TryParse(valueString, out value))
            {
                throw new ToolException("Integer value expected in sheet " + sheet.GetSheetname() + ", cell (" + rowIdx + "," + colIdx + ")" + ": " + valueString + ".");
            }
            return value;
        }

        protected double ParseCellDoubleValue(IWorksheet sheet, int rowIdx, int colIdx)
        {
            string valueString = sheet.GetCellValue(SheetSummaryHeaderRowIdx, 1);
            double value;
            if (!double.TryParse(valueString, NumberStyles.Float, englishCultureInfo, out value))
            {
                throw new ToolException("Double value expected in sheet " + sheet.GetSheetname() + ", cell (" + rowIdx + "," + colIdx + ")" + ": " + valueString + ".");
            }
            return value;
        }

        protected virtual void AddResidualStatistics(IWorksheet sheet, string modelname, string calibrationsetname, List<IPFFile> ipfFileList, int idColNr, int layerColNr, int observedColNr, int simulatedColNr, int residualColNr, int weightColNr, List<int> extraColumnNrs, List<string> extraColumnNames, string metadataString1, string metadataString2, int logIndentLevel, SIFToolSettings settings)
        {
            int obsColIdx = observedColNr - 1;
            int simColIdx = simulatedColNr - 1;
            int resColIdx = residualColNr - 1;

            // Write title
            sheet.SetCellValue(0, 0, "Residual analysis");
            sheet.SetFontBold(0, 0, true);
            sheet.SetCellValue(1, 0, metadataString1);
            sheet.SetCellValue(2, 0, metadataString2);

            // Create and write summary values
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 0, "Layer");
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 1, "N");
            // Note: columnnames of columns 2-6 are written later since they can depend on the residual columnname
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 7, "Min.");
            sheet.SetComment(SheetSummaryHeaderRowIdx, 7, (weightColNr > 0) ? "Minimum of weighted residuals per layer" : "Minimum of residuals per layer");
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 8, "Max.");
            sheet.SetComment(SheetSummaryHeaderRowIdx, 8, (weightColNr > 0) ? "Maximum of weighted residuals per layer" : "Maximum of residuals per layer");
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 9, "Range");
            sheet.SetComment(SheetSummaryHeaderRowIdx, 9, "Difference between Max and Min");

            // Add percentile headers
            for (int pctIdx = 1; pctIdx <= settings.PercentileCount; pctIdx++)
            {
                int percentile = (int)((100.0 / ((float)settings.PercentileCount)) * pctIdx);
                sheet.SetCellValue(SheetSummaryHeaderRowIdx, 9 + pctIdx, percentile.ToString() + "%-pct");
            }
            sheet.SetFontBold(new Range(sheet, SheetSummaryHeaderRowIdx, 0, SheetSummaryHeaderRowIdx, 9 + settings.PercentileCount), true);
            sheet.SetBorderEdgeBottomColor(new Range(sheet, SheetSummaryHeaderRowIdx - 1, 0, SheetSummaryHeaderRowIdx - 1, 9 + settings.PercentileCount), Color.Black);
            sheet.SetBorderEdgeBottomColor(new Range(sheet, SheetSummaryHeaderRowIdx, 0, SheetSummaryHeaderRowIdx, 9 + settings.PercentileCount), Color.Black);

            // Retrieve summary statistics for all layers in specified IPF-files
            List<float> totalWeightList = new List<float>();
            List<float> totalResList = new List<float>();
            List<float> totalWResList = new List<float>();
            List<float> totalAbsResList = new List<float>();
            List<float> totalAbsWResList = new List<float>();
            Dictionary<int, List<float>> layerWeightListDictionary = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> layerResListDictionary = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> layerWResListDictionary = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> layerAbsResListDictionary = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> layerAbsWResListDictionary = new Dictionary<int, List<float>>();
            int filenameLayer = -1;

            string resColName = null;
            int sheetRowIdx = SheetSummaryHeaderRowIdx + 1;
            bool isEmptyStringValueSkipped = settings.SkippedValues.Contains(float.NaN);
            int maskedTotalPointCount = 0;
            int srcTotalPointCount = 0;
            for (int ipfIdx = 0; ipfIdx < ipfFileList.Count; ipfIdx++)
            {
                IPFFile ipfFile = ipfFileList[ipfIdx];
                Log.AddInfo("Processing file " + Path.GetFileName(ipfFile.Filename) + " ...", logIndentLevel + 1);

                // Process all points/rows in the current IPF-file
                int maskedPointCount = 0;
                int srcPointCount = ipfFile.PointCount;
                srcTotalPointCount += srcPointCount;
                for (int rowIdx = 0; rowIdx < ipfFile.PointCount; rowIdx++)
                {
                    IPFPoint ipfPoint = ipfFile.GetPoint(rowIdx);
                    List<string> ipfColumnValues = ipfPoint.ColumnValues;

                    // A negative residualColIdx specifies a columnindex relatieve to the last column
                    obsColIdx = (observedColNr < 0) ? (ipfColumnValues.Count + observedColNr) : observedColNr - 1;
                    simColIdx = (simulatedColNr < 0) ? (ipfColumnValues.Count + simulatedColNr) : simulatedColNr - 1;
                    resColIdx = (residualColNr < 0) ? (ipfColumnValues.Count + residualColNr) : residualColNr - 1;
                    resColName = ipfFile.ColumnNames[resColIdx];

                    if ((obsColIdx >= ipfColumnValues.Count) || (obsColIdx < -1))
                    {
                        throw new ToolException("Invalid actual observed value column number (" + (obsColIdx + 1) + "): smaller than zero or larger than number of columnvalues in IPF-file (" + ipfColumnValues.Count + "): " + Path.GetFileName(ipfFile.Filename));
                    }
                    if ((simColIdx >= ipfColumnValues.Count) || (simColIdx < -1))
                    {
                        throw new ToolException("Invalid actual simulated value column index (" + (simColIdx + 1) + "): smaller than zero or larger than number of columnvalues in IPF-file (" + ipfColumnValues.Count + "): " + Path.GetFileName(ipfFile.Filename));
                    }
                    if ((resColIdx >= ipfColumnValues.Count) || (resColIdx < -1))
                    {
                        throw new ToolException("Invalid actual residual column index (" + (resColIdx + 1) + "): smaller than zero or larger than number of columnvalues in IPF-file (" + ipfColumnValues.Count + "): " + Path.GetFileName(ipfFile.Filename));
                    }

                    // Try to parse layernumber
                    int layer = ParseLayerNr(ipfFile.Filename, rowIdx, ipfColumnValues, layerColNr);

                    bool isMasked = false;
                    isMasked = IsIPFPointMasked(ipfPoint, layer, settings, Log, logIndentLevel);
                    if (!isMasked)
                    {
                        if (!layerResListDictionary.ContainsKey(layer))
                        {
                            // Add empty containers for residuals and absolute residuals if not yet present for this layer
                            layerWeightListDictionary.Add(layer, new List<float>());
                            layerResListDictionary.Add(layer, new List<float>());
                            layerWResListDictionary.Add(layer, new List<float>());
                            layerAbsResListDictionary.Add(layer, new List<float>());
                            layerAbsWResListDictionary.Add(layer, new List<float>());
                        }

                        // Parse residual values
                        float weight = 1.0f;
                        if (weightColNr > 0)
                        {
                            if (!float.TryParse(ipfColumnValues[weightColNr - 1], NumberStyles.Float, englishCultureInfo, out weight))
                            {
                                throw new ToolException("Invalid weight value " + rowIdx + ": " + ipfColumnValues[weightColNr - 1]);
                            }
                        }

                        // Parse observed, simulated and residual value
                        float observedValue = float.NaN;
                        float simulatedValue = float.NaN;
                        float resValue = float.NaN;
                        string observedValueString = (obsColIdx != -1) ? ipfColumnValues[obsColIdx] : null;
                        string simulatedValueString = (simColIdx != -1) ? ipfColumnValues[simColIdx] : null;
                        string resValueString = ipfColumnValues[resColIdx];
                        if ((observedValueString != null))
                        {
                            if (!float.TryParse(observedValueString, NumberStyles.Float, englishCultureInfo, out observedValue))
                            {
                                if (!isEmptyStringValueSkipped || !observedValueString.Equals(string.Empty))
                                {
                                    throw new ToolException("Unexpected observed value: " + observedValueString);
                                }
                                else
                                {
                                    observedValue = float.NaN;
                                }
                            }
                        }
                        if (simulatedValueString != null)
                        {
                            if (!float.TryParse(simulatedValueString, NumberStyles.Float, englishCultureInfo, out simulatedValue))
                            {
                                throw new ToolException("Unexpected simulated value: " + simulatedValueString);
                            }
                        }
                        if (!float.TryParse(resValueString, NumberStyles.Float, englishCultureInfo, out resValue))
                        {
                            throw new ToolException("Unexpected residual value: " + resValueString);
                        }

                        if ((observedValue.Equals(float.NaN) || !settings.SkippedValues.Contains(observedValue)) &&
                            (simulatedValue.Equals(float.NaN) || !settings.SkippedValues.Contains(simulatedValue)) &&
                            !settings.SkippedValues.Contains(resValue))
                        {
                            // calculate weighted residual
                            float weighedResidual = resValue * weight;

                            // Store values
                            layerWeightListDictionary[layer].Add(weight);
                            layerResListDictionary[layer].Add(resValue);
                            layerWResListDictionary[layer].Add(weighedResidual);
                            layerAbsResListDictionary[layer].Add(Math.Abs(resValue));
                            layerAbsWResListDictionary[layer].Add(Math.Abs(weighedResidual));

                            totalWeightList.Add(weight);
                            totalResList.Add(resValue);
                            totalWResList.Add(weighedResidual);
                            totalAbsResList.Add(Math.Abs(resValue));
                            totalAbsWResList.Add(Math.Abs(weighedResidual));
                        }
                    }
                    else
                    {
                        maskedPointCount++;
                    }
                }
                if (maskedPointCount > 0)
                {
                    maskedTotalPointCount += maskedPointCount;
                    Log.AddInfo(maskedPointCount + " / " + srcPointCount + " points were masked", logIndentLevel + 2);
                }
            }

            if (maskedTotalPointCount > 0)
            {
                Log.AddInfo(maskedTotalPointCount + " / " + srcTotalPointCount + " points were masked in total", logIndentLevel + 0);
            }

            // Write columnnames
            bool isWeighed = (weightColNr > 0);
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 2, RetrieveSheetColumnName(ResidualStatistic.ME, isWeighed, settings, resColName));
            sheet.SetComment(SheetSummaryHeaderRowIdx, 2, RetrieveSheetColumnComment(ResidualStatistic.ME, isWeighed));
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 3, RetrieveSheetColumnName(ResidualStatistic.MAE, isWeighed, settings, resColName));
            sheet.SetComment(SheetSummaryHeaderRowIdx, 3, RetrieveSheetColumnComment(ResidualStatistic.MAE, isWeighed));
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 4, RetrieveSheetColumnName(ResidualStatistic.SDE, isWeighed, settings, resColName));
            sheet.SetComment(SheetSummaryHeaderRowIdx, 4, RetrieveSheetColumnComment(ResidualStatistic.MAE, isWeighed));
            // Add (WM)RMSE and (W)SSE
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 5, RetrieveSheetColumnName(ResidualStatistic.RMSE, isWeighed, settings, resColName));
            sheet.SetComment(SheetSummaryHeaderRowIdx, 5, RetrieveSheetColumnComment(ResidualStatistic.MAE, isWeighed));
            sheet.SetCellValue(SheetSummaryHeaderRowIdx, 6, RetrieveSheetColumnName(ResidualStatistic.SSE, isWeighed, settings, resColName));
            sheet.SetComment(SheetSummaryHeaderRowIdx, 6, RetrieveSheetColumnComment(ResidualStatistic.MAE, isWeighed));

            // Now calculated derived statistics
            float totalWeightSum;
            float totalWeigthtMax;
            float totalWMeanRes;
            Dictionary<int, float> layerWeightMaxDictionary = new Dictionary<int, float>();
            Dictionary<int, float> layerWeightSumDictionary = new Dictionary<int, float>();
            Dictionary<int, float> layerWMeanResDictionary = new Dictionary<int, float>();
            List<float> totalSquaredResList = new List<float>();
            Dictionary<int, List<float>> layerSquaredResValueListDictionary = new Dictionary<int, List<float>>();
            
            Series series = new Series(totalWeightList);
            totalWeightSum = series.Sum();
            totalWeigthtMax = series.Max();
            totalWMeanRes = new Series(totalWResList).Average();
            foreach (int layer in layerResListDictionary.Keys)
            {
                List<float> weightList = layerWeightListDictionary[layer];
                List<float> resList = layerResListDictionary[layer];
                List<float> wresList = layerWResListDictionary[layer];

                series = new Series(weightList);
                layerWeightMaxDictionary.Add(layer, series.Max());
                float layerWeightSum = series.Sum();
                layerWeightSumDictionary.Add(layer, layerWeightSum);
                series = new Series(wresList);
                layerWMeanResDictionary.Add(layer, series.Sum() / layerWeightSum);

                // Calculate SSE (Sum Squared Error) statistics
                layerSquaredResValueListDictionary.Add(layer, new List<float>());
                for (int idx = 0; idx < resList.Count; idx++)
                {
                    float resValue = resList[idx];
                    float weight = weightList[idx];

                    // Calculate (weighted) squared error values
                    float wseValue = weight * resValue * resValue;

                    layerSquaredResValueListDictionary[layer].Add(wseValue);
                    totalSquaredResList.Add(wseValue);
                }
            }

            // Calculate weighted standard deviations
            float totalWSD;
            float totalWSDSumPart = 0;
            Dictionary<int, float> layerWSDDictionary = new Dictionary<int, float>();
            foreach (int layer in layerResListDictionary.Keys)
            {
                List<float> weightList = layerWeightListDictionary[layer];
                List<float> resList = layerResListDictionary[layer];

                float layerWSDSumPart = 0;
                float layerWMeanRes = layerWMeanResDictionary[layer];
                for (int idx = 0; idx < resList.Count; idx++)
                {
                    float resValue = resList[idx];
                    float weight = weightList[idx];
                    float diff = (resValue - layerWMeanRes);
                    layerWSDSumPart += weight * diff * diff;
                }
                totalWSDSumPart += layerWSDSumPart;
                float layerWSD = (float)Math.Sqrt(layerWSDSumPart / (layerWeightSumDictionary[layer] * (weightList.Count - 1) / weightList.Count));
                layerWSDDictionary.Add(layer, layerWSD);
            }
            totalWSD = (float)Math.Sqrt(totalWSDSumPart / (totalWeightSum * (totalWeightList.Count - 1) / totalWeightList.Count));

            // Write summary statistics of this model and calibration set to this sheet
            Log.AddInfo( "Creating residual sheet ...", logIndentLevel);
            Series resSeries;
            Series absResSeries;
            Series squaredResSeries;
            foreach (int layer in layerResListDictionary.Keys)
            {
                // Use statistics of weighted residuals also for unweighted request. In the last case the weights will be 1 and give the same result
                resSeries = new Series(layerWResListDictionary[layer]);
                absResSeries = new Series(layerAbsWResListDictionary[layer]);
                squaredResSeries = new Series(layerSquaredResValueListDictionary[layer]);

                sheet.SetCellValue(sheetRowIdx, 0, layer);
                sheet.SetCellValue(sheetRowIdx, 1, resSeries.Count);

                if (resSeries.Count > 0)
                {
                    float resAvg = resSeries.Sum() / layerWeightSumDictionary[layer];
                    sheet.SetCellValue(sheetRowIdx, 2, resAvg);
                    SetResidualLegend(sheet, sheetRowIdx, 2, resAvg);

                    float absResAvg = absResSeries.Sum() / layerWeightSumDictionary[layer];
                    sheet.SetCellValue(sheetRowIdx, 3, absResAvg);
                    SetAbsResidualLegend(sheet, sheetRowIdx, 3, absResAvg);

                    if (!layerWSDDictionary[layer].Equals(float.NaN))
                    {
                        sheet.SetCellValue(sheetRowIdx, 4, layerWSDDictionary[layer]);
                    }
                    else
                    {
                        sheet.SetCellValue(sheetRowIdx, 4, "NaN");
                    }

                    float wrmse = (float)Math.Sqrt(squaredResSeries.Sum() / layerWeightSumDictionary[layer]);
                    sheet.SetCellValue(sheetRowIdx, 5, wrmse);
                    float wsse = squaredResSeries.Sum();
                    sheet.SetCellValue(sheetRowIdx, 6, wsse);
                    sheet.SetCellValue(sheetRowIdx, 7, resSeries.Min());
                    sheet.SetCellValue(sheetRowIdx, 8, resSeries.Max());
                    sheet.SetCellValue(sheetRowIdx, 9, resSeries.Max() - resSeries.Min());
                }

                // Add percentiles for this layer
                float[] percentiles = resSeries.Percentiles();
                if (percentiles != null)
                {
                    for (int pctIdx = 1; pctIdx <= settings.PercentileCount; pctIdx++)
                    {
                        int percentile = (int)((100.0 / ((float)settings.PercentileCount)) * pctIdx);
                        sheet.SetCellValue(sheetRowIdx, 9 + pctIdx, percentiles[percentile]);
                    }
                }

                sheetRowIdx++;
            }

            sheet.SetBorderEdgeBottomColor(new Range(sheet, sheetRowIdx - 1, 0, sheetRowIdx - 1, 9 + settings.PercentileCount), Color.Black);
            sheet.SetBorderEdgeBottomColor(new Range(sheet, sheetRowIdx, 0, sheetRowIdx, 9 + settings.PercentileCount), Color.Black);

            // Calculate total statistics
            resSeries = new Series(totalWResList);
            absResSeries = new Series(totalAbsWResList);
            squaredResSeries = new Series(totalSquaredResList);


            sheet.SetCellValue(sheetRowIdx, 0, "Total");
            sheet.SetComment(sheetRowIdx, 0, "Total over all residuals");
            sheet.SetCellValue(sheetRowIdx, 1, resSeries.Count);

            for (int colIdx = 0; colIdx < 20; colIdx++)
            {
                sheet.SetColumnWidth(colIdx, 12);
            }

            if (resSeries.Count > 0)
            {
                float totalResAvg = (float)Math.Round(resSeries.Sum() / totalWeightSum, DecimalCount);

                sheet.SetCellValue(sheetRowIdx, 2, (float)Math.Round(totalResAvg, DecimalCount));
                SetResidualLegend(sheet, sheetRowIdx, 2, totalResAvg);

                float totalAbsResAvg = absResSeries.Sum() / totalWeightSum;
                sheet.SetCellValue(sheetRowIdx, 3, (float)Math.Round(totalAbsResAvg, DecimalCount));
                SetAbsResidualLegend(sheet, sheetRowIdx, 3, totalAbsResAvg);

                sheet.SetCellValue(sheetRowIdx, 4, (float)Math.Round(totalWSD, DecimalCount));
                sheet.SetCellValue(sheetRowIdx, 5, Math.Sqrt(squaredResSeries.Sum() / totalWeightSum)); // WRMSE
                sheet.SetCellValue(sheetRowIdx, 6, Math.Round(squaredResSeries.Sum(), DecimalCount));   // WSSE
                sheet.SetCellValue(sheetRowIdx, 7, Math.Round(resSeries.Min(), DecimalCount));
                sheet.SetCellValue(sheetRowIdx, 8, Math.Round(resSeries.Max(), DecimalCount));
                sheet.SetCellValue(sheetRowIdx, 9, Math.Round(resSeries.Max() - resSeries.Min(), DecimalCount));

                // Add percentiles for total-layer
                float[] totalPercentiles = resSeries.Percentiles();
                if (totalPercentiles != null)
                {
                    for (int pctIdx = 1; pctIdx <= settings.PercentileCount; pctIdx++)
                    {
                        int percentile = (int)((100.0 / ((float)settings.PercentileCount)) * pctIdx);
                        sheet.SetCellValue(sheetRowIdx, 9 + pctIdx, (float)Math.Round(totalPercentiles[percentile], DecimalCount));
                    }
                }
            }

            sheet.SetNumberFormat(new Range(sheet, SheetSummaryHeaderRowIdx + 1, 2, sheetRowIdx, 9 + settings.PercentileCount), "0.00");
            sheet.SetFontBold(new Range(sheet, sheetRowIdx, 0, sheetRowIdx, 9 + settings.PercentileCount), true);
            sheet.SetColumnWidth(6, 12);

            WriteResidualLegend(sheet, SheetSummaryHeaderRowIdx, 9 + settings.PercentileCount + 2);
            sheetRowIdx += 2;
            int chartRowIdx = sheetRowIdx;

            // Create list with all columnNames for filter values;
            sheetRowIdx += 16;
            List<string> columnNames = new List<string>(new string[] { "X", "Y" });
            List<string> columnNameRemarks = new List<string>(new string[] { string.Empty, string.Empty });
            if (idColNr > 0)
            {
                columnNames.Add(ipfFileList[0].ColumnNames[idColNr - 1]);
                columnNameRemarks.Add(string.Empty);
            }
            columnNames.Add((layerColNr == 0) ? "FNameLayer" : ipfFileList[0].ColumnNames[layerColNr - 1]);
            columnNameRemarks.Add("Modellayer that filter is assigned to");
            if (obsColIdx != -1)
            {
                columnNames.Add(ipfFileList[0].ColumnNames[obsColIdx]);
                columnNameRemarks.Add("Observed measure");
            }
            if (simColIdx != -1)
            {
                columnNames.Add(ipfFileList[0].ColumnNames[simColIdx]);
                columnNameRemarks.Add("Simulated measure");
            }
            columnNames.Add(ipfFileList[0].ColumnNames[resColIdx]);
            columnNameRemarks.Add("Residual value");
            columnNames.Add("Abs(" + ipfFileList[0].ColumnNames[resColIdx] + ")");
            columnNameRemarks.Add("Absolute residual value");
            if (weightColNr > 0)
            {
                // Add weight
                columnNames.Add(ipfFileList[0].ColumnNames[weightColNr - 1]);
                columnNameRemarks.Add("Weight");
                // Add weighted residual
                columnNames.Add("WSE");
                columnNameRemarks.Add("Weighted Squared Error");
                // Add column for normalized weight
                columnNames.Add("Norm." + ipfFileList[0].ColumnNames[weightColNr - 1]);
                columnNameRemarks.Add("Normalized weight: weight divided by max. weight in modellayer");
            }
            for (int extraIdx = 0; extraIdx < extraColumnNrs.Count; extraIdx++)
            {
                int extraColNr = extraColumnNrs[extraIdx];
                if ((extraColNr < ipfFileList[0].ColumnCount) && (extraColNr > 0))
                {
                    // Skip columns that have been used already as obligatory columns
                    if ((extraColNr > 1) && (extraColNr != idColNr) && (extraColNr != layerColNr) && (extraColNr != residualColNr))
                    {
                        if (extraColumnNames.Count == 0)
                        {
                            columnNames.Add(ipfFileList[extraColNr].ColumnNames[extraColNr - 1]);
                            columnNameRemarks.Add(string.Empty);
                        }
                        else
                        {
                            columnNames.Add(extraColumnNames[extraIdx]);
                            columnNameRemarks.Add(string.Empty);
                        }
                    }
                }
                else
                {
                    throw new Exception("Undefined column index " + extraColNr);
                }
            }

            // Write columnnames
            int filterHeaderRowIdx = sheetRowIdx;
            for (int colIdx = 0; colIdx < columnNames.Count; colIdx++)
            {
                sheet.SetCellValue(sheetRowIdx, colIdx, columnNames[colIdx]);
                sheet.SetFontBold(sheetRowIdx, colIdx, true);
                sheet.SetComment(sheetRowIdx, colIdx, columnNameRemarks[colIdx]);
            }
            sheet.SetFontBold(new Range(sheet, filterHeaderRowIdx, 0, filterHeaderRowIdx, columnNames.Count - 1), true);
            sheet.SetBorderEdgeBottomColor(new Range(sheet, filterHeaderRowIdx, 0, filterHeaderRowIdx, columnNames.Count - 1), Color.Black);
            sheet.SetBorderEdgeBottomColor(new Range(sheet, filterHeaderRowIdx - 1, 0, filterHeaderRowIdx - 1, columnNames.Count - 1), Color.Black);

            // Write values
            sheetRowIdx++;
            float minObservedValue = float.MaxValue;
            float maxObservedValue = float.MinValue;
            float minSimulatedValue = float.MaxValue;
            float maxSimulatedValue = float.MinValue;
            IPFFile skippedIPF = new IPFFile();
            skippedIPF.ColumnNames = columnNames;
            List<float> residualValues = new List<float>();
            for (int ipfIdx = 0; ipfIdx < ipfFileList.Count; ipfIdx++)
            {
                IPFFile ipfFile = ipfFileList[ipfIdx];

                if (layerColNr == 0)
                {
                    // try to find layernumber from filename
                    string filename = Path.GetFileName(ipfFile.Filename);
                    filenameLayer = IMODUtils.GetLayerNumber(filename);
                }

                // Check that indices are valid
                if (idColNr >= ipfFile.ColumnCount)
                {
                    throw new ToolException("Id column index (" + idColNr + ") is above columncount of IPF-file: " + Path.GetFileName(ipfFile.Filename));
                }
                if (layerColNr >= ipfFile.ColumnCount)
                {
                    throw new ToolException("Layer column index (" + layerColNr + ") is above columncount of IPF-file: " + Path.GetFileName(ipfFile.Filename));
                }
                if (observedColNr >= ipfFile.ColumnCount)
                {
                    throw new ToolException("Observed value column index (" + observedColNr + ") is above columncount of IPF-file: " + Path.GetFileName(ipfFile.Filename));
                }
                if (simulatedColNr >= ipfFile.ColumnCount)
                {
                    throw new ToolException("Simulated value column index (" + simulatedColNr + ") is above columncount of IPF-file: " + Path.GetFileName(ipfFile.Filename));
                }
                if (residualColNr >= ipfFile.ColumnCount)
                {
                    throw new ToolException("Residual value column index (" + residualColNr + ") is above columncount of IPF-file: " + Path.GetFileName(ipfFile.Filename));
                }

                // Write actual values
                for (int rowIdx = 0; rowIdx < ipfFile.PointCount; rowIdx++)
                {
                    List<string> ipfColumnValues = ipfFile.GetColumnValues(rowIdx);
                    List<object> rowValues = new List<object>();
                    int sheetColIdx = 0;
                    int layer;
                    try
                    {
                        layer = (layerColNr == 0) ? filenameLayer : (int)double.Parse(ipfColumnValues[layerColNr - 1], englishCultureInfo);
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Could not parse layer value for row " + (rowIdx + 1) + " in IPF-file: " + Path.GetFileName(ipfFile.Filename), ex);
                    }

                    IPFPoint ipfPoint = ipfFile.Points[rowIdx];
                    bool isSkipped = IsIPFPointMasked(ipfPoint, layer, settings, Log, logIndentLevel);

                    // Add X, Y, Id, layer and residual values first
                    double x;
                    if (double.TryParse(ipfColumnValues[0], NumberStyles.Float, englishCultureInfo, out x))
                    {
                        rowValues.Add(x);
                    }
                    else
                    {
                        throw new ToolException("Unexpected value for X-coordinate: " + ipfColumnValues[0]);
                    }
                    double y;
                    if (double.TryParse(ipfColumnValues[1], NumberStyles.Float, englishCultureInfo, out y))
                    {
                        rowValues.Add(y);
                    }
                    else
                    {
                        throw new ToolException("Unexpected value for Y-coordinate: " + ipfColumnValues[1]);
                    }

                    if (idColNr > 0)
                    {
                        rowValues.Add(ipfColumnValues[idColNr - 1]);
                    }
                    rowValues.Add(layer);

                    float observedValue = float.NaN;
                    if (obsColIdx != -1)
                    {
                        if (float.TryParse(ipfColumnValues[obsColIdx], NumberStyles.Float, englishCultureInfo, out observedValue))
                        {
                            rowValues.Add(observedValue);
                            if (settings.SkippedValues.Contains(observedValue))
                            {
                                isSkipped = true;
                            }
                        }
                    }

                    float simulatedValue = float.NaN;
                    if (simColIdx != -1)
                    {
                        if (float.TryParse(ipfColumnValues[simColIdx], NumberStyles.Float, englishCultureInfo, out simulatedValue))
                        {
                            rowValues.Add(simulatedValue);
                            if (settings.SkippedValues.Contains(simulatedValue))
                            {
                                isSkipped = true;
                            }
                        }
                    }

                    float weight = float.NaN;
                    float wseValue = float.NaN;
                    float normWeight = float.NaN;
                    if (weightColNr > 0)
                    {
                        if (!float.TryParse(ipfColumnValues[weightColNr - 1], NumberStyles.Float, englishCultureInfo, out weight))
                        {
                            throw new ToolException("Unexpected weight value: " + ipfColumnValues[weightColNr - 1]);
                        }
                    }
                    float resValue;
                    int resIdx;
                    if (float.TryParse(ipfColumnValues[resColIdx], NumberStyles.Float, englishCultureInfo, out resValue))
                    {
                        resIdx = rowValues.Count;
                        rowValues.Add(resValue);
                        rowValues.Add(Math.Abs(resValue));

                        if (!isSkipped && !settings.SkippedValues.Contains(resValue))
                        {
                            residualValues.Add(resValue);
                            if (weightColNr > 0)
                            {
                                // Calculate weighted squared error
                                wseValue = weight * resValue * resValue;

                                // Calculate a normalized weight, with a maximum of 1.0
                                normWeight = weight / layerWeightMaxDictionary[layer];

                                rowValues.Add(weight);
                                rowValues.Add(wseValue);
                                rowValues.Add(normWeight);
                            }
                        }
                        else
                        {
                            if (weightColNr > 0)
                            {
                                rowValues.Add(null);
                                rowValues.Add(null);
                            }
                            isSkipped = true;
                        }
                    }
                    else
                    {
                        throw new ToolException("Unexpected residual value: " + ipfColumnValues[resColIdx]);
                    }

                    foreach (int colIdx in extraColumnNrs)
                    {
                        if ((colIdx < ipfColumnValues.Count) && (colIdx > 0))
                        {
                            // Skip columns that have been used already as obligatory columns
                            if ((colIdx > 1) && (colIdx != idColNr) && (colIdx != layerColNr) && (colIdx != residualColNr))
                            {
                                string value = ipfColumnValues[colIdx - 1];

                                // Check if the value is numeric
                                if (double.TryParse(value, NumberStyles.Float, englishCultureInfo, out double dblValue))
                                {
                                    rowValues.Add(dblValue);
                                }
                                else
                                {
                                    if (long.TryParse(value, NumberStyles.Integer, englishCultureInfo, out long lngValue))
                                    {
                                        rowValues.Add(lngValue);
                                    }
                                    else
                                    {
                                        // Add as it is, a string
                                        rowValues.Add(value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new ToolException("Undefined column index " + colIdx);
                        }
                    }

                    if (isSkipped)
                    {
                        IPFPoint skippedPoint = new IPFPoint(skippedIPF, new FloatPoint((float)x, (float)y), ConvertObjectsToString(rowValues.ToArray()));
                        skippedIPF.AddPoint(skippedPoint);
                    }
                    else
                    {
                        // Update statistics for scatterplot
                        if (observedValue < minObservedValue)
                        {
                            minObservedValue = observedValue;
                        }
                        if (observedValue > maxObservedValue)
                        {
                            maxObservedValue = observedValue;
                        }

                        if (simulatedValue < minSimulatedValue)
                        {
                            minSimulatedValue = simulatedValue;
                        }
                        if (simulatedValue > maxSimulatedValue)
                        {
                            maxSimulatedValue = simulatedValue;
                        }

                        // Write values for selected point
                        for (int idx = 0; idx < rowValues.Count; idx++)
                        {
                            if (rowValues[idx] is int)
                            {
                                sheet.SetCellValue(sheetRowIdx, sheetColIdx++, (int)rowValues[idx]);
                            }
                            else if (rowValues[idx] is long)
                            {
                                sheet.SetCellValue(sheetRowIdx, sheetColIdx++, (long)rowValues[idx]);
                            }
                            else if (rowValues[idx] is float)
                            {
                                sheet.SetCellValue(sheetRowIdx, sheetColIdx++, (float)Math.Round((float)rowValues[idx], DecimalCount));
                            }
                            else if (rowValues[idx] is double)
                            {
                                sheet.SetCellValue(sheetRowIdx, sheetColIdx++, Math.Round((double)rowValues[idx], DecimalCount));
                            }
                            else
                            {
                                sheet.SetCellValue(sheetRowIdx, sheetColIdx++, rowValues[idx].ToString());
                            }

                            if (idx == resIdx)
                            {
                                SetResidualLegend(sheet, sheetRowIdx, sheetColIdx - 1, resValue);
                            }
                            if (idx == (resIdx + 1))
                            {
                                SetAbsResidualLegend(sheet, sheetRowIdx, sheetColIdx - 1, Math.Abs(resValue));
                            }
                        }
                        sheetRowIdx++;
                    }
                }
            }

            int lastColIdx = sheet.End(new Cell(sheet, filterHeaderRowIdx, 6), CellDirection.ToRight).ColIdx;
            int lastRowIdx = sheet.End(new Cell(sheet, filterHeaderRowIdx, 1), CellDirection.Down).RowIdx;
            if (lastRowIdx > filterHeaderRowIdx)
            {
                sheet.SetNumberFormat(new Range(sheet, filterHeaderRowIdx + 1, 3, sheetRowIdx - 1, 10), "0.00");
                sheet.SetAutoFilter(new Range(sheet, filterHeaderRowIdx, 0, filterHeaderRowIdx, lastColIdx));
            }

            // Add scatterplot
            if (!minObservedValue.Equals(float.MaxValue) && !minSimulatedValue.Equals(float.MaxValue))
            {
                IChart scatterPlotChart = sheet.CreateChart(new Range(sheet, chartRowIdx, 0, chartRowIdx + 15, 8), ChartType.ScatterChart);
                int resultObsColIdx = (idColNr > 0) ? 4 : 3;
                scatterPlotChart.AddData(new Range(sheet, filterHeaderRowIdx + 1, resultObsColIdx, sheetRowIdx - 1, resultObsColIdx), new Range(sheet, filterHeaderRowIdx + 1, resultObsColIdx + 1, sheetRowIdx - 1, resultObsColIdx + 1));
                scatterPlotChart.SetXAxis(minObservedValue, maxObservedValue, "Observed Values");
                scatterPlotChart.SetYAxis(minSimulatedValue, maxSimulatedValue, "Computed Values");
                scatterPlotChart.SetMarkerSize(0, 4);
                scatterPlotChart.SetMarkerStyle(0, MarkerStyle.Square);
                scatterPlotChart.SetMarkerColor(0, Color.FromArgb(192, 0, 0));
                scatterPlotChart.DeleteLegend();
                scatterPlotChart.AddTrendLine(0);
                // chart.AddData(new double[] { 1, 2, 3, 4, 5 }, new double[] { 2, 3, 4, 5, 6 });
            }

            // Add histogram
            RetrieveHistogramClasses(residualValues, out List<float> histogramClasses, out int binCount, settings);
            if (binCount > 0)
            {
                // Initialize arrays
                string[] binLabels = new string[binCount];
                int[] binValueCounts = new int[binCount];
                List<float>[] bins = new List<float>[binCount];
                for (int idx = 0; idx < binCount; idx++)
                {
                    bins[idx] = new List<float>();
                    binLabels[idx] = histogramClasses[idx].ToString("0.00") + " - " + histogramClasses[idx + 1].ToString("0.00");
                }

                // Fill bins
                foreach (float resValue in residualValues)
                {
                    int binIdx = 0;
                    while ((binIdx < binCount) && (resValue > histogramClasses[binIdx + 1]))
                    {
                        binIdx++;
                    }
                    if (binIdx == binCount)
                    {
                        binIdx = binCount - 1;
                    }
                    bins[binIdx].Add(resValue);
                    binValueCounts[binIdx]++;
                }
                sheet.SetCellValue(filterHeaderRowIdx, lastColIdx + 2, "BinMin");
                sheet.SetCellValue(filterHeaderRowIdx, lastColIdx + 3, "BinCount");
                sheet.SetFontBold(new Range(sheet, filterHeaderRowIdx, lastColIdx + 2, filterHeaderRowIdx, lastColIdx + 3), true);
                int maxBinCount = 0;
                for (int idx = 0; idx < binCount; idx++)
                {
                    sheet.SetCellValue(filterHeaderRowIdx + 1 + idx, lastColIdx + 2, binLabels[idx]);
                    sheet.SetCellValue(filterHeaderRowIdx + 1 + idx, lastColIdx + 3, binValueCounts[idx]);
                    // For debugging: write actual bin-values to cells behind binclasses
                    //for (int idx2 = 0; idx2 < bins[idx].Count; idx2++)
                    //{
                    //    sheet.SetCellValue(filterHeaderRowIdx + 1 + idx, lastColIdx + 4 + idx2, bins[idx][idx2]);
                    //}
                    if (binValueCounts[idx] > maxBinCount)
                    {
                        maxBinCount = (int)binValueCounts[idx];
                    }
                }
                // For debugging: sheet.SetCellValue(filterHeaderRowIdx + 1 + binCount + 1, lastColIdx + 2, Math.Round(binValueCounts[binCount] + binSize, 2));
                sheet.SetNumberFormat(new Range(sheet, filterHeaderRowIdx + 1, lastColIdx + 2, filterHeaderRowIdx + 1 + binCount, lastColIdx + 2), "0.00");

                int histColIdx1 = 9;
                // Let histogram width depend on number of classes, do not make to wide to have Excel formatt labels vertically
                int histColIdx2 = histColIdx1 + (int)(binCount / 3.5) + 1;

                IChart histogramChart = sheet.CreateChart(new Range(sheet, chartRowIdx, histColIdx1, chartRowIdx + 15, histColIdx2), ChartType.ColumnClustered);
                histogramChart.AddData(new Range(sheet, filterHeaderRowIdx + 1, lastColIdx + 2, filterHeaderRowIdx + 1 + binCount, lastColIdx + 2),
                    new Range(sheet, filterHeaderRowIdx + 1, lastColIdx + 3, filterHeaderRowIdx + 1 + binCount, lastColIdx + 3));
                histogramChart.SetXAxis(0, binCount + 1, "residual (m)");
                histogramChart.SetBarGapWidth(10);
                histogramChart.DeleteLegend();
            }

            sheet.Zoom(85);

            if ((skippedIPF != null) && (skippedIPF.PointCount > 0))
            {
                skippedIPF.WriteFile(Path.Combine(Path.GetDirectoryName(settings.OutputFilename), Path.GetFileNameWithoutExtension(settings.OutputFilename) + "_skipped.ipf"));
            }
        }

        /// <summary>
        /// Retrieve layernumber from filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="pointIdx"></param>
        /// <param name="ipfColumnValues"></param>
        /// <param name="layerColNr"></param>
        /// <returns>layernumber or an Exception if not found</returns>
        private int ParseLayerNr(string filename, int pointIdx, List<string> ipfColumnValues, int layerColNr)
        {
            int filenameLayerNr;
            int layerNr = -1;
            if (layerColNr == 0)
            {
                // try to find layernumber from filename
                filename = Path.GetFileName(filename);
                try
                {
                    filenameLayerNr = IMODUtils.GetLayerNumber(filename);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Unexpected layer value (last digits, after '_L') for file: " + filename, ex);
                }
                layerNr = filenameLayerNr;
                if (layerNr <= 0)
                {
                    throw new ToolException("Invalid layer value for file: " + filename + ": " + layerNr);
                }
            }
            else if (double.TryParse(ipfColumnValues[layerColNr - 1], NumberStyles.Float, englishCultureInfo, out double dblLayer))
            {
                layerNr = (int)dblLayer;
            }
            else
            {
                throw new ToolException("Unexpected layer value for point " + pointIdx + ": " + ipfColumnValues[layerColNr - 1]);
            }

            return layerNr;
        }

        protected virtual bool IsIPFPointMasked(IPFPoint ipfPoint, int layer, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            bool isSkipped = false;
            if (settings.MaskIDFFilename != null)
            {
                if (maskIDFFile == null)
                {
                    Log.AddInfo("Reading pointer IDF-file: " + Path.GetFileName(settings.MaskIDFFilename) + " ...", logIndentLevel);
                    maskIDFFile = IDFFile.ReadFile(settings.MaskIDFFilename);
                }

                float pointerValue = maskIDFFile.GetValue((float)ipfPoint.X, (float)ipfPoint.Y);
                if (!pointerValue.Equals(float.NaN) && !pointerValue.Equals(maskIDFFile.NoDataValue))
                {
                    for (int i = 0; i < settings.MaskIDFValues.Length; i++)
                    {
                        if (pointerValue.Equals(settings.MaskIDFValues[i]))
                        {
                            isSkipped = true;
                            break;
                            // If pointervalue is NoData don't skip
                        }
                    }
                }
                else
                {
                    isSkipped = true;
                }
            }

            return isSkipped;
        }

        protected virtual void RetrieveHistogramClasses(List<float> residualValues, out List<float> histogramClasses, out int binCount, SIFToolSettings settings)
        {
            Statistics.Statistics stats = new Statistics.Statistics(residualValues);
            stats.ComputePercentiles(true, true);
            float minValue = stats.Min;
            float maxValue = stats.Max;
            float IQR = stats.IQR;

            // calculate binsize with Freedman-Diaconis rule: https://en.wikipedia.org/wiki/Freedman%E2%80%93Diaconis_rule. 
            // The bin-width is set to h=2×IQR×n^−1/3; The number number of bins is: (max−min)/h, where n is the number of observations, max is the maximum value and min is the minimum value.
            float binSize = 2 * IQR * ((float)Math.Pow(residualValues.Count(), -1.0 / 3.0));
            binCount = (int)Math.Min(((maxValue - minValue) / binSize) + 1, MaxBinCount);

            if (binCount < MinBinCount)
            {
                // For small bincounts simply calculate binsize based on max-min values
                binCount = MinBinCount;
                binSize = (maxValue - minValue) / (binCount - 1);
            }


            histogramClasses = new List<float>();
            for (int idx = 0; idx <= binCount; idx++)
            {
                histogramClasses.Add(minValue + idx * binSize);
            }           
        }

        protected void WriteResidualLegend(IWorksheet sheet, int startRowIdx, int startColIdx)
        {
            sheet.SetInteriorColor(startRowIdx, startColIdx, Color.FromArgb(54, 96, 146));
            sheet.SetCellValue(startRowIdx, startColIdx + 1, "> 5.00 (model too wet)");
            sheet.SetInteriorColor(startRowIdx + 1, startColIdx, Color.FromArgb(65, 116, 177));
            sheet.SetCellValue(startRowIdx + 1, startColIdx + 1, "2.50 - 5.00");
            sheet.SetInteriorColor(startRowIdx + 2, startColIdx, Color.FromArgb(124, 161, 206));
            sheet.SetCellValue(startRowIdx + 2, startColIdx + 1, "1.00 - 2.50");
            sheet.SetInteriorColor(startRowIdx + 3, startColIdx, Color.FromArgb(157, 185, 219));
            sheet.SetCellValue(startRowIdx + 3, startColIdx + 1, "0.50 - 1.00");
            sheet.SetInteriorColor(startRowIdx + 4, startColIdx, Color.FromArgb(181, 206, 237));
            sheet.SetCellValue(startRowIdx + 4, startColIdx + 1, "0.20 - 0.50");
            sheet.SetInteriorColor(startRowIdx + 5, startColIdx, Color.FromArgb(216, 227, 240));
            sheet.SetCellValue(startRowIdx + 5, startColIdx + 1, "0.10 - 0.20");

            sheet.SetInteriorColor(startRowIdx + 6, startColIdx, Color.FromArgb(210, 222, 176));
            sheet.SetCellValue(startRowIdx + 6, startColIdx + 1, "-0.10 - 0.10");

            sheet.SetInteriorColor(startRowIdx + 7, startColIdx, Color.FromArgb(245, 228, 227));
            sheet.SetCellValue(startRowIdx + 7, startColIdx + 1, "-0.20 - -0.10");
            sheet.SetInteriorColor(startRowIdx + 8, startColIdx, Color.FromArgb(235, 200, 199));
            sheet.SetCellValue(startRowIdx + 8, startColIdx + 1, "-0.50 - -0.20");
            sheet.SetInteriorColor(startRowIdx + 9, startColIdx, Color.FromArgb(223, 165, 165));
            sheet.SetCellValue(startRowIdx + 9, startColIdx + 1, "-0.50 - -1.00");
            sheet.SetInteriorColor(startRowIdx + 10, startColIdx, Color.FromArgb(213, 137, 137));
            sheet.SetCellValue(startRowIdx + 10, startColIdx + 1, "-1.00 - -2.50");
            sheet.SetInteriorColor(startRowIdx + 11, startColIdx, Color.FromArgb(198, 94, 94));
            sheet.SetCellValue(startRowIdx + 11, startColIdx + 1, "-2.50 - -5.00");
            sheet.SetInteriorColor(startRowIdx + 12, startColIdx, Color.FromArgb(173, 61, 61));
            sheet.SetCellValue(startRowIdx + 12, startColIdx + 1, "< -5.00 (model too dry)");
            sheet.SetBorderColor(new Range(sheet, startRowIdx, startColIdx, startRowIdx + 12, startColIdx + 1), Color.Black, BorderWeight.Thin, true);
            sheet.SetColumnWidth(startColIdx + 1, 22);
        }

        protected void SetAbsResidualLegend(IWorksheet sheet, int rowIdx, int colIdx, double value)
        {
            if (value > 5.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(173, 61, 61));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value > 2.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(198, 94, 94));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value > 1.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(213, 137, 137));
            }
            else if (value > 0.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(223, 165, 165));
            }
            else if (value > 0.2)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(235, 200, 199));
            }
            else if (value > 0.1)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(245, 228, 227));
            }
            else
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(210, 222, 176));
            }
        }

        protected void SetResidualLegend(IWorksheet sheet, int rowIdx, int colIdx, double value)
        {
            if (value > 5.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(54, 96, 146));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value > 2.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(65, 116, 177));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value > 1.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(124, 161, 206));
            }
            else if (value > 0.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(157, 185, 219));
            }
            else if (value > 0.2)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(181, 206, 237));
            }
            else if (value > 0.1)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(216, 227, 240));
            }
            else if (value < -5.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(173, 61, 61));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value < -2.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(198, 94, 94));
                sheet.SetFontColor(rowIdx, colIdx, Color.White);
            }
            else if (value < -1.0)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(213, 137, 137));
            }
            else if (value < -0.5)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(223, 165, 165));
            }
            else if (value < -0.2)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(235, 200, 199));
            }
            else if (value < -0.1)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(245, 228, 227));
            }
            else
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(210, 222, 176));
            }
        }

        protected int GetLegendClassIdx(double value)
        {
            if (value > 1.0)
            {
                return 5;
            }
            else if (value > 0.5)
            {
                return 4;
            }
            else if (value > 0.2)
            {
                return 3;
            }
            else if (value > 0.1)
            {
                return 2;
            }
            else if (value < -1.0)
            {
                return 5;
            }
            else if (value < -0.5)
            {
                return 4;
            }
            else if (value < -0.2)
            {
                return 3;
            }
            else if (value < -0.1)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        protected static string[] ConvertObjectsToString(object[] objects)
        {
            string[] strings = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                strings[i] = (objects[i] != null) ? objects[i].ToString() : string.Empty;
            }
            return strings;
        }
    }
}
