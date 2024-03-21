// IDFresample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFresample.
// 
// IDFresample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFresample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFresample. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.GIS.Utilities;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFresample
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
            ToolPurpose = "SIF-tool for resampling values in IDF-files";
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

            // Create output path if not yet existing
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            // Read zone IDF-file if specified
            IDFFile zoneIDFFile = null;
            List<float> uniqueZoneIDFValues = null;
            if (settings.ZoneFilename != null)
            {
                Log.AddInfo("Reading zone IDF-file ...");
                zoneIDFFile = IDFFile.ReadFile(settings.ZoneFilename);
                uniqueZoneIDFValues = uniqueZoneIDFValues = zoneIDFFile.RetrieveUniqueValues();
                if (uniqueZoneIDFValues.Count < 10)
                {
                    Log.AddInfo(uniqueZoneIDFValues.Count.ToString() + " unique values found in zone IDF-file: " + CommonUtils.ToString<float>(uniqueZoneIDFValues), 1);
                }
                else
                {
                    Log.AddInfo(uniqueZoneIDFValues.Count.ToString() + " unique values found in zone IDF-file: " + CommonUtils.ToString<float>(uniqueZoneIDFValues.GetRange(0, 10)) + ",...", 1);
                }
            }
            else
            {
                // Use full extent of each value IDF-file
            }

            Log.AddInfo("Processing input value files ...");
            int fileCount = 0;
            string[] valueFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            foreach (string valueFilename in valueFilenames)
            {
                Log.AddInfo("Processing file " + Path.GetFileName(valueFilename) + " ...", 1);
                ProcessValueIDFFile(valueFilename, zoneIDFFile, uniqueZoneIDFValues, settings, Log, 2);
                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Resample specified value IDF-file within zone(s) of specified zone IDF-file, using nearest neighbor values
        /// Resampled file will have extent of zone IDF-file and is written to output path as specified by settings object.
        /// Note: The result will only have non-NoData-cells for zone cells with a non-NoData-value.
        /// </summary>
        /// <param name="valueIDFFilename"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="uniqueZoneIDFValues"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ProcessValueIDFFile(string valueIDFFilename, IDFFile zoneIDFFile, List<float> uniqueZoneIDFValues, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            Log.AddInfo("reading value IDF-file ...", logIndentLevel);
            IDFFile valueIDFFile = IDFFile.ReadFile(valueIDFFilename, true);

            if (zoneIDFFile == null)
            {
                // Create a zone IDF-file with values 1 depending on resample method
                zoneIDFFile = valueIDFFile.CopyIDF("zone.IDF");
                zoneIDFFile.ResetValues();
                switch (settings.ResampleMethod)
                {
                    case ResampleMethod.NearestNeighbor:
                    case ResampleMethod.IDW:
                        // Define zone value 1 for all value cells with a NoData-value and extend region 1 cell
                        zoneIDFFile.ReplaceValues(valueIDFFile, valueIDFFile.NoDataValue, 1f);
                        zoneIDFFile = Grow(zoneIDFFile, 1f, !settings.SkipDiagonalProcessing);
                        break;
                    default:
                        // Define zone value 1 for all value cells with a non-NoData-value
                        zoneIDFFile.ReplaceValues(valueIDFFile, 1f);
                        break;
                }
            }

            // Check for equal cellsize of value and zone IDF-file
            if (!valueIDFFile.XCellsize.Equals(zoneIDFFile.XCellsize) && !valueIDFFile.YCellsize.Equals(zoneIDFFile.YCellsize))
            {
                throw new ToolException("Cellsizes are different for zone IDF-file (" + zoneIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFile.YCellsize.ToString(EnglishCultureInfo)
                    + ") and value IDF-file (" + valueIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + valueIDFFile.YCellsize.ToString(EnglishCultureInfo) + ")");
            }

            // Now, after first check has been done, load actual IDF values
            valueIDFFile.EnsureLoadedValues();

            Log.AddInfo("initializing ...", logIndentLevel);

            if (!valueIDFFile.Extent.Equals(zoneIDFFile.Extent))
            {
                if (!zoneIDFFile.Extent.Intersects(valueIDFFile.Extent))
                {
                    throw new ToolException("No overlap between extents of zone IDF-file " + zoneIDFFile.Extent.ToString() + " and value IDF-file " + valueIDFFile.Extent.ToString());
                }

                // Ensure value IDF-file has same extent as zone IDF-file
                valueIDFFile = valueIDFFile.ClipIDF(zoneIDFFile.Extent);
                valueIDFFile = valueIDFFile.EnlargeIDF(zoneIDFFile.Extent);
            }

            // Create empty result file for final result (which is equal to extent of zone IDF-file)
            string resultFilename = Path.Combine(settings.OutputPath, settings.OutputFilename ?? FileUtils.AddFilePostFix(Path.GetFileName(valueIDFFile.Filename), "_resampled"));
            IDFFile resultIDFFile = zoneIDFFile.CopyIDF(resultFilename, false);
            resultIDFFile.DeclareValuesMemory();
            resultIDFFile.ResetValues();

            Log.AddInfo("resampling ...", logIndentLevel);
            Resample(valueIDFFile, zoneIDFFile, resultIDFFile, settings, log, logIndentLevel);

            Log.AddInfo("writing resampled value IDF-file " + Path.GetFileName(resultIDFFile.Filename), 1);
            resultIDFFile.WriteFile();
        }

        /// <summary>
        /// Resample specified value IDF-file within zone(s) of specified zone IDF-file, using specified resample method
        /// Resampled file will have extent of zone IDF-file and is written to output path as specified by settings object.
        /// Note: The result will only have non-NoData-cells for zone cells with a non-NoData-value.
        /// </summary>
        /// <param name="valueIDFFile"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="resultIDFFile"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void Resample(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            switch (settings.ResampleMethod)
            {
                case ResampleMethod.NearestNeighbor:
                case ResampleMethod.IDW:
                    ResampleRegions(valueIDFFile, zoneIDFFile, resultIDFFile, settings, log, logIndentLevel);
                    break;
                case ResampleMethod.MinimumValue:
                case ResampleMethod.MaximumValue:
                case ResampleMethod.MeanValue:
                case ResampleMethod.PercentileValue:
                    ResampleZones(valueIDFFile, zoneIDFFile, resultIDFFile, settings, log, logIndentLevel);
                    break;
                default:
                    throw new Exception("Undefined resample method: " + settings.ResampleMethod);
            }
        }

        /// <summary>
        /// Resample specified value IDF-file within zone(s) of specified zone IDF-file, using nearest neighbor values
        /// Resampled file will have extent of zone IDF-file and is written to output path as specified by settings object.
        /// Note: The result will only have non-NoData-cells for zone cells with a non-NoData-value.
        /// </summary>
        /// <param name="valueIDFFile"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="resultIDFFile"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ResampleRegions(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            bool isDebugMode = (settings.DebugMode == DebugMode.All) || (settings.DebugMode == DebugMode.Global);

            // Start with input values for all non-NoData-cells in zoneIDFFile
            resultIDFFile.ReplaceValues(zoneIDFFile, valueIDFFile);

            // The global algorithm is as follows: 
            // - visit all cells and search for an unprocessed zone cell with a NoData-value
            // - retrieve local zone (connected cells with the same zone value)
            // - resample NoData-cells within local zone using non-NoData-value(s) in zone
            // - register cells from local zone as processed cells

            // Currently not used, do not include values outside boundary cells in search of local zone
            bool includeValuesOutsideBoundary = false;

            // Create IDF-file object that keeps track of globally processed cells (in full zone IDF-file). Non-NoData-cells do not need to be processed, only NoData-cells are resampled, so leave non-noData cells (as processed)
            IDFFile processedCellsIDFFile = valueIDFFile.CopyIDF(resultIDFFile.Filename, true);

            int subZoneNr = 1;
            string subZoneId = string.Empty;
            Dictionary<float, int> subZoneValueDictionary = new Dictionary<float, int>();
            for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                {
                    float value = processedCellsIDFFile.values[rowIdx][colIdx];
                    float zoneValue = zoneIDFFile.values[rowIdx][colIdx];

                    // Search for an unprocessed zonecell with a noData-value
                    if (value.Equals(processedCellsIDFFile.NoDataValue) && !zoneValue.Equals(zoneIDFFile.NoDataValue))
                    {
                        subZoneNr = GetSubZoneNumber(subZoneValueDictionary, zoneValue);

                        // Empty cell found within a zone. Retrieve region with other connected NoData-cells within this zone
                        IDFCell idfCell = new IDFCell(rowIdx, colIdx);

                        // Retrieve local zone
                        log.AddInfo("retrieving and resampling local zone with zone value " + zoneValue + "." + subZoneNr + " at cell " + idfCell.ToString() + " ... ", logIndentLevel, false);
                        //IDFFile localValueIDFFile = RetrieveLocalZoneValues(valueIDFFile, zoneIDFFile, idfCell, zoneValue, valueIDFFile.NoDataValue, float.NaN, includeValuesOutsideBoundary, settings);
                        IDFFile localValueIDFFile = RetrieveLocalZoneValues(zoneIDFFile, zoneIDFFile, idfCell, zoneValue, zoneValue, zoneValue, includeValuesOutsideBoundary, settings);
                        IDFFile localZoneIDFFile = localValueIDFFile.CopyIDF(localValueIDFFile.Filename);
                        localValueIDFFile.ReplaceValues(zoneValue, valueIDFFile.ClipIDF(localValueIDFFile.Extent));
                        log.AddInfo(localValueIDFFile.RetrieveElementCount() + " cells found");

                        // Create local zone IDF-file that corresponds to values in local valueIDF-file
                        //IDFFile localZoneIDFFile = localValueIDFFile.CopyIDF("Localzone" + zoneValue);
                        //localZoneIDFFile.ReplaceValues(localZoneIDFFile, zoneValue);
                        //localZoneIDFFile.ReplaceValues(float.NaN, zoneValue);

                        // Create empty local result IDF-file that corresponds to local valueIDF-file (which contains 0 values instead of NoData)
                        IDFFile localResultIDFFile = localValueIDFFile.CopyIDF("LocalResultIDFFile.IDF");
                        localResultIDFFile.ResetValues();

                        //// Now replace NaN-values in valueIDF-file to NoData-values
                        //localValueIDFFile.ReplaceValues(float.NaN, localValueIDFFile.NoDataValue);

                        if (isDebugMode)
                        {
                            if (settings.DebugSubZoneId != null)
                            {
                                subZoneId = zoneValue.ToString() + "." + subZoneNr;
                                if (settings.DebugSubZoneId.Equals(subZoneId))
                                {
                                    isDebugMode = true;
                                }
                            }

                            localValueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalValues" + zoneValue + "." + subZoneNr + ".IDF"));

                            localZoneIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalZone" + zoneValue + "." + subZoneNr + ".IDF"));
                        }

                        // Start resampling values in current region
                        switch (settings.ResampleMethod)
                        {
                            case ResampleMethod.NearestNeighbor:
                                ResampleNN(localValueIDFFile, localZoneIDFFile, zoneValue, localResultIDFFile, settings, log, logIndentLevel);
                                break;
                            case ResampleMethod.IDW:
                                ResampleIDW(localValueIDFFile, localZoneIDFFile, zoneValue, localResultIDFFile, settings, log, logIndentLevel);
                                break;
                            default:
                                throw new Exception("Invalid method for regional resampling: " + settings.ResampleMethod);
                        }

                        if (isDebugMode)
                        {
                            localResultIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalResult" + zoneValue + "." + subZoneNr + ".IDF"));
                        }

                        // Merge zone results with total results IDF-file   
                        resultIDFFile.ReplaceValues(localZoneIDFFile, localResultIDFFile);

                        // Merge locally processed cells with globally processed cells
                        processedCellsIDFFile.ReplaceValues(localZoneIDFFile, localZoneIDFFile);

                        if (isDebugMode)
                        {
                            processedCellsIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ProcessedCells.IDF"));
                            resultIDFFile.WriteFile();
                        }
                    }
                }
            }
        }

        private void ResampleIDW(IDFFile valueIDFFile, IDFFile zoneIDFFile, float zoneValue, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            bool isDebugMode = (settings.DebugMode == DebugMode.All) || (settings.DebugMode == DebugMode.Local);

            // Check for equal cellsize of value and zone IDF-file
            if (!valueIDFFile.XCellsize.Equals(zoneIDFFile.XCellsize) && !valueIDFFile.YCellsize.Equals(zoneIDFFile.YCellsize))
            {
                throw new ToolException("Cellsizes are different for zone IDF-file (" + zoneIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFile.YCellsize.ToString(EnglishCultureInfo)
                    + ") and value IDF-file (" + zoneIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFile.YCellsize.ToString(EnglishCultureInfo) + ")");
            }
            if (!valueIDFFile.Extent.Equals(zoneIDFFile.Extent))
            {
                if (!zoneIDFFile.Extent.Intersects(valueIDFFile.Extent))
                {
                    throw new ToolException("No overlap between extents of zone IDF-file " + zoneIDFFile.Extent.ToString() + " and value IDF-file " + valueIDFFile.Extent.ToString());
                }
            }

            // 1. Select cells with and without a value inside current zone
            // 2. Interpolate NoData-cells inside zone with IDW from known values
            resultIDFFile.ReplaceValues(zoneIDFFile, valueIDFFile);

            int maxRowIdx = resultIDFFile.NRows - 1;
            int maxColIdx = resultIDFFile.NCols - 1;
            float zoneNoDataValue = zoneIDFFile.NoDataValue;
            float resultNoDataValue = resultIDFFile.NoDataValue;

            // First retrieve all cells in this zone with a non-NoData-value
            List<IDFCell> sourceCells = new List<IDFCell>();
            List<Point> sourcePoints = new List<Point>();
            List<float> sourceValues = new List<float>();
            List<IDFCell> targetCells = new List<IDFCell>();
            List<Point> targetPoints = new List<Point>();
            for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                {
                    float zoneIDFValue = zoneIDFFile.values[rowIdx][colIdx];
                    float valueIDFValue = resultIDFFile.values[rowIdx][colIdx];
                    if (zoneIDFValue.Equals(zoneValue))
                    {
                        // When cell in current resultIDFFile has a NoData-value, store it to interpolate from in next loop.
                        if (!valueIDFValue.Equals(resultNoDataValue))
                        {
                            sourceCells.Add(new IDFCell(rowIdx, colIdx, valueIDFValue));
                            sourcePoints.Add(new FloatPoint(zoneIDFFile.GetX(colIdx), zoneIDFFile.GetY(rowIdx)));
                            sourceValues.Add(valueIDFValue);
                        }
                        else
                        {
                            targetCells.Add(new IDFCell(rowIdx, colIdx));
                            targetPoints.Add(new FloatPoint(zoneIDFFile.GetX(colIdx), zoneIDFFile.GetY(rowIdx)));
                        }
                    }
                }
            }

            // Now, loop through all cells in this zone with a NoData-value and interpolate a new value using IDW
            for (int targetCellIdx = 0; targetCellIdx < targetCells.Count; targetCellIdx++)
            {
                IDFCell targetIDFCell = targetCells[targetCellIdx];
                Point targetPoint = targetPoints[targetCellIdx];
                float interpolatedValue = targetPoint.InterpolateIDW(sourcePoints, sourceValues, settings.ResampleIDWPower, settings.ResampleIDWSmoothingFactor, settings.ResampleIDWDistance);
                resultIDFFile.values[targetIDFCell.RowIdx][targetIDFCell.ColIdx] = interpolatedValue;
            }
        }


        /// <summary>
        /// Resample specified value IDF-file within zone(s) of specified zone IDF-file, using specified resample method
        /// Resampled file will have extent of zone IDF-file and is written to output path as specified by settings object.
        /// Note: The result will only have non-NoData-cells for zone cells with a non-NoData-value.
        /// </summary>
        /// <param name="valueIDFFile"></param>;;;
        /// <param name="zoneIDFFile"></param>
        /// <param name="resultIDFFile"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected void ResampleZones(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            if (settings.ResampleMethod == ResampleMethod.PercentileValue)
            {
                if ((settings.ResamplePercentile < 0) || (settings.ResamplePercentile < 0))
                {
                    throw new ToolException("Invalid percentile value for percentile resample method: " + settings.ResamplePercentile);
                }
            }

            if (settings.ResampleStatDistance == 0)
            {
                ResampleFullZones(valueIDFFile, zoneIDFFile, resultIDFFile, settings, log, logIndentLevel);
            }
            else
            {
                ResampleLocalZones(valueIDFFile, zoneIDFFile, resultIDFFile, settings, log, logIndentLevel);
            }
        }

        private void ResampleLocalZones(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            // The global algorithm is as follows: 
            // - create local grid to keep track of current block of processed values
            // - visit all cells and determine zone
            // - visit neighbours and calculate statistic over cells in same zone
            int percentile = settings.ResamplePercentile;

            int dist = (int) settings.ResampleStatDistance;
            int maxRowIdx = zoneIDFFile.NRows - 1;
            int maxColIdx = zoneIDFFile.NCols - 1;
            for (int rowIdx = 0; rowIdx <= maxRowIdx; rowIdx++)
            {
                for (int colIdx = 0; colIdx <= maxColIdx; colIdx++)
                {
                    float zoneValue = zoneIDFFile.values[rowIdx][colIdx];
                    if (!zoneValue.Equals(zoneIDFFile.NoDataValue))
                    {
                        int minSubRowIdx = (rowIdx > dist) ? rowIdx - dist : 0;
                        int minSubColIdx = (colIdx > dist) ? colIdx - dist : 0;
                        int maxSubRowIdx = rowIdx + dist; 
                        int maxSubColIdx = colIdx + dist;
                        if (maxSubRowIdx > maxRowIdx)
                        {
                            maxSubRowIdx = maxRowIdx;
                        }
                        if (maxSubColIdx > maxColIdx)
                        {
                            maxSubColIdx = maxColIdx;
                        }

                        SortedSet<float> values = new SortedSet<float>();
                        for (int subRowIdx = minSubRowIdx; subRowIdx <= maxSubRowIdx; subRowIdx++)
                        {
                            for (int subColIdx = minSubColIdx; subColIdx <= maxSubColIdx; subColIdx++)
                            {
                                float subZoneValue = zoneIDFFile.values[subRowIdx][subColIdx];
                                if (subZoneValue.Equals(zoneValue))
                                {
                                    float value = valueIDFFile.values[subRowIdx][subColIdx];
                                    if (!value.Equals(valueIDFFile.NoDataValue))
                                    {
                                        values.Add(value);
                                    }
                                }
                            }
                        }

                        if (values.Count > 0)
                        {
                            float zoneStatistic;
                            switch (settings.ResampleMethod)
                            {
                                case ResampleMethod.MinimumValue:
                                    zoneStatistic = values.Min;
                                    break;
                                case ResampleMethod.MaximumValue:
                                    zoneStatistic = values.Max;
                                    break;
                                case ResampleMethod.MeanValue:
                                    zoneStatistic = values.Average();
                                    break;
                                case ResampleMethod.PercentileValue:
                                    zoneStatistic = values.ElementAt(percentile * values.Count / 100);
                                    break;
                                default:
                                    throw new Exception("Undefined resample method for ResampleZones(): " + settings.ResampleMethod.ToString());
                            }

                            resultIDFFile.values[rowIdx][colIdx] = zoneStatistic;
                        }
                    }
                }
            }
        }

        protected void ResampleFullZones(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            bool isDebugMode = (settings.DebugMode == DebugMode.All) || (settings.DebugMode == DebugMode.Global);

            // Create dictionary that keeps track of processed zones and non-NoData-values inside each zone
            SortedDictionary<float, List<float>> zoneValuesDictionary = new SortedDictionary<float, List<float>>();

            // The global algorithm is as follows: 
            // - visit all cells and keep track of values per zone in a dictionary
            // - calculate derived value per zone over values inside each zone
            // - visit all cells and assign derived value to cells depending one zone

            Dictionary<float, int> subZoneValueDictionary = new Dictionary<float, int>();
            for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                {
                    float zoneValue = zoneIDFFile.values[rowIdx][colIdx];
                    if (!zoneValue.Equals(zoneIDFFile.NoDataValue))
                    {
                        float value = valueIDFFile.values[rowIdx][colIdx];
                        if (!value.Equals(valueIDFFile.NoDataValue))
                        {
                            if (!zoneValuesDictionary.ContainsKey(zoneValue))
                            {
                                zoneValuesDictionary.Add(zoneValue, new List<float>());
                            }
                            zoneValuesDictionary[zoneValue].Add(value);
                        }
                    }
                }
            }

            // Calculate statistics per zone
            Statistics.Statistics zoneStatistics = null;
            SortedDictionary<float, float> zoneStatisticDictionary = new SortedDictionary<float, float>();
            foreach (float zoneValue in zoneValuesDictionary.Keys)
            {
                zoneStatistics = new Statistics.Statistics(zoneValuesDictionary[zoneValue]);

                float zoneStatistic;
                switch (settings.ResampleMethod)
                {
                    case ResampleMethod.MinimumValue:
                        zoneStatistics.ComputeBasicStatistics();
                        zoneStatistic = zoneStatistics.Min;
                        break;
                    case ResampleMethod.MaximumValue:
                        zoneStatistics.ComputeBasicStatistics();
                        zoneStatistic = zoneStatistics.Max;
                        break;
                    case ResampleMethod.MeanValue:
                        zoneStatistics.ComputeBasicStatistics();
                        zoneStatistic = zoneStatistics.Mean;
                        break;
                    case ResampleMethod.PercentileValue:
                        zoneStatistics.ComputePercentiles();
                        zoneStatistic = zoneStatistics.Percentiles[settings.ResamplePercentile];
                        break;
                    default:
                        throw new Exception("Undefined resample method for ResampleZones(): " + settings.ResampleMethod.ToString());
                }

                zoneStatisticDictionary.Add(zoneValue, zoneStatistic);
            }

            for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                {
                    float zoneValue = zoneIDFFile.values[rowIdx][colIdx];
                    if (!zoneValue.Equals(zoneIDFFile.NoDataValue))
                    {
                        if (zoneValuesDictionary.ContainsKey(zoneValue))
                        {
                            resultIDFFile.values[rowIdx][colIdx] = zoneStatisticDictionary[zoneValue];
                        }
                        else
                        {
                            // log.AddWarning("Missing values for zone " + zoneValue, logIndentLevel);
                        }
                    }
                }
            }
        }

        protected int GetSubZoneNumber(Dictionary<float, int> subZoneValueDictionary, float zoneValue)
        {
            int subZoneNr;
            if (subZoneValueDictionary.ContainsKey(zoneValue))
            {
                subZoneValueDictionary[zoneValue]++;
                subZoneNr = subZoneValueDictionary[zoneValue];
            }
            else
            {
                subZoneNr = 1;
                subZoneValueDictionary.Add(zoneValue, 1);
            }

            return subZoneNr;
        }

        /// <summary>
        /// Resample values in current local zone using nearest neighbor method
        /// </summary>
        /// <param name="valueIDFFile"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="zoneValue"></param>
        /// <param name="resultIDFFile"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        private void ResampleNN(IDFFile valueIDFFile, IDFFile zoneIDFFile, float zoneValue, IDFFile resultIDFFile, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            bool isDebugMode = (settings.DebugMode == DebugMode.All) || (settings.DebugMode == DebugMode.Local);

            // Check for equal cellsize of value and zone IDF-file
            if (!valueIDFFile.XCellsize.Equals(zoneIDFFile.XCellsize) && !valueIDFFile.YCellsize.Equals(zoneIDFFile.YCellsize))
            {
                throw new ToolException("Cellsizes are different for zone IDF-file (" + zoneIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFile.YCellsize.ToString(EnglishCultureInfo)
                    + ") and value IDF-file (" + zoneIDFFile.XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFile.YCellsize.ToString(EnglishCultureInfo) + ")");
            }
            if (!valueIDFFile.Extent.Equals(zoneIDFFile.Extent))
            {
                if (!zoneIDFFile.Extent.Intersects(valueIDFFile.Extent))
                {
                    throw new ToolException("No overlap between extents of zone IDF-file " + zoneIDFFile.Extent.ToString() + " and value IDF-file " + valueIDFFile.Extent.ToString());
                }
            }

            // Use a grow approach to implment nearest neighbor resampling
            // - Copy known values from valuegrid inside current zone
            // - For neighboring cells of all currently known values, copy value from known cells. Use conflict method when surrounded by more than one known cell.
            resultIDFFile.ReplaceValues(zoneIDFFile, valueIDFFile);

            int maxRowIdx = resultIDFFile.NRows - 1;
            int maxColIdx = resultIDFFile.NCols - 1;
            float zoneNoDataValue = zoneIDFFile.NoDataValue;
            float resultNoDataValue = resultIDFFile.NoDataValue;

            // First loop: retrieve all cells in this zone with a NoData-value and one or more neighbours with a value
            Queue<IDFCell> neighbourQueue = new Queue<IDFCell>();
            HashSet<IDFCell> neighbourHashSet = new HashSet<IDFCell>();
            int processedCellCount = 0;
            for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                {
                    float zoneIDFValue = zoneIDFFile.values[rowIdx][colIdx];
                    float valueIDFValue = resultIDFFile.values[rowIdx][colIdx];
                    if (zoneIDFValue.Equals(zoneValue))
                    {
                        // When cell in current resultIDFFile has a NoData-value, try to resample.
                        if (valueIDFValue.Equals(resultNoDataValue))
                        {
                            int neighbourCount = RetrieveNeighbourCount(rowIdx, colIdx, resultIDFFile, resultNoDataValue, maxRowIdx, maxColIdx, settings.SkipDiagonalProcessing);

                            if (neighbourCount == 0)
                            {
                                // This is a cell without neighbours with a value (other than NoData), ignore for now
                            }
                            else
                            {
                                // This is a NoData-cell with one or more neighbours, so it is on the outside of the current NoData-zone. Store for processing in the next loop
                                IDFCell idfCell = new IDFCell(rowIdx, colIdx);
                                neighbourQueue.Enqueue(idfCell);
                                neighbourHashSet.Add(idfCell);
                            }

                            processedCellCount++;
                        }
                    }
                }
            }

            if (isDebugMode)
            {
                IDFFile resampleQueueIDFFile = IDFUtils.ConvertToIDF(neighbourHashSet, resultIDFFile, true);
                if (resampleQueueIDFFile != null)
                {
                    resampleQueueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ResampleQueue.IDF"));
                }
            }

            // Second loop: process all neighbouring cells with NoData-values. In each subloop, the cells at the outside of the current NoData-zone are processed.
            while (neighbourQueue.Count > 0)
            {
                // Start subloop with all NoData-cells that had a non-NoData-neighbour in the previous loop (so the cells along the edge of the current NoData-zone)
                int processedNeighboursInSubloop = neighbourQueue.Count;
                HashSet<IDFCell> tmpResultValueCells = new HashSet<IDFCell>();
                HashSet<IDFCell> tmpResultCells = new HashSet<IDFCell>();
                while (processedNeighboursInSubloop > 0)
                {
                    // Retrieve value from queue to check. Note: new cells will be added to the end of the queue, but they will not yet be processed in this subloop.
                    IDFCell idfCell = neighbourQueue.Dequeue();
                    neighbourHashSet.Remove(idfCell);
                    processedNeighboursInSubloop--;

                    ResampleNeighbourValues(idfCell.RowIdx, idfCell.ColIdx, resultIDFFile, resultNoDataValue, maxRowIdx, maxColIdx, out int neighbourCount, out float resampledValue, settings);

                    if (neighbourCount == 0)
                    {
                        // This is a cell without neighbours with a value (other than NoData), leave isolated for now
                    }
                    else
                    {
                        // This is a cell with one or more neighbours; write resampled value to result IDF-file
                        tmpResultValueCells.Add(new IDFCell(idfCell.RowIdx, idfCell.ColIdx, resampledValue));

                        // Also write without value to be able check possible new neighbours (that don't have a resampled value yet which and would not match an IDFCell with a value)
                        tmpResultCells.Add(new IDFCell(idfCell.RowIdx, idfCell.ColIdx));

                        if (isDebugMode)
                        {
                            HashSet<IDFCell> tmpHashSet = new HashSet<IDFCell>();
                            tmpHashSet.Add(idfCell);
                            IDFFile resampledCellIDFFile = IDFUtils.ConvertToIDF(tmpHashSet, resultIDFFile, true);
                            IDFFile tmpResultValueCellsIDFFile = IDFUtils.ConvertToIDF(tmpResultValueCells, resultIDFFile, false);
                            IDFFile tmpResultCellsIDFFile = IDFUtils.ConvertToIDF(tmpResultCells, resultIDFFile, true);
                            IDFFile resampleQueueIDFFile = IDFUtils.ConvertToIDF(neighbourHashSet, resultIDFFile, true);
                            try
                            {
                                resampledCellIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ResampledCell.IDF"));
                                tmpResultCellsIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "TmpResultCells.IDF"));
                                tmpResultValueCellsIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "TmpResultValueCells.IDF"));

                                if (resampleQueueIDFFile != null)
                                {
                                    resampleQueueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ResampleQueue.IDF"));
                                }
                            }
                            catch
                            {
                                log.AddWarning("Could not write debugging IDF-file", logIndentLevel);
                            }
                        }

                        // Add neighbours with NoData-values
                        if (idfCell.RowIdx < maxRowIdx)
                        {
                            CheckNoDataNeighbour(idfCell.RowIdx + 1, idfCell.ColIdx, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                        }
                        if (idfCell.ColIdx < maxColIdx)
                        {
                            CheckNoDataNeighbour(idfCell.RowIdx, idfCell.ColIdx + 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                        }
                        if (idfCell.RowIdx > 0)
                        {
                            CheckNoDataNeighbour(idfCell.RowIdx - 1, idfCell.ColIdx, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                        }
                        if (idfCell.ColIdx > 0)
                        {
                            CheckNoDataNeighbour(idfCell.RowIdx, idfCell.ColIdx - 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                        }
                        if (!settings.SkipDiagonalProcessing)
                        {
                            if ((idfCell.RowIdx > 0) && (idfCell.ColIdx > 0))
                            {
                                CheckNoDataNeighbour(idfCell.RowIdx - 1, idfCell.ColIdx - 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                            }
                            if ((idfCell.ColIdx > 0) && (idfCell.RowIdx < maxRowIdx))
                            {
                                CheckNoDataNeighbour(idfCell.RowIdx + 1, idfCell.ColIdx - 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                            }
                            if ((idfCell.RowIdx > 0) && (idfCell.ColIdx < maxColIdx))
                            {
                                CheckNoDataNeighbour(idfCell.RowIdx - 1, idfCell.ColIdx + 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                            }
                            if ((idfCell.RowIdx < maxRowIdx) && (idfCell.ColIdx < maxColIdx))
                            {
                                CheckNoDataNeighbour(idfCell.RowIdx + 1, idfCell.ColIdx + 1, resultIDFFile, resultNoDataValue, zoneIDFFile, zoneValue, neighbourQueue, neighbourHashSet, tmpResultCells);
                            }
                        }
                    }
                }

                // All cells from this round have been processed, now add resampled values to result IDF-file
                foreach (IDFCell idfCell in tmpResultValueCells)
                {
                    resultIDFFile.values[idfCell.RowIdx][idfCell.ColIdx] = idfCell.Value;
                }

                if (isDebugMode)
                {
                    resultIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ResampledValues.IDF"));

                    IDFFile resampleQueueIDFFile = IDFUtils.ConvertToIDF(neighbourHashSet, resultIDFFile, true);
                    if (resampleQueueIDFFile != null)
                    {
                        resampleQueueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "ResampleQueue.IDF"));
                    }
                }
            }
        }

        protected void CheckNoDataNeighbour(int rowIdx, int colIdx, IDFFile resultIDFFile, float resultNoDataValue, IDFFile zoneIDFFile, float zoneValue, Queue<IDFCell> neighbourQueue, HashSet<IDFCell> neighbourHashSet, HashSet<IDFCell> tmpResultCells)
        {
            float neighbourValue = resultIDFFile.values[rowIdx][colIdx];
            float neighbourZoneValue = zoneIDFFile.values[rowIdx][colIdx];
            if (neighbourValue.Equals(resultNoDataValue) && neighbourZoneValue.Equals(zoneValue))
            {
                IDFCell idfCell = new IDFCell(rowIdx, colIdx);
                if (!neighbourHashSet.Contains(idfCell) && !tmpResultCells.Contains(idfCell))
                {
                    neighbourQueue.Enqueue(idfCell);
                    neighbourHashSet.Add(idfCell);
                }
            }
        }

        protected int RetrieveNeighbourCount(int rowIdx, int colIdx, IDFFile resultIDFFile, float resultNoDataValue, int maxRowIdx, int maxColIdx, bool skipDiagonalProcessing = false)
        {
            int neighbourCount = 0;

            if (rowIdx < maxRowIdx)
            {
                float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx];
                if (!neighbourValue.Equals(resultNoDataValue))
                {
                    neighbourCount++;
                }
            }
            if (colIdx < maxColIdx)
            {
                float neighbourValue = resultIDFFile.values[rowIdx][colIdx + 1];
                if (!neighbourValue.Equals(resultNoDataValue))
                {
                    neighbourCount++;
                }
            }
            if (rowIdx > 0)
            {
                float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx];
                if (!neighbourValue.Equals(resultNoDataValue))
                {
                    neighbourCount++;
                }
            }
            if (colIdx > 0)
            {
                float neighbourValue = resultIDFFile.values[rowIdx][colIdx - 1];
                if (!neighbourValue.Equals(resultNoDataValue))
                {
                    neighbourCount++;
                }
            }
            if (!skipDiagonalProcessing)
            {
                if ((rowIdx > 0) && (colIdx > 0))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx - 1];
                    if (!neighbourValue.Equals(resultNoDataValue))
                    {
                        neighbourCount++;
                    }
                }
                if ((colIdx > 0) && (rowIdx < maxRowIdx))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx - 1];
                    if (!neighbourValue.Equals(resultNoDataValue))
                    {
                        neighbourCount++;
                    }
                }
                if ((rowIdx < maxRowIdx) && (colIdx < maxColIdx))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx + 1];
                    if (!neighbourValue.Equals(resultNoDataValue))
                    {
                        neighbourCount++;
                    }
                }
                if ((colIdx < maxColIdx) && (rowIdx > 0))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx + 1];
                    if (!neighbourValue.Equals(resultNoDataValue))
                    {
                        neighbourCount++;
                    }
                }
            }
            return neighbourCount;
        }

        protected void ResampleNeighbourValues(int rowIdx, int colIdx, IDFFile resultIDFFile, float resultNoDataValue, int maxRowIdx, int maxColIdx, out int neighbourCount, out float resampledValue, SIFToolSettings settings)
        {
            neighbourCount = 0;
            resampledValue = float.NaN;

            float valueSum = 0;
            float maxValue = float.MinValue;
            float minValue = float.MaxValue;
            int valueCount = 0;
            bool isUndefinedHarmonicAverage = false;

            if (rowIdx < maxRowIdx)
            {
                float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx];
                ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref neighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
            }
            if (colIdx < maxColIdx)
            {
                float neighbourValue = resultIDFFile.values[rowIdx][colIdx + 1];
                ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref neighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
            }
            if (rowIdx > 0)
            {
                float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx];
                ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref neighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
            }
            if (colIdx > 0)
            {
                float neighbourValue = resultIDFFile.values[rowIdx][colIdx - 1];
                ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref neighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
            }
            if (!settings.SkipDiagonalProcessing)
            {
                // Diagonal neighbours are counted seperately and not returned via neighCount which only counts direct neighbours
                int diagonalNeighbourCount = 0;

                if ((rowIdx > 0) && (colIdx > 0))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx - 1];
                    ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref diagonalNeighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
                }
                if ((colIdx > 0) && (rowIdx < maxRowIdx))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx - 1];
                    ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref diagonalNeighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
                }
                if ((rowIdx < maxRowIdx) && (colIdx < maxColIdx))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx + 1][colIdx + 1];
                    ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref diagonalNeighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
                }
                if ((colIdx < maxColIdx) && (rowIdx > 0))
                {
                    float neighbourValue = resultIDFFile.values[rowIdx - 1][colIdx + 1];
                    ResampleNeighbourValue(neighbourValue, resultNoDataValue, ref diagonalNeighbourCount, ref valueCount, ref valueSum, ref minValue, ref maxValue, ref isUndefinedHarmonicAverage, settings.ResampleNNConflictMethod);
                }

                neighbourCount += diagonalNeighbourCount;
            }

            if (valueCount > 0)
            {
                switch (settings.ResampleNNConflictMethod)
                {
                    case ConflictMethod.ArithmeticAverage:
                        resampledValue = valueSum / (float)valueCount;
                        break;
                    case ConflictMethod.HarmonicAverage:
                        if (isUndefinedHarmonicAverage)
                        {
                            resampledValue = 0;
                        }
                        else
                        {
                            resampledValue = ((float)valueCount) / valueSum;
                        }
                        break;
                    case ConflictMethod.MinimumValue:
                        resampledValue = minValue;
                        break;
                    case ConflictMethod.MaximumValue:
                        resampledValue = maxValue;
                        break;
                    default:
                        throw new Exception("Unknown conflict method: " + settings.ResampleNNConflictMethod.ToString());
                }
            }
        }

        protected void ResampleNeighbourValue(float neighbourValue, float noDataValue, ref int neighbourCount, ref int valueCount, ref float valueSum, ref float minValue, ref float maxValue, ref bool isUndefinedHarmonicAverage, ConflictMethod conflictMethod)
        {
            if (!neighbourValue.Equals(noDataValue))
            {
                neighbourCount++;
                switch (conflictMethod)
                {
                    case ConflictMethod.ArithmeticAverage:
                        valueSum += neighbourValue;
                        valueCount++;
                        break;
                    case ConflictMethod.HarmonicAverage:
                        if (!neighbourValue.Equals(0))
                        {
                            valueSum += (1 / neighbourValue);
                            valueCount++;
                        }
                        else
                        {
                            valueCount++;
                            isUndefinedHarmonicAverage = true;
                        }
                        break;
                    case ConflictMethod.MinimumValue:
                        if (neighbourValue < minValue)
                        {
                            minValue = neighbourValue;
                            valueCount = 1;
                        }
                        break;
                    case ConflictMethod.MaximumValue:
                        if (neighbourValue > maxValue)
                        {
                            maxValue = neighbourValue;
                            valueCount = 1;
                        }
                        break;
                    default:
                        throw new Exception("Unknown conflict method: " + conflictMethod.ToString());
                }
            }
        }

        /// <summary>
        /// The purpose of this method is to retrieve clusters with NoData (or other) values in <paramref name="valueIDFFile"/>, with the maximum cluster defined by the specified <paramref name="zoneIDFFile"/> and <paramref name="zoneValue"/>.
        /// The method retrieves a local zone, starting at specified initial cell. A local zone is defined as all connected cells with <paramref name="searchedValue"/> in <paramref name="valueIDFFile"/> and 
        /// <paramref name="zoneValue"/> in <paramref name="zoneIDFFile"/>.
        /// Cell values equal to <paramref name="searchedValue"/> are replaced by <paramref name="replacedValue"/>. Boundary values are not modified. The resulting IDF-file will have the extent of the resulting local zone.
        /// </summary>
        /// <param name="valueIDFFile"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="initialIDFCell"></param>
        /// <param name="zoneValue"></param>
        /// <param name="searchedValue">searched value in valueIDFFile</param>
        /// <param name="replacedValue"></param>
        /// <param name="includeValuesOutsideBoundary">if true, cells outside boundary cells (which are closest non-NoData-cells) with <paramref name="zoneValue"/>, but not <paramref name="searchedValue"/> are included</param>
        /// <returns></returns>
        protected IDFFile RetrieveLocalZoneValues(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFCell initialIDFCell, float zoneValue, float searchedValue, float replacedValue, bool includeValuesOutsideBoundary, SIFToolSettings settings)
        {
            bool isDebugMode = (settings.DebugMode == DebugMode.All) || (settings.DebugMode == DebugMode.Local);

            // The algorithm is as follows: 
            // - visit all cells that are connected to specified cell. Cells to visit are added to queue. 
            // - connected cells that have specified zonevalue (and if defined also have specified searchValue) are added to the ZoneIDFFile and its neighbours are visited as well.
            IDFFile localZoneIDFFile = null;
            List<IDFCell> localZoneIDFCells = new List<IDFCell>();

            // Use a hashset for fast checking if a cell already is visited
            HashSet<IDFCell> visitedIDFCells = new HashSet<IDFCell>();

            // Use both a hashset and a queue for cells to visit for access and process cells in order defined with a queue datastructure
            HashSet<IDFCell> idfCellHashSet = new HashSet<IDFCell>();
            Queue<IDFCell> idfCellQueue = new Queue<IDFCell>();

            // Add specified cell to IDFZone and add its neighbours to queue to visit
            localZoneIDFCells.Add(initialIDFCell);
            visitedIDFCells.Add(initialIDFCell);
            AddZoneNeighbours(initialIDFCell, valueIDFFile, zoneIDFFile, zoneValue, idfCellQueue, idfCellHashSet, visitedIDFCells, !settings.SkipDiagonalProcessing);

            // Define current extent of resulting IDF-file
            int minRowIdx = initialIDFCell.RowIdx;
            int maxRowIdx = initialIDFCell.RowIdx;
            int minColIdx = initialIDFCell.ColIdx;
            int maxColIdx = initialIDFCell.ColIdx;

            if (isDebugMode)
            {
                IDFFile queueIDFFile = IDFUtils.ConvertToIDF(idfCellHashSet, valueIDFFile, true);
                queueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "QueueIDFFile" + zoneValue + ".IDF"));

                localZoneIDFFile = IDFUtils.ConvertToIDF(localZoneIDFCells, valueIDFFile, minColIdx, minRowIdx, maxColIdx, maxRowIdx, true, float.NaN, 1f);
                localZoneIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalZoneIDFFile" + zoneValue + ".IDF"));
            }

            while (idfCellQueue.Count > 0)
            {
                IDFCell visitedCell = idfCellQueue.Dequeue();
                idfCellHashSet.Remove(visitedCell);
                visitedIDFCells.Add(visitedCell);

                if (isDebugMode)
                {
                    if (idfCellQueue.Count > 0)
                    {
                        IDFFile queueIDFFile = IDFUtils.ConvertToIDF(idfCellHashSet, zoneIDFFile, true);
                        queueIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "QueueIDFFile" + zoneValue + ".IDF"));
                    }

                    List<IDFCell> visitedIDFCellList = new List<IDFCell>();
                    visitedIDFCellList.Add(visitedCell);
                    IDFFile visitedCellIDFFile = IDFUtils.ConvertToIDF(visitedIDFCellList, zoneIDFFile, true);
                    visitedCellIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "VisitedCellIDFFile" + zoneValue + ".IDF"));

                    IDFFile visitedCellsIDFFile = IDFUtils.ConvertToIDF(visitedIDFCells, zoneIDFFile, true);
                    visitedCellsIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "VisitedCellsIDFFile" + zoneValue + ".IDF"));
                }

                if (includeValuesOutsideBoundary || valueIDFFile.values[visitedCell.RowIdx][visitedCell.ColIdx].Equals(searchedValue))
                {
                    // Add cell to zone and update extent
                    localZoneIDFCells.Add(visitedCell);

                    IDFCell.UpdateMinMaxIndices(visitedCell, ref minColIdx, ref minRowIdx, ref maxColIdx, ref maxRowIdx);

                    if (isDebugMode)
                    {
                        localZoneIDFFile = IDFUtils.ConvertToIDF(localZoneIDFCells, valueIDFFile, minColIdx, minRowIdx, maxColIdx, maxRowIdx, true, float.NaN, 1f);
                        localZoneIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalZoneIDFFile" + zoneValue + ".IDF"));
                    }

                    // Add neighbours to queue
                    AddZoneNeighbours(visitedCell, valueIDFFile, zoneIDFFile, zoneValue, idfCellQueue, idfCellHashSet, visitedIDFCells, !settings.SkipDiagonalProcessing);
                }
                else
                {
                    IDFCell.UpdateMinMaxIndices(visitedCell, ref minColIdx, ref minRowIdx, ref maxColIdx, ref maxRowIdx);
                    localZoneIDFCells.Add(visitedCell);
                }
            }

            // Convert list of IDFCells to IDFFile
            localZoneIDFFile = IDFUtils.ConvertToIDF(localZoneIDFCells, valueIDFFile, minColIdx, minRowIdx, maxColIdx, maxRowIdx, true, searchedValue, replacedValue);
            if (isDebugMode)
            {
                localZoneIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "LocalZoneIDFFile" + zoneValue + ".IDF"));
            }
            return localZoneIDFFile;
        }

        protected void AddZoneNeighbours(IDFCell idfCell, IDFFile valueIDFFile, IDFFile zoneIDFFile, float zoneValue, Queue<IDFCell> idfCellQueue, HashSet<IDFCell> idfCellHashSet, HashSet<IDFCell> visitedCells, bool isDiagonallyProcessed = false)
        {
            if ((idfCell.RowIdx + 1) < valueIDFFile.NRows)
            {
                float lowerNeighbourZoneValue= zoneIDFFile.values[idfCell.RowIdx + 1][idfCell.ColIdx];
                if (lowerNeighbourZoneValue.Equals(zoneValue))
                {
                    IDFCell lowerNeighbourCell = new IDFCell(idfCell.RowIdx + 1, idfCell.ColIdx);
                    if (!visitedCells.Contains(lowerNeighbourCell) && !idfCellHashSet.Contains(lowerNeighbourCell))
                    {
                        idfCellQueue.Enqueue(lowerNeighbourCell);
                        idfCellHashSet.Add(lowerNeighbourCell);
                    }
                }
            }
            if (idfCell.ColIdx > 0)
            {
                float leftNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx][idfCell.ColIdx - 1];
                if (leftNeighbourZoneValue.Equals(zoneValue))
                {
                    IDFCell leftNeighbourCell = new IDFCell(idfCell.RowIdx, idfCell.ColIdx - 1);
                    if (!visitedCells.Contains(leftNeighbourCell) && !idfCellQueue.Contains(leftNeighbourCell))
                    {
                        idfCellQueue.Enqueue(leftNeighbourCell);
                        idfCellHashSet.Add(leftNeighbourCell);
                    }
                }
            }
            if ((idfCell.ColIdx + 1) < valueIDFFile.NCols)
            {
                float rightNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx][idfCell.ColIdx + 1];
                if (rightNeighbourZoneValue.Equals(zoneValue))
                {
                    IDFCell rightNeighbourCell = new IDFCell(idfCell.RowIdx, idfCell.ColIdx + 1);
                    if (!visitedCells.Contains(rightNeighbourCell) && !idfCellQueue.Contains(rightNeighbourCell))
                    {
                        idfCellQueue.Enqueue(rightNeighbourCell);
                        idfCellHashSet.Add(rightNeighbourCell);
                    }
                }
            }
            if (idfCell.RowIdx > 0)
            {
                float upperNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx - 1][idfCell.ColIdx];
                if (upperNeighbourZoneValue.Equals(zoneValue))
                {
                    IDFCell upperNeighbourCell = new IDFCell(idfCell.RowIdx - 1, idfCell.ColIdx);
                    if (!visitedCells.Contains(upperNeighbourCell) && !idfCellQueue.Contains(upperNeighbourCell))
                    {
                        idfCellQueue.Enqueue(upperNeighbourCell);
                        idfCellHashSet.Add(upperNeighbourCell);
                    }
                }
            }

            if (isDiagonallyProcessed)
            {
                if ((idfCell.RowIdx + 1) < valueIDFFile.NRows)
                {
                    if (idfCell.ColIdx > 0)
                    {
                        float lowerLeftNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx + 1][idfCell.ColIdx - 1];
                        if (lowerLeftNeighbourZoneValue.Equals(zoneValue))
                        {
                            IDFCell lowerLeftNeighbourCell = new IDFCell(idfCell.RowIdx + 1, idfCell.ColIdx - 1);
                            if (!visitedCells.Contains(lowerLeftNeighbourCell) && !idfCellHashSet.Contains(lowerLeftNeighbourCell))
                            {
                                idfCellQueue.Enqueue(lowerLeftNeighbourCell);
                                idfCellHashSet.Add(lowerLeftNeighbourCell);
                            }
                        }
                    }
                    if ((idfCell.ColIdx + 1) < valueIDFFile.NCols)
                    {
                        float lowerRightNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx + 1][idfCell.ColIdx + 1];
                        if (lowerRightNeighbourZoneValue.Equals(zoneValue))
                        {
                            IDFCell lowerRightNeighbourCell = new IDFCell(idfCell.RowIdx + 1, idfCell.ColIdx + 1);
                            if (!visitedCells.Contains(lowerRightNeighbourCell) && !idfCellQueue.Contains(lowerRightNeighbourCell))
                            {
                                idfCellQueue.Enqueue(lowerRightNeighbourCell);
                                idfCellHashSet.Add(lowerRightNeighbourCell);
                            }
                        }
                    }
                }
                if (idfCell.RowIdx > 0)
                {
                    if (idfCell.ColIdx > 0)
                    {
                        float upperLeftNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx - 1][idfCell.ColIdx - 1];
                        if (upperLeftNeighbourZoneValue.Equals(zoneValue))
                        {
                            IDFCell upperLeftNeighbourCell = new IDFCell(idfCell.RowIdx - 1, idfCell.ColIdx - 1);
                            if (!visitedCells.Contains(upperLeftNeighbourCell) && !idfCellQueue.Contains(upperLeftNeighbourCell))
                            {
                                idfCellQueue.Enqueue(upperLeftNeighbourCell);
                                idfCellHashSet.Add(upperLeftNeighbourCell);
                            }
                        }
                    }
                    if ((idfCell.ColIdx + 1) < valueIDFFile.NCols)
                    {
                        float upperRightNeighbourZoneValue = zoneIDFFile.values[idfCell.RowIdx - 1][idfCell.ColIdx + 1];
                        if (upperRightNeighbourZoneValue.Equals(zoneValue))
                        {
                            IDFCell upperRightNeighbourCell = new IDFCell(idfCell.RowIdx - 1, idfCell.ColIdx + 1);
                            if (!visitedCells.Contains(upperRightNeighbourCell) && !idfCellQueue.Contains(upperRightNeighbourCell))
                            {
                                idfCellQueue.Enqueue(upperRightNeighbourCell);
                                idfCellHashSet.Add(upperRightNeighbourCell);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grow Data-cells cells in IDF-file with one cell with specified value
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="newValue"></param>
        /// <param name="useDiagonalProcessing"></param>
        /// <returns></returns>
        public IDFFile Grow(IDFFile idfFile, float newValue, bool useDiagonalProcessing = false)
        {
            IDFFile newIDFFile = idfFile.CopyIDF(idfFile.Filename);

            int maxRowIdx = idfFile.NRows - 1;
            int maxColIdx = idfFile.NCols - 1;
            float noDataValue = idfFile.NoDataValue;

            for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                {
                    if (!idfFile.values[rowIdx][colIdx].Equals(idfFile.NoDataValue))
                    {
                        // Check neighbors for NoData-values
                        if (rowIdx < maxRowIdx)
                        {
                            if (newIDFFile.values[rowIdx + 1][colIdx].Equals(noDataValue))
                            {
                                newIDFFile.values[rowIdx + 1][colIdx] = newValue;
                            }
                        }
                        if (colIdx < maxColIdx)
                        {
                            if (newIDFFile.values[rowIdx][colIdx + 1].Equals(noDataValue))
                            {
                                newIDFFile.values[rowIdx][colIdx + 1] = newValue;
                            }
                        }
                        if (rowIdx > 0)
                        {
                            if (newIDFFile.values[rowIdx - 1][colIdx].Equals(noDataValue))
                            {
                                newIDFFile.values[rowIdx - 1][colIdx] = newValue;
                            }
                        }
                        if (colIdx > 0)
                        {
                           if (newIDFFile.values[rowIdx][colIdx - 1].Equals(noDataValue))
                            {
                                newIDFFile.values[rowIdx][colIdx - 1] = newValue;
                            }
                        }
                        if (!useDiagonalProcessing)
                        {
                            if ((rowIdx > 0) && (colIdx > 0))
                            {
                                if (newIDFFile.values[rowIdx - 1][colIdx - 1].Equals(noDataValue))
                                {
                                    newIDFFile.values[rowIdx - 1][colIdx - 1] = newValue;
                                }
                            }
                            if ((colIdx > 0) && (rowIdx < maxRowIdx))
                            {
                                if (newIDFFile.values[rowIdx + 1][colIdx - 1].Equals(noDataValue))
                                {
                                    newIDFFile.values[rowIdx + 1][colIdx - 1] = newValue;
                                }
                            }
                            if ((rowIdx < maxRowIdx) && (colIdx < maxColIdx))
                            {
                                if (newIDFFile.values[rowIdx + 1][colIdx + 1].Equals(noDataValue))
                                {
                                    newIDFFile.values[rowIdx + 1][colIdx + 1] = newValue;
                                }
                            }
                            if ((colIdx < maxColIdx) && (rowIdx > 0))
                            {
                                if (newIDFFile.values[rowIdx - 1][colIdx + 1].Equals(noDataValue))
                                {
                                    newIDFFile.values[rowIdx - 1][colIdx + 1] = newValue;
                                }
                            }
                        }

                    }
                }
            }

            return newIDFFile;
        }
    }
}
