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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// Class for storing references to iMOD-LayerModel contents (i.e. filenames)
    /// </summary>
    public class IMODLayerModelMap : LayerModelMap
    {
        public IMODLayerModelMap() : base()
        {
        }

        /// <summary>
        /// Read iMOD-filenames for LayerModel from specified settings into new LayerModelMap object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public static LayerModelMap ReadDirectories(SIFToolSettings settings, Log log, int logIndentlevel)
        {
            IMODLayerModelMap layerModelMap = new IMODLayerModelMap();

            layerModelMap.DoReadDirectories(settings, log, logIndentlevel);

            return layerModelMap;
        }

        /// <summary>
        /// Read filenames from specified paths in this LayerModelMap object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        protected override void DoReadDirectories(SIFToolSettings settings, Log log, int logIndentlevel)
        {
            // Read filenames from given folders
            log.AddInfo("Reading iMOD-model filenames ... ", logIndentlevel);
            TOPFilenames = Utils.SelectIDFASCFiles(Utils.GetFiles(settings.TOPParString, settings.TOPFilenamesPatterns, settings.InputPath));
            BOTFilenames = Utils.SelectIDFASCFiles(Utils.GetFiles(settings.BOTPath, settings.BOTFilenamesPatterns, settings.InputPath));
            KHVFilenames = (settings.KHVPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KHVPath, settings.KHVFilenamesPatterns, settings.InputPath));
            KVVFilenames = (settings.KVVPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KVVPath, settings.KVVFilenamesPatterns, settings.InputPath));
            KVAFilenames = (settings.KVAPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KVAPath, settings.KVAFilenamesPatterns, settings.InputPath));
            KDWFilenames = (settings.KDWPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KDWPath, settings.KDWFilenamesPatterns, settings.InputPath));
            VCWFilenames = (settings.VCWPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.VCWPath, settings.VCWFilenamesPatterns, settings.InputPath));

            CheckFileCount(settings, log, logIndentlevel + 1);
            SortFiles(settings, log, logIndentlevel + 1);

            // Check for and correct if topfile of highest layer is missing (this can happen if an aquitard is on top)
            int toplayerNumber = GetLayerNumberFromFilename(TOPFilenames[0]);
            int botlayerNumber = GetLayerNumberFromFilename(BOTFilenames[0]);
            int clayerNumber = (VCWFilenames == null) ? -1 : GetLayerNumberFromFilename(VCWFilenames[0]);
            int kdlayerNumber = (VCWFilenames == null) ? -1 : GetLayerNumberFromFilename(KDWFilenames[0]);
            if (toplayerNumber == botlayerNumber - 1)
            {
                // Insert empty toplayer entry
                List<string> botFilenamesCorr = new List<string>();
                botFilenamesCorr.Add(null);
                botFilenamesCorr.AddRange(BOTFilenames);
                BOTFilenames = botFilenamesCorr.ToArray();

                if (clayerNumber != botlayerNumber)
                {
                    throw new ToolException("C-file layernumber (" + clayerNumber + ") doesn't match BOT-file layernumber (" + botlayerNumber + ") for first entry in input filelist");
                }

                if (kdlayerNumber == botlayerNumber - 1)
                {
                    // Insert empty kd entry
                    List<string> kdFilenamesCorr = new List<string>();
                    kdFilenamesCorr.Add(null);
                    kdFilenamesCorr.AddRange(KDWFilenames);
                    KDWFilenames = kdFilenamesCorr.ToArray();
                }
                else
                {
                    throw new ToolException("Mismatch between TOP-layernumber (" + toplayerNumber + ") and kD-layernumber (" + kdlayerNumber + ") for first entry in input filelist");
                }
            }

            if ((TOPFilenames.Length < BOTFilenames.Length) || (TOPFilenames.Length > BOTFilenames.Length + 1))
            {
                throw new ToolException("Number of TOP-files (" + TOPFilenames.Length + ") should be equal to or one more than then number of BOT-files (" + BOTFilenames.Length + ")");
            }

            // Add layernumbers and check for missing and/or mismatching TOP/BOT-files
            List<string> layerIDs = new List<string>();
            List<int> layerNumbers = new List<int>();
            botlayerNumber = GetLayerNumberFromFilename(BOTFilenames[0]);
            layerIDs.Add(Properties.Settings.Default.OutputLayerPostfix + botlayerNumber.ToString());
            layerNumbers.Add(botlayerNumber);
            for (int idx = 1; idx < BOTFilenames.Length; idx++)
            {
                toplayerNumber = GetLayerNumberFromFilename(TOPFilenames[idx]);
                botlayerNumber = GetLayerNumberFromFilename(BOTFilenames[idx]);

                // Add layernumber as a string ID and as an integer number
                layerIDs.Add(Properties.Settings.Default.OutputLayerPostfix + toplayerNumber.ToString());
                layerNumbers.Add(toplayerNumber);

                if (botlayerNumber != toplayerNumber)
                {
                    throw new ToolException("BOT-file layernumber (" + clayerNumber + ") doesn't match TOP-file layernumber (" + botlayerNumber + ") for entry " + (idx + 1) + " in input filelist: " + Path.GetFileName(BOTFilenames[idx]));
                }
            }
            if (TOPFilenames.Length > BOTFilenames.Length)
            {
                toplayerNumber = GetLayerNumberFromFilename(TOPFilenames[TOPFilenames.Length - 1]);
                layerIDs.Add(Properties.Settings.Default.OutputLayerPostfix + toplayerNumber.ToString());
                layerNumbers.Add(toplayerNumber);
            }
            LayerIDs = layerIDs.ToArray();
            LayerNumbers = layerNumbers.ToArray();
        }

        /// <summary>
        /// Sort currently read filenames in alphanumeric order and check match between corresponding layerfiles
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        public override void SortFiles(SIFToolSettings settings, Log log, int logIndentlevel)
        {
            CommonUtils.SortAlphanumericStrings(TOPFilenames);
            CommonUtils.SortAlphanumericStrings(BOTFilenames);
            if (KHVFilenames != null)
            {
                CommonUtils.SortAlphanumericStrings(KHVFilenames);
            }
            if (KVVFilenames != null)
            {
                CommonUtils.SortAlphanumericStrings(KVVFilenames);
            }
            if (KDWFilenames != null)
            {
                CommonUtils.SortAlphanumericStrings(KDWFilenames);
            }
            if (VCWFilenames != null)
            {
                CommonUtils.SortAlphanumericStrings(VCWFilenames);
            }
            if (KVAFilenames != null)
            {
                CommonUtils.SortAlphanumericStrings(KVAFilenames);
            }
        }

        /// <summary>
        /// Check for aquitard in WVP/SDL format for iMODLayerManager as described in the helpfile
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <returns></returns>
        public override bool IsAquitard(int fileIdx)
        {
            bool isAquitard = false;

            // Check if layer is explicitly defined as an aquifer/aquitard
            if (AquitardDefinitions != null)
            {
                isAquitard = AquitardDefinitions[fileIdx];
            }
            else
            {
                string regisFilename = BOTFilenames[fileIdx];

                // Check iMODLayerManager format
                if (regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower()) || regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower()))
                {
                    isAquitard = regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower());
                }
                else
                {
                    // File doesn't have iMOD format, check c- or kD-columns: when nothing is defined aquifer is the default
                    isAquitard = (VCWFilenames != null) && ((VCWFilenames[fileIdx] != null) && !VCWFilenames[fileIdx].Equals(string.Empty));
                }
            }

            return isAquitard;
        }

        /// <summary>
        /// Check for aquifer in WVP/SDL format for iMODLayerManager as described in the helpfile
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <returns></returns>
        public override bool IsAquifer(int fileIdx)
        {
            bool isAquifer = false;

            // Check if layer is explicitly defined as an aquifer/aquitard
            if (AquitardDefinitions != null)
            {
                isAquifer = !AquitardDefinitions[fileIdx];
            }
            else
            {
                string regisFilename = BOTFilenames[fileIdx];

                // Check iMODLayerManager format
                if (regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower()) || regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower()))
                {
                    isAquifer = regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower());
                }
                else
                {
                    // File doesn't have iMOD format, check c- or kD-columns: when nothing is defined aquifer is the default
                    throw new ToolException("IsAquifer: unknown file format, layer type cannot be determined: " + Path.GetFileName(regisFilename));
                    // isAquifer = !((VCWFilenames != null) && (VCWFilenames[fileIdx] != null) && !VCWFilenames[fileIdx].Equals(string.Empty));
                }
            }
            return isAquifer;
        }
    }
}
