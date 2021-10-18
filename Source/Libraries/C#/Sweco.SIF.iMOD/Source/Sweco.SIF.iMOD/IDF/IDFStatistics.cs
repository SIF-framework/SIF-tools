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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for calculation of basic statistics over values in IDF-files
    /// </summary>
    public class IDFStatistics : Sweco.SIF.Statistics.Statistics
    {
        /// <summary>
        /// IDF-file to calculate statistics for
        /// </summary>
        protected IDFFile idfFile;

        /// <summary>
        /// Internal boolean to keep track of copied of the IDF-file
        /// </summary>
        protected bool hasIDFFileCopy;

        /// <summary>
        /// Number of skipped values (note: NoData-values are always skipped)
        /// </summary>
        public long SkippedCount { get; set; }

        /// <summary>
        /// Fraction of skipped values over total number of values (note: NoData-values are always skipped)
        /// </summary>
        public float SkippedFraction { get; set; }

        /// <summary>
        /// Fraction of not-skipped values over total number of values (note: NoData-values are always skipped)
        /// </summary>
        public float NonSkippedFraction { get; set; }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        public IDFStatistics(IDFFile idfFile)
        {
            this.idfFile = idfFile;
            this.values = CreateList(idfFile.Values, idfFile.NoDataValue);
            hasIDFFileCopy = false;
            CalculateCellPercentages();
        }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file and defined skipped values
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="skippedValues">values other than NoData that are skipped as well</param>
        public IDFStatistics(IDFFile idfFile, List<float> skippedValues)
        {
            this.idfFile = idfFile;
            if (!skippedValues.Contains(idfFile.NoDataValue))
            {
                skippedValues.Add(idfFile.NoDataValue);
            }
            this.values = CreateList(idfFile.Values, skippedValues);
            hasIDFFileCopy = false;
            CalculateCellPercentages();
        }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file and extent
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="extent"></param>
        public IDFStatistics(IDFFile idfFile, Extent extent)
        {
            IDFFile clippedIDFFile = idfFile.ClipIDF(extent);
            hasIDFFileCopy = true;
            this.idfFile = clippedIDFFile;
            this.values = CreateList(idfFile.Values, clippedIDFFile.NoDataValue);
            CalculateCellPercentages();
        }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file, extent and skipped values
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="extent"></param>
        /// <param name="skippedValues">values other than NoData that are skipped as well</param>
        public IDFStatistics(IDFFile idfFile, Extent extent, List<float> skippedValues)
        {
            IDFFile outlierAreaHeadIDFFile = idfFile.Extent.Equals(extent) ? idfFile : idfFile.ClipIDF(extent);
            hasIDFFileCopy = true;
            this.idfFile = outlierAreaHeadIDFFile;
            if (!skippedValues.Contains(outlierAreaHeadIDFFile.NoDataValue))
            {
                skippedValues.Add(outlierAreaHeadIDFFile.NoDataValue);
            }
            this.values = CreateList(idfFile.Values, skippedValues);
            CalculateCellPercentages();

        }

        /// <summary>
        /// Create new IDFStatistics object for specified IDF-file, cell location and buffer of cells around it
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="x">x-coordinate of center cell</param>
        /// <param name="y">y-coordinate of center cell</param>
        /// <param name="bufferCellCount">number of cells in buffer left, right, above or below center cell (i.e. count 1 will give 9 cells)</param>
        public IDFStatistics(IDFFile idfFile, float x, float y, int bufferCellCount)
            : this(idfFile, new Extent(x - bufferCellCount * idfFile.XCellsize, y - bufferCellCount * idfFile.YCellsize, x + bufferCellCount * idfFile.XCellsize, y + bufferCellCount * idfFile.YCellsize))
        {
        }

        /// <summary>
        /// Calculate percentages and fractions for skipped values
        /// </summary>
        private void CalculateCellPercentages()
        {
            totalCount = idfFile.NRows * idfFile.NCols;
            SkippedCount = TotalCount - Count;
            SkippedFraction = (float)SkippedCount / (float)TotalCount;
            NonSkippedFraction = (float)Count / (float)TotalCount;
        }

        /// <summary>
        /// Create a 1D-list from 2D-array in IDF-files
        /// </summary>
        /// <param name="values"></param>
        /// <param name="skippedValues"></param>
        /// <returns></returns>
        private List<float> CreateList(float[][] values, List<float> skippedValues = null)
        {
            if (skippedValues == null)
            {
                List<float> floatList = new List<float>(values.Length * values[0].Length);
                for (int i = 0; i < idfFile.NRows; i++)
                {
                    for (int j = 0; j < idfFile.NCols; j++)
                    {
                        floatList.Add(idfFile.values[i][j]);
                    }
                }
                return floatList;
            }
            else
            {
                List<float> floatList = new List<float>(values.Length * values[0].Length);
                for (int i = 0; i < idfFile.NRows; i++)
                {
                    for (int j = 0; j < idfFile.NCols; j++)
                    {
                        if (!skippedValues.Contains(idfFile.values[i][j]))
                        {
                            floatList.Add(idfFile.values[i][j]);
                        }
                    }
                }
                return floatList;
            }
        }

        /// <summary>
        /// Create a 1D-Listfrom 2D-array in IDF-files
        /// </summary>
        /// <param name="values"></param>
        /// <param name="skippedValue"></param>
        /// <returns></returns>
        private List<float> CreateList(float[][] values, float skippedValue)
        {
            List<float> floatList = new List<float>(values.Length * values[0].Length);
            totalCount = idfFile.NRows * idfFile.NCols;
            for (int i = 0; i < idfFile.NRows; i++)
            {
                for (int j = 0; j < idfFile.NCols; j++)
                {
                    if (!skippedValue.Equals(idfFile.values[i][j]))
                    {
                        floatList.Add(idfFile.values[i][j]);
                    }
                }
            }
            return floatList;
        }

        /// <summary>
        /// Release values when a copy of the source IDF-file was made (i.e. when using an extent)
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseValuesMemory(bool isMemoryCollected = true)
        {
            base.ReleaseValuesMemory(isMemoryCollected);
            if (hasIDFFileCopy)
            {
                idfFile.ReleaseMemory(isMemoryCollected);
            }
        }
    }
}
