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

    }
}
