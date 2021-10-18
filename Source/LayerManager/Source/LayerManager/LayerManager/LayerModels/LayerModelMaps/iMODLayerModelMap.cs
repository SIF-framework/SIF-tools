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
            TOPFilenames = Utils.SelectIDFASCFiles(Directory.GetFiles(settings.TOPPath, Utils.CreateFilePatternString(settings.TOPFilenamesPattern)));
            BOTFilenames = Utils.SelectIDFASCFiles(Directory.GetFiles(settings.BOTPath, Utils.CreateFilePatternString(settings.BOTFilenamesPattern)));
            KHVFilenames = (settings.KHVPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KHVPath, Utils.CreateFilePatternString(settings.KHVFilenamesPattern)));
            KVVFilenames = (settings.KVVPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KVVPath, Utils.CreateFilePatternString(settings.KVVFilenamesPattern)));
            KVAFilenames = (settings.KVAPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KVAPath, Utils.CreateFilePatternString(settings.KVAFilenamesPattern)));
            KDWFilenames = (settings.KDWPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KDWPath, Utils.CreateFilePatternString(settings.KDWFilenamesPattern)));
            VCWFilenames = (settings.VCWPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.VCWPath, Utils.CreateFilePatternString(settings.VCWFilenamesPattern)));

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

    }
}
