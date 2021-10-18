// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.iMOD.Utils
{
    /// <summary>
    /// Class with commonly used iMOD methods
    /// </summary>
    public class IMODUtils
    {
        /// <summary>
        /// Writes a line to the specified StreamWriter if not null. Expcetions are not caught here.
        /// </summary>
        /// <param name="sw">StreamWriter object or null</param>
        /// <param name="line">line to write</param>
        public static void WriteLine(StreamWriter sw, string line)
        {
            if (sw != null)
            {
                sw.Write(line);
            }
        }

        /// <summary>
        /// Checks if two values are equal within specified tolerance
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="tolerance">if difference between value is less than tolerance values are consisderd equal</param>
        /// <returns></returns>
        public static bool IsEqual(float value1, float value2, float tolerance)
        {
            return (Math.Abs(value1 - value2) < tolerance);
        }

        /// <summary>
        /// Checks if the specified string is a known iMOD stressperiod string
        /// </summary>
        /// <param name="stressPeriodString"></param>
        /// <returns></returns>
        public static bool IsStressPeriodString(string stressPeriodString)
        {
            // TODO: move to Runfile class
            if (!int.TryParse(stressPeriodString, out int stressPeriodInt))
            {
                return stressPeriodString.ToLower().Equals("steady-state");
            }

            try
            {
                if (stressPeriodString.Length != 8)
                {
                    return false;
                }
                int year = int.Parse(stressPeriodString.Substring(0, 4));
                int month = int.Parse(stressPeriodString.Substring(4, 2));
                int day = int.Parse(stressPeriodString.Substring(6, 2));
                DateTime date = new DateTime(year, month, day);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves a string with the stressperiod from a given filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetStressPeriodString(string filename)
        {
            // TODO: move to Runfile class
            string fname = Path.GetFileNameWithoutExtension(filename);
            int underScoreIdx1 = fname.IndexOf("_");
            if ((underScoreIdx1 > 0) && (underScoreIdx1 < fname.Length - 1))
            {
                int underScoreIdx2 = fname.IndexOf("_", underScoreIdx1 + 1);
                if (underScoreIdx2 > 0)
                {
                    string stressPeriodString = fname.Substring(underScoreIdx1 + 1, (underScoreIdx2 - underScoreIdx1 - 1));
                    if (!IsStressPeriodString(stressPeriodString))
                    {
                        stressPeriodString = string.Empty;
                    }
                    return stressPeriodString;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Parse a date and time from a stressperiod string
        /// </summary>
        /// <param name="stressperiodString"></param>
        /// <returns>DateTime 0 if an error ocurred during parsing</returns>
        public static DateTime ParseStressPeriodString(string stressperiodString)
        {
            try
            {
                int year = int.Parse(stressperiodString.Substring(0, 4));
                int month = int.Parse(stressperiodString.Substring(4, 2));
                int day = int.Parse(stressperiodString.Substring(6, 2));
                return new DateTime(year, month, day);
            }
            catch (Exception)
            {
                return new DateTime(0);
            }
        }

        /// <summary>
        /// Creates a default filenme for this iMOD-file based a base filename, a layernumber and an optional stressperiod
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="subdir"></param>
        /// <param name="baseFilename"></param>
        /// <param name="ilay"></param>
        /// <param name="extension">filename extension</param>
        /// <param name="stressPeriodString"></param>
        /// <returns></returns>
        public static string CreateDefaultFilename(string outputPath, string subdir, string baseFilename, int ilay, string extension, string stressPeriodString = null)
        {
            string kperString = string.Empty;
            if ((stressPeriodString != null) && !stressPeriodString.Equals(string.Empty))
            {
                kperString = "_" + stressPeriodString;
            }
            //if ((kper > 0) && (startDate != null))
            //{
            //    kperString = "_" + Model.GetStressPeriodString(startDate, kper);
            //}

            if (subdir == null)
            {
                return Path.Combine(outputPath, baseFilename + "_L" + ilay + kperString + "." + extension);
            }
            else
            {
                return Path.Combine(FileUtils.EnsureFolderExists(outputPath, subdir), baseFilename + "_L" + ilay + kperString + "." + extension);
            }
        }

        /// <summary>
        /// Retrieves the layernumber from a layername string, e.g. an iMOD-filename. Take all numeric consecutive values in the remaining string, after last occurence of one of the specified layerNrPrefix substrings.
        /// As a default the following layerNumber prefixes are tried: : '_L', 'LAAG', 'LAYER', 'WVP', 'SDL', 'TOP', 'BOT' or 'L' when L is the starting and only character of the layername string (excluding optional file extension)
        /// or '' (the empty prefix) when the specified layername (excluding optional file extension) just contains numeric values.
        /// </summary>
        /// <param name="layername">string with name for layer, e.g.g an iMOD-filename like 'TOP_L1.IDF' or 'BOT_L10'</param>
        /// <param name="layerNrPrefixes"></param>
        /// <param name="isExceptionThrown">if true an exception is thrown when no numeric value is found, if false -1 is returned in case of an error</param>
        /// <returns>if isExceptionThrow=true, an exception is thrown (default) when no numeric value is found after specified prefixes, otherwise -1 is returned.</returns>
        public static int GetLayerNumber(string layername, List<string> layerNrPrefixes = null, bool isExceptionThrown = true)
        {
            if (layername == null)
            {
                throw new ToolException("Could not parse layernumber for empty layername string");
            }

            string fname = Path.GetFileNameWithoutExtension(layername);
            int value;
            if (int.TryParse(fname, out value))
            {
                return value;
            }

            if (layerNrPrefixes == null)
            {
                if (layername.StartsWith("L") && int.TryParse(layername.Substring(1), out value))
                {
                    layerNrPrefixes = new List<string>() { "L" };
                }
                else
                {
                    layerNrPrefixes = new List<string>() { "_L", "LAAG", "LAYER", "WVP", "SDL", "TOP", "BOT" };
                }
            }

            foreach (string layerNrPrefix in layerNrPrefixes)
            {
                int idx = fname.LastIndexOf(layerNrPrefix, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string numericString = string.Empty;
                    // skip non-numeric values
                    idx = idx + layerNrPrefix.Length;
                    while ((idx < fname.Length) && (fname[idx] < '0') && (fname[idx] > '9'))
                    {
                        idx++;
                    }

                    //take remaing numeric values
                    while ((idx < fname.Length) && (fname[idx] >= '0') && (fname[idx] <= '9'))
                    {
                        numericString += fname[idx];
                        idx++;
                    }

                    return int.Parse(numericString);
                }
            }

            if (isExceptionThrown)
            {
                throw new ToolException("Could not parse layernumber for layername string: " + Path.GetFileName(layername));
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Find last numeric value in a string. If string is a filename with an extension, only the filename (excluding extension) is used.
        /// </summary>
        /// <param name="someString"></param>
        /// <returns></returns>
        public static int GetLastNumericValue(string someString)
        {
            string name = Path.GetFileNameWithoutExtension(someString);
            int layernumber = 0;
            int idx = name.Length - 1;
            int digit;
            int factor = 1;
            // First backwards find last digit in string
            while ((idx >= 0) && !int.TryParse(name.Substring(idx, 1), out digit))
            {
                idx--;
            }

            // read digits from last part of string1
            while ((idx >= 0) && int.TryParse(name.Substring(idx, 1), out digit))
            {
                layernumber += factor * digit;
                factor *= 10;
                idx--;
            }

            if ((idx < 0) && (factor > 1))
            {
                throw new ToolException("Could not parse value for string: " + Path.GetFileName(someString));
            }

            return layernumber;
        }

        /// <summary>
        /// Read and return GENFile objects for all specified GEN filenamers
        /// </summary>
        /// <param name="inputFiles"></param>
        /// <param name="isIDRecalculated">if true, a new numeric ID is generated for all GEN-features</param>
        /// <returns></returns>
        public static GENFile[] ReadGENFiles(string[] inputFiles, bool isIDRecalculated = false)
        {
            List<GENFile> genFiles = new List<GENFile>();
            for (int i = 0; i < inputFiles.Length; i++)
            {
                GENFile genFile = GENFile.ReadFile(inputFiles[i], isIDRecalculated);
                genFiles.Add(genFile);
            }
            return genFiles.ToArray();
        }

        /// <summary>
        /// Convert a Color object to a long RGB-value in blocks of 8 bits, lowest bits specify green
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static long Color2Long(Color color)
        {
            return color.R + color.G * 256 + color.B * 65536;
        }

        /// <summary>
        /// Convert a long value to a Color object
        /// </summary>
        /// <param name="rgbColorValue">RGB-value in blocks of 8 bits, lowest bits specify green</param>
        /// <returns></returns>
        public static Color Long2Color(long rgbColorValue)
        {
            return Color.FromArgb((int)rgbColorValue % 256, (int)(rgbColorValue % 65536) / 256, (int)rgbColorValue / 65536);
        }

        /// <summary>
        /// Sort specified iMOD-filenames in alphanumerical order with TOP- above BOT-files
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns>list with sorted filenames</returns>
        public static List<string> SortiMODLayerFilenames(List<string> filenames)
        {
            return new List<string>(SortiMODLayerFilenames(filenames.ToArray()));
        }

        /// <summary>
        /// Sort specified iMOD-filenames in alphanumerical order with TOP- above BOT-files
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns>array with sorted filenames</returns>
        public static string[] SortiMODLayerFilenames(string[] filenames)
        {
            if (filenames != null)
            {
                List<string> tmpFilenamesList = new List<string>();
                Dictionary<string, string> pathDictionary = new Dictionary<string, string>();
                for (int i = 0; i < filenames.Length; i++)
                {
                    string name = Path.GetFileName(filenames[i]);
                    string prefix = GetLayerNumber(name).ToString();
                    if (name.ToUpper().Contains("TOP"))
                    {
                        prefix += "a";
                    }
                    if (name.ToUpper().Contains("BOT"))
                    {
                        prefix += "b";
                    }
                    tmpFilenamesList.Add(prefix + name);
                    pathDictionary.Add(prefix + name, filenames[i]);
                }
                string[] tmpFilenamesArray = tmpFilenamesList.ToArray();
                CommonUtils.SortAlphanumericStrings(tmpFilenamesArray);

                List<string> sortedFilenames = new List<string>();
                for (int i = 0; i < filenames.Length; i++)
                {
                    string name = Path.GetFileName(tmpFilenamesArray[i]);
                    if (pathDictionary.ContainsKey(name))
                    {
                        string filename = pathDictionary[name];
                        sortedFilenames.Add(filename);
                    }
                    else
                    {
                        throw new Exception("Could not find filename in dictionary: " + name);
                    }
                }

                return sortedFilenames.ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sort specified type of iMOD-files in alphanumerical order of filenames
        /// </summary>
        /// <param name="imodFiles"></param>
        /// <returns>a new, sorted array is returned</returns>
        public static T[] SortAlphanumericIMODFiles<T>(T[] imodFiles) where T : IMODFile
        {
            if (imodFiles != null)
            {
                List<string> filenames = new List<string>();
                string[] filenamesArray;
                Dictionary<string, T> imodFilesDictionary = new Dictionary<string, T>();
                List<T> sortedIMODFiles = new List<T>();
                for (int i = 0; i < imodFiles.Count(); i++)
                {
                    string name = Path.GetFileName(imodFiles[i].Filename);
                    filenames.Add(name);
                    imodFilesDictionary.Add(name, imodFiles[i]);
                }
                filenamesArray = filenames.ToArray();
                CommonUtils.SortAlphanumericStrings(filenamesArray);
                for (int i = 0; i < imodFiles.Count(); i++)
                {
                    string name = Path.GetFileName(filenamesArray[i]);
                    if (imodFilesDictionary.ContainsKey(name))
                    {
                        T imodFile = imodFilesDictionary[name];
                        sortedIMODFiles.Add(imodFile);
                    }
                    else
                    {
                        throw new Exception("Could not find filename in dictionary: " + name);
                    }
                }

                return sortedIMODFiles.ToArray();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve (bounding box) extent of iMOD-file
        /// </summary>
        /// <param name="iMODFilename"></param>
        /// <returns></returns>
        public static Extent RetrieveExtent(string iMODFilename)
        {
            string ext = Path.GetExtension(iMODFilename).ToLower();
            switch (ext)
            {
                case ".gen":
                    GENFile genFile = GENFile.ReadFile(iMODFilename);
                    return genFile.Extent;
                case ".idf":
                    IDFFile idfFile = IDFFile.ReadFile(iMODFilename);
                    return idfFile.Extent;
                case ".ipf":
                    IPFFile ipfFile = IPFFile.ReadFile(iMODFilename);
                    return ipfFile.Extent;
                default:
                    return null;
            }
        }
    }
}
