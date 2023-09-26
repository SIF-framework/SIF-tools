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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODPlus.IDF
{
    /// <summary>
    /// A sparse representation of an IDF-file. This uses less memory when many NoData-values are present in a large IDF-file, since these are not stored explicitly.
    /// The current implementation only facilitates reading of existing IDF-files as SparseIDFFiles.
    /// </summary>
    public class SparseIDFFile : IDFFile, IEquatable<SparseIDFFile>
    {
        /// <summary>
        /// Interal class for storing an IDF-cell as a point with xy-coordinates of the cell
        /// </summary>
        protected class XYPoint : IEquatable<XYPoint>
        {
            /// <summary>
            /// X-coordinate of XYPoint
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// Y-coordinate of XYPoint
            /// </summary>
            public float y { get; set; }

            /// <summary>
            /// Creates new XYPoint object from x and y coordinates
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public XYPoint(float x, float y)
            {
                this.X = x;
                this.y = y;
            }

            /// <summary>
            /// Checks equality of this XYPoint object with another
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(XYPoint other)
            {
                return (this.X.Equals(other.X)) && (this.y.Equals(other.y));
            }

            /// <summary>
            /// Checks if this XYPoint is contained in specified extent
            /// </summary>
            /// <param name="extent"></param>
            /// <returns></returns>
            public bool IsContainedBy(Extent extent)
            {
                return ((this.X >= extent.llx) && (this.y >= extent.lly) && (this.X < extent.urx) && (this.y < extent.ury));
            }
        }

        /// <summary>
        /// For access to lazy loaded values ([rows][cols], [0][0] is upperleft corner)
        /// </summary>
        public override float[][] Values
        {
            get
            {
                return base.Values;
            }
            set
            {
                // TODO write to xyDictionary
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Internal representation of SparseIDFFile: a dictionary of points with xy-coordinates and a corresponding value
        /// </summary>
        protected Dictionary<XYPoint, float> xyDictionary;

        /// <summary>
        /// Construct new SparseIDFFile object
        /// </summary>
        public SparseIDFFile()
            : base()
        {
            xyDictionary = null;
            UseLazyLoading = true;
        }

        /// <summary>
        /// Construct new SparseIDFFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="nrows"></param>
        /// <param name="ncols"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public SparseIDFFile(string filename, Extent extent, int nrows, int ncols, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
            : base(filename, extent, nrows, ncols, noDataValue, true, log, logIndentLevel)
        {
            xyDictionary = null;
            useLazyLoading = true;
        }

        /// <summary>
        /// Construct new SparseIDFFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="csize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public SparseIDFFile(string filename, Extent extent, float csize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
            : base(filename, extent, csize, csize, noDataValue, true, log, logIndentLevel)
        {
            xyDictionary = null;
            useLazyLoading = true;
        }

        /// <summary>
        /// Construct new SparseIDFFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public SparseIDFFile(string filename, Extent extent, float xCellsize, float yCellsize, float noDataValue, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
            : base(filename, extent, xCellsize, yCellsize, noDataValue, true, log, logIndentLevel)
        {
            xyDictionary = null;
            useLazyLoading = true;
        }

        /// <summary>
        /// Construct new SparseIDFFile object
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="isValueCopy"></param>
        public SparseIDFFile(IDFFile idfFile, bool isValueCopy = false)
        {
            this.MaxValue = idfFile.MaxValue;
            this.MinValue = idfFile.MinValue;
            this.NCols = idfFile.NCols;
            this.NRows = idfFile.NRows;
            this.NoDataValue = idfFile.NoDataValue;
            this.NoDataCalculationValue = idfFile.NoDataCalculationValue;
            this.XCellsize = idfFile.XCellsize;
            this.YCellsize = idfFile.YCellsize;
            this.UseLazyLoading = idfFile.UseLazyLoading;
            this.extent = (idfFile.Extent != null) ? idfFile.Extent.Copy() : null;
            this.fileExtent = (idfFile.Extent != null) ? idfFile.Extent.Copy() : null;
            this.modifiedExtent = null;

            if (isValueCopy)
            {
                CreateXYDictionary(idfFile.Values);
            }

            if (Legend != null)
            {
                this.Legend = idfFile.Legend.Copy();
            }
            this.Filename = idfFile.Filename;

            if (Metadata != null)
            {
                this.Metadata = idfFile.Metadata.Copy();
            }
        }

        /// <summary>
        /// Retrieve gridvalue at given coordinate (x,y), or nodata when outside bounds
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override float GetValue(float x, float y)
        {
            float value = float.NaN;
            if (xyDictionary.TryGetValue(new XYPoint(x, y), out value))
            {
                return value;
            }
            else
            {
                return NoDataValue;
            }
        }

        /// <summary>
        /// Set specified value in IDF-cell. If cell is not yet existing in sparse IDF-file is it created.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public override void SetValue(float x, float y, float value)
        {
            XYPoint point = new XYPoint(x, y);
            if (xyDictionary.ContainsKey(point))
            {
                xyDictionary[point] = value;
            }
            else
            {
                xyDictionary.Add(point, value);
            }
        }

        /// <summary>
        /// Add specified value to current value in IDF-cell. If cell is not yet existing in sparse IDF-file is it created.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public override void AddValue(float x, float y, float value)
        {
            XYPoint point = new XYPoint(x, y);
            if (xyDictionary.ContainsKey(point))
            {
                xyDictionary[point] += value;
            }
            else
            {
                xyDictionary.Add(point, value);
            }
        }

        /// <summary>
        /// Resets all values to NoData
        /// </summary>
        public override void ResetValues()
        {
            if (xyDictionary == null)
            {
                xyDictionary = new Dictionary<XYPoint, float>();
            }
            else
            {
                xyDictionary.Clear();
            }
        }

        /// <summary>
        /// Sets newvValue for all input-cells that have a value equal the given oldvalue 
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public override void ReplaceValues(float oldValue, float newValue)
        {
            foreach (XYPoint xyPoint in xyDictionary.Keys)
            {
                float value = xyDictionary[xyPoint];
                if (value.Equals(oldValue))
                {
                    xyDictionary.Remove(xyPoint);
                    xyDictionary.Add(xyPoint, newValue);
                }
            }
            UpdateMinMaxValue();
        }

        /// <summary>
        /// Use the corresponding value from the given newValueIDF for all input-cells that have a value equal the given oldvalue 
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValueIDF"></param>
        public override void ReplaceValues(float oldValue, IDFFile newValueIDF)
        {
            // todo 
            UpdateMinMaxValue();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets newvValue for all cells that have a non noData-value in the given selectionIDF grid
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="newValue"></param>
        public override void ReplaceValues(IDFFile selectionIDF, float newValue)
        {
            // todo
            UpdateMinMaxValue();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value from newvValueIDF for all cells that have a non noData-value in the given selectionIDF grid
        /// </summary>
        /// <param name="selectionIDF"></param>
        /// <param name="newValueIDF"></param>
        public override void ReplaceValues(IDFFile selectionIDF, IDFFile newValueIDF)
        {
            // todo
            UpdateMinMaxValue();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read a given IDF-file, optionally specify the use of lazy loading and the use of logging
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useLazyLoading">optional: specifies that the IDF-values should be lazyloaded</param>
        /// <param name="log">optional: specifies that logging is to be used (e.g. for notice of a lazyload of values)</param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static SparseIDFFile ReadFile(string filename, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
        {
            if (!File.Exists(filename))
            {
                throw new ToolException("IDF-file does not exist: " + filename);
            }

            SparseIDFFile sparseIDFFile = new SparseIDFFile();
            sparseIDFFile.UseLazyLoading = useLazyLoading;
            sparseIDFFile.Log = log;
            sparseIDFFile.LogIndentLevel = logIndentLevel;

            sparseIDFFile.ReadIDFFile(filename);

            return sparseIDFFile;
        }

        /// <summary>
        /// Write this IDFFile object to the file using the given filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string filename, Metadata metadata = null)
        {
            ConvertXYDictionaryToValues();
            base.WriteFile(filename, metadata);
        }

        /// <summary>
        /// Write this IDFFile object to the file using its filename property for the filename
        /// </summary>
        /// <param name="metadata"></param>
        public override void WriteFile(Metadata metadata = null)
        {
            ConvertXYDictionaryToValues();
            base.WriteFile(metadata);
        }

        /// <summary>
        /// Clips IDF-file to given extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isInvertedClip"></param>
        /// <returns></returns>
        public override IDFFile ClipIDF(Extent clipExtent, bool isInvertedClip = false)
        {
            ConvertXYDictionaryToValues();
            return base.ClipIDF(clipExtent, isInvertedClip);
        }

        /// <summary>
        /// Copy existing IDFFile object to another IDF-file object and return as iMODFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override IMODFile Copy(string filename)
        {
            return CopyIDF(filename);
        }

        /// <summary>
        /// Copy existing IDFFile object explicitly to another IDF-file object
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isValueCopy"></param>
        /// <returns></returns>
        public override IDFFile CopyIDF(string filename, bool isValueCopy = true)
        {
            SparseIDFFile sparseCopy = new SparseIDFFile(this);
            if (isValueCopy)
            {
                sparseCopy.xyDictionary = CopyXYDictionary(xyDictionary);
            }
            else
            {
                sparseCopy.xyDictionary = new Dictionary<XYPoint, float>();
            }
            return sparseCopy;
        }

        /// <summary>
        /// Calculate otherIDFfile - this IDFFile, within the extent
        /// </summary>
        /// <param name="otherIDFFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="isNoDataCompared"></param>
        /// <param name="comparedExtent"></param>
        /// <returns></returns>
        public override IDFFile CreateDifferenceFile(IDFFile otherIDFFile, string outputPath, bool isNoDataCompared, Extent comparedExtent = null)
        {
            // Not implemented in basic version
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(IDFFile other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// Retrieves number of cells that have a non-nodata value other than zero
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            EnsureLoadedValues();
            return xyDictionary.Count;
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
                xyDictionary = null;
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// Force values of SparseIDF-file to be actually loaded. This may be necessary if lazy loading is used and reference of Values property is not desired.
        /// </summary>
        public override void EnsureLoadedValues()
        {
            if (xyDictionary == null)
            {
                LoadValues();
            }
        }

        /// <summary>
        /// Update minmax values in header of IDF-file (which are also loaded when using lazy-loading
        /// </summary>
        public override void UpdateMinMaxValue()
        {
            MinValue = float.MaxValue;
            MaxValue = float.MinValue;
            foreach (XYPoint point in xyDictionary.Keys)
            {
                float value = xyDictionary[point];
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

        /// <summary>
        /// checks equality of this IDFFile object with a SparseIDFFile object 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SparseIDFFile other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load values for this IDFFile instance from the defined file
        /// </summary>
        protected override void LoadValues()
        {
            base.LoadValues();
            if (xyDictionary == null)
            {
                xyDictionary = new Dictionary<XYPoint, float>();
            }
            CreateXYDictionary(this.values);
            base.ReleaseMemory(true);
        }

        /// <summary>
        /// Convert values in dictionary implementation to values in array implementation of base class
        /// </summary>
        protected void ConvertXYDictionaryToValues()
        {
            DeclareValuesMemory();
            base.ResetValues();
            foreach (XYPoint point in xyDictionary.Keys)
            {
                values[GetRowIdx(point.y)][GetColIdx(point.X)] = xyDictionary[point];
            }
        }

        /// <summary>
        /// Create values in dictionary implementation for values in specified array implementation
        /// </summary>
        protected void CreateXYDictionary(float[][] values)
        {
            for (int rowIdx = 0; rowIdx < NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < NCols; colIdx++)
                {
                    float value = values[rowIdx][colIdx];
                    if (!value.Equals(NoDataValue))
                    {
                        xyDictionary.Add(new XYPoint(GetX(colIdx), GetY(rowIdx)), value);
                    }
                }
            }
        }

        private Dictionary<XYPoint, float> CopyXYDictionary(Dictionary<XYPoint, float> xyDictionary)
        {
            Dictionary<XYPoint, float> dictionaryCopy = new Dictionary<XYPoint, float>(xyDictionary.Count);
            foreach (XYPoint point in xyDictionary.Keys)
            {
                dictionaryCopy.Add(point, xyDictionary[point]);
            }
            return dictionaryCopy;
        }
    }
}
