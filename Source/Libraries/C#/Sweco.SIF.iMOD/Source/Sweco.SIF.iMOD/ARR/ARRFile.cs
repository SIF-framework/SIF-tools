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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.iMOD.ARR
{
    /// <summary>
    /// Class to read, modify and write ARR-files.
    /// </summary>
    public class ARRFile
    {
        private static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// File extension of ARR-file without dot-prefix
        /// </summary>
        public static string Extension
        {
            get { return "ARR"; }
        }

        /// <summary>
        /// ARR-file data values in a one dimensional array
        /// </summary>
        public float[] Values { get; set; }

        /// <summary>
        /// Number of columns in ARR-file
        /// </summary>
        public int NCols { get; set; }

        /// <summary>
        /// Number of rows in ARR-file
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
        /// X-coordinate of upper right corner
        /// </summary>
        public float XUR { get; set; }

        /// <summary>
        /// Y-coordinate of upper right corner
        /// </summary>
        public float YUR { get; set; }

        /// <summary>
        /// Cellsize in x-dimension
        /// </summary>
        public float XCellsize { get; set; }

        /// <summary>
        /// Cellsize in y-dimension
        /// </summary>
        public float YCellsize { get; set; }

        /// <summary>
        /// NoData-value for this ARR-file
        /// </summary>
        public float NoDataValue { get; set; }

        /// <summary>
        /// Filename of this ARR-fle
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Last time that this ARR-file was written to Filename
        /// </summary>
        private DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Create empty ARRFile objec
        /// </summary>
        public ARRFile()
        {
        }

        /// <summary>
        /// Create ARR-file from specified IDF-file object
        /// </summary>
        /// <param name="idfFile"></param>
        public ARRFile(IDFFile idfFile)
        {
            NoDataValue = idfFile.NoDataValue;
            NRows = idfFile.NRows;
            NCols = idfFile.NCols;
            XLL = idfFile.Extent.llx;
            YLL = idfFile.Extent.lly;
            XUR = idfFile.Extent.urx;
            YUR = idfFile.Extent.ury;
            XCellsize = idfFile.XCellsize;
            YCellsize = idfFile.YCellsize;
            Filename = (Filename != null) ? (Path.Combine(Path.GetDirectoryName(idfFile.Filename), Path.GetFileNameWithoutExtension(idfFile.Filename) + ".ARR")) : null;

            Values = new float[NRows * NCols];
            long valueIdx = 0;
            for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                {
                    Values[valueIdx++] = idfFile.values[rowIdx][colIdx];
                }
            }
        }

        /// <summary>
        /// Retrieve raster value at specified xy-coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetValue(float x, float y)
        {
            int invRow = (int)((y - YLL) / YCellsize);
            int row = (NRows - 1) - invRow;
            int col = (int)((x - XLL) / XCellsize);
            float value = 0;
            if ((row >= 0) && (col >= 0) && (row < NRows) && (col < NCols))
            {
                try
                {
                    value = Values[row * NRows + col];
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
        /// Check if specified filename has a valid ARR-extension
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasARRExtension(string filename)
        {
            return (Path.GetExtension(filename).ToLower().Equals(".arr"));
        }

        /// <summary>
        /// Read an ARR-file into memory
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ARRFile ReadFile(string filename)
        {
            Stream stream = null;
            StreamReader sr = null;

            ARRFile arrFile = new ARRFile();
            long lineIdx = 0;
            int colIdx = 0;
            int rowIdx = 0;
            int idx;
            int count;
            double value;
            string line = string.Empty;
            List<double> values = new List<double>();
            try
            {
                arrFile.Filename = filename;
                arrFile.LastWriteTime = File.GetLastWriteTime(filename);

                stream = File.OpenRead(filename);
                sr = new StreamReader(stream);

                // Read values
                line = sr.ReadLine().Trim();
                string[] lineValues = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (lineValues.Length > 1)
                {
                    // File has multicolumn format, which is not yet supported
                    throw new Exception("ARR-format with multicolumns is not yet supoported: " + filename);
                }
                else
                {
                    // File has single value format and contains all values, including NoData
                    while (!sr.EndOfStream && !line.Equals("DIMENSIONS") && !line.Equals("# DIMENSIONS"))
                    {
                        if (!line[0].Equals('#'))
                        {
                            lineIdx++;
                            idx = line.IndexOf('*');
                            count = 1;
                            if (idx > 0)
                            {
                                count = int.Parse(line.Substring(0, idx));
                                value = double.Parse(line.Substring(idx + 1, line.Length - idx - 1), englishCultureInfo);
                            }
                            else
                            {
                                value = double.Parse(line, englishCultureInfo);
                            }

                            for (int valueIdx = 0; valueIdx < count; valueIdx++)
                            {
                                values.Add(value);
                                colIdx++;
                                if (colIdx == arrFile.NCols)
                                {
                                    colIdx = 0;
                                    rowIdx++;
                                }
                            }
                        }
                        line = sr.ReadLine().Trim();
                    }
                }

                if (line[0].Equals('#'))
                {
                    line = line.Replace("#", string.Empty).Trim();
                }

                double dblNoDataValue = double.NaN;
                if (line.Equals("DIMENSIONS"))
                {
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.NCols = int.Parse(line);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.NRows = int.Parse(line);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.XLL = float.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.YLL = float.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.XUR = float.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.YUR = float.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    dblNoDataValue = double.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    // ignore value
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.XCellsize = float.Parse(line, englishCultureInfo);
                    line = sr.ReadLine().Replace("#", string.Empty).Trim();
                    arrFile.XCellsize = float.Parse(line, englishCultureInfo);
                    arrFile.YCellsize = float.Parse(line, englishCultureInfo);
                }
                else
                {
                    throw new ToolException("Invalid ARR-file, missing DIMENSIONS keyword");
                }

                float fltNoDataValue = -9999.0f;
                try
                {
                    fltNoDataValue = (float)dblNoDataValue;
                }
                catch (Exception)
                {
                    // ignore, leave to default value
                }
                arrFile.NoDataValue = fltNoDataValue;

                if (values.Count > 0)
                {
                    if ((arrFile.NRows * arrFile.NCols) != values.Count)
                    {
                        throw new ToolException("Defined number of rows and columns (" + arrFile.NRows + "x" + arrFile.NCols + ") is not equal to number of values (" + values + ")");
                    }

                    // Convert doubles to float
                    try
                    {
                        arrFile.Values = new float[arrFile.NRows * arrFile.NCols];
                        for (int valueIdx = 0; valueIdx < values.Count; valueIdx++)
                        {
                            if (values[valueIdx].Equals(dblNoDataValue))
                            {
                                arrFile.Values[valueIdx] = fltNoDataValue;
                            }
                            else
                            {
                                arrFile.Values[valueIdx] = (float)values[valueIdx];
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not convert ARR double values to IDF float values", ex);
                    }
                }
                else
                {
                    // Correct for empty ARR-file: set all values to NoData
                    arrFile.Values = new float[arrFile.NRows * arrFile.NCols];
                    for (int valueIdx = 0; valueIdx < arrFile.Values.Length; valueIdx++)
                    {
                        arrFile.Values[valueIdx] = fltNoDataValue;
                    }
                }
            }
            catch (Exception ex)
            {
                if (lineIdx > 0)
                {
                    throw new ToolException("Could not read ARR-file line " + lineIdx + ": " + "'" + line + "' of file: " + filename, ex);
                }
                else
                {
                    throw new ToolException("Could not read ARR-file: " + filename, ex);
                }
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return arrFile;
        }

        /// <summary>
        /// Write ARR-file data to file on disk with specified filename
        /// </summary>
        /// <param name="arrFilename"></param>
        /// <param name="copyLastWriteTime"></param>
        public void WriteFile(string arrFilename, bool copyLastWriteTime = false)
        {
            Filename = arrFilename;
            WriteARRFile(copyLastWriteTime);
        }

        /// <summary>
        /// Write ARR-file data to file on disk with filename as defined in this ARRFile object
        /// </summary>
        /// <param name="copyLastWriteTime">if true, the same write is set as when the ARR-file was read</param>
        public void WriteARRFile(bool copyLastWriteTime = false)
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
                    Filename += "." + Extension;
                }

                sw = new StreamWriter(Filename, false);

                if (Values.Length > 0)
                {
                    float currentValue = Values[0];
                    int currentValueCount = 1;
                    int colIdx = 0;
                    for (int valueIdx = 1; valueIdx < Values.Length; valueIdx++)
                    {
                        colIdx++;
                        float value = Values[valueIdx];
                        if (value.Equals(currentValue) && (colIdx < NCols))
                        {
                            currentValueCount++;
                        }
                        else
                        {
                            // Write previous value, and save current value as new current value
                            if (currentValueCount > 1)
                            {
                                sw.WriteLine(currentValueCount + "*" + currentValue.ToString("F5", englishCultureInfo));
                            }
                            else
                            {
                                sw.WriteLine("   " + currentValue.ToString("F5", englishCultureInfo));
                            }
                            currentValueCount = 1;
                            currentValue = value;
                        }
                        if (colIdx == NCols)
                        {
                            colIdx = 0;
                        }
                    }
                    // Write last value
                    if (currentValueCount > 1)
                    {
                        sw.WriteLine(currentValueCount + "*" + currentValue.ToString("E7", englishCultureInfo));
                    }
                    else
                    {
                        sw.WriteLine("   " + currentValue.ToString("E7", englishCultureInfo));
                    }
                }

                // Write Definitions
                sw.WriteLine(" DIMENSIONS");
                sw.WriteLine(NCols.ToString().PadLeft(12));
                sw.WriteLine(NRows.ToString().PadLeft(12));
                sw.WriteLine("   " + XLL.ToString("F1", englishCultureInfo));
                sw.WriteLine("   " + YLL.ToString("F1", englishCultureInfo));
                sw.WriteLine("   " + XUR.ToString("F1", englishCultureInfo));
                sw.WriteLine("   " + YUR.ToString("F1", englishCultureInfo));
                sw.WriteLine(" "+ NoDataValue.ToString("E7", englishCultureInfo).PadLeft(15));
                sw.WriteLine("0".PadLeft(12));
                sw.WriteLine(XCellsize.ToString("F4", englishCultureInfo).PadLeft(11));
                sw.WriteLine(YCellsize.ToString("F4", englishCultureInfo).PadLeft(11));
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
    }
}
