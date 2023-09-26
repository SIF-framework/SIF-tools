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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IMF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Settings;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// Keeps track of validator results of type Result per layer/sytem of type ResultLayer. 
    /// For each layertype (as defined by the field ResultLayer.ResultType) a summarylayer is kept
    /// This class can create a corresponding iMOD IMF file with all results. 
    /// For each added layer with a legend property the presentation in iMOD can be be defined.
    /// </summary>
    public abstract class ResultHandler
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        protected Dictionary<string, SummaryResultLayer> summaryLayerDictionary;
        protected Dictionary<string, List<ResultLayer>> resultlayersDictionary;
        //        protected Dictionary<string, Statistics> statisticsDictionary;
        protected List<IMODFile> extraMapFiles;

        protected string toolVersion;
        public string ToolVersion
        {
            get { return toolVersion; }
            set { toolVersion = value; }
        }

        protected Model model;
        public Model Model
        {
            get { return model; }
            set { model = value; }
        }

        protected string baseModelFilename;
        public string BaseModelFilename
        {
            get { return baseModelFilename; }
            set { baseModelFilename = value; }
        }

        protected float noDataValue;
        public float NoDataValue
        {
            get { return noDataValue; }
            set { noDataValue = value; }
        }
        protected Extent extent;

        public Extent Extent
        {
            get { return extent; }
            set { extent = value; }
        }
        protected float levelErrorMargin;
        public float LevelErrorMargin
        {
            get { return levelErrorMargin; }
            set { levelErrorMargin = value; }
        }
        protected int minKPER;
        /// <summary>
        /// Minimum KPER to check. Note: KPER 0 is used for steady-state models.
        /// </summary>
        public int MinKPER
        {
            get { return minKPER; }
            set { minKPER = value; }
        }
        protected int maxKPER;
        /// <summary>
        /// Maximum KPER to check. Note: KPER 0 is used for steady-state models, KPER 1 refers to first timestep in transient models. For steady-state models MaxKPER is reset to 0 automatically.
        /// </summary>
        public int MaxKPER
        {
            get { return maxKPER; }
            set { maxKPER = value; }
        }
        private int minILAY;
        /// <summary>
        /// Minimum number of entry to check (>= 1)
        /// </summary>
        public int MinEntryNumber
        {
            get { return minILAY; }
            set { minILAY = value; }
        }
        private int maxILAY;
        /// <summary>
        /// Maximum number of entry to check (>= 1)
        /// </summary>
        public int MaxEntryNumber
        {
            get { return maxILAY; }
            set { maxILAY = value; }
        }
        private bool useSparseGrids;
        public bool UseSparseGrids
        {
            get { return useSparseGrids; }
            set { useSparseGrids = value; }
        }
        protected float summaryMinCellsize;
        public float SummaryMinCellsize
        {
            get { return summaryMinCellsize; }
            set { summaryMinCellsize = value; }
        }
        protected string outputPath;
        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }
        protected DateTime? startDate;
        public DateTime? StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        public string Creator { get; set; }

        protected ResultHandler()
        {
        }

        /// <summary>
        /// Creates a ResultHandler object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultNoDataValue">NoData-value in resultfiles</param>
        /// <param name="checkExtent">the extent within which the check is done</param>
        /// <param name="creator">some string that identifies instance that created this object (e.g. toolname)</param>
        public ResultHandler(Model model, float resultNoDataValue, Extent checkExtent, string creator)
        {
            this.model = model;
            this.baseModelFilename = model.Runfilename;
            this.startDate = model.StartDate;
            this.outputPath = model.ToolOutputPath;
            this.noDataValue = resultNoDataValue;
            this.extent = checkExtent;
            this.Creator = creator;
            Initialize();
        }

        /// <summary>
        /// Creates a ResultHandler object
        /// </summary>
        /// <param name="modelFilename">e.g. runfilename</param>
        /// <param name="startDate">startdate or null for steady-state model</param>
        /// <param name="outputPath">the outputpath to write modelresults</param>
        /// <param name="resultNoDataValue">NoData-value in resultfiles</param>
        /// <param name="checkExtent">the extent within which the check is done</param>
        /// <param name="creator">some string that identifies instance that created this object (e.g. toolname)</param>
        public ResultHandler(string modelFilename, DateTime? startDate, string outputPath, float resultNoDataValue, Extent checkExtent, string creator)
        {
            this.model = null;
            this.baseModelFilename = modelFilename;
            this.startDate = startDate;
            this.outputPath = outputPath;
            this.noDataValue = resultNoDataValue;
            this.extent = checkExtent;
            this.Creator = creator;
            Initialize();
        }

        public void Initialize()
        {
            this.extraMapFiles = new List<IMODFile>();
            this.resultlayersDictionary = new Dictionary<string, List<ResultLayer>>();
            this.summaryLayerDictionary = new Dictionary<string, SummaryResultLayer>();
            //            this.statisticsDictionary = new Dictionary<string, Statistics>();
            // Ensure Check layers in the following order, to have results reported in this order
            this.resultlayersDictionary.Add(CheckError.TYPENAME, new List<ResultLayer>());
            this.resultlayersDictionary.Add(CheckWarning.TYPENAME, new List<ResultLayer>());
            this.resultlayersDictionary.Add(CheckDetail.TYPENAME, new List<ResultLayer>());

            if (model.IsSteadyStateModel())
            {
                this.maxKPER = 0;
            }
        }

        public virtual bool HasResultLayer(ResultLayer resultLayer)
        {
            // Check if resultLayer already is added
            if (resultlayersDictionary.ContainsKey(resultLayer.ResultType))
            {
                if (resultlayersDictionary[resultLayer.ResultType].Contains(resultLayer))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void AddResultLayer(ResultLayer resultLayer)
        {
            // First check that resultLayer is not yet existing, which indicates a programming error
            if (HasResultLayer(resultLayer))
            {
                throw new Exception("ResultLayer-object " + resultLayer.ToString() + " is already present in resultLayersDictionary, please check '" + resultLayer.Id + "'-implementation");
            }

            EnsureAddedLayer(resultLayer);
        }

        /// <summary>
        /// Adds an iMOD file to the list of extra mapfiles, if not yet present. 
        /// These extra mapfiles will be added to de IMF-project when this is created.
        /// </summary>
        /// <param name="imodFile"></param>
        public void AddExtraMapFile(IMODFile imodFile)
        {
            if (imodFile != null)
            {
                if (!(extraMapFiles.Contains(imodFile)))
                {
                    extraMapFiles.Add(imodFile);
                }
            }
        }

        /// <summary>
        /// Adds iMOD files to the list of extra mapfiles, if not yet present. 
        /// These extra mapfiles will be added to de IMF-project when this is created.
        /// </summary>
        /// <param name="imodFile"></param>
        public void AddExtraMapFiles(List<IMODFile> imodFiles)
        {
            if (imodFiles != null)
            {
                foreach (IMODFile imodFile in imodFiles)
                {
                    if (imodFile != null)
                    {
                        if (!(extraMapFiles.Contains(imodFile)))
                        {
                            extraMapFiles.Add(imodFile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes all results to summary files, an iMOD-projectfile and tables
        /// </summary>
        /// <param name="imfFilename"></param>
        /// <param name="tableFilenamePrefix"></param>
        /// <param name="noErrorMessage"></param>
        /// <param name="isIModOpened"></param>
        /// <param name="isRelativePathIMFAdded"></param>
        /// <param name="isExcelOpened"></param>
        /// <param name="log"></param>
        public virtual void WriteResults(string imfFilename, string tableFilenamePrefix, string noErrorMessage, bool isRelativePathIMFAdded, bool isIModOpened, bool isExcelOpened, Log log)
        {
            string[] selectedResultTypes = GetSelectedResulTypes();


            if ((GetTotalResultCount(selectedResultTypes) != 0) || (log.Warnings.Count > 0) || (log.Errors.Count > 0))
            {

                // Write summary results
                log.AddInfo("\r\nWriting results...");
                WriteSummaryFiles(log);

                // First calculate result, so errorfiles will be closed when opening iMOD
                ResultTable summaryResultTable = CreateSummaryTable(log);

                if (GetTotalResultCount(selectedResultTypes) != 0)
                {
                    // Now open iMOD
                    if (Path.GetDirectoryName(imfFilename).Equals(string.Empty))
                    {
                        imfFilename = Path.Combine(outputPath, imfFilename);
                    }
                    CreateIMFFile(imfFilename, log, isRelativePathIMFAdded, isIModOpened);
                }

                // Now export table file (after opening iMOD to avoid openfile-conflicts). Note file extension doesn't matter since it will be set within the export-method
                string tableFilename = Path.Combine(outputPath, iMODValidatorSettingsManager.Settings.TooloutputSubfoldername + tableFilenamePrefix + "_" + DateTime.Now.ToString("dd-MM-yyyy HH:mm").Replace(":", ".") + ".txt"); //iMODValidatorSettingsManager.Settings.TooloutputSubfoldername+
                summaryResultTable.Export(tableFilename, log, isExcelOpened);
            }
            else
            {
                log.AddInfo(noErrorMessage);
            }
            log.AddInfo(string.Empty);
        }

        protected virtual string[] GetSelectedResulTypes()
        {
            return new string[] { CheckWarning.TYPENAME, CheckError.TYPENAME };
        }

        protected ResultLayer GetResultLayer(string resultType, string id, string id2, int ilay, int kper)
        {
            if (resultlayersDictionary.ContainsKey(resultType))
            {
                foreach (ResultLayer resultLayer in resultlayersDictionary[resultType])
                {
                    if ((resultLayer.Id == null) || resultLayer.Id.Equals(id))
                    {
                        if (resultLayer.Id2.Equals(id2) && (resultLayer.ILay == ilay) && (resultLayer.KPER == kper))
                        {
                            return resultLayer;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Ensures that the given layer is stored in the resultlayers dictionary
        /// </summary>
        /// <param name="resultLayer"></param>
        protected void EnsureAddedLayer(ResultLayer resultLayer)
        {
            // Ensure a list-object has been made for this resultType
            if (!resultlayersDictionary.ContainsKey(resultLayer.ResultType))
            {
                resultlayersDictionary.Add(resultLayer.ResultType, new List<ResultLayer>());
            }

            // Add resultLayer-object to list if not yet existing
            if (!resultlayersDictionary[resultLayer.ResultType].Contains(resultLayer))
            {
                resultlayersDictionary[resultLayer.ResultType].Add(resultLayer);
            }

            // Also create a summmary-resultlayer for this type if not yet existing
            if (!summaryLayerDictionary.ContainsKey(resultLayer.ResultType))
            {
                float cellSize = GetMaxResultLayerCellSize(resultLayer.ResultType);
                if (summaryMinCellsize > cellSize)
                {
                    cellSize = summaryMinCellsize;
                }
                SummaryResultLayer summaryLayer = new SummaryResultLayer(resultLayer.KPER, resultLayer.ILay, startDate, extent, cellSize, noDataValue, outputPath);
                summaryLayer.ResultFile.Filename = Path.Combine(outputPath, Path.Combine(outputPath, "summary"), "summary_" + resultLayer.ResultType.ToLower() + "s" + ".IDF");
                summaryLayerDictionary.Add(resultLayer.ResultType, summaryLayer);
            }
        }

        protected float GetMaxResultLayerCellSize(string resultType)
        {
            float maxCellsize = 0;
            foreach (ResultLayer resultLayer in resultlayersDictionary[resultType])
            {
                if (resultLayer.ResultFile is IDFFile)
                {
                    IDFFile idfFile = (IDFFile)resultLayer.ResultFile;
                    if (!(idfFile is ConstantIDFFile) && (idfFile.XCellsize > maxCellsize))
                    {
                        maxCellsize = idfFile.XCellsize;
                    }
                    if (!(idfFile is ConstantIDFFile) && (idfFile.YCellsize > maxCellsize))
                    {
                        maxCellsize = idfFile.XCellsize;
                    }
                }
            }
            return maxCellsize;
        }

        /// <summary>
        ///  Implement this method in the subclass and, to ensure results are summarized, 
        ///  make sure it is called from the subclass when a result is added
        /// </summary>
        /// <param name="resultType"></param>
        /// <param name="someResult"></param>
        protected abstract void AddSummaryResult(string resultType, Result someResult);

        /// <summary>
        /// Write summary files for errors and warnings to disk
        /// </summary>
        /// <param name="log"></param>
        protected void WriteSummaryFiles(Log log)
        {
            // Write summary IDFFiles
            foreach (string resultType in summaryLayerDictionary.Keys)
            {
                ResultLayer summaryLayer = summaryLayerDictionary[resultType];
                if ((summaryLayer.ResultCount > 0) && (summaryLayer.ResultFile != null))
                {
                    log.AddInfo("Writing summary " + resultType + "-IDFFile " + summaryLayer.ResultFile.Filename + " ...", 1);
                    summaryLayer.ResultFile.WriteFile(CreateSummaryFileMetadata(resultType));
                }
            }
            log.AddInfo(string.Empty);
        }

        /// <summary>
        /// Deletes (old) summary files from disk
        /// </summary>
        /// <param name="log"></param>
        protected void DeleteSummaryFiles(Log log)
        {
            // Write summary IDFFiles
            foreach (string resultType in summaryLayerDictionary.Keys)
            {
                ResultLayer summaryLayer = summaryLayerDictionary[resultType];
                if ((summaryLayer.ResultFile != null) && (File.Exists(summaryLayer.ResultFile.Filename)))
                {
                    log.AddInfo("Deleting summary " + resultType + "-IDFFile " + summaryLayer.ResultFile.Filename + " ...", 1);
                    try
                    {
                        foreach (string filename in Directory.GetFiles(Path.GetDirectoryName(summaryLayer.ResultFile.Filename), Path.GetFileNameWithoutExtension(summaryLayer.ResultFile.Filename) + "*"))
                        {
                            File.Delete(filename);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.AddWarning("Could not delete summaryfile " + summaryLayer.ResultFile.Filename + ": " + ex.GetBaseException().Message);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a table with a summary of results: a row per errorfile
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual ResultTable CreateSummaryTable(Log log)
        {
            string[] selectedResultTypes = GetSelectedResulTypes();

            // Create summary table
            ResultTable summaryResultTable = CreateExcelResultSheet();
            summaryResultTable.TableTitle = "iMODValidator Validation";
            summaryResultTable.TableCreator = Creator;

            summaryResultTable.LogErrors = log.Errors;
            summaryResultTable.LogWarnings = log.Warnings;

            // Loop through all result layers, grouped by resulttype, to retrieve result statistics
            // All statistics of a layer (so for all resultypes) are stored in one LayerStatistics object
            foreach (string resultType in resultlayersDictionary.Keys)
            {
                // Don't show details at summarylevel, skip Detail-resultTypes
                if (selectedResultTypes.Contains(resultType))
                {
                    foreach (ResultLayer resultLayer in resultlayersDictionary[resultType])
                    {
                        if ((resultLayer != null) && resultLayer.HasResults())
                        {
                            string filename = null;
                            if (resultLayer.ResultFile != null)
                            {
                                filename = resultLayer.ResultFile.Filename;
                            }
                            else
                            {
                                filename = "Missing";
                            }

                            // Create statistics-object for this layer
                            LayerStatistics layerStatistics = CreateLayerStatistics(resultLayer, Model.GetStressPeriodString(startDate, resultLayer.KPER));
                            if (summaryResultTable.ContainsLayerStatistic(layerStatistics))
                            {
                                // Use existing layerstatistics if statistics for this layer are already available
                                layerStatistics = summaryResultTable.GetLayerStatistics(layerStatistics);
                            }

                            // Calculate locationcount (the number of layercells that have a result) for this layer/system
                            long resultLocationCount = 0;
                            string resultFilename = "undefined";
                            if (resultLayer.ResultFile != null)
                            {
                                resultFilename = resultLayer.ResultFile.Filename;
                                if (log != null)
                                {
                                    log.AddMessage(LogLevel.Debug, "Calculating cell locationcount for " + Path.GetFileName(resultLayer.ResultFile.Filename) + "...", 2);
                                }
                                resultLocationCount = resultLayer.ResultFile.RetrieveElementCount();
                            }

                            // Add resultType-specific statistics for this layer to layerstatistics
                            if (layerStatistics != null)
                            {
                                if (!layerStatistics.Contains(resultType))
                                {
                                    ResultLayerStatistics resultLayerStatistics = resultLayer.CreateResultLayerStatistics(resultFilename, resultLocationCount);
                                    layerStatistics.Add(resultType, resultLayerStatistics);

                                    summaryResultTable.AddRow(layerStatistics);
                                }
                                else
                                {
                                    throw new Exception("A resultlayer of type '" + resultType.ToString() + "' has been added twice. Check '" + resultLayer.Id + "'-implementation");
                                }
                            }
                        }
                    }
                }
            }

            ReleaseMemory(log);

            return summaryResultTable;
        }

        public virtual LayerStatistics CreateLayerStatistics(ResultLayer resultLayer, string stressperiodString)
        {
            return new LayerStatistics((resultLayer.ResultFile != null) ? resultLayer.ResultFile.Filename : string.Empty, RetrieveLayerStatisticsMainType(resultLayer), RetrieveLayerStatisticsSubType(resultLayer), RetrieveLayerStatisticsMessageType(resultLayer), resultLayer.ILay, stressperiodString);
        }

        protected virtual string RetrieveLayerStatisticsMainType(ResultLayer resultLayer)
        {
            return resultLayer.Id;
        }

        protected virtual string RetrieveLayerStatisticsSubType(ResultLayer resultLayer)
        {
            return resultLayer.Id2;
        }

        protected virtual string RetrieveLayerStatisticsMessageType(ResultLayer resultLayer)
        {
            return resultLayer.Id2;
        }

        /// <summary>
        /// Creates an iMOD IMF-file with all the summary files, result-files and extra map files.
        /// Legends are added automatically to the IMF, based on the legend classes which are specified for the Result-files
        /// If no legend is specified for a result file, a default range-legend will be created
        /// </summary>
        /// <param name="log"></param>
        /// <param name="isIMODOpened">if true iMOD will be openend with the specified IMF-project file</param>
        protected void CreateIMFFile(string imfFilename, Log log, bool isRelativePathIMFAdded = false, bool isIMODOpened = true)
        {
            log.AddMessage(LogLevel.Trace, "Writing iMOD-projectfile...");
            Extent enlargedExtent = extent.Enlarge(0.2f);
            IMFFile imfFile = new IMFFile(enlargedExtent);

            // Add summary legends
            foreach (string resultType in summaryLayerDictionary.Keys)
            {
                SummaryResultLayer summaryLayer = summaryLayerDictionary[resultType];
                if ((summaryLayer.ResultFile != null) && (summaryLayer.ResultCount > 0))
                {
                    // if (summaryLayer.ResultFile.Legend == null)
                    // {
                    summaryLayer.ResultFile.Legend = SummaryLegend.CreateSummaryLegend(summaryLayer.ResultFile.MaxValue, resultType, summaryLayer.ResultFile.Filename);
                    // }
                    Map summaryMap = IMFFile.CreateMap(summaryLayer.ResultFile.Legend, summaryLayer.ResultFile.Filename);
                    summaryMap.Selected = true;
                    imfFile.AddMap(summaryMap);
                }
            }

            // Add resultfile legends
            foreach (string resultType in resultlayersDictionary.Keys)
            {
                List<ResultLayer> resultLayers = resultlayersDictionary[resultType];
                foreach (ResultLayer resultLayer in resultLayers)
                {
                    if (resultLayer.ResultCount != 0)
                    {
                        if ((resultLayer.ResultFile != null) && !(resultLayer.ResultFile is ConstantIDFFile))
                        {
                            IMODFile resultFile = resultLayer.ResultFile;
                            Legend legend = null;
                            if (resultFile.Legend != null)
                            {
                                legend = resultFile.Legend;
                            }
                            else
                            {
                                legend = resultFile.CreateLegend("iMODValidator result file for layer ");
                            }
                            imfFile.AddMap(legend, resultFile.Filename);

                            // add source files as extra mapfiles if present
                            foreach (IMODFile sourceFile in resultLayer.SourceFiles)
                            {
                                AddExtraMapFile(sourceFile);
                            }
                        }
                    }
                }
            }

            // Add extra IMODfile legends
            if (extraMapFiles.Count > 0)
            {
                foreach (IMODFile imodFile in extraMapFiles)
                {
                    if (File.Exists(imodFile.Filename))
                    {
                        if (imodFile.Legend != null)
                        {
                            imfFile.AddMap(imodFile.Legend, imodFile.Filename);
                        }
                        else
                        {
                            imfFile.AddMap(imodFile.CreateLegend("IMOD-file source file"), imodFile.Filename);
                        }
                    }
                    else
                    {
                        if (imodFile.Metadata != null)
                        {
                            log.AddWarning(null, imodFile.Filename, "File " + imodFile.Filename + " (" + imodFile.Metadata.Description + ") does not exist and cannot be added to IMF-file", 1);
                        }
                        else
                        {
                            log.AddWarning(null, imodFile.Filename, "File " + imodFile.Filename + " does not exist and cannot be added to IMF-file.", 1);
                        }
                    }
                }
            }

            // Add GEN-file overlays
            for (int i = 0; i < iMODValidatorSettingsManager.Settings.GENFiles.Count; i++)
            {
                Overlay overlay = CreateGENOverlay(i, log);
                if (File.Exists(overlay.Filename))
                {
                    overlay.Filename = Path.GetFullPath(overlay.Filename);
                    imfFile.AddOverlay(overlay);
                }
                else
                {
                    log.AddWarning("IMF-File", overlay.Filename, "Overlay GEN-file not found and skipped:" + overlay.Filename);
                }
            }

            // Check and fix aspect ratio
            float xyRatio = imfFile.GetExtentAspectRatio();
            if (imfFile.FixExtentAspectRatio())
            {
                log.AddMessage(LogLevel.Trace, "Fixed aspect ratio for extent from " + xyRatio.ToString("F3", englishCultureInfo) + " to " + IMFFile.IMFXYRatio.ToString("F3", englishCultureInfo) + " ...");

                // Enlarge extent to fit between axes
                // imfFile.Extent = imfFile.Extent.Enlarge(0.125f);
            }

            if (isRelativePathIMFAdded)
            {
                string relativeIMFFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(imfFilename) + "_relatievepaths.IMF");
                imfFile.WriteFile(relativeIMFFilename, true);
                string absoluteIMFFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(imfFilename) + "_absolutepaths.IMF");
                imfFile.WriteFile(absoluteIMFFilename, false);
            }
            else
            {
                imfFile.WriteFile(imfFilename, false);
            }
            if (isIMODOpened)
            {
                IMODTool.Start(Path.GetFullPath(imfFilename), log);
            }
            log.AddMessage(LogLevel.Trace, string.Empty);
        }

        /// <summary>
        /// Create GEN-overlay based on settings in array iMODValidatorSettingsManager.Settings.GENFiles
        /// </summary>
        /// <param name="genIdx"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected Overlay CreateGENOverlay(int genIdx, Log log)
        {
            string genFilename = null;
            bool genSelected = false;
            if (iMODValidatorSettingsManager.Settings.GENFiles.Count > genIdx)
            {
                genFilename = iMODValidatorSettingsManager.Settings.GENFiles[genIdx].Filename;
                string genSelectedString = null;
                try
                {
                    genSelected = iMODValidatorSettingsManager.Settings.GENFiles[genIdx].IsSelected;
                }
                catch (Exception)
                {
                    log.AddInfo("Could not parse GEN-selected " + genIdx + ": " + genSelectedString);
                }
            }
            int genThickness = 1;
            if (iMODValidatorSettingsManager.Settings.GENFiles.Count > genIdx)
            {
                string genThicknessString = null;
                try
                {
                    genThickness = iMODValidatorSettingsManager.Settings.GENFiles[genIdx].Thickness;
                }
                catch (Exception)
                {
                    log.AddInfo("Could not parse GEN-thickness " + genIdx + ": " + genThicknessString);
                }
            }

            Color genColor = Color.Gray;
            if (iMODValidatorSettingsManager.Settings.GENFiles.Count > genIdx)
            {
                string genColorString = null;
                try
                {
                    genColorString = iMODValidatorSettingsManager.Settings.GENFiles[genIdx].Colors;
                    string[] rgbValues = genColorString.Split(',');
                    if (rgbValues.Length == 3)
                    {
                        int red = int.Parse(rgbValues[0]);
                        int green = int.Parse(rgbValues[1]);
                        int blue = int.Parse(rgbValues[2]);
                        genColor = Color.FromArgb(red, green, blue);
                    }
                    else
                    {
                        log.AddInfo("Invalid GEN-color definition " + genIdx + ": " + genColorString);
                    }
                }
                catch (Exception)
                {
                    log.AddInfo("Could not parse GEN-color definition " + genIdx + ": " + genColorString);
                }
            }

            GENLegend genLegend = new GENLegend(genThickness, genColor);
            genLegend.Selected = genSelected;
            return new Overlay(genLegend, genFilename);
        }

        protected Metadata CreateSummaryFileMetadata(string resultType)
        {
            Metadata metadata = Model.CreateDefaultMetadata();
            metadata.Description = "Summary for " + resultType.ToLower() + "s. Cellvalue indicates total number of " + resultType.ToLower() + "s";
            ResultLayer summaryLayer = summaryLayerDictionary[resultType];
            if (summaryLayer != null)
            {
                if (summaryLayer.ResultFile != null)
                {
                    IMODFile summaryFile = summaryLayer.ResultFile;
                    if (summaryFile.Legend != null)
                    {
                        metadata.Description += summaryFile.Legend.ToLongString();
                    }
                    metadata.Resolution = summaryLayer.Resolution;
                }
                metadata.ProcessDescription = "Automatically generated by iMODValidator";
            }
            return metadata;
        }

        protected long GetTotalResultCount(string[] selectedResultTypes = null)
        {
            long totalResultCount = 0;

            foreach (string resultType in resultlayersDictionary.Keys)
            {
                if ((selectedResultTypes != null) && selectedResultTypes.Contains(resultType))
                {
                    foreach (ResultLayer resultLayer in resultlayersDictionary[resultType])
                    {
                        totalResultCount += resultLayer.ResultCount;
                    }
                }
            }

            return totalResultCount;
        }

        /// <summary>
        ///  Releases memory for errorfile cells.
        ///  These are lazy loaded and will be loaded again when used
        /// </summary>
        /// <param name="log"></param>
        public void ReleaseMemory(Log log)
        {
            foreach (string resultType in resultlayersDictionary.Keys)
            {
                foreach (ResultLayer resultLayer in resultlayersDictionary[resultType])
                {
                    resultLayer.ReleaseMemory(false);
                }
            }
            GC.Collect();
        }

        public virtual ResultSheet CreateExcelResultSheet()
        {
            return new ResultSheet(ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus), BaseModelFilename, extent);
        }

        public static ResultSheet ReadExcelResultTable(string filename, Log log)
        {
            ResultSheet resultTable = null;
            resultTable = new ResultSheet(ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus));
            resultTable.Import(filename, log);
            return resultTable;
        }
    }
}
