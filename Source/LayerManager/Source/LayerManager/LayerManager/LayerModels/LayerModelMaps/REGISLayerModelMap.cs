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
using Sweco.SIF.LayerManager.NameParsers;
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
        private LayerNameParser regisNameParser;

        public REGISLayerModelMap() : base()
        {
            regisNameParser = null;
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
            TOPFilenames = Utils.SelectIDFASCFiles(Utils.GetFiles(settings.TOPPath, settings.TOPFilenamesPatterns, settings.InputPath));
            BOTFilenames = Utils.SelectIDFASCFiles(Utils.GetFiles(settings.BOTPath, settings.BOTFilenamesPatterns, settings.InputPath));
            KHVFilenames = (settings.KHVPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KHVPath, settings.KHVFilenamesPatterns, settings.InputPath));
            KVVFilenames = (settings.KVVPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KVVPath, settings.KVVFilenamesPatterns, settings.InputPath));
            KDWFilenames = (settings.KDWPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.KDWPath, settings.KDWFilenamesPatterns, settings.InputPath));
            VCWFilenames = (settings.VCWPath == null) ? null : Utils.SelectIDFASCFiles(Utils.GetFiles(settings.VCWPath, settings.VCWFilenamesPatterns, settings.InputPath));

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
                // Do not sort
            }

            // Check that filenames in both arrays have equal prefixes
            CheckPrefixes(TOPFilenames, BOTFilenames, "TOP", "BOT");

            // Match kh, kv, kD or c-files to TOP-Files and place at corresponding indices. For leftover files from array 2 warnings are logged.
            MatchByPrefix(TOPFilenames, ref KHVFilenames, "TOP", "KHV", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref KVVFilenames, "TOP", "KVV", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref KDWFilenames, "TOP", "KDW", log, logIndentlevel);
            MatchByPrefix(TOPFilenames, ref VCWFilenames, "TOP", "VCW", log, logIndentlevel);
        }

        /// <summary>
        /// Files in array layerFilenames2 are matched with files in array layerFilenames1 with corresponding REGIS-prefix and placed at corresponding indices.
        /// Mismatches are logged.
        /// </summary>
        /// <param name="layerFilenames1"></param>
        /// <param name="layerFilenames2"></param>
        /// <param name="typeString1"></param>
        /// <param name="typeString2"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        protected void MatchByPrefix(string[] regisFilenames1, ref string[] regisFilenames2, string typeString1, string typeString2, Log log, int logIndentlevel)
        {
            if ((regisFilenames2 == null) || (regisFilenames2.Length == 0))
            {
                // nothing to do
                return;
            }

            // List<string> layerFilenames2List = new List<string>(regisFilenames2);
            Dictionary<string, string> layerPrefix2Dictionary = new Dictionary<string, string>();
            for (int fileIdx2 = 0; fileIdx2 < regisFilenames2.Length; fileIdx2++)
            {
                string filename2 = regisFilenames2[fileIdx2];
                if (filename2 != null)
                {
                    string prefix2 = GetREGISPrefix(filename2).ToLower();
                    layerPrefix2Dictionary.Add(prefix2, filename2);
                }
            }

            // Now loop through layerFilenames1 and search file with corresponding REGIS-prefix in layerFilenames2
            string[] tmpREGISFilename2 = new string[regisFilenames1.Length];
            List<string> mismatchedFilenames1 = new List<string>();
            List<string> mismatchedPrefixes1 = new List<string>();
            for (int fileIdx1 = 0; fileIdx1 < regisFilenames1.Length; fileIdx1++)
            {
                string filename1 = regisFilenames1[fileIdx1];
                string prefix1 = GetREGISPrefix(filename1).ToLower();

                if (layerPrefix2Dictionary.ContainsKey(prefix1))
                {
                    tmpREGISFilename2[fileIdx1] = layerPrefix2Dictionary[prefix1];
                    layerPrefix2Dictionary.Remove(prefix1);
                }
                else
                {
                    mismatchedFilenames1.Add(filename1);
                    mismatchedPrefixes1.Add(prefix1);
                }
            }

            regisFilenames2 = tmpREGISFilename2;
        }

        /// <summary>
        /// Check that filenames in both arrays have equal prefixes
        /// </summary>
        /// <param name="regisFilenames1"></param>
        /// <param name="regisFilenames2"></param>
        /// <param name="typeString1"></param>
        /// <param name="typeString2"></param>
        protected void CheckPrefixes(string[] regisFilenames1, string[] regisFilenames2, string typeString1, string typeString2)
        {
            // Check that TOP and BOT-filenames have equal length
            if (regisFilenames1.Length != regisFilenames2.Length)
            {
                throw new ToolException("Nummber of REGIS " + typeString1 + "-files (" + regisFilenames1.Length + ") is not equal to number of " + typeString2 + "-files (" + regisFilenames2.Length + "). Check input path.");
            }

            for (int fileIdx = 0; fileIdx < regisFilenames1.Length; fileIdx++)
            {
                string filename1 = regisFilenames1[fileIdx];
                string filename2 = regisFilenames2[fileIdx];
                string prefix1 = GetREGISPrefix(filename1);
                string prefix2 = GetREGISPrefix(filename2);
                if (prefix1 == null)
                {
                    if (prefix2 != null)
                    {
                        throw new ToolException("REGIS " + typeString1 + "-file prefix NULL does not match " + typeString1 + "-file prefix (" + prefix2 + ") for " + typeString1 + "-file at index " + (fileIdx + 1).ToString() + ": " + Path.GetFileName(filename1) + ". Check for missing REGIS-files.");
                    }
                }
                else if (!prefix1.ToLower().Equals(prefix2.ToLower()))
                {
                    throw new ToolException("REGIS " + typeString1 + "-file prefix (" + prefix1 + ") does not match " + typeString1 + "-file prefix (" + prefix2 + ") for " + typeString1 + "-file at index " + (fileIdx + 1).ToString() + ": " + Path.GetFileName(filename1));
                }
            }
        }

        protected void SortByPrefix(string[] filenames, Log log, int logIndentlevel)
        {
            if (layerOrderPrefixIdxDictionary != null)
            {
                if (filenames != null)
                {
                    string[] tmpFilenames = new string[layerOrderPrefixIdxDictionary.Keys.Count];
                    List<string> mismatchedFilenames = new List<string>();
                    List<string> prefixes = new List<string>();
                    for (int fileIdx = 0; fileIdx < filenames.Length; fileIdx++)
                    {
                        string filename = filenames[fileIdx];
                        string prefix = GetREGISPrefix(filename).ToLower();
                        // Check that prefix is unique 
                        if (prefixes.Contains(prefix.ToLower()))
                        {
                            throw new ToolException("Prefix " + prefix + " is not unique, check file: " + Path.GetFileName(filename));
                        }
                        else
                        {
                            prefixes.Add(prefix);
                        }

                        prefixes.Add(prefix);

                        int layerIdx = layerOrderPrefixIdxDictionary.ContainsKey(prefix) ? layerOrderPrefixIdxDictionary[prefix] : -1;
                        if (layerIdx > -1)
                        {
                            tmpFilenames[layerIdx] = filename;
                        }
                        else
                        {
                            log.AddWarning("Layer prefix '" + prefix + "' not found in layer order file, layer file is ignored: " + Path.GetFileName(filename), logIndentlevel);
                            mismatchedFilenames.Add(filename);
                        }
                    }

                    // Add sorted filenames to original array
                    int filenameIdx = 0;
                    for (int fileIdx = 0; fileIdx < tmpFilenames.Length; fileIdx++)
                    {
                        string filename = tmpFilenames[fileIdx];
                        if (filename != null)
                        {
                            filenames[filenameIdx] = filename;
                            filenameIdx++;
                        }
                    }

                    // Add null values for mismatched filenames at end
                    foreach (string filename in mismatchedFilenames)
                    {
                        filenames[filenameIdx] = null; // 
                        filenameIdx++;
                    }
                }
            }
            else
            {
                throw new Exception("For sorting a layer order should be defined, but layerOrderPrefixIdxDictionary == null");
            }
        }

        /// <summary>
        /// Check for aquitard in REGIS filename format
        /// </summary>
        /// <param name="fileIdx"></param>
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

                // Also check iMODLayerManager format, e.g. for names like '1.1_WVP00_HLC-B-CK.IDF'
                if (regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower()) || regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower()))
                {
                    isAquitard = regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower());
                }
                else
                {
                    try
                    {
                        // Check REGIS format
                        if (regisNameParser == null)
                        {
                            regisNameParser = LayerNameParserFactory.RetrieveNameParser(regisFilename);
                        }

                        regisNameParser.ParseLayerFilename(regisFilename, out string lithologyCode, out int index, out bool isAquifer, out isAquitard, out string layerName, out string substring);
                    }
                    catch (Exception)
                    {
                        // File doesn't have REGIS format, check c- or kD-columns: when nothing is defined aquifer is the default
                        isAquitard = (VCWFilenames != null) && ((VCWFilenames[fileIdx] != null) && !VCWFilenames[fileIdx].Equals(string.Empty));
                    }
                }
            }

            return isAquitard;
        }

        /// <summary>
        /// Check for aquifer in REGIS filename format
        /// </summary>
        /// <param name="fileIdx"></param>
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

                // Also check iMODLayerManager format, e.g. for names like '1.1_WVP00_HLC-B-CK.IDF'
                if (regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquitardAbbr.ToLower()) || regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower()))
                {
                    isAquifer = regisFilename.ToLower().Contains(Properties.Settings.Default.REGISAquiferAbbr.ToLower());
                }
                else
                {
                    try
                    {
                        // Check REGIS format
                        if (regisNameParser == null)
                        {
                            regisNameParser = LayerNameParserFactory.RetrieveNameParser(regisFilename);
                        }

                        regisNameParser.ParseLayerFilename(regisFilename, out string lithologyCode, out int index, out isAquifer, out bool isAquitard, out string layerName, out string substring);
                    }
                    catch (Exception)
                    {
                        // File doesn't have REGIS format, check c- or kD-columns: when nothing is defined aquifer is the default
                        throw new ToolException("IsAquifer: unknown file format, layer type cannot be determined: " + Path.GetFileName(regisFilename));
                        // isAquifer = !((VCWFilenames != null) && (VCWFilenames[fileIdx] != null) && !VCWFilenames[fileIdx].Equals(string.Empty));
                    }
                }
            }
            return isAquifer;
        }

        /// <summary>
        /// Resets REGIS NameParser object so that it is identified again for the next regisFilename
        /// </summary>
        public void ResetREGISNameParser()
        {
            regisNameParser = null;
        }

        public string GetREGISPrefix(string regisFilename)
        {
            if (regisNameParser == null)
            {
                regisNameParser = LayerNameParserFactory.RetrieveNameParser(regisFilename);
            }

            return regisNameParser.GetLayerPrefix(regisFilename);
        }

        public string GetTopFilePatternString(string regisFilename)
        {
            if (regisNameParser == null)
            {
                regisNameParser = LayerNameParserFactory.RetrieveNameParser(regisFilename);
            }

            return regisNameParser.GetTopFilePatternString(regisFilename);
        }
    }
}
