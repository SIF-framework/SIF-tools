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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Common utilities for IDF-file processing
    /// </summary>
    public class IDFUtils
    {
        /// <summary>
        /// Calculate the most occurring value inside specified matrix, excluding some specified values
        /// </summary>
        public static float GetMostOccurringValue(float[][] cellValues, float[] excludedValues)
        {
            Dictionary<float, int> occurranceDictionary = new Dictionary<float, int>();
            for (int subRowIdx = 0; subRowIdx < cellValues.Length; subRowIdx++)
            {
                for (int subColIdx = 0; subColIdx < cellValues.Length; subColIdx++)
                {
                    float cellValue = cellValues[subRowIdx][subColIdx];
                    if (!excludedValues.Contains(cellValue))
                    {
                        if (occurranceDictionary.ContainsKey(cellValue))
                        {
                            occurranceDictionary[cellValue] = occurranceDictionary[cellValue] + 1;
                        }
                        else
                        {
                            occurranceDictionary.Add(cellValue, 1);
                        }
                    }
                }
            }
            int maxOccurrance = 0;
            float mostOccurringValue = float.NaN;
            foreach (float value in occurranceDictionary.Keys)
            {
                int occurrance = occurranceDictionary[value];
                if (occurrance > maxOccurrance)
                {
                    maxOccurrance = occurrance;
                    mostOccurringValue = value;
                }
            }
            return mostOccurringValue;
        }

        /// <summary>
        /// Determines the number of ConstantIDFFile subclassed objects in given list of IDFFile objects
        /// </summary>
        /// <param name="idfFileList"></param>
        /// <returns></returns>
        public static int GetConstantIDFFileCount(List<IDFFile> idfFileList)
        {
            int count = 0;
            foreach (IDFFile idfFile in idfFileList)
            {
                if (idfFile is ConstantIDFFile)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Convert HashSet with IDFCell objects into an IDF-file as defined by valueIDFFile (cellsize, extent, NoData)
        /// If IDFCell objects have a value different from float.NaN, it is copied to the resulting IDF-file; otherwise the value is taken from valueIDFFile 
        /// Optionally, a specific (searched) value in valueIDFFile can be replaced by a replacement value, other values are taken from valueIDFFile (IDFCell.value is not used then)
        /// </summary>
        /// <param name="cellHashSet"></param>
        /// <param name="valueIDFFile">an IDF-file that specified properties of resulting IDF-file and values to copy depending on IDFCell.Value or isReplaced</param>
        /// <param name="isReplaced">if true, searchedValue will be replaced in the resulting IDF-file; if false the value is taken from IDFCell.value or from valueIDFFile if IDFcell.Value==float.NaN</param>
        /// <param name="searchedValue">if isReplaced=true, specifies value to replace, or use float.NaN to replace all cells in cellList with replacedValue</param>
        /// <param name="replacedValue">if isReplaced=true, specifies the new value that is used as replacement</param>
        /// <returns></returns>
        public static IDFFile ConvertToIDF(HashSet<IDFCell> cellHashSet, IDFFile valueIDFFile, bool isReplaced = false, float searchedValue = float.NaN, float replacedValue = 1f)
        {
            List<IDFCell> cellList = cellHashSet.ToList();
            return ConvertToIDF(cellList, valueIDFFile, isReplaced, searchedValue, replacedValue);
        }

        /// <summary>
        /// Convert Queue with IDFCell objects into an IDF-file as defined by valueIDFFile (cellsize, extent, NoData)
        /// If IDFCell objects have a value different from float.NaN, it is copied to the resulting IDF-file; otherwise the value is taken from valueIDFFile 
        /// Optionally, a specific (searched) value in valueIDFFile can be replaced by a replacement value, other values are taken from valueIDFFile (IDFCell.value is not used then)
        /// </summary>
        /// <param name="cellQueue"></param>
        /// <param name="valueIDFFile">an IDF-file that specified properties of resulting IDF-file and values to copy depending on IDFCell.Value or isReplaced</param>
        /// <param name="isReplaced">if true, searchedValue will be replaced in the resulting IDF-file; if false the value is taken from IDFCell.value or from valueIDFFile if IDFcell.Value==float.NaN</param>
        /// <param name="searchedValue">if isReplaced=true, specifies value to replace, or use float.NaN to replace all cells in cellList with replacedValue</param>
        /// <param name="replacedValue">if isReplaced=true, specifies the new value that is used as replacement</param>
        /// <returns></returns>
        public static IDFFile ConvertToIDF(Queue<IDFCell> cellQueue, IDFFile valueIDFFile, bool isReplaced = false, float searchedValue = float.NaN, float replacedValue = 1f)
        {
            List<IDFCell> cellList = cellQueue.ToList();
            return ConvertToIDF(cellList, valueIDFFile, isReplaced, searchedValue, replacedValue);
        }

        /// <summary>
        /// Convert List with IDFCell objects into an IDF-file as defined by valueIDFFile (cellsize, extent, NoData)
        /// If IDFCell objects have a value different from float.NaN, it is copied to the resulting IDF-file; otherwise the value is taken from valueIDFFile 
        /// Optionally, a specific (searched) value in valueIDFFile can be replaced by a replacement value, other values are taken from valueIDFFile (IDFCell.value is not used then)
        /// </summary>
        /// <param name="cellList"></param>
        /// <param name="valueIDFFile">an IDF-file that specified properties of resulting IDF-file and values to copy depending on IDFCell.Value or isReplaced</param>
        /// <param name="isReplaced">if true, searchedValue will be replaced in the resulting IDF-file; if false the value is taken from IDFCell.value or from valueIDFFile if IDFcell.Value==float.NaN</param>
        /// <param name="searchedValue">if isReplaced=true, specifies value to replace, or use float.NaN to replace all cells in cellList with replacedValue</param>
        /// <param name="replacedValue">if isReplaced=true, specifies the new value that is used as replacement</param>
        /// <returns></returns>
        public static IDFFile ConvertToIDF(List<IDFCell> cellList, IDFFile valueIDFFile, bool isReplaced = false, float searchedValue = float.NaN, float replacedValue = 1f)
        {
            int minRowIdx = int.MaxValue;
            int maxRowIdx = int.MinValue;
            int minColIdx = int.MaxValue;
            int maxColIdx = int.MinValue;
            foreach (IDFCell idfCell in cellList)
            {
                IDFCell.UpdateMinMaxIndices(idfCell, ref minColIdx, ref minRowIdx, ref maxColIdx, ref maxRowIdx);
            }

            return ConvertToIDF(cellList, valueIDFFile, minColIdx, minRowIdx, maxColIdx, maxRowIdx, isReplaced, searchedValue, replacedValue);
        }

        /// <summary>
        /// Convert List with IDFCell objects into an IDF-file as defined by valueIDFFile (cellsize, extent, NoData)
        /// If IDFCell objects have a value different from float.NaN, it is copied to the resulting IDF-file; otherwise the value is taken from valueIDFFile 
        /// Optionally, a specific (searched) value in valueIDFFile can be replaced by a replacement value, other values are taken from valueIDFFile (IDFCell.value is not used then)
        /// </summary>
        /// <param name="idfCells"></param>
        /// <param name="valueIDFFile">an IDF-file that specified properties of resulting IDF-file and values to copy depending on IDFCell.Value or isReplaced</param>
        /// <param name="minColIdx"></param>
        /// <param name="minRowIdx"></param>
        /// <param name="maxColIdx"></param>
        /// <param name="maxRowIdx"></param>
        /// <param name="isReplaced">if true, searchedValue will be replaced in the resulting IDF-file; if false the value is taken from IDFCell.value or from valueIDFFile if IDFcell.Value==float.NaN</param>
        /// <param name="searchedValue">if isReplaced=true, specifies value to replace, or use float.NaN to replace all cells in cellList with replacedValue</param>
        /// <param name="replacedValue">if isReplaced=true, specifies the new value that is used as replacement</param>
        /// <returns></returns>
        public static IDFFile ConvertToIDF(List<IDFCell> idfCells, IDFFile valueIDFFile, int minColIdx, int minRowIdx, int maxColIdx, int maxRowIdx, bool isReplaced = false, float searchedValue = float.NaN, float replacedValue = float.NaN)
        {
            if (idfCells.Count == 0)
            {
                return null;
            }

            Extent extent = new Extent(valueIDFFile.GetX(minColIdx), valueIDFFile.GetY(maxRowIdx), valueIDFFile.GetX(maxColIdx), valueIDFFile.GetY(minRowIdx));
            extent = extent.Snap(valueIDFFile.XCellsize, true);
            IDFFile localValueIDFFile = new IDFFile(Path.GetFileName(valueIDFFile.Filename), extent, valueIDFFile.XCellsize, valueIDFFile.NoDataValue);
            localValueIDFFile.ResetValues();
            foreach (IDFCell idfCell in idfCells)
            {
                if ((idfCell.RowIdx > valueIDFFile.NRows) || (idfCell.ColIdx > valueIDFFile.NCols))
                {
                    throw new Exception("Specified maximum indices of idfCells (" + idfCell.RowIdx + "," + idfCell.ColIdx + ") don't match indices (" + valueIDFFile.NRows + "," + valueIDFFile.NCols + ") of valueIDFFile: " + Path.GetFileName(valueIDFFile.Filename));
                }

                float x = valueIDFFile.GetX(idfCell.ColIdx);
                float y = valueIDFFile.GetY(idfCell.RowIdx);
                if (!isReplaced)
                {
                    // No replacement defined: use Value if defined, otherwise copy value from valueIDFFile
                    localValueIDFFile.SetValue(x, y, idfCell.Value.Equals(float.NaN) ? valueIDFFile.values[idfCell.RowIdx][idfCell.ColIdx] : idfCell.Value);
                }
                else if (searchedValue.Equals(float.NaN))
                {
                    // searchValue is not defined, replace all listed cells in value IDF-file with replacedValue
                    localValueIDFFile.SetValue(x, y, replacedValue);
                }
                else
                {
                    // searchValue and replaceValue defined: replace searchValue by replaceValue
                    float value = valueIDFFile.values[idfCell.RowIdx][idfCell.ColIdx];
                    localValueIDFFile.SetValue(x, y, value.Equals(searchedValue) ? replacedValue : value);
                }
            }

            return localValueIDFFile;
        }

    }
}
