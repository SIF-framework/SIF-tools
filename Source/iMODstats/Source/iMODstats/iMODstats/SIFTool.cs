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
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Values;
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

        protected const string NoResultsMessage = "No results found";
        protected const int MaxUniqueValueCount = 1000;

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
            ToolPurpose = "SIF-tool for creating Excelfile with statistics for IDF-files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = -1;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            // Retrieve statistics for all files and (optional) zones
            Dictionary<string, List<ZoneStatistics>> fileZoneStats = GetFileZoneStatistics(settings.InputPath, settings, Log);
            if ((fileZoneStats == null) || (fileZoneStats.Count == 0))
            {
                throw new ToolException("No files found in inputpath for specified filter (" + settings.InputFilter + "): " + settings.InputPath);
            }

            // Create statistics result table
            StatisticssResult statsResult = null;
            try
            {
                statsResult = CreateStatsResultsObject(fileZoneStats);
                statsResult.Initialize();

                // Loop through filenames and write statistics per file (and optional zones) to result table
                for (int idx = 0; idx < fileZoneStats.Keys.Count; idx++)
                {
                    // Retrieve filename
                    string filename = fileZoneStats.Keys.ElementAt(idx);
                    // Retrieve statistics for current file
                    List<ZoneStatistics> zoneStats = fileZoneStats.Values.ElementAt(idx);
                    for (int zoneIdx = 0; zoneIdx < zoneStats.Count; zoneIdx++)
                    {
                        ZoneStatistics stats = zoneStats[zoneIdx];
                        if (stats != null)
                        {
                            statsResult.AddZoneStats(filename, stats);
                        }
                    }
                }

                statsResult.ProcessLayout();

                Log.AddInfo("Writing result to " + settings.OutputFile + " ...", 1);
                statsResult.WriteFile(settings.OutputFile);

                ToolSuccessMessage = "Finished processing";

                exitcode = 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not write spreadsheet", ex);
            }
            finally
            {
                if (statsResult != null)
                {
                    statsResult.Cleanup();
                }
            }

            return exitcode;
        }

        /// <summary>
        /// Create an empty StatsResult object
        /// </summary>
        /// <param name="fileStats"></param>
        /// <returns></returns>
        protected virtual StatisticssResult CreateStatsResultsObject(Dictionary<string, List<ZoneStatistics>> fileStats)
        {
            return new StatisticssResult((SIFToolSettings) Settings, fileStats, Log);
        }

        /// <summary>
        /// Retrieves dictionary with list of ZoneStatistics (one per zone) per filename
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, List<ZoneStatistics>> GetFileZoneStatistics(string inputPath, SIFToolSettings settings, Log log)
        {
            string currentFilename = null;
            string currentFilePath = null;

            log.AddInfo("Processing folder " + inputPath + " ...", 0);
            string[] inputFiles = Directory.GetFiles(inputPath, settings.InputFilter);
            CommonUtils.SortAlphanumericStrings(inputFiles);

            // Loop through all input files and retrieve statistics per file, stored in dictionary with full filename as key
            Dictionary<string, List<ZoneStatistics>> fileStatsDictionary = new Dictionary<string, List<ZoneStatistics>>();
            for (int i = 0; i < inputFiles.Length; i++)
            {
                currentFilePath = inputFiles[i];
                currentFilename = Path.GetFileName(currentFilePath);
                log.AddInfo("Processing file " + currentFilename + " ...", 1);
                if (!Path.GetExtension(currentFilename).ToLower().Equals(".idf"))
                {
                    continue;
                }

                try
                {
                    // Create statistics per zone for current file
                    IDFFile idfFile = IDFFile.ReadFile(currentFilePath, false, log, 0, settings.Extent);
                    if (idfFile.Extent == null)
                    {
                        // Empty extent, skip file
                        if (settings.Extent != null)
                        {
                            log.AddWarning("Clipped file has empty extent and is skipped: " + currentFilename);
                        }
                        else
                        {
                            log.AddWarning("File has empty extent and is skipped: " + currentFilename);
                        }
                        continue;
                    }

                    fileStatsDictionary.Add(currentFilePath, GetZoneStatistics(idfFile, settings));
                }
                catch (Exception ex)
                {
                    log.AddError("Could not read IDF-file " + currentFilePath, 0);
                    log.AddInfo(ex.GetBaseException().Message, 1);
                    log.AddInfo("File is skipped", 1);
                }
            }

            return fileStatsDictionary;
        }

        /// <summary>
        /// Retrieve statistics per zone for specified IDF-file and settings. Note: currently only a single zone is used, equal to the whole extent of the IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual List<ZoneStatistics> GetZoneStatistics(IDFFile idfFile, SIFToolSettings settings)
        {
            List<ZoneStatistics> statList = new List<ZoneStatistics>();

            // Retrieve statistics for zones: currently only a single zone (null) is used, equal to the whole extent of the IDF-file
            statList.Add(GetZoneStatistics(idfFile, settings, null));

            return statList;
        }

        /// <summary>
        /// Retrieve statistics for all non-NoData cells in specified IDF-file for specified settings.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="settings"></param>
        /// <param name="zoneID">an id for the non-NoData cells in the specified IDF-file</param>
        /// <returns></returns>
        protected virtual ZoneStatistics GetZoneStatistics(IDFFile idfFile, SIFToolSettings settings, string zoneID)
        {
            ZoneStatistics zoneStatistics = new ZoneStatistics(settings.PercentileClassCount, settings.DecimalCount, zoneID);

            IDFStatistics idfStats = null;
            if ((settings.Extent != null) && idfFile.Extent.Equals(settings.Extent))
            {
                idfStats = new IDFStatistics(idfFile, settings.Extent, new List<float>());
            }
            else
            {
                idfStats = new IDFStatistics(idfFile, new List<float>());
            }
            idfStats.ComputeBasicStatistics(false, false);
            idfStats.ComputePercentiles();
            List<float> statList = new List<float>();
            long count = idfStats.Count;
            statList.Add(idfFile.XCellsize);
            statList.Add(count);
            statList.Add(idfStats.NonSkippedFraction);
            statList.Add((count > 0) ? idfStats.Min : 0);
            statList.Add((count > 0) ? idfStats.Max : 0);
            statList.Add((count > 0) ? idfStats.Mean : 0);
            statList.Add((count > 0) ? idfStats.SD : 0);
            statList.Add((count > 0) ? idfStats.Sum : 0);

            if (count > 0)
            {
                float[] percentiles = idfStats.Percentiles;
                for (int pctIdx = 1; pctIdx <= settings.PercentileClassCount; pctIdx++)
                {
                    int percentile = (int)((100.0 / ((float)settings.PercentileClassCount)) * pctIdx);
                    statList.Add(percentiles[percentile]);
                }
            }
            else
            {
                for (int pctIdx = 1; pctIdx <= settings.PercentileClassCount; pctIdx++)
                {
                    statList.Add(0);
                }
            }
            zoneStatistics.StatList = statList;

            return zoneStatistics;
        }

        /// <summary>
        /// Corrects scale of source IDFFile to scale of reference IDFile. Currently only works when X- and Y-cellsizes of both IDF-files are equal.
        /// If cellsize of source and reference are different a new IDF-file object is created, otheriwse no scaling is applied and this IDF-file object is returned.
        /// </summary>
        /// <param name="sourceIDFFile"></param>
        /// <param name="refIDFFile"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected static IDFFile CorrectScale(IDFFile sourceIDFFile, IDFFile refIDFFile, Log log, int logIndentLevel = 0)
        {
            IDFFile scaledIDFFile = sourceIDFFile;

            if ((sourceIDFFile != null) && (!refIDFFile.XCellsize.Equals(sourceIDFFile.XCellsize) || !refIDFFile.YCellsize.Equals(sourceIDFFile.YCellsize)))
            {
                // zone IDF-file and IDF-file have mismatch in cellsize, try to scale when x- and y-cellsizes are equal
                if (sourceIDFFile.XCellsize.Equals(sourceIDFFile.YCellsize) && refIDFFile.XCellsize.Equals(refIDFFile.YCellsize))
                {
                    log.AddInfo("Scaling " + Path.GetFileName(sourceIDFFile.Filename) + " from " + sourceIDFFile.XCellsize + " to " + refIDFFile.XCellsize + " ...", logIndentLevel);
                    if (sourceIDFFile.XCellsize < refIDFFile.XCellsize)
                    {
                        scaledIDFFile = sourceIDFFile.Upscale(refIDFFile.XCellsize, UpscaleMethodEnum.MostOccurring);
                    }
                    else
                    {
                        scaledIDFFile = sourceIDFFile.Downscale(refIDFFile.XCellsize, DownscaleMethodEnum.Block);
                    }
                }
            }

            return scaledIDFFile;
        }
    }
}
