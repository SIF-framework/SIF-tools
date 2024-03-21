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
using Sweco.SIF.GIS.Clipping;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.ISG;

namespace Sweco.SIF.iMOD.Utils
{
    /// <summary>
    /// Class with commonly used iMOD methods
    /// </summary>
    public class IMODUtils
    {
        /// <summary>
        /// Error margin (m) for conversion, currently only used for GEN-line to IDF conversion
        /// </summary>
        public static double ERRORMARGIN = 0.05;

        /// <summary>
        /// Name of steady-state stress period in RUN-/PRJ-files
        /// </summary>
        public static string SteadyStateName = "steady-state";

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
        /// Checks if the specified string is a known iMOD stress period string
        /// </summary>
        /// <param name="stressPeriodString"></param>
        /// <returns></returns>
        public static bool IsStressPeriodString(string stressPeriodString)
        {
            // TODO: move to Runfile class
            if (!int.TryParse(stressPeriodString, out int stressPeriodInt))
            {
                return stressPeriodString.ToLower().Equals(SteadyStateName);
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
        /// Retrieves a string with the stress period from a given filename
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
        /// Parse a date and time from a stress period string
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
        /// Creates a default filenme for this iMOD-file based a base filename, a layernumber and an optional stress period
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
        /// <param name="layerNrPrefixes">alternative ordered list with prefix strings before layernumber that should be tried, or leave null for default list</param>
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
        /// <param name="topSubString">alternative substring for selection of TOP-files, default is "TOP"</param>
        /// <param name="botSubstring">alternative substring for selection of BOT-files, default is "BOT"</param>
        /// <param name="layerNrPrefixes">alternative ordered list with prefix strings before layernumber that should be tried, or leave null for default</param>
        /// <returns>list with sorted filenames</returns>
        public static List<string> SortiMODLayerFilenames(List<string> filenames, string topSubString = "TOP", string botSubstring = "BOT", List<string> layerNrPrefixes = null)
        {
            return new List<string>(SortiMODLayerFilenames(filenames.ToArray(), topSubString, botSubstring, layerNrPrefixes));
        }

        /// <summary>
        /// Sort specified iMOD-filenames in alphanumerical order with TOP- above BOT-files
        /// </summary>
        /// <param name="filenames"></param>
        /// <param name="topSubString">alternative substring for selection of TOP-files, default is "TOP"</param>
        /// <param name="botSubstring">alternative substring for selection of BOT-files, default is "BOT"</param>
        /// <param name="layerNrPrefixes">alternative ordered list with prefix strings before layernumber that should be tried, or leave null for default</param>
        /// <returns>array with sorted filenames</returns>
        public static string[] SortiMODLayerFilenames(string[] filenames, string topSubString = "TOP", string botSubstring = "BOT", List<string> layerNrPrefixes = null)
        {
            if (filenames != null)
            {
                List<string> tmpFilenamesList = new List<string>();
                Dictionary<string, string> pathDictionary = new Dictionary<string, string>();
                for (int i = 0; i < filenames.Length; i++)
                {
                    string name = Path.GetFileName(filenames[i]);
                    string prefix = GetLayerNumber(name, layerNrPrefixes).ToString();
                    if (name.ToUpper().Contains(topSubString))
                    {
                        prefix += "a";
                    }
                    if (name.ToUpper().Contains(botSubstring))
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
        public static List<T> SortAlphanumericIMODFiles<T>(List<T> imodFiles) where T : IMODFile
        {
            T[] imodFilesArray = imodFiles.ToArray();
            imodFilesArray = iMOD.Utils.IMODUtils.SortAlphanumericIMODFiles<T>(imodFilesArray);
            return imodFilesArray.ToList();
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
                    IDFFile idfFile = IDFFile.ReadFile(iMODFilename, true);
                    return idfFile.Extent;
                case ".ipf":
                    IPFFile ipfFile = IPFFile.ReadFile(iMODFilename, true);
                    return ipfFile.Extent;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert specified IMODFile instance to an IDF-file with specified extent, cellsize and NoData-value
        /// </summary>
        /// <param name="imodFile"></param>
        /// <param name="extent"></param>
        /// <param name="cellSize"></param>
        /// <param name="noDataValue"></param>
        /// <returns></returns>
        public static IDFFile ConvertToIDF(IMODFile imodFile, Extent extent, float cellSize, float noDataValue)
        {
            if (imodFile is IDFFile)
            {
                return (IDFFile)imodFile.Clip(extent);
            }
            else if (imodFile is IPFFile)
            {
                return ConvertIPFToIDF((IPFFile)imodFile, extent, cellSize, noDataValue);
            }
            else if (imodFile is GENFile)
            {
                return ConvertGENToIDF((GENFile)imodFile, extent, cellSize, noDataValue);
            }
            else if (imodFile is ISGFile)
            {
                return ConvertISGToIDF((ISGFile)imodFile, extent, cellSize, noDataValue);
            }
            else
            {
                throw new Exception("ConvertToIDF: Currently no conversion to IDF is available for IMODFile instance of type " + imodFile.GetType().Name);
            }
        }

        /// <summary>
        /// Convert features in specified GENFile instance to IDF-file. Note: currently only contour of polygons is gridded, interior is skipped.
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="extent"></param>
        /// <param name="cellSize"></param>
        /// <param name="noDataValue"></param>
        /// <returns></returns>
        public static IDFFile ConvertGENToIDF(GENFile genFile, Extent extent, float cellSize, float noDataValue)
        {
            Log log = new Log();
            Extent genExtent = genFile.Extent;
            if (genExtent != null)
            {
                genExtent = genExtent.Snap(cellSize, true);
            }
            else
            {
                genExtent = extent;
            }
            IDFFile valueIDFFile = new IDFFile(Path.GetFileNameWithoutExtension(genFile.Filename) + ".IDF", genExtent, cellSize, noDataValue);
            valueIDFFile.DeclareValuesMemory();
            valueIDFFile.ResetValues();
            IDFFile lengthIDFFile = new IDFFile("Length.IDF", genExtent, cellSize, noDataValue);
            lengthIDFFile.DeclareValuesMemory();
            lengthIDFFile.SetValues(0);

            foreach (GENFeature genFeature in genFile.Features)
            {
                GENLine genLine = null;
                if (genFeature is GENLine)
                {
                    genLine = (GENLine)genFeature;
                }
                else 
                {
                    if (genFeature.Points.Count == 0)
                    {
                        continue;
                    }

                    List<GIS.Point> linePoints = null;
                    if (genFeature.Points[0] is Point3D)
                    {
                        // Polygon is 3D, this is used for 3D HFB-files, select only first two points
                        linePoints = new List<GIS.Point>();
                        linePoints.Add(genFeature.Points[0]);
                        if (genFeature.Points.Count > 1)
                        {
                            linePoints.Add(genFeature.Points[1]);
                        }
                    }
                    else
                    {
                        linePoints = new List<GIS.Point>(genFeature.Points);
                        linePoints.RemoveAt(linePoints.Count - 1);
                    }
                    genLine = new GENLine(genFile, genFeature.ID, linePoints);
                }
                
                ConvertGENLineToIDF(genLine, 1, 1, valueIDFFile, lengthIDFFile, log, 0);
            }

            return valueIDFFile;
        }

        /// <summary>
        /// Convert segments in specified ISGFile instance to IDF-file
        /// </summary>
        /// <param name="isgFile"></param>
        /// <param name="extent"></param>
        /// <param name="cellSize"></param>
        /// <param name="noDataValue"></param>
        /// <returns></returns>
        public static IDFFile ConvertISGToIDF(ISGFile isgFile, Extent extent, float cellSize, float noDataValue)
        {
            GENFile isgGENFile = ConvertISGToGEN(isgFile);
            IDFFile idfFile = ConvertGENToIDF(isgGENFile, extent, cellSize, noDataValue);
            return idfFile;
        }

        /// <summary>
        /// Convert segments in specified ISGFile instance to a GEN-file
        /// </summary>
        /// <param name="isgFile"></param>
        /// <returns></returns>
        public static GENFile ConvertISGToGEN(ISGFile isgFile)
        {
            GENFile isgGENFile = new GENFile();
            isgFile.EnsureLoadedSegments();
            if (isgFile.Segments != null)
            {
                foreach (ISGSegment isgSegment in isgFile.Segments)
                {
                    GENLine genLine = new GENLine(isgGENFile, isgSegment.Label);
                    foreach (ISGNode isgNode in isgSegment.Nodes)
                    {
                        genLine.AddPoint(new FloatPoint(isgNode.X, isgNode.Y));
                    }
                    isgGENFile.AddFeature(genLine);
                }
            }
            return isgGENFile;
        }

        public static IDFFile ConvertIPFToIDF(IPFFile ipfFile, Extent extent, float cellSize, float noDataValue)
        {
            string idfFilename = Path.Combine(Path.GetDirectoryName(ipfFile.Filename), Path.GetFileNameWithoutExtension(ipfFile.Filename) + ".IDF");
            IDFFile idfFile = new IDFFile(idfFilename, extent, cellSize, noDataValue);
            idfFile.ResetValues();
            for (int i = 0; i < ipfFile.PointCount; i++)
            {
                IPFPoint ipfPoint = ipfFile.GetPoint(i);
                if (ipfPoint.IsContainedBy(extent))
                {
                    float x = (float) ipfPoint.X;
                    float y = (float) ipfPoint.Y;
                    float value = 1;
                    if ((ipfFile.AssociatedFileColIdx > 0) && (ipfFile.AssociatedFileColIdx < ipfFile.ColumnNames.Count))
                    {
                        if (ipfPoint.ColumnValues.Count < ipfFile.AssociatedFileColIdx)
                        {
                            throw new Exception("Missing columnvalues: TextFileColumnIdx (" + ipfFile.AssociatedFileColIdx + ") > number of columnvalues (" + ipfPoint.ColumnValues.Count + ") for point " + i + " in: " + ipfFile.Filename);
                        }
                        string valueString = ipfPoint.ColumnValues[ipfFile.AssociatedFileColIdx];
                        if (!float.TryParse(valueString, out value))
                        {
                            value = 1;
                        }
                    }
                    idfFile.SetValue(x, y, 1);
                }
            }
            return idfFile;
        }

        /// <summary>
        /// Convert GEN-line to IDF-cell values in valueIDFFile
        /// </summary>
        /// <param name="genLine">GENLine object to convert</param>
        /// <param name="genValue">value at starting node</param>
        /// <param name="genValue2">value at startendnode</param>
        /// <param name="valueIDFFile">IDFFile object to store interpolated result</param>
        /// <param name="lengthIDFFile">IDFFile object to store line length per cel</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public static void ConvertGENLineToIDF(GENLine genLine, float genValue, float genValue2, IDFFile valueIDFFile, IDFFile lengthIDFFile, Log log = null, int logIndentLevel = 0)
        {
            // Algorithm is as follows:
            // 0. Assume value to set along line has been defined and stored in class variable genValue
            // 1. Loop through line segments
            //   2. Loop through cells that line segment passes through
            //     2a. determine cell that line segment starts in
            //     2b. determine part of line segment in cell
            //     3. if line segment continues outside cell:
            //       3a. update statistics for line length inside cell
            //       3b. continue at 2b, with part of line segment outside cell
            //     4. if line segment is completely inside cell: 
            //       4a. update statisics for length of line inside cell
            //       4b. continue at 1 with next line segment
            // Note: valueIDFFile, angleIDFFile, etc. are class variables.

            // Work with GEN-extent for looping through all lines
            float xCellsize = valueIDFFile.XCellsize;
            float yCellsize = valueIDFFile.YCellsize;
            Extent genExtent = genLine.GENFile.Extent;
            genExtent = genExtent.Snap(valueIDFFile.XCellsize, valueIDFFile.YCellsize, true);

            float cellsize = xCellsize;
            float halfCellsize = cellsize / 2.0f;
            double lineLength = genLine.CalculateLength();

            // Determine cell that line starts in
            int currCellRowIdx = GetRowIdx(genExtent, yCellsize, genLine.Points[0].Y);
            int currCellColIdx = GetColIdx(genExtent, xCellsize, genLine.Points[0].X);
            float currCellX = GetX(genExtent, xCellsize, currCellColIdx);
            float currCellY = GetY(genExtent, yCellsize, currCellRowIdx);
            GIS.Point currLinePoint = genLine.Points[0];
            GIS.Point currEndPoint = currLinePoint;
            GIS.Point currentCellEntrancePoint = currLinePoint.Copy(); // new FloatPoint(currCellX, currCellY);
            GIS.Point currentCellLeavingPoint = null;

            // 1. Loop through line segments
            int cellIdx = 0;
            double processedLineDistance = 0;
            double currCellStartDistance = 0;
            for (int pointIdx = 1; pointIdx < genLine.Points.Count; pointIdx++)
            {
                // Retrieve next line segment
                GIS.Point nextLinePoint = genLine.Points[pointIdx];
                LineSegment segment = new LineSegment(currLinePoint, nextLinePoint);

                // 2. Loop through cells that line segment passes through
                double processedSegmentLength = 0;
                int emptySegmentCount = 0;
                do
                {
                    // determine part of line segment in cell
                    Extent currCellExtent = new Extent(currCellX - halfCellsize, currCellY - halfCellsize, currCellX + halfCellsize, currCellY + halfCellsize);
                    LineSegment cellSegment = CSHFClipper.ClipLine(segment, currCellExtent);
                    if ((cellSegment != null) && (cellSegment.Length > 0))
                    {
                        processedSegmentLength += cellSegment.Length;
                        processedLineDistance += cellSegment.Length;

                        currEndPoint = cellSegment.P2;
                        emptySegmentCount = 0;
                    }
                    else
                    {
                        emptySegmentCount++;

                        if (emptySegmentCount > 2)
                        {
                            HandleMissingSegment(currCellX, currCellY, emptySegmentCount, segment.Length, processedSegmentLength, log);

                            currCellX = -1;
                            currCellY = -1;
                            currentCellEntrancePoint = null;
                            currentCellLeavingPoint = null;
                            break;
                        }

                    }

                    // 3. Check if line segment continues outside cell:
                    if ((segment.Length - processedSegmentLength) > ERRORMARGIN)
                    {
                        //  3a. determine interpolated value for current cell (which may be based on line length and distance along line halfway through cell)
                        CalculateCellLineValue(currCellX, currCellY, cellIdx, currCellStartDistance, processedLineDistance, lineLength, genValue, genValue2, valueIDFFile, lengthIDFFile);

                        currentCellLeavingPoint = currEndPoint;
                        currentCellEntrancePoint = currentCellLeavingPoint;
                        currentCellLeavingPoint = null;

                        // Find next cell with steps of 10 cm after endpoint of cell segment in the direction of the line segment
                        int nextCellRowIdx;
                        int nextCellColIdx;
                        GIS.Point nextCellPoint = currEndPoint;
                        do
                        {
                            nextCellPoint = nextCellPoint.Move(new Vector(nextLinePoint.X - currLinePoint.X, nextLinePoint.Y - currLinePoint.Y), 0.1);
                            nextCellRowIdx = GetRowIdx(genExtent, yCellsize, nextCellPoint.Y);
                            nextCellColIdx = GetColIdx(genExtent, xCellsize, nextCellPoint.X);
                        } while ((nextCellRowIdx == currCellRowIdx) && (nextCellColIdx == currCellColIdx));
                        currCellRowIdx = nextCellRowIdx;
                        currCellColIdx = nextCellColIdx;
                        currCellX = GetX(genExtent, xCellsize, currCellColIdx);
                        currCellY = GetY(genExtent, yCellsize, currCellRowIdx);

                        currCellStartDistance = processedLineDistance;

                        // 3c. continue at 2b, with part of line segment outside cell
                        cellIdx++;
                    }
                }
                while ((segment.Length - processedSegmentLength) > ERRORMARGIN);

                //  4. last part of line segment is completely inside current cell, continue with step 1 for next line segment
                currLinePoint = nextLinePoint;
            }

            InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength, valueIDFFile, lengthIDFFile);
        }

        /// <summary>
        /// Calculate value for current cell (based on line length and distance along line halfway through cell)
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="cellIdx"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="processedLineDistance"></param>
        /// <param name="lineLength"></param>
        private static void CalculateCellLineValue(float currCellX, float currCellY,  int cellIdx, double currCellStartDistance, double processedLineDistance, double lineLength, float genValue, float genValue2, IDFFile valueIDFfile, IDFFile lengthIDFFile)
        {
            if (cellIdx == 0)
            {
                // Handle first cell
                InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength, valueIDFfile, lengthIDFFile);
            }
            else
            {
                // Handle intermediate cell
                InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength, valueIDFfile, lengthIDFFile);
            }
        }

        /// <summary>
        /// Interpolate value for specific cell along GEN-line based on val1 and val2 at both ends of the line
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="currCellEndDistance"></param>
        /// <param name="lineLength"></param>
        /// <param name="valueIDFFile"></param>
        /// <param name="maxIntDist"></param>
        private static void InterpolateCurrentCellValue(float currCellX, float currCellY, float val1, float val2, double currCellStartDistance, double currCellEndDistance, double lineLength, IDFFile valueIDFFile, IDFFile lengthIDFFile, float maxIntDist = float.NaN)
        {
            // Calculate current cell value 
            if ((currCellX >= 0) && (currCellY >= 0) && valueIDFFile.Extent.Contains(currCellX, currCellY))
            {
                float currCellValue = valueIDFFile.GetValue(currCellX, currCellY);
                double distance = (currCellStartDistance + currCellEndDistance) / 2;
                float newCellValue = float.NaN;
                if (val1.Equals(float.NaN) || val1.Equals(valueIDFFile.NoDataValue))
                {
                    if (!val2.Equals(float.NaN))
                    {
                        if (maxIntDist.Equals(float.NaN) || ((lineLength - distance) < maxIntDist))
                        {
                            newCellValue = val2;
                        }
                        else
                        {
                            newCellValue = valueIDFFile.NoDataValue;
                        }
                    }
                    else
                    {
                        newCellValue = valueIDFFile.NoDataValue;
                    }
                }
                else if (val2.Equals(float.NaN))
                {
                    if (maxIntDist.Equals(float.NaN) || (distance < maxIntDist))
                    {
                        newCellValue = val1;
                    }
                    else
                    {
                        newCellValue = valueIDFFile.NoDataValue;
                    }
                }
                else
                {
                    newCellValue = (float)(val1 + (distance / lineLength) * (val2 - val1));
                }
                if (currCellValue.Equals(valueIDFFile.NoDataValue))
                {
                    // no other value has been set yet for this cell, so simply set cell value
                    valueIDFFile.SetValue(currCellX, currCellY, newCellValue);
                }
                else if (!newCellValue.Equals(valueIDFFile.NoDataValue))
                {
                    // another value is already present in this cell from another line segment
                    double currLength = lengthIDFFile.GetValue(currCellX, currCellY);
                    double newLength = currCellEndDistance - currCellStartDistance;
                    // correct currLength, since newLength is already added during line segment processing
                    currLength = currLength - newLength;

                    // set new cell value to weighted average of both values;
                    newCellValue = (float)((newLength / (newLength + currLength)) * newCellValue + (currLength / (newLength + currLength)) * currCellValue);
                    valueIDFFile.SetValue(currCellX, currCellY, newCellValue);
                }
                else
                {
                    // Currrent line segment doesn't have values, keep current cellvalue
                }
            }
        }

        /// <summary>
        /// Handle missed segment length's during processing which might occur because of rounding, etc.
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="emptySegmentCount"></param>
        /// <param name="segmentLength"></param>
        /// <param name="processedSegmentLength"></param>
        private static void HandleMissingSegment(float currCellX, float currCellY, int emptySegmentCount, double segmentLength, double processedSegmentLength, Log log = null, IPFFile warningIPFFile = null)
        {
            if (emptySegmentCount > 2)
            {
                if (log != null)
                {
                    log.AddWarning("Unexpected missing segment, set to zero-value: (" + currCellX.ToString("F3", ParseUtils.EnglishCultureInfo) + "," + currCellY.ToString("F3", ParseUtils.EnglishCultureInfo) + ")");
                }
                if (warningIPFFile != null)
                {
                    warningIPFFile.AddPoint(new IPFPoint(warningIPFFile, new FloatPoint(currCellX, currCellY), new List<string>() { currCellX.ToString(ParseUtils.EnglishCultureInfo), currCellY.ToString(ParseUtils.EnglishCultureInfo), (segmentLength - processedSegmentLength).ToString(ParseUtils.EnglishCultureInfo), "Unexpected missing segment: segment.Length (" + segmentLength + ") - processedSegmentLength (" + processedSegmentLength + ") > ERRORMARGIN (" + ERRORMARGIN + ")" }));
                }
            }
        }

        /// <summary>
        /// Retrieves the rowindex into the values-array for the given y-value
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetRowIdx(IDFFile idfFile, double y)
        {
            return (int)(((idfFile.Extent.ury - 0.00001) - y) / idfFile.YCellsize);
        }

        /// <summary>
        /// Retrieves the rowindex into the extent for the given y
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="yCellsize"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetRowIdx(Extent extent, float yCellsize, double y)
        {
            return (int)(((extent.ury - 0.00001) - y) / yCellsize);
        }

        /// <summary>
        /// Retrieves the columnindex into the extent for the given x
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int GetColIdx(Extent extent, float xCellsize, double x)
        {
            return (int)((x - extent.llx) / xCellsize);
        }

        /// <summary>
        /// Retrieves the x-value for the given columnindex into the extent
        /// </summary>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public static float GetX(Extent extent, float xCellsize, int colIdx)
        {
            return extent.llx + (colIdx + 0.5f) * xCellsize;
        }

        /// <summary>
        /// Retrieves the y-value for the given rowindex into the extent
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <returns></returns>
        public static float GetY(Extent extent, float yCellsize, int rowIdx)
        {
            return extent.ury - (rowIdx + 0.5f) * yCellsize;
        }
    }
}
