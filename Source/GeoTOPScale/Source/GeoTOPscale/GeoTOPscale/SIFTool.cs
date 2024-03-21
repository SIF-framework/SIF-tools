// GeoTOPScale is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GeoTOPScale.
// 
// GeoTOPScale is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GeoTOPScale is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GeoTOPScale. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.ARR;
using System.Globalization;
using System.Windows.Forms;

namespace Sweco.SIF.GeoTOPscale
{
    public class SIFTool : SIFToolBase
    {
        protected const string modelname = "VOXELMODEL_STAT-MF6";
        protected const string mf6SubmodelSubDir = "GWF_1";

        protected string khPrefix = null;
        protected string kvPrefix = null;
        protected string icPrefix = null;
        protected string iboundPrefix = null;
        protected string botmPrefix = null;
        protected string chdPrefix = null;
        protected string mf6Path = null;

        protected string modelInputPath = null;
        protected string modelOutputPath = null;
        protected string relativeModelInputPath = null;
        protected Extent outputExtent = null;
        protected float outputXcellSize = float.NaN;
        protected float outputYcellSize = float.NaN;

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

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("K. Jansen");
            AddAuthor("K. van der Hauw");
            ToolPurpose = "SIF-tool for calculating aggregated kv-value for GeoTOP voxelmodel";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            Initialize(settings);

            // Create output path if not yet existing
            string outputPath = settings.OutputPath;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Read Excel permeability table
            PermeabilityData permeabilityData = new PermeabilityData();
            permeabilityData.ReadPermeabilityTable(settings, Log);

            // Read TOP/BOT-file, area outside scale extent is set to NoData-values
            Layer selLayer = ReadTOPBOTFiles(settings);
            
            // Read voxel files
            Log.AddInfo("Reading voxel files ...");
            VoxelInfo voxelInfo = new VoxelInfo(settings.StratVoxelPath, settings.LithoVoxelPath);

            // Check that extents of input files do overlap
            if (!voxelInfo.Extent.Intersects(selLayer.TOPIDFFile.Extent))
            {
                throw new ToolException("Invalid input extent, voxel extent (" + voxelInfo.Extent + ") has no overlap with extent of TOP IDF-file: " + selLayer.TOPIDFFile.Extent);
            }
            outputExtent = voxelInfo.Extent.Clip(selLayer.TOPIDFFile.Extent); //??is this the same extent as topidffile when cellsize between strat en top idffile differ??
            outputXcellSize = voxelInfo.XCellSize;
            outputYcellSize = voxelInfo.YCellSize;

            // Read selected voxels and retrieve relevant characteristics
            int layerCount;
            float modelBOTLevel;
            Dictionary<int, int> maxboundDictionary;
            IDFFile totalThicknessIDFFile;
            IDFFile stackResistanceIDFFile;
            ProcessLayerFiles(voxelInfo, selLayer, permeabilityData, out modelBOTLevel, out layerCount, out maxboundDictionary, out totalThicknessIDFFile, out stackResistanceIDFFile, settings);

            // Run MF6-model
            Runmodel(ref exitcode, settings);

            // Read MF6-results
            List<IDFFile> headList = new List<IDFFile>();
            List<IDFFile> fluxList = new List<IDFFile>();
            ReadMF6Results(layerCount + 2, out headList, out fluxList, settings);

            // Calculate and write kv-value for specified layer
            CreateKVLayers(selLayer, fluxList, layerCount + 2, totalThicknessIDFFile, stackResistanceIDFFile, settings);

            ToolSuccessMessage = "Finished scaling GeoTOP kv-values";

            return exitcode;
        }

        protected void Initialize(SIFToolSettings settings)
        {
            // Define paths
            mf6Path = Path.Combine(settings.OutputPath, "MF6-model");
            modelInputPath = Path.Combine(mf6Path, mf6SubmodelSubDir, "MODELINPUT");
            modelOutputPath = Path.Combine(mf6Path, mf6SubmodelSubDir, "MODELOUTPUT");
            relativeModelInputPath = ".\\" + mf6SubmodelSubDir + "\\MODELINPUT";

            // Define prefixes
            khPrefix = "K_L";
            kvPrefix = "K33_L";
            icPrefix = "IC_L";
            iboundPrefix = "IBOUND_L";
            botmPrefix = "BOTM_L";
            chdPrefix = "SYSX";
        }

        /// <summary>
        /// Read specified IDF-files of that define selected layer within voxel model; also set area outside scale extent to NoData-values
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected Layer ReadTOPBOTFiles(SIFToolSettings settings)
        {
            Log.AddInfo("Reading TOP-/BOT-files ...");
            IDFFile topIDFFile = IDFFile.ReadFile(settings.TOPFilename);
            IDFFile botIDFFile = IDFFile.ReadFile(settings.BOTFilename);

            //it is assumed that top/bot file have same extent and cell size.
            if (!topIDFFile.Extent.Equals(botIDFFile.Extent))
            {
                throw new ToolException("Extents of TOP and BOT IDF-files do not match: " + topIDFFile.Extent + " versus " + botIDFFile.Extent);
            }
            else if (!topIDFFile.XCellsize.Equals(botIDFFile.XCellsize))
            {
                throw new ToolException("Cellsize of TOP and BOT IDF-files do not match: " + topIDFFile.XCellsize + " versus " + botIDFFile.XCellsize);
            }

            // Set TOP/BOT outside area to NoData
            if (settings.ClipExtent != null)
            {
                if (!settings.ClipExtent.Intersects(topIDFFile.Extent))
                {
                    throw new ToolException("Clipextent: (" + settings.ClipExtent + ") has no overlap with TOP IDF-file: " + topIDFFile.Extent);
                }
                //// TODO, remove ITB-levels if IDF-file is not a voxel file
                //// ITB-levels are needed for clipping
                //topIDFFile.SetITBLevels(-9999, -9999);
                //botIDFFile.SetITBLevels(-9999, -9999);
                topIDFFile = topIDFFile.ClipIDF(settings.ClipExtent);
                botIDFFile = botIDFFile.ClipIDF(settings.ClipExtent);
                float xmin = settings.ClipExtent.llx;
                float xmax = settings.ClipExtent.urx;
                float ymin = settings.ClipExtent.lly;
                float ymax = settings.ClipExtent.ury;
                for (int rowIdx = 0; rowIdx < topIDFFile.NRows; rowIdx++)
                {
                    float y = topIDFFile.GetY(rowIdx);
                    for (int colIdx = 0; colIdx < topIDFFile.NCols; colIdx++)
                    {
                        float x = topIDFFile.GetX(colIdx);
                        if (x < xmin || x > xmax || y < ymin || y > ymax)
                        {

                            topIDFFile.SetValue(x, y, topIDFFile.NoDataValue);
                            botIDFFile.SetValue(x, y, botIDFFile.NoDataValue);
                        }
                        else // Check that TOP > BOT, otherwise an error occurs in MF6
                        {
                            float top = topIDFFile.GetValue(x, y);
                            float bot = botIDFFile.GetValue(x, y);
                            if (top < bot)
                            {
                                throw new ToolException("Invalid levels for TOP/BOT-cell (row,col: " + (rowIdx + 1) + "," + colIdx + ")," 
                                    + " xy-coordinate (" + x.ToString(EnglishCultureInfo) + "," + y.ToString(EnglishCultureInfo) + "): TOP-value (" + top + ") cannot be lower than BOT-value (" + bot + ")");
                            }
                        }
                    }
                }
                topIDFFile.UpdateMinMaxValue();
                botIDFFile.UpdateMinMaxValue();
            }

            return new Layer(topIDFFile, botIDFFile);
        }

