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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModelManagers
{
    /// <summary>
    /// Abstract base class for managing LayerModels
    /// </summary>
    public abstract class LayerModelManager
    {
        protected static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        protected SIFToolSettings Settings;

        internal LayerModelManager(SIFToolSettings settings)
        {
            this.Settings = settings;
        }

        /// <summary>
        /// Read filenames to LayerModelMap object as defined by settings 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public abstract LayerModelMap ReadLayerModelMap(IDFLog log, int logIndentlevel);

        /// <summary>
        /// Read and create LayerModel object as defined by settings 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public virtual LayerModel ReadLayerModel(SIFToolSettings settings, IDFLog log, int logIndentlevel)
        {
            LayerModelMap inputLayerModelMap = ReadLayerModelMap(log, logIndentlevel);

            return ReadLayerModel(inputLayerModelMap, settings, log, logIndentlevel);
        }

        /// <summary>
        /// Read and create LayerModel object as defined by layerModelMap settings 
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public virtual LayerModel ReadLayerModel(LayerModelMap layerModelMap, SIFToolSettings settings, IDFLog log, int logIndentlevel)
        {
            return LayerModel.Read(layerModelMap, settings, false, log, logIndentlevel);
        }

        /// <summary>
        /// Reads and checks specified modelmap for inconsistencies in layermodel
        /// </summary>
        /// <param name="layerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="modelname">a modelname for the checked files, used to create a subdirectory for checkresults, or leave empty</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public abstract bool Check(LayerModelMap layerModelMap, SIFToolSettings settings, string modelname, IDFLog log, int logIndentLevel = 0);

        /// <summary>
        /// Calculates kD-values, c-values and thickness for layers in specified LayerModelMap
        /// </summary>
        /// <param name="inputLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public abstract int CalculateKDC(LayerModelMap inputLayerModelMap, SIFToolSettings settings, IDFLog log, int logIndentLevel = 0);

        /// <summary>
        /// Read an IDFFile from specified idfFilename. If idfFilename is a value, a ConstantIDFFile is returned.
        /// </summary>
        /// <param name="idfFilename"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        protected static IDFFile ReadIDFFileOrConstant(string idfFilename, bool useLazyLoading, Log log, int logIndentLevel, Extent extent)
        {
            IDFFile idfFile = null;

            if (float.TryParse(idfFilename, NumberStyles.Float, EnglishCultureInfo, out float fltValue))
            {
                idfFile = new ConstantIDFFile(fltValue);
            }
            else
            {
                idfFile = IDFFile.ReadFile(idfFilename, useLazyLoading, log, logIndentLevel, extent);
            }
            return idfFile;
        }

        /// <summary>
        /// Create a new and empty IDFLog object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="modelname"></param>
        /// <param name="baseIDFFile"></param>
        /// <param name="listeners"></param>
        /// <returns></returns>
        protected IDFLog CreateIDFLog(SIFToolSettings settings, string modelname, IDFFile baseIDFFile, List<AddMessageDelegate> listeners)
        {
            string outputPath = settings.OutputPath;
            string modelcheckPath = Path.Combine(outputPath, Properties.Settings.Default.ModelchecksSubdirname);
            if ((modelname != null) && !modelname.Equals(string.Empty))
            {
                modelcheckPath = Path.Combine(modelcheckPath, modelname);
            }
            if (!Directory.Exists(modelcheckPath))
            {
                Directory.CreateDirectory(modelcheckPath);
            }

            IDFLog modelCheckLog = new IDFLog(Path.Combine(modelcheckPath, "modelcheck.log"), baseIDFFile, listeners);
            modelCheckLog.AddInfo("Height tolerance: " + Layer.LevelTolerance, 1);

            return modelCheckLog;
        }
    }
}
