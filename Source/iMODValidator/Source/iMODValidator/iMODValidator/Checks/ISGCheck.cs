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
using OrderedPropertyGrid;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Point = Sweco.SIF.GIS.Point;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class ISGCheckSettings : CheckSettings
    {
        private float resultCellSize;

        [Category("\tISG-properties"), Description("Cellsize (m) for the resultgrids"), PropertyOrder(1)]
        public float ResultCellSize
        {
            get { return resultCellSize; }
            set
            {
                if (value > 0)
                {
                    resultCellSize = value;
                }
            }
        }

        [Category("Warning-properties"), Description("Comma seperated list of filenames for ISG-files that should have higher winter than summer levels (RegEx allowed)"), PropertyOrder(10)]
        public string WinterHighFNameStrings { get; set; }

        [Category("Warning-properties"), Description("The minimum difference between winter- and summerlevel for a low winterlevel to give a warning"), PropertyOrder(11)]
        public float MinWinterLowDifference { get; set; }

        [Category("Warning-properties"), Description("Comma seperated list of filenames for ISG-files that should have lower winter than summer levels (RegEx allowed); WinterHighFNames are always excluded"), PropertyOrder(12)]
        public string WinterLowFNameStrings { get; set; }

        [Category("Warning-properties"), Description("The maximum WinterHigh-ratio (0-1): number of WinterHigh-segments out of all segments in ISG-file; a higher ratio will give a warning; use 0 when all segments should have a lower winter level"), PropertyOrder(13)]
        public float MaxWinterHighRatio { get; set; }

        [Category("Warning-properties"), Description("The minimum number of calculation points with unexpected levels to report a WinterHigh- or WinterLow-warning"), PropertyOrder(15)]
        public int MinWinterCheckCPCount { get; set; }

        [Category("Warning-properties"), Description("The maximum valid relative stage change per meter within segments"), PropertyOrder(20)]
        public string MaxRelStageChange { get; set; }

        [Category("Warning-properties"), Description("The maximum valid stage change between calculation points at same coordinate"), PropertyOrder(21)]
        public string MaxAbsStageChange { get; set; }

        [Category("Warning-properties"), Description("The minimum valid hydraulic resistance (d) for this region. Used for calculating the maximum conductance."), PropertyOrder(30)]
        public string HydraulicResistanceMinValue { get; set; }

        [Category("Warning-properties"), Description("The maximum valid hydraulic resistance (d) for this region. Used for calculating the minimum conductance."), PropertyOrder(31)]
        public string HydraulicResistanceMaxValue { get; set; }

        public ISGCheckSettings(string checkName) : base(checkName)
        {
            resultCellSize = 100f;
            WinterHighFNameStrings = "Rivier";
            WinterLowFNameStrings = ".*";
            MinWinterLowDifference = 0.05f;
            MaxWinterHighRatio = 0.33f;
            MinWinterCheckCPCount = 0;
            MaxRelStageChange = "0.2";
            MaxAbsStageChange = "1";
            HydraulicResistanceMinValue = ((float)0.1f).ToString(englishCultureInfo);
            HydraulicResistanceMaxValue = "1000";
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("result cellsize: " + resultCellSize, logIndentLevel);
            log.AddInfo("WinterHigh filename strings: " + WinterHighFNameStrings, logIndentLevel);
            log.AddInfo("Minimum WinterLowDifference-ratio: " + MinWinterLowDifference, logIndentLevel);
            log.AddInfo("WinterLow filename strings: " + WinterLowFNameStrings, logIndentLevel);
            log.AddInfo("Maximum WinterHigh-ratio (0-1): " + MaxWinterHighRatio, logIndentLevel);
            log.AddInfo("Minimum calculation points for winter checks: " + MinWinterCheckCPCount, logIndentLevel);
            log.AddInfo("Maximum relative stage change: " + MaxRelStageChange + " m/m", logIndentLevel);
            log.AddInfo("Maximum absolute stage change: " + MaxAbsStageChange + " m", logIndentLevel);
            log.AddInfo("HydraulicResistanceMinValue: " + HydraulicResistanceMinValue + " m", logIndentLevel);
            log.AddInfo("HydraulicResistanceMaxValue: " + HydraulicResistanceMaxValue + " m", logIndentLevel);
        }
    }

    class ISGCheck : Check
    {
        public override string Abbreviation
        {
            get { return "ISG"; }
        }

        public override string Description
        {
            get { return "Checks ISG-files per model layer/timestep"; }
        }

        private ISGCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is ISGCheckSettings)
                {
                    settings = (ISGCheckSettings)value;
                }
            }
        }

        public ISGCheck()
        {
            settings = new ISGCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking " + Abbreviation + "-package ...");

                Package isgPackage = model.GetPackage(ISGPackage.DefaultKey);
                if (!IsPackageActive(isgPackage, ISGPackage.DefaultKey, log, 1))
                {
                    return;
                }

                settings.LogSettings(log, 1);
                RunISGCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        public void ConvertISGFiles(IDFPackage rivPackage, Model model, CheckResultHandler resultHandler, Log log, int logIndentLevel)
        {
            // Retrieve ISG-package
            Package isgPackage = model.GetPackage(ISGPackage.DefaultKey);

            // Process all periods
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (isgPackage.GetEntryCount(kper) > 0)
                {
                    if (model.NPER > 1)
                    {
                        log.AddInfo("Converting stress period " + kper + " " + model.RetrieveSNAME(kper) + " to RIV-files ...", logIndentLevel);
                    }
                    else
                    {
                        log.AddMessage(LogLevel.Trace, "Converting stress period " + kper + " " + model.RetrieveSNAME(kper) + " to RIV-files ...", logIndentLevel);
                    }

                    DateTime? modelStartDate = null;
                    DateTime? modelEndDate = null;
                    if (model.StartDate != null)
                    {
                        modelStartDate = model.StartDate.Value;
                        modelEndDate = model.StartDate.Value.AddDays(model.NPER);
                    }

                    // Process all specified modellayers within the current period
                    int entryCount = (int)Math.Min(isgPackage.GetEntryCount(kper), resultHandler.MaxEntryNumber) - resultHandler.MinEntryNumber + 1;
                    for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < isgPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
                    {
                        CheckManager.Instance.CheckForAbort();
                        ISGPackageFile isgPackageFile = (ISGPackageFile)isgPackage.GetPackageFile(entryIdx, 0, kper);
                        if (isgPackageFile != null)
                        {
                            if (isgPackageFile.ILAY == 0)
                            {
                                isgPackageFile.ILAY = 1;
                            }

                            int ilay = isgPackageFile.ILAY;
                            ISGFile isgFile = (ISGFile)isgPackageFile.IMODFile;
                            if (isgFile != null)
                            {
                                // Ensure ISG-segments are loaded (in case ISG-file is lazy loaded)
                                isgFile.EnsureLoadedSegments();

                                if (isgFile.Extent.Union(resultHandler.Extent) != null)
                                {
                                    log.AddInfo("Converting entry " + (entryIdx + 1) + ": " + Path.GetFileName(isgFile.Filename) + " ...", logIndentLevel + 1);
                                    if (model.StartDate == null)
                                    {
                                        log.AddInfo("Converting ISG-file " + Path.GetFileName(isgFile.Filename) + " for averageperiod ...", logIndentLevel + 2);
                                        string resultPath = FileUtils.EnsureFolderExists(Path.Combine(Path.Combine(Path.Combine(resultHandler.OutputPath, "ISG-grids"), "avg"), Path.GetFileName(isgFile.Filename)));
                                        ConvertToRIV(isgPackageFile, resultPath, false, null, null, resultHandler.Extent, rivPackage, entryCount, log, logIndentLevel + 2);
                                    }
                                    else
                                    {
                                        // Retrieve date of current stress period
                                        StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                                        if (isgPackageFile.StressPeriod != null)
                                        {
                                            // use defined stress period from ISG-file
                                            stressPeriod = isgPackageFile.StressPeriod;
                                        }
                                        DateTime sdate = stressPeriod.DateTime.Value;
                                        string SNAME = stressPeriod.SNAME;

                                        log.AddInfo("Converting ISG-file " + Path.GetFileName(isgFile.Filename) + " for stress period " + sdate + " ...", logIndentLevel + 2);
                                        string resultPath = FileUtils.EnsureFolderExists(Path.Combine(Path.Combine(Path.Combine(resultHandler.OutputPath, "ISG-grids"), SNAME), Path.GetFileName(isgFile.Filename)));
                                        ConvertToRIV(isgPackageFile, resultPath, true, sdate, sdate, resultHandler.Extent, rivPackage, entryCount, log, logIndentLevel + 2);
                                    }
                                }
                            }
                            else
                            {
                                log.AddInfo("ISG-file has extent outside checked extent and is skipped: " + isgFile.Filename, logIndentLevel + 1);
                            }
                        }
                        else
                        {
                            if (kper != 0)
                            {
                                log.AddInfo("ISG-file is missing for KPER: " + kper, logIndentLevel + 1);
                            }
                            else
                            {
                                // skip missing ISG-file for this kper (which may be the case for kper=0)
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts ISG-packagefile to IDF
        /// </summary>
        /// <param name="isgPackageFile"></param>
        /// <param name="resultPath"></param>
        /// <param name="usePeriod"></param>
        /// <param name="sdate"></param>
        /// <param name="edate"></param>
        /// <param name="extent">if null the modelextent is used</param>
        /// <param name="package"></param>
        /// <param name="entryCount"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected void ConvertToRIV(ISGPackageFile isgPackageFile, string resultPath, bool usePeriod, DateTime? sdate, DateTime? edate, Extent extent, Package package, int entryCount, Log log, int logIndentLevel = 0)
        {
            ISGFile isgFile = isgPackageFile.ISGFile;
            string batchFilename = Path.Combine(resultPath, Path.GetFileNameWithoutExtension(isgFile.Filename) + "-conversion" + ".INI");
            FileUtils.EnsureFolderExists(batchFilename);

            StressPeriod startStressPeriod = package.Model.RetrieveStressPeriod(sdate);
            StressPeriod endStressPeriod = package.Model.RetrieveStressPeriod(edate);
            string postFix = "_" + Path.GetFileNameWithoutExtension(isgFile.Filename) + ((startStressPeriod != null) ? startStressPeriod.SNAME : string.Empty);

            if (extent == null)
            {
                extent = package.Model.GetExtent();
            }

            string sdateString = sdate.Value.ToString("yyyyMMdd");
            string edateString = edate.Value.ToString("yyyyMMdd");
            string periodString = sdateString + "-" + edateString;

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(batchFilename);
                sw.WriteLine("FUNCTION=ISGGRID");
                sw.WriteLine("ISGFILE_IN=\"" + isgFile.Filename + "\"");
                sw.WriteLine("CELL_SIZE=25.0");
                sw.WriteLine("NODATA=-9999.0");
                sw.WriteLine("WINDOW=" + extent.llx + "," + extent.lly + "," + extent.urx + "," + extent.ury);
                sw.WriteLine("POSTFIX=" + postFix);
                if (usePeriod)
                {
                    sw.WriteLine("IPERIOD=2");
                    sw.WriteLine("SDATE=" + sdateString);
                    sw.WriteLine("EDATE=" + edateString);
                }
                else
                {
                    sw.WriteLine("IPERIOD=1");
                }
                sw.WriteLine("ICDIST=0"); // obligatory line for iMOD 4.4
                sw.WriteLine("ISAVE=1,1,1,1,0,0,0,0,0,0,0,0");
                sw.WriteLine("OUTPUTFOLDER=\"" + resultPath + "\"");
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create INI-file for ISG-conversion", ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                }
            }

            int exitCode = IMODTool.StartBatchFunction(batchFilename, log, logIndentLevel, 0); // wait until finished
            if (exitCode == 0)
            {
                int entryIdx = package.GetEntryCount(isgPackageFile.StressPeriod.KPER);
                for (int partIdx = 0; partIdx < package.MaxPartCount; partIdx++)
                {
                    string partFilename = Path.Combine(resultPath, package.PartAbbreviations[partIdx] + postFix + ".IDF");
                    package.AddFile(isgPackageFile.ILAY, isgPackageFile.FCT, isgPackageFile.IMP, partFilename, entryIdx, partIdx, isgPackageFile.StressPeriod);
                }
            }
            else
            {
                log.AddError("Unknown error: MOD did not finish ISG-conversion for " + isgFile.Filename + ". ISG-check is skipped.");
            }
        }

        protected virtual void RunISGCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            ///////////////////////
            // Retrieve Packages //
            ///////////////////////

            // Retrieve ISG-package
            Package isgPackage = model.GetPackage(ISGPackage.DefaultKey);

            // Retrieve TOP-/BOT- and KDW- or kHV-package(s)
            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);
            IDFPackage kdwPackage = (IDFPackage)model.GetPackage(KDWPackage.DefaultKey);
            IDFPackage khvPackage = (IDFPackage)model.GetPackage(KHVPackage.DefaultKey);
            bool hasTOPPackage = model.HasActivePackage(TOPPackage.DefaultKey);
            bool hasBOTPackage = model.HasActivePackage(BOTPackage.DefaultKey);
            bool hasKDWPackage = model.HasActivePackage(KDWPackage.DefaultKey);
            bool hasKHVPackage = model.HasActivePackage(KHVPackage.DefaultKey);
            if ((hasKDWPackage && hasKHVPackage) || (!hasKDWPackage && !hasKHVPackage))
            {
                log.AddInfo("ISG-package is active, but KDW or KHV-package is not active...", 1);
            }
            if ((!hasTOPPackage || !hasBOTPackage))
            {
                log.AddInfo("ISG-package is active, but TOP and BOT-packages are not active...", 1);
            }

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////
            float resultCellSize = settings.ResultCellSize;
            float maxWinterHighRatio = settings.MaxWinterHighRatio;
            float minWinterLowDifference = settings.MinWinterLowDifference;
            int minWinterCheckCPCount = settings.MinWinterCheckCPCount;

            // Define errors (Currently no errors are defined)

            // Define warnings
            CheckWarning SuspectLowWinterLevelWarning = CreateCheckWarning("Suspect low winterlevel", "ISG-Segment calculation point has suspect lower winter level than summer level");
            CheckWarning SuspectHighWinterLevelWarning = CreateCheckWarning("Suspect high winterlevel", "ISG-Segment calculation point has suspect higher winter level than summer level");
            CheckWarning WaterLevelChangeWarning = CreateCheckWarning("Unexpected change in waterlevel", "Change in waterlevel over segment or between connected segments is larger than maximum defined change");
            CheckWarning BottomLevelChangeWarning = CreateCheckWarning("Unexpected change in bottomlevel", "Change in bottomlevel over segment or between connected segments is larger than maximum defined change");

            IDFLegend warningLegend = CreateIDFLegend();
            warningLegend.AddClass(SuspectLowWinterLevelWarning.CreateLegendValueClass(Color.Violet, true));
            warningLegend.AddClass(SuspectHighWinterLevelWarning.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddClass(WaterLevelChangeWarning.CreateLegendValueClass(Color.Blue, true));
            warningLegend.AddClass(BottomLevelChangeWarning.CreateLegendValueClass(Color.Brown, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile maxRelStageChangeSettingIDFFile = settings.GetIDFFile(settings.MaxRelStageChange, log, 1);
            IDFFile maxAbsStageChangeSettingIDFFile = settings.GetIDFFile(settings.MaxAbsStageChange, log, 1);

            // Process all periods
            Dictionary<string, int> checkedISGFiles = new Dictionary<string, int>();
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (isgPackage.GetEntryCount(kper) > 0)
                {
                    StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                    if (model.NPER > 1)
                    {
                        log.AddInfo("Checking stress period " + kper + " " + stressPeriod.SNAME + "...", 1);
                    }
                    else
                    {
                        log.AddInfo("Checking stress period " + kper + " " + stressPeriod.SNAME + "...", 1);
                    }

                    DateTime? modelStartDate = null;
                    DateTime? modelEndDate = null;
                    if (model.StartDate != null)
                    {
                        modelStartDate = model.StartDate.Value;
                        modelEndDate = model.StartDate.Value.AddDays(model.NPER);
                    }

                    // Process all specified modellayers within the current period
                    for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < isgPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
                    {
                        PackageFile isgPackageFile = isgPackage.GetPackageFile(entryIdx, 0, kper);
                        int ilay = (isgPackageFile != null) ? isgPackageFile.ILAY : -1;

                        ISGFile isgFile = (ISGFile)isgPackageFile.IMODFile;
                        // Ensure file exists 
                        if (isgFile != null)
                        {
                            // Avoid checking a file again
                            if (checkedISGFiles.ContainsKey(isgFile.Filename))
                            {
                                int checkedILay = checkedISGFiles[isgFile.Filename];
                                if (checkedILay.Equals(ilay))
                                {
                                    // File has been checked before with this ilay, don't check again
                                }
                                else
                                {
                                    // File has been checked before with another ilay
                                    log.AddError(isgPackage.Key, isgFile.Filename, "ISG-file for ilay " + ilay + " has been assigned before to another ilay (" + checkedILay + "), skipped: " + isgFile.Filename, 1);
                                }
                            }
                            else
                            {
                                log.AddInfo("Checking entry " + (entryIdx + 1) + ": " + isgFile.Filename + " ...", 1);
                                checkedISGFiles.Add(isgFile.Filename, ilay);
                                log.AddInfo("Building ISG-network for " + isgFile.Segments.Count() + " segments ...", 2);
                                ISGNetwork isgNetwork = new ISGNetwork(isgFile);
                                isgNetwork.BuildNetwork();

                                List<IMODFile> sourceFiles = new List<IMODFile> { isgFile };

                                // Create warning IDFfiles for current entry
                                CheckWarningLayer isgWarningLayer = CreateWarningLayer(resultHandler, isgPackage, "SYS" + (entryIdx + 1), stressPeriod, entryIdx + 1, settings.ResultCellSize, warningLegend);
                                isgWarningLayer.AddSourceFiles(sourceFiles);

                                // Create temporary IDF-files for storing possible warnings
                                IDFFile idfFile = ((IDFFile)isgWarningLayer.ResultFile);
                                IDFFile winterCheckIDFFile = idfFile.CopyIDF(string.Empty, false);
                                winterCheckIDFFile.ResetValues();

                                // Create IPF detail layer to store problem locations in detail
                                // IPFFile isgDetailsIPFFile = new IPFFile();
                                // isgDetailsIPFFile.Filename = Path.Combine(Path.GetDirectoryName(isgWarningLayer.ResultFile.Filename), Path.GetFileNameWithoutExtension(isgWarningLayer.ResultFile.Filename) + ".IPF");
                                IPFLegend ipfDetailLegend = IPFLegend.CreateLegend("ISGDetailLegend", "iMODValidator warnings for: " + isgFile.Filename + "Supect winter levels", Color.DarkOrange);
                                ipfDetailLegend.SelectedLabelColumns = new List<int>() { 6, 7 };
                                CheckDetailLayer isgDetailLayer = CreateDetailLayer(resultHandler, isgPackage, "SYS" + (entryIdx + 1), stressPeriod, entryIdx + 1, ipfDetailLegend);

                                // Create temporary IPFPoint-list for storing details about possible warnings: columns contain: "Warningmessage", "SegmentID", "CalculationpointID"
                                List<IPFPoint> tmpDetailPoints = new List<IPFPoint>();

                                bool hasWinterHighMatch = !settings.WinterHighFNameStrings.Equals(string.Empty) && HasMatch(isgFile.Filename, settings.WinterHighFNameStrings);
                                bool hasWinterLowMatch = !settings.WinterLowFNameStrings.Equals(string.Empty) && HasMatch(isgFile.Filename, settings.WinterLowFNameStrings) && !hasWinterHighMatch;

                                // Process all segments for the current ISG-file
                                log.AddInfo("Start checking segments ...", 2);
                                long highWinterLevelSegmentCount = 0;
                                long lowWinterLevelSegmentCount = 0;
                                foreach (ISGSegment isgSegment in isgFile.Segments)
                                {
                                    try
                                    {
                                        CheckManager.Instance.CheckForAbort();

                                        float x = 0; // isgSegment.XValue;
                                        float y = 0; // isgSegment.YValue;

                                        // Check if location is inside modelling area
                                        if (isgSegment.HasOverlap(isgWarningLayer.ResultFile.Extent))
                                        {
                                            float maxRelStageChange = (maxRelStageChangeSettingIDFFile != null) ? maxRelStageChangeSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;
                                            float maxAbsStageChange = (maxAbsStageChangeSettingIDFFile != null) ? maxAbsStageChangeSettingIDFFile.GetNaNBasedValue(x, y) : float.NaN;

                                            //////////////////////
                                            // Do actual checks //
                                            //////////////////////

                                            // Check for changes in waterlevel along segment and its neighbour-segments
                                            CheckSegmentChanges(isgNetwork, isgSegment, maxRelStageChange, maxAbsStageChange, resultHandler, isgWarningLayer, isgDetailLayer, WaterLevelChangeWarning, BottomLevelChangeWarning, modelStartDate, modelEndDate, log, 2);

                                            if (hasWinterHighMatch)
                                            {
                                                // Check that average winter-waterlevels are higher than average summer-waterlevels
                                                CheckWinterLevels(isgFile, isgSegment, false, minWinterLowDifference, winterCheckIDFFile, tmpDetailPoints, ref highWinterLevelSegmentCount, ref lowWinterLevelSegmentCount, log, 2, modelStartDate, modelEndDate);
                                            }
                                            else if (hasWinterLowMatch)
                                            {
                                                // Check that average winter-waterlevels are lower than average summer-waterlevels
                                                CheckWinterLevels(isgFile, isgSegment, true, 0, winterCheckIDFFile, tmpDetailPoints, ref highWinterLevelSegmentCount, ref lowWinterLevelSegmentCount, log, 2, modelStartDate, modelEndDate);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Could not process ISG-entry " + isgSegment.ToString() + " in ISG-file: " + isgFile.Filename, ex);
                                    }
                                }

                                if (hasWinterLowMatch)
                                {
                                    if (tmpDetailPoints.Count > minWinterCheckCPCount)
                                    {
                                        if (highWinterLevelSegmentCount > (maxWinterHighRatio * isgFile.SegmentCount))
                                        {
                                            resultHandler.AddIDFCheckResult(isgWarningLayer, winterCheckIDFFile, SuspectHighWinterLevelWarning);
                                            foreach (IPFPoint ipfPoint in tmpDetailPoints)
                                            {
                                                // Add check detail with: message and segment/cp id
                                                resultHandler.AddCheckDetail(isgDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, 
                                                    new CheckDetail(SuspectHighWinterLevelWarning, isgFile, ipfPoint.ColumnValues[2], ipfPoint.ColumnValues[3] + "; " + ipfPoint.ColumnValues[4], float.NaN));
                                            }

                                            log.AddWarning(isgPackage.Key, isgFile.Filename, "Average winterlevel is more often higher than average summerlevel", 2);
                                        }
                                    }
                                }
                                else if (hasWinterHighMatch)
                                {
                                    if (tmpDetailPoints.Count > minWinterCheckCPCount)
                                    {
                                        resultHandler.AddIDFCheckResult(isgWarningLayer, winterCheckIDFFile, SuspectLowWinterLevelWarning);
                                        foreach (IPFPoint ipfPoint in tmpDetailPoints)
                                        {
                                            // Add check detail with: message and segment/cp id
                                            resultHandler.AddCheckDetail(isgDetailLayer, (float)ipfPoint.X, (float)ipfPoint.Y, 
                                                new CheckDetail(SuspectLowWinterLevelWarning, isgFile, ipfPoint.ColumnValues[2], ipfPoint.ColumnValues[3] + "; " + ipfPoint.ColumnValues[4], float.NaN));
                                        }

                                        log.AddWarning(isgPackage.Key, isgFile.Filename, "Average winterlevels found that are lower than average summerlevel", 2);
                                    }
                                }

                                // Write warningfiles
                                if (isgWarningLayer.HasResults())
                                {
                                    isgWarningLayer.CompressLegend(CombinedResultLabel);
                                    isgWarningLayer.WriteResultFile(log);

                                    if (isgDetailLayer.HasResults())
                                    {
                                        isgDetailLayer.WriteResultFile(log);
                                    }

                                    resultHandler.AddExtraMapFiles(isgWarningLayer.SourceFiles);
                                }

                                isgFile.ReleaseMemory(false);
                                isgWarningLayer.ReleaseMemory(true);
                            }
                        }
                    }
                }
            }

            settings.ReleaseMemory(true);
            isgPackage.ReleaseMemory(true);
        }

        private void AddWarningDetailPoints(CheckResultHandler resultHandler, CheckDetailLayer isgDetailLayer, IPFFile tmpSelectionIPFFile, CheckWarning checkWarning)
        {
            foreach (IPFPoint ipfPoint in tmpSelectionIPFFile.Points)
            {
                resultHandler.AddCheckDetail(isgDetailLayer, (float) ipfPoint.X, (float)ipfPoint.Y, new CheckDetail(checkWarning, null, ipfPoint.ColumnValues[0], null, float.NaN));
            }
            // isgDetailsIPFFile.AddPoints(tmpSelectionIPFFile.Points);
        }

        private bool HasMatch(string filename, string patterns)
        {
            string[] winterLowFnames = patterns.Split(new char[] { ',' });

            foreach (string fnameString in winterLowFnames)
            {
                Match match = Regex.Match(filename, fnameString, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckSegmentChanges(ISGNetwork isgNetwork, ISGSegment segment, float maxRelChange, float maxAbsChange, CheckResultHandler resultHandler, CheckWarningLayer isgWarningLayer, CheckDetailLayer isgDetailLayer, CheckWarning WaterLevelChangeWarning, CheckWarning BottomLevelChangeWarning, DateTime? modelStartDate, DateTime? modelEndDate, Log log, int logIndentLevel = 0)
        {
            // Retrieve connected nodes at the start of each segment. 
            ISGNode node = segment.Nodes[0];
            List<ISGNode> connectedNodes = isgNetwork.GetNodes(node.X, node.Y);
            if (connectedNodes.Count() > 1)
            {
                foreach (ISGNode otherNode in connectedNodes)
                {
                    if (!otherNode.ISGSegment.Label.Equals(node.ISGSegment.Label))
                    {
                        CheckLevelChange(isgNetwork.ISGFile, node, otherNode, maxRelChange, maxAbsChange, resultHandler, isgWarningLayer, isgDetailLayer, WaterLevelChangeWarning, BottomLevelChangeWarning, modelStartDate, modelEndDate, log, logIndentLevel);
                    }
                }
            }

            // Check internal levelchanges
            ISGCalculationPoint cp = segment.CalculationPoints[0];
            ISGCalculationPoint nextCP = null;
            for (int cpIdx = 1; cpIdx < segment.CalculationPoints.Count() - 1; cpIdx++)
            {
                nextCP = segment.CalculationPoints[cpIdx];
                CheckLevelChange(isgNetwork.ISGFile, segment, segment, cp, nextCP, maxRelChange, maxAbsChange, resultHandler, isgWarningLayer, isgDetailLayer, WaterLevelChangeWarning, BottomLevelChangeWarning, modelStartDate, modelEndDate, log, logIndentLevel);
                cp = nextCP;
            }

            // Retrieve connected nodes at the end of the segment. 
            node = segment.Nodes[segment.Nodes.Count() - 1];
            connectedNodes = isgNetwork.GetNodes(node.X, node.Y);
            if (connectedNodes.Count() > 1)
            {
                foreach (ISGNode otherNode in connectedNodes)
                {
                    if (!otherNode.ISGSegment.Label.Equals(node.ISGSegment.Label))
                    {
                        CheckLevelChange(isgNetwork.ISGFile, node, otherNode, maxRelChange, maxAbsChange, resultHandler, isgWarningLayer, isgDetailLayer, WaterLevelChangeWarning, BottomLevelChangeWarning, modelStartDate, modelEndDate, log, logIndentLevel);
                    }
                }
            }
        }

        /// <summary>
        /// Check for unexpected level change between specified segment and other segment.
        /// </summary>
        /// <param name="isgFile"></param>
        /// <param name="segment"></param>
        /// <param name="otherSegment"></param>
        /// <param name="cp"></param>
        /// <param name="otherCP"></param>
        /// <param name="maxRelChange"></param>
        /// <param name="maxAbsChange"></param>
        /// <param name="resultHandler"></param>
        /// <param name="isgWarningLayer"></param>
        /// <param name="isgDetailLayer"></param>
        /// <param name="WaterLevelChangeWarning"></param>
        /// <param name="BottomLevelChangeWarning">Note: bottom is currently not checked!</param>
        /// <param name="modelStartDate"></param>
        /// <param name="modelEndDate"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        private void CheckLevelChange(ISGFile isgFile, ISGSegment segment, ISGSegment otherSegment, ISGCalculationPoint cp, ISGCalculationPoint otherCP, float maxRelChange, float maxAbsChange, CheckResultHandler resultHandler, CheckWarningLayer isgWarningLayer, CheckDetailLayer isgDetailLayer, CheckWarning WaterLevelChangeWarning, CheckWarning BottomLevelChangeWarning, DateTime? modelStartDate, DateTime? modelEndDate, Log log, int logIndentLevel = 0)
        {
            try
            {
                // Check change of level for all defined dates within specified modelperiod

                // find first date in each calculation point
                float maxChange = maxRelChange;
                Timeseries ts = cp.GetWaterlevelTimeseries(modelStartDate, modelEndDate);
                Timeseries otherTS = otherCP.GetWaterlevelTimeseries(modelStartDate, modelEndDate);
                ISGCoordinate cpCoordinate = segment.GetCoordinate(cp.DIST);
                ISGCoordinate otherCPCoordinate = otherSegment.GetCoordinate(otherCP.DIST);
                if (cpCoordinate == null)
                {
                    log.AddError(ISGPackage.DefaultKey, null, "ISG-segment " + segment.Label + ": distance of calculationpoint " + cp.CNAME + " (" + cp.DIST + ") is larger than length of segment (" + segment.GetLength() + "), using last segment coordinate");
                    cpCoordinate = segment.Nodes[segment.Nodes.Count() - 1];
                }
                if (otherCPCoordinate == null)
                {
                    log.AddError(ISGPackage.DefaultKey, isgFile.Filename, "ISG-segment " + otherSegment.Label + ": distance of calculationpoint " + otherCP.CNAME + " (" + otherCP.DIST + ") is larger than length of segment (" + otherSegment.GetLength() + "), using last segment coordinate");
                    otherCPCoordinate = otherSegment.Nodes[otherSegment.Nodes.Count() - 1];
                }
                float dX = (cpCoordinate.X - otherCPCoordinate.X);
                float dY = (cpCoordinate.Y - otherCPCoordinate.Y);
                float realDistance = (float)Math.Sqrt(dX * dX + dY * dY);
                float calcDistance = realDistance;
                if (calcDistance < ISGFile.DistanceErrorMargin)
                {
                    // For points that lie at the same coordinate, assume distance 1
                    calcDistance = 1f;
                    maxChange = maxAbsChange;
                }
                if ((ts.Timestamps != null) && (otherTS.Timestamps != null))
                {
                    int otherDateIdx = 0;
                    for (int dateIdx = 0; dateIdx < ts.Timestamps.Count(); dateIdx++)
                    {
                        DateTime date = ts.Timestamps[dateIdx];
                        // Find last date in other timeseries that is smaller or equal to the current date in the first timeseries
                        while (((otherDateIdx + 1) < otherTS.Timestamps.Count()) && (otherTS.Timestamps[otherDateIdx + 1] <= date))
                        {
                            otherDateIdx++;
                        }
                        float waterlevel = ts.Values[dateIdx];
                        float otherWaterlevel = otherTS.Values[otherDateIdx];
                        float levelChange = Math.Abs(waterlevel - otherWaterlevel) / calcDistance;
                        if (levelChange > maxChange)
                        {
                            ISGCoordinate warningCoordinate = segment.GetCoordinate(cp.DIST);
                            if (warningCoordinate == null)
                            {
                                log.AddError(ISGPackage.DefaultKey, isgFile.Filename, "ISG-segment " + segment.Label + ": distance of calculationpoint " + cp.CNAME + " (" + cp.DIST + ") is larger than length of segment (" + segment.GetLength() + "), using last segment coordinate");
                                warningCoordinate = segment.Nodes[segment.Nodes.Count() - 1];
                            }

                            string segmentString = segment.Label;
                            string cpString = cp.CNAME;
                            if (otherSegment != segment)
                            {
                                segmentString += "-" + otherSegment.Label;
                                cpString += "-" + otherCP.CNAME;
                            }
                            cpString = cpString.Replace(" ", "");

                            // Add point to details-IPFfile if not yet present
                            string detailMessage;
                            if (realDistance < ISGFile.DistanceErrorMargin)
                            {
                                detailMessage = "\"WLVL-change at " + date.ToShortDateString() + ": "
                                + waterlevel.ToString("F2", SIFTool.EnglishCultureInfo) + " - " + otherWaterlevel.ToString("F2", SIFTool.EnglishCultureInfo) + "\"";
                            }
                            else
                            {
                                detailMessage = "\"WLVL-change at " + date.ToShortDateString() + ": "
                                + waterlevel.ToString("F2", SIFTool.EnglishCultureInfo) + " - " + otherWaterlevel.ToString("F2", SIFTool.EnglishCultureInfo) + " over " + realDistance + "m distance (= " + levelChange + "m/m)\"";
                            }
                            string[] columnValues = new string[] { warningCoordinate.X.ToString("F3", SIFTool.EnglishCultureInfo), warningCoordinate.Y.ToString("F3", SIFTool.EnglishCultureInfo),
                                detailMessage, segmentString, cpString};
                            CheckDetail detail = new CheckDetail(WaterLevelChangeWarning, isgFile, detailMessage, segmentString + "; " + cpString, float.NaN);

                            isgDetailLayer.AddCheckDetail(warningCoordinate.X, warningCoordinate.Y, detail);

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.GetBaseException().Message;
                log.AddWarning(ISGPackage.DefaultKey, isgFile.Filename, "Could not check levelchange for segment " + segment.Label + ", cp '" + cp.CNAME + "' and other segment " + otherSegment.Label + ", cp '" + otherCP.CNAME + "'" + s, logIndentLevel);
            }
        }

        private void CheckLevelChange(ISGFile isgFile, ISGNode node, ISGNode otherNode, float maxRelChange, float maxAbsChange, CheckResultHandler resultHandler, CheckWarningLayer isgWarningLayer, CheckDetailLayer isgDetailLayer, CheckWarning WaterLevelChangeWarning, CheckWarning BottomLevelChangeWarning, DateTime? modelStartDate, DateTime? modelEndDate, Log log, int logIndentLevel = 0)
        {
            ISGSegment segment = node.ISGSegment;
            ISGSegment otherSegment = otherNode.ISGSegment;

            try
            {
                // Find closest calculationpoint on each segment
                float segmentDistance = node.CalculateDistance();
                float otherSegmentDistance = otherNode.CalculateDistance();
                ISGCalculationPoint cp = segment.GetCalculationPoint(segmentDistance);
                ISGCalculationPoint otherCP = otherSegment.GetCalculationPoint(otherSegmentDistance);

                CheckLevelChange(isgFile, segment, otherSegment, cp, otherCP, maxRelChange, maxAbsChange, resultHandler, isgWarningLayer, isgDetailLayer, WaterLevelChangeWarning, BottomLevelChangeWarning, modelStartDate, modelEndDate, log, logIndentLevel);
            }
            catch (Exception ex)
            {
                string s = ex.GetBaseException().Message;
                log.AddWarning(ISGPackage.DefaultKey, isgFile.Filename, "Could not check levelchange for segment " + segment.Label + " and other segment " + otherSegment.Label, logIndentLevel);
            }
        }

        /// <summary>
        /// Check if specified segment has more calculation points with higher than lower (average) winterlevel or vice versa. 
        /// </summary>
        /// <param name="isgFile"></param>
        /// <param name="isgSegment"></param>
        /// <param name="checkSuspectHighWinter">if true, specified segment is checked for suspect high winter levels</param>
        /// <param name="minDifference"></param>
        /// <param name="isgWarningIDFFile">IDF-file to store cells with suspect points</param>
        /// <param name="tmpDetailPoint">list to store IPF-points with suspect points</param>
        /// <param name="highWinterLevelSegmentCount"></param>
        /// <param name="lowWinterLevelSegmentCount"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <param name="modelStartDate"></param>
        /// <param name="modelEndDate"></param>
        private void CheckWinterLevels(ISGFile isgFile, ISGSegment isgSegment, bool checkSuspectHighWinter, float minDifference, IDFFile isgWarningIDFFile, List<IPFPoint> detailSuspectPoints, ref long highWinterLevelSegmentCount, ref long lowWinterLevelSegmentCount, Log log, int logIndentlevel = 0, DateTime? modelStartDate = null, DateTime? modelEndDate = null)
        {
            // Check all calculation points for this segment and count points with high/low winterlevel
            int highWinterLevelCPCount = 0;
            int lowWinterLevelCPCount = 0;

            IPFFile tmpWarningIPFFile = new IPFFile();
            tmpWarningIPFFile.AddXYColumns();
            tmpWarningIPFFile.AddColumns(new List<string>() { "Message", "SegmentID", "CpID" });

            Extent selectionExtent = isgWarningIDFFile.Extent;
            foreach (ISGCalculationPoint isgCalcPoint in isgSegment.CalculationPoints)
            {
                ISGCoordinate cpCoordinate = isgSegment.GetCoordinate(isgCalcPoint.DIST);
                if (cpCoordinate == null)
                {
                    log.AddError(ISGPackage.DefaultKey, isgFile.Filename, "ISG-segment " + isgSegment.Label + ": distance of calculationpoint " + isgCalcPoint.CNAME + " (" + isgCalcPoint.DIST + ") is larger than length of segment (" + isgSegment.GetLength() + "), using last segment coordinate");
                    cpCoordinate = isgSegment.Nodes[isgSegment.Nodes.Count() - 1];
                }

                Point cpPoint = (Point)new FloatPoint(cpCoordinate.X.ToString("F3", EnglishCultureInfo), cpCoordinate.Y.ToString("F3", EnglishCultureInfo));
                if (cpPoint.IsContainedBy(selectionExtent))
                {
                    Timeseries waterlevelTS = isgCalcPoint.GetWaterlevelTimeseries(modelStartDate, modelEndDate);
                    if (waterlevelTS != null)
                    {
                        Timeseries equidistantWaterlevelTS = waterlevelTS.InterpolateTimeseries();
                        Timeseries winterTS = equidistantWaterlevelTS.Select(10, 1, 3, 31);
                        Timeseries summerTS = equidistantWaterlevelTS.Select(4, 1, 9, 30);
                        Statistics.Statistics winterStats = new Statistics.Statistics(winterTS.Values);
                        Statistics.Statistics summerStats = new Statistics.Statistics(summerTS.Values);
                        winterStats.ComputeBasicStatistics(false);
                        summerStats.ComputeBasicStatistics(false);

                        float avgWinterLevel = (float)Math.Round(winterStats.Mean, 2);
                        float avgSummerLevel = (float)Math.Round(summerStats.Mean, 2);
                        if ((avgWinterLevel - minDifference) > avgSummerLevel)
                        {
                            highWinterLevelCPCount++;

                            if (checkSuspectHighWinter)
                            {
                                AddSuspectCP(tmpWarningIPFFile, isgFile, isgSegment, isgCalcPoint, cpPoint, 
                                    "suspect high winterlevel ("+ avgWinterLevel.ToString("F3", EnglishCultureInfo) + " > " + avgSummerLevel.ToString("F3", EnglishCultureInfo) + ")", log);
                            }
                        }
                        else if ((avgSummerLevel - minDifference) > avgWinterLevel)
                        {
                            lowWinterLevelCPCount++;

                            if (!checkSuspectHighWinter)
                            {
                                AddSuspectCP(tmpWarningIPFFile, isgFile, isgSegment, isgCalcPoint, cpPoint,
                                    "suspect low winterlevel (" + avgWinterLevel.ToString("F3", EnglishCultureInfo) + " < " + avgSummerLevel.ToString("F3", EnglishCultureInfo) + ")", log);
                            }
                        }
                    }
                }
            }

            // If suspect, store suspect points, depending on type of check
            if (checkSuspectHighWinter)
            {
                // Check if more (suspect) high than low CP's where found for this segment
                if (highWinterLevelCPCount > lowWinterLevelCPCount)
                {
                    highWinterLevelSegmentCount++;
                    AddSuspectSegmentPoints(tmpWarningIPFFile, detailSuspectPoints, isgWarningIDFFile);
                }
                else
                {
                    lowWinterLevelSegmentCount++;
                }
            }
            else
            {
                // Check if any (suspect) low CP's where found for this segment
                if (lowWinterLevelCPCount > 0)
                {
                    // One or more low winter lvels were found
                    lowWinterLevelSegmentCount++;
                    AddSuspectSegmentPoints(tmpWarningIPFFile, detailSuspectPoints, isgWarningIDFFile);
                }
                else
                {
                    // All winter levels are higher than summer levels (or high enough)
                    highWinterLevelSegmentCount++;
                }
            }
        }

        /// <summary>
        /// Add possible suspect points as suspect points to IPF-file (pointlist) and IDF-file
        /// </summary>
        /// <param name="tmpWarningIPFFile"></param>
        /// <param name="detailSuspectPoints"></param>
        /// <param name="isgWarningIDFFile"></param>
        private void AddSuspectSegmentPoints(IPFFile tmpWarningIPFFile, List<IPFPoint> detailSuspectPoints, IDFFile isgWarningIDFFile)
        {
            foreach (IPFPoint ipfPoint in tmpWarningIPFFile.Points)
            {
                isgWarningIDFFile.AddValue((float)ipfPoint.X, (float)ipfPoint.Y, 1);
                detailSuspectPoints.Add(ipfPoint);
            }
        }

        /// <summary>
        /// Add specified calculation point to temporary point list for specified IPF-file and ISG-file
        /// </summary>
        /// <param name="tmpWarningIPFFile"></param>
        /// <param name="isgFile"></param>
        /// <param name="isgSegment"></param>
        /// <param name="isgCalcPoint"></param>
        /// <param name="warningPoint"></param>
        /// <param name="warningString"></param>
        /// <param name="log"></param>
        private void AddSuspectCP(IPFFile tmpWarningIPFFile, ISGFile isgFile, ISGSegment isgSegment, ISGCalculationPoint isgCalcPoint, Point warningPoint, string warningString, Log log)
        {
            tmpWarningIPFFile.AddPoint(new IPFPoint(tmpWarningIPFFile, warningPoint, new string[] { CommonUtils.EnsureDoubleQuotes(warningString), isgSegment.Label, isgCalcPoint.CNAME }));
        }
    }
}
