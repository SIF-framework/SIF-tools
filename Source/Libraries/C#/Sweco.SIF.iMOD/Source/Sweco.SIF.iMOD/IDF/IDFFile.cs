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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.ARR;
using Sweco.SIF.iMOD.ASC;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Available methods for upscaling IDF-files
    /// </summary>
    public enum UpscaleMethodEnum
    {
        Minimum,
        Mean,
        Median,
        Maximum,
        MostOccurring,
        /// <summary>
        /// Used for scaling boundary values (i.e. -1, 0, 1); equal to MostOccuring.
        /// </summary>
        Boundary,
        /// <summary>
        /// Sum values of fine cells inside coarse cell
        /// </summary>
        Sum
    }

    /// <summary>
    /// Available methods for downscaling IDF-files
    /// </summary>
    public enum DownscaleMethodEnum
    {
        /// <summary>
        /// Use value of course cell for all scaled cells
        /// </summary>
        Block,
        /// <summary>
        /// Divide value of coarse cell by number of fine cells within coarse cell
        /// </summary>
        Divide
    }

    /// <summary>
    /// Class to read, modify and write IDF-files. See iMOD-manual for details of IDF-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class IDFFile : IMODFile, IEquatable<IDFFile>
    {
        /// <summary>
        /// Length in bytes of IDF-header which can be used for direct access of cells in IDF-files
        /// </summary>
        private const long HeaderByteLength = 13 * 4;

        /// <summary>
        /// The default NoData-value that is used when no specific NoData-value is defined
        /// </summary>
        public const float NoDataDefaultValue = -9999.0f;

        /// <summary>
        /// File extension of an IDF-file without dot-prefix
        /// </summary>
        public override string Extension
        {
            get { return "IDF"; }
        }

        /// <summary>
        /// Lahey Record Length Identiﬁcation; 1271 is a single precision IDF, 2296 a double precision (iMOD 4.4 creates 2295).
        /// </summary>
        protected int idfRecordLength;

        /// <summary>
        /// Number of columns in IDF-file
        /// </summary>
        public int NCols { get; protected set; }

        /// <summary>
        /// Number of rows in IDF-file
        /// </summary>
        public int NRows { get; protected set; }

        /// <summary>
        /// Cellsize in x-dimension
        /// </summary>
        public float XCellsize { get; protected set; }

        /// <summary>
        /// Cellsize in y-dimension
        /// </summary>
        public float YCellsize { get; protected set; }

        /// <summary>
        /// TOP-level of IDF-file when displayed as a voxel (check ITB in iMOD-manual), float.NaN otherwise
        /// </summary>
        public float TOPLevel { get; protected set; }

        /// <summary>
        /// BOT-level of IDF-file when displayed as a voxel (check ITB in iMOD-manual), float.NaN otherwise
        /// </summary>
        public float BOTLevel { get; protected set; }

        /// <summary>
        /// For direct (faster) access to values (which may or may not be loaded yet) 
        /// dimensions: [rows][cols], starting from upperleft corner ([0][0])
        /// </summary>
        public float[][] values;
        /// <summary>
        /// For access to lazy loaded values ([rows][cols], [0][0] is upperleft corner)
        /// </summary>
        public virtual float[][] Values
        {
            get
            {
                if (values != null)
                {
                    return values;
                }
                else
                {
                    LoadValues();
                    return values;
                }
            }
            set { values = value; }
        }

        /// <summary>
        /// Log object to write logmessages for IDF-file processing to 
        /// </summary>
        public Log Log { get; set; }

        /// <summary>
        /// Indentation level for logmessages that are written to the Log object 
        /// </summary>
        public int LogIndentLevel { get; set; }

        /// <summary>
        /// Specifies that the actual values are only loaded at first access
        /// </summary>
        public override bool UseLazyLoading { get; set; }

        /// <summary>
        /// The value that is used for NoData in calculations. Use float.NaN (the default) to keep using NoData, which may give a NoData result. Set to the actual NoData-value to use NoData it as a value.
        /// </summary>
        public float NoDataCalculationValue { get; set; }

        /// <summary>
        /// Full filename of this IDF-file
        /// </summary>
        public override string Filename
        {
            get { return base.Filename; }
            set
            {
                base.Filename = value;
            }
        }

        /// <summary>
        /// BinaryReader object that is used for direct access of IDF-file. This is set by OpenFile()/CloseFile() methods
        /// </summary>
        private BinaryReader idfReader;

        /// <summary>
        /// Create empty IDFFile object
        /// </summary>
        public IDFFile() : base()
        {
            Initialize(string.Empty, null, 0, 0, 0, 0, NoDataDefaultValue);
        }

        /// <summary>
        /// Create IDF-file for specified extent, number of rows/columns, etc. Cellsize is derived from extent and number of row/columns and rounded to one decimal.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="nrows"></param>
        /// <param name="ncols"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public IDFFile(string filename, Extent extent, int nrows, int ncols, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0) : base()
        {
            this.XCellsize = (float)Math.Round((extent.urx - extent.llx + 1) / ncols, 1);
            this.YCellsize = (float)Math.Round((extent.ury - extent.lly + 1) / nrows, 1);
            Initialize(filename, extent, nrows, ncols, this.XCellsize, this.YCellsize, noDataValue, useLazyLoading, log, logIndentLevel);
        }

        /// <summary>
        /// Create IDF-file for specified extent, cellsize, etc. number of rows/columns is derived from extent and cellsize
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="csize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public IDFFile(string filename, Extent extent, float csize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0) : base()
        {
            Initialize(filename, extent, csize, csize, noDataValue, useLazyLoading, log, logIndentLevel);
        }

        /// <summary>
        /// Create IDF-file for specified extent, x-/y-cellsize, etc. number of rows/columns is derived from extent and x-/y-cellsize
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public IDFFile(string filename, Extent extent, float xCellsize, float yCellsize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0) : base()
        {
            Initialize(filename, extent, xCellsize, yCellsize, noDataValue, useLazyLoading, log, logIndentLevel);
        }

        /// <summary>
        /// Create IDF-file from specified ARR-file;
        /// </summary>
        /// <param name="arrFile"></param>
        public IDFFile(ARRFile arrFile)
        {
            string filename = Path.Combine(Path.GetDirectoryName(arrFile.Filename), Path.GetFileNameWithoutExtension(arrFile.Filename) + ".IDF");
            Extent extent = new Extent(arrFile.XLL, arrFile.YLL, arrFile.XUR, arrFile.YUR);
            Initialize(filename, extent, arrFile.NRows, arrFile.NCols, arrFile.XCellsize, arrFile.YCellsize, arrFile.NoDataValue);

            long valueIdx = 0;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    values[rowIdx][colIdx] = arrFile.Values[valueIdx++];
                }
            }
        }

        /// <summary>
        /// Create IDF-file from specified ASC-file;
        /// </summary>
        /// <param name="ascFile"></param>
        public IDFFile(ASCFile ascFile)
        {
            string filename = Path.Combine(Path.GetDirectoryName(ascFile.Filename), Path.GetFileNameWithoutExtension(ascFile.Filename) + ".IDF");
            Extent extent = new Extent(ascFile.XLL, ascFile.YLL, ascFile.XLL + ascFile.NCols * ascFile.Cellsize, ascFile.YLL + ascFile.NRows * ascFile.Cellsize);
            Initialize(filename, extent, ascFile.NRows, ascFile.NCols, ascFile.Cellsize, ascFile.Cellsize, ascFile.NoDataValue);

            UpdateMinMaxValue();

            values = ascFile.Values;
        }

        /// <summary>
        /// Retrieves the columnindex into the values-array for the given x-value. Cell x-coordinates range from left boundary up to, but not including right boundary of the cell.
        /// Note: higher x-coordinates will give a higher column index. 
        /// The x-coordinate of the left boundary of an IDF-file extent, will give column index 0.
        /// The x-coordinate of the right boundary of an IDF-file extent, will give a cell just outside (right of) the IDF-raster.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public int GetColIdx(float x)
        {
            return (int)Math.Floor((x - extent.llx) / XCellsize);
        }

        /// <summary>   
        /// Retrieves the rowindex into the values-array for the given y-value. Cell y-coordinates range from top boundary up to, but not including lower boundary of the cell.
        /// Note: higher y-coordinates will give a lower row index. 
        /// The y-coordinate of the top boundary of an IDF-file extent, will give row index 0.
        /// The y-coordinate of the lower boundary of an IDF-file extent, will give a cell just outside (below) the IDF-raster.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetRowIdx(float y)
        {
            return (int)Math.Floor(((extent.ury) - y) / YCellsize);
        }

        /// <summary>
        /// Retrieves the x-value for the given columnindex into the values-array 
        /// </summary>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public float GetX(int colIdx)
        {
            return extent.llx + (colIdx + 0.5f) * XCellsize;
        }

        /// <summary>
        /// Retrieves the y-value for the given rowindex into the values-array 
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <returns></returns>
        public float GetY(int rowIdx)
        {
            return extent.ury - (rowIdx + 0.5f) * YCellsize;
        }

        /// <summary>
        /// Retrieve gridvalue at given coordinate (x,y), or float.NaN when outside bounds
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public virtual float GetValue(float x, float y)
        {
            int row = GetRowIdx(y);
            int col = GetColIdx(x);
            if ((row >= 0) && (col >= 0) && (row < NRows) && (col < NCols))
            {
                return Values[row][col];
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Retrieves interpolated cell value using bilineair interpolation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>interpolated value, float.NaN if XY is outside the grid, or NoData if the XY-cell has NoData-value</returns>
        public float GetInterpolatedValue(float x, float y)
        {
            int rowIdx = GetRowIdx(y);
            int colIdx = GetColIdx(x);
            if ((rowIdx < 0) || (rowIdx >= NRows) || (colIdx < 0) || (colIdx >= NCols))
            {
                // If the cell that the XY-coordinates are in is outside the IDF-extent, then the result should always be float.NaN
                return float.NaN;
            }
            else
            {
                float celValue = Values[rowIdx][colIdx];
                if (celValue.Equals(NoDataValue))
                {
                    if (NoDataCalculationValue.Equals(float.NaN))
                    {
                        // If the cell that the XY-coordinates are in is NoData and no NoDataCalculationValue is defined, then the result should always be NoData
                        return NoDataValue;
                    }
                }
            }

            // use bilineair interpolation: https://en.wikipedia.org/wiki/Bilinear_interpolation
            // using notation from: http://supercomputingblog.com/graphics/coding-bilinear-interpolation/
            // R1 = ((x2 – x)/(x2 – x1))*Q11 + ((x – x1)/(x2 – x1))*Q21
            // R2 = ((x2 – x)/(x2 – x1))*Q12 + ((x – x1)/(x2 – x1))*Q22
            // P = ((y2 – y)/(y2 – y1))*R1 + ((y – y1)/(y2 – y1))*R2
            // Find four gridvalues around specified xy

            // First find xy-coordinates of cellcenter leftbelow of specified xy-coordinates, this is the cell that the XY-coordinates are in
            float x1 = XCellsize * ((int)((x + (XCellsize / 2f)) / XCellsize)) + (XCellsize / 2f);
            float y1 = YCellsize * ((int)((y + (YCellsize / 2f)) / YCellsize)) + (YCellsize / 2f);
            if (x1 > x)
            {
                x1 -= XCellsize;
            }
            if (y1 > y)
            {
                y1 -= YCellsize;
            }

            int row1 = GetRowIdx(y1);
            int col1 = GetColIdx(x1);

            // Now find other three cellcenters that surround specified xy-coordinates
            float y2 = y1 + YCellsize;
            float x2 = x1 + XCellsize;

            // Get row and column indices
            int row2 = row1 - 1;
            int col2 = col1 + 1;

            // Now retrieve values
            float q11;
            if ((row1 >= 0) && (col1 >= 0) && (row1 < NRows) && (col1 < NCols))
            {
                q11 = Values[row1][col1];
                if (q11.Equals(NoDataValue))
                {
                    q11 = NoDataCalculationValue;
                }
            }
            else
            {
                q11 = float.NaN;
            }
            float q12;
            if ((row2 >= 0) && (col1 >= 0) && (row2 < NRows) && (col1 < NCols))
            {
                q12 = Values[row2][col1];
                if (q12.Equals(NoDataValue))
                {
                    q12 = NoDataCalculationValue;
                }
            }
            else
            {
                q12 = float.NaN;
            }
            float q21;
            if ((row1 >= 0) && (col2 >= 0) && (row1 < NRows) && (col2 < NCols))
            {
                q21 = Values[row1][col2];
                if (q21.Equals(NoDataValue))
                {
                    q21 = NoDataCalculationValue;
                }
            }
            else
            {
                q21 = float.NaN;
            }
            float q22;
            if ((row2 >= 0) && (col2 >= 0) && (row2 < NRows) && (col2 < NCols))
            {
                q22 = Values[row2][col2];
                if (q22.Equals(NoDataValue))
                {
                    q22 = NoDataCalculationValue;
                }
            }
            else
            {
                q22 = float.NaN;
            }

            // Now perform bilinear interpolation (if any of these values equal float.NaN, the results would be float.NaN, so process these seperately)
            float r1;
            if (!q11.Equals(float.NaN) && !q21.Equals(float.NaN))
            {
                r1 = ((x2 - x) / (x2 - x1)) * q11 + ((x - x1) / (x2 - x1)) * q21;
            }
            else
            {
                r1 = (q11.Equals(float.NaN)) ? q21 : q11;
            }

            float r2 = float.NaN;
            if (!q12.Equals(float.NaN) && !q22.Equals(float.NaN))
            {
                r2 = ((x2 - x) / (x2 - x1)) * q12 + ((x - x1) / (x2 - x1)) * q22;
            }
            else
            {
                r2 = q12.Equals(float.NaN) ? q22 : q12;
            }
            float p;
            if (!r1.Equals(float.NaN) && !r2.Equals(float.NaN))
            {
                p = ((y2 - y) / (y2 - y1)) * r1 + ((y - y1) / (y2 - y1)) * r2;
            }
            else
            {
                p = r1.Equals(float.NaN) ? r2 : r1;
            }
            return p;
        }

        /// <summary>
        /// Retrieve a local grid with dimensions (2 * cellDistance + 1) * (2 * cellDistance + 1)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cellDistance">max distance from given cell (i.e. use 1 for 3x3-grid)</param>
        /// <param name="precision">when >= 0 specifies precision for rounding, when less than 0 no rounding is done</param>
        /// <returns></returns>
        public float[][] GetCellValues(float x, float y, int cellDistance, int precision = -1)
        {
            int rowIdx = GetRowIdx(y);
            int colIdx = GetColIdx(x);

            return GetCellValues(rowIdx, colIdx, cellDistance, precision);
        }

        /// <summary>
        /// Retrieve a local grid with dimensions (2 * cellDistance + 1) * (2 * cellDistance + 1)
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="cellDistance">max distance from given cell (i.e. use 1 for 3x3-grid)</param>
        /// <param name="precision">when >= 0 specifies precision for rounding, when less than 0 no rounding is done</param>
        /// <returns></returns>
        public float[][] GetCellValues(int rowIdx, int colIdx, int cellDistance, int precision = -1)
        {
            float[][] cellValues = null;

            int gridSize = 2 * cellDistance + 1;
            cellValues = new float[gridSize][];
            for (int rowSubidx = 0; rowSubidx < gridSize; rowSubidx++)
            {
                cellValues[rowSubidx] = new float[gridSize];
                for (int colSubidx = 0; colSubidx < gridSize; colSubidx++)
                {
                    if (((rowIdx + rowSubidx - cellDistance) >= 0) && ((rowIdx + rowSubidx - cellDistance) < NRows) && ((colIdx + colSubidx - cellDistance )>= 0) && ((colIdx + colSubidx - cellDistance) < NCols))
                    {
                        if (precision >= 0)
                        {
                            cellValues[rowSubidx][colSubidx] = (float)Math.Round(values[rowIdx + rowSubidx - cellDistance][colIdx + colSubidx - cellDistance], precision);
                        }
                        else
                        {
                            cellValues[rowSubidx][colSubidx] = values[rowIdx + rowSubidx - cellDistance][colIdx + colSubidx - cellDistance];
                        }
                    }
                    else
                    {
                        cellValues[rowSubidx][colSubidx] = float.NaN;
                    }
                }
            }
            return cellValues;
        }

        /// <summary>
        /// Retrieve gridvalue at given coordinate (x,y), or float.NaN if nodata or when outside bounds
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public virtual float GetNaNBasedValue(float x, float y)
        {
            int row = GetRowIdx(y);
            int col = GetColIdx(x);
            if ((row >= 0) && (col >= 0) && (row < NRows) && (col < NCols))
            {
                float value = Values[row][col];
                return (value.Equals(NoDataValue)) ? float.NaN : value;
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Set cell value for specified x- and y-coordinate. Note: Min/Max-value is not updated!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public virtual void SetValue(float x, float y, float value)
        {
            Values[GetRowIdx(y)][GetColIdx(x)] = value;
        }

        /// <summary>
        /// Adds specified value to the current cellvalue. When the current value is NoData it is replaced with zero before adding. Note: Min/Max-value is not updated!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public virtual void AddValue(float x, float y, float value)
        {
            int rowIdx = GetRowIdx(y);
            int colIdx = GetColIdx(x);

            if (Values[rowIdx][colIdx].Equals(NoDataValue))
            {
                values[rowIdx][colIdx] = 0;
            }

            values[rowIdx][colIdx] += value;
        }

        /// <summary>
        /// Set all cell values of this IDF-file to specified value
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetValues(float value)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    values[rowIdx][colIdx] = value;
                }
            }
            MinValue = value;
            MaxValue = value;
        }

        /// <summary>
        /// Sets the values for all cells to the values of newvValueIDF
        /// </summary>
        /// <param name="newValueIDF"></param>
        public void SetValues(IDFFile newValueIDF)
        {
            if (!Extent.Equals(newValueIDF.Extent))
            {
                throw new Exception("newValueIDF (" + newValueIDF.Extent.ToString() + ") should have equal extent as reset IDF (" + Extent.ToString() + ")");
            }
            if (!XCellsize.Equals(newValueIDF.XCellsize))
            {
                throw new Exception("newValueIDF (" + newValueIDF.XCellsize.ToString() + ") should have equal cellsize as reset IDF (" + XCellsize.ToString() + ")");
            }

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                Values[rowIdx][colIdx] = newValueIDF.Values[rowIdx][colIdx];
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }
            UpdateMinMaxValue();
        }

        /// <summary>
        /// Resets all values to NoData
        /// </summary>
        public override void ResetValues()
        {
            if (values == null)
            {
                DeclareValuesMemory();
            }
            SetValues(NoDataValue);
        }

        /// <summary>
        /// Replaces cellvalues above the specified maximum value by this maximum value. NoData values are skipped.
        /// </summary>
        /// <param name="value"></param>
        public void SetMaxValue(float value)
        {
            if (value.Equals(NoDataValue))
            {
                return;
            }

            // Force (lazy) load of values
            EnsureLoadedValues();

            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if (!values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        // Select value if smaller than cell value
                        if (value < values[rowIdx][colIdx])
                        {
                            values[rowIdx][colIdx] = value;
                        }
                    }
                }
            }

            if (value < MinValue)
            {
                MinValue = value;
            }
            if (value < MaxValue)
            {
                MaxValue = value;
            }
        }

        /// <summary>
        /// Replaces cellvalues below the specified maximum value by this maximum value. NoData values are skipped.
        /// </summary>
        /// <param name="value"></param>
        public void SetMinValue(float value)
        {
            if (value.Equals(NoDataValue))
            {
                return;
            }

            // Force (lazy) load of values
            EnsureLoadedValues();

            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if (!values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        // Select value if larger than cell value
                        if (value > values[rowIdx][colIdx])
                        {
                            values[rowIdx][colIdx] = value;
                        }
                    }
                }
            }

            if (value > MinValue)
            {
                MinValue = value;
            }
            if (value > MaxValue)
            {
                MaxValue = value;
            }
        }

        /// <summary>
        /// Checks if the given cell has a minimum or maximum value in the specified local grid around it with dimensions (2 * cellDistance + 1) * (2 * cellDistance + 1)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cellDistance"></param>
        /// <param name="tolerance">minimum difference between min and max value within local grid to report value in given cell as a minmax value</param>
        /// <returns>true if it is the highest or the lowest value</returns>
        public bool IsMinMaxValue(float x, float y, int cellDistance, float tolerance = 0)
        {
            int xColIdx = GetColIdx(x);
            int yRowIdx = GetRowIdx(y);
            float xyValue = values[yRowIdx][xColIdx];
            float min = float.MaxValue;
            float max = float.MinValue;
            int rowIdx = Math.Max(yRowIdx - cellDistance, 0);
            int colIdx = Math.Max(xColIdx - cellDistance, 0);
            int gridSize = 2 * cellDistance + 1;
            for (int rowSubidx = 0; rowSubidx < gridSize; rowSubidx++)
            {
                for (int colSubidx = 0; colSubidx < gridSize; colSubidx++)
                {
                    if ((rowIdx + rowSubidx < NRows) && (colIdx + colSubidx < NCols))
                    {
                        float value = values[rowIdx + rowSubidx][colIdx + colSubidx];
                        if (value < min)
                        {
                            min = value;
                        }
                        if (value > max)
                        {
                            max = value;
                        }
                    }
                }
            }

            return (xyValue.Equals(min) || xyValue.Equals(max)) && ((max - min) > tolerance);
        }

        /// <summary>
        /// Adds values from idfFile1 and idfFile2. 
        /// NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public static IDFFile operator +(IDFFile idfFile1, IDFFile idfFile2)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                if (!((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile1.NoDataValue) && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile2.NoDataValue))
                {
                    float resultValue = ((ConstantIDFFile)idfFile1).ConstantValue + ((ConstantIDFFile)idfFile2).ConstantValue;
                    return new ConstantIDFFile(resultValue);
                }
                else
                {
                    return new ConstantIDFFile(idfFile1.NoDataValue);
                }
            }
            if (idfFile1 is ConstantIDFFile)
            {
                return idfFile2 + ((ConstantIDFFile)idfFile1).ConstantValue;
            }
            if (idfFile2 is ConstantIDFFile)
            {
                return idfFile1 + ((ConstantIDFFile)idfFile2).ConstantValue;
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile1.NoDataValue;
            float noDataValue2 = idfFile2.NoDataValue;
            float noDataCalculationValue1 = idfFile1.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile2.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (idfFile1.Extent.Equals(idfFile2.Extent) && idfFile1.XCellsize.Equals(idfFile2.XCellsize) && idfFile1.YCellsize.Equals(idfFile2.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "Plus", idfFile2), idfFile1.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile1.Values;
                float[][] values2 = idfFile2.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultValues[rowIdx][colIdx] = value1 + value2;
                        }
                        else
                        {
                            resultValues[rowIdx][colIdx] = resultNoDataValue;
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);
                cellIterator.AddIDFFile(idfFile1);
                cellIterator.AddIDFFile(idfFile2);

                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "+", idfFile2), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(idfFile1);
                        value2 = cellIterator.GetCellValue(idfFile2);

                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, value1 + value2);
                        }
                        else
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, resultNoDataValue);
                        }

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing + operator", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(idfFile1, "+", idfFile2);
                resultIDFFile.Legend = CreateLegend(idfFile1, idfFile2);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Subtracts values from idfFile2 from idfFile1. 
        /// NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public static IDFFile operator -(IDFFile idfFile1, IDFFile idfFile2)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                if (!((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile1.NoDataValue) && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile2.NoDataValue))
                {
                    float resultValue = ((ConstantIDFFile)idfFile1).ConstantValue - ((ConstantIDFFile)idfFile2).ConstantValue;
                    return new ConstantIDFFile(resultValue);
                }
                else
                {
                    return new ConstantIDFFile(idfFile1.NoDataValue);
                }
            }
            if (idfFile1 is ConstantIDFFile)
            {
                return Transform(idfFile2, -1, ((ConstantIDFFile)idfFile1).ConstantValue);
            }
            if (idfFile2 is ConstantIDFFile)
            {
                return idfFile1 - ((ConstantIDFFile)idfFile2).ConstantValue;
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile1.NoDataValue;
            float noDataValue2 = idfFile2.NoDataValue;
            float noDataCalculationValue1 = (idfFile1.NoDataCalculationValue.Equals(0)) ? 0 : idfFile1.NoDataCalculationValue;
            float noDataCalculationValue2 = (idfFile2.NoDataCalculationValue.Equals(0)) ? 0 : idfFile2.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (idfFile1.Extent.Equals(idfFile2.Extent) && idfFile1.XCellsize.Equals(idfFile2.XCellsize) && idfFile1.YCellsize.Equals(idfFile2.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "Min", idfFile2), idfFile1.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile1.Values;
                float[][] values2 = idfFile2.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultValues[rowIdx][colIdx] = value1 - value2;
                        }
                        else
                        {
                            resultValues[rowIdx][colIdx] = resultNoDataValue;
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);
                cellIterator.AddIDFFile(idfFile1);
                cellIterator.AddIDFFile(idfFile2);

                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "-", idfFile2), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(idfFile1);
                        value2 = cellIterator.GetCellValue(idfFile2);

                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, value1 - value2);
                        }
                        else
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, resultNoDataValue);
                        }

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing - operator", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(idfFile1, "-", idfFile2);
                resultIDFFile.Legend = CreateLegend(idfFile1, idfFile2);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks equality of cells for this IDF-file and another IDF-file, each cell will result in a 1 (true, equal) or 0 (false, unequal). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsEqual(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = ((ConstantIDFFile)this).ConstantValue.Equals(((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (this is ConstantIDFFile)
            {
                ConstantIDFFile constIDFFile = (ConstantIDFFile)this;
                if (constIDFFile.ConstantValue.Equals(constIDFFile.NoDataValue))
                {
                    // Use NoData-value of other IDF-file
                    return idfFile.IsEqual(idfFile.NoDataValue);
                }
                else
                {
                    return idfFile.IsEqual(((ConstantIDFFile)this).ConstantValue);
                }
            }
            if (idfFile is ConstantIDFFile)
            {
                ConstantIDFFile constIDFFile = (ConstantIDFFile)idfFile;
                if (constIDFFile.ConstantValue.Equals(constIDFFile.NoDataValue))
                {
                    // Use NoData-value of this IDF-file
                    return this.IsEqual(this.NoDataValue);
                }
                else
                {
                    return this.IsEqual(((ConstantIDFFile)idfFile).ConstantValue);
                }
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "EQ", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (value1.Equals(value2)) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "EQ", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (value1.Equals(value2)) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsEqual", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "EQ", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks inequality of cells for this IDF-file and another IDF-file, each cell will result in a 1 (true, unequal) or 0 (false, equal). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsNotEqual(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = !((ConstantIDFFile)this).ConstantValue.Equals(((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (this is ConstantIDFFile)
            {
                ConstantIDFFile constIDFFile = (ConstantIDFFile)this;
                if (constIDFFile.ConstantValue.Equals(constIDFFile.NoDataValue))
                {
                    // Use NoData-value of other IDF-file
                    return idfFile.IsNotEqual(idfFile.NoDataValue);
                }
                else
                {
                    return idfFile.IsNotEqual(((ConstantIDFFile)this).ConstantValue);
                }
            }
            if (idfFile is ConstantIDFFile)
            {
                ConstantIDFFile constIDFFile = (ConstantIDFFile)idfFile;
                if (constIDFFile.ConstantValue.Equals(constIDFFile.NoDataValue))
                {
                    // Use NoData-value of this IDF-file
                    return this.IsNotEqual(this.NoDataValue);
                }
                else
                {
                    return this.IsNotEqual(((ConstantIDFFile)idfFile).ConstantValue);
                }
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "UNEQ", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (!value1.Equals(value2)) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "UNEQ", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (!value1.Equals(value2)) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsNotEqual", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "UNEQ", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks if cells of this IDF-file are greater than cells of another IDF-file, each cell will result in a 1 (true) or 0 (false). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsGreater(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = (((ConstantIDFFile)this).ConstantValue > ((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (idfFile is ConstantIDFFile)
            {
                return this.IsGreater(((ConstantIDFFile)idfFile).ConstantValue);
            }
            if (this is ConstantIDFFile)
            {
                return idfFile.IsLesserEqual(((ConstantIDFFile)this).ConstantValue);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "GT", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (value1 > value2) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "GT", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (value1 > value2) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsGreater", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "GT" , idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks if cells of this IDF-file are greater than or equal cells of another IDF-file, each cell will result in a 1 (true) or 0 (false). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsGreaterEqual(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = (((ConstantIDFFile)this).ConstantValue >= ((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (idfFile is ConstantIDFFile)
            {
                return this.IsGreaterEqual(((ConstantIDFFile)idfFile).ConstantValue);
            }
            if (this is ConstantIDFFile)
            {
                return idfFile.IsLesser(((ConstantIDFFile)this).ConstantValue);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "GTE", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (value1 >= value2) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "GTE", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (value1 >= value2) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsGreaterEqual", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "GTE", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks if cells of this IDF-file are lesser than cells of another IDF-file, each cell will result in a 1 (true) or 0 (false). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsLesser(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = (((ConstantIDFFile)this).ConstantValue < ((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (idfFile is ConstantIDFFile)
            {
                return this.IsLesser(((ConstantIDFFile)idfFile).ConstantValue);
            }
            if (this is ConstantIDFFile)
            {
                return idfFile.IsGreaterEqual(((ConstantIDFFile)this).ConstantValue);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "LT", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (value1 < value2) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "LT", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (value1 < value2) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsLesser", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "LT", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks if cells of this IDF-file are lesser than or equal cells of another IDF-file, each cell will result in a 1 (true) or 0 (false). 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        public IDFFile IsLesserEqual(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = (((ConstantIDFFile)this).ConstantValue <= ((ConstantIDFFile)idfFile).ConstantValue) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (idfFile is ConstantIDFFile)
            {
                return this.IsLesserEqual(((ConstantIDFFile)idfFile).ConstantValue);
            }
            if (this is ConstantIDFFile)
            {
                return idfFile.IsGreater(((ConstantIDFFile)this).ConstantValue);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "LTE", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = (value1 <= value2) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "LTE", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, (value1 <= value2) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing IsLesserEqual", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "LTE", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// The then-else part of an IfThenElse expression. If cellvalues in this IDF-file are 1 the 'Then IDF-file' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="thenIDFFile">IDF-file to use if this cellvalue is 1</param>
        /// <param name="elseIDFFile">IDF-file to use if this cellvalue is not 1</param>
        /// <returns></returns>
        public IDFFile ThenElse(IDFFile thenIDFFile, IDFFile elseIDFFile)
        {
            return IfThenElse(this, thenIDFFile, elseIDFFile);
        }

        /// <summary>
        /// The then-else part of an IfThenElse expression. If cellvalues in this IDF-file are 1 the 'Then float value' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="thenValue"></param>
        /// <param name="elseIDFFile"></param>
        /// <returns></returns>
        public IDFFile ThenElse(float thenValue, IDFFile elseIDFFile)
        {
            return IfThenElse(this, new ConstantIDFFile(thenValue), elseIDFFile);
        }

        /// <summary>
        /// The then-else part of an IfThenElse expression. If cellvalues in this IDF-file are 1 the 'Then IDF-file' is used, otherwise the 'Else float value'
        /// </summary>
        /// <param name="thenIDFFile"></param>
        /// <param name="elseValue"></param>
        /// <returns></returns>
        public IDFFile ThenElse(IDFFile thenIDFFile, float elseValue)
        {
            return IfThenElse(this, thenIDFFile, new ConstantIDFFile(elseValue));
        }

        /// <summary>
        /// The then-else part of an IfThenElse expression. If cellvalues in this IDF-file are 1 the 'Then float value' is used, otherwise the 'Else float value'
        /// </summary>
        /// <param name="thenValue"></param>
        /// <param name="elseValue"></param>
        /// <returns></returns>
        public IDFFile ThenElse(float thenValue, float elseValue)
        {
            return IfThenElse(this, new ConstantIDFFile(thenValue), new ConstantIDFFile(elseValue));
        }

        /// <summary>
        /// If cellvalues in the 'Condition IDF-file' are 1 the 'Then IDF-file' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="conditionIDFFile">IDF-file with cell condition 1 for true or otherwise for false</param>
        /// <param name="thenValue">value to use if this cellvalue is 1</param>
        /// <param name="elseIDFFile">IDF-file to use if this cellvalue is not 1</param>
        /// <returns></returns>
        public static IDFFile IfThenElse(IDFFile conditionIDFFile, float thenValue, IDFFile elseIDFFile)
        {
            return IfThenElse(conditionIDFFile, new ConstantIDFFile(thenValue), elseIDFFile);
        }

        /// <summary>
        /// If cellvalues in the 'Condition IDF-file' are 1 the 'Then IDF-file' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="conditionIDFFile">IDF-file with cell condition 1 for true or otherwise for false</param>
        /// <param name="thenIDFFile">IDF-file to use if this cellvalue is 1</param>
        /// <param name="elseValue">value to use if this cellvalue is not 1</param>
        /// <returns></returns>
        public static IDFFile IfThenElse(IDFFile conditionIDFFile, IDFFile thenIDFFile, float elseValue)
        {
            return IfThenElse(conditionIDFFile, thenIDFFile, new ConstantIDFFile(elseValue));
        }

        /// <summary>
        /// If cellvalues in the 'Condition IDF-file' are 1 the 'Then IDF-file' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="conditionIDFFile">IDF-file with cell condition 1 for true or otherwise for false</param>
        /// <param name="thenValue">value to use if this cellvalue is 1</param>
        /// <param name="elseValue">value to use if this cellvalue is not 1</param>
        /// <returns></returns>
        public static IDFFile IfThenElse(IDFFile conditionIDFFile, float thenValue, float elseValue)
        {
            return IfThenElse(conditionIDFFile, new ConstantIDFFile(thenValue), new ConstantIDFFile(elseValue));
        }

        /// <summary>
        /// If cellvalues in the 'Condition IDF-file' are 1 the 'Then IDF-file' is used, otherwise the 'Else IDF-file'
        /// </summary>
        /// <param name="conditionIDFFile">IDF-file with cell condition 1 for true or otherwise for false</param>
        /// <param name="thenIDFFile">IDF-file to use if this cellvalue is 1</param>
        /// <param name="elseIDFFile">IDF-file to use if this cellvalue is not 1</param>
        /// <returns></returns>
        public static IDFFile IfThenElse(IDFFile conditionIDFFile, IDFFile thenIDFFile, IDFFile elseIDFFile)
        {
            if (thenIDFFile is ConstantIDFFile)
            {
                thenIDFFile = ((ConstantIDFFile)thenIDFFile).Allocate(conditionIDFFile);
            }
            if (elseIDFFile is ConstantIDFFile)
            {
                elseIDFFile = ((ConstantIDFFile)elseIDFFile).Allocate(conditionIDFFile);
            }

            /////////////////
            // Fix Extents //
            /////////////////

            // Enlarge IDF-files to union of IDF-files
            Extent expressionExtent = conditionIDFFile.Extent;
            if (!(conditionIDFFile is ConstantIDFFile) && !(thenIDFFile is ConstantIDFFile))
            {
                expressionExtent = conditionIDFFile.Extent.Union(elseIDFFile.Extent);
            }
            if (!(conditionIDFFile is ConstantIDFFile) && !(elseIDFFile is ConstantIDFFile))
            {
                expressionExtent = expressionExtent.Union(elseIDFFile.Extent);
            }

            if (!conditionIDFFile.Extent.Equals(expressionExtent))
            {
                if (!conditionIDFFile.Extent.Contains(expressionExtent))
                {
                    conditionIDFFile = conditionIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(conditionIDFFile.Extent))
                {
                    conditionIDFFile = conditionIDFFile.ClipIDF(expressionExtent);
                }
            }
            if (!thenIDFFile.Extent.Equals(expressionExtent))
            {
                if (!thenIDFFile.Extent.Contains(expressionExtent))
                {
                    thenIDFFile = thenIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(thenIDFFile.Extent))
                {
                    thenIDFFile = thenIDFFile.ClipIDF(expressionExtent);
                }
            }
            if (!elseIDFFile.Extent.Equals(expressionExtent))
            {
                if (!elseIDFFile.Extent.Contains(expressionExtent))
                {
                    elseIDFFile = elseIDFFile.EnlargeIDF(expressionExtent);
                }
                if (!expressionExtent.Contains(elseIDFFile.Extent))
                {
                    elseIDFFile = elseIDFFile.ClipIDF(expressionExtent);
                }
            }

            // Now evaluatie IF-expression
            IDFFile resultIDFFile = elseIDFFile.CopyIDF("IfThenElse.IDF");
            resultIDFFile.ReplaceValues(conditionIDFFile, 1, thenIDFFile);

            return resultIDFFile;
        }

        /// <summary>
        /// Calculates the logical AND between this and another IDF-file. Source cells values should be 1/0 for true/false values.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns>cells with 1/0 values for true/false</returns>
        public IDFFile LogicalAnd(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = ((((ConstantIDFFile)this).ConstantValue * ((ConstantIDFFile)idfFile).ConstantValue) > 0) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (this is ConstantIDFFile)
            {
                IDFFile tmpIDFFile = idfFile * ((ConstantIDFFile)this).ConstantValue;
                return tmpIDFFile.IsGreater(0);
            }
            if (idfFile is ConstantIDFFile)
            {
                IDFFile tmpIDFFile = this * ((ConstantIDFFile)idfFile).ConstantValue;
                return tmpIDFFile.IsGreater(0);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(Path.GetFileNameWithoutExtension(this.Filename) + "AND" + Path.GetFileNameWithoutExtension(idfFile.Filename) + ".IDF", this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = ((value1 > 0) && (value2 > 0)) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "AND", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, ((value1 > 0) && (value2 > 0)) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing Logical AND", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "AND", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Calculates the logical OR between this and another IDF-file. Source cells values should be 1/0 for true/false values.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <returns>cells with 1/0 values for true/false</returns>
        public IDFFile LogicalOr(IDFFile idfFile)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((this is ConstantIDFFile) && (idfFile is ConstantIDFFile))
            {
                float resultValue = ((((ConstantIDFFile)this).ConstantValue + ((ConstantIDFFile)idfFile).ConstantValue) > 0) ? 1 : 0;
                return new ConstantIDFFile(resultValue);
            }
            if (this is ConstantIDFFile)
            {
                IDFFile tmpIDFFile = idfFile + ((ConstantIDFFile)this).ConstantValue;
                return tmpIDFFile.IsGreater(0);
            }
            if (idfFile is ConstantIDFFile)
            {
                IDFFile tmpIDFFile = this + ((ConstantIDFFile)idfFile).ConstantValue;
                return tmpIDFFile.IsGreater(0);
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = this.NoDataValue;
            float noDataValue2 = idfFile.NoDataValue;
            float noDataCalculationValue1 = this.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (this.Extent.Equals(idfFile.Extent) && this.XCellsize.Equals(idfFile.XCellsize) && this.YCellsize.Equals(idfFile.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(this, "OR", idfFile), this.Extent, this.XCellsize, this.YCellsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = this.Values;
                float[][] values2 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < this.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < this.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultValues[rowIdx][colIdx] = ((value1 > 0) || (value2 > 0)) ? 1 : 0;
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(this);
                cellIterator.AddIDFFile(idfFile);

                resultIDFFile = new IDFFile(CreateFilename(this, "OR", idfFile), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, this.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(this);
                        value2 = cellIterator.GetCellValue(idfFile);

                        if (value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, ((value1 > 0) || (value2 > 0)) ? 1 : 0);

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing Logical OR", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(this, "OR", idfFile);
                resultIDFFile.Legend = CreateLegend(this, idfFile);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all values in this IDF if they are equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsEqual(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(CreateFilename(this, "EQ", testValue));

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;

            // For this test ensure NoDataCalculationValue is different from float.NaN when testValue equal NoData to avoid unexpected results
            float noDataCalculationValue = (testValue.Equals(noDataValue) && this.NoDataCalculationValue.Equals(float.NaN)) ? noDataValue : this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = value.Equals(testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all values in this IDF if they are not equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsNotEqual(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(CreateFilename(this, "UNEQ", testValue));

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;

            // For this test ensure NoDataCalculationValue is different from float.NaN when testValue equal NoData to avoid unexpected results
            float noDataCalculationValue = (testValue.Equals(noDataValue) && this.NoDataCalculationValue.Equals(float.NaN)) ? noDataValue : this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = !value.Equals(testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are greater than the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsGreater(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + "GT" + testValue.ToString("F3", EnglishCultureInfo) + ".IDF");

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = (value > testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are greater than or equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsGreaterEqual(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + "GTE" + testValue.ToString("F3", EnglishCultureInfo) + ".IDF");

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = (value >= testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are lesser than the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsLesser(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + "LT" + testValue.ToString("F3", EnglishCultureInfo) + ".IDF");

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = (value < testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are lesser than or equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsLesserEqual(float testValue)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + "LTE" + testValue.ToString("F3", EnglishCultureInfo) + ".IDF");

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    value = values[rowIdx][colIdx];
                    if (value.Equals(noDataValue))
                    {
                        value = noDataCalculationValue;
                    }

                    resultValues[rowIdx][colIdx] = (value <= testValue) ? 1 : 0;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are inside the specified range
        /// </summary>
        /// <param name="testValue1"></param>
        /// <param name="testValue2">value2 should be larger than value1</param>
        /// <param name="isIncludedVal1">true if value1 should be included in the condition</param>
        /// <param name="isIncludedVal2">true if value2 should be included in the condition</param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsBetween(float testValue1, float testValue2, bool isIncludedVal1 = true, bool isIncludedVal2 = true)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + testValue1.ToString("F3", EnglishCultureInfo) + "-" + testValue2.ToString("F3", EnglishCultureInfo) + ".IDF");

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            if (isIncludedVal1 && isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value >= testValue1) && (value <= testValue2)) ? 1 : 0;
                    }
                }
            }
            else if (!isIncludedVal1 && isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value > testValue1) && (value <= testValue2)) ? 1 : 0;
                    }
                }
            }
            else if (isIncludedVal1 && !isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value >= testValue1) && (value < testValue2)) ? 1 : 0;
                    }
                }
            }
            else
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value > testValue1) && (value < testValue2)) ? 1 : 0;
                    }
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they outside between the specified range
        /// </summary>
        /// <param name="testValue1"></param>
        /// <param name="testValue2">value2 should be larger than value1</param>
        /// <param name="isIncludedVal1">true if value1 should be included in the condition</param>
        /// <param name="isIncludedVal2">true if value2 should be included in the condition</param>
        /// <returns>1 if true, 0 if false</returns>
        public IDFFile IsNotBetween(float testValue1, float testValue2, bool isIncludedVal1 = true, bool isIncludedVal2 = true)
        {
            IDFFile resultIDFFile = this.CopyIDF(Path.GetFileNameWithoutExtension(this.Filename) + testValue1.ToString("F3", EnglishCultureInfo) + "-" + testValue2.ToString("F3", EnglishCultureInfo) + ".IDF");
            resultIDFFile.SetValues(0);

            // Force (lazy) load of values
            EnsureLoadedValues();

            float[][] resultValues = resultIDFFile.values;
            float noDataValue = this.NoDataValue;
            float noDataCalculationValue = this.NoDataCalculationValue;

            float value = float.NaN;
            if (!isIncludedVal1 && !isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value <= testValue1) || (value >= testValue2)) ? 1 : 0;
                    }
                }
            }
            else if (isIncludedVal1 && !isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value < testValue1) || (value >= testValue2)) ? 1 : 0;
                    }
                }
            }
            else if (!isIncludedVal1 && isIncludedVal2)
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value <= testValue1) || (value > testValue2)) ? 1 : 0;
                    }
                }
            }
            else
            {
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(noDataValue))
                        {
                            value = noDataCalculationValue;
                        }

                        resultValues[rowIdx][colIdx] = ((value < testValue1) || (value > testValue2)) ? 1 : 0;
                    }
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Raises values from idfFile1 to the power specified by the values in idfFile2. Note: power is rounded to an integer
        /// NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public static IDFFile operator ^(IDFFile idfFile1, IDFFile idfFile2)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                if (!((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile1.NoDataValue) && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile2.NoDataValue))
                {
                    float resultValue = (float)Math.Pow(((ConstantIDFFile)idfFile1).ConstantValue, ((ConstantIDFFile)idfFile2).ConstantValue);
                    return new ConstantIDFFile(resultValue);
                }
                else
                {
                    return new ConstantIDFFile(idfFile1.NoDataValue);
                }
            }
            if (idfFile2 is ConstantIDFFile)
            {
                return idfFile1 ^ ((ConstantIDFFile)idfFile2).ConstantValue;
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile1.NoDataValue;
            float noDataValue2 = idfFile2.NoDataValue;
            float noDataCalculationValue1 = idfFile1.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile2.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (idfFile1.Extent.Equals(idfFile2.Extent) && idfFile1.XCellsize.Equals(idfFile2.XCellsize) && idfFile1.YCellsize.Equals(idfFile2.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "POW", idfFile2), idfFile1.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile1.Values;
                float[][] values2 = idfFile2.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultValues[rowIdx][colIdx] = value1 * value2;
                        }
                        else
                        {
                            resultValues[rowIdx][colIdx] = resultNoDataValue;
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(idfFile1);
                cellIterator.AddIDFFile(idfFile2);

                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "POW", idfFile2), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(idfFile1);
                        value2 = cellIterator.GetCellValue(idfFile2);

                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, value1 * value2);
                        }
                        else
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, resultNoDataValue);
                        }

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing ^ operator", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(idfFile1, "POW", idfFile2);
                resultIDFFile.Legend = CreateLegend(idfFile1, idfFile2);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Multiplies values from idfFile1 with idfFile2. 
        /// NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public static IDFFile operator *(IDFFile idfFile1, IDFFile idfFile2)
        {
            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                if (!((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile1.NoDataValue) && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(idfFile2.NoDataValue))
                {
                    float resultValue = ((ConstantIDFFile)idfFile1).ConstantValue * ((ConstantIDFFile)idfFile2).ConstantValue;
                    return new ConstantIDFFile(resultValue);
                }
                else
                {
                    return new ConstantIDFFile(idfFile1.NoDataValue);
                }
            }
            if (idfFile1 is ConstantIDFFile)
            {
                return idfFile2 * ((ConstantIDFFile)idfFile1).ConstantValue;
            }
            if (idfFile2 is ConstantIDFFile)
            {
                return idfFile1 * ((ConstantIDFFile)idfFile2).ConstantValue;
            }

            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile1.NoDataValue;
            float noDataValue2 = idfFile2.NoDataValue;
            float noDataCalculationValue1 = idfFile1.NoDataCalculationValue;
            float noDataCalculationValue2 = idfFile2.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (idfFile1.Extent.Equals(idfFile2.Extent) && idfFile1.XCellsize.Equals(idfFile2.XCellsize) && idfFile1.YCellsize.Equals(idfFile2.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "MULT", idfFile2), idfFile1.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile1.Values;
                float[][] values2 = idfFile2.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultValues[rowIdx][colIdx] = value1 * value2;
                        }
                        else
                        {
                            resultValues[rowIdx][colIdx] = resultNoDataValue;
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);

                cellIterator.AddIDFFile(idfFile1);
                cellIterator.AddIDFFile(idfFile2);

                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "MULT", idfFile2), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(idfFile1);
                        value2 = cellIterator.GetCellValue(idfFile2);

                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN))
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, value1 * value2);
                        }
                        else
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, resultNoDataValue);
                        }

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing * operator", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(idfFile1, "MULT", idfFile2);
                resultIDFFile.Legend = CreateLegend(idfFile1, idfFile2);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Divides values from idfFile1 by idfFile2.
        /// NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// A division by zero results in NoData.
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public static IDFFile operator /(IDFFile idfFile1, IDFFile idfFile2)
        {
            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile1.NoDataValue;
            float noDataValue2 = idfFile2.NoDataValue;
            float noDataCalculationValue1 = (idfFile1.NoDataCalculationValue.Equals(0)) ? float.NaN : idfFile1.NoDataCalculationValue;
            float noDataCalculationValue2 = (idfFile2.NoDataCalculationValue.Equals(0)) ? float.NaN : idfFile2.NoDataCalculationValue;

            // Process special cases for subclas here, since this cannot be done properly from the subclass itself
            if ((idfFile1 is ConstantIDFFile) && (idfFile2 is ConstantIDFFile))
            {
                if (!((ConstantIDFFile)idfFile2).ConstantValue.Equals(0f) 
                    && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(noDataValue1) 
                    && !((ConstantIDFFile)idfFile2).ConstantValue.Equals(noDataValue2))
                {
                    float resultValue = ((ConstantIDFFile)idfFile1).ConstantValue / ((ConstantIDFFile)idfFile2).ConstantValue;
                    return new ConstantIDFFile(resultValue);
                }
                else
                {
                    return new ConstantIDFFile(noDataValue1);
                }
            }
            if (idfFile2 is ConstantIDFFile)
            {
                return idfFile1 / ((ConstantIDFFile)idfFile2).ConstantValue;
            }
            if (idfFile1 is ConstantIDFFile)
            {
                return ((ConstantIDFFile)idfFile1).ConstantValue / idfFile2;
            }

            // If extent and cellsize are equal, use a fast iteration loop
            if (idfFile1.Extent.Equals(idfFile2.Extent) && idfFile1.XCellsize.Equals(idfFile2.XCellsize) && idfFile1.YCellsize.Equals(idfFile2.YCellsize))
            {
                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "DIV", idfFile2), idfFile1.Extent, idfFile1.XCellsize, idfFile1.YCellsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile1.Values;
                float[][] values2 = idfFile2.Values;
                float[][] resultValues = resultIDFFile.values;
                float value1 = float.NaN;
                float value2 = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile1.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile1.NCols; colIdx++)
                    {
                        value1 = values1[rowIdx][colIdx];
                        value2 = values2[rowIdx][colIdx];
                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN) && !value2.Equals(0))
                        {
                            resultValues[rowIdx][colIdx] = value1 / value2;
                        }
                        else
                        {
                            resultValues[rowIdx][colIdx] = resultNoDataValue;
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);
                cellIterator.AddIDFFile(idfFile1);
                cellIterator.AddIDFFile(idfFile2);

                resultIDFFile = new IDFFile(CreateFilename(idfFile1, "DIV", idfFile2), cellIterator.MaxExtent, cellIterator.XStepsize, cellIterator.YStepsize, idfFile1.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile1.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile1.TOPLevel, idfFile1.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float value1 = float.NaN;
                float value2 = float.NaN;

                cellIterator.Reset();
                try
                {
                    while (cellIterator.IsInsideExtent())
                    {
                        value1 = cellIterator.GetCellValue(idfFile1);
                        value2 = cellIterator.GetCellValue(idfFile2);

                        if (value1.Equals(float.NaN) || value1.Equals(noDataValue1))
                        {
                            if (!noDataCalculationValue1.Equals(float.NaN))
                            {
                                value1 = noDataCalculationValue1;
                            }
                            else
                            {
                                value1 = float.NaN;
                            }
                        }
                        if (value2.Equals(float.NaN) || value2.Equals(noDataValue2))
                        {
                            if (!noDataCalculationValue2.Equals(float.NaN))
                            {
                                value2 = noDataCalculationValue2;
                            }
                            else
                            {
                                value2 = float.NaN;
                            }
                        }

                        if (!value1.Equals(float.NaN) && !value2.Equals(float.NaN) && !value2.Equals(0))
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, value1 / value2);
                        }
                        else
                        {
                            resultIDFFile.SetValue(cellIterator.X, cellIterator.Y, resultNoDataValue);
                        }

                        cellIterator.MoveNext();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while processing / operator", ex);
                }
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.UpdateMinMaxValue();

                resultIDFFile.Filename = CreateFilename(idfFile1, "DIV", idfFile2);
                resultIDFFile.Legend = CreateLegend(idfFile1, idfFile2);
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Multiplies all non-NoData-values in this IDF with the given factor
        /// </summary>
        /// <param name="factor"></param>
        public void Multiply(float factor)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            if (factor != float.NaN)
            {
                float value = float.NaN;
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        value = values[rowIdx][colIdx];
                        if (value.Equals(float.NaN) || value.Equals(NoDataValue))
                        {
                            value = NoDataCalculationValue;
                        }
                        if (!value.Equals(float.NaN))
                        {
                            values[rowIdx][colIdx] = value * factor;
                        }
                    }
                }
            }

            MinValue *= factor;
            MaxValue *= factor;
        }

        /// <summary>
        /// Adds the given value to all non-NoData-values in this IDF
        /// </summary>
        /// <param name="value"></param>
        public void Add(float value)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            if (value != float.NaN)
            {
                float baseValue = float.NaN;
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        baseValue = (values[rowIdx][colIdx].Equals(float.NaN) || value.Equals(NoDataValue)) ? NoDataCalculationValue : values[rowIdx][colIdx];
                        if (!baseValue.Equals(float.NaN))
                        {
                            values[rowIdx][colIdx] = baseValue + value;
                        }
                    }
                }
            }

            MinValue += value;
            MaxValue += value;
        }

        /// <summary>
        /// Adds a value to idfFile. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDFFile operator +(IDFFile idfFile, float value)
        {
            return Transform(idfFile, 1.0f, value);
        }

        /// <summary>
        /// Substracts a value from idfFile. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDFFile operator -(IDFFile idfFile, float value)
        {
            return Transform(idfFile, 1.0f, -1 * value);
        }

        /// <summary>
        /// Multiplies an idfFile with a value. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDFFile operator *(IDFFile idfFile, float value)
        {
            return Transform(idfFile, value, 0f);
        }

        /// <summary>
        /// Raises values in idfFile to specified power (value). NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="power"></param>
        /// <returns></returns>
        public static IDFFile operator ^(IDFFile idfFile, float power)
        {
            float noDataValue1 = idfFile.NoDataValue;
            float noDataCalculationValue1 = idfFile.NoDataCalculationValue;

            IDFFile resultIDFFile = idfFile.CopyIDF(Path.GetFileNameWithoutExtension(idfFile.Filename) + "POW" + power.ToString("F3", EnglishCultureInfo) + ".IDF");
            resultIDFFile.SetValues(0);

            // Force (lazy) load of values
            idfFile.EnsureLoadedValues();

            float value = float.NaN;
            float resultValue = float.NaN;
            for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                {
                    value = idfFile.values[rowIdx][colIdx];
                    if (value.Equals(float.NaN) || value.Equals(noDataValue1))
                    {
                        value = noDataCalculationValue1;
                    }
                    if (!value.Equals(float.NaN))
                    {
                        resultValue = (float)Math.Pow(value, power);
                    }
                    else
                    {
                        resultValue = resultIDFFile.NoDataValue;
                    }

                    resultIDFFile.values[rowIdx][colIdx] = resultValue;
                }
            }

            resultIDFFile.UpdateMinMaxValue();
            return resultIDFFile;
        }

        /// <summary>
        /// Divides an idfFile by a value. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// Division by zero results in an IDF-file with NoData-values
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDFFile operator /(IDFFile idfFile, float value)
        {
            if (value.Equals(0))
            {
                // Force NoData result file
                return Transform(idfFile, float.NaN, 0f);
            }
            else
            {
                return Transform(idfFile, 1.0f / value, 0f);
            }
        }

        /// <summary>
        /// Divides an idfFile by a value. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// Division by zero results in an IDF-file with NoData-values
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDFFile operator /(float value, IDFFile idfFile)
        {
            if (value.Equals(0))
            {
                // Return zero result file
                return Transform(idfFile, 0, 0);
            }
            else
            {
                IDFFile resultIDFFile = null;
                float noDataValue1 = idfFile.NoDataValue;
                float noDataCalculationValue1 = idfFile.NoDataCalculationValue;

                // Use a fast iteration loop
                resultIDFFile = new IDFFile(CreateFilename(idfFile, "DIV", value), idfFile.Extent, idfFile.XCellsize, idfFile.YCellsize, idfFile.NoDataValue);
                resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
                resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
                float resultNoDataValue = resultIDFFile.NoDataValue;
                float[][] values1 = idfFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float idfValue = float.NaN;
                float resultValue = float.NaN;

                for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                    {
                        idfValue = values1[rowIdx][colIdx];
                        if (idfValue.Equals(float.NaN) || idfValue.Equals(noDataValue1))
                        {
                            idfValue = noDataCalculationValue1;
                        }
                        if (!idfValue.Equals(float.NaN))
                        {
                            resultValue = value / idfValue;
                        }
                        else
                        {
                            resultValue = resultIDFFile.NoDataValue;
                        }

                        resultValues[rowIdx][colIdx] = resultValue;
                    }
                }
                resultIDFFile.MinValue = value / idfFile.MaxValue;
                resultIDFFile.MaxValue = value / idfFile.MinValue;

                if (resultIDFFile != null)
                {
                    resultIDFFile.Filename = CreateFilename(idfFile, "div", value);
                    resultIDFFile.Legend = (idfFile.Legend != null) ? idfFile.Legend.Copy() : null;
                }

                return resultIDFFile;
            }
        }

        /// <summary>
        /// Calculates a * x + b for all cell values (x) in IDFFile. NoData-values are replaced by the defined NoDataCalculation-value or result in NoData when NoDataCalculation-value equals float.NaN.
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static IDFFile Transform(IDFFile idfFile, float a, float b)
        {
            IDFFile resultIDFFile = null;
            float noDataValue1 = idfFile.NoDataValue;
            float noDataCalculationValue1 = idfFile.NoDataCalculationValue;

            // If extent and cellsize are equal, use a fast iteration loop
            resultIDFFile = new IDFFile(CreateFilename(idfFile, a, b), idfFile.Extent, idfFile.XCellsize, idfFile.YCellsize, idfFile.NoDataValue);
            resultIDFFile.NoDataCalculationValue = idfFile.NoDataCalculationValue;
            float resultNoDataValue = resultIDFFile.NoDataValue;
            resultIDFFile.SetITBLevels(idfFile.TOPLevel, idfFile.BOTLevel);
            float[][] values1 = idfFile.Values;
            float[][] resultValues = resultIDFFile.values;
            float value = float.NaN;
            float resultValue = float.NaN;

            if (a.Equals(float.NaN) || b.Equals(float.NaN))
            {
                for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                    {
                        resultValues[rowIdx][colIdx] = resultNoDataValue;
                    }
                }
                resultIDFFile.MinValue = resultNoDataValue;
                resultIDFFile.MaxValue = resultNoDataValue;
            }
            else
            {
                for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                    {
                        value = values1[rowIdx][colIdx];
                        if (value.Equals(float.NaN) || value.Equals(noDataValue1))
                        {
                            value = noDataCalculationValue1;
                        }
                        if (!value.Equals(float.NaN))
                        {
                            resultValue = a * value + b;
                        }
                        else
                        {
                            resultValue = resultIDFFile.NoDataValue;
                        }

                        resultValues[rowIdx][colIdx] = resultValue;
                    }
                }
                resultIDFFile.MinValue = a * idfFile.MinValue + b;
                resultIDFFile.MaxValue = a * idfFile.MaxValue + b;
            }

            if (resultIDFFile != null)
            {
                resultIDFFile.Filename = CreateFilename(idfFile, a, b);
                resultIDFFile.Legend = (idfFile.Legend != null) ? idfFile.Legend.Copy() : null;
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Remove ITB TOP and BOT-levels for this IDF-file, which may be used for voxel visualisation
        /// </summary>
        /// <param name="topLevel"></param>
        /// <param name="botLevel"></param>
        public void SetITBLevels(float topLevel, float botLevel)
        {
            if ((topLevel.Equals(float.NaN) && !botLevel.Equals(float.NaN)) || (!topLevel.Equals(float.NaN) && botLevel.Equals(float.NaN)))
            {
                throw new Exception("SetITBLevels: it is not allowed to set only top- or botlevel to a NaN-value, use NaN for both levels");
            }
            if (topLevel < botLevel)
            {
                throw new Exception("ITB TOP-level (" + topLevel + ") cannot be lower than BOT-level (" + botLevel + ")");
            }

            this.TOPLevel = topLevel;
            this.BOTLevel = botLevel;
        }

        /// <summary>
        /// Remove ITB TOP and BOT-levels for this IDF-file, which may be used for voxel visualisation
        /// </summary>
        public void ClearITBLevels()
        {
            this.TOPLevel = float.NaN;
            this.BOTLevel = float.NaN;
        }

        /// <summary>
        /// Counts input-cells that have a value equal to the given value 
        /// </summary>
        /// <param name="value"></param>
        public virtual long CountValues(float value)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            long valueCount = 0;
            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                if (values[rowIdx][colIdx].Equals(value))
                {
                    valueCount++;
                }
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }
            return valueCount++;
        }

        /// <summary>
        /// Round values to specified number of decimals using Math.Round()
        /// </summary>
        /// <param name="decimalCount"></param>
        public virtual void RoundValues(int decimalCount)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                float value = values[rowIdx][colIdx];
                values[rowIdx][colIdx] = (float)Math.Round(value, decimalCount);
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }

            this.MinValue = (float)Math.Round(this.MinValue, decimalCount);
            this.MaxValue = (float)Math.Round(this.MaxValue, decimalCount);
        }

        /// <summary>
        /// Replace values by their fractional part, the part behind the decimal point 
        /// </summary>
        public virtual void FractionValues()
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                float value = values[rowIdx][colIdx];
                values[rowIdx][colIdx] = (float)(value - Math.Truncate(value));
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }

            UpdateMinMaxValue();
        }

        /// <summary>
        /// Sets newvValue for all input-cells that have a value equal to the given oldvalue 
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public virtual void ReplaceValues(float oldValue, float newValue)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                if (values[rowIdx][colIdx].Equals(oldValue))
                {
                    values[rowIdx][colIdx] = newValue;
                }
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }
            UpdateMinMaxValue(oldValue, newValue);
        }

        /// <summary>
        /// Use the corresponding values from the given IDF-file for all input-cells
        /// </summary>
        /// <param name="newValueIDF"></param>
        public virtual void ReplaceValues(IDFFile newValueIDF)
        {
            if (newValueIDF == null)
            {
                throw new Exception("ReplaceValues(oldValue,newValueIDF) is not defined for null-newValueIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (newValueIDF is ConstantIDFFile)
            {
                float constantValue = ((ConstantIDFFile)newValueIDF).ConstantValue;
                if (constantValue.Equals(newValueIDF.NoDataValue) || constantValue.Equals(float.NaN))
                {
                    SetValues(newValueIDF.NoDataCalculationValue.Equals(float.NaN) ? NoDataValue : newValueIDF.NoDataCalculationValue);
                }
                else
                {
                    SetValues(((ConstantIDFFile)newValueIDF).ConstantValue);
                }
                return;
            }
            if (!Extent.Equals(newValueIDF.Extent))
            {
                throw new Exception("Replacing IDF (" + newValueIDF.Extent.ToString() + ") should have equal extent as replaced IDF (" + Extent.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            float newValueNoDataValue = newValueIDF.NoDataValue;
            float newValueNoDataCalculationValue = newValueIDF.NoDataCalculationValue;
            float thisNoDataValue = this.NoDataValue;

            if (XCellsize.Equals(newValueIDF.XCellsize) && YCellsize.Equals(newValueIDF.YCellsize))
            {
                int colIdx = 0;
                int rowIdx = 0;
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    float value = newValueIDF.values[rowIdx][colIdx];

                    if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                    {
                        if (!newValueNoDataCalculationValue.Equals(float.NaN))
                        {
                            value = newValueNoDataCalculationValue;
                        }
                        else
                        {
                            value = thisNoDataValue;
                        }
                    }
                    values[rowIdx][colIdx] = value;

                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else
            {
                IDFCellIterator idfCellIterator = new IDFCellIterator();
                idfCellIterator.AddIDFFile(this);
                idfCellIterator.AddIDFFile(newValueIDF);
                idfCellIterator.SetStepsize(this.XCellsize);
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    float value = idfCellIterator.GetCellValue(newValueIDF);

                    if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                    {
                        if (!newValueNoDataCalculationValue.Equals(float.NaN))
                        {
                            value = newValueNoDataCalculationValue;
                        }
                        else
                        {
                            value = thisNoDataValue;
                        }
                    }
                    this.SetValue(x, y, value);

                    idfCellIterator.MoveNext();
                }
            }

            UpdateMinMaxValue();
        }

        /// <summary>
        /// Use the corresponding value from the given newValueIDF for all input-cells that have a value equal to the given oldvalue 
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValueIDF"></param>
        public virtual void ReplaceValues(float oldValue, IDFFile newValueIDF)
        {
            if (newValueIDF == null)
            {
                throw new Exception("ReplaceValues(oldValue,newValueIDF) is not defined for null-newValueIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (newValueIDF is ConstantIDFFile)
            {
                float constantValue = ((ConstantIDFFile)newValueIDF).ConstantValue;
                if (constantValue.Equals(newValueIDF.NoDataValue) || constantValue.Equals(float.NaN))
                {
                    ReplaceValues(oldValue, newValueIDF.NoDataCalculationValue.Equals(float.NaN) ? NoDataValue : newValueIDF.NoDataCalculationValue);
                }
                else
                {
                    ReplaceValues(oldValue, ((ConstantIDFFile)newValueIDF).ConstantValue);
                }
                return;
            }
            if (!Extent.Equals(newValueIDF.Extent))
            {
                throw new Exception("Replacing IDF (" + newValueIDF.Extent.ToString() + ") should have equal extent as replaced IDF (" + Extent.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            float newValueNoDataValue = newValueIDF.NoDataValue;
            float newValueNoDataCalculationValue = newValueIDF.NoDataCalculationValue;
            float thisNoDataValue = this.NoDataValue;

            if (XCellsize.Equals(newValueIDF.XCellsize) && YCellsize.Equals(newValueIDF.YCellsize))
            {
                int colIdx = 0;
                int rowIdx = 0;
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if (values[rowIdx][colIdx].Equals(oldValue))
                    {
                        float value = newValueIDF.values[rowIdx][colIdx];

                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        values[rowIdx][colIdx] = value;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else
            {
                IDFCellIterator idfCellIterator = new IDFCellIterator();
                idfCellIterator.AddIDFFile(this);
                idfCellIterator.AddIDFFile(newValueIDF);
                idfCellIterator.SetStepsize(this.XCellsize);
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    if (idfCellIterator.GetCellValue(this).Equals(oldValue))
                    {
                        float value = idfCellIterator.GetCellValue(newValueIDF);

                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        this.SetValue(x, y, value);
                    }
                    idfCellIterator.MoveNext();
                }
            }

            UpdateMinMaxValue();
        }

        /// <summary>
        /// Sets newvValue for all cells that have a non noData-value in the given selectionIDF grid
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="newValue"></param>
        public virtual void ReplaceValues(IDFFile selectionIDF, float newValue)
        {
            if (selectionIDF == null)
            {
                throw new Exception("ReplaceValues(selectionIDF,newValue) is not defined for null-selectionIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (!Extent.Equals(selectionIDF.Extent))
            {
                throw new Exception("Selection IDF (" + selectionIDF.Extent.ToString() + ") should have equal extent as replaced IDF (" + Extent.ToString() + ")");
            }
            if (!XCellsize.Equals(selectionIDF.XCellsize))
            {
                throw new Exception("Selection IDF (" + selectionIDF.XCellsize.ToString() + ") should have equal cellsize as replaced IDF (" + XCellsize.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            selectionIDF.EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                if (!selectionIDF.values[rowIdx][colIdx].Equals(selectionIDF.NoDataValue))
                {
                    values[rowIdx][colIdx] = newValue;
                }
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }
            UpdateMinMaxValue();
        }

        /// <summary>
        /// Replaces all cells of this IDF-file that have a value in the selectionIDF equal to the selectionValue with the specified newValue
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="selectionValue"></param>
        /// <param name="newValue"></param>
        public void ReplaceValues(IDFFile selectionIDF, float selectionValue, float newValue)
        {
            if (selectionIDF == null)
            {
                throw new Exception("ReplaceValues(selectionIDF,newValue) is not defined for null-selectionIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (!Extent.Equals(selectionIDF.Extent))
            {
                throw new Exception("Selection IDF (" + selectionIDF.Extent.ToString() + ") should have equal extent as replaced IDF (" + Extent.ToString() + ")");
            }
            if (!XCellsize.Equals(selectionIDF.XCellsize))
            {
                throw new Exception("Selection IDF (" + selectionIDF.XCellsize.ToString() + ") should have equal cellsize as replaced IDF (" + XCellsize.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            selectionIDF.EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;
            while ((colIdx < NCols) && (rowIdx < NRows))
            {
                if (selectionIDF.values[rowIdx][colIdx].Equals(selectionValue))
                {
                    values[rowIdx][colIdx] = newValue;
                }
                colIdx++;
                if (colIdx == NCols)
                {
                    colIdx = 0;
                    rowIdx++;
                }
            }
            UpdateMinMaxValue();
        }

        /// <summary>
        /// Replaces all cells of this IDF-file that have a value in the selectionIDF equal to the selectionValue with the value of the corresponding cell in the specified newValueIDF
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="selectionValue"></param>
        /// <param name="newValueIDF"></param>
        public void ReplaceValues(IDFFile selectionIDF, float selectionValue, IDFFile newValueIDF)
        {
            if (selectionIDF == null)
            {
                throw new Exception("ReplaceValues(selectionIDF,newValue) is not defined for null-selectionIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (newValueIDF is ConstantIDFFile)
            {
                float constantValue = ((ConstantIDFFile)newValueIDF).ConstantValue;
                if (constantValue.Equals(newValueIDF.NoDataValue) || constantValue.Equals(float.NaN))
                {
                    ReplaceValues(selectionIDF, selectionValue, newValueIDF.NoDataCalculationValue.Equals(float.NaN) ? NoDataValue : newValueIDF.NoDataCalculationValue);
                }
                else
                {
                    ReplaceValues(selectionIDF, selectionValue, ((ConstantIDFFile)newValueIDF).ConstantValue);
                }
                return;
            }
            if (!Extent.Equals(selectionIDF.Extent))
            {
                throw new Exception("Selection IDF (" + selectionIDF.Extent.ToString() + ") should have equal extent as replaced IDF (" + Extent.ToString() + ")");
            }

            if (newValueIDF == null)
            {
                throw new Exception("ReplaceValues is not defined for null-newValueIDF");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (!Extent.Equals(newValueIDF.Extent))
            {
                throw new Exception("Replacing IDF '" + Path.GetFileNameWithoutExtension(newValueIDF.Filename) + "' (" + newValueIDF.Extent.ToString() + ") should have equal extent as replaced IDF '" + Path.GetFileNameWithoutExtension(this.Filename) + "' (" + Extent.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            selectionIDF.EnsureLoadedValues();
            newValueIDF.EnsureLoadedValues();

            float newValueNoDataValue = newValueIDF.NoDataValue;
            float newValueNoDataCalculationValue = newValueIDF.NoDataCalculationValue;
            float thisNoDataValue = this.NoDataValue;

            if (XCellsize.Equals(selectionIDF.XCellsize) && YCellsize.Equals(selectionIDF.YCellsize) && XCellsize.Equals(newValueIDF.XCellsize) && YCellsize.Equals(newValueIDF.YCellsize))
            {
                int colIdx = 0;
                int rowIdx = 0;
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if (selectionIDF.values[rowIdx][colIdx].Equals(selectionValue))
                    {
                        float value = newValueIDF.values[rowIdx][colIdx];
                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        values[rowIdx][colIdx] = value;
                    }

                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else
            {
                IDFCellIterator idfCellIterator = new IDFCellIterator();
                idfCellIterator.AddIDFFile(this);
                idfCellIterator.AddIDFFile(selectionIDF);
                idfCellIterator.AddIDFFile(newValueIDF);
                idfCellIterator.SetStepsize(this.XCellsize);
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    if (idfCellIterator.GetCellValue(selectionIDF).Equals(selectionValue))
                    {
                        float value = idfCellIterator.GetCellValue(newValueIDF);

                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        this.SetValue(x, y, value);
                    }
                    idfCellIterator.MoveNext();
                }
                UpdateMinMaxValue();
            }
        }

        /// <summary>
        /// Sets the value from newvValueIDF for all cells that have a non noData-value in the given selectionIDF grid
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="newValueIDF"></param>
        public virtual void ReplaceValues(IDFFile selectionIDF, IDFFile newValueIDF)
        {
            if ((selectionIDF == null) || (newValueIDF == null))
            {
                throw new Exception("ReplacesValues(selectionIDF,newValueIDF) is not defined for null-value IDFs");
            }
            if (Extent == null)
            {
                throw new Exception("Replaced IDF has null extent");
            }
            if (!XCellsize.Equals(selectionIDF.XCellsize))
            {
                throw new Exception("Selection IDF (" + selectionIDF.XCellsize.ToString() + ") should have equal cellsize as replaced IDF (" + XCellsize.ToString() + ")");
            }
            if (!XCellsize.Equals(newValueIDF.XCellsize))
            {
                throw new Exception("Replacing IDF newValueIDF (" + newValueIDF.XCellsize.ToString() + ") should have equal cellsize as replaced IDF (" + XCellsize.ToString() + ")");
            }

            // Force (lazy) load of values
            EnsureLoadedValues();
            selectionIDF.EnsureLoadedValues();
            newValueIDF.EnsureLoadedValues();
            float newValueNoDataValue = newValueIDF.NoDataValue;
            float newValueNoDataCalculationValue = newValueIDF.NoDataCalculationValue;
            float thisNoDataValue = this.NoDataValue;

            if (Extent.Equals(selectionIDF.Extent) && Extent.Equals(newValueIDF.Extent))
            {
                int colIdx = 0;
                int rowIdx = 0;
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if (!selectionIDF.values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        float value = newValueIDF.values[rowIdx][colIdx];

                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        Values[rowIdx][colIdx] = value;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }

                UpdateMinMaxValue();
            }
            else
            {
                IDFCellIterator idfCellIterator = new IDFCellIterator();
                idfCellIterator.AddIDFFile(this);
                idfCellIterator.AddIDFFile(selectionIDF);
                idfCellIterator.AddIDFFile(newValueIDF);
                idfCellIterator.SetStepsize(this.XCellsize);
                idfCellIterator.Reset();
                bool isGlobalMinMaxUpdateNeeded = false;
                while (idfCellIterator.IsInsideExtent())
                {
                    float x = idfCellIterator.X;
                    float y = idfCellIterator.Y;

                    if (!idfCellIterator.GetCellValue(selectionIDF).Equals(selectionIDF.NoDataValue))
                    {
                        float oldValue = idfCellIterator.GetCellValue(this);
                        float value = idfCellIterator.GetCellValue(newValueIDF);

                        if (value.Equals(float.NaN) || value.Equals(newValueNoDataValue))
                        {
                            if (!newValueNoDataCalculationValue.Equals(float.NaN))
                            {
                                value = newValueNoDataCalculationValue;
                            }
                            else
                            {
                                value = thisNoDataValue;
                            }
                        }
                        this.SetValue(x, y, value);
                    }
                    idfCellIterator.MoveNext();
                }

                UpdateMinMaxValue();
            }
        }

        /// <summary>
        /// Sets newvValue for all input-cells that have a value within the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="newValue"></param>
        public virtual void ReplaceValues(ValueRange range, float newValue)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            int colIdx = 0;
            int rowIdx = 0;

            // To reduce computation time take test for inclusion out of while loop 
            if (range.IsV1Included && range.IsV2Included)
            {
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if ((values[rowIdx][colIdx] >= range.V1) && (values[rowIdx][colIdx] <= range.V2))
                    {
                        values[rowIdx][colIdx] = newValue;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else if (!range.IsV1Included && range.IsV2Included)
            {
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if ((values[rowIdx][colIdx] > range.V1) && (values[rowIdx][colIdx] <= range.V2))
                    {
                        values[rowIdx][colIdx] = newValue;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else if (range.IsV1Included && !range.IsV2Included)
            {
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if ((values[rowIdx][colIdx] >= range.V1) && (values[rowIdx][colIdx] < range.V2))
                    {
                        values[rowIdx][colIdx] = newValue;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else if (!range.IsV1Included && !range.IsV2Included)
            {
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    if ((values[rowIdx][colIdx] > range.V1) && (values[rowIdx][colIdx] < range.V2))
                    {
                        values[rowIdx][colIdx] = newValue;
                    }
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }

            UpdateMinMaxValue();
        }

        /// <summary>
        /// Check if the given file has a valid IDF-extension
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasIDFExtension(string filename)
        {
            return (Path.GetExtension(filename).ToLower().Equals(".idf"));
        }

        /// <summary>
        /// Read a given IDF- or ASC-file, optionally specify the use of lazy loading and the use of logging
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useLazyLoading">optional: specifies that the IDF-values should be lazyloaded</param>
        /// <param name="log">optional: specifies that logging is to be used (e.g. for notice of a lazyload of values)</param>
        /// <param name="logIndentLevel"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        public static IDFFile ReadFile(string filename, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0, Extent extent = null)
        {
            if (!File.Exists(filename))
            {
                throw new ToolException("IDF-file does not exist: " + filename);
            }

            IDFFile idfFile = null;
            if (Path.GetExtension(filename).ToLower().Equals(".idf"))
            {
                idfFile = new IDFFile();
                idfFile.UseLazyLoading = useLazyLoading;
                idfFile.Log = log;
                idfFile.LogIndentLevel = logIndentLevel;
                idfFile.ReadIDFFile(filename, extent);
            }
            else if (Path.GetExtension(filename).ToLower().Equals(".asc"))
            {
                if (log != null)
                {
                    log.AddInfo("Reading " + Path.GetFileName(filename) + " ...", logIndentLevel);
                }
                ASCFile ascFile = ASCFile.ReadFile(filename, EnglishCultureInfo);
                idfFile = new IDFFile(ascFile);
                idfFile = idfFile.ClipIDF(extent);
            }
            else
            {
                throw new Exception("Unsupported extension for IDF-file: " + filename);
            }

            if (idfFile.IsConstantIDFFile())
            {
                idfFile = new ConstantIDFFile(idfFile.Values[0][0]);
            }

            return idfFile;
        }

        /// <summary>
        /// Open IDF-file for direct access 
        /// </summary>
        public void OpenFile()
        {
            if (idfReader != null)
            {
                Log.AddMessage(LogLevel.Debug, "Closing IDF-file: " + Filename, 1);
                idfReader.Close();
            }

            if (Log != null)
            {
                Log.AddMessage(LogLevel.Debug, "Opening IDF-file for direct access: " + Filename, 1);
            }

            if (Filename != null)
            {
                if (!File.Exists(Filename))
                {
                    throw new ToolException("IDF-file does not exist: " + Filename);
                }

                Stream stream = null;
                try
                {
                    stream = File.OpenRead(Filename);
                    idfReader = new BinaryReader(stream);
                }
                catch (EndOfStreamException ex)
                {
                    if (stream != null)
                    {
                        stream.Close();
                        idfReader = null;
                    }
                    throw new Exception("Unexpected end of file while opening IDF-file " + Filename, ex);
                }
                catch (Exception ex)
                {
                    if (stream != null)
                    {
                        stream.Close();
                        idfReader = null;
                    }
                    throw new Exception("Could not open IDF-file " + Filename, ex);
                }
            }
        }

        /// <summary>
        /// Close IDF-file that was opened for direct access
        /// </summary>
        public void CloseFile()
        {
            if (idfReader != null)
            {
                idfReader.Close();
                idfReader = null;
            }
        }

        /// <summary>
        /// Reads a value directly from the IDF-file on disk. 
        /// For this, the IDF-file should be opened before with the OpenFile () method.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float ReadValue(float x, float y)
        {
            return ReadValue(GetRowIdx(y), GetColIdx(x));
        }

        /// <summary>
        /// Reads a value directly from the IDF-file on disk. For this, the IDF-file should be opened before with the OpenFile() method. 
        /// For optimization purposes, no checking is done for unopened IDF-files. Use CloseFile() method to close IDF-file afterwards.
        /// If row and/or column index is out-of-bound, a NoData-value is returned.
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public float ReadValue(int rowIdx, int colIdx)
        {
            if ((rowIdx < NRows) && (colIdx < NCols))
            {
                idfReader.BaseStream.Position = HeaderByteLength + 4 * (rowIdx * NCols + colIdx);
                if (IsDoublePrecisionFile())
                {
                    return (float)idfReader.ReadDouble();
                }
                else
                {
                    return (float)idfReader.ReadSingle();
                }
            }
            else
            {
                return NoDataValue;
            }
        }

        /// <summary>
        /// Write this IDFFile object to the file using the given filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string filename, Metadata metadata = null)
        {
            // Force (lazy) load of values
            EnsureLoadedValues();

            this.Filename = filename;
            WriteFile(metadata);
        }

        /// <summary>
        /// Write this IDFFile object to the file using its filename property for the filename
        /// </summary>
        /// <param name="metadata"></param>
        public override void WriteFile(Metadata metadata = null)
        {
            Stream stream = null;
            BinaryWriter bw = null;

            try
            {
                // Force (lazy) load of values
                EnsureLoadedValues();
                UpdateMinMaxValue();

                if (!Path.GetDirectoryName(Filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(Filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Filename));
                }

                if (Path.GetExtension(Filename).Equals(string.Empty))
                {
                    Filename += ".IDF";
                }

                stream = File.Open(Filename, FileMode.Create);
                bw = new BinaryWriter(stream);

                // Write Definitions
                int startValue = 1271;  // is always 1271
                bw.Write(startValue);
                bw.Write(NCols);
                bw.Write(NRows);
                if (extent == null)
                {
                    throw new Exception("Define an extent before writing IDF-file: " + Filename);
                }
                bw.Write(extent.llx);
                bw.Write(extent.urx);
                bw.Write(extent.lly);
                bw.Write(extent.ury);
                bw.Write(MinValue);
                bw.Write(MaxValue);
                bw.Write(NoDataValue);

                byte ieq = 0;
                bw.Write(ieq);
                byte itb = (TOPLevel.Equals(float.NaN) || BOTLevel.Equals(float.NaN)) ? (byte)0 : (byte)1;
                bw.Write(itb);
                bw.Write((byte)0);
                bw.Write((byte)0);

                bw.Write(XCellsize);
                bw.Write(YCellsize);

                if (itb == 1)
                {
                    bw.Write(TOPLevel);
                    bw.Write(BOTLevel);
                }

                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        float value = values[rowIdx][colIdx];
                        bw.Write(value);
                        if ((value != NoDataValue) && ((value > MaxValue) || (value < MinValue)))
                        {
                            throw new Exception("value " + value + " at (" + GetX(colIdx) + "," + GetY(rowIdx) + ") is outside (incorrectly computed) min-max bounds of IDF's");
                        }
                    }
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
                if (bw != null)
                {
                    bw.Close();
                    bw = null;
                    stream = null;
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }

            // Since the object has been written to a new file, now the file extent is defined
            fileExtent = extent.Copy();
            modifiedExtent = null;

            if (metadata != null)
            {
                // force metadata to refer to this IDF-file
                metadata.IMODFilename = Filename;
                metadata.METFilename = null;
                metadata.Type = Extension;
                metadata.Resolution = this.XCellsize.ToString(EnglishCultureInfo) + "x" + this.YCellsize.ToString(EnglishCultureInfo);
                metadata.WriteMetaFile();
            }
        }

        /// <summary>
        /// Enlarges this IDF to the union its current extent and the specified extent, cells outside current extent will be set to NoData
        /// </summary>
        /// <param name="enlargeExtent">extext to enlarge to</param>
        /// <param name="defaultValue">a value to initalize new cells with, or leave empty to use NoData</param>
        /// <returns></returns>
        public virtual IDFFile EnlargeIDF(Extent enlargeExtent, float defaultValue = float.NaN)
        {
            if (enlargeExtent == null)
            {
                throw new Exception("A non-null extent should be defined for enlarging IDF-file: " + Path.GetFileName(this.Filename));
            }

            if (this.extent == null)
            {
                throw new Exception("No extent is defined for the base file. Enlarge is not possible for: " + this.Filename);
            }

            if (!this.extent.IsAligned(enlargeExtent, this.XCellsize, this.YCellsize))
            {
                throw new Exception("Extents " + this.extent.ToString() + " and " + enlargeExtent.ToString() + " are not aligned for resolution " + XCellsize + "x" + YCellsize + ". Enlarge is not possible for: " + this.Filename);
            }

            if (this.extent.Contains(enlargeExtent))
            {
                // No need to enlarge; return this IDF-file
                return this;
            }

            Extent newExtent = this.extent.Union(enlargeExtent);
            IDFFile enlargedIDFFile = new IDFFile();
            enlargedIDFFile.Initialize(Filename, newExtent, XCellsize, YCellsize, NoDataValue, false, Log, LogIndentLevel);
            enlargedIDFFile.NoDataCalculationValue = NoDataCalculationValue;
            enlargedIDFFile.SetITBLevels(TOPLevel, BOTLevel);
            enlargedIDFFile.UseLazyLoading = UseLazyLoading;
            enlargedIDFFile.MinValue = float.MaxValue;
            enlargedIDFFile.MaxValue = float.MinValue;

            // Define initial values
            if (this.Values != null)
            {
                enlargedIDFFile.DeclareValuesMemory();
                if (defaultValue.Equals(float.NaN))
                {
                    enlargedIDFFile.ResetValues();
                }
                else
                {
                    enlargedIDFFile.SetValues(defaultValue);
                }

                // Now start copying
                enlargedIDFFile.MinValue = this.MinValue;
                enlargedIDFFile.MaxValue = this.MaxValue;
                for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < NCols; colIdx++)
                    {
                        float x = GetX(colIdx);
                        float y = GetY(rowIdx);
                        float value = values[rowIdx][colIdx];
                        enlargedIDFFile.SetValue(x, y, value);
                    }
                }

                if (!defaultValue.Equals(float.NaN))
                {
                    UpdateMinMaxValue(defaultValue);
                }
            }

            if (Legend != null)
            {
                enlargedIDFFile.Legend = Legend.Copy();
            }

            if (Metadata != null)
            {
                enlargedIDFFile.Metadata = Metadata.Copy();
                string clipString = "Enlarged to extent " + newExtent.ToString();
                if (enlargedIDFFile.Metadata.ProcessDescription.Equals(string.Empty))
                {
                    enlargedIDFFile.Metadata.ProcessDescription = clipString;
                }
                else
                {
                    enlargedIDFFile.Metadata.ProcessDescription += clipString;
                }
            }

            return enlargedIDFFile;
        }

        /// <summary>
        /// Clips IMODFile instance to given extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns>exception if no overlap in extents</returns>
        public override IMODFile Clip(Extent clipExtent)
        {
            IDFFile idfFile = ClipIDF(clipExtent);
            if (idfFile == null)
            {
                throw new ToolException("No overlap in extent of '" + Path.GetFileName(this.Filename) + "' " + this.extent.ToString() + " and clipExtent: '" + clipExtent.ToString());
            }
            return idfFile;
        }

        /// <summary>
        /// Clips IDF-file to given extent
        /// </summary>
        /// <param name="clipExtent">the extent to clip with</param>
        /// <param name="isInvertedClip">if true, the part within the extent is clipped away</param>
        /// <returns>null, if no overlap in extents</returns>
        public virtual IDFFile ClipIDF(Extent clipExtent, bool isInvertedClip = false)
        {
            int upperRowIdx;
            int lowerRowIdx;
            int leftColIdx;
            int rightColIdx;
            int invRow;

            if (clipExtent == null)
            {
                // Use extent of IDF-file
                clipExtent = this.extent;
                // throw new Exception("A non-null extent should be defined for clipping IDF-file: " + Path.GetFileName(this.filename));
            }

            if (!this.extent.Clip(clipExtent).IsValidExtent())
            {
                return null;
                // throw new Exception("No overlap in extent of '" + Path.GetFileName(this.Filename) + "' " +  this.extent.ToString() + " and clipExtent: '" + clipExtent.ToString());
            }

            if (clipExtent.Contains(this.extent) && !isInvertedClip)
            {
                // No need to clip; return this IDF-file
                return this;
            }

            // Snap clip extent to extent and cellsize of source IDF-file, ensure corrected clipExtent is not smaller than original clipExtent
            float llxMismatch = (clipExtent.llx - extent.llx) % XCellsize;
            float llyMismatch = (clipExtent.lly - extent.lly) % YCellsize;
            float urxMismatch = (extent.urx - clipExtent.urx) % XCellsize;
            float uryMismatch = (extent.ury - clipExtent.ury) % YCellsize;
            float llxCorr = clipExtent.llx - llxMismatch;
            float llyCorr = clipExtent.lly - llyMismatch;
            float urxCorr = clipExtent.urx + urxMismatch;
            float uryCorr = clipExtent.ury + uryMismatch;
            if (urxCorr < clipExtent.urx)
            {
                urxCorr += XCellsize;
            }
            if (uryCorr < clipExtent.ury)
            {
                uryCorr += YCellsize;
            }
            clipExtent = new Extent(llxCorr, llyCorr, urxCorr, uryCorr);

            // Clip the extent
             Extent clippedExtent = this.extent.Clip(clipExtent);

            // Initialize clipped result IDFFile
            IDFFile clippedIDFFile = new IDFFile();
            clippedIDFFile.Filename = Filename;
            clippedIDFFile.XCellsize = XCellsize;
            clippedIDFFile.YCellsize = YCellsize;
            clippedIDFFile.NoDataValue = NoDataValue;
            clippedIDFFile.NoDataCalculationValue = NoDataCalculationValue;
            clippedIDFFile.MinValue = float.MaxValue;
            clippedIDFFile.MaxValue = float.MinValue;
            clippedIDFFile.UseLazyLoading = UseLazyLoading;
            clippedIDFFile.fileExtent = (this.fileExtent != null) ? this.fileExtent.Copy() : null;
            clippedIDFFile.modifiedExtent = isInvertedClip ? null : ((clipExtent != null) ? clipExtent.Copy() : null);
            clippedIDFFile.extent = (this.extent != null) ? this.extent.Copy() : null;
            clippedIDFFile.SetITBLevels(TOPLevel, BOTLevel);

            if (this.extent == null)
            {
                throw new Exception("No extent is defined for the base file. Clip is not possible for: " + this.Filename);
            }

            // Calculate new number of columns and number of rows
            invRow = (int)((clippedExtent.ury - extent.lly - (YCellsize / 10)) / YCellsize); // don't include upper row if clipboundary is exactly at cell border
            upperRowIdx = NRows - invRow - 1;
            invRow = (int)((clippedExtent.lly - extent.lly) / YCellsize);
            lowerRowIdx = NRows - invRow - 1;
            leftColIdx = (int)((clippedExtent.llx - extent.llx) / XCellsize);
            rightColIdx = (int)(((clippedExtent.urx - extent.llx - (XCellsize / 10)) / XCellsize));  // don't include right column if clipboundary is exactly at cell border

            clippedIDFFile.NCols = isInvertedClip ? NCols : (rightColIdx - leftColIdx + 1);
            clippedIDFFile.NRows = isInvertedClip ? NRows : (lowerRowIdx - upperRowIdx + 1);
            clippedIDFFile.extent = isInvertedClip ? this.Extent : clippedExtent.Copy();

            if (this.values != null)
            {
                clippedIDFFile.DeclareValuesMemory();

                if (isInvertedClip)
                {
                    // Copy values from source file
                    for (int rowIdx = 0; rowIdx < clippedIDFFile.NRows; rowIdx++)
                    {
                        for (int colIdx = 0; colIdx < clippedIDFFile.NCols; colIdx++)
                        {
                            clippedIDFFile.values[rowIdx][colIdx] = values[rowIdx][colIdx];
                        }
                    }

                    // Remove values inside rectangle: set to NoData
                    for (int rowIdx = upperRowIdx; rowIdx <= lowerRowIdx; rowIdx++)
                    {
                        for (int colIdx = leftColIdx; colIdx <= rightColIdx; colIdx++)
                        {
                            clippedIDFFile.values[rowIdx][colIdx] = NoDataValue;
                        }
                    }

                    UpdateMinMaxValue();
                }
                else
                {
                    // copy clipped values and recalculate min/max-values
                    float outputValue;
                    for (int rowIdx = 0; rowIdx < clippedIDFFile.NRows; rowIdx++)
                    {
                        for (int colIdx = 0; colIdx < clippedIDFFile.NCols; colIdx++)
                        {
                            outputValue = float.NaN;
                            if (((upperRowIdx + rowIdx) > values.Length) || ((leftColIdx + colIdx) > values[values.Length - 1].Length) || (upperRowIdx < 0) || (leftColIdx < 0))
                            {
                                // this should actually never happen...
                                outputValue = float.NaN;
                            }
                            else
                            {
                                outputValue = values[upperRowIdx + rowIdx][leftColIdx + colIdx];
                            }
                            clippedIDFFile.values[rowIdx][colIdx] = outputValue;
                            if (!outputValue.Equals(NoDataValue) && (outputValue > clippedIDFFile.MaxValue))
                            {
                                clippedIDFFile.MaxValue = outputValue;
                            }
                            if (!outputValue.Equals(NoDataValue) && (outputValue < clippedIDFFile.MinValue))
                            {
                                clippedIDFFile.MinValue = outputValue;
                            }
                        }
                    }
                }
            }

            if (Legend != null)
            {
                clippedIDFFile.Legend = Legend.Copy();
            }

            if (Metadata != null)
            {
                clippedIDFFile.Metadata = Metadata.Copy();
                string clipString = (isInvertedClip ? "Inverse clip" : "Clip") + " to extent " + clipExtent.ToString();
                if (clippedIDFFile.Metadata.ProcessDescription.Equals(string.Empty))
                {
                    clippedIDFFile.Metadata.ProcessDescription = clipString;
                }
                else
                {
                    clippedIDFFile.Metadata.ProcessDescription += clipString;
                }
            }

            return clippedIDFFile;
        }

        /// <summary>
        /// Snap the extent of this IDFFile to the specified cellsizes for the x- and y-direction. The cellvalues are not modified.
        /// </summary>
        /// <param name="xCellSize"></param>
        /// <param name="yCellSize"></param>
        /// <param name="isEnlarged">If true, snapped extent is enlarged to ensure original extent is contained. If false, extent dimensions will not change</param>
        public void SnapExtent(float xCellSize, float yCellSize, bool isEnlarged = false)
        {
            Extent snapExtent = extent.Snap(XCellsize, YCellsize, isEnlarged);
            extent.llx = snapExtent.llx;
            extent.lly = snapExtent.lly;
            extent.urx = snapExtent.urx;
            extent.ury = snapExtent.ury;
        }

        /// <summary>
        /// Snap the extent of this IDFFile to the specified cellsize in the x- and y-direction. The cellvalues are not modified.
        /// </summary>
        /// <param name="cellsize"></param>
        /// <param name="isEnlarged">If true, snapped extent is enlarged to ensure original extent is contained. If false, extent dimensions will not change</param>
        public void SnapExtent(int cellsize, bool isEnlarged = false)
        {
            Extent snapExtent = extent.Snap(cellsize, isEnlarged);
            extent.llx = snapExtent.llx;
            extent.lly = snapExtent.lly;
            extent.urx = snapExtent.urx;
            extent.ury = snapExtent.ury;
        }

        /// <summary>
        /// Copy this IDFFile object to an new IDF-file with specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>IMODFile object</returns>
        public override IMODFile Copy(string filename)
        {
            return CopyIDF(filename);
        }

        /// <summary>
        /// Copy IDF-file metadata and values (if specified) to a new IDF-object with specified filename. New memory is allocated for values.
        /// Note: at this point an actual clip is made, when clipExtent has been set for the base IDF-file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isValueCopy">if true, cellvalues are copied as well</param>
        /// <returns>IDFFile object</returns>
        public virtual IDFFile CopyIDF(string filename, bool isValueCopy = true)
        {
            IDFFile idfFile = new IDFFile();
            idfFile.CopyContents(this, isValueCopy);
            idfFile.Filename = filename;

            return idfFile;
        }

        /// <summary>
        /// Copy properties and (optionally) values from specified other IDF-file to this IDF-file
        /// </summary>
        /// <param name="otherIDFFile"></param>
        /// <param name="isValueCopy">if true, values of IDF-file are copied as well</param>
        public void CopyContents(IDFFile otherIDFFile, bool isValueCopy = true)
        {
            if (isValueCopy)
            {
                // Force (lazy) load of values in specified IDF-file (which might clip IDF-file in memory (to support lazy-loading) based on current clipextent)
                otherIDFFile.EnsureLoadedValues();
            }

            MaxValue = otherIDFFile.MaxValue;
            MinValue = otherIDFFile.MinValue;
            NCols = otherIDFFile.NCols;
            NoDataValue = otherIDFFile.NoDataValue;
            NoDataCalculationValue = otherIDFFile.NoDataCalculationValue;
            NRows = otherIDFFile.NRows;
            XCellsize = otherIDFFile.XCellsize;
            YCellsize = otherIDFFile.YCellsize;
            UseLazyLoading = otherIDFFile.UseLazyLoading;
            fileExtent = (otherIDFFile.fileExtent != null) ? otherIDFFile.fileExtent.Copy() : null;
            extent = (otherIDFFile.extent != null) ? otherIDFFile.extent.Copy() : null;
            modifiedExtent = (otherIDFFile.modifiedExtent != null) ? otherIDFFile.modifiedExtent.Copy() : null;
            SetITBLevels(otherIDFFile.TOPLevel, otherIDFFile.BOTLevel);

            if (isValueCopy)
            {
                // declare memory for values
                values = new float[NRows][];
                for (int i = 0; i < NRows; i++)
                {
                    values[i] = new float[NCols];
                }

                int colIdx = 0;
                int rowIdx = 0;
                while ((colIdx < NCols) && (rowIdx < NRows))
                {
                    values[rowIdx][colIdx] = otherIDFFile.values[rowIdx][colIdx];
                    colIdx++;
                    if (colIdx == NCols)
                    {
                        colIdx = 0;
                        rowIdx++;
                    }
                }
            }
            else
            {
                values = null;
            }

            if (Legend != null)
            {
                Legend = otherIDFFile.Legend.Copy();
            }
            Filename = otherIDFFile.Filename;

            if (Metadata != null)
            {
                Metadata = otherIDFFile.Metadata.Copy();
            }
        }

        /// <summary>
        /// Calculate otherIDFfile - this IDFFile, within the extent
        /// </summary>
        /// <param name="otherIDFFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="isNoDataCompared">if true the actual NoData-values are compared and may result in a difference</param>
        /// <param name="comparedExtent"></param>
        /// <returns></returns>
        public virtual IDFFile CreateDifferenceFile(IDFFile otherIDFFile, string outputPath, bool isNoDataCompared, Extent comparedExtent = null)
        {
            try
            {
                // If specified so, force comparison of cells with NoData, don't use NoDataCalculation-value, but save current value to restore later
                float currentNoDataCalculationValue1 = this.NoDataCalculationValue;
                float currentNoDataCalculationValue2 = otherIDFFile.NoDataCalculationValue;
                if (!isNoDataCompared)
                {
                    // Force the noDataCalculaion values of both IDF-files to be equal, so it won't give a difference
                    this.NoDataCalculationValue = 0;
                    otherIDFFile.NoDataCalculationValue = 0;
                }
                else
                {
                    // use the NoData-value as the NoDataCalculation-value, so the actual value of NoData will be used in the comparison
                    this.NoDataCalculationValue = this.NoDataValue;
                    otherIDFFile.NoDataCalculationValue = otherIDFFile.NoDataValue;
                }

                // Actually compare IDF-files
                IDFFile diffIDFFile = this - otherIDFFile;

                // Reset nodata calculationvalues
                this.NoDataCalculationValue = currentNoDataCalculationValue1;
                otherIDFFile.NoDataCalculationValue = currentNoDataCalculationValue2;

                IDFFile clippedDiffIDFFile = diffIDFFile.ClipIDF(comparedExtent);
                clippedDiffIDFFile.Filename = FileUtils.GetUniqueFilename(Path.Combine(outputPath, "DIFF_" + Path.GetFileNameWithoutExtension(Filename)) + ".IDF");
                return clippedDiffIDFFile;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create difference file", ex);
            }
        }

        /// <summary>
        /// Checks if extent and cellsize of this IDF-file is equal to that of the second IDF-file
        /// </summary>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        public bool HasEqualExtentAndCellsize(IDFFile idfFile2)
        {
            if (idfFile2 != null)
            {
                if ((this.extent != null) && (idfFile2.extent != null))
                {
                    // Check that the lower left corners of the files match
                    if (!this.extent.llx.Equals(idfFile2.extent.llx) || !this.extent.lly.Equals(idfFile2.extent.lly))
                    {
                        return false;
                    }
                    // Check that the upper right corners of the files match
                    if (!this.extent.urx.Equals(idfFile2.extent.urx) || !this.extent.ury.Equals(idfFile2.extent.ury))
                    {
                        return false;
                    }
                    // Check that the cellsizes of the files match
                    if (!this.XCellsize.Equals(idfFile2.XCellsize) || !this.YCellsize.Equals(idfFile2.YCellsize))
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(IDFFile other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// Create default range-legend between min-max values of this IDF-file
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Legend CreateLegend(string description)
        {
            return IDFLegend.CreateLegend(description, this.MinValue, this.MaxValue);
        }

        /// <summary>
        /// Retrieves number of cells that have a non-nodata value other than zero. Overridden from base class.
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            return RetrieveValueCount(0f, true);
        }

        /// <summary>
        /// Retrieves number of cells that have a non-nodata value
        /// </summary>
        /// <returns></returns>
        public long RetrieveValueCount()
        {
            return RetrieveValueCount(NoDataValue, true);
        }

        /// <summary>
        /// Retrieves number of cells that have the specified value
        /// </summary>
        /// <param name="value">searched value</param>
        /// <param name="isInverted">if true the specified value and NoData-value will be skipped from cell count</param>
        /// <returns></returns>
        public long RetrieveValueCount(float value, bool isInverted = false)
        {
            bool isLazyLoaded = false;
            if (values == null)
            {
                // Force (lazy) load of values
                EnsureLoadedValues();
                isLazyLoaded = true;
            }

            long valueCount = 0;

            if (values != null)
            {
                float cellValue;

                if (!isInverted)
                {
                    for (int rowidx = 0; rowidx < NRows; rowidx++)
                    {
                        for (int colidx = 0; colidx < NCols; colidx++)
                        {
                            cellValue = values[rowidx][colidx];
                            if (cellValue.Equals(value))
                            {
                                valueCount++;
                            }
                        }
                    }
                }
                else
                {
                    for (int rowidx = 0; rowidx < NRows; rowidx++)
                    {
                        for (int colidx = 0; colidx < NCols; colidx++)
                        {
                            cellValue = values[rowidx][colidx];
                            if (!cellValue.Equals(NoDataValue) && !cellValue.Equals(value))
                            {
                                valueCount++;
                            }
                        }
                    }
                }
            }

            if (isLazyLoaded)
            {
                ReleaseMemory(true);
            }

            return valueCount;
        }

        /// <summary>
        /// Retrieves list of all values in IDF-file. NoData is ignored. Values are loaded when not yet in memory
        /// </summary>
        /// <returns></returns>
        public List<float> RetrieveValues()
        {
            List<float> selValues = new List<float>();

            EnsureLoadedValues();

            if (values != null)
            {
                float cellValue;

                for (int rowidx = 0; rowidx < NRows; rowidx++)
                {
                    for (int colidx = 0; colidx < NCols; colidx++)
                    {
                        cellValue = values[rowidx][colidx];
                        if (!cellValue.Equals(NoDataValue))
                        {
                            selValues.Add(cellValue);
                        }
                    }
                }
            }

            return selValues;

        }

        /// <summary>
        /// Retrieves list of unique values in IDF-file. NoData is ignored.
        /// </summary>
        /// <returns></returns>
        public List<float> RetrieveUniqueValues()
        {
            HashSet<float> uniqueValues = new HashSet<float>();

            bool isLazyLoaded = false;
            if (values == null)
            {
                // Force (lazy) load of values
                EnsureLoadedValues();
                isLazyLoaded = true;
            }

            if (values != null)
            {
                float cellValue;

                for (int rowidx = 0; rowidx < NRows; rowidx++)
                {
                    for (int colidx = 0; colidx < NCols; colidx++)
                    {
                        cellValue = values[rowidx][colidx];
                        if (!uniqueValues.Contains(cellValue) && !cellValue.Equals(NoDataValue))
                        {
                            uniqueValues.Add(cellValue);
                        }
                    }
                }
            }

            if (isLazyLoaded)
            {
                ReleaseMemory(true);
            }
            return uniqueValues.ToList();
        }

        /// <summary>
        /// Retrieves dictionary with unique values and counts for IDF-file. NoData is ignored.
        /// </summary>
        /// <returns></returns>
        public Dictionary<float, long> RetrieveUniqueValueStats()
        {
            HashSet<float> uniqueValues = new HashSet<float>();
            Dictionary<float, long> uniqueValueCountDictionary = new Dictionary<float, long>();

            bool isLazyLoaded = false;
            if (values == null)
            {
                // Force (lazy) load of values
                EnsureLoadedValues();
                isLazyLoaded = true;
            }

            if (values != null)
            {
                float cellValue;

                for (int rowidx = 0; rowidx < NRows; rowidx++)
                {
                    for (int colidx = 0; colidx < NCols; colidx++)
                    {
                        cellValue = values[rowidx][colidx];
                        if (!cellValue.Equals(NoDataValue))
                        {
                            if (!uniqueValues.Contains(cellValue))
                            {
                                uniqueValues.Add(cellValue);
                                uniqueValueCountDictionary.Add(cellValue, 1);
                            }
                            else
                            {
                                uniqueValueCountDictionary[cellValue]++;
                            }
                        }
                    }
                }
            }

            if (isLazyLoaded)
            {
                ReleaseMemory(true);
            }
            return uniqueValueCountDictionary;
        }

        /// <summary>
        /// Releases memory for lazy loaded values. Should only be called after values have been written.
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (UseLazyLoading)
            {
                values = null;
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// Initialize empty IDF-file object with specified parameters
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected void Initialize(string filename, Extent extent, float xCellsize, float yCellsize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
        {
            int nrows = (int)Math.Ceiling((extent.ury - extent.lly) / yCellsize);
            int ncols = (int)Math.Ceiling((extent.urx - extent.llx) / xCellsize);

            Initialize(filename, extent, nrows, ncols, xCellsize, yCellsize, noDataValue, useLazyLoading, log, logIndentLevel);
        }

        /// <summary>
        /// Initialize empty IDF-file object with specified parameters
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="nrows"></param>
        /// <param name="ncols"></param>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected void Initialize(string filename, Extent extent, int nrows, int ncols, float xCellsize, float yCellsize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
        {
            this.Filename = filename;

            this.extent = (extent != null) ? extent.Copy() : null;
            this.fileExtent = null;
            this.modifiedExtent = null;
            this.NRows = nrows;
            this.NCols = ncols;
            this.XCellsize = xCellsize;
            this.YCellsize = yCellsize;

            this.NoDataValue = noDataValue;
            this.NoDataCalculationValue = float.NaN;
            this.MinValue = float.MaxValue;
            this.MaxValue = float.MinValue;

            this.TOPLevel = float.NaN;
            this.BOTLevel = float.NaN;

            this.UseLazyLoading = useLazyLoading;
            this.Log = log;
            this.LogIndentLevel = logIndentLevel;
            this.Metadata = null;

            if (!useLazyLoading)
            {
                DeclareValuesMemory();
            }
            else
            {
                // leave values null
                values = null;
            }
        }

        /// <summary>
        /// Declare memory for values of this IDF-file object based on defined number of rows and columns. Cells will get default float value (0).
        /// </summary>
        public void DeclareValuesMemory()
        {
            // declare memory for values
            if (NRows > 0)
            {
                values = new float[NRows][];
                for (int i = 0; i < NRows; i++)
                {
                    values[i] = new float[NCols];
                }
            }
            else if ((NRows == 0) && (NCols == 0))
            {
                values = null; // new float[0][];
            }
            else
            {
                throw new Exception("Invalid rowcount: " + NRows);
            }
        }

        /// <summary>
        /// Check if current IDF-file is read from a double precision IDF-file
        /// </summary>
        /// <returns></returns>
        public bool IsDoublePrecisionFile()
        {
            return ((idfRecordLength == 2295) || (idfRecordLength == 2296));
        }

        /// <summary>
        /// Create default filename for some operation with two IDF-files
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="op">operator symbol</param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        protected static string CreateFilename(IDFFile idfFile1, string op, IDFFile idfFile2)
        {
            string filename = string.Empty;

            string filename1 = string.Empty;
            if (idfFile1 != null)
            {
                filename1 = "IDFFILE1.IDF";
                if (idfFile1.Filename != null)
                {
                    filename1 = idfFile1.Filename;
                }
            }

            string filename2 = string.Empty;
            if (idfFile2 != null)
            {
                filename2 = "IDFFILE2.IDF";
                if (idfFile2.Filename != null)
                {
                    filename2 = idfFile2.Filename;
                }
            }

            filename = filename1;
            if (filename.Length > 259)
            {
                filename = filename.Substring(0, 259);
            }

            if (idfFile2 != null)
            {
                string path = Path.GetDirectoryName(filename);
                if (path.Equals(string.Empty))
                {
                    path = Path.GetDirectoryName(filename2);
                }
                filename = Path.Combine(path, Path.GetFileNameWithoutExtension(filename)
                            + op + Path.GetFileNameWithoutExtension(filename2) + Path.GetExtension(filename));
            }
            if (filename.Length > 259)
            {
                filename = filename.Substring(0, 259);
            }

            return filename;
        }

        /// <summary>
        /// Creates a default filename for "idffile 'op' value", with 'op' some operation
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="op"></param>
        /// <param name="value"></param>
        /// <returns>null when Filename of input IDF-file is null</returns>
        private static string CreateFilename(IDFFile idfFile, string op, float value)
        {
            string filename = null;
            if (idfFile.Filename != null)
            {
                filename = Path.Combine(Path.GetDirectoryName(idfFile.Filename), Path.GetFileNameWithoutExtension(idfFile.Filename) + op + value.ToString("F3", EnglishCultureInfo));
                if (filename.Length > 259)
                {
                    filename = filename.Substring(0, 259);
                }
            }

            return filename;
        }

        /// <summary>
        /// Creates a default filename for "value 'op' idfFile", with 'op' some operation
        /// </summary>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <param name="idfFile"></param>
        /// <returns></returns>
        protected static string CreateFilename(float value, string op, IDFFile idfFile)
        {
            string filename = Path.Combine(Path.GetDirectoryName(idfFile.Filename), value.ToString("F3", EnglishCultureInfo) + "op" + Path.GetFileNameWithoutExtension(idfFile.Filename));
            if (filename.Length > 259)
            {
                filename = filename.Substring(0, 259);
            }
            return filename;
        }

        /// <summary>
        /// Creates a default filename for "a * idfFile1 + b" operation
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected static string CreateFilename(IDFFile idfFile, float a, float b)
        {
            string filename = string.Empty;

            string filename1 = string.Empty;
            if (idfFile != null)
            {
                filename1 = "IDFFILE1.IDF";
                if (idfFile.Filename != null)
                {
                    filename1 = idfFile.Filename;
                }
            }

            filename = filename1;
            if (filename.Length > 259)
            {
                filename = filename.Substring(0, 259);
            }
            string path = Path.GetDirectoryName(filename);
            filename = Path.Combine(path, Path.GetFileNameWithoutExtension(filename) + (a.Equals(0f) ? string.Empty : "x" + a.ToString(EnglishCultureInfo))
                + ((b > 0) ? "+" + b.ToString(EnglishCultureInfo) : ((b < 0) ? "-" + (-1 * b).ToString(EnglishCultureInfo) : string.Empty))
                            + Path.GetExtension(filename));
            if (filename.Length > 259)
            {
                filename = filename.Substring(0, 259);
            }
            return filename;
        }

        /// <summary>
        /// Create an IDFLegend as a copy of the exiting legend from either idfFile1 or idfFile2
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="idfFile2"></param>
        /// <returns></returns>
        protected static Legend CreateLegend(IDFFile idfFile1, IDFFile idfFile2)
        {
            Legend legend = null;

            if ((idfFile1 != null) && (idfFile1.Legend != null))
            {
                legend = idfFile1.Legend.Copy();
            }

            if ((legend == null) && (idfFile2 != null) && (idfFile2.Legend != null))
            {
                legend = idfFile2.Legend.Copy();
            }

            return legend;
        }

        /// <summary>
        /// Force values of IDF-file to be actually loaded. This may be necessary if lazy loading is used and reference of Values property is not desired.
        /// </summary>
        public virtual void EnsureLoadedValues()
        {
            if (values == null)
            {
                LoadValues();
            }
        }

        /// <summary>
        /// Check if normal IDF-file has properties of a ConstantIDFFile (i.e. one cell, large cellsize)
        /// </summary>
        /// <returns></returns>
        protected bool IsConstantIDFFile()
        {
            // Check if IDF-file has one single cell with a large cellsize
            ConstantIDFFile testConstantIDFFile = new ConstantIDFFile(0);
            return (NRows == 1) && (NCols == 1) && (XCellsize >= testConstantIDFFile.XCellsize) && (YCellsize >= testConstantIDFFile.YCellsize);
        }

        /// <summary>
        /// Read a given IDF-file into this IDFFile object
        /// </summary>
        protected void ReadIDFFile(string filename, Extent clipExtent = null)
        {
            if (!File.Exists(filename))
            {
                throw new Exception("IDF-file does not exist: " + filename);
            }

            this.Filename = filename;
            Stream stream = null;
            BinaryReader br = null;

            try
            {
                stream = File.OpenRead(filename);
                br = new BinaryReader(stream);

                // Read Definitions
                ReadDefinitions(br);

                // If defined for this method, store clipExtent, so it can be used later when lazy loading
                if (clipExtent != null)
                {
                    this.modifiedExtent = clipExtent;
                }

                if (NoDataValue.Equals(float.NaN) && (Log != null))
                {
                    Log.AddWarning("NoData-value of IDF-file '" + Path.GetFileName(filename) + "'is NaN, this may produce unexpected results!");
                }

                if (!UseLazyLoading)
                {
                    // When lazy loading is not used, load values immediately
                    ReadValues(br);
                }
                else
                {
                    // Otherwise, set/leave values unloaded
                    values = null;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file while reading header of " + filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read IDF-file " + filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
            }
        }

        /// <summary>
        /// Load values for this IDFFile instance from the defined file. Note: file will be clipped if modifiedExtent is defined and different from fileExtent
        /// </summary>
        protected virtual void LoadValues()
        {
            if (Log != null)
            {
                Log.AddMessage(LogLevel.Debug, "Lazy load of IDF-values for file: " + Filename, 1);
            }

            if (Filename != null)
            {
                if (!File.Exists(Filename))
                {
                    throw new ToolException("IDF-file does not exist: " + Filename);
                }

                Stream stream = null;
                BinaryReader br = null;

                try
                {
                    stream = File.OpenRead(Filename);
                    br = new BinaryReader(stream);

                    // Definitions have been read already, read again to get NCols/NRows that correspond with value array in file. 
                    // Note: modifiedExtent is not reset and after reading, the values may get clipped again if modifiedExtent differs from Extent or fileExtent,
                    ReadDefinitions(br);

                    ReadValues(br);
                }
                catch (EndOfStreamException ex)
                {
                    throw new Exception("Unexpected end of file while reading IDF-file " + Filename, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read IDF-file " + Filename, ex);
                }
                finally
                {
                    if (br != null)
                    {
                        br.Close();
                    }
                }
            }
            else
            {
                DeclareValuesMemory();
            }

            if (Log != null)
            {
                Log.AddMessage(LogLevel.Debug, "Allocated memory after loading values: " + GC.GetTotalMemory(true) / 1000000 + "Mb", 1);
            }
        }

        /// <summary>
        /// Read IDF-file definitions from an initialized BinaryReader object, but skip storing of values in this IDF-file object
        /// </summary>
        /// <param name="br"></param>
        protected void SkipDefinitions(BinaryReader br)
        {
            if (!IsDoublePrecisionFile())
            {
                int idfRecordLength = br.ReadInt32();
                int ncols = br.ReadInt32();
                int nrows = br.ReadInt32();
                float llx = (float)br.ReadSingle();
                float urx = (float)br.ReadSingle();
                float lly = (float)br.ReadSingle();
                float ury = (float)br.ReadSingle();
                float minValue = (float)br.ReadSingle();
                float maxValue = (float)br.ReadSingle();
                float noDataValue = (float)br.ReadSingle();
                int ieq = br.ReadByte();
                byte itb = br.ReadByte();
                br.ReadByte();
                br.ReadByte();
                float xCellsize = (float)br.ReadSingle();
                float yCellsize = (float)br.ReadSingle();
                if (itb == 1)
                {
                    TOPLevel = (float)br.ReadSingle();
                    BOTLevel = (float)br.ReadSingle();
                }
            }
            else
            {
                int idfRecordLength = (int)br.ReadInt64();
                int ncols = (int)br.ReadInt64();
                int nrows = (int)br.ReadInt64();
                float llx = (float)br.ReadDouble();
                float urx = (float)br.ReadDouble();
                float lly = (float)br.ReadDouble();
                float ury = (float)br.ReadDouble();
                float minValue = (float)br.ReadDouble();
                float maxValue = (float)br.ReadDouble();
                float noDataValue = (float)br.ReadDouble();
                int ieq = (int)br.ReadByte();
                byte itb = br.ReadByte();
                br.ReadByte();
                br.ReadByte();
                br.ReadSingle();
                float xCellsize = (float)br.ReadDouble();
                float yCellsize = (float)br.ReadDouble();
                if (itb == 1)
                {
                    TOPLevel = (float)br.ReadDouble();
                    BOTLevel = (float)br.ReadDouble();
                }
            }
        }

        /// <summary>
        /// Read IDF-file definitions from an initialized BinaryReader object and store values in this IDF-file object
        /// </summary>
        /// <param name="br"></param>
        protected void ReadDefinitions(BinaryReader br)
        {
            idfRecordLength = br.ReadInt32();
            if (idfRecordLength == 1271)
            {
                // Single precision IDF

                NCols = br.ReadInt32();
                NRows = br.ReadInt32();
                float llx = (float)br.ReadSingle();
                float urx = (float)br.ReadSingle();
                float lly = (float)br.ReadSingle();
                float ury = (float)br.ReadSingle();
                fileExtent = new Extent(llx, lly, urx, ury);
                extent = fileExtent.Copy();  // Leave clipextent as it might have been set beforehand and will be used when reading the values
                MinValue = (float)br.ReadSingle();
                MaxValue = (float)br.ReadSingle();
                NoDataValue = (float)br.ReadSingle();
                byte ieq = br.ReadByte();
                if (ieq != 0)
                {
                    throw new ToolException("Currently only equidistant IDFs can be read (IEQ=0). IDF-file cannot be read for IEQ-value " + ieq + ": " + Filename);
                }
                byte itb = br.ReadByte();
                br.ReadByte();
                br.ReadByte();

                XCellsize = (float)br.ReadSingle();
                YCellsize = (float)br.ReadSingle();

                if (itb == 1)
                {
                    TOPLevel = (float)br.ReadSingle();
                    BOTLevel = (float)br.ReadSingle();
                }

                // Check that ncols,nrows match extent and cellsize
                int nrows2 = (int)Math.Ceiling((extent.ury - extent.lly) / YCellsize);
                int ncols2 = (int)Math.Ceiling((extent.urx - extent.llx) / XCellsize);
                if ((nrows2 != NRows) || (ncols2 != NCols))
                {
                    throw new ToolException("IDF-definition is inconsistent: defined nrows,ncols: (" + NRows + "," + NCols + "); calculated nrows,ncols based on extent and cellsize: (" + nrows2 + "," + ncols2 + "), for IDF-file: " + this.Filename);
                }
            }
            else if ((idfRecordLength == 2295) || (idfRecordLength == 2296))
            {
                // Double precision IDF

                // Skip remaining 4 bytes
                br.ReadBytes(4);
                NCols = (int)br.ReadInt64();
                NRows = (int)br.ReadInt64();
                float llx = (float)br.ReadDouble();
                float urx = (float)br.ReadDouble();
                float lly = (float)br.ReadDouble();
                float ury = (float)br.ReadDouble();
                fileExtent = new Extent(llx, lly, urx, ury);
                extent = fileExtent.Copy();  // Leave clipextent as it might have been set beforehand and will be used when reading the values
                MinValue = (float)br.ReadDouble();
                MaxValue = (float)br.ReadDouble();
                NoDataValue = (float)br.ReadDouble();
                byte ieq = br.ReadByte();
                if (ieq != 0)
                {
                    throw new ToolException("Currently only equidistant IDFs can be read (IEQ=0). IDF-file cannot be read for IEQ-value " + ieq + ": " + Filename);
                }
                byte itb = br.ReadByte();
                br.ReadByte();
                br.ReadByte();
                br.ReadSingle();

                XCellsize = (float)br.ReadDouble();
                YCellsize = (float)br.ReadDouble();

                if (itb == 1)
                {
                    TOPLevel = (float)br.ReadDouble();
                    BOTLevel = (float)br.ReadDouble();
                }

                // Check that ncols,nrows match extent and cellsize
                int nrows2 = (int)Math.Ceiling((extent.ury - extent.lly) / YCellsize);
                int ncols2 = (int)Math.Ceiling((extent.urx - extent.llx) / XCellsize);
                if ((nrows2 != NRows) || (ncols2 != NCols))
                {
                    throw new ToolException("IDF-definition is inconsistent: defined nrows,ncols: (" + NRows + "," + NCols + "); calculated nrows,ncols based on extent and cellsize: (" + nrows2 + "," + ncols2 + "), for IDF-file: " + this.Filename);
                }
            }
            else
            {
                throw new ToolException("Unknown IDF-format (" + idfRecordLength + ") not supported for IDF-file: " + this.Filename);
            }
        }

        /// <summary>
        /// Read IDF-file values into memory from an initialized BinaryReader object from which IDF-file definitions have been read already and which currently points to the values part.
        /// After reading values from file, the IDF-file will be clipped in memory when current modifiedExtent differs from fileExtent.
        /// </summary>
        /// <param name="br"></param>
        protected void ReadValues(BinaryReader br)
        {
            int colIdx = 0;
            int rowIdx = 0;

            try
            {
                // declare memory for values
                values = new float[NRows][];
                for (int i = 0; i < NRows; i++)
                {
                    values[i] = new float[NCols];
                }

                float newMinValue = float.MaxValue;
                float newMaxValue = float.MinValue;
                float value = float.NaN;

                if (IsDoublePrecisionFile())
                {
                    // Seperate while-loop for faster reading
                    while ((colIdx < NCols) && (rowIdx < NRows))
                    {
                        value = (float)br.ReadDouble();
                        values[rowIdx][colIdx] = value;

                        // update min/max
                        if (!value.Equals(NoDataValue))
                        {
                            if (value > newMaxValue)
                            {
                                newMaxValue = value;
                            }
                            if (value < newMinValue)
                            {
                                newMinValue = value;
                            }
                        }

                        colIdx++;
                        if (colIdx == NCols)
                        {
                            colIdx = 0;
                            rowIdx++;
                        }
                    }
                }
                else
                {
                    // Seperate while-loop for faster reading
                    while ((colIdx < NCols) && (rowIdx < NRows))
                    {
                        value = (float)br.ReadSingle();
                        Values[rowIdx][colIdx] = value;

                        // update min/max
                        if (!value.Equals(NoDataValue))
                        {
                            if (value > newMaxValue)
                            {
                                newMaxValue = value;
                            }
                            if (value < newMinValue)
                            {
                                newMinValue = value;
                            }
                        }

                        colIdx++;
                        if (colIdx == NCols)
                        {
                            colIdx = 0;
                            rowIdx++;
                        }
                    }
                }
                MinValue = newMinValue;
                MaxValue = newMaxValue;

                if ((modifiedExtent != null) && !modifiedExtent.Contains(extent))
                {
                    // If a clipextent is defined and is smaller than the IDF-extent, clip the IDF-file
                    IDFFile clippedIDFFile = this.ClipIDF(modifiedExtent);
                    if (clippedIDFFile != null)
                    {
                        this.NCols = clippedIDFFile.NCols;
                        this.NRows = clippedIDFFile.NRows;
                        this.values = clippedIDFFile.values;
                        this.MinValue = clippedIDFFile.MinValue;
                        this.MaxValue = clippedIDFFile.MaxValue;
                        this.extent = clippedIDFFile.extent.Copy();
                        this.modifiedExtent = modifiedExtent.Copy();
                    }
                    else
                    {
                        this.NCols = 0;
                        this.NRows = 0;
                        this.values = null;
                        this.MinValue = float.NaN;
                        this.MaxValue = float.NaN;
                        this.extent = null;
                        this.modifiedExtent = null;
                    }
                }
                else if (!extent.Equals(modifiedExtent) && extent.Contains(modifiedExtent))
                {
                    // If a clipextent is defined and is larger than the IDF-extent, enlarge the IDF-file
                    IDFFile enlargedIDFFile = this.EnlargeIDF(modifiedExtent);
                    this.NCols = enlargedIDFFile.NCols;
                    this.NRows = enlargedIDFFile.NRows;
                    this.values = enlargedIDFFile.values;
                    this.MinValue = enlargedIDFFile.MinValue;
                    this.MaxValue = enlargedIDFFile.MaxValue;
                    this.extent = enlargedIDFFile.extent.Copy();
                    this.modifiedExtent = modifiedExtent.Copy();
                }
            }
            catch (EndOfStreamException ex)
            {
                if ((colIdx > 0) || (rowIdx > 0))
                {
                    throw new Exception("Unexpected end of file while reading row " + rowIdx + " and column " + colIdx, ex);
                }
            }
        }

        /// <summary>
        /// Retrieve extent around around all non NoData-values or around specified selection values
        /// </summary>
        /// <param name="selectionValues"></param>
        /// <param name="isExcluded"></param>
        /// <returns></returns>
        public Extent RetrieveExtent(List<float> selectionValues = null, bool isExcluded = false)
        {
            float llx = float.NaN;
            float lly = float.NaN;
            float urx = float.NaN;
            float ury = float.NaN;
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                float y = GetY(rowIdx);
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    float x = GetX(colIdx);
                    float value = values[rowIdx][colIdx];
                    if (selectionValues == null)
                    {
                        if (!value.Equals(NoDataValue))
                        {
                            if (llx.Equals(float.NaN))
                            {
                                llx = x;
                                lly = y;
                                urx = x;
                                ury = y;
                            }
                            else
                            {
                                // use inverted expressions to deal with initial NaN-values
                                if (!(x >= llx))
                                {
                                    llx = x;
                                }
                                else if (!(x <= urx))
                                {
                                    urx = x;
                                }
                                if (!(y >= lly))
                                {
                                    lly = y;
                                }
                                else if (!(y <= ury))
                                {
                                    ury = y;
                                }
                            }
                        }
                    }
                    else if (selectionValues.Contains(value))
                    {
                        if (!isExcluded)
                        {
                            if (llx.Equals(float.NaN))
                            {
                                llx = x;
                                lly = y;
                                urx = x;
                                ury = y;
                            }
                            else
                            {
                                // use inverted expressions to deal with initial NaN-values
                                if (!(x >= llx))
                                {
                                    llx = x;
                                }
                                else if (!(x <= urx))
                                {
                                    urx = x;
                                }
                                if (!(y >= lly))
                                {
                                    lly = y;
                                }
                                else if (!(y <= ury))
                                {
                                    ury = y;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (isExcluded)
                        {
                            if (llx.Equals(float.NaN))
                            {
                                llx = x;
                                lly = y;
                                urx = x;
                                ury = y;
                            }
                            else
                            {
                                // use inverted expressions to deal with initial NaN-values
                                if (!(x >= llx))
                                {
                                    llx = x;
                                }
                                else if (!(x <= urx))
                                {
                                    urx = x;
                                }
                                if (!(y >= lly))
                                {
                                    lly = y;
                                }
                                else if (!(y <= ury))
                                {
                                    ury = y;
                                }
                            }
                        }
                    }
                }
            }

            if (urx.Equals(float.NaN))
            {
                urx = llx;
            }
            if (ury.Equals(float.NaN))
            {
                ury = lly;
            }

            return new Extent(llx - XCellsize / 2f, lly - YCellsize / 2f, urx + XCellsize / 2f, ury + YCellsize / 2f);
        }

        /// <summary>
        /// Update min and max values of this IDF-file based on an old and new value for some IDF-cell.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="isGlobalUpdateAllowed">if true, a global update is performed when the old value was the min/max-value</param>
        /// <returns>true if succesful; if a global update is needed but not allowed false is returned</returns>
        public virtual bool UpdateMinMaxValue(float oldValue, float newValue, bool isGlobalUpdateAllowed = true)
        {
            if (oldValue.Equals(NoDataValue) || ((oldValue > MinValue) && (oldValue < MaxValue)))
            {
                // The old value is not equal to the min or maximum value, just update with new value
                UpdateMinMaxValue(newValue);
                return true;
            }
            else if (isGlobalUpdateAllowed)
            {
                // The old value is equal to the current min or maximum, do a full update
                UpdateMinMaxValue();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Update min and max values of this IDF-file. Note: it is assumed that old cellvalue(s) are NoData or smaller than the current min/max-value
        /// </summary>
        /// <param name="value"></param>
        private void UpdateMinMaxValue(float value)
        {
            if (!value.Equals(NoDataValue))
            {
                if (value > MaxValue)
                {
                    MaxValue = value;
                }
                if (value < MinValue)
                {
                    MinValue = value;
                }
            }
        }

        /// <summary>
        /// Update min- and max-value of this IDF-file for current cell values. 
        /// </summary>
        public virtual void UpdateMinMaxValue()
        {
            MinValue = float.MaxValue;
            MaxValue = float.MinValue;

            // force values to be loaded
            EnsureLoadedValues();

            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    float value = values[rowIdx][colIdx];
                    if (!value.Equals(NoDataValue))
                    {
                        if (value > MaxValue)
                        {
                            MaxValue = value;
                        }
                        if (value < MinValue)
                        {
                            MinValue = value;
                        }
                    }
                }
            }

            if (MinValue.Equals(float.MaxValue) && MaxValue.Equals(float.MinValue))
            {
                // No non-NoData values found
                MinValue = NoDataValue;
                MaxValue = NoDataValue;
            }
        }

        /// <summary>
        /// Buffers cells with given value: for each cell with the specified value, 
        /// all cells within the specified buffersize are set to this buffered value.
        /// </summary>
        /// <param name="bufferedValue"></param>
        /// <param name="buffersize"></param>
        /// <returns></returns>
        public IDFFile Buffer(float bufferedValue, float buffersize)
        {
            IDFFile bufferedIDFFile = this.CopyIDF(Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + "_buffer" + buffersize + ".IDF"));
            //            IDFFile loopIDFFile = this.CopyIDF(string.Empty);

            int firstRowIdx = GetRowIdx(Extent.ury);
            int firstColIdx = GetColIdx(Extent.llx);
            int lastRowIdx = GetRowIdx(Extent.lly) - 1;
            int lastColIdx = GetColIdx(Extent.urx) - 1;

            float bufferCellDistance = buffersize / XCellsize;
            long bufferSqrCellDistance = (long)(bufferCellDistance * bufferCellDistance);

            for (int rowIdx = 0; rowIdx <= lastRowIdx; rowIdx++)
            {
                for (int colIdx = 0; colIdx <= lastColIdx; colIdx++)
                {
                    if (values[rowIdx][colIdx].Equals(bufferedValue))
                    {
                        // Loop through (buffersize x buffersize)-matrix around specified cell
                        int minRowIdx = (int)(rowIdx - bufferCellDistance);
                        if (minRowIdx < 0)
                        {
                            minRowIdx = 0;
                        }
                        int maxRowIdx = (int)(rowIdx + bufferCellDistance);
                        if (maxRowIdx > NRows)
                        {
                            maxRowIdx = NRows;
                        }
                        int minColIdx = (int)(colIdx - bufferCellDistance);
                        if (minColIdx < 0)
                        {
                            minColIdx = 0;
                        }
                        int maxColIdx = (int)(colIdx + bufferCellDistance);
                        if (maxColIdx > NCols)
                        {
                            maxColIdx = NCols;
                        }

                        for (int bufferRowIdx = minRowIdx; bufferRowIdx < maxRowIdx; bufferRowIdx++)
                        {
                            for (int bufferColIdx = minColIdx; bufferColIdx < maxColIdx; bufferColIdx++)
                            {
                                long dRow = (bufferRowIdx - rowIdx);
                                long dCol = (bufferColIdx - colIdx);
                                if ((dRow * dRow + dCol * dCol) <= bufferSqrCellDistance)
                                {
                                    bufferedIDFFile.values[bufferRowIdx][bufferColIdx] = bufferedValue;
                                }
                            }
                        }
                    }
                }
            }
            return bufferedIDFFile;
        }

        private float GetMostOccurringValue(List<float> valueList)
        {
            Dictionary<float, long> valueCountDictionary = new Dictionary<float, long>();
            foreach (float value in valueList)
            {
                if (!valueCountDictionary.ContainsKey(value))
                {
                    valueCountDictionary.Add(value, 1);
                }
                else
                {
                    valueCountDictionary[value]++;
                }
            }
            float mostOccurringValue = float.NaN;
            float mostOccurringValueCount = 0;
            foreach (float value in valueCountDictionary.Keys)
            {
                if (valueCountDictionary[value] > mostOccurringValueCount)
                {
                    mostOccurringValue = value;
                    mostOccurringValueCount = valueCountDictionary[value];
                }
            }
            return mostOccurringValue;
        }

        /// <summary>
        /// Select most occurring negative value, above most occurring positive value, above zero values
        /// </summary>
        /// <param name="valueList"></param>
        /// <returns></returns>
        private float GetBoundaryValue(List<float> valueList)
        {
            bool hasNegativeValues = false;
            bool hasPositiveValues = false;
            List<float> selValueList = new List<float>();
            List<float> selNegValueList = new List<float>();
            List<float> selPosValueList = new List<float>();
            foreach (float value in valueList)
            {
                if (value < 0)
                {
                    hasNegativeValues = true;
                    selNegValueList.Add(value);
                }
                else if (value > 0)
                {
                    hasPositiveValues = true;
                    selPosValueList.Add(value);
                }
            }
            if (hasNegativeValues)
            {
                selValueList = selNegValueList;
            }
            else if (hasPositiveValues)
            {
                selValueList = selPosValueList;
            }
            else
            {
                return 0;
            }

            // Select most occuring negatieve or positive value
            Dictionary<float, long> valueCountDictionary = new Dictionary<float, long>();
            foreach (float value in selValueList)
            {
                if (!valueCountDictionary.ContainsKey(value))
                {
                    valueCountDictionary.Add(value, 1);
                }
                else
                {
                    valueCountDictionary[value]++;
                }
            }
            float mostOccurringValue = float.NaN;
            float mostOccurringValueCount = 0;
            foreach (float value in valueCountDictionary.Keys)
            {
                if (valueCountDictionary[value] > mostOccurringValueCount)
                {
                    mostOccurringValue = value;
                    mostOccurringValueCount = valueCountDictionary[value];
                }
            }
            return mostOccurringValue;
        }

        /// <summary>
        /// Replace all cell values with NoData-value to float.NaN value
        /// </summary>
        /// <param name="idfFile"></param>
        public static void ReplaceNoDataWithNaN(IDFFile idfFile)
        {
            if (idfFile != null)
            {
                idfFile.ReplaceValues(idfFile.NoDataValue, float.NaN);
                idfFile.NoDataValue = float.NaN;
            }
        }

        /// <summary>
        /// Replace all cell values with NaN to NoData-value
        /// </summary>
        /// <param name="idfFile"></param>
        public static void ReplaceNaNWithNoData(IDFFile idfFile)
        {
            if (idfFile != null)
            {
                idfFile.ReplaceValues(float.NaN, NoDataDefaultValue);
                idfFile.NoDataValue = NoDataDefaultValue;
            }
        }

        /// <summary>
        /// Check if this IDF-file has NoData values
        /// </summary>
        /// <returns></returns>
        public bool HasNoDataValues()
        {
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if (values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if this IDF-file has non-NoData values
        /// </summary>
        /// <returns></returns>
        public bool HasDataValues()
        {
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if (!values[rowIdx][colIdx].Equals(NoDataValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if this IDF-file has values at or between the given min and max values
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public bool HasValuesBetween(float minValue, float maxValue)
        {
            EnsureLoadedValues();
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    if ((values[rowIdx][colIdx] >= minValue) && (values[rowIdx][colIdx] <= maxValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if this IDF-file has a value larger than the specified value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HasValueLargerThan(float value)
        {
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    float x = GetX(colIdx);
                    float y = GetY(rowIdx);

                    if (!values[rowIdx][colIdx].Equals(NoDataValue) && values[rowIdx][colIdx] > value)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks for equality at contentlevel only within the given extent
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="comparedExtent">An optional extent to check equality within, use null for default (full extent)</param>
        /// <param name="isNoDataCompared"></param>
        /// <param name="isContentComparisonForced"></param>
        /// <returns></returns>
        public override bool HasEqualContent(IMODFile otherIMODFile, Extent comparedExtent, bool isNoDataCompared, bool isContentComparisonForced = false)
        {
            if (!(otherIMODFile is IDFFile))
            {
                return false;
            }

            IDFFile otherIDFFile = (IDFFile)otherIMODFile;
            if (!isContentComparisonForced && this.Equals(otherIDFFile))
            {
                return true;
            }

            EnsureLoadedValues();
            otherIDFFile.EnsureLoadedValues();

            // Determine comparison extent
            if (!((this.extent != null) && (otherIDFFile.Extent != null)))
            {
                // One or both files have a null extent
                if ((this.extent == null) && (otherIDFFile.Extent == null))
                {
                    // both files have no content, so equal...
                    return true;
                }
                else
                {
                    // Only have file has some content, so unequal
                    return false;
                }
            }
            else
            {
                if (comparedExtent == null)
                {
                    if (!this.extent.Equals(otherIDFFile.Extent))
                    {
                        // Both files have different extents so different content
                        return false;
                    }

                    // Both files have equal extent, continue with actual comparison below
                }
                else
                {
                    // Clip both IDF-files to compared extent
                    Extent comparedExtent1 = this.Extent.Clip(comparedExtent);
                    Extent comparedExtent2 = otherIDFFile.Extent.Clip(comparedExtent);
                    if (!comparedExtent1.Equals(comparedExtent2))
                    {
                        // Both files differ in content, even within compared extent
                        return false;
                    }
                    comparedExtent = comparedExtent1;

                    if (!comparedExtent.IsValidExtent())
                    {
                        // The comparison extent has no overlap with the other extents, so within the comparison extent the files are actually equal
                        return true;
                    }

                    // A valid comparison extent remains, continue with actual comparison
                }
            }

            // Actually compare contents of both files
            return HasEqualValues(otherIDFFile, comparedExtent, isNoDataCompared);
        }

        /// <summary>
        /// Checks if this IDF-file has equal values as specified other IDF-file
        /// </summary>
        /// <param name="otherIDFFile"></param>
        /// <param name="comparedExtent"></param>
        /// <param name="isNoDataCompared"></param>
        /// <returns></returns>
        protected virtual bool HasEqualValues(IDFFile otherIDFFile, Extent comparedExtent, bool isNoDataCompared)
        {
            if (comparedExtent == null)
            {
                comparedExtent = this.extent.Copy();
            }

            if (HasEqualExtentAndCellsize(otherIDFFile))
            {
                // use fast comparison if extents and cellsizes are equal
                int firstRowIdx = GetRowIdx(comparedExtent.ury);
                int firstColIdx = GetColIdx(comparedExtent.llx);
                int lastRowIdx = GetRowIdx(comparedExtent.lly + 1);
                int lastColIdx = GetColIdx(comparedExtent.urx - 1);
                if (!isNoDataCompared)
                {
                    for (int rowIdx = firstRowIdx; rowIdx <= lastRowIdx; rowIdx++)
                    {
                        for (int colIdx = firstColIdx; colIdx <= lastColIdx; colIdx++)
                        {
                            try
                            {
                                float value = values[rowIdx][colIdx];
                                float otherValue = otherIDFFile.values[rowIdx][colIdx];
                                if (value.Equals(NoDataValue))
                                {
                                    if (!otherValue.Equals(otherIDFFile.NoDataValue))
                                    {
                                        return false;
                                    }
                                }
                                else if (!value.Equals(otherValue))
                                {
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Invalid indices: " + rowIdx + ", " + colIdx + " in IDF-files", ex);
                            }
                        }
                    }
                }
                else
                {
                    for (int rowIdx = firstRowIdx; rowIdx <= lastRowIdx; rowIdx++)
                    {
                        for (int colIdx = firstColIdx; colIdx <= lastColIdx; colIdx++)
                        {
                            try
                            {
                                float value = values[rowIdx][colIdx];
                                float otherValue = otherIDFFile.values[rowIdx][colIdx];
                                if (!value.Equals(otherValue))
                                {
                                    return false;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Invalid indices: " + rowIdx + ", " + colIdx + " in IDF-files", ex);
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                // use comparison that allows for different extent or cellsize

                float xStepsize = XCellsize;
                float yStepsize = YCellsize;
                if (xStepsize > otherIDFFile.XCellsize)
                {
                    xStepsize = otherIDFFile.XCellsize;
                }
                if (yStepsize > otherIDFFile.YCellsize)
                {
                    yStepsize = otherIDFFile.YCellsize;
                }

                float x = comparedExtent.llx;
                float y = comparedExtent.lly;
                if (!isNoDataCompared)
                {
                    while ((x >= comparedExtent.llx) && (x < comparedExtent.urx) && (y >= comparedExtent.lly) && (y < comparedExtent.ury))
                    {
                        float value = GetValue(x, y);
                        float otherValue = otherIDFFile.GetValue(x, y);
                        if (value.Equals(NoDataValue))
                        {
                            if (!otherValue.Equals(otherIDFFile.NoDataValue))
                            {
                                return false;
                            }
                        }
                        else if (!value.Equals(otherValue))
                        {
                            return false;
                        }

                        x += xStepsize;
                        if (x >= comparedExtent.urx)
                        {
                            x = comparedExtent.llx;
                            y += yStepsize;
                        }
                    }
                }
                else
                {
                    while ((x >= comparedExtent.llx) && (x < comparedExtent.urx) && (y >= comparedExtent.lly) && (y < comparedExtent.ury))
                    {
                        float value = GetValue(x, y);
                        float otherValue = otherIDFFile.GetValue(x, y);
                        if (!value.Equals(otherValue))
                        {
                            return false;
                        }

                        x += xStepsize;
                        if (x >= comparedExtent.urx)
                        {
                            x = comparedExtent.llx;
                            y += yStepsize;
                        }
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Scale IDF-file up from coarse to finer resolution
        /// </summary>
        /// <param name="cellSize"></param>
        /// <param name="upscaleMethod"></param>
        /// <param name="clipExtent">note: null is returned, if clipextent does not intersect with this IDF-extent</param>
        /// <param name="alignExtent"></param>
        /// <returns></returns>
        public IDFFile ScaleUp(float cellSize, UpscaleMethodEnum upscaleMethod, Extent clipExtent = null, Extent alignExtent = null)
        {
            if (cellSize < this.XCellsize)
            {
                throw new ToolException("Cannot upscale IDF from cellsize " + this.XCellsize + " to cellsize " + cellSize);
            }

            float fltScaleFactor = cellSize / this.XCellsize;
            if (!((float)Math.Truncate(fltScaleFactor)).Equals(fltScaleFactor))
            {
                throw new ToolException("Cannot upscale IDF from cellsize " + this.XCellsize + " to cellsize " + cellSize + ". Factor should be an integer.");
            }

            if ((clipExtent != null) && !this.Extent.Intersects(clipExtent))
            {
                return null;
            }

            int scaleFactor = (int)fltScaleFactor;

            EnsureLoadedValues();
            IDFFile fineIDF = this;
            Extent scaleExtent = this.extent;
            if (clipExtent != null)
            {
                // If a clip is defined, clip before scaling
                fineIDF = this.ClipIDF(clipExtent);
                scaleExtent = fineIDF.Extent;
            }

            // Correct for alignExtent
            if (alignExtent == null)
            {
                alignExtent = new Extent(0, 0, cellSize, cellSize);
            }

            scaleExtent.llx = scaleExtent.llx + (((cellSize - (scaleExtent.llx % cellSize)) + (alignExtent.llx % cellSize)) % cellSize);
            scaleExtent.lly = scaleExtent.lly + (((cellSize - (scaleExtent.lly % cellSize)) + (alignExtent.lly % cellSize)) % cellSize);
            scaleExtent.urx = scaleExtent.urx - (((scaleExtent.urx % cellSize) + (cellSize - (alignExtent.urx % cellSize))) % cellSize);
            scaleExtent.ury = scaleExtent.ury - (((scaleExtent.ury % cellSize) + (cellSize - (alignExtent.ury % cellSize))) % cellSize);

            int firstFineRowIdx = fineIDF.GetRowIdx(scaleExtent.ury);
            int firstFineColIdx = fineIDF.GetColIdx(scaleExtent.llx);
            int lastFineRowIdx = fineIDF.GetRowIdx(scaleExtent.lly) - 1;
            int lastFineColIdx = fineIDF.GetColIdx(scaleExtent.urx) - 1;

            int lastCoarseRowIdx = (int)((lastFineRowIdx - firstFineRowIdx + 1) / scaleFactor) - 1;
            int lastCoarseColIdx = (int)((lastFineColIdx - firstFineColIdx + 1) / scaleFactor) - 1;
            if ((lastCoarseRowIdx < 0) || (lastCoarseColIdx < 0))
            {
                throw new ToolException("Invalid extent used for scaling: " + scaleExtent.ToString());
            }
            string scaledFilename = Path.Combine(Path.GetDirectoryName(this.Filename), Path.GetFileNameWithoutExtension(this.Filename) + "_scaled" + ((int)cellSize) + Path.GetExtension(this.Filename));
            Extent coarseIDFExtent = new Extent(scaleExtent.llx, scaleExtent.lly, scaleExtent.llx + (lastCoarseColIdx + 1) * cellSize, scaleExtent.lly + (lastCoarseRowIdx + 1) * cellSize);
            IDFFile coarseIDF = new IDFFile(scaledFilename, coarseIDFExtent, cellSize, this.NoDataValue);

            List<float> valueList = new List<float>(scaleFactor * scaleFactor);
            for (int coarseRowIdx = 0; coarseRowIdx <= lastCoarseRowIdx; coarseRowIdx++)
            {
                for (int coarseColIdx = 0; coarseColIdx <= lastCoarseColIdx; coarseColIdx++)
                {
                    int fineBaseRowIdx = firstFineRowIdx + coarseRowIdx * scaleFactor;
                    int fineBaseColIdx = firstFineColIdx + coarseColIdx * scaleFactor;
                    valueList.Clear();
                    for (int subRowIdx = 0; subRowIdx < scaleFactor; subRowIdx++)
                    {
                        for (int subColIdx = 0; subColIdx < scaleFactor; subColIdx++)
                        {
                            int fineRowIdx = fineBaseRowIdx + subRowIdx;
                            int fineColIdx = fineBaseColIdx + subColIdx;
                            if ((fineRowIdx <= lastFineRowIdx) && (fineColIdx <= lastFineColIdx))
                            {
                                float value = fineIDF.Values[fineRowIdx][fineColIdx];
                                if (!value.Equals(fineIDF.NoDataValue) || upscaleMethod.Equals(UpscaleMethodEnum.MostOccurring))
                                {
                                    valueList.Add(value);
                                }
                            }
                        }
                    }
                    if (valueList.Count == 0)
                    {
                        coarseIDF.values[coarseRowIdx][coarseColIdx] = this.NoDataValue;
                    }
                    else
                    {
                        switch (upscaleMethod)
                        {
                            case UpscaleMethodEnum.Minimum:
                                valueList.Sort();
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = valueList[0];
                                break;
                            case UpscaleMethodEnum.Mean:
                            case UpscaleMethodEnum.Sum:
                                float sum = 0;
                                for (int i = 0; i < valueList.Count; i++)
                                {
                                    sum += valueList[i];
                                }
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = (upscaleMethod == UpscaleMethodEnum.Sum) ? sum : sum / valueList.Count;
                                break;
                            case UpscaleMethodEnum.Median:
                                valueList.Sort();
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = valueList[(int)((valueList.Count) / 2)];
                                break;
                            case UpscaleMethodEnum.Maximum:
                                valueList.Sort();
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = valueList[valueList.Count - 1];
                                break;
                            case UpscaleMethodEnum.MostOccurring:
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = GetMostOccurringValue(valueList);
                                break;
                            case UpscaleMethodEnum.Boundary:
                                coarseIDF.values[coarseRowIdx][coarseColIdx] = GetMostOccurringValue(valueList);
                                break;
                            default:
                                throw new Exception("Unknown upscale method " + upscaleMethod.ToString());
                        }
                    }
                }
            }
            return coarseIDF;
        }

        /// <summary>
        /// Scale IDF-file down from coarse to finer resolution
        /// </summary>
        /// <param name="cellSize"></param>
        /// <param name="downscaleMethod"></param>
        /// <param name="clipExtent"></param>
        /// <param name="alignExtent"></param>
        /// <returns></returns>
        public IDFFile ScaleDown(float cellSize, DownscaleMethodEnum downscaleMethod, Extent clipExtent = null, Extent alignExtent = null)
        {
            if (cellSize > this.XCellsize)
            {
                throw new ToolException("Cannot downscale IDF from cellsize " + this.XCellsize + " to cellsize " + cellSize);
            }

            float fltScaleFactor = this.XCellsize / cellSize;
            if (!((float)Math.Truncate(fltScaleFactor)).Equals(fltScaleFactor))
            {
                throw new ToolException("Cannot downscale IDF from cellsize " + this.XCellsize + " to cellsize " + cellSize + ". Factor should be an integer.");
            }
            int scaleFactor = (int)fltScaleFactor;

            EnsureLoadedValues();
            IDFFile courseIDF = this;
            Extent scaleExtent = this.extent;
            if (clipExtent != null)
            {
                // If a clip is defined, clip before scaling
                courseIDF = this.ClipIDF(clipExtent);
                scaleExtent = courseIDF.Extent;
            }

            // Correct for alignExtent
            if (alignExtent == null)
            {
                alignExtent = new Extent(0, 0, cellSize, cellSize);
            }

            scaleExtent.llx = scaleExtent.llx + (((cellSize - (scaleExtent.llx % cellSize)) + (alignExtent.llx % cellSize)) % cellSize);
            scaleExtent.lly = scaleExtent.lly + (((cellSize - (scaleExtent.lly % cellSize)) + (alignExtent.lly % cellSize)) % cellSize);
            scaleExtent.urx = scaleExtent.urx - (((scaleExtent.urx % cellSize) + (cellSize - (alignExtent.urx % cellSize))) % cellSize);
            scaleExtent.ury = scaleExtent.ury - (((scaleExtent.ury % cellSize) + (cellSize - (alignExtent.ury % cellSize))) % cellSize);

            int firstCourseRowIdx = courseIDF.GetRowIdx(scaleExtent.ury);
            int firstCourseColIdx = courseIDF.GetColIdx(scaleExtent.llx);
            int lastCourseRowIdx = courseIDF.GetRowIdx(scaleExtent.lly) - 1;
            int lastCourseColIdx = courseIDF.GetColIdx(scaleExtent.urx) - 1;

            int lastFineRowIdx = (int)((lastCourseRowIdx - firstCourseRowIdx + 1) * scaleFactor) - 1;
            int lastFineColIdx = (int)((lastCourseColIdx - firstCourseRowIdx + 1) * scaleFactor) - 1;
            if ((lastFineRowIdx < 0) || (lastFineColIdx < 0))
            {
                throw new ToolException("Invalid extent used for scaling: " + scaleExtent.ToString());
            }
            string scaledFilename = Path.Combine(Path.GetDirectoryName(this.Filename), Path.GetFileNameWithoutExtension(this.Filename) + "_scaled" + ((int)cellSize) + Path.GetExtension(this.Filename));
            Extent fineIDFExtent = new Extent(scaleExtent.llx, scaleExtent.lly, scaleExtent.llx + (lastFineColIdx + 1) * cellSize, scaleExtent.lly + (lastFineRowIdx + 1) * cellSize);
            IDFFile fineIDF = new IDFFile(scaledFilename, fineIDFExtent, cellSize, this.NoDataValue);
            fineIDF.SetITBLevels(TOPLevel, BOTLevel);

            int fineCellCount = scaleFactor * scaleFactor;
            for (int fineRowIdx = 0; fineRowIdx <= lastFineRowIdx; fineRowIdx++)
            {
                for (int fineColIdx = 0; fineColIdx <= lastFineColIdx; fineColIdx++)
                {
                    int courseBaseRowIdx = firstCourseRowIdx + (int)(fineRowIdx / scaleFactor);
                    int courseBaseColIdx = firstCourseColIdx + (int)(fineColIdx / scaleFactor);
                    switch (downscaleMethod)
                    {
                        case DownscaleMethodEnum.Block:
                            fineIDF.values[fineRowIdx][fineColIdx] = courseIDF.values[courseBaseRowIdx][courseBaseColIdx];
                            break;
                        case DownscaleMethodEnum.Divide:
                            fineIDF.values[fineRowIdx][fineColIdx] = courseIDF.values[courseBaseRowIdx][courseBaseColIdx] / fineCellCount;
                            break;
                        default:
                            throw new Exception("Unknown downscale method " + downscaleMethod.ToString());
                    }
                }
            }
            return fineIDF;
        }

        /// <summary>
        /// Correct MinValues/MaxValues of this IDFFile object to the NoData-value of this IDFFile object. Only correct when close to the specified NoData-value (when this is not equal to zero).
        /// Only correct when no values equal to the NoData-value of this IDFFile object are found. This correction can be useful if an extreme NoData-value is defined, but values are sligthly different somehow.
        /// </summary>
        /// <param name="noDataValueRatio">max difference between Min/MaxValue and specified approxNoDataValue, for correction</param>
        /// <param name="approxNoDataValue">value to compare values of IDFFile with, or IDFFile.NoDataValue if float.NaN was specified</param>
        public void CorrectNoDataValues(float noDataValueRatio = 1E-6f, double approxNoDataValue = float.NaN)
        {
            // Only correct when no values equal to NoData are found
            if (RetrieveValueCount(NoDataValue) == 0)
            {
                if (approxNoDataValue.Equals(float.NaN))
                {
                    // use NoData-value of this IDFFile
                    approxNoDataValue = NoDataValue;
                }

                // Do not correct when NoData is equal to zero, only correct for extreme negative/positive values
                if (!approxNoDataValue.Equals(0))
                {
                    if ((MinValue < 0) && (approxNoDataValue < 0))
                    {
                        // Check for extreme negative NoData-value 

                        // Check for small difference between NoData (which may be double.MinValue or double.MaxValue) and MinValue
                        double diff = Math.Max(Math.Abs(MinValue), Math.Abs(approxNoDataValue)) - Math.Min(Math.Abs(MinValue), Math.Abs(approxNoDataValue)); // Use max-min to avoid possible overflow
                        double ratio = diff / approxNoDataValue;
                        if (ratio < noDataValueRatio)
                        {
                            ReplaceValues(MinValue, NoDataValue);
                        }
                    }
                    else if ((MaxValue > 0) && (approxNoDataValue > 0))
                    {
                        // Check for extreme positive NoData-value 

                        // Check for small difference between NoData (which may be double.MinValue or double.MaxValue) and MinValue
                        double diff = Math.Max(MaxValue, approxNoDataValue) - Math.Min(MaxValue, approxNoDataValue); // Use max-min to avoid possible overflow
                        double ratio = diff / approxNoDataValue;
                        if (ratio < noDataValueRatio)
                        {
                            ReplaceValues(MaxValue, NoDataValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Correct boundary of this IDFFile using specified settings. The boundary is searched from the specified extent inwards.
        /// </summary>
        /// <param name="activeValue"></param>
        /// <param name="bndValue"></param>
        /// <param name="inactiveValue"></param>
        /// <param name="useOnlyActiveCells">if true, only active cells will be converted to boundary cells; if false, also boundary or inactive cells can be converted</param>
        /// <param name="keepInactiveCells">if true, inactive cells will not be changed to boundary cells</param>
        /// <param name="isDiagonallyChecked"></param>
        /// <param name="useGridEdge"></param>
        /// <param name="extent"></param>
        public IDFFile CorrectBoundary(float activeValue, float bndValue, float inactiveValue, Extent extent, bool useOnlyActiveCells = false, bool keepInactiveCells = false, bool isDiagonallyChecked = false, bool useGridEdge = false)
        {
            // Algorithm is as follows: all cells are visited from the outside edge to find specified type of boundary. 
            // 0. Use a Queue for all cells that currently still should be visited and need to be checked for a boundary condition
            //    Use a seperate grid for all cells that have already been visited. Visited cells will not be placed in the queue to visit again.
            // 1. Set all non NoData-cells in the input IDF-file as not visited.
            // 2. Add all non NoData-cells on the outside edges to the queue
            // 3. For each cell from the queue check if it's a boundary cell and if neighbours should be visited as well

            string outputBndFilename = Path.Combine(Path.GetDirectoryName(this.Filename), Path.GetFileNameWithoutExtension(this.Filename) + "_bndcorr.idf");
            IDFFile outputIDFFile = this.CopyIDF(outputBndFilename);

            IDFFile visitedCellsIDFFile = this.CopyIDF(string.Empty);

            // Set visited cells initially to value 0, meaning not visited yet
            visitedCellsIDFFile.SetValues(0);
            // Don't visit cells with active values or NoData-values
            visitedCellsIDFFile.ReplaceValues(this, this.NoDataValue, visitedCellsIDFFile.NoDataValue);
            if (!useOnlyActiveCells)
            {
                visitedCellsIDFFile.ReplaceValues(this, activeValue, visitedCellsIDFFile.NoDataValue);
            }

            // Retrieve indices of top and bottom row and left and right column in input IDF-file for specified extent 
            int bndTopRow = 0;
            int bndBotRow = this.NRows - 1;
            int bndLeftCol = 0;
            int bndRightCol = this.NCols - 1;
            float[][] outputIDFValues = outputIDFFile.values;
            float outputIDFNoDataValue = outputIDFFile.NoDataValue;
            if (extent != null)
            {
                if (!this.Extent.Contains(extent))
                {
                    throw new ToolException("Specified extent (" + extent.ToString() + ") is not inside extent of input boundary files (" + this.Extent.ToString() + "): " + Path.GetFileName(this.Filename));
                }

                // Retrieve row/column indices. GetRowIdx takes row below upper/lower edge and right of left/right edge of extent, so correct bottom row and right column with cellsize
                bndTopRow = this.GetRowIdx(extent.ury);
                bndBotRow = this.GetRowIdx(extent.lly + this.YCellsize);
                bndLeftCol = this.GetColIdx(extent.llx);
                bndRightCol = this.GetColIdx(extent.urx - this.XCellsize);
            }

            // Start from cells at edge of IDF grid, add these to a queue of cells to visit
            Queue<IDFCell> cellQueue = new Queue<IDFCell>();
            // Loop through rows and add cells at left and right columns to queue to visit
            for (int rowIdx = bndTopRow; rowIdx <= bndBotRow; rowIdx++)
            {
                if (useGridEdge)
                {
                    // When the grid edge or extent is used, don't visit any cells, simply set all cell at the grid edge to a boundary value, unless cell in this IDF-file is NoData
                    if (!this.values[rowIdx][bndLeftCol].Equals(this.NoDataValue) && (!useOnlyActiveCells || !this.values[rowIdx][bndLeftCol].Equals(inactiveValue)))
                    {
                        if (!keepInactiveCells || !outputIDFValues[rowIdx][bndLeftCol].Equals(inactiveValue))
                        {
                            outputIDFValues[rowIdx][bndLeftCol] = bndValue;
                        }
                    }
                    if (!this.values[rowIdx][bndRightCol].Equals(this.NoDataValue) && (!useOnlyActiveCells || !this.values[rowIdx][bndRightCol].Equals(inactiveValue)))
                    {
                        if (!keepInactiveCells || !outputIDFValues[rowIdx][bndRightCol].Equals(inactiveValue))
                        {
                            outputIDFValues[rowIdx][bndRightCol] = bndValue;
                        }
                    }
                }
                else
                {
                    // Add cells in left and right column to cellqueue
                    cellQueue.Enqueue(new IDFCell(rowIdx, bndLeftCol));
                    cellQueue.Enqueue(new IDFCell(rowIdx, bndRightCol));

                    // Mark cells as visited
                    visitedCellsIDFFile.values[rowIdx][bndLeftCol] = 1;
                    visitedCellsIDFFile.values[rowIdx][bndRightCol] = 1;

                    // remove active values at the outside edge of the grid, to allow boundary cells there, unless it is specified that only active cells can be set as a boundary 
                    if (!useOnlyActiveCells && outputIDFValues[rowIdx][bndLeftCol].Equals(activeValue))
                    {
                        outputIDFValues[rowIdx][bndLeftCol] = outputIDFNoDataValue;
                    }
                    if (!useOnlyActiveCells && outputIDFValues[rowIdx][bndRightCol].Equals(activeValue))
                    {
                        outputIDFValues[rowIdx][bndRightCol] = outputIDFNoDataValue;
                    }
                }
            }
            // Loop through columms and add cells at top and bottom rows to queue to visit
            for (int colIdx = bndLeftCol + 1; colIdx < bndRightCol; colIdx++)
            {
                if (useGridEdge)
                {
                    // When the grid edge or extent is used, don't visit any cells, simply set all cell at the grid edge to a boundary value, unless cell in this IDF-file is NoData
                    if (!this.values[bndTopRow][colIdx].Equals(this.NoDataValue) && (!useOnlyActiveCells || !this.values[bndTopRow][colIdx].Equals(inactiveValue)))
                    {
                        if (!keepInactiveCells || !outputIDFValues[bndTopRow][colIdx].Equals(inactiveValue))
                        {
                            outputIDFValues[bndTopRow][colIdx] = bndValue;
                        }
                    }
                    if (!this.values[bndBotRow][colIdx].Equals(this.NoDataValue) && (!useOnlyActiveCells || !this.values[bndBotRow][colIdx].Equals(inactiveValue)))
                    {
                        if (!keepInactiveCells || !outputIDFValues[bndBotRow][colIdx].Equals(inactiveValue))
                        {
                            outputIDFValues[bndBotRow][colIdx] = bndValue;
                        }
                    }
                }
                else
                {
                    cellQueue.Enqueue(new IDFCell(bndTopRow, colIdx));
                    cellQueue.Enqueue(new IDFCell(bndBotRow, colIdx));
                    visitedCellsIDFFile.values[bndTopRow][colIdx] = 1;
                    visitedCellsIDFFile.values[bndBotRow][colIdx] = 1;

                    // remove active values at the outside edge of the grid, to allow boundary cells there, unless it is specified that only active cells can be set as a boundary 
                    if (!useOnlyActiveCells && outputIDFValues[bndTopRow][colIdx].Equals(activeValue))
                    {
                        outputIDFValues[bndTopRow][colIdx] = outputIDFNoDataValue;
                    }
                    if (!useOnlyActiveCells && outputIDFValues[bndBotRow][colIdx].Equals(activeValue))
                    {
                        outputIDFValues[bndBotRow][colIdx] = outputIDFNoDataValue;
                    }
                }
            }

            // Now start visiting cells to find boundary. Note for option /g (useGridEdge) this is not done (as there will be no cells to visit)
            while (cellQueue.Count > 0)
            {
                // Take next cell to be visited
                IDFCell currentCell = cellQueue.Dequeue();
                int currentRowIdx = currentCell.RowIdx;
                int currentColIdx = currentCell.ColIdx;
                float currentCellValue = this.values[currentRowIdx][currentColIdx];
                bool isActiveCurrentCell = currentCellValue.Equals(activeValue);

                bool isBoundaryCell = false;
                if (useOnlyActiveCells && isActiveCurrentCell)
                {
                    isBoundaryCell = true;
                }
                else
                {
                    // Check all neighbours
                    for (int rowSubIdx = -1; rowSubIdx <= 1; rowSubIdx++)
                    {
                        for (int colSubIdx = -1; colSubIdx <= 1; colSubIdx++)
                        {
                            if (isDiagonallyChecked || (rowSubIdx * colSubIdx == 0))
                            {
                                int neighbourRowIdx = currentRowIdx + rowSubIdx;
                                int neighbourColIdx = currentColIdx + colSubIdx;
                                // Check that neighbour is inside input grid
                                if ((neighbourRowIdx >= bndTopRow) && (neighbourRowIdx <= bndBotRow) && (neighbourColIdx >= bndLeftCol) && (neighbourColIdx <= bndRightCol))
                                {
                                    // If neighbour has not yet been visited (and should be visited (with value 0)), add it to the queue to visit
                                    if (visitedCellsIDFFile.values[neighbourRowIdx][neighbourColIdx].Equals(0))
                                    {
                                        cellQueue.Enqueue(new IDFCell(neighbourRowIdx, neighbourColIdx));
                                        visitedCellsIDFFile.values[neighbourRowIdx][neighbourColIdx] = 1;
                                    }
                                    if (!useOnlyActiveCells)
                                    {
                                        float neighbourCellValue = this.values[neighbourRowIdx][neighbourColIdx];
                                        if (neighbourCellValue.Equals(activeValue))
                                        {
                                            isBoundaryCell = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (isBoundaryCell && (!keepInactiveCells || !outputIDFValues[currentRowIdx][currentColIdx].Equals(inactiveValue)))
                {
                    outputIDFValues[currentRowIdx][currentColIdx] = bndValue;
                }
            }

            return outputIDFFile;
        }
    }
}
