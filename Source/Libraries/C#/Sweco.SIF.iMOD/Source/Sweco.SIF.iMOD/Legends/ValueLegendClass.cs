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
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Legends
{
    /// <summary>
    /// Class to describe a unique value class of a legend.
    /// </summary>
    public class ValueLegendClass : RangeLegendClass
    {
        private static Random rnd = new Random(DateTime.Now.Second);

        /// <summary>
        /// The unique value of this legend class
        /// </summary>
        public float UniqueValue { get; }

        private ValueLegendClass() { }

        /// <summary>
        /// Create value class object with specified unique value, a label and a random color and optional description
        /// </summary>
        /// <param name="uniquevalue"></param>
        /// <param name="label"></param>
        /// <param name="description"></param>
        public ValueLegendClass(float uniquevalue, string label, string description = null)
        {
            this.UniqueValue = uniquevalue;
            this.MinValue = uniquevalue - 0.5f;
            this.MaxValue = uniquevalue + 0.5f;
            this.Label = label;
            this.Color = Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255));
            this.Description = description;
        }

        /// <summary>
        /// Create value class object with specified unique value, a label, color and optional description
        /// </summary>
        /// <param name="uniquevalue"></param>
        /// <param name="label"></param>
        /// <param name="color"></param>
        /// <param name="description"></param>
        public ValueLegendClass(float uniquevalue, string label, Color color, string description = null)
        {
            this.UniqueValue = uniquevalue;
            this.MinValue = uniquevalue - 0.5f;
            this.MaxValue = uniquevalue + 0.5f;
            this.Label = label;
            this.Color = color;
            this.Description = description;
        }

        /// <summary>
        /// Retrieve a string representation of this class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UniqueValue.ToString();
        }
    }
}
