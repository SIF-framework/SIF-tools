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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;

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
            StreamReader sr = null;

            ASCFile ascFile = new ASCFile();
            try
            {
                ascFile.Filename = filename;
                ascFile.LastWriteTime = File.GetLastWriteTime(filename);

                stream = File.OpenRead(filename); // File.Open(filename, FileMode.Open, FileAccess.Read)
                sr = new StreamReader(stream);

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
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
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
        /// <param name="clipRectangle"></param>
        /// <param name="isInvertedClip">if true, part outside extent is retained</param>
        /// <returns></returns>
        public ASCFile Clip(Extent clipRectangle, bool isInvertedClip = false)
        {

            float xllClipRectangle = clipRectangle.llx;
            float yllClipRectangle = clipRectangle.lly;
            float xurClipRectangle = clipRectangle.urx;
            float yurClipRectangle = clipRectangle.ury;

            ASCFile outputASCFile = new ASCFile();
            outputASCFile.Filename = Filename;
            outputASCFile.LastWriteTime = LastWriteTime;

            if (xllClipRectangle < XLL)
            {
                xllClipRectangle = XLL;
            }
            if (yllClipRectangle < YLL)
            {
                yllClipRectangle = YLL;
            }
            float xurcorner = XLL + Cellsize * NCols;
            if (xurClipRectangle > xurcorner)
            {
                xurClipRectangle = xurcorner;
            }
            float yurcorner = YLL + Cellsize * NRows;
            if (yurClipRectangle > yurcorner)
            {
                yurClipRectangle = yurcorner;
            }
            if (yllClipRectangle > yurClipRectangle)
            {
                yllClipRectangle = yurClipRectangle;
            }
            if (xllClipRectangle > xurClipRectangle)
            {
                xllClipRectangle = xurClipRectangle;
            }
            int invRow = (int)((yurClipRectangle - YLL - 1) / Cellsize); // don't include upper row if clipboundary is exactly at cell border
            int upperRowIdx = NRows - invRow - 1;
            invRow = (int)((yllClipRectangle - YLL) / Cellsize);
            int lowerRowIdx = NRows - invRow - 1;
            int leftColIdx = (int)((xllClipRectangle - XLL) / Cellsize);
            int rightColIdx = (int)(((xurClipRectangle - XLL - 1) / Cellsize));  // don't include right column if clipboundary is exactly at cell border

            // initialize output ASCFile
            outputASCFile.Cellsize = Cellsize;
            outputASCFile.NoDataValue = NoDataValue;
            outputASCFile.NCols = isInvertedClip ? NCols : (rightColIdx - leftColIdx + 1);
            outputASCFile.NRows = isInvertedClip ? NRows : (lowerRowIdx - upperRowIdx + 1);
            outputASCFile.XLL = isInvertedClip ? XLL : (float)xllClipRectangle;
            outputASCFile.YLL = isInvertedClip ? YLL : (float)yllClipRectangle;
            outputASCFile.Values = new float[outputASCFile.NRows][];

            // Declare memory for values in output file
            for (int i = 0; i < outputASCFile.NRows; i++)
            {
                outputASCFile.Values[i] = new float[outputASCFile.NCols];
            }

            if (isInvertedClip)
            {
                // Copy values from source file
                int rowIdx;
                for (rowIdx = 0; rowIdx < outputASCFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < outputASCFile.NCols; colIdx++)
                    {
                        outputASCFile.Values[rowIdx][colIdx] = Values[rowIdx][colIdx];
                    }
                }

                // Remove values inside rectangle: set to NoData
                for (rowIdx = upperRowIdx; rowIdx <= lowerRowIdx; rowIdx++)
                {
                    for (int colIdx = leftColIdx; colIdx <= rightColIdx; colIdx++)
                    {
                        outputASCFile.Values[rowIdx][colIdx] = NoDataValue;
                    }
                }
            }
            else
            {
                // copy clipped values
                for (int rowIdx = 0; rowIdx < outputASCFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < outputASCFile.NCols; colIdx++)
                    {
                        outputASCFile.Values[rowIdx][colIdx] = Values[upperRowIdx + rowIdx][leftColIdx + colIdx];
                    }
                }
            }
            return outputASCFile;
        }
    }
}
