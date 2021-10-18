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
    /// Class for managing iMOD-LayerModels
    /// </summary>
    public class IMODLayerModelManager : LayerModelManager
    {
        public IMODLayerModelManager(SIFToolSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Read filenames to layermodel files as defined by settings 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public override LayerModelMap ReadLayerModelMap(IDFLog log, int logIndentlevel)
        {
            return IMODLayerModelMap.ReadDirectories(Settings, log, logIndentlevel);
        }

        /// <summary>
        /// Reads and checks specified LayerModelMap for inconsistencies in layermodel
        /// </summary>
        /// <param name="imodLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="modelname">a modelname for the checked files, used to create a subdirectory for checkresults, or leave empty</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public override bool Check(LayerModelMap imodLayerModelMap, SIFToolSettings settings, string modelname, IDFLog log, int logIndentLevel = 0)
        {
            LayerModel imodInputLayerModel = LayerModel.Read(imodLayerModelMap, settings, false, log, logIndentLevel + 1);
            imodInputLayerModel.EnsureKDCLayers(settings, true, log, 1);
            log.AddInfo();

            // Set base IDF for logwarnings and errors 
            log.SetBaseIDFFile(imodInputLayerModel.TOPIDFFiles[imodInputLayerModel.ModellayerCount]); // topIDFFiles.Length - 1]);

            return Check(imodInputLayerModel, settings, modelname, log, logIndentLevel);
        }

        /// <summary>
        /// Checks specified LayerModel for inconsistencies in layermodel
        /// </summary>
        /// <param name="imodLayerModel"></param>
        /// <param name="settings"></param>
        /// <param name="modelname">a modelname for the checked files, used to create a subdirectory for checkresults, or leave empty</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public bool Check(LayerModel imodInputLayerModel, SIFToolSettings settings, string modelname, IDFLog log, int logIndentLevel = 0)
        {
            // Set base IDF for logwarnings and errors 
            log.SetBaseIDFFile(imodInputLayerModel.TOPIDFFiles[imodInputLayerModel.ModellayerCount]); // topIDFFiles.Length - 1]);

            // Check inputmodel and write IDFLog-files if issues were found
            log.AddInfo("Checking iMOD-model for inconsistencies ... ", logIndentLevel);
            IDFLog modelcheckLog = CreateIDFLog(settings, modelname, log.BaseIDFFile, log.Listeners);
            bool isValidModel = imodInputLayerModel.Check(settings, modelcheckLog);
            log.AddInfo();

            if (isValidModel)
            {
                if (modelcheckLog.WarningCount > 0)
                {
                    log.AddWarning("No fatal model inconsistencies found in iMOD-model, but " + modelcheckLog.WarningCount + " warnings found.", logIndentLevel);
                    log.AddInfo("Check log and warning-IDF's in " + settings.OutputPath, logIndentLevel);
                }
                else
                {
                    log.AddInfo("No model inconsistencies found in iMOD-model.", logIndentLevel);
                }
            }
            else
            {
                log.AddError("Model inconsistencies found in iMOD-model: " + modelcheckLog.WarningCount + " warnings and " + modelcheckLog.ErrorCount + " errors.", logIndentLevel);
                log.AddInfo("Check log and error/warning-IDF's in " + settings.OutputPath, logIndentLevel);
            }

            log.AddInfo();

            return isValidModel;
        }

        /// <summary>
        /// Calculate and write IDF-files with kD- and c-values
        /// </summary>
        /// <param name="imodLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public override int CalculateKDC(LayerModelMap imodLayerModelMap, SIFToolSettings settings, IDFLog log, int logIndentLevel = 0)
        {
            LayerModel imodLayerModel = LayerModel.Read(imodLayerModelMap, settings, true, log, logIndentLevel);

            log.AddInfo("Calculating and writing kD/c-values ...", logIndentLevel);
            imodLayerModel.RetrieveKDCValues(settings, false, true, true, true, log, logIndentLevel + 1);
            log.AddInfo();

            return 0;
        }

        /// <summary>
        /// Retrieve thickness IDF-file, if not-null it is simply returned, otherwise is it calculated from TOP- and BOT-files
        /// </summary>
        /// <param name="imodLayerModelMap"></param>
        /// <param name="fileIdx"></param>
        /// <param name="thicknessIDFFile"></param>
        /// <param name="topIDFFile"></param>
        /// <param name="botIDFFile"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        private IDFFile RetrieveThicknessIDFFile(LayerModelMap imodLayerModelMap, int fileIdx, IDFFile thicknessIDFFile, IDFFile topIDFFile, IDFFile botIDFFile, IDFLog log, int logIndentLevel, Extent extent)
        {
            if (thicknessIDFFile == null)
            {
                if (topIDFFile == null)
                {
                    topIDFFile = IDFFile.ReadFile(imodLayerModelMap.TOPFilenames[fileIdx], false, log, logIndentLevel, extent);
                }
                if (botIDFFile == null)
                {
                    botIDFFile = IDFFile.ReadFile(imodLayerModelMap.BOTFilenames[fileIdx], false, log, logIndentLevel, extent);
                }
                thicknessIDFFile = topIDFFile - botIDFFile;
            }

            return thicknessIDFFile;
        }
    }
}