        /// <summary>
        /// Read selected voxels and retrieve relevant characteristics of voxel model
        /// </summary>
        /// <param name="voxelInfo"></param>
        /// <param name="selLayer"></param>
        /// <param name="permeabilityData"></param>
        /// <param name="modelBOTLevel"></param>
        /// <param name="layerCount"></param>
        /// <param name="maxboundDictionary"></param>
        /// <param name="totalThicknessIDFFile"></param>
        /// <param name="stackResistanceIDFFile"></param>
        /// <param name="settings"></param>
        protected void ProcessLayerFiles(VoxelInfo voxelInfo, Layer selLayer, PermeabilityData permeabilityData, out float modelBOTLevel, out int layerCount, out Dictionary<int, int> maxboundDictionary, out IDFFile totalThicknessIDFFile, out IDFFile stackResistanceIDFFile, SIFToolSettings settings)
        {
            modelBOTLevel = float.NaN;
            layerCount = 0;

            Log.AddInfo("Writing modelinput files ...");
            float noDataValue = selLayer.TOPIDFFile.NoDataValue;
            IDFFile topL1 = new IDFFile("topL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile botL1 = new IDFFile("botL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile minBot = new IDFFile("minBot", outputExtent, outputXcellSize, noDataValue);
            IDFFile botLBot = new IDFFile("botLBot", outputExtent, outputXcellSize, noDataValue);
            topL1.SetValues(topL1.NoDataValue);
            botL1.SetValues(botL1.NoDataValue);
            minBot.SetValues(minBot.NoDataValue);
            botLBot.SetValues(botLBot.NoDataValue);

            // Define IDF-file counter for thickness per cell
            totalThicknessIDFFile = new IDFFile("totalThickness", outputExtent, outputXcellSize, noDataValue);
            stackResistanceIDFFile = new IDFFile("totalResistance", outputExtent, outputXcellSize, noDataValue);

            // Create path if not yet existing
            if (!Directory.Exists(Path.Combine(modelInputPath, "CHD6")))
            {
                Directory.CreateDirectory(Path.Combine(modelInputPath, "CHD6"));
            }
            if (!Directory.Exists(Path.Combine(modelInputPath, "DIS6")))
            {
                Directory.CreateDirectory(Path.Combine(modelInputPath, "DIS6"));
            }
            if (!Directory.Exists(Path.Combine(modelInputPath, "IC6")))
            {
                Directory.CreateDirectory(Path.Combine(modelInputPath, "IC6"));
            }
            if (!Directory.Exists(Path.Combine(modelInputPath, "NPF6")))
            {
                Directory.CreateDirectory(Path.Combine(modelInputPath, "NPF6"));
            }

            List<IDFFile> iboundList = new List<IDFFile>();
            List<IDFFile> botmList = new List<IDFFile>();
            maxboundDictionary = new Dictionary<int, int>();

            // It is assumed that voxels are sorted from top to bot, which also defines the layer number order
            int lithoVoxelIdx = 0;
            foreach (string stratVoxelName in voxelInfo.StratFilenames)
            {
                IDFFile stratIDFFile = IDFFile.ReadFile(stratVoxelName, true);
                IDFFile lithoIDFFile = IDFFile.ReadFile(voxelInfo.LithoFilenames[lithoVoxelIdx], true);
                bool isVoxelSelected = true;
                if(settings.BoundaryLayerSelectionMethod < 0 || settings.BoundaryLayerSelectionMethod > 2)
                {
                    throw new ToolException("Boundary selection method must be 0, 1, or 2");
                }
                else
                {
                    switch (settings.BoundaryLayerSelectionMethod)
                    {
                        case 0:
                            if(stratIDFFile.TOPLevel > selLayer.TOPIDFFile.MaxValue  || stratIDFFile.BOTLevel < selLayer.BOTIDFFile.MinValue)
                            {
                                isVoxelSelected = false;
                            }
                            break;
                        case 1:
                            if (stratIDFFile.TOPLevel > selLayer.TOPIDFFile.MaxValue + voxelInfo.Thickness || stratIDFFile.BOTLevel < selLayer.BOTIDFFile.MinValue - voxelInfo.Thickness)
                            {
                                isVoxelSelected = false;
                            }
                            break;
                        case 2:
                            if (stratIDFFile.TOPLevel > selLayer.TOPIDFFile.MaxValue + 0.5 * voxelInfo.Thickness || stratIDFFile.BOTLevel < selLayer.BOTIDFFile.MinValue - 0.5 * voxelInfo.Thickness)
                            {
                                isVoxelSelected = false;
                            }
                            break;
                    }
                }

                if (isVoxelSelected) 
                {
                    layerCount++;
                    // Log.AddInfo("layer L" + (layerNumber + 1) + " ...", 1);

                    // Create modelinput layerfiles
                    IDFFile khIDFFile = new IDFFile("kh", outputExtent, outputXcellSize, noDataValue);
                    IDFFile kvIDFFile = new IDFFile("kv", outputExtent, outputXcellSize, noDataValue);
                    IDFFile icIDFFile = new IDFFile("ic", outputExtent, outputXcellSize, noDataValue);
                    IDFFile iboundIDFFile = new IDFFile("ibound", outputExtent, outputXcellSize, noDataValue);
                    IDFFile botmIDFFile = new IDFFile("botm", outputExtent, outputXcellSize, noDataValue);

                    permeabilityData.RetrieveKValues(selLayer, voxelInfo.Thickness, totalThicknessIDFFile, stackResistanceIDFFile, ref khIDFFile, ref kvIDFFile, stratIDFFile, lithoIDFFile, modelOutputPath, settings);
                    UpdateMaxTOPBOT(voxelInfo, khIDFFile, ref botLBot, ref minBot, ref botL1, ref topL1);

                    // Retrieve values if kh==NoData
                    icIDFFile.SetValues(icIDFFile.NoDataValue);
                    iboundIDFFile.SetValues(-1);
                    botmIDFFile.SetValues(botmIDFFile.NoDataValue);

                    // Retrieve values if kh!=NoData
                    icIDFFile.ReplaceValues(khIDFFile, (float)0.5);
                    iboundIDFFile.ReplaceValues(khIDFFile, 1);
                    botmIDFFile.ReplaceValues(minBot, minBot);

                    // Correct ibound and botm
                    //IboundResetFirstValue(ref ibound);
                    iboundList.Add(iboundIDFFile);
                    botmList.Add(botmIDFFile);

                    // Write ARR file per layer  
                    ARRFile khARRFile = new ARRFile(khIDFFile);
                    ARRFile kvARRFile = new ARRFile(kvIDFFile);
                    ARRFile icARRFile = new ARRFile(icIDFFile);

                    khARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", khPrefix + (layerCount + 1)));
                    kvARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", kvPrefix + (layerCount + 1)));
                    icARRFile.WriteFile(Path.Combine(modelInputPath, "IC6", icPrefix + (layerCount + 1)));

                    if (layerCount == 1)
                    {
                        modelBOTLevel = khIDFFile.TOPLevel;
                    }
                }
                else
                {
                    // Skip layer, outside min/max range
                }

                lithoVoxelIdx++;
            }

            WriteBoundaryLayerFiles(selLayer, layerCount, botLBot, botL1, topL1, botmList, iboundList, ref maxboundDictionary);

            // Write layer dependent MF6 modelinput files
            WriteMF6InputFiles(modelBOTLevel + voxelInfo.Thickness, selLayer.TOPIDFFile.NRows, selLayer.TOPIDFFile.NCols, layerCount + 2, maxboundDictionary, settings);
        }

        protected void WriteCHDARRFile(ARRFile chdARR, string Filename, int layerNumber, ref Dictionary<int, int> maxboundDictionary)
        {
            // Variables in ARR class
            string Extension = ARRFile.Extension;
            float[] Values = chdARR.Values;
            int NRows = chdARR.NRows;
            int NCols = chdARR.NCols;
            float XLL = chdARR.XLL;
            float YLL = chdARR.YLL;
            float XUR = chdARR.XUR;
            float YUR = chdARR.YUR;
            float NoDataValue = chdARR.NoDataValue;

            StreamWriter sw = null;
            try
            {
                if (!Path.GetDirectoryName(Filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(Filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Filename));
                }

                if (Path.GetExtension(Filename).Equals(string.Empty))
                {
                    Filename += "." + Extension;
                }

                sw = new StreamWriter(Filename, false);

                if (Values.Length > 0)
                {
                    int colIdx = 0;
                    int rowIdx = 0;
                    int maxbound = 0;
                    // Value of 1 gives error in MF6 (sees value 1 as bool and cannot convert to int)
                    for (int valueIdx = 0; valueIdx < Values.Length; valueIdx++)
                    {
                        colIdx++;
                        if (!Values[valueIdx].Equals(chdARR.NoDataValue))
                        {
                            int value = (int)Values[valueIdx];
                            sw.WriteLine("    " + layerNumber + "     " + (rowIdx + 1) + "     " + (colIdx) + "    " + value.ToString("F6", EnglishCultureInfo) +
                                         "        " + value.ToString("F6", EnglishCultureInfo) + "         " + layerNumber);
                            maxbound++;
                        }

                        if (colIdx == NCols)
                        {
                            colIdx = 0;
                            rowIdx++;
                        }
                    }

                    maxboundDictionary.Add(layerNumber, maxbound);
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + Filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        private void UpdateMaxTOPBOT(VoxelInfo voxelInfo, IDFFile kh, ref IDFFile minBotLBot, ref IDFFile minBot, ref IDFFile botL1, ref IDFFile topL1)
        {
            float localBot = kh.BOTLevel;

            for (int rowIdx = 0; rowIdx < kh.NRows; rowIdx++)
            {
                float y = kh.GetY(rowIdx);
                for (int colIdx = 0; colIdx < kh.NCols; colIdx++)
                {
                    float x = kh.GetX(colIdx);
                    float botL1Value = botL1.GetValue(x, y);
                    float minBotValue = minBot.GetValue(x, y);
                    float hasValue = kh.GetValue(x, y);

                    if (!hasValue.Equals(kh.NoDataValue))
                    {
                        // Set maxbot 
                        if (botL1Value.Equals(botL1.NoDataValue))
                        {
                            topL1.SetValue(x, y, localBot + 2 * voxelInfo.Thickness);
                            botL1.SetValue(x, y, localBot + voxelInfo.Thickness);
                        }

                        // Set minbot
                        minBot.SetValue(x, y, localBot);
                        minBotLBot.SetValue(x, y, localBot - voxelInfo.Thickness);
                    }
                }
            }
        }

        /// <summary>
        /// Write MF6 input files for layers that are above and below voxel model and define its boundary
        /// </summary>
        /// <param name="selLayer"></param>
        /// <param name="layerNumber"></param>
        /// <param name="botLBot"></param>
        /// <param name="botL1"></param>
        /// <param name="topL1"></param>
        /// <param name="botmList"></param>
        /// <param name="iboundList"></param>
        /// <param name="maxboundDictionary"></param>
        protected void WriteBoundaryLayerFiles(Layer selLayer, int layerNumber, IDFFile botLBot, IDFFile botL1, IDFFile topL1, List<IDFFile> botmList, List<IDFFile> iboundList, ref Dictionary<int, int> maxboundDictionary)
        {
            // Create path if not yet existing
            string chdPath = Path.Combine(modelInputPath, "CHD6");
            if (!Directory.Exists(Path.Combine(chdPath, chdPrefix.Replace("SYSX", "SYS1"))))
            {
                Directory.CreateDirectory(Path.Combine(chdPath, chdPrefix.Replace("SYSX", "SYS1")));
            }
            if (!Directory.Exists(Path.Combine(chdPath, chdPrefix.Replace("SYSX", "SYS" + (layerNumber + 2)))))
            {
                Directory.CreateDirectory(Path.Combine(chdPath, chdPrefix.Replace("SYSX", "SYS" + (layerNumber + 2))));
            }

            float noDataValue = selLayer.TOPIDFFile.NoDataValue;
            IDFFile khL1IDFFile = new IDFFile("khL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile kvL1IDFFile = new IDFFile("kvL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile icL1IDFFile = new IDFFile("icL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile iboundL1IDFFile = new IDFFile("iboundL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile topmL1IDFFile = new IDFFile("topmL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile botmL1IDFFile = new IDFFile("botmL1", outputExtent, outputXcellSize, noDataValue);
            IDFFile chdL1IDFFile = new IDFFile("chdL1", outputExtent, outputXcellSize, noDataValue);
            
            IDFFile khLbotIDFFile = new IDFFile("khLbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile kvLbotIDFFile = new IDFFile("kvLbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile icLbotIDFFile = new IDFFile("icLbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile iboundLbotIDFFile = new IDFFile("iboundLbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile botmLbotIDFFile = new IDFFile("botmLbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile chdLbotIDFFile = new IDFFile("chdLbot", outputExtent, outputXcellSize, noDataValue);

            // Assign fixed boundary value, only if the layer exist in the (vertical) soil column
            khL1IDFFile.ReplaceValues(topL1, 100000);
            kvL1IDFFile.ReplaceValues(topL1, 100000);
            icL1IDFFile.ReplaceValues(topL1, 1);
            iboundL1IDFFile.ReplaceValues(topL1, 1);
            topmL1IDFFile.SetValues(topL1);
            botmL1IDFFile.SetValues(botL1);
            chdL1IDFFile.SetValues(chdL1IDFFile.NoDataValue);
            chdL1IDFFile.ReplaceValues(topL1, 1);

            khLbotIDFFile.ReplaceValues(topL1, 100000);
            kvLbotIDFFile.ReplaceValues(topL1, 100000);
            icLbotIDFFile.ReplaceValues(topL1, 0);
            iboundLbotIDFFile.ReplaceValues(topL1, 1);
            botmLbotIDFFile.SetValues(botLBot);
            chdLbotIDFFile.SetValues(chdLbotIDFFile.NoDataValue);
            chdLbotIDFFile.ReplaceValues(topL1, 0);

            // ibound correction to 0 within all layers for vertically missing soil column
            // botm correction for all NoData cells above soil column
            int layerCount = 2;
            foreach(IDFFile ibound in iboundList)
            {
                IDFFile ibound2IDFFile = new IDFFile("ibound2", outputExtent, outputXcellSize, noDataValue);
                IDFFile botmIDFFile = botmList[layerCount - 2];

                ibound2IDFFile.SetValues(0);
                ibound2IDFFile.ReplaceValues(topL1, ibound);
                botmIDFFile.ReplaceValues(botmIDFFile.NoDataValue, botL1);

                ARRFile ibound2ARRFile = new ARRFile(ibound2IDFFile);
                ARRFile botmARRFile = new ARRFile(botmIDFFile);
                
                ibound2ARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", iboundPrefix + layerCount));
                botmARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", botmPrefix + layerCount));

                layerCount++;
            }

            //set first value of ibound to 2
            ResetFirstIBOUNDValue(ref iboundL1IDFFile);
            ResetFirstIBOUNDValue(ref iboundLbotIDFFile);

            ARRFile khL1ARRFile = new ARRFile(khL1IDFFile);
            ARRFile kvL1ARRFile = new ARRFile(kvL1IDFFile);
            ARRFile icL1ARRFile = new ARRFile(icL1IDFFile);
            ARRFile iboundL1ARRFile = new ARRFile(iboundL1IDFFile);
            ARRFile topmL1ARRFile = new ARRFile(topmL1IDFFile);
            ARRFile botmL1ARRFile = new ARRFile(botmL1IDFFile);
            ARRFile chdL1ARRFile = new ARRFile(chdL1IDFFile);

            ARRFile khLbotARRFile = new ARRFile(khLbotIDFFile);
            ARRFile kvLbotARRFile = new ARRFile(kvLbotIDFFile);
            ARRFile icLbotARRFile = new ARRFile(icLbotIDFFile);
            ARRFile iboundLbotARRFile = new ARRFile(iboundLbotIDFFile);
            ARRFile botmLbotARRFile = new ARRFile(botmLbotIDFFile);
            ARRFile chdLbotARRFile = new ARRFile(chdLbotIDFFile);

            // Write grids for L1 and bottom layer seperately, these have different characteristics (i.e. boundary and not part of voxel model)
            khL1ARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", khPrefix + 1));
            kvL1ARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", kvPrefix + 1));
            icL1ARRFile.WriteFile(Path.Combine(modelInputPath, "IC6", icPrefix + 1));
            iboundL1ARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", iboundPrefix + 1));
            topmL1ARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", botmPrefix.Replace("BOTM", "TOPM") + 1));
            botmL1ARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", botmPrefix + 1));
            WriteCHDARRFile(chdL1ARRFile, Path.Combine(modelInputPath, "CHD6", chdPrefix.Replace("SYSX", "SYS1" + "\\CHD_T1")), 1, ref maxboundDictionary);

            khLbotARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", khPrefix + (layerNumber + 2)));
            kvLbotARRFile.WriteFile(Path.Combine(modelInputPath, "NPF6", kvPrefix + (layerNumber + 2)));
            icLbotARRFile.WriteFile(Path.Combine(modelInputPath, "IC6", icPrefix + (layerNumber + 2)));
            iboundLbotARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", iboundPrefix + (layerNumber + 2)));
            botmLbotARRFile.WriteFile(Path.Combine(modelInputPath, "DIS6", botmPrefix + (layerNumber + 2)));
            WriteCHDARRFile(chdLbotARRFile, Path.Combine(modelInputPath, "CHD6", chdPrefix.Replace("SYSX", "SYS" + (layerNumber + 2) + "\\CHD_T1")), (layerNumber + 2), ref maxboundDictionary);
        }

        private void ResetFirstIBOUNDValue(ref IDFFile ibound)
        {
            // First value in ibound has value 2 instead of 1
            bool edited = false;
            for (int rowIdx = 0; rowIdx < ibound.NRows; rowIdx++)
            {
                float y = ibound.GetY(rowIdx);
                for (int colIdx = 0; colIdx < ibound.NCols; colIdx++)
                {
                    float x = ibound.GetX(colIdx);
                    float value = ibound.GetValue(x, y);

                    if (value == 1)
                    {
                        ibound.SetValue(x, y, 2);
                        edited = true;
                        break;
                    }
                }
                if (edited)
                {
                    break;
                }
            }
        }

        protected void WriteConstantARRFile(IDFFile copiedIDFFile, float constantValue, int layerNumber, string FileName)
        {
            IDFFile emptyIDFFile = new IDFFile("emptyIDFFile", copiedIDFFile.Extent, copiedIDFFile.XCellsize, copiedIDFFile.NoDataValue);
            emptyIDFFile.SetValues(constantValue);
            ARRFile emptyARRFile = new ARRFile(emptyIDFFile);
            emptyARRFile.WriteFile(FileName + layerNumber);
        }

        /// <summary>
        /// Write MF6 input files for layers of voxel model
        /// </summary>
        /// <param name="topModel"></param>
        /// <param name="nrow"></param>
        /// <param name="ncol"></param>
        /// <param name="nlay"></param>
        /// <param name="maxboundDictionary"></param>
        /// <param name="settings"></param>
        protected void WriteMF6InputFiles(float topModel, int nrow, int ncol, int nlay, Dictionary<int, int> maxboundDictionary, SIFToolSettings settings)
        {
            WriteNAMFile();
            WriteIMFile();
            WriteTDIFile();
            WriteNAM2File(nlay, maxboundDictionary);
            WriteDISFile(nlay, nrow, ncol);
            WriteICFile(nlay);
            WriteNPFFile(nlay);
            WriteOCFile();
            WriteCHD6References(nlay, maxboundDictionary);
        }

        protected void WriteNAMFile()
        {
            string NAMString =
                "# MFSIM.NAM File Generated by iMOD [V5_4 X64 Optimized]\n" +
                "\n" +
                "#General Options\n" +
                "\n" +
                "BEGIN OPTIONS\n" +
                "END OPTIONS\n" +
                "\n" +
                "#Timing Options\n" +
                "\n" +
                "BEGIN TIMING\n" +
                " TDIS6 .\\MFSIM.TDIS6\n" +
                "END TIMING\n" +
                "\n" +
                "#List of Models\n" +
                "\n" +
                "BEGIN MODELS\n" +
                " GWF6 .\\" + mf6SubmodelSubDir + "\\" + modelname + ".NAM " + mf6SubmodelSubDir + "\n" +
                "END MODELS\n" +
                "\n" +
                "#List of Exchanges\n" +
                "\n" +
                "BEGIN EXCHANGES\n" +
                "END EXCHANGES\n" +
                "\n" +
                "#Definition of Numerical Solution\n" +
                "\n" +
                "BEGIN SOLUTIONGROUP 1\n" +
                " MXITER 1\n" +
                " IMS6 .\\MFSIM.IMS6 " + mf6SubmodelSubDir + "\n" +
                "END SOLUTIONGROUP\n";

            FileUtils.WriteFile(mf6Path + "\\MFSIM.NAM", NAMString);
        }

        protected void WriteIMFile()
        {
            string IMString =
                "# IMS6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
                "\n" +
                "#General options\n" +
                "\n" +
                "BEGIN OPTIONS\n" +
                " PRINT_OPTION SUMMARY\n" +
                " COMPLEXITY COMPLEX     \n" +
                " CSV_OUTER_OUTPUT FILEOUT MFSIM_OUTER.CSV\n" +
                " CSV_INNER_OUTPUT FILEOUT MFSIM_INNER.CSV\n" +
                "END OPTIONS\n" +
                "\n" +
                "#Nonlinear options\n" +
                "\n" +
                "BEGIN NONLINEAR\n" +
                " OUTER_DVCLOSE   0.1000000E-02\n" +
                " OUTER_MAXIMUM       5000\n" +
                "END NONLINEAR\n" +
                "\n" +
                "#Linear options\n" +
                "\n" +
                "BEGIN LINEAR\n" +
                " INNER_MAXIMUM        500\n" +
                " INNER_DVCLOSE   0.1000000E-02\n" +
                " INNER_RCLOSE    100.0000    \n" +
                "END LINEAR\n";

            FileUtils.WriteFile(mf6Path + "\\MFSIM.IMS6", IMString);
        }

        protected void WriteTDIFile()
        {
            string TDIString =
              "# TDIS6 File Generated by iMOD[V5_4 X64 Optimized]\n" +
              "\n" +
              "#General Options\n" +
              "\n" +
              "BEGIN OPTIONS\n" +
              " TIME_UNITS DAYS\n" +
              "END OPTIONS\n" +
              "\n" +
              "#Time Dimensions\n" +
              "\n" +
              "BEGIN DIMENSIONS\n" +
              " NPER 1\n" +
              "END DIMENSIONS\n" +
              "\n" +
              "#Stress periods\n" +
              "\n" +
              "BEGIN PERIODDATA\n" +
              " 1.000000,1,1.000000 [STEADY-STATE] [STEADY-STATE]\n" +
              "END PERIODDATA\n";

            FileUtils.WriteFile(mf6Path + "\\MFSIM.TDIS6", TDIString);
        }

        protected void WriteNAM2File(int nlay, Dictionary<int, int> maxboundDictionary)
        {
            string CHDString = null;
            for (int layerNr = 1; layerNr <= nlay; layerNr++)
            {
                maxboundDictionary.TryGetValue(layerNr, out int value);
                if (value > 0)
                {
                    CHDString = CHDString + " CHD6 " + FileUtils.EnsureTrailingSlash(relativeModelInputPath) + modelname + "_SYS" + layerNr + ".CHD6 CHD_SYS" + layerNr + "\n";
                }             
            }

            string nam2String =
              "# " + modelname + ".NAM File Generated by iMOD [V5_4 X64 Optimized]\n" +
              "\n" +
              "#General Options\n" +
              "\n" +
              "BEGIN OPTIONS\n" +
              " LIST .\\GWF_1\\" + modelname + ".LST\n" +
              " NEWTON UNDER_RELAXATION\n" +
              "END OPTIONS\n" +
              "\n" +
              "#List of Packages\n" +
              "\n" +
              "BEGIN PACKAGES\n" +
              " DIS6 " + FileUtils.EnsureTrailingSlash(relativeModelInputPath) + modelname + ".DIS6\n" +
              " IC6  " + FileUtils.EnsureTrailingSlash(relativeModelInputPath) + modelname + ".IC6\n" +
              " NPF6 " + FileUtils.EnsureTrailingSlash(relativeModelInputPath) + modelname + ".NPF6\n" +
              " OC6  " + FileUtils.EnsureTrailingSlash(relativeModelInputPath) + modelname + ".OC6\n" +
              CHDString +
              "END PACKAGES\n";

            FileUtils.WriteFile(mf6Path + "\\" + mf6SubmodelSubDir + "\\" + modelname + ".NAM", nam2String);
        }

        protected void WriteDISFile(int nlay, int nrow, int ncol)
        {
            string IBOUNDString = null;
            string BOTMString = null;
            
                for (int layerNr = 1; layerNr <= nlay; layerNr++)
                {
                        IBOUNDString = IBOUNDString + "  OPEN/CLOSE " + relativeModelInputPath + "\\DIS6\\IBOUND_L" + layerNr + ".ARR FACTOR 1 IPRN -1\n";
                        BOTMString = BOTMString + "  OPEN/CLOSE " + relativeModelInputPath + "\\DIS6\\BOTM_L" + layerNr + ".ARR FACTOR 1.0D0 IPRN -1\n";
                }
            
                string DISString =
                "# DIS6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
                "\n" +
                "General Options\n" +
                "\n" +
                "BEGIN OPTIONS\n" +
                " LENGTH_UNITS METERS\n" +
                " XORIGIN " + outputExtent.llx + "\n" +
                " YORIGIN " + outputExtent.lly + "\n" +
                " ANGROT 0.0\n" +
                "END OPTIONS\n" +
                "\n" +
                "#Model Dimensions\n" +
                "\n" +
                "BEGIN DIMENSIONS\n" +
                " NLAY " + nlay + "\n" +
                " NROW " + nrow + "\n" +
                " NCOL " + ncol + "\n" +
                "END DIMENSIONS\n" +
                "\n" +
                "#Cell Sizes\n" +
                "\n" +
                "BEGIN GRIDDATA\n" +
                " DELR\n" +
                "  CONSTANT " + outputYcellSize.ToString("F6", EnglishCultureInfo) + "\n" +
                " DELC\n" +
                "  CONSTANT " + outputXcellSize.ToString("F6", EnglishCultureInfo) + "\n" +
                "\n" +
                "#Vertical Configuration\n" +
                "\n" +
                "TOP\n" +
                "  OPEN/CLOSE " + relativeModelInputPath + "\\DIS6\\TOPM_L1.ARR FACTOR 1.0D0 IPRN -1\n" +
                "BOTM LAYERED\n" +
                BOTMString +
                "\n" +
                "#Boundary Settings\n" +
                "\n" +
                "IDOMAIN LAYERED\n" +
                IBOUNDString +
                "END GRIDDATA\n";

            FileUtils.WriteFile(modelInputPath + "\\" + modelname + ".DIS6", DISString);
        }

        protected void WriteICFile(int nlay)
        {
            string ICStringpart = null;
            for (int i = 1; i <= nlay; i++)
            {
                ICStringpart = ICStringpart + "  OPEN/CLOSE " + relativeModelInputPath + "\\IC6\\IC_L" + i + ".ARR FACTOR 1.0D0 IPRN -1\n";
            }

            string ICstring =
              "# IC6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
              "\n" +
              "#General Options\n" +
              "\n" +
              "BEGIN OPTIONS\n" +
              "END OPTIONS\n" +
              "\n" +
              "#Initial Head Data\n" +
              "\n" +
              "BEGIN GRIDDATA\n" +
              " STRT LAYERED\n" +
              ICStringpart +
              "END GRIDDATA\n";

            FileUtils.WriteFile(modelInputPath + "\\" + modelname + ".IC6", ICstring);
        }

        protected void WriteNPFFile(int nlay)
        {
            string iCellTypeString = null;
            string khString = null;
            string kvString = null;

            for (int layerNr = 1; layerNr <= nlay; layerNr++)
            {
                iCellTypeString = iCellTypeString + "  CONSTANT 0\n";
                //OPEN/CLOSE.\GWF_1\MODELINPUT\NPF6\ICELLTYPE_L1.ARR FACTOR 1 IPRN -1
                khString = khString + "  OPEN/CLOSE " + relativeModelInputPath + "\\NPF6\\K_L" + layerNr + ".ARR FACTOR 1.0D0 IPRN -1\n";
                kvString = kvString + "  OPEN/CLOSE " + relativeModelInputPath + "\\NPF6\\K33_L" + layerNr + ".ARR FACTOR 1.0D0 IPRN -1\n";
            }

            string NPFstring =
               "# NPF6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
               "\n" +
               "#General Options\n" +
               "\n" +
               "BEGIN OPTIONS\n" +
               " SAVE_FLOWS\n" +
               " ALTERNATIVE_CELL_AVERAGING AMT-HMK\n" +
               " SAVE_SATURATION\n" +
               "END OPTIONS\n" +
               "\n" +
               "#Geology Options\n" +
               "\n" +
               "BEGIN GRIDDATA\n" +
               " ICELLTYPE LAYERED\n" +
               iCellTypeString +
               " K LAYERED\n" +
               khString +
               " K33 LAYERED\n" +
               kvString +
               "END GRIDDATA\n";

            FileUtils.WriteFile(modelInputPath + "\\" + modelname + ".NPF6", NPFstring);
        }

        protected void WriteOCFile()
        {
            string OCstring =
                "# OC6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
                "\n" +
                "#General Options\n" +
                "\n" +
                "BEGIN OPTIONS\n" +
                " BUDGET FILEOUT .\\" + mf6SubmodelSubDir + "\\MODELOUTPUT\\BUDGET\\BUDGET.CBC\n" +
                " HEAD FILEOUT .\\" + mf6SubmodelSubDir + "\\MODELOUTPUT\\HEAD\\HEAD.HED\n" +
                "END OPTIONS\n" +
                "\n" +
                "#Stressperiod Save Options\n" +
                "\n" +
                "BEGIN PERIOD 1\n" +
                " SAVE HEAD ALL\n" +
                " SAVE BUDGET ALL\n" +
                "END PERIOD\n";

            FileUtils.WriteFile(modelInputPath + "\\" + modelname + ".OC6", OCstring);
        }

        protected void WriteCHD6References(int nlay, Dictionary<int, int> maxboundDictionary)
        {
            for (int layerNr = 1; layerNr <= (nlay); layerNr++)
            {
                maxboundDictionary.TryGetValue(layerNr, out int value);
                if(value > 0)
                {
                    string CHDstring2 =
                       "# CHD6 File Generated by iMOD [V5_4 X64 Optimized]\n" +
                       "\n" +
                       "#General Options\n" +
                       "\n" +
                       "BEGIN OPTIONS\n" +
                       "SAVE_FLOWS\n" +
                       "END OPTIONS\n" +
                       "\n" +
                       "#General Dimensions\n" +
                       "\n" +
                       "BEGIN DIMENSIONS\n" +
                       "MAXBOUND " + value + "\n" +
                       "END DIMENSIONS\n" +
                       "\n" +
                       "BEGIN PERIOD 1\n" +
                       "OPEN/CLOSE " + relativeModelInputPath + "\\CHD6\\SYS" + layerNr + "\\CHD_T1.ARR 1.0 (FREE) -1\n" +
                       "END PERIOD\n";

                    FileUtils.WriteFile(modelInputPath + "\\" + modelname + "_SYS" + layerNr + ".CHD6", CHDstring2);
                }
            }
        }

        protected void Runmodel(ref int exitcode, SIFToolSettings settings)
        {
            Log.AddInfo();
            Log.AddInfo("Starting MF6 modelrun...");
            string namFilename = Path.Combine(Path.GetFullPath(mf6Path), "MFSIM.NAM");

            // Create path if not yet existing
            if (!Directory.Exists(Path.Combine(modelOutputPath, "HEAD")))
            {
                Directory.CreateDirectory(Path.Combine(modelOutputPath, "HEAD"));
            }
            if (!Directory.Exists(Path.Combine(modelOutputPath, "BUDGET")))
            {
                Directory.CreateDirectory(Path.Combine(modelOutputPath, "BUDGET"));
            }

            string batchFileString = "@ECHO OFF\r\n" + settings.MF6Exe + " " + "MFSIM.NAM\r\n" + "PAUSE\r\n";
            FileUtils.WriteFile(Path.Combine(Path.GetDirectoryName(namFilename), "run MF6.bat"), batchFileString);

            int exitcodeMF6 = CommonUtils.ExecuteCommand(settings.MF6Exe + " " + "MFSIM.NAM", settings.MF6Timeout, out string outputString, Path.GetDirectoryName(namFilename));
            Log.AddInfo(outputString);
            if (exitcodeMF6 != 0)
            {
                if ((exitcodeMF6 == -1) && (outputString.StartsWith("Timeout")))
                {
                    throw new ToolException("Timeout occurred, MODFLOW 6 didn't finish, check LIST-file for convergence issues and/or increase timeout length");
                }
                else
                {
                    throw new ToolException("Error while running MODFLOW 6");
                }
            }
        }

        protected void ReadMF6Results(int nlay, out List<IDFFile> headList, out List<IDFFile> fluxList, SIFToolSettings settings)
        {
            Log.AddInfo("Converting MF6-results to IDF-files ...");
            headList = new List<IDFFile>();
            fluxList = new List<IDFFile>();

            string inifilename = mf6Path + "\\ConvertModelOutput.ini";
            string extension = Path.GetExtension(inifilename);
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(inifilename, false);
                sw.WriteLine("FUNCTION=MF6TOIDF");
                sw.WriteLine("GRB=.\\GWF_1\\MODELINPUT\\" + modelname + ".DIS6.grb");
                sw.WriteLine("HED=.\\GWF_1\\MODELOUTPUT\\HEAD\\HEAD.HED");
                sw.WriteLine("BDG=.\\GWF_1\\MODELOUTPUT\\BUDGET\\BUDGET.CBC");
                sw.WriteLine("ISTEADY=1");
                // ToDo: Write in debug mode for analysis?
                // sw.WriteLine("SAVESHD=-1");
                // sw.WriteLine("SAVECHD=-1");
                if (settings.WriteKVbot)
                {
                    // Write/convert all fluxes
                    sw.WriteLine("SAVEFLX=-1");
                }
                else
                {
                    // Just write/convert flux for layer 1
                    sw.WriteLine("SAVEFLX=1");
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(extension + "-file cannot be written, because it is being used by another process: " + inifilename);
                }
                else
                {
                    throw new Exception("Unexpected error while reading modelresult with imod " + extension + "-file: " + inifilename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading modelresult with imod " + extension + "-file: " + inifilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            string batchFileString = "@ECHO OFF\r\n" + settings.iMODExe + " " + Path.GetFileName(inifilename) + "\r\n" + "PAUSE\r\n";
            FileUtils.WriteFile(Path.Combine(Path.GetDirectoryName(inifilename), "run MF6TOIDF.bat"), batchFileString);

            int exitcodeiMOD = CommonUtils.ExecuteCommand(settings.iMODExe + " " + Path.GetFileName(inifilename), settings.MF6TOIDFTimeout, out string outputString, Path.GetDirectoryName(inifilename));
            Log.AddInfo(outputString);
            bool hasTimeout = false;
            if ((exitcodeiMOD == -1) && (outputString.StartsWith("Timeout")))
            {
                hasTimeout = true;
            }
            else if (!outputString.Contains("iMOD removed tmp-folder:"))
            {
                // Note: iMOD doesn't have a specific exitcode (succes/failure currently both yield exitcode 0), so check for substring that indicates success
                throw new ToolException("Error while reading modelresult with iMOD");
            }

            // Read IDFFile
            Log.AddInfo("Reading head and budget files ...");
            // ToDo: for debug option
            // for (int i = 1; i <= nlay; i++)
            // {
            // IDFFile head = IDFFile.ReadFile(modelOutputPath + "\\HEAD\\HEAD\\HEAD_STEADY-STATE_L" + i + ".IDF");
            // headList.Add(head);
            // }

            Log.AddInfo("Number of layers: " + nlay);
            int nlay_flux = nlay - 1;
            if (!settings.WriteKVbot)
            {
                // When only kv_top is written, just read layer 1
                nlay = 1;
                nlay_flux = 1;
            }
            for (int i = 1; i <= nlay_flux; i++)
            {
                string bdgFLFFilename = modelOutputPath + "\\BUDGET\\BDGFLF\\BDGFLF_STEADY-STATE_L" + i + ".IDF";
                if (!File.Exists(bdgFLFFilename))
                {
                    Log.AddWarning("iMOD-batchfunction MF6TOIDF didn't finish. Missing IDF-file: '" + Path.GetFileName(bdgFLFFilename) + "'\n" 
                        + "Check message window for options ...", 1);
                    DialogResult dialogResult = MessageBox.Show("Fatal timeout occurred, iMOD-batchfunction MF6TOIDF didn't finish and not all budget files have been written.\n"
                        + "Missing IDF-file: '" + Path.GetFileName(bdgFLFFilename) + "'\n"
                        + "Try to run 'MF6-model\\run MF6TOIDF.bat' manually and press OK to search again.", "GeoTOPscale MF6TOIDF-issue", 
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }

                if (!File.Exists(bdgFLFFilename))
                {
                    throw new ToolException("Fatal timeout occurred, iMOD-batchfunction MF6TOIDF didn't finish and not all budget files have been written\n"
                        + "Missing IDF-file: " + Path.GetFileName(bdgFLFFilename) + "\n"
                        + ", check logfile for issues and/or increase timeout length");
                }

                IDFFile flux = IDFFile.ReadFile(bdgFLFFilename);
                fluxList.Add(flux);
            }

            Log.AddInfo();
        }

        /// <summary>
        /// Calculate and write scaled kv-layers based on TOP, BOT and (simple) stack methods.
        /// </summary>
        /// <param name="selLayer"></param>
        /// <param name="fluxList"></param>
        /// <param name="nlay"></param>
        /// <param name="resistanceStackIDFFile"></param>
        /// <param name="totalThicknessIDFFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected IDFFile CreateKVLayers(Layer selLayer, List<IDFFile> fluxList, int nlay, IDFFile totalThicknessIDFFile, IDFFile resistanceStackIDFFile, SIFToolSettings settings)
        {
            IDFFile fluxLFL1 = fluxList[0];
            IDFFile fluxLFLbot = (!settings.WriteKVbot) ? null : fluxList[nlay - 2];
            float cellArea = outputXcellSize * outputYcellSize;
            // Q=dh/dl*k*A=1/thickness*cellsizeX*cellsizeY*A, k=Q / A * thickness

            float noDataValue = selLayer.TOPIDFFile.NoDataValue;
            IDFFile topKVIDFFile = new IDFFile("kvLayertop", outputExtent, outputXcellSize, noDataValue);
            IDFFile botKVIDFFile = (!settings.WriteKVbot) ? null : new IDFFile("kvLayerbot", outputExtent, outputXcellSize, noDataValue);
            IDFFile stackKVIDFFile = (!settings.WriteKVstack) ? null : new IDFFile("kvStack", outputExtent, outputXcellSize, noDataValue);

            // string kvLayertopString = null;
            // string kvLayerbotString = null;
            // string kvStackString = null;
            for (int rowIdx = 0; rowIdx < totalThicknessIDFFile.NRows; rowIdx++)
            {
                float y = totalThicknessIDFFile.GetY(rowIdx);
                for (int colIdx = 0; colIdx < totalThicknessIDFFile.NCols; colIdx++)
                {
                    float x = totalThicknessIDFFile.GetX(colIdx);
                    float localThickness = totalThicknessIDFFile.GetValue(x, y);
                    if (localThickness.Equals(0))
                    {
                        topKVIDFFile.SetValue(x, y, topKVIDFFile.NoDataValue);

                        if (settings.WriteKVbot)
                        {
                            botKVIDFFile.SetValue(x, y, botKVIDFFile.NoDataValue);
                        }
                        if (settings.WriteKVstack)
                        {
                            stackKVIDFFile.SetValue(x, y, stackKVIDFFile.NoDataValue);
                        }
                    }
                    else
                    {
                        float localFluxL1 = fluxLFL1.GetValue(x, y);
                        topKVIDFFile.SetValue(x, y, localThickness * -localFluxL1 / cellArea);

                        if (settings.WriteKVbot)
                        {
                            float localFluxLbot = 0;
                            int i = 0;
                            while (localFluxLbot.Equals(0))
                            {
                                localFluxLbot = fluxLFLbot.GetValue(x, y);
                                fluxLFLbot = fluxList[nlay - 2 - i];
                                i++;
                                if (i == (nlay - 2))
                                {
                                    break;
                                }
                            }
                            botKVIDFFile.SetValue(x, y, localThickness * -localFluxLbot / cellArea);
                        }

                        if (settings.WriteKVstack)
                        {
                            float localStackResistance = resistanceStackIDFFile.GetValue(x, y);
                            stackKVIDFFile.SetValue(x, y, localThickness / localStackResistance);
                        }

                        // Write voor debugging purpose? E.g. can be used for scatterplots. This gives cellvalues as a list
                        // kvLayertopString = kvLayertopString + (localThickness * -localFluxL1 / cellArea) + "; ";
                        // kvLayerbotString = kvLayerbotString + (localThickness * -localFluxLbot / cellArea) + "; ";
                        // kvStackString = kvStackString + (localThickness / localStackResistance) + "; ";
                    }
                }
            }

            Log.AddInfo("Writing resulting kv-file(s) ...");
            topKVIDFFile.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-TOP"));
            if (settings.WriteKVbot)
            {
                botKVIDFFile.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-BOT"));
            }
            if (settings.WriteKVstack)
            {
                stackKVIDFFile.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-Stack"));
            }

            // Write voor debugging purpose? E.g. can be used for scatterplots. This gives cellvalues as a list
            // FileUtils.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-TOP.CSV"), kvLayertopString, false);
            // FileUtils.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-BOT.CSV"), kvLayerbotString);
            // FileUtils.WriteFile(Path.Combine(settings.OutputPath, "kvLayer-Stack.CSV"), kvStackString);

            Log.AddInfo();

            return topKVIDFFile;
        }
    }
}
