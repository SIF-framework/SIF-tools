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

namespace Sweco.SIF.iMOD.Values
{
    /// <summary>
    /// Class to define a range of (double) values
    /// </summary>
    public class ValueRange
    {
        /// <summary>
        /// Lower value of range
        /// </summary>
        public double V1 { get; set; }

        /// <summary>
        /// Upper value of range
        /// </summary>
        public double V2 { get; set; }

        /// <summary>
        /// Defines if lower value is included in range 
        /// </summary>
        public bool IsV1Included { get; set; }

        /// <summary>
        /// Defines if upper value is included in range 
        /// </summary>
        public bool IsV2Included { get; set; }

        /// <summary>
        /// Creates new ValueRange object
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public ValueRange(double v1, double v2)
        {
            this.V1 = v1;
            this.V2 = v2;
            this.IsV1Included = true;
            this.IsV2Included = true;
        }

        /// <summary>
        /// Creates new ValueRange object
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="isV1Included"></param>
        /// <param name="isV2Included"></param>
        public ValueRange(double v1, double v2, bool isV1Included, bool isV2Included)
        {
            this.V1 = v1;
            this.V2 = v2;
            this.IsV1Included = isV1Included;
            this.IsV2Included = isV2Included;
        }

        /// <summary>
        /// Checks if range contains specified value 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(float value)
        {
            if ((value < V1) || (value > V2))
            {
                return false;
            }
            else if (!IsV1Included && value.Equals(V1))
            {
                return false;
            }
            else if (!IsV2Included && value.Equals(V2))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Format range as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (V1.Equals(V2))
            {
                return V1.ToString();
            }
            else
            {
                return (IsV1Included ? "[" : "(") + V1 + "-" + V2 + (IsV2Included ? "]" : ")");
            }
        }

        /// <summary>
        /// Format range as a simple string: V1-V2
        /// </summary>
        /// <returns></returns>
        public string ToBasicString()
        {
            if (V1.Equals(V2))
            {
                return V1.ToString();
            }
            else
            {
                return V1 + "-" + V2;
            }
        }

        /// <summary>
        /// Convert list of ranges to a string seperated by semicolons
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static string ToString(List<ValueRange> ranges)
        {
            string skippedValuesString = string.Empty;
            if (ranges.Count > 0)
            {
                skippedValuesString = ranges[0].ToString();
            }
            for (int idx = 1; idx < ranges.Count; idx++)
            {
                skippedValuesString += ";" + ranges[idx].ToString();
            }
            return skippedValuesString;
        }
    }
}
