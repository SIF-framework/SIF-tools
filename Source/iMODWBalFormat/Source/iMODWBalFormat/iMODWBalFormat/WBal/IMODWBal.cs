// iMODWBalFormat is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODWBalFormat.
// 
// iMODWBalFormat is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODWBalFormat is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODWBalFormat. If not, see <https://www.gnu.org/licenses/>.
using iMODWBalFormat.WBal;
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMODWBalFormat;

namespace iMODWBalFormat
{
    public class IMODWBal
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);
        protected const string CreationDatePrefix = "Waterbalance file created at ";
        protected const string SteadyStateDateString = "STEADY-STATE";
        protected const int SheetHeaderRowIdx = 3;
        protected const int SummaryHeaderRowIdx = 3;

        protected string sourceFilename;
        protected DateTime creationDate;
        protected GENFile genFile;
        protected string genFilename;
        protected List<string> columnNames;
        protected List<double> zoneAreas;
        protected List<LayerWBal> layerWBals = new List<LayerWBal>();

        public IMODWBal()
        {
            layerWBals = new List<LayerWBal>();
            zoneAreas = new List<double>(); 
        }

        public void Export(string excelFilename, SIFToolSettings settings, Log log = null, int logIndentLevel = 0)
        {
            ExcelManager excelManager = CreateExcelManager(settings);
            excelManager.SetScreenUpdating(false);
            IWorkbook workbook = excelManager.CreateWorkbook(false);

            bool isGroupedByZone = true;
            List<LayerWBalGroup> layerWBalGroups = CreateLayerWBalGroups(layerWBals, isGroupedByZone);
            CheckEmptyWaterbalanceGroups(layerWBalGroups, log);
            List<string> summaryPostNames = RetrieveSummaryPostNames(layerWBalGroups);

            if (log != null)
            {
                log.AddInfo("Creating waterbalances for " + layerWBalGroups.Count + " zones ...", logIndentLevel);
            }

            IWorksheet summarySheet = null;
            if (layerWBalGroups.Count > 0)
            {
                summarySheet = workbook.AddSheet("Summary");
                summarySheet.Zoom(85);
                summarySheet.FreezeRow(SummaryHeaderRowIdx);
                summarySheet.SetCellValue(0, 0, "Waterbalans " + Path.GetFileNameWithoutExtension(this.sourceFilename));
                summarySheet.SetFontBold(0, 0, true);
                summarySheet.SetCellValue(1, 0, "GEN-file:");
                if (genFilename != null)
                {
                    summarySheet.SetHyperlink(1, 1, genFilename, null, null, genFilename);
                }

                summarySheet.SetCellValue(SummaryHeaderRowIdx, 0, "Zone");
                summarySheet.SetFontBold(SummaryHeaderRowIdx, 0, true);
                int lastColIdx = 0;
                if ((genFile != null) && genFile.HasDATFile())
                {
                    for (int datFileColIdx = 0; datFileColIdx < genFile.DATFile.ColumnNames.Count; datFileColIdx++)
                    {
                        summarySheet.SetCellValue(SummaryHeaderRowIdx, lastColIdx + 1 + datFileColIdx, genFile.DATFile.ColumnNames[datFileColIdx]);
                    }
                    lastColIdx += genFile.DATFile.ColumnNames.Count;
                    summarySheet.SetFontBold(new Range(summarySheet, SummaryHeaderRowIdx, 0, SummaryHeaderRowIdx, lastColIdx), true);
                }
                summarySheet.SetCellValue(SummaryHeaderRowIdx, lastColIdx + 1, "WBAL LAYERS");
                summarySheet.SetFontBold(SummaryHeaderRowIdx, lastColIdx + 1, true);
                lastColIdx++;
                for (int postNameIdx = 0; postNameIdx < summaryPostNames.Count; postNameIdx++)
                {
                    summarySheet.SetCellValue(SummaryHeaderRowIdx, lastColIdx + 1 + 2 * postNameIdx + 0, summaryPostNames[postNameIdx] + " IN (%)");
                    summarySheet.SetCellValue(SummaryHeaderRowIdx, lastColIdx + 1 + 2 * postNameIdx + 1, summaryPostNames[postNameIdx] + " OUT (%)");
                }
                summarySheet.SetFontBold(new Range(summarySheet, SummaryHeaderRowIdx, lastColIdx + 1, SummaryHeaderRowIdx, lastColIdx + 1 + 2 * summaryPostNames.Count + 1), true);
                lastColIdx += summaryPostNames.Count;
            }

            for (int layerWBalGroupIdx = 0; layerWBalGroupIdx < layerWBalGroups.Count; layerWBalGroupIdx++)
            {
                LayerWBalGroup layerWBalsGroup = layerWBalGroups[layerWBalGroupIdx];
                ExportLayerWBalGroup(layerWBalsGroup, layerWBalsGroup.Name, workbook, summarySheet, settings.SumStartLayer, settings.SumEndLayer, summaryPostNames, settings.IDColIdx);
            }

            workbook.Sheets[0].Activate();

            workbook.Save(excelFilename);
            workbook.Close();
            excelManager.Cleanup();
        }

        protected virtual ExcelManager CreateExcelManager(SIFToolSettings settings)
        {
            return ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
        }

        private void CheckEmptyWaterbalanceGroups(List<LayerWBalGroup> layerWBalGroups, Log log, int logIndentLevel = 0)
        {
            bool isFirstWarning = true;

            // Check and correct for empty waterbalances
            for (int groupIdx = 0; groupIdx < layerWBalGroups.Count(); groupIdx++)
            {
                LayerWBalGroup currentGroup = layerWBalGroups[groupIdx];
                if ((Math.Round(currentGroup.TotalIn, 3).Equals(0d)) && (Math.Round(currentGroup.TotalOut, 3).Equals(0d)))
                {
                    // Empty waterbalance. May have been caused by overwrite of a zone with equal polygon 
                    HandleEmptyBalanceGroup(layerWBalGroups, currentGroup, isFirstWarning, log, logIndentLevel);
                    isFirstWarning = false;
                }
            }

            if (!isFirstWarning)
            {
                System.Console.WriteLine();
            }
        }

        protected virtual void HandleEmptyBalanceGroup(List<LayerWBalGroup> layerWBalGroups, LayerWBalGroup currentGroup, bool isFirstWarning, Log log, int logIndentLevel)
        {
            if (isFirstWarning)
            {
                System.Console.WriteLine();
                isFirstWarning = false;
            }

            log.AddError("Zone " + currentGroup.Zone + " has 0-balance, zone is ignored", logIndentLevel);
        }

        public void ReadWaterbalanceFile(string csvFilename, Log log, string genFilename = null)
        {
            if (!File.Exists(csvFilename))
            {
                throw new Exception("Specified CSV-file doesn't exist: " + csvFilename);
            }

            StreamReader sr = null;
            try
            {
                sr = new StreamReader(csvFilename);
                sourceFilename = csvFilename;

                string wholeLine = sr.ReadLine().Trim();
                int lineNumber = 1;

                if (wholeLine.StartsWith(CreationDatePrefix))
                {
                    string creationDateString = wholeLine.Substring(CreationDatePrefix.Length, wholeLine.Length - CreationDatePrefix.Length);
                    if (!DateTime.TryParse(creationDateString, englishCultureInfo, DateTimeStyles.None, out creationDate))
                    {
                        creationDate = File.GetCreationTime(csvFilename);
                    }
                }
                else
                {
                    log.AddWarning("Unexpected content: could not parse creation date, missing prefix '" + CreationDatePrefix + "' in first line: " + wholeLine);
                }

                // Skip empty lines(s)
                wholeLine = sr.ReadLine().Trim();
                lineNumber++;
                while (!sr.EndOfStream && wholeLine.Equals(string.Empty))
                {
                    wholeLine = sr.ReadLine().Trim();
                    lineNumber++;
                }
                if (sr.EndOfStream)
                {
                    throw new ToolException("Error: unexpected end of line at line " + lineNumber);
                }

                if (wholeLine.ToLower().StartsWith("waterbalance for selected area given by polygon"))
                {
                    // Polygone message found, read line with polygon GEN-filename
                    wholeLine = sr.ReadLine().Trim();
                    lineNumber++;
                    if (File.Exists(wholeLine))
                    {
                        genFilename = wholeLine.Trim();
                    }
                    else if (genFilename == null)
                    {
                        throw new ToolException("GEN-filename is missing in CSV-file, please use option g to specify the source GEN-file");
                    }

                    if (genFilename != null)
                    {
                        // If a genfilename was specified as a tooloption, use that genfilename
                        genFilename = genFilename;
                    }

                    // Skip empty line(s)
                    wholeLine = sr.ReadLine();
                    lineNumber++;
                    while (!sr.EndOfStream && wholeLine.Trim().Equals(string.Empty))
                    {
                        wholeLine = sr.ReadLine();
                        lineNumber++;
                    }
                    if (sr.EndOfStream)
                    {
                        throw new ToolException("Error: unexpected end of line at line " + lineNumber);
                    }
                }

                if (wholeLine.ToLower().StartsWith("bear in mind that disclosure of the waterbalance might be caused by absent budget"))
                {
                    // Warning message found, skip two lines with warning
                    wholeLine = sr.ReadLine();
                    lineNumber++;
                    wholeLine = sr.ReadLine();
                    lineNumber++;

                    // Skip empty line(s)
                    while (!sr.EndOfStream && wholeLine.Trim().Equals(string.Empty))
                    {
                        wholeLine = sr.ReadLine();
                        lineNumber++;
                    }
                    if (sr.EndOfStream)
                    {
                        throw new ToolException("Error: unexpected end of line at line " + lineNumber);
                    }
                }

                // Read polygonfile
                if ((genFilename != null) && Path.GetExtension(genFilename).ToLower().Equals(".gen"))
                {
                    if (!Path.IsPathRooted(genFilename))
                    {
                        genFilename = Path.Combine(Path.GetDirectoryName(csvFilename), genFilename);
                    }
                    if (File.Exists(genFilename))
                    {

                        genFile = GENFile.ReadFile(genFilename);

                        foreach (GENFeature feature in genFile.Features)
                        {
                            if (feature is GENPolygon)
                            {
                                double area = feature.CalculateMeasure();
                                if (area < 0)
                                {
                                    area = Math.Abs(area);
                                }
                                zoneAreas.Add(area);
                            }
                            else
                            {
                                throw new ToolException("GEN feature " + feature.ID + " is not a polygon");
                            }
                        }
                    }
                    else
                    {
                        log.AddWarning("Area calculation is skipped. Specified GEN-file in CSV-file cannot be found: " + genFilename);
                    }
                }
                else
                {
                    log.AddWarning("GEN-polygon missing in CSV-file, area calculation from GEN-file is skipped.");
                }

                //// The current line could be the waterbalance headerline, check if it starts with the date columnname
                //if (!wholeLine.ToLower().StartsWith("date"))
                //{
                //    // Skip empty line(s)
                //    wholeLine = sr.ReadLine();
                //    lineNumber++;
                //    while (!sr.EndOfStream && wholeLine.Trim().Equals(string.Empty))
                //    {
                //        wholeLine = sr.ReadLine();
                //        lineNumber++;
                //    }

                //}
                //else
                //{
                //    // This is a new iMOD 4.2 format which cannot be parsed currently
                //    throw new ToolException("The format of this waterbalance CSV-file cannot currently be parsed, use iMOD version 4.0");
                //}

                // Parse column names
                columnNames = new List<string>(wholeLine.Trim().Split(new char[] { ',', ';' }));
                if (columnNames.Count < 3)
                {
                    throw new Exception("Not enough columnnames found in line " + lineNumber + " (at least expected date, layer, zone): " + wholeLine);
                }

                if (!columnNames[0].ToLower().StartsWith("date"))
                {
                    throw new ToolException("Missing Date columnname in first column: " + columnNames[0]);
                }
                if (!columnNames[1].ToLower().Equals("layer"))
                {
                    throw new ToolException("Missing Layer columnname in second column: " + columnNames[1]);
                }
                if (!columnNames[2].ToLower().Equals("zone"))
                {
                    throw new ToolException("Missing Zone columnname in third column: " + columnNames[2]);
                }

                if (sr.EndOfStream)
                {
                    throw new Exception("Unexpected end of line when reading CSV-file at line " + lineNumber);
                }

                //// Read data depending on format of CSV-file
                //if (!columnNames[0].ToLower().Equals("date_time"))
                //{
                //    // iMOD 4.2 or higher
                //    throw new ToolException("Missing Date columnname in first column: " + columnNames[0]);
                //}
                //else
                //{
                //}

                // Skip line with units
                wholeLine = sr.ReadLine();
                lineNumber++;

                //if (sr.EndOfStream)
                //{
                //    System.Console.WriteLine("Error: unexpected end of file at line " + lineNumber);
                //    return null;
                //}

                // Now start parsing waterbalance rows
                while (!sr.EndOfStream && !wholeLine.StartsWith("---")) // Note: when a line is found starting with '---' the waterbalance part is finished and the 'ARRAY OF WATERBALANCE AREA' is started, which is not used here
                {
                    wholeLine = sr.ReadLine().Trim();
                    wholeLine = wholeLine.Replace("0.0D0", "0.0");
                    lineNumber++;

                    if (!wholeLine.StartsWith("---"))
                    {
                        string[] rowValues = wholeLine.Trim().Split(new char[] { ',', ';' });
                        if (rowValues.Length < 3)
                        {
                            throw new Exception("Not enough values found in line " + lineNumber + " (at least expected date, layer, zone): " + wholeLine);
                        }
                        if (rowValues.Length != columnNames.Count)
                        {
                            throw new Exception("Number of row values (" + rowValues.Length + ") in line " + lineNumber + " doesn't match number of column names (" + columnNames.Count + ")");
                        }

                        DateTime? postDateTime = null;
                        if (!rowValues[0].Equals(SteadyStateDateString))
                        {
                            if (!DateTime.TryParse(rowValues[0], englishCultureInfo, DateTimeStyles.None, out DateTime dateTime))
                            {
                                throw new Exception("Unexpected date-value at line " + lineNumber + ": " + rowValues[0]);
                            }
                            postDateTime = dateTime;
                        }
                        if (!int.TryParse(rowValues[1], out int layer))
                        {
                            throw new Exception("Could not parse layer value in line " + lineNumber);
                        }

                        if (!int.TryParse(rowValues[2], out int zone))
                        {
                            throw new Exception("Could not parse layer value in line " + lineNumber);
                        }

                        int colIdx = 3;
                        if (columnNames[colIdx].ToLower().Equals("area"))
                        {
                            // read area (in km2)
                            if (!double.TryParse(rowValues[colIdx], NumberStyles.Float, englishCultureInfo, out double area))
                            {
                                throw new Exception("Could not parse area value in line " + lineNumber);
                            }
                            area = area * 1000 * 1000;
                            if (zone > zoneAreas.Count)
                            {
                                zoneAreas.Add(area);
                            }
                            //zoneAreas[zone - 1] = area;

                            colIdx++;
                        }

                        LayerWBal layerWBal = new LayerWBal(postDateTime, layer, zone);
                        if ((zone - 1) < zoneAreas.Count)
                        {
                            layerWBal.ZoneArea = zoneAreas[zone - 1];
                        }
                        while ((colIdx + 1) < rowValues.Length)
                        {
                            if (!double.TryParse(rowValues[colIdx].Replace(",", "."), NumberStyles.Any, englishCultureInfo, out double inValue))
                            {
                                throw new Exception("Could not parse float value in line " + lineNumber + ", column " + (colIdx + 1) + ": " + rowValues[colIdx]);
                            }
                            if (!double.TryParse(rowValues[colIdx + 1].Replace(",", "."), NumberStyles.Any, englishCultureInfo, out double outValue))
                            {
                                throw new Exception("Could not parse float value in line " + lineNumber + ", column " + (colIdx + 2) + ": " + rowValues[colIdx + 1]);
                            }
                            string columnName = columnNames[colIdx].Replace("\"", "").Trim();
                            string postName = columnName.Substring(0, columnName.Length - 3);
                            WBalPost post = new WBalPost(postName, (float)inValue, (float) outValue);
                            layerWBal.AddPost(post);
                            colIdx += 2;
                        }
                        AddLayerWBal(layerWBal);
                    }
                }
            }
            catch (ToolException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not process CSV-file: " + Path.GetFileName(csvFilename), ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
        }

        protected void AddLayerWBal(LayerWBal layerWBal)
        {
            layerWBals.Add(layerWBal);
        }

        private List<string> RetrieveSummaryPostNames(List<LayerWBalGroup> layerWBalGroups)
        {
            List<string> summaryPostnames = new List<string>();
            foreach (LayerWBalGroup layerWBalGroup in layerWBalGroups)
            {
                foreach (LayerWBal layerWBal in layerWBalGroup.LayerWBals)
                {
                    layerWBal.Posts.Sort(); 
                    foreach (WBalPost post in layerWBal.Posts)
                    {
                        string postname = post.Name;
                        string abbreviatedName = GetSummaryPostName(postname);
                        if (!summaryPostnames.Contains(abbreviatedName))
                        {
                            summaryPostnames.Add(abbreviatedName); 
                        }
                    }
                }
            }
            return summaryPostnames;
        }

        private string GetSummaryPostName(string postname)
        {
            int idx = postname.IndexOf("_SYS");
            if (idx > 0)
            {
                return postname.Substring(0, idx);
            }
            else
            {
                return postname;
            }
        }

        private void ExportLayerWBalGroup(LayerWBalGroup layerWBalsGroup, string groupname, IWorkbook workbook, IWorksheet summarySheet, int sumStartLayer = 1, int sumEndLayer = 999, List<string> summaryPostNames = null, int idColIdx = -1)
        {
            int summarySheetLastRowIdx = -1;
            int summarySheetLastColIdx = -1;

            if (sumEndLayer >= layerWBalsGroup.LayerWBals[layerWBalsGroup.LayerWBals.Count - 1].Layer)
            {
                sumEndLayer = layerWBalsGroup.LayerWBals[layerWBalsGroup.LayerWBals.Count - 1].Layer;
            }

            string id = null;
            if (summarySheet != null)
            {
                summarySheetLastRowIdx = summarySheet.End(new Cell(summarySheet, short.MaxValue, 0), CellDirection.Up).RowIdx;
                summarySheet.SetCellValue(summarySheetLastRowIdx + 1, 0, layerWBalsGroup.Zone);
                summarySheetLastColIdx = 0;

                if ((genFile != null) && genFile.HasDATFile())
                {
                    summarySheetLastColIdx += genFile.DATFile.ColumnNames.Count;
                    GENFeature feature = genFile.Features[layerWBalsGroup.Zone - 1];
                    if (feature != null)
                    {
                        DATRow datRow = genFile.DATFile.GetRow(feature.ID);
                        if (datRow != null)
                        {
                            for (int datFileColIdx = 0; datFileColIdx < datRow.Count; datFileColIdx++)
                            {
                                summarySheet.SetCellValue(summarySheetLastRowIdx + 1, 1 + datFileColIdx, datRow[datFileColIdx], englishCultureInfo);
                            }
                            if (idColIdx > 0)
                            {
                                if (idColIdx < genFile.DATFile.ColumnNames.Count)
                                {
                                    id = datRow[idColIdx];
                                }
                                else
                                {
                                    throw new ToolException("ID-columnindex (" + idColIdx + ") not valid for DAT-file: " + genFile.DATFile.Filename);
                                }
                            }
                        }
                    }
                }
                summarySheet.SetCellValue(summarySheetLastRowIdx + 1, summarySheetLastColIdx + 1, (object) (sumStartLayer + "-" + sumEndLayer));
                summarySheetLastColIdx++;
                summarySheetLastRowIdx++;
            }

            string basename = groupname;
            if (id != null)
            {
                basename = id;
            }
            if (basename.Length > 27)
            {
                // Correct for max sheetname length of 31 characters
                basename = basename.Substring(0, 27);
            }

            int idx = 1;
            string sheetname1 = SpreadsheetUtils.CorrectSheetname(basename + " abs");
            string sheetname2 = SpreadsheetUtils.CorrectSheetname(basename + " pct");
            while (workbook.GetSheet(sheetname1) != null)
            {
                if (basename.Length > 24)
                {
                    // Correct for max sheetname length of 31 characters
                    basename = basename.Substring(0, 24);
                }

                idx++;
                sheetname1 = SpreadsheetUtils.CorrectSheetname(basename + " #" + idx + " abs");
                sheetname2 = SpreadsheetUtils.CorrectSheetname(basename + " #" + idx + " pct");
            }
            IWorksheet sheet1 = workbook.AddSheet(sheetname1);
            IWorksheet sheet2 = workbook.AddSheet(sheetname2);

            // Add absolute water balance statistics
            sheet1.SetCellValue(SheetHeaderRowIdx, 0, "Zone");
            sheet1.SetCellValue(SheetHeaderRowIdx, 1, "Layer");
            sheet1.SetCellValue(SheetHeaderRowIdx, 2, "Post");
            sheet1.SetCellValue(SheetHeaderRowIdx, 3, "Type");
            sheet1.SetCellValue(SheetHeaderRowIdx, 4, "IN (m3)");
            sheet1.SetCellValue(SheetHeaderRowIdx, 5, "OUT (m3)");
            sheet1.SetCellValue(SheetHeaderRowIdx, 6, "SUM (m3)");
            if (zoneAreas.Count > 0)
            {
                sheet1.SetCellValue(SheetHeaderRowIdx, 8, "AREA (m2)");
                sheet1.SetCellValue(SheetHeaderRowIdx, 9, "IN (mm)");
                sheet1.SetCellValue(SheetHeaderRowIdx, 10, "OUT (mm)");
                sheet1.SetCellValue(SheetHeaderRowIdx, 11, "SUM (mm)");
            }
            sheet1.SetFontBold(new Range(sheet1, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 11), true);
            sheet1.SetBorderColor(new Range(sheet1, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 11), Color.Black, BorderWeight.Thick, true);

            int rowIdx = SheetHeaderRowIdx + 1;
            int zoneFirstRowIdx = rowIdx;
            int currentZone = (layerWBalsGroup.LayerWBals.Count > 0) ? layerWBalsGroup.LayerWBals[0].Zone : 0;
            Dictionary<int, int> zoneExternM3SumListIndices = new Dictionary<int, int>();
            List<double> zoneExternM3SumList = new List<double>();
            zoneExternM3SumList.Add(0);
            zoneExternM3SumListIndices.Add(currentZone, 0);
            int currentZoneExternM3SumListIdx = 0;
            for (int layerIdx = 0; layerIdx < layerWBalsGroup.LayerWBals.Count; layerIdx++)
            {
                LayerWBal layerWBal = layerWBalsGroup.LayerWBals[layerIdx];
                int zone = layerWBal.Zone;

                if (zone != currentZone)
                {
                    AddZoneTotal(sheet1, zoneFirstRowIdx, rowIdx, 9, 10, 11, currentZone);
                    zoneExternM3SumList.Add(0);
                    if (!zoneExternM3SumListIndices.ContainsKey(zone))
                    {
                        zoneExternM3SumListIndices.Add(zone, zoneExternM3SumList.Count - 1);
                    }
                    currentZoneExternM3SumListIdx = zoneExternM3SumListIndices[zone];
                    currentZone = zone;
                    rowIdx++;
                    zoneFirstRowIdx = rowIdx;
                }

                int layerFirstRowIdx = rowIdx;
                layerWBal.Posts.Sort();
                for (int postIdx = 0; postIdx < layerWBal.Posts.Count; postIdx++)
                {
                    WBalPost post = layerWBal.Posts[postIdx];
                    sheet1.SetCellValue(rowIdx, 0, zone);
                    sheet1.SetCellValue(rowIdx, 1, layerWBal.Layer);
                    sheet1.SetCellValue(rowIdx, 2, post.Name);
                    sheet1.SetCellValue(rowIdx, 3, post.GetTypeString());
                    sheet1.SetCellValue(rowIdx, 4, post.In);
                    sheet1.SetCellValue(rowIdx, 5, post.Out);
                    sheet1.SetCellValue(rowIdx, 6, post.Sum);
                    sheet1.SetNumberFormat(new Range(sheet1, rowIdx, 0, rowIdx, 1), "0");
                    sheet1.SetNumberFormat(new Range(sheet1, rowIdx, 4, rowIdx, 6), "0.0");

                    if (post.GetTypeString().Equals(WBalPost.ExternTypeString))
                    {
                        zoneExternM3SumList[currentZoneExternM3SumListIdx] += post.In;
                    }

                    if ((zone - 1) < zoneAreas.Count)
                    {
                        double zoneArea = layerWBal.ZoneArea;
                        sheet1.SetCellValue(rowIdx, 8, zoneArea);
                        sheet1.SetNumberFormat(new Range(sheet1, rowIdx, 8, rowIdx, 8), "0");
                        double mmIn = 1000 * (post.In / zoneArea);
                        double mmOut = 1000 * (post.Out / zoneArea);
                        double mmSum = 1000 * (post.Sum / zoneArea);
                        sheet1.SetCellValue(rowIdx, 9, mmIn);
                        sheet1.SetCellValue(rowIdx, 10, mmOut);
                        sheet1.SetCellValue(rowIdx, 11, mmSum);
                        SetFluxLegend(sheet1, rowIdx, 9, mmIn);
                        SetFluxLegend(sheet1, rowIdx, 10, mmOut);
                        SetFluxLegend(sheet1, rowIdx, 11, mmSum);
                        sheet1.SetNumberFormat(new Range(sheet1, rowIdx, 9, rowIdx, 11), "0.00");
                    }
                    rowIdx++;
                }
                sheet1.SetBorderColor(new Range(sheet1, layerFirstRowIdx, 0, rowIdx - 1, 11), Color.Black, BorderWeight.Thin, true);
                sheet1.SetBorderColor(new Range(sheet1, layerFirstRowIdx, 0, rowIdx - 1, 11), Color.Black, BorderWeight.Thick, false);
            }
            AddZoneTotal(sheet1, zoneFirstRowIdx, rowIdx, 9, 10, 11, currentZone);

            sheet1.SetAutoFilter(new Range(sheet1, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 11));
            sheet1.FreezeRow(SheetHeaderRowIdx);
            sheet1.Zoom(85);
            sheet1.AutoFitColumn(2);

            // Add absolute summarystatistics of specified layers
            int lastStatsRowIdx = rowIdx - 1;
            int firstSummaryStatsRowIdx = lastStatsRowIdx + 4;
            int summaryStatsRowIdx = firstSummaryStatsRowIdx + 1;
            sheet1.SetCellValue(firstSummaryStatsRowIdx - 1, 0, "Summary of absolute water balance post of layers " + sumStartLayer + " to " + sumEndLayer);
            sheet1.SetFontItalic(firstSummaryStatsRowIdx - 1, 0, true);
            sheet1.CopyRange(new Range(sheet1, 3, 0, 3, 11), new Cell(sheet1, firstSummaryStatsRowIdx, 0));
            Dictionary<string, WBalPost> summaryPosts = new Dictionary<string, WBalPost>();
            double summaryZoneExternM3InSum = 0;
            double summaryZoneExternM3OutSum = 0;
            for (int layerIdx = 0; layerIdx < layerWBalsGroup.LayerWBals.Count; layerIdx++)
            {
                LayerWBal layerWBal = layerWBalsGroup.LayerWBals[layerIdx];
                if ((layerWBal.Layer >= sumStartLayer) && (layerWBal.Layer <= sumEndLayer))
                {
                    foreach (WBalPost post in layerWBal.Posts)
                    {
                        // Sum m3 values of external post over all specified layers, for later use in percentage water balance
                        if ((post.GetTypeString().Equals(WBalPost.ExternTypeString))
                            || (post.GetFluxPosition().Equals(WBalPost.FluxPosition.Upper) && (layerWBal.Layer == sumStartLayer))
                            || (post.GetFluxPosition().Equals(WBalPost.FluxPosition.Lower) && (layerWBal.Layer == sumEndLayer)))
                        {
                            if (!summaryPosts.ContainsKey(post.Name))
                            {
                                summaryPosts.Add(post.Name, new WBalPost(post.Name, 0, 0));
                            }
                            WBalPost summaryPost = summaryPosts[post.Name];
                            summaryPost.In += post.In;
                            summaryPost.Out += post.Out;
                            summaryPost.Sum = summaryPost.In + summaryPost.Out;

                            summaryZoneExternM3InSum += post.In;
                            summaryZoneExternM3OutSum += Math.Abs(post.Out);
                        }
                    }
                }
            }
            foreach (string postName in summaryPosts.Keys)
            {
                WBalPost summaryPost = summaryPosts[postName];
                double mmIn = 1000 * (summaryPost.In / layerWBalsGroup.ZoneArea);
                double mmOut = 1000 * (summaryPost.Out / layerWBalsGroup.ZoneArea);
                double mmSum = 1000 * (summaryPost.Sum / layerWBalsGroup.ZoneArea);
                sheet1.SetCellValue(summaryStatsRowIdx, 0, layerWBalsGroup.Zone);
                // Sum m3 values of external post over all specified layers, for later use in percentage water balance
                if (summaryPost.GetTypeString().Equals(WBalPost.ExternTypeString))
                {
                    sheet1.SetCellValue(summaryStatsRowIdx, 1, (object) (sumStartLayer + "-" + sumEndLayer));
                }
                else if (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Upper))
                {
                    sheet1.SetCellValue(summaryStatsRowIdx, 1, sumStartLayer);
                    sheet1.SetHorizontalAlignmentLeft(summaryStatsRowIdx, 1);
                }
                else if (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Lower))
                {
                    sheet1.SetCellValue(summaryStatsRowIdx, 1, sumEndLayer);
                    sheet1.SetHorizontalAlignmentLeft(summaryStatsRowIdx, 1);
                }
                else
                {
                    // ignore, should not happen, leave empty
                }
                sheet1.SetCellValue(summaryStatsRowIdx, 2, summaryPost.Name);
                sheet1.SetCellValue(summaryStatsRowIdx, 3, summaryPost.GetTypeString());
                sheet1.SetCellValue(summaryStatsRowIdx, 4, summaryPost.In);
                sheet1.SetCellValue(summaryStatsRowIdx, 5, summaryPost.Out);
                sheet1.SetCellValue(summaryStatsRowIdx, 6, summaryPost.Sum);
                sheet1.SetCellValue(summaryStatsRowIdx, 8, layerWBalsGroup.ZoneArea);
                sheet1.SetCellValue(summaryStatsRowIdx, 9, mmIn);
                sheet1.SetCellValue(summaryStatsRowIdx, 10, mmOut);
                sheet1.SetCellValue(summaryStatsRowIdx, 11, mmSum);
                SetFluxLegend(sheet1, summaryStatsRowIdx, 9, mmIn);
                SetFluxLegend(sheet1, summaryStatsRowIdx, 10, mmOut);
                SetFluxLegend(sheet1, summaryStatsRowIdx, 11, mmSum);
                sheet1.SetNumberFormat(new Range(sheet1, summaryStatsRowIdx, 0, summaryStatsRowIdx, 1), "0");
                sheet1.SetNumberFormat(new Range(sheet1, summaryStatsRowIdx, 4, summaryStatsRowIdx, 6), "0.0");
                sheet1.SetNumberFormat(new Range(sheet1, summaryStatsRowIdx, 8, summaryStatsRowIdx, 8), "0");
                sheet1.SetNumberFormat(new Range(sheet1, summaryStatsRowIdx, 9, summaryStatsRowIdx, 11), "0.00");
                summaryStatsRowIdx++;
            }
            sheet1.SetBorderColor(new Range(sheet1, firstSummaryStatsRowIdx, 0, summaryStatsRowIdx - 1, 11), Color.Black, BorderWeight.Thin, true);
            sheet1.SetBorderColor(new Range(sheet1, firstSummaryStatsRowIdx, 0, summaryStatsRowIdx - 1, 11), Color.Black, BorderWeight.Thick, false);
            AddZoneTotal(sheet1, firstSummaryStatsRowIdx, summaryStatsRowIdx, 9, 10, 11, currentZone);
            sheet1.AutoFitColumns(new Range(sheet1, firstSummaryStatsRowIdx, 0, summaryStatsRowIdx - 1, 11));

            // Now set Title, after autofit of columns in sheet with absolute statistics
            sheet1.SetCellValue(0, 0, "Waterbalans " + Path.GetFileNameWithoutExtension(this.sourceFilename));
            sheet1.SetFontBold(0, 0, true);
            sheet1.SetCellValue(1, 0, "GEN-file:");
            if (genFilename != null)
            {
                sheet1.SetHyperlink(1, 1, genFilename, null, null, genFilename);
            }
            try
            {
                sheet1.Select(firstSummaryStatsRowIdx, 0);
            }
            catch (Exception)
            {
                // ignore
            }

            // Add percentage statistics to second sheet
            sheet2.SetCellValue(SheetHeaderRowIdx, 0, "Zone");
            sheet2.SetCellValue(SheetHeaderRowIdx, 1, "Layer");
            sheet2.SetCellValue(SheetHeaderRowIdx, 2, "Post");
            sheet2.SetCellValue(SheetHeaderRowIdx, 3, "IN (%)");
            sheet2.SetCellValue(SheetHeaderRowIdx, 4, "OUT (%)");
            sheet2.SetCellValue(SheetHeaderRowIdx, 5, "SUM (%)");
            sheet2.SetFontBold(new Range(sheet2, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 5), true);
            sheet2.SetBorderColor(new Range(sheet2, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 5), Color.Black, BorderWeight.Thick, true);

            rowIdx = SheetHeaderRowIdx + 1;
            zoneFirstRowIdx = rowIdx;
            currentZone = (layerWBalsGroup.LayerWBals.Count > 0) ? layerWBalsGroup.LayerWBals[0].Zone : 0; ;
            double zoneExternMmSum;
            for (int layerIdx = 0; layerIdx < layerWBalsGroup.LayerWBals.Count; layerIdx++)
            {
                LayerWBal layerWBal = layerWBalsGroup.LayerWBals[layerIdx];
                int zone = layerWBal.Zone;

                if (zone != currentZone)
                {
                    AddZoneTotal(sheet2, zoneFirstRowIdx, rowIdx, 3, 4, 5, currentZone);
                    zoneExternMmSum = 1000 * zoneExternM3SumList[currentZoneExternM3SumListIdx] / zoneAreas[currentZone - 1];
                    sheet2.SetCellValue(rowIdx, 2, "(" + zoneExternMmSum.ToString("F2") + " mm/d)");
                    currentZone = zone;
                    rowIdx++;
                    zoneFirstRowIdx = rowIdx;
                }

                int layerFirstRowIdx = rowIdx;
                layerWBal.Posts.Sort();
                for (int postIdx = 0; postIdx < layerWBal.Posts.Count; postIdx++)
                {
                    WBalPost post = layerWBal.Posts[postIdx];
                    if (post.GetTypeString().Equals(WBalPost.ExternTypeString))
                    {
                        sheet2.SetCellValue(rowIdx, 0, zone);
                        sheet2.SetCellValue(rowIdx, 1, layerWBal.Layer);
                        sheet2.SetCellValue(rowIdx, 2, post.Name);
                        if (zoneExternM3SumList[currentZoneExternM3SumListIdx] > 0)
                        {
                            sheet2.SetCellFormula(rowIdx, 3, "=" + post.In.ToString(englishCultureInfo) + "/" + zoneExternM3SumList[currentZoneExternM3SumListIdx].ToString(englishCultureInfo));
                            sheet2.SetCellFormula(rowIdx, 4, "=" + post.Out.ToString(englishCultureInfo) + "/" + zoneExternM3SumList[currentZoneExternM3SumListIdx].ToString(englishCultureInfo));
                            sheet2.SetCellFormula(rowIdx, 5, "=" + post.Sum.ToString(englishCultureInfo) + "/" + zoneExternM3SumList[currentZoneExternM3SumListIdx].ToString(englishCultureInfo));
                        }
                        SetFluxLegend(sheet2, rowIdx, 3, post.In / zoneExternM3SumList[currentZoneExternM3SumListIdx]);
                        SetFluxLegend(sheet2, rowIdx, 4, post.Out / zoneExternM3SumList[currentZoneExternM3SumListIdx]);
                        SetFluxLegend(sheet2, rowIdx, 5, post.Sum / zoneExternM3SumList[currentZoneExternM3SumListIdx]);
                        sheet2.SetNumberFormat(new Range(sheet2, rowIdx, 0, rowIdx, 1), "0");
                        sheet2.SetNumberFormat(new Range(sheet2, rowIdx, 3, rowIdx, 5), "0.0%");
                        rowIdx++;
                    }
                }
                if (rowIdx > layerFirstRowIdx)
                {
                    sheet2.SetBorderColor(new Range(sheet2, layerFirstRowIdx, 0, rowIdx - 1, 5), Color.Black, BorderWeight.Thin, true);
                    sheet2.SetBorderColor(new Range(sheet2, layerFirstRowIdx, 0, rowIdx - 1, 5), Color.Black, BorderWeight.Thick, false);
                }
            }
            AddZoneTotal(sheet2, zoneFirstRowIdx, rowIdx, 3, 4, 5, currentZone);
            sheet2.SetNumberFormat(new Range(sheet2, rowIdx, 3, rowIdx, 5), "0.0%");
            if (zoneAreas.Count > 0)
            {
                zoneExternMmSum = 1000 * zoneExternM3SumList[currentZoneExternM3SumListIdx] / (float)zoneAreas[currentZoneExternM3SumListIdx];
            }
            else
            {
                zoneExternMmSum = float.NaN;
            }
            sheet2.SetCellValue(rowIdx, 2, "(" + zoneExternMmSum.ToString("F2") + " mm/d)");

            // Add percentage summary statistics of specified layers in second sheet
            lastStatsRowIdx = rowIdx - 1;
            firstSummaryStatsRowIdx = lastStatsRowIdx + 4;
            summaryStatsRowIdx = firstSummaryStatsRowIdx + 1;
            sheet2.SetCellValue(firstSummaryStatsRowIdx - 1, 0, "Summary water balance in percentages of total of external posts over layers " + sumStartLayer + " to " + sumEndLayer);
            sheet2.SetFontItalic(firstSummaryStatsRowIdx - 1, 0, true);
            sheet2.CopyRange(new Range(sheet1, 3, 0, 3, 5), new Cell(sheet2, firstSummaryStatsRowIdx, 0));
            Dictionary<string, double> summedPostInValues = new Dictionary<string, double>();
            Dictionary<string, double> summedPostOutValues = new Dictionary<string, double>();
            foreach (string postName in summaryPosts.Keys)
            {
                WBalPost summaryPost = summaryPosts[postName];

                if ((summaryPost.GetTypeString().Equals(WBalPost.ExternTypeString))
                    || (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Upper)) 
                    || (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Lower)))
                {
                    sheet2.SetCellValue(summaryStatsRowIdx, 0, layerWBalsGroup.Zone);
                    if (summaryPost.GetTypeString().Equals(WBalPost.ExternTypeString))
                    {
                        sheet2.SetCellValue(summaryStatsRowIdx, 1, (object) (sumStartLayer + "-" + sumEndLayer));
                    }
                    else if (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Upper))
                    {
                        sheet2.SetCellValue(summaryStatsRowIdx, 1, sumStartLayer);
                        sheet2.SetHorizontalAlignmentLeft(summaryStatsRowIdx, 1);
                    }
                    else if (summaryPost.GetFluxPosition().Equals(WBalPost.FluxPosition.Lower))
                    {
                        sheet2.SetCellValue(summaryStatsRowIdx, 1, sumEndLayer);
                        sheet2.SetHorizontalAlignmentLeft(summaryStatsRowIdx, 1);
                    }
                    else
                    {
                        // ignore, should not happen, leave empty
                    }
                    sheet2.SetCellValue(summaryStatsRowIdx, 2, summaryPost.Name);
                    double pctIn = 0;
                    double pctOut = 0;
                    double pctSum = 0;
                    if (summaryZoneExternM3InSum > 0)
                    {
                        sheet2.SetCellFormula(summaryStatsRowIdx, 3, "=" + summaryPost.In.ToString(englishCultureInfo) + "/" + summaryZoneExternM3InSum.ToString(englishCultureInfo));
                        sheet2.SetCellFormula(summaryStatsRowIdx, 4, "=" + summaryPost.Out.ToString(englishCultureInfo) + "/" + summaryZoneExternM3OutSum.ToString(englishCultureInfo));
                        sheet2.SetCellFormula(summaryStatsRowIdx, 5, "=" + summaryPost.Sum.ToString(englishCultureInfo) + "/" + summaryZoneExternM3InSum.ToString(englishCultureInfo));
                        pctIn = summaryPost.In / summaryZoneExternM3InSum;
                        pctOut = summaryPost.Out / summaryZoneExternM3OutSum;
                        pctSum = summaryPost.Sum / summaryZoneExternM3InSum;
                        SetFluxLegend(sheet2, summaryStatsRowIdx, 3, pctIn);
                        SetFluxLegend(sheet2, summaryStatsRowIdx, 4, pctOut);
                        SetFluxLegend(sheet2, summaryStatsRowIdx, 5, pctSum);
                    }

                    // Add summary percentage statistics to summary sheet 
                    if (summarySheet != null)
                    {
                        if (summaryPostNames != null)
                        {
                            string abbreviatedPostName = GetSummaryPostName(summaryPost.Name);
                            if (summaryPostNames.Contains(abbreviatedPostName))
                            {
                                int postNameIdx = summaryPostNames.IndexOf(abbreviatedPostName);

                                if (abbreviatedPostName.Equals(summaryPost.Name))
                                {
                                    summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, pctIn);
                                    summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, pctOut);
                                    SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, pctIn);
                                    SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, pctOut);
                                    summarySheet.SetNumberFormat(new Range(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2), "0.0%");
                                }
                                else
                                {
                                    if (summedPostInValues.Keys.Contains(abbreviatedPostName))
                                    {
                                        summedPostInValues[abbreviatedPostName] += pctIn;
                                        summedPostOutValues[abbreviatedPostName] += pctOut;
                                        summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, summedPostInValues[abbreviatedPostName]);
                                        summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, summedPostOutValues[abbreviatedPostName]);
                                        SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, summedPostInValues[abbreviatedPostName]);
                                        SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, summedPostOutValues[abbreviatedPostName]);
                                    }
                                    else
                                    {
                                        summedPostInValues.Add(abbreviatedPostName, pctIn);
                                        summedPostOutValues.Add(abbreviatedPostName, pctOut);
                                        summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, pctIn);
                                        summarySheet.SetCellValue(summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, pctOut);
                                        SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, pctIn);
                                        SetFluxLegend(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2, pctOut);
                                        summarySheet.SetNumberFormat(new Range(summarySheet, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 1, summarySheetLastRowIdx, summarySheetLastColIdx + 2 * postNameIdx + 2), "0.0%");
                                    }
                                }
                            }
                        }
                    }
                    sheet2.SetNumberFormat(new Range(sheet2, summaryStatsRowIdx, 0, summaryStatsRowIdx, 1), "0");
                    sheet2.SetNumberFormat(new Range(sheet2, summaryStatsRowIdx, 3, summaryStatsRowIdx, 5), "0.0%");
                    summaryStatsRowIdx++;
                }
            }
            
            if (summarySheet != null)
            {
                int hyperlinkCol = 0;
                string hyperlinkText = layerWBalsGroup.Zone.ToString();
                if (idColIdx > 0)
                {
                    // use specified columnindex in GEN-file plus Zone- and GEN-file featureId-column
                    hyperlinkCol = idColIdx + 2;
                    hyperlinkText = id;
                }
                summarySheet.SetHyperlink(summarySheetLastRowIdx, hyperlinkCol, null, "'" + sheet2.GetSheetname() + "'!" + "B" + (summaryStatsRowIdx), null, hyperlinkText);
            }

            sheet2.SetBorderColor(new Range(sheet2, firstSummaryStatsRowIdx, 0, summaryStatsRowIdx - 1, 5), Color.Black, BorderWeight.Thin, true);
            sheet2.SetBorderColor(new Range(sheet2, firstSummaryStatsRowIdx, 0, summaryStatsRowIdx - 1, 5), Color.Black, BorderWeight.Thick, false);
            AddZoneTotal(sheet2, firstSummaryStatsRowIdx, summaryStatsRowIdx, 3, 4, 5, currentZone);
            sheet2.SetNumberFormat(new Range(sheet2, summaryStatsRowIdx, 3, summaryStatsRowIdx + 1, 5), "0.0%");

            sheet2.SetAutoFilter(new Range(sheet2, SheetHeaderRowIdx, 0, SheetHeaderRowIdx, 5));
            sheet2.FreezeRow(SheetHeaderRowIdx);
            sheet2.Zoom(85);
            sheet2.AutoFitColumn(2);
            sheet2.Activate(firstSummaryStatsRowIdx, 0);

            // Now set Title, after autofit of columns
            sheet2.SetCellValue(0, 0, "Waterbalans " + Path.GetFileNameWithoutExtension(this.sourceFilename));
            sheet2.SetFontBold(0, 0, true);
            sheet2.SetCellValue(1, 0, "Water balance in percentages of total of external posts over all layers");

            if (idColIdx > 0)
            {
                summarySheet.AutoFitColumn(idColIdx + 2);
            }
        
        }

        /// <summary>
        /// Groups LayerWBal objects by zone
        /// </summary>
        /// <param name="layerWBals"></param>
        /// <param name="isGroupedByZone"></param>
        /// <returns></returns>
        private List<LayerWBalGroup> CreateLayerWBalGroups(List<LayerWBal> layerWBals, bool isGroupedByZone)
        {
            Dictionary<int, List<LayerWBal>> groupDictionary = new Dictionary<int, List<LayerWBal>>();
            List<LayerWBalGroup> groupList = new List<LayerWBalGroup>();

            for (int layerWBalIdx = 0; layerWBalIdx < layerWBals.Count; layerWBalIdx++)
            {
                LayerWBal layerWBal = layerWBals[layerWBalIdx];
                if (isGroupedByZone)
                {
                    if (!HasWBalGroup(groupList, layerWBal.Zone))
                    {
                        GENFeature genFeature = genFile?.Features[layerWBal.Zone - 1];
                        groupList.Add(new LayerWBalGroup(layerWBal.Zone, "Zone " + layerWBal.Zone, new List<LayerWBal>(), layerWBal.ZoneArea, genFeature));
                    }

                    LayerWBalGroup layerWBalGroup = GetWBalGroup(groupList, layerWBal.Zone);
                    layerWBalGroup.AddLayerWBal(layerWBal);
                }
                else
                {
                    if (groupList.Count == 0)
                    {
                        groupList.Add(new LayerWBalGroup(0, "Total", new List<LayerWBal>(), 0, null));
                    }
                    groupList[0].LayerWBals.Add(layerWBal);
                }
            }
            // groupList.RemoveRange(3, grouplist.Count - 3); // for testing purposes
            return groupList;
        }

        public bool HasWBalGroup(List<LayerWBalGroup> layerWBalGroups, int zone)
        {
            return GetWBalGroup(layerWBalGroups, zone) != null;
        }

        public LayerWBalGroup GetWBalGroup(List<LayerWBalGroup> layerWBalGroups, int zone)
        {
            for (int idx = 0; idx < layerWBalGroups.Count; idx++)
            {
                if (layerWBalGroups[idx].Zone.Equals(zone))
                {
                    return layerWBalGroups[idx];
                }
            }
            return null;
        }

        private void AddZoneTotal(IWorksheet sheet, int zoneFirstRowIdx, int rowIdx, int inColIdx, int outColIdx, int sumColIdx, int zone)
        {
            sheet.SetCellValue(rowIdx, 0, zone);
            sheet.SetCellValue(rowIdx, 1, "Total");
            sheet.SetCellFormula(rowIdx, inColIdx, "=SUM(" + SpreadsheetUtils.ExcelColumnFromNumber(inColIdx + 1) + (zoneFirstRowIdx + 1) + ":" + SpreadsheetUtils.ExcelColumnFromNumber(inColIdx + 1) + (rowIdx) + ")");
            sheet.SetNumberFormat(rowIdx, inColIdx, "0.00");
            sheet.SetCellFormula(rowIdx, outColIdx, "=SUM(" + SpreadsheetUtils.ExcelColumnFromNumber(outColIdx + 1) + (zoneFirstRowIdx + 1) + ":" + SpreadsheetUtils.ExcelColumnFromNumber(outColIdx + 1) + (rowIdx) + ")");
            sheet.SetNumberFormat(rowIdx, outColIdx, "0.00");
            sheet.SetCellFormula(rowIdx, sumColIdx, "=SUM(" + SpreadsheetUtils.ExcelColumnFromNumber(sumColIdx + 1) + (zoneFirstRowIdx + 1) + ":" + SpreadsheetUtils.ExcelColumnFromNumber(sumColIdx + 1) + (rowIdx) + ")");
            sheet.SetNumberFormat(rowIdx, sumColIdx, "0.00");
            sheet.SetBorderColor(new Range(sheet, rowIdx, 0, rowIdx, sumColIdx), Color.Black, BorderWeight.Thin, true);
            sheet.SetBorderColor(new Range(sheet, rowIdx, 0, rowIdx, sumColIdx), Color.Black, BorderWeight.Thick, false);
            sheet.SetFontBold(new Range(sheet, rowIdx, 0, rowIdx, sumColIdx), true);
        }

        private void SetFluxLegend(IWorksheet sheet, int rowIdx, int colIdx, double value)
        {
            if (value > 0.1)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(141, 180, 226));
            }
            else if (value > 0.05)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(197, 217, 241));
            }
            else if (value > 0.02)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(220, 239, 244));
            } else if (value < -0.1)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(218, 150, 148));
            }
            else if (value < -0.05)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(250, 191, 143));
            }
            else if (value < -0.02)
            {
                sheet.SetInteriorColor(rowIdx, colIdx, Color.FromArgb(253, 233, 217));
            }
        }
    }
}
