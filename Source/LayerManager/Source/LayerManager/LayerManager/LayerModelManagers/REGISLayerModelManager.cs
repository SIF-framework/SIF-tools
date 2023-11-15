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
using Sweco.SIF.LayerManager.LayerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModelManagers
{
    /// <summary>
    /// Class for managing REGIS-LayerModels
    /// </summary>
    public class REGISLayerModelManager : LayerModelManager
    {
        public bool IsLayerGroupLevelMerged { get; set; }

        public REGISLayerModelManager(SIFToolSettings settings) : base(settings)
        {
            IsLayerGroupLevelMerged = settings.IsLayerGroupLevelMerged;
        }

        /// <summary>
        /// Read filenames to layermodel files as defined by settings 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public override LayerModelMap ReadLayerModelMap(IDFLog log, int logIndentlevel)
        {
            return REGISLayerModelMap.ReadDirectories(Settings, log, logIndentlevel);
        }

        /// <summary>
        /// Reads files from specified modelmap and check for inconsistencies in layermodel
        /// </summary>
        /// <param name="regisLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="modelname">a modelname for the checked files, used to create a subdirectory for checkresults, or leave empty</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public override bool Check(LayerModelMap regisLayerModelMap, SIFToolSettings settings, string modelname, IDFLog log, int logIndentLevel = 0)
        {
            IDFFile regisTopIDFFile;
            IDFFile regisBotIDFFile;
            IDFFile regisKDCIDFFile;
            LayerType regisLayerType;
            int regisModellayerNumber;

            // Check REGIS-layers, work from top to bottom
            log.AddInfo("Checking input REGIS-model for inconsistencies ... ", logIndentLevel);
            if (!regisLayerModelMap.IsOrdered)
            {
                log.AddInfo("No order has been defined: level consistency with upper/lower layers will not be checked", logIndentLevel + 1);
            }

            bool isValidModel = true;
            Layer prevLayers = null;
            for (int fileIdx = 0; fileIdx < regisLayerModelMap.TOPFilenames.Length; fileIdx++)
            {
                string layerName = REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.TOPFilenames[fileIdx]);
                if (layerName != null)
                {
                    log.AddInfo("Checking REGIS-layer '" + layerName + "' ...", logIndentLevel + 1);

                    // Retrieve characteristics of new layer: aquifer/aquitard and iMOD-layernumber, etc
                    RetrieveLayerInfo(regisLayerModelMap, fileIdx, out regisLayerType, out regisModellayerNumber, log, logIndentLevel + 2);

                    // Actually read top-, bot- and kD/c-files
                    regisTopIDFFile = IDFFile.ReadFile(regisLayerModelMap.TOPFilenames[fileIdx], false, log, logIndentLevel + 2, settings.Extent);
                    regisBotIDFFile = IDFFile.ReadFile(regisLayerModelMap.BOTFilenames[fileIdx], false, log, logIndentLevel + 2, settings.Extent);

                    if (fileIdx == 0)
                    {
                        // Set base IDF for logwarnings and -errors 
                        log.SetBaseIDFFile(regisTopIDFFile);
                    }

                    // Create a Layer object with all inputdata for this REGIS layer and do layer concistency checks for this layer
                    Layer regisLayer = null;
                    if (regisLayerType == LayerType.Complex)
                    {
                        // Complex layers are checked for both kD- and c-values
                        log.AddInfo("checking " + REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.BOTFilenames[fileIdx]) + "-complex aquifer-part ...", logIndentLevel + 2);
                        regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, LayerType.Aquifer, regisTopIDFFile, regisBotIDFFile, settings.Extent, log, logIndentLevel + 2);
                        regisLayer = new Layer(layerName, regisModellayerNumber, LayerType.Aquifer, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                        isValidModel = regisLayer.CheckLayer(log, logIndentLevel + 2) && isValidModel;

                        log.AddInfo("checking " + REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.BOTFilenames[fileIdx]) + "-complex aquitard-part ...", logIndentLevel + 2);
                        regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, LayerType.Aquitard, regisTopIDFFile, regisBotIDFFile, settings.Extent, log, logIndentLevel + 2);
                        regisLayer = new Layer(layerName, regisModellayerNumber, LayerType.Aquitard, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                        isValidModel = regisLayer.CheckLayer(log, logIndentLevel + 2) && isValidModel;
                    }
                    else
                    {
                        regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, regisLayerType, regisTopIDFFile, regisBotIDFFile, settings.Extent, log, logIndentLevel + 2);
                        regisLayer = new Layer(layerName, regisModellayerNumber, regisLayerType, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                        isValidModel = regisLayer.CheckLayer(log, logIndentLevel + 2) && isValidModel;
                    }

                    if (regisLayerModelMap.IsOrdered)
                    {
                        // Do level equality checks if an order was defined
                        if (prevLayers != null)
                        {
                            // Check that BOT-values of previous layer are equal to all non-NoData-values of this REGIS-layer
                            isValidModel = prevLayers.CheckLevelEquality(regisLayer, regisLayer.LayerName + "-t", log, logIndentLevel + 2) && isValidModel;

                            // Fill up NoData-values of current layer with levels of previous layer(s)
                            regisLayer.TOPIDFFile.ReplaceValues(regisLayer.TOPIDFFile.NoDataValue, prevLayers.TOPIDFFile);
                            regisLayer.BOTIDFFile.ReplaceValues(regisLayer.BOTIDFFile.NoDataValue, prevLayers.BOTIDFFile);

                            prevLayers = regisLayer;
                            //prevLayers.layerName = layerName;
                            //prevLayers.modellayerNumber = regisModellayerNumber;
                            // prevLayers.botIDFFile.WriteFile(Path.Combine(settings.outputPath, "PREVLayers-BOT.IDF"));
                        }
                        else
                        {
                            prevLayers = regisLayer;
                            prevLayers.BOTIDFFile.Filename = Path.Combine(settings.OutputPath, "PREVLayers-BOT.IDF");
                            // prevLayers.botIDFFile.WriteFile();
                        }
                    }
                }
            }

            log.AddInfo();

            // Write logfiles when errors and/or warnings occurred
            if ((log.Warnings.Count > 0) || (log.Errors.Count > 0))
            {
                log.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel);
                string baseFilename = log.BaseFilename;
                if (modelname != null)
                {
                    // Temporary change basefilename to force results to be written to subdirectory named 'modelname'
                    log.BaseFilename = Path.Combine(Path.Combine(Path.GetDirectoryName(log.BaseFilename), modelname), Path.GetFileName(log.BaseFilename));
                }
                log.WriteLogFiles(false);
                IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(log);
                idfFileStatsWriter.WriteExcelFile();
                if (modelname != null)
                {
                    log.BaseFilename = baseFilename;
                }
                if (log.Warnings.Count > 0)
                {
                    LayerModel.WriteWarningLegend(Path.Combine(Path.GetDirectoryName(log.BaseFilename), "iMODLayerManager_warnings.leg"), log);
                }
                if (log.Errors.Count > 0)
                {
                    LayerModel.WriteErrorLegend(Path.Combine(Path.GetDirectoryName(log.BaseFilename), "iMODLayerManager_errors.leg"), log);
                }

                log.AddInfo();
            }

            if (isValidModel)
            {
                if (log.Warnings.Count > 0)
                {
                    log.AddWarning("No fatal model inconsistencies in REGIS-model, but " + log.Warnings.Count + " warnings found.", logIndentLevel);
                    log.AddInfo("Check log and warning-IDF's in " + settings.OutputPath, logIndentLevel);
                }
                else
                {
                    log.AddInfo("No model inconsistencies found in REGIS-model.", logIndentLevel);
                }
            }
            else
            {
                log.AddError("Model inconsistencies found in REGIS-model: " + log.Warnings.Count + " warnings and " + log.Errors.Count + " errors.", logIndentLevel);
                log.AddInfo("Check log and error/warning-IDF's in " + settings.OutputPath, logIndentLevel);
            }

            log.AddInfo();

            return isValidModel;
        }

        /// <summary>
        /// Calculates kD-values, c-values and thickness for layers in specified REGIS LayerModelMap
        /// </summary>
        /// <param name="inputLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public override int CalculateKDC(LayerModelMap regisLayerModelMap, SIFToolSettings settings, IDFLog log, int logIndentLevel = 0)
        {
            IDFFile regisTopIDFFile;
            IDFFile regisBotIDFFile;
            IDFFile regisKDCIDFFile;
            LayerType regisLayerType;
            int regisModellayerNumber;

            log.AddInfo("Calculating and writing kD/c-values ...", logIndentLevel);
            for (int fileIdx = 0; fileIdx < regisLayerModelMap.TOPFilenames.Length; fileIdx++)
            {
                string layerName = REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.TOPFilenames[fileIdx]);
                if (layerName != null)
                {
                    // Retrieve characteristics of new layer: aquifer/aquitard and iMOD-layernumber, etc
                    RetrieveLayerInfo(regisLayerModelMap, fileIdx, out regisLayerType, out regisModellayerNumber, log, logIndentLevel);
                    log.AddInfo("Calculating kD/c-values for REGIS-layer '" + layerName + "' ...", logIndentLevel + 1);

                    // Actually read top-, bot- and kD/c-files
                    regisTopIDFFile = IDFFile.ReadFile(regisLayerModelMap.TOPFilenames[fileIdx], false, log, logIndentLevel + 1, settings.Extent);
                    regisBotIDFFile = IDFFile.ReadFile(regisLayerModelMap.BOTFilenames[fileIdx], false, log, logIndentLevel + 1, settings.Extent);

                    // Write thickness IDF-file
                    IDFFile regisThicknessIDFFile = regisTopIDFFile - regisBotIDFFile;
                    string regisThicknessFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputThicknessSubdirname, layerName + Properties.Settings.Default.REGISThicknessPostfix + ".IDF"));
                    regisThicknessIDFFile.WriteFile(regisThicknessFilename);

                    // Create a Layer object with all inputdata for this REGIS layer and calculate kDc for this layer
                    if (regisLayerType == LayerType.Complex)
                    {
                        // Complex layers are checked for both kD- and c-values
                        log.AddInfo("calculating kD-values for " + REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.BOTFilenames[fileIdx]) + "-complex aquifer-part ...", logIndentLevel + 1);
                        regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, LayerType.Aquifer, regisTopIDFFile, regisBotIDFFile, settings.Extent, log);
                        if (regisKDCIDFFile == null)
                        {
                            if (settings.IsSkipWriteKDCDefault)
                            {
                                log.AddInfo("Input k-file missing, writing of kD-file is skipped ...", logIndentLevel + 2);
                            }
                            else
                            {
                                throw new ToolException("No kh-, kv-, kD- or c-files were found: kD-values cannot be calculated");
                            }
                        }
                        else
                        {
                            // regisLayer = new Layer(layerName, regisModellayerNumber, LayerType.Aquifer, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                            string kdcFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputKDSubdirname, layerName + Properties.Settings.Default.REGISkDFilePostfix + ".IDF"));
                            regisKDCIDFFile.WriteFile(kdcFilename);

                            log.AddInfo("calculating c-values for " + REGISLayerModelMap.GetREGISPrefix(regisLayerModelMap.BOTFilenames[fileIdx]) + "-complex aquitard-part ...", logIndentLevel + 1);
                            regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, LayerType.Aquitard, regisTopIDFFile, regisBotIDFFile, settings.Extent, log);
                            if (regisKDCIDFFile == null)
                            {
                                if (settings.IsSkipWriteKDCDefault)
                                {
                                    log.AddInfo("Input k-file missing, writing of c-file is skipped ...", logIndentLevel + 2);
                                }
                                else
                                {
                                    throw new ToolException("No kh-, kv-, kD- or c-files were found: c-values cannot be calculated");
                                }
                            }
                            else
                            {
                                // regisLayer = new Layer(layerName, regisModellayerNumber, LayerType.Aquitard, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                                kdcFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputCSubdirname, layerName + Properties.Settings.Default.REGISCFilePostfix + ".IDF"));
                                regisKDCIDFFile.WriteFile(kdcFilename);
                            }
                        }
                    }
                    else
                    {
                        regisKDCIDFFile = RetrieveKDCFile(regisLayerModelMap, fileIdx, regisLayerType, regisTopIDFFile, regisBotIDFFile, settings.Extent, log);
                        if (regisKDCIDFFile == null)
                        {
                            if (settings.IsSkipWriteKDCDefault)
                            {
                                log.AddInfo("Input k-file missing, writing of kD/c-file is skipped ...", logIndentLevel + 2);
                            }
                            else
                            {
                                throw new ToolException("No kh-, kv-, kD- or c-files were found: kD/c-values cannot be calculated");
                            }
                        }
                        else
                        {
                            // regisLayer = new Layer(layerName, regisModellayerNumber, regisLayerType, regisTopIDFFile, regisBotIDFFile, regisKDCIDFFile);
                            string kdcFilename = null;
                            if (regisLayerType == LayerType.Aquifer)
                            {
                                kdcFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputKDSubdirname, layerName + Properties.Settings.Default.REGISkDFilePostfix + ".IDF"));
                            }
                            else
                            {
                                kdcFilename = Path.Combine(settings.OutputPath, Path.Combine(settings.OutputCSubdirname, layerName + Properties.Settings.Default.REGISCFilePostfix + ".IDF"));
                            }

                            regisKDCIDFFile.WriteFile(kdcFilename);
                        }
                    }
                }
            }

            log.AddInfo();

            return 0;
        }

        /// <summary>
        /// Retrieves the corresponding kDCFile, either a kD- or a C-value. A non-null file with a non-null values property is guaranteed
        /// When it is not found in the list of kh/kv/kD/c-filenames, a copy is made of the topIDFFile and it's non-noData values are replaced by default kD/c-values or calculated from default kh/v-values
        /// </summary>
        /// <param name="regisLayerModelMap"></param>
        /// <param name="fileIdx"></param>
        /// <param name="layerType"></param>
        /// <param name="topIDFFile"></param>
        /// <param name="botIDFFile"></param>
        /// <param name="extent"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        protected IDFFile RetrieveKDCFile(LayerModelMap regisLayerModelMap, int fileIdx, LayerType layerType, IDFFile topIDFFile, IDFFile botIDFFile, Extent extent, Log log, int logIndentLevel = 0)
        {
            string topFilename = Path.GetFileNameWithoutExtension(topIDFFile.Filename);
            IDFFile idfFile = null;

            int idx1 = -1;
            if (layerType == LayerType.Complex)
            {
                throw new Exception("RetrieveKDCFile cannot be called for complex layers, call twice with LayerType Aquifer or Aquitard");
            }
            else if (layerType == LayerType.Aquitard)
            {
                idx1 = topFilename.IndexOf(Properties.Settings.Default.REGISAquitardAbbr);
            }
            else
            {
                idx1 = topFilename.IndexOf(Properties.Settings.Default.REGISAquiferAbbr);
            }

            if (idx1 > 0)
            {
                // Filename format is iMODLayerManager format, start parsing and find corresponding kDc-file
                if (layerType == LayerType.Aquitard)
                {
                    idx1 += Properties.Settings.Default.REGISAquitardAbbr.Length;
                }
                else
                {
                    idx1 += Properties.Settings.Default.REGISAquiferAbbr.Length;
                }
                idx1 = idx1 + 2;

                int idx2 = topFilename.IndexOf(Properties.Settings.Default.REGISTopFilePatternString);

                string searchString = topFilename.Substring(idx1 + 1, idx2 - idx1).ToLower();

                string[] filenames = null;
                if (layerType == LayerType.Aquitard)
                {
                    filenames = regisLayerModelMap.VCWFilenames;
                }
                else
                {
                    filenames = regisLayerModelMap.KDWFilenames;
                }

                // First try to find file in expected list (kD in for aquifers, c for aquitards)
                foreach (string filename in filenames)
                {
                    if ((filename != null) && filename.ToLower().Contains(searchString))
                    {
                        idfFile = IDFFile.ReadFile(filename, false, log, logIndentLevel, extent);
                        if ((idfFile != null) && (idfFile.values != null))
                        {
                            return idfFile;
                        }
                    }
                }

                // File is not found. Sometimes, for schematisation purposes, a REGIS-aquitard is placed in an iMOD-aquifer or inverse
                // Try the alternative file: kD for aquitards, c for aquifers
                filenames = null;
                if (layerType == LayerType.Aquitard)
                {
                    filenames = regisLayerModelMap.KDWFilenames;
                }
                else
                {
                    filenames = regisLayerModelMap.VCWFilenames;
                }

                foreach (string filename in filenames)
                {
                    if ((filename != null) && filename.ToLower().Contains(searchString))
                    {
                        idfFile = IDFFile.ReadFile(filename, false, log, logIndentLevel, extent);

                        if (layerType == LayerType.Aquitard)
                        {
                            // the value from file is actually a kD-value, convert it to a c-value
                            idfFile = LayerUtils.ConvertKDtoCIDFFile(idfFile, topIDFFile, botIDFFile);
                        }
                        else
                        {
                            // the value from file is actually a c-value, convert it to a kD-value
                            idfFile = LayerUtils.ConvertCtoKDIDFFile(idfFile, topIDFFile, botIDFFile);
                        }

                        if ((idfFile != null) && (idfFile.values != null))
                        {
                            return idfFile;
                        }
                    }
                }
            }
            else
            {
                // Filename format is not iMODLayerManager format, check REGIS format
                string kdFilename = (regisLayerModelMap.KDWFilenames != null) && (fileIdx < regisLayerModelMap.KDWFilenames.Length) ? regisLayerModelMap.KDWFilenames[fileIdx] : null;
                string cFilename = (regisLayerModelMap.VCWFilenames != null) && (fileIdx < regisLayerModelMap.VCWFilenames.Length) ? regisLayerModelMap.VCWFilenames[fileIdx] : null;
                string khFilename = (regisLayerModelMap.KHVFilenames != null) && (fileIdx < regisLayerModelMap.KHVFilenames.Length) ? regisLayerModelMap.KHVFilenames[fileIdx] : null;
                string kvFilename = (regisLayerModelMap.KVVFilenames != null) && (fileIdx < regisLayerModelMap.KVVFilenames.Length) ? regisLayerModelMap.KVVFilenames[fileIdx] : null;

                if (layerType == LayerType.Aquifer)
                {
                    if ((khFilename != null) && !khFilename.Equals(string.Empty))
                    {
                        IDFFile khIDFFile = ReadIDFFileOrConstant(khFilename, false, log, logIndentLevel, extent);
                        IDFFile thickness = topIDFFile - botIDFFile;
                        idfFile = thickness * khIDFFile;
                    }
                    else if ((kvFilename != null) && !kvFilename.Equals(string.Empty))
                    {
                        IDFFile kvIDFFile = ReadIDFFileOrConstant(kvFilename, false, log, logIndentLevel, extent);
                        IDFFile thickness = topIDFFile - botIDFFile;
                        idfFile = thickness * kvIDFFile;
                    }
                    else if ((kdFilename != null) && !kdFilename.Equals(string.Empty))
                    {
                        idfFile = IDFFile.ReadFile(kdFilename, false, log, logIndentLevel, extent);
                    }
                    else if ((cFilename != null) && !cFilename.Equals(string.Empty))
                    {
                        idfFile = IDFFile.ReadFile(cFilename, false, log, logIndentLevel, extent);
                        idfFile = LayerUtils.ConvertKDtoCIDFFile(idfFile, topIDFFile, botIDFFile);
                    }
                }
                else
                {
                    if ((kvFilename != null) && !kvFilename.Equals(string.Empty))
                    {
                        IDFFile kvIDFFile = ReadIDFFileOrConstant(kvFilename, false, log, logIndentLevel, extent);
                        IDFFile thickness = topIDFFile - botIDFFile;
                        idfFile = thickness / kvIDFFile;
                    }
                    else if ((khFilename != null) && !khFilename.Equals(string.Empty))
                    {
                        IDFFile khIDFFile = ReadIDFFileOrConstant(khFilename, false, log, logIndentLevel, extent);
                        IDFFile thickness = topIDFFile - botIDFFile;
                        idfFile = thickness / khIDFFile;
                    }
                    else if ((cFilename != null) && !cFilename.Equals(string.Empty))
                    {
                        idfFile = IDFFile.ReadFile(cFilename, false, log, logIndentLevel, extent);
                    }
                    else if ((kdFilename != null) && !kdFilename.Equals(string.Empty))
                    {
                        idfFile = IDFFile.ReadFile(kdFilename, false, log, logIndentLevel, extent);
                        idfFile = LayerUtils.ConvertCtoKDIDFFile(idfFile, topIDFFile, botIDFFile);
                    }
                }

                if ((idfFile != null) && (idfFile.values != null))
                {
                    return idfFile;
                }
            }

            // File is not found
            if (!Settings.IsSkipWriteKDCDefault)
            {
                // Create default file
                if (layerType == LayerType.Aquitard)
                {
                    if (regisLayerModelMap.HasVCWFiles() || regisLayerModelMap.HasKVVFiles())
                    {
                        // Add log message only if other c or kv-files have been defined
                        log.AddWarning("No c/kv-values found, using default kv-value (" + Properties.Settings.Default.DefaultKVValue + ") for " + topFilename, logIndentLevel);
                    }
                    idfFile = (IDFFile)topIDFFile.Copy(string.Empty);
                    for (int rowidx = 0; rowidx < idfFile.NRows; rowidx++)
                    {
                        for (int colidx = 0; colidx < idfFile.NCols; colidx++)
                        {
                            float topValue = topIDFFile.values[rowidx][colidx];
                            float botValue = botIDFFile.values[rowidx][colidx];
                            if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue))
                            {
                                idfFile.values[rowidx][colidx] = (topValue - botValue) / Properties.Settings.Default.DefaultKVValue;
                            }
                            else
                            {
                                idfFile.values[rowidx][colidx] = 0;
                            }
                        }
                    }
                }
                else
                {
                    idfFile = (IDFFile)topIDFFile.Copy(string.Empty);
                    if (regisLayerModelMap.HasKDWFiles() || regisLayerModelMap.HasKHVFiles())
                    {
                        // Add log message only if other kD or kh-files have been defined
                        log.AddWarning("No kD/kh-values found, using default kh-value (" + Properties.Settings.Default.DefaultKHValue + ") for " + topFilename, logIndentLevel);
                    }
                    for (int rowidx = 0; rowidx < idfFile.NRows; rowidx++)
                    {
                        for (int colidx = 0; colidx < idfFile.NCols; colidx++)
                        {
                            float topValue = topIDFFile.values[rowidx][colidx];
                            float botValue = botIDFFile.values[rowidx][colidx];
                            if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue))
                            {
                                idfFile.values[rowidx][colidx] = (topValue - botValue) * Properties.Settings.Default.DefaultKHValue;
                            }
                            else
                            {
                                idfFile.values[rowidx][colidx] = 0;
                            }
                        }
                    }
                }

                // Replace NoData-values by zero value
                idfFile.ReplaceValues(idfFile.NoDataValue, 0);

                return idfFile;
            }
            else
            {
                // No default was created
                return null;
            }
        }

        protected void RetrieveLayerInfo(LayerModelMap regisLayerModelMap, int fileIdx, out LayerType layerType, out int layerNumber, Log log, int logIndentlevel)
        {
            string regisFilename = regisLayerModelMap.BOTFilenames[fileIdx];
            bool isAquifer = regisLayerModelMap.IsAquifer(fileIdx);
            bool isAquitard = regisLayerModelMap.IsAquitard(fileIdx);
            bool isComplex = isAquifer && isAquitard;
            if (!isAquifer && !isAquitard)
            {
                log.AddWarning("File is neither aquifer (\"" + Properties.Settings.Default.REGISAquiferAbbr +
                    "\") nor aquitard (\"" + Properties.Settings.Default.REGISAquitardAbbr + "\") and is skipped: " + Path.GetFileName(regisFilename), logIndentlevel);
            }

            if (isComplex)
            {
                layerType = LayerType.Complex;
            }
            else if (isAquifer)
            {
                layerType = LayerType.Aquifer;
            }
            else
            {
                layerType = LayerType.Aquitard;
            }

            layerNumber = regisLayerModelMap.LayerNumbers[fileIdx];
        }
    }
}


