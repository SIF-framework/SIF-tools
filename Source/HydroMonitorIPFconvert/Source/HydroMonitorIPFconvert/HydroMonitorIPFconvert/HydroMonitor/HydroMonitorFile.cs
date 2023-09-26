// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Class for reading, storing and converting an HydroMonitor-file
    /// </summary>
    public class HydroMonitorFile
    {
        /// <summary>
        /// A sheetname or (one-based) sheet index to read Excel input data from 
        /// </summary>
        public static string ExcelSheetId { get; set; }

        public string Filename { get; set; }
        public string HydroMonitorVersion { get; }
        public string FormatVersion { get; private set; }
        public string FormatDefinition { get; private set; }
        public string FileType { get; private set; }
        public List<string> FileContents { get; private set; }
        public HydroObjectType ObjectType { get; private set; }
        public List<string> ObjectIdentificationNames { get; private set; }

        /// <summary>
        /// All available metadata columnnames in order of input file, including XY- and Id-columns
        /// </summary>
        public List<string> MetadataColumnNames { get; private set; }
        public List<string> MetadataColumnUnits { get; private set; }

        /// <summary>
        /// All available data columnnames in order of input file, Id-columns
        /// </summary>
        public List<string> DataColumnNames { get; private set; }
        public List<string> DataColumnUnits { get; private set; }

        public List<HydroObject> HydroObjects { get; private set; }

        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public HydroMonitorFile()
        {
            HydroObjects = new List<HydroObject>();
            MetadataColumnNames = new List<string>();
            MetadataColumnUnits = new List<string>();
            DataColumnNames = new List<string>();
            DataColumnUnits = new List<string>();
        }

        public List<string> XYColumnNames
        {
            get { return new List<string>() { HydroMonitorSettings.XCoordinateString, HydroMonitorSettings.YCoordinateString }; }
        }

        //public List<string> ObligatoryColumnNames
        //{
        //    get
        //    {
        //        List<string> obligatoryColumnNames = new List<string>() { HydroMonitorSettings.XCoordinateString, HydroMonitorSettings.YCoordinateString };
        //        obligatoryColumnNames.AddRange(ObjectIdentificationNames);
        //        return obligatoryColumnNames;
        //    }
        //}

        public static HydroMonitorFile ReadFile(string inputFilename, Log log)
        {
            return ReadFile(inputFilename, null, log);
        }

        public static HydroMonitorFile ReadFile(string inputFilename, string sheetId, Log log)
        {
            HydroMonitorFile hydroMonitorFile = null;

            string sourceFileExtension = Path.GetExtension(inputFilename).ToLower();
            if (sourceFileExtension.Equals(".xlsx") || sourceFileExtension.Equals(".xls"))
            {
                hydroMonitorFile = ImportHydroMonitorExcelFile(inputFilename, sheetId ?? "1", log);
            }
            else
            {
                throw new ToolException("Import of HydroMonitor-files with extension '" + sourceFileExtension + "' is currently not supported: " + inputFilename);
            }

            return hydroMonitorFile;
        }

        private static HydroMonitorFile ImportHydroMonitorExcelFile(string excelFilename, string sheetId, Log log)
        {
            HydroMonitorFile hydroMonitorFile = null;

            ExcelManager excelManager = null;
            IWorkbook workbook = null;
            try
            {
                excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
                workbook = excelManager.OpenWorkbook(excelFilename, ";");
                IWorksheet sheet = null;
                if (int.TryParse(sheetId, out int sheetNr))
                {
                    sheet = workbook.GetSheet(sheetNr - 1);
                }
                else
                {
                    sheet = workbook.GetSheet(sheetId);
                }

                // Check for HydroMonitor format
                int rowIdx = CheckFormat(sheet);

                // Now start reading HydroMonitor Header values
                hydroMonitorFile = new HydroMonitorFile();
                hydroMonitorFile.Filename = excelFilename;
                try
                {
                    hydroMonitorFile.ReadHeader(sheet, ref rowIdx, log);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read HydroMonitor header for " + Path.GetFileName(excelFilename), ex);
                }

                if (hydroMonitorFile.HasMetadataTable())
                {
                    try
                    {
                        hydroMonitorFile.ReadMetadataTable(sheet, ref rowIdx, log);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not read HydroMonitor metadata for " + Path.GetFileName(excelFilename), ex);
                    }
                }

                if (hydroMonitorFile.HasDataTable())
                {
                    try
                    {
                        hydroMonitorFile.ReadDataTable(sheet, ref rowIdx, log);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not read HydroMonitor data for " + Path.GetFileName(excelFilename), ex);
                    }
                }
            }
            catch (ToolException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read Excel file " + Path.GetFileName(excelFilename), ex);
            }
            finally
            {
                if (workbook != null)
                {
                    workbook.Close();
                    workbook = null;
                    excelManager.Cleanup();
                }
            }

            hydroMonitorFile.Check();

            return hydroMonitorFile;
        }

        private bool HasDataTable()
        {
            return FileContents.Contains(HydroMonitorSettings.FormatContentsMetadataString);
        }

        private bool HasMetadataTable()
        {
            return FileContents.Contains(HydroMonitorSettings.FormatContentsMetadataString);
        }

        /// <summary>
        /// Checks that sheet contains data in HydroMonitor format
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns>row index of next row of header data, with Format Version</returns>
        private static int CheckFormat(IWorksheet sheet)
        {
            string cellA1Value = sheet.GetCellValue(0, 0);
            string cellA2Value = sheet.GetCellValue(0, 1);
            if ((cellA1Value == null) || !cellA1Value.ToLower().Equals(HydroMonitorSettings.FormatNameHeaderString.ToLower()))
            {
                throw new ToolException("Excel sheet does not have HydroMonitor format: '" + HydroMonitorSettings.FormatNameHeaderString + "' expected in cell A1");
            }
            if ((cellA2Value == null) || !cellA2Value.ToLower().StartsWith(HydroMonitorSettings.FormatNameHydroMonitorMinString.ToLower()))
            {
                throw new ToolException("Excel sheet does not have HydroMonitor format: value starting with '" + HydroMonitorSettings.FormatNameHydroMonitorMinString + "' expected in cell B1");
            }

            return 1;
        }

        private void ReadHeader(IWorksheet sheet, ref int rowIdx, Log log)
        {
            FormatVersion = sheet.GetCellValue(rowIdx++, 1);
            FormatDefinition = sheet.GetCellValue(rowIdx++, 1);
            FileType = sheet.GetCellValue(rowIdx++, 1);

            FileContents = new List<string>();
            int colIdx = 1;
            while (!sheet.IsEmpty(rowIdx, colIdx))
            {
                FileContents.Add(sheet.GetCellValue(rowIdx, colIdx++));
            }
            rowIdx++;

            ObjectType = ParseObjectType(sheet.GetCellValue(rowIdx++, 1));

            ObjectIdentificationNames = new List<string>();
            colIdx = 1;
            while (!sheet.IsEmpty(rowIdx, colIdx))
            {
                ObjectIdentificationNames.Add(sheet.GetCellValue(rowIdx, colIdx++));
            }
            rowIdx++;
        }

        /// <summary>
        /// Read Data table part of file
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="rowIdx"></param>
        /// <param name="log"></param>
        /// <returns>number of metadata entries read</returns>
        private void ReadMetadataTable(IWorksheet sheet, ref int rowIdx, Log log)
        {
            // Skip empty rows
            while (sheet.IsEmpty(rowIdx, 1))
            {
                rowIdx++;
            }
            int headerRowIdx = rowIdx;
            rowIdx += 2;

            // Read column definitions
            int colIdx = 0;
            MetadataColumnNames = new List<string>();
            MetadataColumnUnits = new List<string>();
            while (!sheet.IsEmpty(headerRowIdx, colIdx))
            {
                string columnName = sheet.GetCellValue(headerRowIdx, colIdx);
                string columnUnit = sheet.GetCellValue(headerRowIdx + 1, colIdx);
                CheckUnit(columnUnit, log, 2);
                MetadataColumnNames.Add(columnName);
                MetadataColumnUnits.Add(columnUnit);
                colIdx++;
            }

            // Read object metadata
            while (!sheet.IsEmpty(rowIdx, 0))
            {
                // HydroObject hydroObject = HydroObjectFactory.CreateObject(this, ObjectType);
                HydroMetadataEntry metadataEntry = new HydroMetadataEntry(this);

                colIdx = 0;
                while (!sheet.IsEmpty(headerRowIdx, colIdx))
                {
                    string columnName = sheet.GetCellValue(headerRowIdx, colIdx);
                    string columnValue = null;
                    if (IsDateTimeMetadataColumn(columnName))
                    {
                        object objectValue = sheet.GetCellObjectValue(rowIdx, colIdx);
                        if (objectValue != null)
                        {
                            if (objectValue is DateTime)
                            {
                                columnValue = ((DateTime)objectValue).ToString(HydroMonitorSettings.DateTimeFormatString);
                            }
                            else if (objectValue is double)
                            {
                                columnValue = DateTime.FromOADate((double)objectValue).ToString(HydroMonitorSettings.DateTimeFormatString);
                            }
                            else
                            {
                                columnValue = objectValue.ToString();
                            }
                        }
                        else
                        {
                            columnValue = null;
                        }
                    }
                    else
                    {
                        columnValue = sheet.GetCellValue(rowIdx, colIdx, englishCultureInfo);
                    }

                    metadataEntry.AddMetadataValue(columnName, columnValue);

                    colIdx++;
                }

                HydroObject hydroObject = GetHydroObject(metadataEntry.Id);
                if (hydroObject == null)
                {
                    hydroObject = HydroObjectFactory.CreateObject(this, ObjectType, metadataEntry.Id);
                    AddHydroObject(hydroObject);
                }
                hydroObject.AddMetadataEntry(metadataEntry);

                rowIdx++;
            }
        }

        private void ReadDataTable(IWorksheet sheet, ref int rowIdx, Log log)
        {
            // Skip max 2 empty rows
            if (sheet.IsEmpty(rowIdx, 0))
            {
                rowIdx++;
            }
            if (sheet.IsEmpty(rowIdx, 0))
            {
                rowIdx++;
            }

            if (sheet.IsEmpty(rowIdx, 0))
            {
                throw new ToolException("Data table header not found");
            }
            else
            {
                for (int colIdx = 0; colIdx < ObjectIdentificationNames.Count; colIdx++)
                {
                    if (!sheet.GetCellValue(rowIdx, colIdx).Equals(ObjectIdentificationNames[colIdx]))
                    {
                        throw new ToolException("ObjectIdentification names (" + CommonUtils.ToString(ObjectIdentificationNames) + ") not found for Data table in cell A" + (rowIdx + 1));
                    }
                }
            }

            // Find data table cell range
            int lastRowIdx;
            int lastColIdx;
            try
            {
                lastRowIdx = sheet.GetCell(sheet.LastRowIdx, 0).End(CellDirection.Up).RowIdx;
                lastColIdx = sheet.GetCell(rowIdx, sheet.LastColIdx).End(CellDirection.ToLeft).ColIdx;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get last column or row index", ex);
            }

            if (lastColIdx < ObjectIdentificationNames.Count + 2 - 1)
            {
                throw new ToolException("Data table value columns missing in row " + (rowIdx + 1));
            }

            if (lastRowIdx == rowIdx)
            {
                throw new ToolException("Data table value entries not found");
            }

            // Read Data column names and units
            // HydroData hydroData = new HydroData();
            for (int colIdx = 0; colIdx <= lastColIdx; colIdx++)
            {
                string columnName = sheet.GetCellValue(rowIdx, colIdx);
                string columnUnit = sheet.GetCellValue(rowIdx + 1, colIdx);
                CheckUnit(columnUnit, log, 2);

                AddDataColumn(columnName, columnUnit);
            }

            object[,] values = null;
            try
            {
                Range range = sheet.GetRange(rowIdx + 2, 0, lastRowIdx, lastColIdx);
                values = range.GetValues();
            }
            catch (Exception ex)
            {
                throw new ToolException("Could not read range [(1,1),(" + (lastRowIdx + 1) + "," + (lastColIdx + 1) + ")", ex);
            }

            if (values == null)
            {
                throw new ToolException("Could not read range [(1,1),(" + (lastRowIdx + 1) + "," + (lastColIdx + 1) + ")");
            }

            // Now start reading measurements
            HydroObject hydroObject = null;
            for (int valIdx = 0; valIdx < values.GetLength(0); valIdx++)
            {
                List<string> idStrings = new List<string>();

                for (int colIdx = 0; colIdx < ObjectIdentificationNames.Count; colIdx++)
                {
                    object idValue = values[valIdx, colIdx];
                    if (idValue != null)
                    {
                        string idString = idValue.ToString();
                        idStrings.Add(idString);
                    }
                }

                string id = HydroUtils.GetId(idStrings);
                if ((hydroObject == null) || !hydroObject.Id.Equals(id))
                {
                    hydroObject = GetHydroObject(id);
                    if (hydroObject == null)
                    {
                        throw new ToolException("Data found for unknown HydroObject: " + id);
                    }
                }

                List<object> rowValues = new List<object>(idStrings);
                for (int colIdx = ObjectIdentificationNames.Count; colIdx <= lastColIdx; colIdx++)
                {
                    object value = values[valIdx, colIdx];
                    rowValues.Add(value);
                }
                hydroObject.AddDataRow(rowValues);

            }
        }

        private HydroObject GetHydroObject(object id)
        {
            foreach (HydroObject hydroObject in HydroObjects)
            {
                if (hydroObject.Id.Equals(id))
                {
                    return hydroObject;
                }
            }
            return null;
        }

        public void AddDataColumn(string columnName, string columnUnit)
        {
            DataColumnNames.Add(columnName);
            DataColumnUnits.Add(columnUnit);
        }

        public void Clean()
        {
            List<int> colValueCounts = new List<int>();
            for (int colIdx = 0; colIdx < MetadataColumnNames.Count; colIdx++)
            {
                colValueCounts.Add(0);
            }

            // Count non-empty metadata values per column
            foreach (HydroObject hydroObject in HydroObjects)
            {
                foreach (HydroMetadataEntry metadataEntry in hydroObject.Metadata)
                {
                    for (int colIdx = 0; colIdx < metadataEntry.Values.Count; colIdx++)
                    {
                        string columnValue = metadataEntry.Values[colIdx];
                        if (!string.IsNullOrEmpty(columnValue))
                        {
                            colValueCounts[colIdx]++;
                        }
                    }
                }
            }

            // Remove empty columns
            int colIdx2 = 0;
            while (colIdx2 < colValueCounts.Count)
            {
                if (colValueCounts[colIdx2] == 0)
                {
                    MetadataColumnNames.RemoveAt(colIdx2);
                    MetadataColumnUnits.RemoveAt(colIdx2);

                    foreach (HydroObject hydroObject in HydroObjects)
                    {
                        foreach (HydroMetadataEntry metadataEntry in hydroObject.Metadata)
                        {
                            metadataEntry.Values.RemoveAt(colIdx2);
                        }
                    }

                    colValueCounts.RemoveAt(colIdx2);
                }
                else
                {
                    colIdx2++;
                }
            }
        }

        public void Check()
        {
            foreach (HydroObject hydroObject in HydroObjects)
            {
                hydroObject.Check(true);
            }
        }

        public void CheckUnit(string unitString, Log log, int logIndentLevel = 0)
        {
            if (!HydroMonitorSettings.ValidUnitStrings.Contains(unitString))
            {
                log.AddWarning("Undefined unit string: " + unitString, logIndentLevel);
            }
        }

        public bool IsDateTimeMetadataColumn(string columnName)
        {
            string columnUnit = GetMetadataColumnUnit(columnName);
            return columnUnit.Equals(HydroMonitorSettings.ExcelDateUnitString);
        }

        public bool IsDateTimeMetadataColumn(int colIdx)
        {
            string columnUnit = MetadataColumnUnits[colIdx];
            return (columnUnit != null) ? columnUnit.Equals(HydroMonitorSettings.ExcelDateUnitString) : false;
        }

        public bool IsDateTimeDataColumn(string columnName)
        {
            string columnUnit = GetDataColumnUnit(columnName);
            return (columnUnit != null) ? columnUnit.Equals(HydroMonitorSettings.ExcelDateUnitString) : false;
        }

        public bool IsDateTimeDataColumn(int colIdx)
        {
            string columnUnit = DataColumnUnits[colIdx];
            return (columnUnit != null) ? columnUnit.Equals(HydroMonitorSettings.ExcelDateUnitString) : false;
        }

        /// <summary>
        /// Retrieve column index for specified column name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns>-1 if not found</returns>
        public int GetMetadataColumnIndex(string columnName)
        {
            int colIdx = MetadataColumnNames.IndexOf(columnName);
            if (colIdx >= 0)
            {
                return colIdx;
            }

            return -1;
        }

        public string GetMetadataColumnUnit(string columnName)
        {
            string columnUnit = null;
            int colIdx = MetadataColumnNames.IndexOf(columnName);
            if (colIdx >= 0)
            {
                columnUnit = MetadataColumnUnits[colIdx];
            }

            return columnUnit;
        }

        /// <summary>
        /// Retrieve column index for specified column name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns>-1 if not found</returns>
        public int GetDataColumnIndex(string columnName)
        {
            int colIdx = DataColumnNames.IndexOf(columnName);
            if (colIdx >= 0)
            {
                return colIdx;
            }

            return -1;
        }

        public string GetDataColumnUnit(string columnName)
        {
            string columnUnit = null;
            int colIdx = DataColumnNames.IndexOf(columnName);
            if (colIdx >= 0)
            {
                columnUnit = DataColumnUnits[colIdx];
            }

            return columnUnit;
        }

        public void AddHydroObject(HydroObject hydroObject)
        {
            hydroObject.Check();
            HydroObjects.Add(hydroObject);
        }

        private static HydroObjectType ParseObjectType(string objectTypeString)
        {
            switch(objectTypeString.Trim().ToLower())
            {
                case "observationwell":
                    return HydroObjectType.ObservationWell;
                case "surfacewaterlevelgauge":
                    return HydroObjectType.SurfaceWaterLevelGauge;
                case "pumpingwell":
                    return HydroObjectType.PumpingWell;
                case "weatherstation":
                    return HydroObjectType.WeatherStation;
                //case "boreholelog":
                //    return HydroObjectType.BoreholeLog;
                //case "manualmeasurement":
                //    return HydroObjectType.ManualMeasurement;
                //case "loggermeasurement":
                //    return HydroObjectType.LoggerMeasurement;
                default: throw new Exception("Unrecognized HydroMonitor ObjectType: " + objectTypeString);
            }
        }

        /// <summary>
        /// Export HydroMonitor-file to specified file
        /// </summary>
        /// <param name="outputFilename"></param>
        /// <param name="metadata"></param>
        public void Export(string outputFilename, Metadata metadata = null)
        {
            Export(outputFilename, null, metadata);
        }

        /// <summary>
        /// Export selected columns of HydroMonitor-file in specified order to specified file
        /// </summary>
        /// <param name="outputFilename"></param>
        /// <param name="selColumnNames">strings with selected columnnames or columnnumbers</param>
        /// <param name="metadata"></param>
        public void Export(string outputFilename, SIFToolSettings settings, Metadata metadata = null)
        {
            string extension = Path.GetExtension(outputFilename);
            switch (extension.ToLower())
            {
                case ".ipf":
                    ExportIPF(outputFilename, settings, metadata);
                    break;
                default:
                    throw new Exception("Export not defined for file extension '" + extension + "': " + outputFilename);
            }
        }

        private void ExportIPF(string outputFilename, SIFToolSettings settings, Metadata metadata = null)
        {
            List<string> selColumnStrings = settings.ResultColumnNames;

            IPFFile ipfFile = new IPFFile();
            ipfFile.Filename = outputFilename;
            ipfFile.AddXYColumns();

            bool hasTimeseries = false;
            List<int> selColumnIndices = null;
            if (selColumnStrings == null)
            {
                // Add all metadata columns as a default in order: X, Y, Ids and rest
                ipfFile.AddColumns(ObjectIdentificationNames);
                foreach (string columnName in MetadataColumnNames)
                {
                    if (!XYColumnNames.Contains(columnName) && !ObjectIdentificationNames.Contains(columnName))
                    {
                        ipfFile.AddColumn(columnName);
                    }
                }
            }
            else
            {
                // Add specified columns
                selColumnIndices = new List<int>();

                // Remove XY-columns from specified list, these have been added as obligatory columns for IPF-files
                if (selColumnStrings.Contains(HydroMonitorSettings.XCoordinateString))
                {
                    selColumnStrings.Remove(HydroMonitorSettings.XCoordinateString);
                }
                if (selColumnStrings.Contains(HydroMonitorSettings.YCoordinateString))
                {
                    selColumnStrings.Remove(HydroMonitorSettings.YCoordinateString);
                }

                for (int selColIdx = 0; selColIdx < selColumnStrings.Count; selColIdx++)
                {
                    string selColString = selColumnStrings[selColIdx];

                    int colIdx = GetMetadataColumnIndex(selColString);
                    if (colIdx == -1)
                    {
                        // Column not found, check if string is a column number
                        if (!int.TryParse(selColString, out int colNr))
                        {
                            throw new ToolException("Invalid columnstring, column name or number not found: " + selColString);
                        }
                        if ((colNr < 0) || (colNr > MetadataColumnNames.Count))
                        {
                            throw new ToolException("Invalid column number, expected between 0 and " + MetadataColumnNames.Count + ": " + selColString);
                        }
                        colIdx = colNr - 1;
                    }
                    selColumnIndices.Add(colIdx);
                    string columnName = null;
                    if (colIdx == -1)
                    {
                        columnName = SIFToolSettings.TSFilename;
                        ipfFile.AddColumn(columnName);
                        ipfFile.AssociatedFileColIdx = ipfFile.ColumnCount - 1;
                        hasTimeseries = true;
                    }
                    else
                    {
                        columnName = MetadataColumnNames[colIdx];
                        ipfFile.AddColumn(columnName);
                    }
                }
            }

            Extent clipExtent = settings.Extent;
            foreach (HydroObject hydroObject in HydroObjects)
            {
                HydroMetadataEntry metadataEntry = hydroObject.GetMetadataEntry(HydroMetadataSearchMethod.Recent);
                if (metadataEntry != null)
                {
                    if ((clipExtent == null) || clipExtent.Contains(metadataEntry.Point.X, metadataEntry.Point.Y))
                    {
                        List<string> ipfColumnValues = new List<string>();
                        ipfColumnValues.Add(metadataEntry.XString);
                        ipfColumnValues.Add(metadataEntry.YString);

                        IPFTimeseries ipfTimeseries = null;
                        if ((hydroObject.DataValues.Count > 0) && (hydroObject is TimeseriesHydroObject))
                        {
                            Timeseries ts = null;
                            int dateColIdx = ((TimeseriesHydroObject)hydroObject).GetDateColumnIndex();
                            int volumeColIdx = dateColIdx + 1;
                            if (DataColumnUnits[volumeColIdx].Equals(HydroMonitorSettings.VolumeUnitString))
                            {
                                int valColIdx = volumeColIdx - dateColIdx - 1;
                                Timeseries tsVolume = ((TimeseriesHydroObject)hydroObject).RetrieveTimeseries();
                                ts = HydroUtils.ConvertVolumeToRate(tsVolume, valColIdx, Unit.m3d, settings.VolumeFirstVolumeMethod, settings.VolumeEndDateMethod);
                            }
                            else
                            {
                                ts = ((TimeseriesHydroObject)hydroObject).RetrieveTimeseries();
                            }
                            if ((settings.StartDate != null) || (settings.EndDate != null))
                            {
                                ts = ts.Select(settings.StartDate, settings.EndDate, -1);
                                ExtendTS(ts, settings.StartDate, settings.EndDate, 0);
                            }

                            List<string> valueColNames = ((TimeseriesHydroObject)hydroObject).RetrieveDataValueColumnNames();
                            ipfTimeseries = new IPFTimeseries(ts, valueColNames);
                        }

                        IPFPoint ipfPoint = null;
                        if (selColumnIndices == null)
                        {
                            // Add column values in default order: X, Y, Ids and rest
                            ipfColumnValues.AddRange(metadataEntry.IdStrings);
                            for (int colIdx = 0; colIdx < MetadataColumnNames.Count; colIdx++)
                            {
                                string columnName = MetadataColumnNames[colIdx];
                                string columnValue = metadataEntry.Values[colIdx];
                                if (!XYColumnNames.Contains(columnName) && !ObjectIdentificationNames.Contains(columnName))
                                {
                                    ipfColumnValues.Add(columnValue);
                                }
                            }

                            ipfPoint = new IPFPoint(ipfFile, metadataEntry.Point, ipfColumnValues);
                            if (ipfTimeseries != null)
                            {
                                if (!hasTimeseries)
                                {
                                    hasTimeseries = true;
                                    ipfFile.AddColumn(SIFToolSettings.TSFilename);
                                    ipfFile.AssociatedFileColIdx = ipfFile.ColumnCount - 1;
                                }
                                //  Add associated timeseries 
                                string txtFilenameWithoutExtension = Path.Combine(Path.GetFileNameWithoutExtension(outputFilename), hydroObject.Id);
                                ipfPoint.ColumnValues.Add(txtFilenameWithoutExtension);
                                ipfPoint.Timeseries = ipfTimeseries;
                            }
                        }
                        else
                        {
                            // Add selected column values in specified order
                            for (int idx = 0; idx < selColumnIndices.Count; idx++)
                            {
                                int selColIdx = selColumnIndices[idx];
                                if (selColIdx != -1)
                                {
                                    string columnName = MetadataColumnNames[selColIdx];
                                    string columnValue = metadataEntry.Values[selColIdx];
                                    ipfColumnValues.Add(columnValue);
                                }
                                else
                                {
                                    if (ipfTimeseries != null)
                                    {
                                        //  Add associated timeseries 
                                        string txtFilenameWithoutExtension = Path.Combine(Path.GetFileNameWithoutExtension(outputFilename), hydroObject.Id);
                                        ipfColumnValues.Add(txtFilenameWithoutExtension);
                                    }
                                }
                            }

                            ipfPoint = new IPFPoint(ipfFile, metadataEntry.Point, ipfColumnValues);
                            ipfPoint.Timeseries = ipfTimeseries;
                        }

                        ipfFile.AddPoint(ipfPoint);
                    }
                }
            }

            ipfFile.WriteFile(outputFilename);
        }

        /// <summary>
        /// Extend this timeseries with specified value to specified period, by adding the specified value for start- and enddate.
        /// When the difference between the start- or enddate is more than the specified resolution an extra timestamp with the specified value is added just before and after existing dates.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="resolution">optional resolution in day units, default is 1 day</param>
        /// <returns></returns>
        public void ExtendTS(Timeseries ts, DateTime? startDate, DateTime? endDate, float value, double resolution = 1)
        {
            if (ts.Timestamps.Count > 0)
            {
                if (startDate != null)
                {
                    if (((DateTime)startDate) < ts.Timestamps[0])
                    {
                        ts.Timestamps.Insert(0, (DateTime)startDate);
                        ts.Values.Insert(0, value);

                        double timespan = ts.Timestamps[1].Subtract(ts.Timestamps[0]).TotalDays;
                        if (timespan > resolution)
                        {
                            // Add extra date before existing date
                            timespan = timespan % resolution;
                            if (timespan.Equals(0))
                            {
                                timespan = resolution;
                            }
                            DateTime extraDate = ts.Timestamps[1].AddDays(-resolution);
                            ts.Timestamps.Insert(1, (DateTime)extraDate);
                            ts.Values.Insert(1, value);
                        }
                    }
                }
                if (endDate != null)
                {
                    if (((DateTime)endDate) > ts.Timestamps[ts.Timestamps.Count - 1])
                    {
                        ts.Timestamps.Add((DateTime)endDate);
                        ts.Values.Add(value);

                        double timespan = ts.Timestamps[ts.Timestamps.Count - 1].Subtract(ts.Timestamps[ts.Timestamps.Count - 2]).TotalDays;
                        if (timespan > resolution)
                        {
                            // Add extra date after existing date
                            timespan = timespan % resolution;
                            if (timespan.Equals(0))
                            {
                                timespan = resolution;
                            }
                            DateTime extraDate = ts.Timestamps[ts.Timestamps.Count - 2].AddDays(resolution);
                            ts.Timestamps.Insert(ts.Timestamps.Count - 1, (DateTime)extraDate);
                            ts.Values.Insert(ts.Timestamps.Count - 1, value);
                        }
                    }
                }
            }
            else
            {
                ts.Timestamps.Add((DateTime)startDate);
                ts.Timestamps.Add((DateTime)endDate);
                ts.Values.Add(value);
                ts.Values.Add(value);
            }
        }
    }
}
