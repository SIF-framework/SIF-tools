// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// General utility methods for processing layers
    /// </summary>
    public static class LayerUtils
    {
        public static IDFFile ConvertKDtoKIDFFile(IDFFile kdIDFFile, IDFFile topIDFFile, IDFFile botIDFFile)
        {
            IDFFile kIDFFile = (IDFFile)kdIDFFile.Copy(string.Empty);

            for (int rowIdx = 0; rowIdx < kdIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < kdIDFFile.NCols; colIdx++)
                {
                    float topValue = topIDFFile.values[rowIdx][colIdx];
                    float botValue = botIDFFile.values[rowIdx][colIdx];
                    float kdValue = kdIDFFile.Values[rowIdx][colIdx];
                    if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue) && !kdValue.Equals(kdIDFFile.NoDataValue))
                    {
                        kIDFFile.Values[rowIdx][colIdx] = ConvertKDtoK(kdValue, topValue, botValue);
                    }
                    else
                    {
                        kIDFFile.Values[rowIdx][colIdx] = kIDFFile.NoDataValue;
                    }
                }
            }
            return kIDFFile;
        }

        public static IDFFile ConvertKDtoCIDFFile(IDFFile kdIDFFile, IDFFile topIDFFile, IDFFile botIDFFile)
        {
            IDFFile cIDFFile = null;
            if (kdIDFFile != null)
            {
                cIDFFile = (IDFFile)kdIDFFile.Copy(string.Empty);

                for (int rowIdx = 0; rowIdx < kdIDFFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < kdIDFFile.NCols; colIdx++)
                    {
                        float topValue = topIDFFile.values[rowIdx][colIdx];
                        float botValue = botIDFFile.values[rowIdx][colIdx];
                        float kdValue = kdIDFFile.Values[rowIdx][colIdx];
                        if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue) && !kdValue.Equals(kdIDFFile.NoDataValue))
                        {
                            cIDFFile.Values[rowIdx][colIdx] = LayerUtils.ConvertKDtoC(kdValue, topValue, botValue);
                        }
                        else
                        {
                            cIDFFile.Values[rowIdx][colIdx] = cIDFFile.NoDataValue;
                        }
                    }
                }
            }
            return cIDFFile;
        }

        /// <summary>
        /// Converts kD-value to k-value for a given thickness, undefined for thickness 0
        /// </summary>
        /// <param name="kdValue"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        /// <returns></returns>
        public static float ConvertKDtoK(float kdValue, float topValue, float botValue)
        {
            // the value from file is  a kD-value, covert it to a k-value
            float thickness = topValue - botValue;
            if (thickness > 0)
            {
                return kdValue / thickness;
            }
            else
            {
                return 0; // float.PositiveInfinity;
            }
        }

        /// <summary>
        /// Converts kD-value to C-value for a given thickness
        /// </summary>
        /// <param name="kdValue"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        /// <returns></returns>
        public static float ConvertKDtoC(float kdValue, float topValue, float botValue)
        {
            // the value from file is  a kD-value, covert it to a c-value
            float thickness = topValue - botValue;
            if ((thickness > 0) && (kdValue > 0))
            {
                float kValue = kdValue / thickness;
                return thickness / kValue;
            }
            else
            {
                return 0;
            }
        }

        public static IDFFile ConvertCtoKDIDFFile(IDFFile cIDFFile, IDFFile topIDFFile, IDFFile botIDFFile)
        {
            IDFFile kdIDFFile = (IDFFile)cIDFFile.Copy(string.Empty);

            for (int rowIdx = 0; rowIdx < kdIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < kdIDFFile.NCols; colIdx++)
                {
                    float topValue = topIDFFile.values[rowIdx][colIdx];
                    float botValue = botIDFFile.values[rowIdx][colIdx];
                    float cValue = cIDFFile.Values[rowIdx][colIdx];
                    if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue) && !cValue.Equals(cIDFFile.NoDataValue))
                    {
                        kdIDFFile.Values[rowIdx][colIdx] = ConvertCtoKD(cValue, topValue, botValue);
                    }
                    else
                    {
                        kdIDFFile.Values[rowIdx][colIdx] = kdIDFFile.NoDataValue;
                    }
                }
            }
            return kdIDFFile;
        }

        /// <summary>
        /// Converts C-value to kD-value for a given thickness
        /// </summary>
        /// <param name="cValue"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        /// <returns></returns>
        public static float ConvertCtoKD(float cValue, float topValue, float botValue)
        {
            // the value from file is a c-value, covert it to a kD-value
            float thickness = topValue - botValue;
            float kValue = float.PositiveInfinity;
            if (cValue > 0)
            {
                kValue = thickness / cValue;
            }
            else
            {
                kValue = 0;
            }
            return thickness * kValue;
        }

        public static IDFFile ConvertCtoKIDFFile(IDFFile cIDFFile, IDFFile topIDFFile, IDFFile botIDFFile)
        {
            IDFFile kIDFFile = null;
            if (cIDFFile != null)
            {
                kIDFFile = (IDFFile)cIDFFile.Copy(string.Empty);
                for (int rowIdx = 0; rowIdx < cIDFFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < cIDFFile.NCols; colIdx++)
                    {
                        float topValue = topIDFFile.values[rowIdx][colIdx];
                        float botValue = botIDFFile.values[rowIdx][colIdx];
                        float cValue = cIDFFile.Values[rowIdx][colIdx];
                        if (!topValue.Equals(topIDFFile.NoDataValue) && !botValue.Equals(botIDFFile.NoDataValue) && !cValue.Equals(cIDFFile.NoDataValue))
                        {
                            kIDFFile.Values[rowIdx][colIdx] = ConvertCtoK(cValue, topValue, botValue);
                        }
                        else
                        {
                            kIDFFile.Values[rowIdx][colIdx] = kIDFFile.NoDataValue;
                        }
                    }
                }
            }
            return kIDFFile;
        }

        /// <summary>
        /// Converts C-value to k-value for a given thickness, undefined for thickness 0
        /// </summary>
        /// <param name="cValue"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        /// <returns></returns>
        public static float ConvertCtoK(float cValue, float topValue, float botValue)
        {
            // the value from file is a c-value, covert it to a kD-value
            float thickness = topValue - botValue;
            if (cValue > 0)
            {
                return thickness / cValue;
            }
            else
            {
                return 0;  // float.PositiveInfinity;
            }
        }

    }
}
