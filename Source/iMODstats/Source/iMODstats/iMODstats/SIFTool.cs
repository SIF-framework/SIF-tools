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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.ASC;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Values;
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
            ToolPurpose = "SIF-tool for creating Excel-file with statistics for iMOD-files (IDF/IPF)";
            ToolDescription = "This tool will create an Excelsheet with statistics about one or more iMOD-files. Currently supported are IDF, ASC and IPF-files. For IPF-files statistics will be created over all related timeseries.";
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

            string[] inputfiles = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (ContainsExtension(inputfiles, "IPF"))
            {
                if ((settings.IPFValueColRef == null) && (settings.IPFTSValueColIdx1 < 0))
                {
                    Log.AddInfo("Using default value column numnber " + SIFToolSettings.DefaultIPFTSValueColNr + " for calculation of timeseries statistics");
                    settings.IPFTSValueColIdx1 = SIFToolSettings.DefaultIPFTSValueColNr - 1;
                }
            }

            // Retrieve statistics for all files and (optional) zones per file type (file extension)
            Dictionary<string, Dictionary<string, List<ZoneStatistics>>> fileExtZoneStatsDictionary = GetFileExtensionZoneStatistics(settings.InputPath, settings, Log);
            if ((fileExtZoneStatsDictionary == null) || (fileExtZoneStatsDictionary.Count == 0))
            {
                throw new ToolException("No files found in input path for specified filter (" + settings.InputFilter + "): " + settings.InputPath);
            }

            // Loop through all file types (extension) and add results to result table
            foreach (string fileExtension in fileExtZoneStatsDictionary.Keys)
            {
                Dictionary<string, List<ZoneStatistics>> fileZoneStatsDictionary = fileExtZoneStatsDictionary[fileExtension];

                // Create statistics result table
                ResultTable resultTable = null;

                string resultFilename = settings.OutputFile;
                try
                {
                    resultTable = CreateResultTable(fileZoneStatsDictionary, fileExtension);

                    // Write results to file(s)
                    if (fileExtZoneStatsDictionary.Count > 1)
                    {
                        // Add file extension to result filename when multiple extensions were processed
                        resultFilename = FileUtils.AddFilePostFix(resultFilename, "_" + fileExtension);
                    }

                    Log.AddInfo("Writing result to " + resultFilename + " ...", 1);
                    resultTable.WriteFile(resultFilename);
                }
                catch (Exception ex)
                {
                    bool isLockedExcelFile = false;
                    if (ex is InvalidOperationException || ex is IOException)
                    {
                        string innerMsg = (ex.InnerException != null) ? ex.InnerException.Message.ToLower() : string.Empty;
                        string baseMsg = ex.GetBaseException().Message;
                        if ((innerMsg.Contains(".xlsx") && innerMsg.Contains("error overwriting file")) 
                            || (baseMsg.Contains(".xlsx") && (baseMsg.Contains("gebruikt door een ander proces") || baseMsg.Contains("used by an other process"))))
                        {
                            isLockedExcelFile = true;
                        }
                    }
                    if (isLockedExcelFile)
                    {
                        throw new ToolException("Could not overwrite existing spreadsheet, as it is locked by another process: " + resultFilename, ex);
                    }
                    else
                    {
                        throw new Exception("Could not create and/or write result spreadsheet", ex);
                    }
                }
                finally
                {
                    if (resultTable != null)
                    {
                        resultTable.Cleanup();
                    }
                }
            }

            ToolSuccessMessage = "Finished processing";

            exitcode = 0;

            return exitcode;
        }

        protected virtual ResultTable CreateResultTable(Dictionary<string, List<ZoneStatistics>> fileZoneStatsDictionary, string fileExtension)
        {
            SIFToolSettings settings = (SIFToolSettings) Settings;

            // Create object for result table
            ResultTable resultTable = CreateResultTableObject(fileZoneStatsDictionary, settings);

            // Create list of extra settings specific for file type/extension that should be logged above the result table
            Dictionary<string, string> loggedTableSettings = new Dictionary<string, string>();
            switch (fileExtension.ToUpper())
            {
                case "IPF":
                    if (settings.IPFValueColRef != null)
                    {
                        loggedTableSettings.Add("IPF value column", settings.IPFValueColRef);
                    }
                    else
                    {
                        loggedTableSettings.Add("IPF TS-value column v1", settings.IPFTSValueColIdx1.ToString());
                        if (settings.IPFTSValueColIdx2 >= 0)
                        {
                            loggedTableSettings.Add("IPF TS-value column v2 for residual", settings.IPFTSValueColIdx2.ToString());
                        }
                    }
                    if ((settings.IPFTSPeriodStartDate != null) || (settings.IPFTSPeriodEndDate != null))
                    {
                        string periodString = ((settings.IPFTSPeriodStartDate != null) ? settings.IPFTSPeriodStartDate.ToString() : string.Empty) + " - " + ((settings.IPFTSPeriodEndDate != null) ? settings.IPFTSPeriodEndDate.ToString() : string.Empty);
                        loggedTableSettings.Add("Period", periodString);
                    }
                    break;
                case "IDF":
                case "ASC":
                    break;
                default:
                    // ignore
                    break;
            }

            // Initialize result table and add extra settings to header
            resultTable.Initialize(loggedTableSettings);

            // Loop through filenames and add statistics per file (and optional zones) to result table
            for (int idx = 0; idx < fileZoneStatsDictionary.Keys.Count; idx++)
            {
                // Retrieve filename
                string filename = fileZoneStatsDictionary.Keys.ElementAt(idx);
                // Retrieve statistics for current file
                List<ZoneStatistics> zoneStats = fileZoneStatsDictionary.Values.ElementAt(idx);
                for (int zoneIdx = 0; zoneIdx < zoneStats.Count; zoneIdx++)
                {
                    ZoneStatistics stats = zoneStats[zoneIdx];
                    if (stats != null)
                    {
                        resultTable.AddZoneStatistics(filename, stats);
                    }
                }
            }

            resultTable.ProcessLayout();

            return resultTable;
        }

        /// <summary>
        /// Create an empty ResultTable object
        /// </summary>
        /// <param name="zoneStatistics"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual ResultTable CreateResultTableObject(Dictionary<string, List<ZoneStatistics>> zoneStatistics, SIFToolSettings settings)
        {
            ResultTableSettings resultTableSettings = CreateResultTableSettings(settings);

            return new ResultTable(resultTableSettings, zoneStatistics, Log);
        }

        /// <summary>
        /// Retrieves dictionary with list of ZoneStatistics (one per zone) per filename per file extension (which is key of first dictionary)
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, Dictionary<string, List<ZoneStatistics>>> GetFileExtensionZoneStatistics(string inputPath, SIFToolSettings settings, Log log)
        {
            string currentFilename = null;
            string currentFilePath = null;

            log.AddInfo("Processing folder " + inputPath + " ...", 0);
            string[] inputFiles = Directory.GetFiles(inputPath, settings.InputFilter);
            CommonUtils.SortAlphanumericStrings(inputFiles);

            // Loop through all input files and retrieve statistics per file, stored in dictionary with full filename as key, which itself is stores in a dictionary with the file extension (upper case) as a string
            Dictionary<string, Dictionary<string, List<ZoneStatistics>>> fileExtensionStatsDictionary = new Dictionary<string, Dictionary<string, List<ZoneStatistics>>>();
            for (int i = 0; i < inputFiles.Length; i++)
            {
                currentFilePath = inputFiles[i];
                currentFilename = Path.GetFileName(currentFilePath);
                log.AddInfo("Processing file " + currentFilename + " ...", 1);

                string fileExtension = Path.GetExtension(currentFilename);
                if ((fileExtension != null) && !fileExtension.Equals(string.Empty))
                {
                    // Remove initial dot and convert to uppercase
                    fileExtension = fileExtension.Substring(1).ToUpper();
                }
                if (!fileExtensionStatsDictionary.ContainsKey(fileExtension))
                {
                    fileExtensionStatsDictionary.Add(fileExtension, new Dictionary<string, List<ZoneStatistics>>());
                }
                Dictionary<string, List<ZoneStatistics>> fileStatsDictionary = fileExtensionStatsDictionary[fileExtension];

                // Create statistics per zone for current file
                List<ZoneStatistics> zoneStatistics = null;
                IMODFile imodFile = null;
                string extension = Path.GetExtension(currentFilename);
                switch (Path.GetExtension(currentFilename).ToLower())
                {
                    case ".idf":
                        IDFFile idfFile = IDFFile.ReadFile(currentFilePath, false, log, 0, settings.Extent);
                        imodFile = idfFile;
                        zoneStatistics = GetZoneStatistics(idfFile, settings);
                        break;
                    case ".asc":
                        ASCFile ascFile = ASCFile.ReadFile(currentFilePath, EnglishCultureInfo);
                        idfFile = new IDFFile(ascFile);
                        imodFile = idfFile;
                        if (settings.Extent != null)
                        {
                            idfFile = idfFile.ClipIDF(settings.Extent);
                        }
                        zoneStatistics = GetZoneStatistics(idfFile, settings);
                        break;
                    case ".ipf":
                        IPFFile ipfFile = ReadIPFFile(currentFilePath, settings);
                        imodFile = ipfFile;
                        zoneStatistics = GetZoneStatistics(ipfFile, settings);
                        break;
                    default:
                        log.AddWarning("Extension " + extension + " is not supported, file is skipped: " + Path.GetFileName(currentFilePath));
                        continue;
                }

                try
                {
                    if (imodFile.Extent == null)
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

                    if (zoneStatistics != null)
                    {
                        fileStatsDictionary.Add(currentFilePath, zoneStatistics);
                    }
                }
                catch (Exception ex)
                {
                    log.AddError("Could not read IDF-file " + currentFilePath, 0);
                    log.AddInfo(ex.GetBaseException().Message, 1);
                    log.AddInfo("File is skipped", 1);
                }
            }

            if (settings.IsRecursive)
            {
                string[] subdirs = Directory.GetDirectories(inputPath);
                foreach (string subdir in subdirs)
                {
                    Dictionary<string, Dictionary<string, List<ZoneStatistics>>> subFileExtList = GetFileExtensionZoneStatistics(subdir, settings, log);
                    foreach (string fileExtension in subFileExtList.Keys)
                    {
                        Dictionary<string, List<ZoneStatistics>> subFileList = subFileExtList[fileExtension];
                        foreach (string filename in subFileList.Keys)
                        {
                            if (!fileExtensionStatsDictionary.ContainsKey(fileExtension))
                            {
                                fileExtensionStatsDictionary.Add(fileExtension, new Dictionary<string, List<ZoneStatistics>>());
                            }
                            fileExtensionStatsDictionary[fileExtension].Add(filename, subFileList[filename]);
                        }
                    }
                }
            }

            return fileExtensionStatsDictionary;
        }

        protected IPFFile ReadIPFFile(string ipfFilename, SIFToolSettings settings)
        {
            IPFFile ipfFile = IPFFile.ReadFile(ipfFilename, true);
            int xColIdx = ipfFile.FindColumnIndex(settings.IPFXColRef);
            int yColIdx = ipfFile.FindColumnIndex(settings.IPFYColRef);
            if ((xColIdx < 0) || (xColIdx >= ipfFile.ColumnCount))
            {
                throw new ToolException("Invalid X-column: " + settings.IPFXColRef);
            }
            if ((yColIdx < 0) || (yColIdx >= ipfFile.ColumnCount))
            {
                throw new ToolException("Invalid Y-column: " + settings.IPFYColRef);
            }

            ipfFile = IPFFile.ReadFile(ipfFilename, xColIdx, yColIdx);

            return ipfFile;
        }

        /// <summary>
        /// Retrieve statistics per zone for specified IDF-file and settings. Note: currently only a single zone is used, equal to the whole extent of the IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual List<ZoneStatistics> GetZoneStatistics(IDFFile idfFile, SIFToolSettings settings)
        {
            List<ZoneStatistics> statList = new List<ZoneStatistics>();

            IDFZoneSettings zoneSettings = CreateIDFZoneSettings(settings);

            // Retrieve statistics: currently only a single zone (null) is used, equal to the whole extent of the IDF-file
            ZoneStatistics zoneStatistics = new IDFZoneStatistics(idfFile, zoneSettings, null);
            zoneStatistics.Calculate();

            statList.Add(zoneStatistics);

            return statList;
        }


        /// <summary>
        /// Retrieve statistics per zone for specified IPF-file and settings. Note: currently only a single zone is used, equal to the whole IPF-file
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual List<ZoneStatistics> GetZoneStatistics(IPFFile ipfFile, SIFToolSettings settings)
        {
            List<ZoneStatistics> statList = new List<ZoneStatistics>();

            IPFZoneSettings zoneSettings = CreateIPFZoneSettings(settings);

            // Retrieve statistics: currently only a single zone (null) is used, equal to the whole IPF-file
            IPFZoneStatistics zoneStatistics = new IPFZoneStatistics(ipfFile, zoneSettings, null);
            zoneStatistics.Calculate();

            if ((zoneStatistics.ResultIPFFile != null) && (zoneStatistics.ResultIPFFile.PointCount > 0))
            {
                // Write resulting IPF-file
                string resultIPFFilename = Path.Combine(Path.GetDirectoryName(settings.OutputFile), FileUtils.GetRelativePath(ipfFile.Filename, settings.InputPath));
                zoneStatistics.ResultIPFFile.WriteFile(resultIPFFilename);
            }

            if (zoneStatistics.StatValues == null)
            {
                Log.AddInfo("No IPF-points with results found for IPF-file, no results written: " + FileUtils.GetRelativePath(ipfFile.Filename, settings.InputPath), 2);
            }

            statList.Add(zoneStatistics);

            return statList;
        }

        /// <summary>
        /// Create ZoneSettings object specific for IDF-file statistics; copy settings from SIFToolsSettings object
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IDFZoneSettings CreateIDFZoneSettings(SIFToolSettings settings)
        {
            IDFZoneSettings idfZoneSettings = new IDFZoneSettings();
            CopyIDFZoneSettings(settings, idfZoneSettings);
            return idfZoneSettings;
        }

        /// <summary>
        /// Create ZoneSettings object specific for IPF-file statistics; copy settings from SIFToolsSettings object
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IPFZoneSettings CreateIPFZoneSettings(SIFToolSettings settings)
        {
            IPFZoneSettings ipfZoneSettings = new IPFZoneSettings();
            CopyIPFZoneSettings(settings, ipfZoneSettings);
            return ipfZoneSettings;
        }

        /// <summary>
        /// Create ResultTableSettings object from SIFToolsSettings object
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected ResultTableSettings CreateResultTableSettings(SIFToolSettings settings)
        {
            ResultTableSettings resultTableSettings = new ResultTableSettings();
            CopyResultTableSettings(settings, resultTableSettings);
            return resultTableSettings;
        }

        /// <summary>
        /// Copy settings from SIFToolsSettings object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="idfZoneSettings"></param>
        protected void CopyIDFZoneSettings(SIFToolSettings settings, IDFZoneSettings idfZoneSettings)
        {
            CopyBasicZoneSettings(settings, idfZoneSettings);

            // Copy settings that are specific for IDF-files
        }

        /// <summary>
        /// Copy settings from SIFToolsSettings object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="idfZoneSettings"></param>
        protected void CopyIPFZoneSettings(SIFToolSettings settings, IPFZoneSettings ipfZoneSettings)
        {
            CopyBasicZoneSettings(settings, ipfZoneSettings);

            // Copy settings that are specific for IPF-files
            ipfZoneSettings.InputPath = settings.InputPath;
            ipfZoneSettings.OutputPath = Path.GetDirectoryName(settings.OutputFile);
            ipfZoneSettings.IPFIDColRef = settings.IPFIDColRef;
            ipfZoneSettings.IPFValueColRef = settings.IPFValueColRef;
            ipfZoneSettings.IPFSelColRefs = settings.IPFSelColRefs;
            ipfZoneSettings.IPFTSValueColIdx1 = settings.IPFTSValueColIdx1;
            ipfZoneSettings.IPFTSValueColIdx2 = settings.IPFTSValueColIdx2;
            ipfZoneSettings.IPFResidualMethod = settings.IPFResidualMethod;
            ipfZoneSettings.IPFTSPeriodStartDate = settings.IPFTSPeriodStartDate;
            ipfZoneSettings.IPFTSPeriodEndDate = settings.IPFTSPeriodEndDate;
        }

        /// <summary>
        /// Copy basic settings from SIFToolsSettings object that are used for all statistic analysis
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="idfZoneSettings"></param>
        protected void CopyBasicZoneSettings(SIFToolSettings settings, ZoneSettings zoneSettings)
        {
            zoneSettings.DecimalCount = settings.DecimalCount;
            zoneSettings.PercentilePercentages = settings.PercentilePercentages;
            zoneSettings.Extent = settings.Extent;
        }

        /// <summary>
        /// Copy settings from SIFToolsSettings object that are used to create result table
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="idfZoneSettings"></param>
        protected void CopyResultTableSettings(SIFToolSettings settings, ResultTableSettings resultTableSettings)
        {
            resultTableSettings.InputPath = settings.InputPath;
            resultTableSettings.InputFilter = settings.InputFilter;
            resultTableSettings.OutputFile = settings.OutputFile;
            resultTableSettings.IsRecursive = settings.IsRecursive;

            resultTableSettings.Extent = settings.Extent;
            resultTableSettings.IsOverwrite = settings.IsOverwrite;
        }

        /// <summary>
        /// Check if array of filenames contains a filename with specified extension
        /// </summary>
        /// <param name="filenames"></param>
        /// <param name="extension">heck file extension, case ignored</param>
        /// <returns></returns>
        protected bool ContainsExtension(string[] filenames, string extension)
        {
            foreach (string filename in filenames)
            {
                string currentExtension = Path.GetExtension(filename);
                if (!currentExtension.Equals(string.Empty))
                {
                    // remove leading dot
                    currentExtension = currentExtension.Substring(1);
                }
                if (currentExtension.ToUpper().Equals(extension.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
