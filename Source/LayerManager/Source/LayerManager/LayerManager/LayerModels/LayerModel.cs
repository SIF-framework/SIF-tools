// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Utils;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// LayerModel class stores an model in iMOD-format, i.e. aquifers between TOP- and BOT-files of the same layer and aquitards between BOT-file of one layer and TOP-file of lower layer.
    /// kh- and kv-values are converted to kD- and c-values for ease of possible merging of layers later in the process. 
    /// </summary>
    public class LayerModel
    {
        /// <summary>
        /// Language definition for english culture as used in most tools
        /// </summary>
        public static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        public int ModellayerCount { get; set; }
        public string OutputPath { get; set; }

        public string[] BaseFilenames { get; set; }
        public IDFFile[] TOPIDFFiles { get; set; }
        public IDFFile[] BOTIDFFiles { get; set; }
        public IDFFile[] KDWIDFFiles { get; set; }
        public IDFFile[] VCWIDFFiles { get; set; }
        public IDFFile[] KHVIDFFiles { get; set; }
        public IDFFile[] KVVIDFFiles { get; set; }
        public IDFFile[] KVAIDFFiles { get; set; }
        public bool IsConsequtiveLayerChecked { get; set; }

        protected LayerModel(string outputPath)
        {
            this.OutputPath = outputPath;
        }

        /// <summary>
        /// Create new LayerModel object initialized for specified number of modellayers
        /// </summary>
        /// <param name="modellayerCount"></param>
        /// <param name="outputPath"></param>
        /// <param name="log"></param>
        public LayerModel(int modellayerCount, string outputPath) : this(outputPath)
        {
            Initialize(modellayerCount);
        }

        /// <summary>
        /// Read and create new LayerModel from specified LayerModelMap 
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static LayerModel Read(LayerModelMap layerModelMap, SIFToolSettings settings, bool useLazyLoading, IDFLog log, int logIndentLevel = 0)
        {
            LayerModel layerModel = new LayerModel(string.Empty);
            layerModel.DoRead(layerModelMap, settings, useLazyLoading, log, logIndentLevel);
            return layerModel;
        }

        public virtual Layer GetLayer(int modellayerNumber, LayerType layerType)
        {
            if (layerType == LayerType.Aquifer)
            {
                if ((modellayerNumber < TOPIDFFiles.Length) & (modellayerNumber > 0))
                {
                    if ((TOPIDFFiles[modellayerNumber] != null) && (BOTIDFFiles[modellayerNumber] != null))
                    {
                        return new Layer(this, modellayerNumber, layerType, TOPIDFFiles[modellayerNumber], BOTIDFFiles[modellayerNumber], KDWIDFFiles[modellayerNumber]);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if ((modellayerNumber <= TOPIDFFiles.Length) & (modellayerNumber >= 0))
                {
                    if ((BOTIDFFiles[modellayerNumber] != null) && (TOPIDFFiles[modellayerNumber + 1] != null))
                    {
                        return new Layer(this, modellayerNumber, layerType, BOTIDFFiles[modellayerNumber], TOPIDFFiles[modellayerNumber + 1], VCWIDFFiles[modellayerNumber]);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Check consistenct of this LayerModel according to specified settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="modelCheckLog"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public bool Check(SIFToolSettings settings, IDFLog modelCheckLog, int logIndentLevel = 0)
        {
            bool isValidModelLayer = true;
            bool isValidModel;
            bool isLayerMessageWritten;

            modelCheckLog.ClearLayerIDFFiles();
            long previousLogWarningCount = 0;
            Layer previousExistingLayer = null;
            string previousExistingLayerBaseFilename = null;
            if (BOTIDFFiles[0] != null)
            {
                Layer aquitardLayer0 = GetLayer(0, LayerType.Aquitard); 
                isValidModelLayer = aquitardLayer0.CheckTOPBOTConsistency(modelCheckLog, logIndentLevel + 1);
                previousExistingLayer = aquitardLayer0;
                previousExistingLayerBaseFilename = null;
            }

            isValidModel = isValidModelLayer;
            for (int i = 1; i <= ModellayerCount; i++)
            {
                bool isUndefinedLayer = true;
                isLayerMessageWritten = false;
                if ((TOPIDFFiles[i] != null) && (BOTIDFFiles[i] != null))
                {
                    isUndefinedLayer = false;
                    modelCheckLog.AddInfo("Checking modellayer " + i + " ... ", logIndentLevel + 1);
                    isLayerMessageWritten = true;

                    Layer aquiferLayer = GetLayer(i, LayerType.Aquifer);
                    string baseFilename = (i < ModellayerCount) ? BaseFilenames[i] : string.Empty;
                    isValidModelLayer = aquiferLayer.CheckTOPBOTConsistency(modelCheckLog, logIndentLevel + 2);
                    if (KDWIDFFiles[i] != null)
                    {
                        isValidModelLayer = aquiferLayer.CheckTOPBOTKDCValueConsistency(modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                        isValidModelLayer = aquiferLayer.CheckKDCValidity(modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                    }
                    if ((previousExistingLayer != null) && (IsConsequtiveLayerChecked || baseFilename.Equals(previousExistingLayerBaseFilename)))
                    {
                        isValidModelLayer = previousExistingLayer.CheckLevelEquality(aquiferLayer, aquiferLayer.LayerName + "-t", modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                    }
                    previousExistingLayer = aquiferLayer;
                    previousExistingLayerBaseFilename = (i < ModellayerCount) ? BaseFilenames[i] : string.Empty;

                    isValidModel &= isValidModelLayer;
                }

                if (IsConsequtiveLayerChecked || ((i > 0) && (i < BaseFilenames.Length) && BaseFilenames[i - 1].Equals(BaseFilenames[i])))
                {
                    if ((BOTIDFFiles[i] != null) && (TOPIDFFiles[i + 1] != null))
                    {
                        isUndefinedLayer = false;
                        if (!isLayerMessageWritten)
                        {
                            modelCheckLog.AddInfo("Checking modellayer " + i + " ... ", logIndentLevel + 1);
                        }

                        Layer aquitardLayer = GetLayer(i, LayerType.Aquitard);
                        string baseFilename = (i < ModellayerCount) ? BaseFilenames[i] : string.Empty;
                        isValidModelLayer = aquitardLayer.CheckTOPBOTConsistency(modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                        if (VCWIDFFiles[i] != null)
                        {
                            isValidModelLayer = aquitardLayer.CheckTOPBOTKDCValueConsistency(modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                            isValidModelLayer = aquitardLayer.CheckKDCValidity(modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                        }
                        if ((previousExistingLayer != null) && (IsConsequtiveLayerChecked || baseFilename.Equals(previousExistingLayerBaseFilename)))
                        {
                            isValidModelLayer = previousExistingLayer.CheckLevelEquality(aquitardLayer, aquitardLayer.LayerName + "-t", modelCheckLog, logIndentLevel + 2) && isValidModelLayer;
                        }
                        previousExistingLayer = aquitardLayer;
                        previousExistingLayerBaseFilename = (i < ModellayerCount) ? BaseFilenames[i] : string.Empty;

                        isValidModel &= isValidModelLayer;
                    }
                }

                if (isValidModelLayer && !isUndefinedLayer)
                {
                    string msg = "No inconsistencies found in L" + i;
                    if (modelCheckLog.Warnings.Count > previousLogWarningCount)
                    {
                        msg = "No fatal inconsistencies found in L" + i;
                    }

                    modelCheckLog.AddInfo(msg, logIndentLevel + 2);
                }
            }

            if (isValidModel)
            {
                string msg = "No inconsistencies found in model";
                if (modelCheckLog.Warnings.Count > 0)
                {
                    msg = "No fatal inconsistencies found in model";
                }
                modelCheckLog.AddInfo(msg, logIndentLevel + 1);
                modelCheckLog.AddInfo();
            }

            modelCheckLog.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel + 1);
            modelCheckLog.WriteLogFiles(false);
            IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(modelCheckLog);
            idfFileStatsWriter.WriteExcelFile();

            if (modelCheckLog.Warnings.Count > 0)
            {
                WriteWarningLegend(Path.Combine(Path.GetDirectoryName(modelCheckLog.BaseFilename), "iMODLayerManager_warnings.leg"), modelCheckLog, logIndentLevel + 1);
            }
            if (modelCheckLog.Errors.Count > 0)
            { 
                WriteErrorLegend(Path.Combine(Path.GetDirectoryName(modelCheckLog.BaseFilename), "iMODLayerManager_errors.leg"), modelCheckLog, logIndentLevel + 1);
            }

            return isValidModel;
        }

        /// <summary>
        /// Calculates kD- and c-values from KHV/KHV/KVA-values if no kD/C-values are present yet
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="useDefaultKDCValues">if true, default kh- and kv-values, as defined in properties XML-file, are used when no IDF-files for kh- or kv-values are defined</param>
        /// <param name="isWritten"></param>
        /// <param name="isThicknessWritten"></param>
        /// <param name="isMemoryReleased"></param>
        public void RetrieveKDCValues(SIFToolSettings settings, bool useDefaultKDCValues = false, bool isWritten = false, bool isThicknessWritten = false, bool isMemoryReleased = false, Log log = null, int logIndentLevel = 0)
        {
            IDFFile topIDFFile = null;
            IDFFile botIDFFile = null;
            IDFFile lowerTopIDFFile = null;
            IDFFile lowerBotIDFFile = null;
            IDFFile aquiferThicknessIDFFile = null;
            IDFFile lowerAquiferThicknessIDFFile = null;

            for (int layerNumber = 1; layerNumber <= ModellayerCount; layerNumber++)
            {
                topIDFFile = TOPIDFFiles[layerNumber];
                botIDFFile = BOTIDFFiles[layerNumber];

                /////////////////////////
                // Handle aquifer part //
                /////////////////////////

                // Check if kD is not yet present
                if (KDWIDFFiles[layerNumber] == null)
                {
                    if (log != null)
                    {
                        log.AddInfo("Calculating kDc-" + (isThicknessWritten ? " and thickness-" : string.Empty) + "values for iMOD-layer " + layerNumber + " ...", logIndentLevel);
                    }

                    if (lowerAquiferThicknessIDFFile != null)
                    {
                        aquiferThicknessIDFFile = lowerAquiferThicknessIDFFile;
                    }
                    else
                    {
                        aquiferThicknessIDFFile = topIDFFile - botIDFFile;
                    }
                    if (isThicknessWritten)
                    {
                        string aquiferThicknessFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputThicknessSubdirname, Properties.Settings.Default.ThicknessFilePrefix + "_" + Properties.Settings.Default.OutputAquiferLayerPostfix + layerNumber + ".IDF"));
                        aquiferThicknessIDFFile.WriteFile(aquiferThicknessFilename);
                    }

                    IDFFile khvIDFFile = KHVIDFFiles[layerNumber];
                    if (khvIDFFile != null)
                    {
                        KDWIDFFiles[layerNumber] = aquiferThicknessIDFFile * khvIDFFile;
                    }
                    else if (useDefaultKDCValues)
                    {
                        KDWIDFFiles[layerNumber] = aquiferThicknessIDFFile * Properties.Settings.Default.DefaultKHValue;
                    }
                    else
                    {
                        throw new ToolException("KHV-file not found for layer " + layerNumber + ", kD cannot be calculated");
                    }
                }

                if (isWritten)
                {
                    string kdFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputKDSubdirname, Properties.Settings.Default.kDFilePrefix + "_" + Properties.Settings.Default.OutputLayerPostfix + layerNumber + ".IDF"));
                    KDWIDFFiles[layerNumber].WriteFile(kdFilename);
                }

                /////////////////////////
                // Handle aquifer part //
                /////////////////////////

                // Check if c is not yet present
                if (VCWIDFFiles[layerNumber] == null)
                {
                    lowerTopIDFFile = TOPIDFFiles[layerNumber + 1];
                    if (aquiferThicknessIDFFile == null)
                    {
                        aquiferThicknessIDFFile = topIDFFile - botIDFFile;
                    }

                    if (lowerTopIDFFile != null)
                    {
                        IDFFile aquitardThicknessIDFFile = botIDFFile - lowerTopIDFFile;
                        if (isThicknessWritten)
                        {
                            string aquitardThicknessFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputThicknessSubdirname, Properties.Settings.Default.ThicknessFilePrefix + "_" + Properties.Settings.Default.OutputAquitardLayerPostfix + layerNumber + ".IDF"));
                            aquitardThicknessIDFFile.WriteFile(aquitardThicknessFilename);
                        }

                        IDFFile kvvIDFFile = KVVIDFFiles[layerNumber];
                        if ((kvvIDFFile != null) || useDefaultKDCValues)
                        {
                            if (kvvIDFFile != null)
                            {
                                VCWIDFFiles[layerNumber] = aquitardThicknessIDFFile / kvvIDFFile;
                            }
                            else
                            {
                                VCWIDFFiles[layerNumber] = aquitardThicknessIDFFile / Properties.Settings.Default.DefaultKVValue;
                            }
                            VCWIDFFiles[layerNumber].NoDataCalculationValue = 0;

                            if ((KVAIDFFiles != null) && (KVAIDFFiles[layerNumber] != null))
                            {
                                IDFFile cAqf1 = (aquiferThicknessIDFFile * 0.5f) / (KVAIDFFiles[layerNumber] * KHVIDFFiles[layerNumber]);
                                cAqf1.NoDataCalculationValue = 0;
                                VCWIDFFiles[layerNumber] += cAqf1;
                                if (BOTIDFFiles[layerNumber + 1] != null)
                                {
                                    lowerBotIDFFile = BOTIDFFiles[layerNumber + 1];
                                    lowerAquiferThicknessIDFFile = (lowerBotIDFFile != null) ? (lowerTopIDFFile - lowerBotIDFFile) : null;
                                    if (lowerAquiferThicknessIDFFile != null)
                                    {
                                        IDFFile cAqf2 = (lowerAquiferThicknessIDFFile * 0.5f) / (KVAIDFFiles[layerNumber + 1] * KHVIDFFiles[layerNumber + 1]);
                                        cAqf2.NoDataCalculationValue = 0;
                                        VCWIDFFiles[layerNumber] += cAqf2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new ToolException("KVV-file not found for layer " + layerNumber + ", C cannot be calculated");
                        }
                    }

                    if (isWritten && (VCWIDFFiles[layerNumber] != null))
                    {
                        string cFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputCSubdirname, Properties.Settings.Default.CFilePrefix + "_" + Properties.Settings.Default.OutputLayerPostfix + layerNumber + ".IDF"));
                        VCWIDFFiles[layerNumber].WriteFile(cFilename);
                    }
                }

                if (isMemoryReleased && (layerNumber > 1))
                {
                    ReleaseLayerMemory(layerNumber - 1, true, true, true, true, true, isWritten, isWritten, true);
                }
            }

            if (isMemoryReleased)
            {
                ReleaseMemory(true, true, true, true, true, isWritten, isWritten);
            }
        }

        /// <summary>
        /// Release memory from IDF-files in current LayerModel object
        /// </summary>
        /// <param name="isTOPReleased"></param>
        /// <param name="isBOTReleased"></param>
        /// <param name="isKHVReleased"></param>
        /// <param name="isKVVReleased"></param>
        /// <param name="isKVAReleased"></param>
        /// <param name="isKDReleased"></param>
        /// <param name="isCReleased"></param>
        public void ReleaseMemory(bool isTOPReleased = true, bool isBOTReleased = true, bool isKHVReleased = true, bool isKVVReleased = true, bool isKVAReleased = true, bool isKDReleased = true, bool isCReleased = true)
        {
            for (int layerNumber = 0; layerNumber < TOPIDFFiles.Length; layerNumber++)
            {
                ReleaseLayerMemory(layerNumber, isTOPReleased, isBOTReleased, isKHVReleased, isKVVReleased, isKVAReleased, isKDReleased, isCReleased, false);
            }

            GC.Collect();
        }

        /// <summary>
        /// Create iMOD legendfile for warnings
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public static void WriteWarningLegend(string filename, Log log = null, int logIndentLevel = 0)
        {
            IDFLegend legend = new IDFLegend("iMODLayerManager modelcheck warnings");
            legend.AddClass(new ValueLegendClass(0, "No warnings found (0)", Color.White));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Warning1Code_unexpectedKDC,
                Layer.Warning1String_unexpectedKDC + " (" + Layer.Warning1Code_unexpectedKDC + ")",
                Color.Red));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Warning2Code_missingKDC,
                Layer.Warning2String_missingKDC + " (" + Layer.Warning2Code_missingKDC + ")",
                Color.Orange));
            if (log != null)
            {
                log.AddInfo("Writing warning legend to " + Path.GetFileName(filename) + " ...", logIndentLevel);
            }
            legend.Sort();
            legend.WriteLegendFile(filename);
        }

        /// <summary>
        /// Create iMOD legendfile for errors
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public static void WriteErrorLegend(string filename, Log log = null, int logIndentLevel = 0)
        {
            IDFLegend legend = new IDFLegend("iMODLayerManager modelcheck errors");
            legend.AddClass(new ValueLegendClass(0, "No errors found (0)", Color.White));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Error1Code_layerInconsistency,
                Layer.Error1String_layerInconsistency + " (" + Layer.Error1Code_layerInconsistency + ")",
                Color.Orange));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Error2Code_missingKDC,
                Layer.Error2String_missingKDC + " (" + Layer.Error2Code_missingKDC + ")",
                Color.Red));
            legend.AddClass(new ValueLegendClass((float)Layer.Error2Code_missingKDC + 1,
                "Errorcombination (3)", Color.DarkRed));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Error3Code_invalidKDC,
                Layer.Error3String_invalidKDC + " (" + Layer.Error3Code_invalidKDC + ")",
                Color.Purple));
            legend.AddClass(new RangeLegendClass((float)Layer.Error3Code_invalidKDC + 1,
                (float)2 * Layer.Error3Code_invalidKDC - 1,
                "Errorcombination (5-7)", Color.DarkRed));
            legend.AddClass(new ValueLegendClass(
                (float)Layer.Error4Code_unexpectedLevelInequality,
                Layer.Error4String_unexpectedLevelInequality + " (" + Layer.Error4Code_unexpectedLevelInequality + ")",
                Color.DarkBlue));
            legend.AddClass(new RangeLegendClass((float)Layer.Error4Code_unexpectedLevelInequality + 1,
                (float)2 * Layer.Error4Code_unexpectedLevelInequality - 1,
                "Errorcombination (9-15)", Color.DarkRed));
            if (log != null)
            {
                log.AddInfo("Writing error legend to " + Path.GetFileName(filename) + " ...", logIndentLevel);
            }
            legend.Sort();
            legend.WriteLegendFile(filename);
        }

        /// <summary>
        /// Initialize internal variables for specified number of modellayers. Note: KVAIDFFiles will be empty.
        /// </summary>
        /// <param name="modellayerCount"></param>
        protected virtual void Initialize(int modellayerCount)
        {
            this.ModellayerCount = modellayerCount;
            this.IsConsequtiveLayerChecked = true;

            // Use dummy layer at 0-index and at end of array
            BaseFilenames = new string[modellayerCount + 2];
            TOPIDFFiles = new IDFFile[modellayerCount + 2];
            BOTIDFFiles = new IDFFile[modellayerCount + 2];
            KDWIDFFiles = new IDFFile[modellayerCount + 2];
            VCWIDFFiles = new IDFFile[modellayerCount + 2];
            KHVIDFFiles = new IDFFile[modellayerCount + 2];
            KVVIDFFiles = new IDFFile[modellayerCount + 2];
            KVAIDFFiles = null;

            for (int i = 0; i < modellayerCount + 2; i++)
            {
                BaseFilenames[i] = null;
                TOPIDFFiles[i] = null;
                BOTIDFFiles[i] = null;
                KHVIDFFiles[i] = null;
                KVVIDFFiles[i] = null;
                KDWIDFFiles[i] = null;
                VCWIDFFiles[i] = null;
            }
        }

        /// <summary>
        /// Initialize internal KVA-array for current number of modellayers
        /// </summary>
        protected void InitializeKVAIDFFiles()
        {
            KVAIDFFiles = new IDFFile[ModellayerCount + 2];
            for (int i = 0; i < ModellayerCount + 2; i++)
            {
                KVAIDFFiles[i] = null;
            }
        }

        /// <summary>
        /// Read LayerModel-data for this LayerModelb-object from specified LayerModelMap
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void DoRead(LayerModelMap layerModelMap, SIFToolSettings settings, bool useLazyLoading, IDFLog log, int logIndentLevel)
        {
            string imodTopFilename = null;
            string imodBotFilename = null;
            IDFFile imodTopIDFFile = null;
            IDFFile imodBotIDFFile = null;
            IDFFile imodLowerTopIDFFile = null;

            log.AddInfo("Reading layer model ...", logIndentLevel);

            // Retrieve lowest iMOD-modellayernumber
            string layerID = layerModelMap.GetLayerID(layerModelMap.TOPFilenames.Length - 1);
            ModellayerCount = IMODUtils.GetLayerNumber(layerID, null);
            if (ModellayerCount < 0)
            {
                throw new ToolException("Invalid layerID for last entry in filelist: " + layerID);
            }

            if (layerModelMap.HasExtraTopFile())
            {
                ModellayerCount--;
            }

            // Reinitalize LayerModel object for number of modellayers
            Initialize(ModellayerCount);

            // Now start reading files, work from bottom to top
            for (int fileIdx = layerModelMap.TOPFilenames.Length - 1; fileIdx >= 0; fileIdx--)
            {
                // Process filenames based on top-filenames
                imodTopFilename = layerModelMap.TOPFilenames[fileIdx];
                if (imodTopFilename != null)
                {
                    int layerNumber = ReadTopFile(layerModelMap, fileIdx, useLazyLoading, log, logIndentLevel + 1, settings);
                    if (layerNumber < 0)
                    {
                        throw new ToolException("Invalid layerID for entry " + (fileIdx + 1) + " in filelist: " + layerID);
                    }
                    imodTopIDFFile = TOPIDFFiles[layerNumber];

                    // Retrieve and check corresponding bot-file
                    imodBotFilename = layerModelMap.BOTFilenames[fileIdx];
                    if (imodBotFilename != null)
                    {
                        ReadBotFile(layerModelMap, fileIdx, layerNumber, useLazyLoading, log, logIndentLevel + 1, settings);
                        imodBotIDFFile = BOTIDFFiles[layerNumber];

                        CheckCellsizeAndExtent(imodBotIDFFile, imodTopIDFFile);

                        // Read corresponding kD- or KHV/KVA-file
                        if (layerModelMap.KDWFilenames != null)
                        {
                            string imodKDFilename = RetrieveModelFilename(layerModelMap.KDWFilenames, layerNumber);
                            if (imodKDFilename != null)
                            {
                                IDFFile imodKDIDFFile = IDFFile.ReadFile(imodKDFilename, useLazyLoading, log, logIndentLevel + 1, settings.Extent);
                                CheckCellsizeAndExtent(imodKDIDFFile, imodTopIDFFile);
                                imodKDIDFFile.ReplaceValues(imodKDIDFFile.NoDataValue, 0);
                                KDWIDFFiles[layerNumber] = imodKDIDFFile;
                            }
                            else
                            {
                                // Aquifer layer should have a corresponding kD-file
                                log.AddWarning("Missing corresponding KDW-file for iMOD-modellayer " + layerNumber, logIndentLevel + 1);
                            }
                        }
                        else if (layerModelMap.KHVFilenames != null)
                        {
                            string imodKHVFilename = RetrieveModelFilename(layerModelMap.KHVFilenames, layerNumber);
                            if (imodKHVFilename != null)
                            {
                                IDFFile imodKHVIDFFile = IDFFile.ReadFile(imodKHVFilename, useLazyLoading, log, logIndentLevel + 1, settings.Extent);
                                CheckCellsizeAndExtent(imodKHVIDFFile, imodTopIDFFile);
                                KHVIDFFiles[layerNumber] = imodKHVIDFFile;
                            }
                            else
                            {
                                // Aquifer layer should have a corresponding KHV-file
                                log.AddWarning("Missing corresponding KHV-file for iMOD-modellayer " + layerNumber, logIndentLevel + 1);
                            }
                        }

                        // If any KVA-file is defined, read and ensure all are defined
                        if ((KHVIDFFiles[layerNumber] != null) && (layerModelMap.KVAFilenames != null))
                        {
                            // Check for corresponding KVA-file
                            string imodKVAFilename = RetrieveModelFilename(layerModelMap.KVAFilenames, layerNumber);
                            if (imodKVAFilename != null)
                            {
                                if (KVAIDFFiles == null)
                                {
                                    InitializeKVAIDFFiles();
                                }
                                IDFFile imodKVAIDFFile = IDFFile.ReadFile(imodKVAFilename, useLazyLoading, log, logIndentLevel + 1, settings.Extent);
                                CheckCellsizeAndExtent(imodKVAIDFFile, imodTopIDFFile);
                                KVAIDFFiles[layerNumber] = imodKVAIDFFile;
                            }
                            else
                            {
                                if (KVAIDFFiles == null)
                                {
                                    InitializeKVAIDFFiles();
                                }

                                // Aquifer layer should have a corresponding KVA-file
                                log.AddWarning("Missing corresponding KVA-file for iMOD-modellayer " + layerNumber + ", using default value: " + settings.DefaultKVAValue, logIndentLevel + 1);
                                KVAIDFFiles[layerNumber] = new ConstantIDFFile(settings.DefaultKVAValue);
                            }
                        }

                        if (imodLowerTopIDFFile != null)
                        {
                            // If a lower top-file exists, read corresponding C-or KVV-File
                            if (layerModelMap.VCWFilenames != null)
                            {
                                string imodCFilename = RetrieveModelFilename(layerModelMap.VCWFilenames, layerNumber);
                                if (imodCFilename != null)
                                {
                                    IDFFile imodCIDFFile = IDFFile.ReadFile(imodCFilename, useLazyLoading, log, logIndentLevel + 1, settings.Extent);
                                    CheckCellsizeAndExtent(imodCIDFFile, imodTopIDFFile);
                                    imodCIDFFile.ReplaceValues(imodCIDFFile.NoDataValue, 0);
                                    VCWIDFFiles[layerNumber] = imodCIDFFile;
                                }
                                else
                                {
                                    log.AddWarning("Missing corresponding VCW-file for iMOD-modellayer " + layerNumber + ", using default value: " + settings.DefaultVCWValue, logIndentLevel + 1);
                                    //cIDFFiles[layerNumber] = new ConstantIDFFile(settings.defaultVCWValue);
                                }
                            }
                            else
                            {
                                if (layerModelMap.KVVFilenames != null)
                                {
                                    string imodKVVFilename = RetrieveModelFilename(layerModelMap.KVVFilenames, layerNumber);
                                    if (imodKVVFilename != null)
                                    {
                                        IDFFile imodKVVIDFFile = IDFFile.ReadFile(imodKVVFilename, useLazyLoading, log, logIndentLevel + 1, settings.Extent);
                                        CheckCellsizeAndExtent(imodKVVIDFFile, imodTopIDFFile);
                                        // imodKVVIDFFile.ReplaceValues(imodKVVIDFFile.NoDataValue, 0);
                                        KVVIDFFiles[layerNumber] = imodKVVIDFFile;
                                    }
                                    else
                                    {
                                        // Aquifer layer should have a corresponding KVA-file
                                        log.AddWarning("Missing corresponding KVV-file for iMOD-modellayer " + layerNumber + ", using default value: " + settings.DefaultKVVValue, logIndentLevel + 1);
                                        //kvvIDFFiles[layerNumber] = new ConstantIDFFile(settings.defaultKVVValue);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (fileIdx != layerModelMap.TOPFilenames.Length - 1)
                        {
                            // Top-file should have a corresponding Bot-file, except maybe for the lowest top-file
                            throw new ToolException("Missing corresponding BOT-file for TOP-file: " + imodTopFilename);
                        }
                    }

                    imodLowerTopIDFFile = imodTopIDFFile;
                }
            }

            BaseFilenames = layerModelMap.GetGroupLayerIDs();
        }

        /// <summary>
        /// Read BOT-file at specified index from specified LayerModelMap object
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="fileIdx"></param>
        /// <param name="layerNumber"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="">logIndentLevel</param>
        /// <param name="settings"></param>
        protected virtual void ReadBotFile(LayerModelMap layerModelMap, int fileIdx, int layerNumber, bool useLazyLoading, IDFLog log, int logIndentLevel, SIFToolSettings settings)
        {
            string imodBotFilename = layerModelMap.BOTFilenames[fileIdx];
            IDFFile imodBotIDFFile = IDFFile.ReadFile(imodBotFilename, useLazyLoading, log, logIndentLevel, settings.Extent);
            BOTIDFFiles[layerNumber] = imodBotIDFFile;
       }

        /// <summary>
        /// Read TOP-file from specified filename (index in LayerModelMap) and add to this LayerModel object at layernumber index based on layerID in TOP-filename
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="fileIdx">index in TOP-filenames array of specified imodLayerModelMap</param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual int ReadTopFile(LayerModelMap layerModelMap, int fileIdx, bool useLazyLoading, IDFLog log, int logIndentLevel, SIFToolSettings settings)
        {
            string imodTopFilename = layerModelMap.TOPFilenames[fileIdx];
            IDFFile imodTopIDFFile = IDFFile.ReadFile(imodTopFilename, useLazyLoading, log, logIndentLevel, settings.Extent);
            string layerID = layerModelMap.GetLayerID(fileIdx);
            int layerNumber = IMODUtils.GetLayerNumber(layerID, null);
            if (layerNumber < 0)
            {
                throw new ToolException("Invalid layerID for entry " + (fileIdx + 1) + " in filelist: " + layerID);
            }
            TOPIDFFiles[layerNumber] = imodTopIDFFile;

            return layerNumber;
        }

        /// <summary>
        /// Retrieve filename in array with iMOD-filenames as specified by layernumber
        /// </summary>
        /// <param name="iMODFilenames"></param>
        /// <param name="layerNumber"></param>
        /// <returns></returns>
        protected static string RetrieveModelFilename(string[] iMODFilenames, int layerNumber)
        {
            if (iMODFilenames != null)
            {
                for (int idx = 0; idx < iMODFilenames.Length; idx++)
                {
                    int iMODFilenameLayerNumber = -1;
                    try
                    {
                        // Try to find layernumber after default prefixes
                        iMODFilenameLayerNumber = IMODUtils.GetLayerNumber(iMODFilenames[idx]);
                    }
                    catch (Exception)
                    {
                        // Try to find layernumber as last number in filename
                        iMODFilenameLayerNumber = IMODUtils.GetLastNumericValue(iMODFilenames[idx]);
                    }
                    if (layerNumber == iMODFilenameLayerNumber)
                    {
                        return iMODFilenames[idx];
                    }
                }
            }
            return null;
        }

        protected void ReleaseLayerMemory(int layerNumber, bool isTOPReleased = true, bool isBOTReleased = true, bool isKHVReleased = true, bool isKVVReleased = true, bool isKVAReleased = true, bool isKDReleased = true, bool isCReleased = true, bool isMemoryCollected = true)
        {
            if (isTOPReleased && (TOPIDFFiles[layerNumber] != null))
            {
                TOPIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isBOTReleased && (BOTIDFFiles[layerNumber] != null))
            {
                BOTIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isKHVReleased && (KHVIDFFiles[layerNumber] != null))
            {
                KHVIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isKVVReleased && (KVVIDFFiles[layerNumber] != null))
            {
                KVVIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isKVAReleased && ((KVAIDFFiles != null) && (KVAIDFFiles[layerNumber] != null)))
            {
                KVAIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isKDReleased && (KDWIDFFiles[layerNumber] != null))
            {
                KDWIDFFiles[layerNumber].ReleaseMemory(false);
            }
            if (isCReleased && (VCWIDFFiles[layerNumber] != null))
            {
                VCWIDFFiles[layerNumber].ReleaseMemory(false);
            }

            if (isMemoryCollected)
            {
                GC.Collect();
            }
        }

        protected static void CheckCellsizeAndExtent(IDFFile imodIDFFile, IDFFile imodTOPIDFFile)
        {
            if (!imodIDFFile.XCellsize.Equals(imodTOPIDFFile.XCellsize) || !imodIDFFile.YCellsize.Equals(imodTOPIDFFile.YCellsize))
            {
                throw new ToolException("Mismatch in cellsize between IDF-file (" + imodIDFFile.XCellsize + "x" + imodIDFFile.YCellsize + ")"
                    + " and TOP (" + imodTOPIDFFile.XCellsize + "x" + imodTOPIDFFile.YCellsize + ") IDF-file: " + imodIDFFile.Filename);
            }
            if (!imodIDFFile.Extent.Equals(imodTOPIDFFile.Extent))
            {
                throw new ToolException("Mismatch in extent between kD/c/kh/kv (" + imodIDFFile.Extent + ")"
                    + " and TOP (" + imodTOPIDFFile.Extent + ") IDF-files: " + imodIDFFile.Filename);
            }
        }

        /// <summary>
        /// Check difference between idfFile1 - idfFile2, write idf (based on idfFile1) with differences if differences are found
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <param name="outputPath"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected static void CheckLayerConnection(IDFFile idfFile1, IDFFile idfFile2, string outputPath, Log log, int logIndentLevel = 0)
        {
            IDFFile errorIDFFile = (IDFFile)idfFile1.Copy(Path.Combine(Path.Combine(outputPath, Properties.Settings.Default.MismatchSubdirname), Path.GetFileNameWithoutExtension(idfFile1.Filename) + "_mismatch.IDF"));
            errorIDFFile.ResetValues();

            int errorCount = 0;
            for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                {
                    float value1 = idfFile1.values[rowIdx][colIdx];
                    float value2 = idfFile2.values[rowIdx][colIdx];
                    if (!value1.Equals(idfFile1.NoDataValue) && !value2.Equals(idfFile2.NoDataValue))
                    {
                        if (Math.Abs(value1 - value2) > Layer.LevelTolerance)
                        {
                            float x = idfFile1.GetX(colIdx);
                            float y = idfFile2.GetY(rowIdx);

                            errorCount++;
                            errorIDFFile.values[rowIdx][colIdx] = value1 - value2;
                            if (errorCount <= Properties.Settings.Default.MaxWarningMessageCount)
                            {
                                float missingThickness = value1 - value2;
                                string msg = missingThickness.ToString("F2", EnglishCultureInfo) + "m missing below " + Path.GetFileName(idfFile1.Filename) + ". Value (" + value1.ToString("F2", EnglishCultureInfo) + ") at " + 
                                    GISUtils.GetXYString(x, y) + " doesn't match lower value (" + value2.ToString("F2", EnglishCultureInfo) + ")";
                                log.AddWarning(msg, logIndentLevel);

                                if (errorCount == Properties.Settings.Default.MaxWarningMessageCount)
                                {
                                    log.AddInfo("This warning is not reported anymore for this file", logIndentLevel + 1);
                                }
                            }
                        }
                    }
                }
            }
            if (errorCount > Properties.Settings.Default.MaxWarningMessageCount)
            {
                errorIDFFile.WriteFile();
                log.AddWarning(errorCount.ToString() + " IDF-values (see error-IDF) didn't match lower values and will be ignored for IDF-file " + Path.GetFileName(idfFile1.Filename), logIndentLevel);
            }
        }

        /// <summary>
        /// Check if for any of the defined modellayers the corresponding IDF-file with kD- or c-values is null
        /// </summary>
        /// <returns></returns>
        public bool HasMissingKDCLayers()
        {
            for (int layerNumber = 1; layerNumber <= ModellayerCount; layerNumber++)
            {
                if (KDWIDFFiles[layerNumber] == null)
                {
                    return true;
                }
                if ((layerNumber < ModellayerCount) && (VCWIDFFiles[layerNumber] == null))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if for any of the defined modellayers the corresponding IDF-file with kh- or kv-values is null
        /// </summary>
        /// <returns></returns>
        public bool HasMissingKHKVLayers()
        {
            for (int layerNumber = 1; layerNumber <= ModellayerCount; layerNumber++)
            {
                if (KHVIDFFiles[layerNumber] == null)
                {
                    return true;
                }
                if ((layerNumber < ModellayerCount) && (KVVIDFFiles[layerNumber] == null))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ensure that kD- and C-layers exist, either based on existing or default kh- and/or kv-values.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="useDefaultKDCValues"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public void EnsureKDCLayers(SIFToolSettings settings, bool useDefaultKDCValues, Log log = null, int logIndentLevel = 0)
        {
            if (HasMissingKDCLayers())
            {
                if (log != null)
                {
                    if (HasMissingKHKVLayers())
                    {
                        log.AddInfo("Calculating kDc-layers based on (default) kh- and kv-values...", logIndentLevel++);
                    }
                    else
                    {
                        log.AddInfo("Calculating kDc-layers based on kh- and kv-values ...", logIndentLevel++);
                    }
                }
                RetrieveKDCValues(settings, useDefaultKDCValues, false, false, false, log, logIndentLevel);
            }
        }
    }
}
