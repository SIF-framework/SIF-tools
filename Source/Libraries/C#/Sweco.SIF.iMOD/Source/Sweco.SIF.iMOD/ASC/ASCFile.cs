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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMOD.ASC
{
    /// <summary>
    /// Class to read, modify and write ASC-files.
    /// </summary>
    public class ASCFile
    {
        /// <summary>
        /// File extension of an ASC-file
        /// </summary>
        public static string Extension = "ASC";

        /// <summary>
        /// // Matrix with ASC-values in this ASC-file: [rows][cols]
        /// </summary>
        public float[][] Values { get; set; }

        /// <summary>
        /// Number of columns in this ASC-file
        /// </summary>
        /// 
        public int NCols { get; set; }

        /// <summary>
        /// Number of rows in this ASC-file
        /// </summary>
        public int NRows { get; set; }

        /// <summary>
        /// X-coordinate of lower left corner 
        /// </summary>
        public float XLL { get; set; }

        /// <summary>
        /// Y-coordinate of lower left corner 
        /// </summary>
        public float YLL { get; set; }

        /// <summary>
        /// Cellsize of this ASC-file
        /// </summary>
        public float Cellsize { get; set; }

        /// <summary>
        /// NoData-value of this ASC-file
        /// </summary>
        public float NoDataValue { get; set; }

        /// <summary>
        /// Filename of this ASC-file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// LastWriteTime of this ASC-file when it was read
        /// </summary>
        private DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Create empty ASC-file object
        /// </summary>
        public ASCFile()
        {
        }

        /// <summary>
        /// Create ASC-file from specified IDF-file object
        /// </summary>
        /// <param name="idfFile"></param>
        public ASCFile(IDFFile idfFile)
        {
            string filename = Path.Combine(Path.GetDirectoryName(idfFile.Filename), Path.GetFileNameWithoutExtension(idfFile.Filename) + ".IDF");
            this.Filename = filename;
            this.XLL = idfFile.Extent.llx;
            this.YLL = idfFile.Extent.lly;
            this.NRows = idfFile.NRows;
            this.NCols = idfFile.NCols;
            this.Cellsize = idfFile.XCellsize;
            this.NoDataValue = idfFile.NoDataValue;
            this.Values = idfFile.values;
        }

        /// <summary>
        /// Check if this ASC-file has any non-NoData values
        /// </summary>
        /// <returns></returns>
        public bool HasDataValues()
        {
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if (!Values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieve value at specified x- and y-coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetValue(float x, float y)
        {
            int invRow = (int)((y - YLL) / Cellsize);
            int row = (NRows - 1) - invRow;
            int col = (int)((x - XLL) / Cellsize);
            float value = 0;
            if ((row >= 0) && (col >= 0) && (row < NRows) && (col < NCols))
            {
                try
                {
                    value = Values[row][col];
                }
                catch (Exception ex)
                {
                    throw new Exception("Coordinates (" + x + "," + y + ") are outside the bounds of file " + Filename, ex);
                }
            }
            else
            {
                value = NoDataValue;
            }
            return value;
        }

        /// <summary>
        /// Check if specified file has an ASC-extension
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasASCExtension(string filename)
        {
            return (Path.GetExtension(filename).ToUpper().Equals("." + Extension));
        }

        /// <summary>
        /// Read ASC-file from disk with specified format provider for language settings to use when parsing values.
        /// List seperators that are tried for seperating ASC-values are: space, tab and comma.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public static ASCFile ReadFile(string filename, IFormatProvider formatProvider)
        {
            Stream stream = null;
            StringReader sr = null;

            ASCFile ascFile = new ASCFile();
            try
            {
                ascFile.Filename = filename;
                ascFile.LastWriteTime = File.GetLastWriteTime(filename);

                string ascString = File.ReadAllText(filename);

                if (!ascString.Contains(".") && ascString.Contains(","))
                {
                    // Correct for ASC-file with comma's as decimal seperator
                    ascString = ascString.Replace(',', '.');
                }

                // stream = File.OpenRead(filename); // File.Open(filename, FileMode.Open, FileAccess.Read)
                sr = new StringReader(ascString);

                // Read Definitions
                string line = sr.ReadLine();
                string[] splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.NCols = int.Parse(splits[1]);
                line = sr.ReadLine();
                splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.NRows = int.Parse(splits[1]);
                line = sr.ReadLine();
                splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.XLL = float.Parse(splits[1], formatProvider);
                line = sr.ReadLine();
                splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.YLL = float.Parse(splits[1], formatProvider);
                line = sr.ReadLine();
                splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.Cellsize = float.Parse(splits[1], formatProvider);
                line = sr.ReadLine();
                splits = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                ascFile.NoDataValue = float.Parse(splits[1], formatProvider);

                ascFile.Values = new float[ascFile.NRows][];
                for (int i = 0; i < ascFile.NRows; i++)
                {
                    ascFile.Values[i] = new float[ascFile.NCols];
                }

                int colIdx = 0;
                int rowIdx = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line != null)
                    {
                        string[] values = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (rowIdx >= ascFile.NRows)
                            {
                                throw new ToolException("Too many values found in ASC-file, check decimal seperator");
                            }

                            float value = float.Parse(values[i], formatProvider);
                            ascFile.Values[rowIdx][colIdx] = value;
                            colIdx++;
                            if (colIdx == ascFile.NCols)
                            {
                                colIdx = 0;
                                rowIdx++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read ASC-file " + filename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return ascFile;
        }

        /// <summary>
        /// Write ASC-file data to specified file, using specified format provider with language settings
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="formatProvider"></param>
        public void WriteFile(string filename, IFormatProvider formatProvider)
        {
            this.Filename = filename;
            WriteFile(formatProvider, false);
        }

        /// <summary>
        /// Write ASC-file data to file with as defined by Filename, using specified format provider with language settings
        /// </summary>
        /// <param name="formatProvider"></param>
        /// <param name="copyLastWriteTime"></param>
        public void WriteFile(IFormatProvider formatProvider, bool copyLastWriteTime)
        {
            StreamWriter sw = null;
            try
            {
                if (!Path.GetDirectoryName(Filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(Filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Filename));
                }

                if (Path.GetExtension(Filename).Equals(string.Empty))
                {
                    Filename += ".ASC";
                }

                sw = new StreamWriter(Filename, false);

                // Write Definitions
                sw.WriteLine("NCOLS        " + NCols);
                sw.WriteLine("NROWS        " + NRows);
                sw.WriteLine("XLLCORNER    " + XLL.ToString(formatProvider));
                sw.WriteLine("YLLCORNER    " + YLL.ToString(formatProvider));
                sw.WriteLine("CELLSIZE     " + Cellsize.ToString(formatProvider));
                sw.WriteLine("NODATA_VALUE " + NoDataValue.ToString(formatProvider));

                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        sw.Write(Values[rowIdx][colIdx].ToString(formatProvider) + " ");
                    }
                    sw.WriteLine();
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + Filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            if (copyLastWriteTime)
            {
                File.SetLastWriteTime(Filename, this.LastWriteTime);
            }
        }

        /// <summary>
        /// Clip ASC-file data to specified extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isInvertedClip">if true, part outside extent is retained</param>
        /// <returns></returns>
        public ASCFile Clip(Extent clipExtent, bool isInvertedClip = false)
        {
            int upperRowIdx;
            int lowerRowIdx;
            int leftColIdx;
            int rightColIdx;
            int invRow;

            float xur = XLL + Cellsize * NCols;
            float yur = YLL + Cellsize * NRows;
            Extent ascExtent = new Extent(XLL, YLL, xur, yur);
            if (clipExtent == null)
            {
                // Use extent of ASC-file
                clipExtent = ascExtent.Copy();
            }

            if (!ascExtent.Clip(clipExtent).IsValidExtent())
            {
                return null;
                // throw new Exception("No overlap in extent of '" + Path.GetFileName(this.Filename) + "' " +  ascExtent.ToString() + " and clipExtent: '" + clipExtent.ToString());
            }

            if (clipExtent.Contains(ascExtent) && !isInvertedClip)
            {
                // No need to clip; return this IDF-file
                return this.Copy(Filename);
            }

            // Snap clip extent to extent and cellsize of source IDF-file, ensure corrected clipExtent is not smaller than original clipExtent
            float llxMismatch = (clipExtent.llx - ascExtent.llx) % Cellsize;
            float llyMismatch = (clipExtent.lly - ascExtent.lly) % Cellsize;
            float urxMismatch = (ascExtent.urx - clipExtent.urx) % Cellsize;
            float uryMismatch = (ascExtent.ury - clipExtent.ury) % Cellsize;
            float llxCorr = clipExtent.llx - llxMismatch;
            float llyCorr = clipExtent.lly - llyMismatch;
            float urxCorr = clipExtent.urx + urxMismatch;
            float uryCorr = clipExtent.ury + uryMismatch;
            if (urxCorr < clipExtent.urx)
            {
                urxCorr += Cellsize;
            }
            if (uryCorr < clipExtent.ury)
            {
                uryCorr += Cellsize;
            }
            clipExtent = new Extent(llxCorr, llyCorr, urxCorr, uryCorr);

            // Clip the extent
            Extent clippedExtent = ascExtent.Clip(clipExtent);

            // Initialize clipped result IDFFile
            ASCFile clippedASCFile = new ASCFile();
            clippedASCFile.Filename = Filename;
            clippedASCFile.Cellsize = Cellsize;
            clippedASCFile.NoDataValue = NoDataValue;
            clippedASCFile.LastWriteTime = LastWriteTime;

            if (ascExtent == null)
            {
                throw new Exception("No extent is defined for the base file. Clip is not possible for: " + this.Filename);
            }

            // Calculate new number of columns and number of rows
            invRow = (int)((clippedExtent.ury - ascExtent.lly - (Cellsize / 10)) / Cellsize); // don't include upper row if clipboundary is exactly at cell border
            upperRowIdx = NRows - invRow - 1;
            invRow = (int)((clippedExtent.lly - ascExtent.lly) / Cellsize);
            lowerRowIdx = NRows - invRow - 1;
            leftColIdx = (int)((clippedExtent.llx - ascExtent.llx) / Cellsize);
            rightColIdx = (int)(((clippedExtent.urx - ascExtent.llx - (Cellsize / 10)) / Cellsize));  // don't include right column if clipboundary is exactly at cell border

            clippedASCFile.NCols = isInvertedClip ? NCols : (rightColIdx - leftColIdx + 1);
            clippedASCFile.NRows = isInvertedClip ? NRows : (lowerRowIdx - upperRowIdx + 1);
            clippedASCFile.XLL = isInvertedClip ? ascExtent.llx : clippedExtent.llx;
            clippedASCFile.YLL = isInvertedClip ? ascExtent.lly : clippedExtent.lly;

            if (this.Values != null)
            {
                clippedASCFile.DeclareValuesMemory();

                if (isInvertedClip)
                {
                    // Copy values from source file
                    for (int rowIdx = 0; rowIdx < clippedASCFile.NRows; rowIdx++)
                    {
                        for (int colIdx = 0; colIdx < clippedASCFile.NCols; colIdx++)
                        {
                            clippedASCFile.Values[rowIdx][colIdx] = Values[rowIdx][colIdx];
                        }
                    }

                    // Remove values inside rectangle: set to NoData
                    for (int rowIdx = upperRowIdx; rowIdx <= lowerRowIdx; rowIdx++)
                    {
                        for (int colIdx = leftColIdx; colIdx <= rightColIdx; colIdx++)
                        {
                            clippedASCFile.Values[rowIdx][colIdx] = NoDataValue;
                        }
                    }
                }
                else
                {
                    // copy clipped values and recalculate min/max-values
                    float outputValue;
                    for (int rowIdx = 0; rowIdx < clippedASCFile.NRows; rowIdx++)
                    {
                        for (int colIdx = 0; colIdx < clippedASCFile.NCols; colIdx++)
                        {
                            outputValue = float.NaN;
                            if (((upperRowIdx + rowIdx) > Values.Length) || ((leftColIdx + colIdx) > Values[Values.Length - 1].Length) || (upperRowIdx < 0) || (leftColIdx < 0))
                            {
                                // this should actually never happen...
                                outputValue = float.NaN;
                            }
                            else
                            {
                                outputValue = Values[upperRowIdx + rowIdx][leftColIdx + colIdx];
                            }
                            clippedASCFile.Values[rowIdx][colIdx] = outputValue;
                        }
                    }
                }
            }

            return clippedASCFile;
        }

        /// <summary>
        /// Declare memory for values of this ASC-file object based on defined number of rows and columns. Cells will get default float value (0).
        /// </summary>
        public void DeclareValuesMemory()
        {
            // declare memory for values
            if (NRows > 0)
            {
                Values = new float[NRows][];
                for (int i = 0; i < NRows; i++)
                {
                    Values[i] = new float[NCols];
                }
            }
            else if ((NRows == 0) && (NCols == 0))
            {
                Values = new float[0][];
            }
            else
            {
                throw new Exception("Invalid rowcount: " + NRows);
            }
        }

        /// <summary>
        /// Copy properties and values to new ASCFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public ASCFile Copy(string filename)
        {
            ASCFile newASCFile = new ASCFile();
            newASCFile.Filename = filename;
            newASCFile.XLL = XLL;
            newASCFile.YLL = YLL;
            newASCFile.Cellsize = Cellsize;
            newASCFile.NoDataValue = NoDataValue;
            newASCFile.NCols = NCols;
            newASCFile.NRows = NRows;
            newASCFile.LastWriteTime = LastWriteTime;
            newASCFile.DeclareValuesMemory();

            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    newASCFile.Values[rowIdx][colIdx] = Values[rowIdx][colIdx];
                }
            }

            return newASCFile;
        }

        ///// <summary>
        ///// Clip ASC-file data to specified extent
        ///// </summary>
        ///// <param name="clipRectangle"></param>
        ///// <param name="isInvertedClip">if true, part outside extent is retained</param>
        ///// <returns></returns>
        //public ASCFile ClipDeprecated(Extent clipRectangle, bool isInvertedClip = false)
        //{
        //    float xllClipRectangle = clipRectangle.llx;
        //    float yllClipRectangle = clipRectangle.lly;
        //    float xurClipRectangle = clipRectangle.urx;
        //    float yurClipRectangle = clipRectangle.ury;

        //    ASCFile outputASCFile = new ASCFile();
        //    outputASCFile.Filename = Filename;
        //    outputASCFile.LastWriteTime = LastWriteTime;

        //    if (xllClipRectangle < XLL)
        //    {
        //        xllClipRectangle = XLL;
        //    }
        //    if (yllClipRectangle < YLL)
        //    {
        //        yllClipRectangle = YLL;
        //    }
        //    float xurcorner = XLL + Cellsize * NCols;
        //    if (xurClipRectangle > xurcorner)
        //    {
        //        xurClipRectangle = xurcorner;
        //    }
        //    float yurcorner = YLL + Cellsize * NRows;
        //    if (yurClipRectangle > yurcorner)
        //    {
        //        yurClipRectangle = yurcorner;
        //    }
        //    if (yllClipRectangle > yurClipRectangle)
        //    {
        //        yllClipRectangle = yurClipRectangle;
        //    }
        //    if (xllClipRectangle > xurClipRectangle)
        //    {
        //        xllClipRectangle = xurClipRectangle;
        //    }
        //    int invRow = (int)((yurClipRectangle - YLL - 1) / Cellsize); // don't include upper row if clipboundary is exactly at cell border
        //    int upperRowIdx = NRows - invRow - 1;
        //    invRow = (int)((yllClipRectangle - YLL) / Cellsize);
        //    int lowerRowIdx = NRows - invRow - 1;
        //    int leftColIdx = (int)((xllClipRectangle - XLL) / Cellsize);
        //    int rightColIdx = (int)(((xurClipRectangle - XLL - 1) / Cellsize));  // don't include right column if clipboundary is exactly at cell border

        //    // initialize output ASCFile
        //    outputASCFile.Cellsize = Cellsize;
        //    outputASCFile.NoDataValue = NoDataValue;
        //    outputASCFile.NCols = isInvertedClip ? NCols : (rightColIdx - leftColIdx + 1);
        //    outputASCFile.NRows = isInvertedClip ? NRows : (lowerRowIdx - upperRowIdx + 1);
        //    outputASCFile.XLL = isInvertedClip ? XLL : (float)xllClipRectangle;
        //    outputASCFile.YLL = isInvertedClip ? YLL : (float)yllClipRectangle;
        //    outputASCFile.Values = new float[outputASCFile.NRows][];

        //    // Declare memory for values in output file
        //    for (int i = 0; i < outputASCFile.NRows; i++)
        //    {
        //        outputASCFile.Values[i] = new float[outputASCFile.NCols];
        //    }

        //    if (isInvertedClip)
        //    {
        //        // Copy values from source file
        //        int rowIdx;
        //        for (rowIdx = 0; rowIdx < outputASCFile.NRows; rowIdx++)
        //        {
        //            for (int colIdx = 0; colIdx < outputASCFile.NCols; colIdx++)
        //            {
        //                outputASCFile.Values[rowIdx][colIdx] = Values[rowIdx][colIdx];
        //            }
        //        }

        //        // Remove values inside rectangle: set to NoData
        //        for (rowIdx = upperRowIdx; rowIdx <= lowerRowIdx; rowIdx++)
        //        {
        //            for (int colIdx = leftColIdx; colIdx <= rightColIdx; colIdx++)
        //            {
        //                outputASCFile.Values[rowIdx][colIdx] = NoDataValue;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // copy clipped values
        //        for (int rowIdx = 0; rowIdx < outputASCFile.NRows; rowIdx++)
        //        {
        //            for (int colIdx = 0; colIdx < outputASCFile.NCols; colIdx++)
        //            {
        //                outputASCFile.Values[rowIdx][colIdx] = Values[upperRowIdx + rowIdx][leftColIdx + colIdx];
        //            }
        //        }
        //    }
        //    return outputASCFile;
        //}
    }
}
