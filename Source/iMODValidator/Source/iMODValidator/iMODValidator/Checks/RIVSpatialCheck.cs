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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using OrderedPropertyGrid;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Models.Files;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class RIVSpatialCheckSettings : CheckSettings
    {
        [Category("Change-properties"), Description("The maximum valid relative change in stage per meter (m/m) this region"), PropertyOrder(10)]
        public string MaxRelStageLevelChange { get; set; }

        [Category("Change-properties"), Description("The maximum valid absolute change in stage (m) this region"), PropertyOrder(11)]
        public string MaxAbsStageLevelChange
        {
            get { return MaxAbsStageLevelChange1; }
            set { MaxAbsStageLevelChange1 = value; }
        }

        [Category("Change-properties"), Description("The minimum nunmber of surrounding cells with an exceeded stage change, before reporting a warning in this region"), PropertyOrder(12)]
        public string MinStageChangeCellCount
        {
            get { return MinStageChangeCellCount1; }
            set { MinStageChangeCellCount1 = value; }
        }

        [Category("Change-properties"), Description("The maximum nunmber of surrounding cells with an exceeded stage change, for  reporting a warning in this region"), PropertyOrder(13)]
        public string MaxStageChangeCellCount { get; set; }

        [Category("Change-properties"), Description("The maximum valid relative change in bottom level per meter (m/m) this region"), PropertyOrder(20)]
        public string MaxRelBottomLevelChange { get; set; }

        [Category("Change-properties"), Description("The maximum valid absolute change in bottom level per meter (m/m) this region"), PropertyOrder(20)]
        public string MaxAbsBottomLevelChange { get; set; }

        [Category("Change-properties"), Description("The minimum nunmber of surrounding cells with an exceeded bottom change, before reporting a warning in this region"), PropertyOrder(21)]
        public string MinBottomChangeCellCount { get; set; }

        [Category("Change-properties"), Description("The maximum nunmber of surrounding cells with an exceeded bottom change, before reporting a warning in this region"), PropertyOrder(22)]
        public string MaxBottomChangeCellCount { get; set; }

        [Category("Orphan-properties"), Description("The number of cells in each direction (N,E,S,W) that are investigated around a possible orphan cell"), PropertyOrder(30)]
        public string CellDistance { get; set; }

        [Category("Orphan-properties"), Description("The maximum levels of recursive checks for connected neighbours of neighbourcells around a possible orphan cell (use 0 for no recursion)"), PropertyOrder(31)]
        public string MaxRecursiveLevel { get; set; }

        [Category("Orphan-properties"), Description("The maximum number of surrounding cells (including possible ophan) than may have the same value as a possible orphan cell"), PropertyOrder(32)]
        public string MaxMainConnectionCount { get; set; }

        [Category("Orphan-properties"), Description("The minimum number of neighbouring cells that have the same, most occurring value, other than the value of a possible orphan cell"), PropertyOrder(33)]
        public string MinMostOccurringCount { get; set; }

        [Category("Orphan-properties"), Description("The maximum number of surrounding cells with another value than the most occurring value or the value of a possible orphan cell"), PropertyOrder(34)]
        public string MaxOtherValueCount { get; set; }

        [Category("Orphan-properties"), Description("The minimum difference between a possible orphan cell and the most ocurring surrounding values to report it as an orphan cell"), PropertyOrder(35)]
        public string MinDifference { get; set; }

        public string MaxAbsStageLevelChange1 { get; set; }
        public string MinStageChangeCellCount1 { get; set; }

        public RIVSpatialCheckSettings(string checkName) : base(checkName)
        {
            MaxRelStageLevelChange = "0.04";
            MaxAbsStageLevelChange1 = "2";
            MinStageChangeCellCount1 = "6";
            MaxStageChangeCellCount = "8";
            MaxRelBottomLevelChange = "0.5";
            MaxAbsBottomLevelChange = "10";
            MinBottomChangeCellCount = "6";
            MaxBottomChangeCellCount = "8";

            MaxMainConnectionCount = "2";
            MinMostOccurringCount = "4";
            MaxOtherValueCount = "1";
            CellDistance = "1";
            MaxRecursiveLevel = "1";
            MinDifference = "0.1";
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("MaxRelStageLevelChange: " + MaxRelStageLevelChange, logIndentLevel);
            log.AddInfo("MaxAbsStageLevelChange: " + MaxAbsStageLevelChange1, logIndentLevel);
            log.AddInfo("MinStageChangeCellCount: " + MinStageChangeCellCount1, logIndentLevel);
            log.AddInfo("MaxStageChangeCellCount: " + MaxStageChangeCellCount, logIndentLevel);
            log.AddInfo("MaxRelBottomLevelChange: " + MaxRelBottomLevelChange, logIndentLevel);
            log.AddInfo("MaxAbsBottomLevelChange: " + MaxAbsBottomLevelChange, logIndentLevel);
            log.AddInfo("MinBottomChangeCellCount: " + MinBottomChangeCellCount, logIndentLevel);
            log.AddInfo("MaxBottomChangeCellCount: " + MaxBottomChangeCellCount, logIndentLevel);

            log.AddInfo("MaxMainConnectionCount: " + MaxMainConnectionCount, logIndentLevel);
            log.AddInfo("MinMostOccurringCount: " + MinMostOccurringCount, logIndentLevel);
            log.AddInfo("MaxOtherValueCount: " + MaxOtherValueCount, logIndentLevel);
            log.AddInfo("CellDistance: " + CellDistance, logIndentLevel);
            log.AddInfo("MaxRecursiveLevel: " + MaxRecursiveLevel, logIndentLevel);
            log.AddInfo("MinDifference: " + MinDifference, logIndentLevel);
        }
    }

    class RIVSpatialCheck : Check
    {
        private CheckWarning OrphanedStageWarning;
        private CheckWarning UnexpectedStageLevelChangeWarning;
        private CheckWarning UnexpectedBottomLevelChangeWarning;

        protected IDFLegend warningLegend;

        public override string Abbreviation
        {
            get { return "RIV-spatial"; }
        }

        public override string Description
        {
            get { return "Checks spatial consistincy of bottom and stage per system"; }
        }

        private RIVSpatialCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is RIVSpatialCheckSettings)
                {
                    settings = (RIVSpatialCheckSettings)value;
                }
            }
        }

        public RIVSpatialCheck()
        {
            settings = new RIVSpatialCheckSettings(this.Name);

            // Define warnings
            UnexpectedStageLevelChangeWarning = new CheckWarning(1, "Unexpected stage level change", "Stage level change to neighbouring cell is above expected maximum of "
                + settings.MaxRelStageLevelChange.ToString() + "m/m or " + settings.MaxAbsStageLevelChange + "m");
            UnexpectedBottomLevelChangeWarning = new CheckWarning(2, "Unexpected bottom level change", "Bottom level change to neighbouring cell is above expected maximum of "
                + settings.MaxRelBottomLevelChange.ToString() + "m/m or " + settings.MaxAbsStageLevelChange + "m");
            OrphanedStageWarning = new CheckWarning(4, "Orphaned stage", "Orphaned stage: max 2 cells, surrounded by more cells with another stage");
            warningLegend = new IDFLegend("Legend for RIV-spatial check");
            warningLegend.AddClass(new ValueLegendClass(0, "No warnings found", Color.White));
            warningLegend.AddClass(UnexpectedStageLevelChangeWarning.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(UnexpectedBottomLevelChangeWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddClass(OrphanedStageWarning.CreateLegendValueClass(Color.Violet, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking RIV-package spatially ...");
                settings.LogSettings(log, 1);
                RunRIVSpatialCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        protected virtual void RunRIVSpatialCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Retrieve RIV-package
            IDFPackage rivPackage = (IDFPackage)model.GetPackage(RIVPackage.DefaultKey);
            if ((rivPackage == null) || !rivPackage.IsActive)
            {
                log.AddWarning(this.Name, model.Runfilename, "RIV-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (rivPackage.GetEntryCount(kper) > 0)
                {
                    if (model.NPER > 1)
                    {
                        log.AddInfo("Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + " ...", 1);
                    }
                    else
                    {
                        log.AddInfo("Checking stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + " ...", 1);
                    }

                    // Process all entries
                    for (int entryIdx = 0; entryIdx < rivPackage.GetEntryCount(kper); entryIdx++)
                    {
                        log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                        // Retrieve IDF files for current layer
                        IDFFile rivStageIDFFile = rivPackage.GetIDFFile(entryIdx, RIVPackage.StagePartIdx, kper);
                        IDFFile rivBottomIDFFile = rivPackage.GetIDFFile(entryIdx, RIVPackage.BottomPartIdx, kper);

                        if ((rivStageIDFFile == null) || (rivBottomIDFFile == null))
                        {
                            log.AddError("Stage or bottom RIV-files are missing, RIV-check is skipped");
                        }
                        else
                        {
                            // Prevent checking the same fileset twice
                            List<IDFFile> checkFileList = new List<IDFFile>() { rivStageIDFFile, rivBottomIDFFile };
                            if (rivPackage.IsFileListPresent(checkFileList, resultHandler.MinKPER, kper))
                            {
                                log.AddInfo("files have been checked already", 2);
                            }
                            else
                            {
                                // Create warning IDFfiles for current layer
                                IDFCellIterator idfCellIterator = new IDFCellIterator(checkFileList);
                                CheckWarningLayer rivWarningLayer = CreateWarningLayer(resultHandler, rivPackage, "SYS" + (entryIdx + 1), kper, entryIdx + 1, rivStageIDFFile.XCellsize, warningLegend);
                                rivWarningLayer.Id2 = Abbreviation;
                                rivWarningLayer.Description = rivWarningLayer.CreateLayerDescription("RIV-spatial", entryIdx + 1);
                                rivWarningLayer.ProcessDescription = this.Description;
                                rivWarningLayer.AddSourceFiles(checkFileList);

                                try
                                {
                                    log.AddInfo("Checking RIV-stage spatially ...", 2);
                                    // CheckMaxChange(resultHandler, rivStageIDFFile, settings, settings.MaxRelStageLevelChange, settings.MaxAbsStageLevelChange, 
                                    //   settings.MinStageChangeCellCount, settings.MaxStageChangeCellCount, rivWarningLayer, UnexpectedStageLevelChangeWarning, log, 1);
                                    CheckOrphanage(resultHandler, rivStageIDFFile, settings, rivWarningLayer, OrphanedStageWarning, log, 1, settings.CellDistance,
                                        settings.MaxRecursiveLevel, settings.MaxMainConnectionCount, settings.MinMostOccurringCount, settings.MaxOtherValueCount, settings.MinDifference, 5);

                                    log.AddInfo("Checking RIV-bottom spatially ...", 2);
                                    // CheckMaxChange(resultHandler, rivBottomIDFFile, settings, settings.MaxRelBottomLevelChange, settings.MaxBottomChangeCellCount, 
                                    //   settings.MinBottomChangeCellCount, settings.MaxBottomChangeCellCount, rivWarningLayer, UnexpectedBottomLevelChangeWarning, log, 1);
                                }
                                catch (Exception ex)
                                {
                                    log.AddInfo("\r\n" + "Unexpected error: " + ExceptionHandler.GetExceptionChainString(ex));
                                    log.AddInfo(ExceptionHandler.GetStacktraceString(ex, true, 1));
                                }

                                // Write warningfiles and add files to error handler
                                if (rivWarningLayer.HasResults())
                                {
                                    rivWarningLayer.CompressLegend(CombinedResultLabel);
                                    rivWarningLayer.WriteResultFile(log);
                                    //if (surfacelevelIDFFile != null)
                                    //{
                                    //    resultHandler.AddExtraMapFile(surfacelevelIDFFile);
                                    //}
                                    resultHandler.AddExtraMapFile(rivStageIDFFile);
                                    resultHandler.AddExtraMapFile(rivBottomIDFFile);
                                }

                                rivWarningLayer.ReleaseMemory(true);
                            }
                        }
                        rivPackage.ReleaseMemory(true);
                    }
                    rivPackage.ReleaseMemory(true);
                }
            }
        }

        /// <summary>
        /// An orphan cell has at most one neighbour with the same value, but is surrounded by cells with another value
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="checkedIDFFile"></param>
        /// <param name="settings"></param>
        /// <param name="warningLayer"></param>
        /// <param name="warning"></param>
        /// <param name="log"></param>
        /// <param name="p"></param>
        private void CheckOrphanage(CheckResultHandler resultHandler, IDFFile checkedIDFFile, CheckSettings settings, CheckWarningLayer warningLayer, CheckWarning warning, Log log, int logIndentLevel, string cellDistanceSetting, string maxRecursiveLevelSetting, string maxMainConnectionCountSetting, string minMostOccurringCountSetting, string maxOtherValueCountSetting, string maxDifferenceSetting, int precision)
        {
            // Retrieve setting-values
            IDFFile cellDistanceIDFFile = settings.GetIDFFile(cellDistanceSetting, log, 1);
            IDFFile maxRecursiveLevelIDFFile = settings.GetIDFFile(maxRecursiveLevelSetting, log, 1);
            IDFFile maxMainConnectionCountIDFFile = settings.GetIDFFile(maxMainConnectionCountSetting, log, 1);
            IDFFile minMostOccurringCountIDFFile = settings.GetIDFFile(minMostOccurringCountSetting, log, 1);
            IDFFile maxOtherValueCountIDFFile = settings.GetIDFFile(maxOtherValueCountSetting, log, 1);
            IDFFile maxDifferenceIDFFile = settings.GetIDFFile(maxDifferenceSetting, log, 1);

            IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
            //idfCellIterator.AddIDFFile(surfacelevelIDFFile);
            idfCellIterator.AddIDFFile(checkedIDFFile);
            idfCellIterator.AddIDFFile(cellDistanceIDFFile);
            idfCellIterator.AddIDFFile(maxRecursiveLevelIDFFile);
            idfCellIterator.AddIDFFile(maxMainConnectionCountIDFFile);
            idfCellIterator.AddIDFFile(minMostOccurringCountIDFFile);
            idfCellIterator.AddIDFFile(maxOtherValueCountIDFFile);
            idfCellIterator.AddIDFFile(maxDifferenceIDFFile);

            // Iterate through cells
            idfCellIterator.Reset();
            while (idfCellIterator.IsInsideExtent())
            {
                //float surfacelevelValue = idfCellIterator.GetCellValue(s
                int cellDistance = (int)idfCellIterator.GetCellValue(cellDistanceIDFFile);
                int maxRecursiveLevel = (int)idfCellIterator.GetCellValue(maxRecursiveLevelIDFFile);
                int maxMainConnectionCount = (int)idfCellIterator.GetCellValue(maxMainConnectionCountIDFFile);
                int minMostOccurringCount = (int)idfCellIterator.GetCellValue(minMostOccurringCountIDFFile);
                int maxOtherValueCount = (int)idfCellIterator.GetCellValue(maxOtherValueCountIDFFile);
                float maxDifference = idfCellIterator.GetCellValue(maxDifferenceIDFFile);
                int neighbourCellCount = (2 * cellDistance + 1) * (2 * cellDistance + 1) - 1;
                float cellValue = idfCellIterator.GetCellValue(checkedIDFFile);
                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;

                if (!cellValue.Equals(float.NaN) && !cellValue.Equals(checkedIDFFile.NoDataValue))
                {
                    if (IsOrphanCell(idfCellIterator, checkedIDFFile, x, y, cellDistance, maxRecursiveLevel, maxMainConnectionCount, minMostOccurringCount, maxOtherValueCount, Math.Max(resultHandler.LevelErrorMargin, maxDifference), precision))
                    {
                        resultHandler.AddCheckResult(warningLayer, x, y, warning);
                    }
                }

                idfCellIterator.MoveNext();
            }
        }

        /// <summary>
        /// Checks if given cell is an orphan cell or part of a larger set of cells with the same value
        /// An orphan cell has at most one neighbour with the same value, but is surrounded by cells with another value
        /// </summary>
        /// <param name="idfCellIterator"></param>
        /// <param name="checkedIDFFile"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cellDistance"></param>
        /// <param name="valueMargin"></param>
        /// <param name="isRecursive"></param>
        /// <param name="maxRecursiveLevel"></param>
        /// <returns></returns>
        private bool IsOrphanCell(IDFCellIterator idfCellIterator, IDFFile checkedIDFFile, float x, float y, int cellDistance, int maxRecursiveLevel, int maxMainConnectionCount, int minMostOccurringCount, int maxOtherValueCount, float valueMargin, int precision)
        {
            int otherCellValueCount = 0;
            int rowIdx = checkedIDFFile.GetRowIdx(y);
            int colIdx = checkedIDFFile.GetColIdx(x);
            float cellValue = (float)Math.Round(checkedIDFFile.values[rowIdx][colIdx], precision);

            // Get direct neighbours in local 3x3-grid
            float[][] neighbourCellValues = idfCellIterator.GetCellValues(checkedIDFFile, cellDistance, precision);

            // Retrieve most occurring value for direct neighbours
            float mostOccurringValue = IDFCellIterator.GetMostOccurringValue(neighbourCellValues, new float[] { float.NaN, checkedIDFFile.NoDataValue, cellValue });
            IDFCellIterator.GetMinMaxValue(neighbourCellValues, new float[] { float.NaN, checkedIDFFile.NoDataValue, cellValue }, out float minValue, out float maxValue);

            // If difference between this cell-value and the most occurring value is less then the defined errorlevel, do not report it as an orphan cell
            if (Math.Abs(cellValue - mostOccurringValue) < valueMargin)
            {
                return false;
            }

            // If cell-value is between min and max neighbouring values, assume values are not constant and a single intermediate value is valid
            if ((cellValue > minValue) && (cellValue < maxValue))
            {
                return false;
            }

            // Check how many connected-neighbours starting from the local 3x3-grid have this most occurring value
            List<IDFCell> mainCellConnections = new List<IDFCell>();
            List<IDFCell> mostOccurringValueConnections = new List<IDFCell>();
            List<float> otherValues = new List<float>();
            if (!cellValue.Equals(mostOccurringValue) && !mostOccurringValue.Equals(float.NaN))
            {
                for (int subRowIdx = 0; subRowIdx < neighbourCellValues.Length; subRowIdx++)
                {
                    for (int subColIdx = 0; subColIdx < neighbourCellValues.Length; subColIdx++)
                    {
                        float neighbourCellValue = neighbourCellValues[subRowIdx][subColIdx];
                        if (!neighbourCellValue.Equals(float.NaN) && !neighbourCellValue.Equals(checkedIDFFile.NoDataValue))
                        {
                            if (neighbourCellValue.Equals(mostOccurringValue) || neighbourCellValue.Equals(cellValue))
                            {
                                int neighbourRowIdx = rowIdx + subRowIdx - 1;
                                int neighbourColIdx = colIdx + subColIdx - 1;
                                IDFCell cell = new IDFCell(neighbourColIdx, neighbourRowIdx);

                                // If this cell has the most occurring value store it as part of the connected cells
                                if (neighbourCellValue.Equals(mostOccurringValue))
                                {
                                    if (!mostOccurringValueConnections.Contains(cell))
                                    {
                                        mostOccurringValueConnections.Add(cell);

                                        // Extend most occurring-value-connections
                                        ExtendConnectedCells(checkedIDFFile, neighbourRowIdx, neighbourColIdx, cellDistance, maxRecursiveLevel - 1, mostOccurringValue, precision, ref mostOccurringValueConnections);
                                    }
                                }
                                else
                                {
                                    if (!mainCellConnections.Contains(cell))
                                    {
                                        mainCellConnections.Add(cell);
                                    }

                                    // Extend maincell-value-connections
                                    ExtendConnectedCells(checkedIDFFile, neighbourRowIdx, neighbourColIdx, cellDistance, maxRecursiveLevel - 1, cellValue, precision, ref mainCellConnections);
                                }
                            }
                            else
                            {
                                if (!otherValues.Contains(neighbourCellValue))
                                {
                                    otherValues.Add(neighbourCellValue);
                                    otherCellValueCount++;
                                }
                            }
                        }
                        else
                        {
                            // ignore NoData-values
                        }
                    }
                }
            }

            return (mainCellConnections.Count <= maxMainConnectionCount) && (mostOccurringValueConnections.Count >= minMostOccurringCount) && (otherCellValueCount <= maxOtherValueCount);
        }

        private void ExtendConnectedCells(IDFFile checkedIDFFile, int rowIdx, int colIdx, int cellDistance, int maxRecursiveLevel, float searchValue, int precision, ref List<IDFCell> foundConnectedCells)
        {
            // Get direct neighbours in local 3x3-grid
            float[][] neighbourCellValues = checkedIDFFile.GetCellValues(rowIdx, colIdx, cellDistance, precision);

            for (int subRowIdx = 0; subRowIdx < neighbourCellValues.Length; subRowIdx++)
            {
                for (int subColIdx = 0; subColIdx < neighbourCellValues.Length; subColIdx++)
                {
                    float neighbourCellValue = neighbourCellValues[subRowIdx][subColIdx];
                    if (neighbourCellValue.Equals(searchValue))
                    {
                        int neighbourRowIdx = rowIdx + subRowIdx - 1;
                        int neighbourColIdx = colIdx + subColIdx - 1;
                        IDFCell neighbourCell = new IDFCell(neighbourColIdx, neighbourRowIdx);
                        if (!foundConnectedCells.Contains(neighbourCell))
                        {
                            foundConnectedCells.Add(neighbourCell);
                            if (maxRecursiveLevel > 0)
                            {
                                ExtendConnectedCells(checkedIDFFile, neighbourColIdx, neighbourRowIdx, cellDistance, maxRecursiveLevel - 1, searchValue, precision, ref foundConnectedCells);
                            }
                        }
                    }
                }
            }
        }
    }
}
