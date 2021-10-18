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
using Sweco.SIF.iMOD.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// Class for storing references to REGIS-LayerModel contents (i.e. filenames)
    /// </summary>
    public class REGISLayerModelMap : LayerModelMap
    {
        protected const string LayerStringSeperator = "-";

        public REGISLayerModelMap() : base()
        {
        }

        public static string GetREGISPrefix(string regisLayerFilename)
        {
            if (regisLayerFilename == null)
            {
                return null;
            }

            int idx = Path.GetFileNameWithoutExtension(regisLayerFilename).IndexOf("-");
            if (idx < 0)
            {
                throw new ToolException("Unexpected format for REGIS-filename: " + Path.GetFileName(regisLayerFilename));
            }

            string prefix = Path.GetFileNameWithoutExtension(regisLayerFilename);
            int prefixIdx = prefix.IndexOf(LayerStringSeperator);
            prefix = prefix.Substring(0, prefixIdx);
            return prefix;
        }

        /// <summary>
        /// Read REGIS-filenames for LayerModel from specified settings into new LayerModelMap object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        /// <returns></returns>
        public static LayerModelMap ReadDirectories(SIFToolSettings settings, Log log, int logIndentlevel)
        {
            REGISLayerModelMap layerModelMap = new REGISLayerModelMap();

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
            log.AddInfo("Reading model filenames ... ", logIndentlevel);
            TOPFilenames = Utils.SelectIDFASCFiles(Directory.GetFiles(settings.TOPPath, Utils.CreateFilePatternString(settings.TOPFilenamesPattern)));
            BOTFilenames = Utils.SelectIDFASCFiles(Directory.GetFiles(settings.BOTPath, Utils.CreateFilePatternString(settings.BOTFilenamesPattern)));
            KHVFilenames = (settings.KHVPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KHVPath, Utils.CreateFilePatternString(settings.KHVFilenamesPattern)));
            KVVFilenames = (settings.KVVPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KVVPath, Utils.CreateFilePatternString(settings.KVVFilenamesPattern)));
            KDWFilenames = (settings.KDWPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.KDWPath, Utils.CreateFilePatternString(settings.KDWFilenamesPattern)));
            VCWFilenames = (settings.VCWPath == null) ? null : Utils.SelectIDFASCFiles(Directory.GetFiles(settings.VCWPath, Utils.CreateFilePatternString(settings.VCWFilenamesPattern)));

            CheckFileCount(settings, log, logIndentlevel + 1);
            SortFiles(settings, log, logIndentlevel + 1);

            // Define layerIDs, etc.
            List<string> layerIDs = new List<string>();
            List<int> layerNumbers = new List<int>();
            for (int layerIdx = 0; layerIdx < BOTFilenames.Length; layerIdx++)
            {
                // Add layernumber as a string ID and as an integer number
                int layerNumber = layerIdx + 1;
                layerIDs.Add(layerNumber.ToString());
                layerNumbers.Add(layerNumber);
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
            if (settings.LayerOrderFilename != null)
            {
                ReadOrderFile(settings.LayerOrderFilename, log);

                SortByPrefix(TOPFilenames, log, logIndentlevel);
                SortByPrefix(BOTFilenames, log, logIndentlevel);
                SortByPrefix(KHVFilenames, log, logIndentlevel);
                SortByPrefix(KVVFilenames, log, logIndentlevel);
                SortByPrefix(KDWFilenames, log, logIndentlevel);
                SortByPrefix(VCWFilenames, log, logIndentlevel);
                SortByPrefix(KVAFilenames, log, logIndentlevel);
                IsOrdered = true;
            }
            else
            {
                TOPFilenames = IMODUtils.SortiMODLayerFilenames(TOPFilenames);
                BOTFilenames = IMODUtils.SortiMODLayerFilenames(BOTFilenames);
                KHVFilenames = IMODUtils.SortiMODLayerFilenames(KHVFilenames);
                KVVFilenames = IMODUtils.SortiMODLayerFilenames(KVVFilenames);
                KDWFilenames = IMODUtils.SortiMODLayerFilenames(KDWFilenames);
                VCWFilenames = IMODUtils.SortiMODLayerFilenames(VCWFilenames);
                KVAFilenames = IMODUtils.SortiMODLayerFilenames(KVAFilenames);
            }

            // Check that filenames in both arrays have equal prefixes
            CheckPrefixes(TOPFilenames, BOTFilenames, "TOP", "BOT");

            // Match kh, kv, kD or c-files to TOP-Files and place at corresponding indices. For leftover files from array 2 warnings are logged.
            MatchByPrefix(TOPFilenames, ref KHVFilenames, "TOP", "KHV", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref KVVFilenames, "TOP", "KVV", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref KDWFilenames, "TOP", "KDW", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref VCWFilenames, "TOP", "VCW", log, logIndentlevel);
        }
    }
}
