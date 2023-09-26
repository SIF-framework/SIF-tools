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
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// Abstract class for storing references to LayerModel contents (i.e. filenames)
    /// </summary>
    public abstract class LayerModelMap
    {
        protected static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        public string[] TOPFilenames;
        public string[] BOTFilenames;
        public string[] KHVFilenames;
        public string[] KVVFilenames;
        public string[] KVAFilenames;
        public string[] VCWFilenames;
        public string[] KDWFilenames;
        public bool[] AquitardDefinitions { get; set; }

        /// <summary>
        /// List with layer ID-value per modellayer
        /// </summary>
        public string[] LayerIDs { get; set; }

        /// <summary>
        /// List with layernumber per modellayer
        /// </summary>
        public int[] LayerNumbers { get; set; }

        /// <summary>
        /// Specifies if input files are sorted with an orderfile as specified by settings.LayerOrderFilename
        /// </summary>
        public bool IsOrdered { get; protected set; }

        protected Dictionary<string, int> layerOrderPrefixIdxDictionary;
        protected Dictionary<int, string> layerOrderIdxPrefixDictionary;

        internal LayerModelMap()
        {
            TOPFilenames = null;
            BOTFilenames = null;
            VCWFilenames = null;
            KDWFilenames = null;
            KHVFilenames = null;
            KVVFilenames = null;
            KVAFilenames = null;
            LayerIDs = null;
            LayerNumbers = null;

            layerOrderPrefixIdxDictionary = null;
            layerOrderIdxPrefixDictionary = null;
            IsOrdered = false;
        }

        /// <summary>
        /// Returns layerID of layerfile at specified index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public string GetLayerID(int idx)
        {
            if (LayerIDs != null)
            {
                string layerID = LayerIDs[idx];
                if (!LayerIDs.Equals(int.MinValue))
                {
                    return layerID;
                }
            }

            string topFilename = TOPFilenames[idx];
            return GetLayerNumberFromFilename(topFilename).ToString();
        }

        public string GetLowestModellayerID()
        {
            return GetLayerID(TOPFilenames.Length - 1);
        }

        public string[] GetGroupLayerIDs()
        {
            if (LayerIDs == null)
            {
                return null;
            }

            List<string> groupLayerIDs = new List<string>();
            groupLayerIDs.Add(LayerIDs[0]);
            for (int i = 1; i < LayerIDs.Length; i++)
            {
                if (!groupLayerIDs[groupLayerIDs.Count - 1].Equals(LayerIDs[i]))
                {
                    groupLayerIDs.Add(LayerIDs[i]);
                }
            }
            return groupLayerIDs.ToArray();
        }

        public static int GetLayerNumberFromFilename(string imodModellayerFilename)
        {
            imodModellayerFilename = Path.GetFileNameWithoutExtension(imodModellayerFilename);
            int idx = -1;
            string numericString = null;
            if (imodModellayerFilename.Contains(Properties.Settings.Default.REGISAquiferAbbr))
            {
                idx = imodModellayerFilename.IndexOf(Properties.Settings.Default.REGISAquiferAbbr) + Properties.Settings.Default.REGISAquiferAbbr.Length;
                if (idx != -1)
                {
                    numericString = imodModellayerFilename.Substring(idx, 2);
                }
                else
                {
                    throw new ToolException("Invalid layermodel filename: " + Path.GetFileName(imodModellayerFilename) + ", expected to find '" + Properties.Settings.Default.REGISAquiferAbbr + "'");
                }
            }
            else if (imodModellayerFilename.Contains(Properties.Settings.Default.REGISAquitardAbbr))
            {
                idx = imodModellayerFilename.IndexOf(Properties.Settings.Default.REGISAquitardAbbr) + Properties.Settings.Default.REGISAquitardAbbr.Length;
                if (idx != -1)
                {
                    if (imodModellayerFilename.Length > idx + 1)
                    {
                        numericString = imodModellayerFilename.Substring(idx, 2);
                    }
                    if (imodModellayerFilename.Length > idx)
                    {
                        numericString = imodModellayerFilename.Substring(idx, 1);
                    }
                }
                else
                {
                    throw new ToolException("Invalid layermodel filename: " + Path.GetFileName(imodModellayerFilename) + ", expected to find '" + Properties.Settings.Default.REGISAquitardAbbr + "'");
                }
            }
            else
            {
                idx = Utils.GetNumericValue(imodModellayerFilename);
                if (idx > 0)
                {
                    numericString = imodModellayerFilename.Substring(idx, imodModellayerFilename.Length - idx);
                }
            }

            if (numericString != null)
            {
                int layerNumber = -1;
                if (int.TryParse(numericString, out layerNumber))
                {
                    return layerNumber;
                }
            }

            throw new ToolException("Layernumber not found for filename: " + imodModellayerFilename);
        }

        /// <summary>
        /// Check that file counts match for different layer files. If not a ToolException is thrown for TOP/BOT-files and a warning is written in log for missing other files.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        public void CheckFileCount(SIFToolSettings settings, Log log, int logIndentlevel)
        {
            // Check number of filenames 
            if ((TOPFilenames == null) || (TOPFilenames.Length == 0))
            {
                throw new ToolException("No TOP-files (\"" + settings.TOPFilenamesPattern + "\") found in " + settings.TOPPath);
            }
            if ((BOTFilenames == null) || (BOTFilenames.Length == 0))
            {
                throw new ToolException("No BOT-files (\"" + settings.BOTFilenamesPattern + "\") found in " + settings.BOTPath);
            }
            if (TOPFilenames.Length != BOTFilenames.Length)
            {
                throw new ToolException("Number of TOP-files (" + TOPFilenames.Length + ") is not equal to number of BOT-files (" + BOTFilenames.Length + ")");
            }
            if ((KHVFilenames != null) && ((KHVFilenames.Length == 0) && (settings.KHVParString != null)))
            {
                log.AddWarning("No kh-files (\"" + settings.KHVFilenamesPattern + "\") found in " + settings.KHVPath, logIndentlevel);
            }
            if ((KVVFilenames != null) && ((KVVFilenames.Length == 0) && (settings.KVVParString != null)))
            {
                log.AddWarning("No kv-files (\"" + settings.KVVFilenamesPattern + "\") found in " + settings.KHVPath, logIndentlevel);
            }
            if ((KDWFilenames != null) && ((KDWFilenames.Length == 0) && (settings.KDWParString != null)))
            {
                log.AddInfo("No kD-files (\"" + settings.KDWFilenamesPattern + "\") found in " + settings.KDWPath, logIndentlevel);
            }
            if ((VCWFilenames != null) && ((VCWFilenames.Length == 0) && (settings.VCWParString != null)))
            {
                log.AddInfo("No C-files (\"" + settings.VCWFilenamesPattern + "\") found in " + settings.VCWPath, logIndentlevel);
            }
        }

        /// <summary>
        /// Check if the number of TOP-files is one more than the number of BOT-files because of one extra TOP-file at the bottom of the model, thereby representing the thickness of the lowest aquitard.
        /// </summary>
        /// <returns></returns>
        public bool HasExtraTopFile()
        {
            if (TOPFilenames.Length != BOTFilenames.Length)
            {
                return true;
            }
            else
            {
                string lowestBotFilename = BOTFilenames[BOTFilenames.Length - 1];
                if ((lowestBotFilename == null) || lowestBotFilename.Equals(string.Empty))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check for aquitard in either REGIS filename format or WVP/SDL format for iMODLayerManager as described in the helpfile
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <returns></returns>
        public bool IsAquitard(int fileIdx)
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
                    try
                    {
                        // Check REGIS format
                        ParseREGISFilename(regisFilename, out string lithologyCode, out int index, out bool isAquifer, out isAquitard, out string layerName, out string substring);
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
        /// Check for aquifer in either REGIS filename format or WVP/SDL format for iMODLayerManager as described in the helpfile
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <returns></returns>
        public bool IsAquifer(int fileIdx)
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
                    try
                    {
                        // Check REGIS format
                        ParseREGISFilename(regisFilename, out string lithologyCode, out int index, out isAquifer, out bool isAquitard, out string layerName, out string substring);
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
                    string prefix2 = REGISLayerModelMap.GetREGISPrefix(filename2).ToLower();
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
                string prefix1 = REGISLayerModelMap.GetREGISPrefix(filename1).ToLower();

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
                string prefix1 = REGISLayerModelMap.GetREGISPrefix(filename1);
                string prefix2 = REGISLayerModelMap.GetREGISPrefix(filename1);
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
                        string prefix = REGISLayerModelMap.GetREGISPrefix(filename).ToLower();
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

        protected void ReadOrderFile(string orderTextFilename, Log log)
        {
            layerOrderPrefixIdxDictionary = new Dictionary<string, int>();
            layerOrderIdxPrefixDictionary = new Dictionary<int, string>();

            Stream stream = null;
            StreamReader sr = null;
            try
            {
                stream = File.Open(orderTextFilename, FileMode.Open, FileAccess.Read);
                sr = new StreamReader(stream);
                int lineCount = 0;
                string wholeLine = null;

                // Skip commment lines
                while ((!sr.EndOfStream) && ((wholeLine == null) || (wholeLine.Trim().StartsWith("#"))))
                {
                    wholeLine = sr.ReadLine();
                    lineCount++;
                }

                // Read last part of each line as the layer prefix
                int layerIdx = 0;
                while (wholeLine != null)
                {
                    string[] lineValues = wholeLine.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    //if (!int.TryParse(lineValues[0], out layerIdx))
                    //{
                    //    throw new ToolException("Could not parse layer index in line " + lineCount + ": " + lineValues[0]);
                    //}
                    string layerprefix = lineValues[lineValues.Length - 1].ToLower();
                    layerOrderPrefixIdxDictionary.Add(layerprefix, layerIdx);
                    layerOrderIdxPrefixDictionary.Add(layerIdx, layerprefix);

                    if (!sr.EndOfStream)
                    {
                        wholeLine = sr.ReadLine();
                        lineCount++;
                        layerIdx++;
                    }
                    else
                    {
                        wholeLine = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read order textfile: " + orderTextFilename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
        }

        /// <summary>
        /// Parse REGIS filename with format: (LAYER(z|k|v|c|q)[_SUB]-(t|b|kh|kv|kd|c)-(ck|sk).IDF
        /// where _SUB is optional and SUB can be a substring of any length
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <param name="lithologyCode"></param>
        /// <param name="index"></param>
        /// <param name="isAquifer"></param>
        /// <param name="isAquitard"></param>
        private static void ParseREGISFilename(string regisFilename, out string lithologyCode, out int index, out bool isAquifer, out bool isAquitard, out string layerName, out string substring)
        {
            index = 0;
            lithologyCode = null;
            substring = null;
            regisFilename = Path.GetFileName(regisFilename);

            int dashIndex1 = regisFilename.IndexOf("-");
            if (dashIndex1 > 0)
            {
                // Check for substring after layername
                int underscoreIndex = regisFilename.IndexOf("_");
                if (underscoreIndex > 0)
                {
                    substring = regisFilename.Substring(underscoreIndex + 1, (dashIndex1 - underscoreIndex - 1));
                    regisFilename = regisFilename.Remove(underscoreIndex, substring.Length + 1);
                    dashIndex1 = regisFilename.IndexOf("-");
                }

                layerName = regisFilename.Substring(0, dashIndex1);
                int dashIndex2 = regisFilename.IndexOf("-", dashIndex1 + 1);
                if (dashIndex2 > 0)
                {
                    string postfix = string.Empty;
                    if (ContainsDigits(layerName))
                    {
                        // check for postfix after layer unit index, e.g.KIk1a or KIz2b
                        int postfixIdx = dashIndex1 - 1;
                        while (postfixIdx > 0)
                        {
                            if (!int.TryParse(regisFilename.Substring(postfixIdx, 1), out int digit))
                            {
                                postfix = regisFilename.Substring(postfixIdx, 1) + postfix;
                            }
                            else
                            {
                                postfixIdx = 0;
                            }
                            postfixIdx--;
                        }
                    }
                    else
                    {
                        postfix = string.Empty;
                    }

                    index = 0;
                    int charIdx = dashIndex1 - 1 - postfix.Length;
                    int factor = 1;
                    while (charIdx > 0)
                    {
                        if (int.TryParse(regisFilename.Substring(charIdx, 1), out int digit))
                        {
                            index += factor * digit;
                            factor *= 10;
                        }
                        else
                        {
                            lithologyCode = regisFilename.Substring(charIdx, 1);
                            charIdx = 0;
                        }
                        charIdx--;
                    }
                }
                else
                {
                    throw new Exception("Filename doesn't have REGIS format: " + Path.GetFileName(regisFilename));
                }
            }
            else
            {
                throw new Exception("Filename doesn't have REGIS format: " + Path.GetFileName(regisFilename));
            }

            isAquifer = false;
            isAquitard = false;
            if (lithologyCode != null)
            {
                switch (lithologyCode.ToLower())
                {
                    case "z":
                        // zand
                        isAquifer = true;
                        break;
                    case "k":
                        // klei
                        isAquitard = true;
                        break;
                    case "c":
                        // complex
                        isAquifer = true;
                        isAquitard = true;
                        break;
                    case "q":
                        // kalk
                        isAquifer = true;
                        isAquitard = true;
                        break;
                    case "v":
                        // veen
                        isAquitard = true;
                        break;
                    case "b":
                        // bruinkool
                        isAquitard = true;
                        break;
                    default:
                        throw new Exception("Unknown REGIS lithology code in filename: " + regisFilename);
                }
            }
        }

        private static bool ContainsDigits(string layerName)
        {
            return layerName.Any(c => char.IsDigit(c));
        }

        /// <summary>
        /// Checks if LayerModelMap contains any KDW-files
        /// </summary>
        /// <returns></returns>
        public bool HasKDWFiles()
        {
            if (KDWFilenames != null)
            {
                for (int idx = 0; idx < KDWFilenames.Length; idx++)
                {
                    if (KDWFilenames[idx] != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if LayerModelMap contains any VCW-files
        /// </summary>
        /// <returns></returns>
        public bool HasVCWFiles()
        {
            if (VCWFilenames != null)
            {
                for (int idx = 0; idx < VCWFilenames.Length; idx++)
                {
                    if (VCWFilenames[idx] != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if LayerModelMap contains any KHV-files
        /// </summary>
        /// <returns></returns>
        public bool HasKHVFiles()
        {
            if (KHVFilenames != null)
            {
                for (int idx = 0; idx < KHVFilenames.Length; idx++)
                {
                    if (KHVFilenames[idx] != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if LayerModelMap contains any KVV-files
        /// </summary>
        /// <returns></returns>
        public bool HasKVVFiles()
        {
            if (KVVFilenames != null)
            {
                for (int idx = 0; idx < KVVFilenames.Length; idx++)
                {
                    if (KVVFilenames[idx] != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Sort currently read filenames in alphanumeric order and check match between corresponding layerfiles
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        public abstract void SortFiles(SIFToolSettings settings, Log log, int logIndentlevel);

        /// <summary>
        /// Read filenames from specified paths in this LayerModelMap object
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentlevel"></param>
        protected abstract void DoReadDirectories(SIFToolSettings settings, Log log, int logIndentlevel);
    }
}
