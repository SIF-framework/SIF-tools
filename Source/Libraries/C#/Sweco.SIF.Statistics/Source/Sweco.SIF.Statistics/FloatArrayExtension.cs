// Sweco.SIF.Statistics is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Statistics.
// 
// Sweco.SIF.Statistics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Statistics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Statistics. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Statistics
{
    /// <summary>
    /// Extension class with utilities for float arrays
    /// </summary>
    public static class FloatArrayExtension
    {
        /// <summary>
        /// Calculate natural (base e) logarithm of specified values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float[] LogTransform(this float[] values)
        {
            float[] result = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = (float)Math.Log(values[i]);
            }
            return result;
        }

        /// <summary>
        /// Delete given value from specified array
        /// </summary>
        /// <param name="values"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float[] DeleteValue(this float[] values, float value)
        {
            List<float> newValues = new List<float>();

            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].Equals(value))
                {
                    newValues.Add(values[i]);
                }
            }
            return newValues.ToArray();
        }
    }
}
