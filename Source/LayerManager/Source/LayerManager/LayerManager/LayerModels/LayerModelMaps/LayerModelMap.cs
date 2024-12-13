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
                throw new ToolException("No TOP-files (\"" + settings.TOPFilenamesPatterns + "\") found in " + settings.TOPPath);
            }
            if ((BOTFilenames == null) || (BOTFilenames.Length == 0))
            {
                throw new ToolException("No BOT-files (\"" + settings.BOTFilenamesPatterns + "\") found in " + settings.BOTPath);
            }
            if (TOPFilenames.Length != BOTFilenames.Length)
            {
                throw new ToolException("Number of TOP-files (" + TOPFilenames.Length + ") is not equal to number of BOT-files (" + BOTFilenames.Length + ")");
            }
            if ((KHVFilenames != null) && ((KHVFilenames.Length == 0) && (settings.KHVParString != null)))
            {
                log.AddWarning("No kh-files (\"" + settings.KHVFilenamesPatterns + "\") found in " + settings.KHVPath, logIndentlevel);
            }
            if ((KVVFilenames != null) && ((KVVFilenames.Length == 0) && (settings.KVVParString != null)))
            {
                log.AddWarning("No kv-files (\"" + settings.KVVFilenamesPatterns + "\") found in " + settings.KHVPath, logIndentlevel);
            }
            if ((KDWFilenames != null) && ((KDWFilenames.Length == 0) && (settings.KDWParString != null)))
            {
                log.AddInfo("No kD-files (\"" + settings.KDWFilenamesPatterns + "\") found in " + settings.KDWPath, logIndentlevel);
            }
            if ((VCWFilenames != null) && ((VCWFilenames.Length == 0) && (settings.VCWParString != null)))
            {
                log.AddInfo("No C-files (\"" + settings.VCWFilenamesPatterns + "\") found in " + settings.VCWPath, logIndentlevel);
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

        /// <summary>
        /// Check for aquifer
        /// </summary>
        /// <param name="fileIdx"></param>
        /// <returns></returns>
        public abstract bool IsAquifer(int fileIdx);

        /// <summary>
        /// Check for aquitard
        /// </summary>
        /// <param name="fileIdx"></param>
        /// <returns></returns>
        public abstract bool IsAquitard(int fileIdx);
    }
}
