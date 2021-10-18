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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Legends
{
    /// <summary>
    /// Class to describe the class range of a legend. Note: ranges are sorted from high to low; the uppervalue of the class if the maxium value in the class.
    /// </summary>
    public class RangeLegendClass : IComparable<RangeLegendClass>
    {
        /// <summary>
        /// Maximum value in this class
        /// </summary>
        public float MaxValue { get; protected set; }

        /// <summary>
        /// Minimum value in this class
        /// </summary>
        public float MinValue { get; protected set; }

        /// <summary>
        /// Short label to describe this class
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Long label to describe this class
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Color for this class
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Create empty legend class
        /// </summary>
        protected RangeLegendClass() { }

        /// <summary>
        /// Create legend class object with specified minvalue, maxvalue and description and a random color.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="label"></param>
        /// <param name="description"></param>
        public RangeLegendClass(float minValue, float maxValue, string label, string description = null)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Label = label;
            this.Description = description;
            Random rnd = new Random(DateTime.Now.Second);
            this.Color = Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255));
        }

        /// <summary>
        /// Create legend class object with specified minvalue, maxvalue, color and optional description.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="label"></param>
        /// <param name="color"></param>
        /// <param name="description"></param>
        public RangeLegendClass(float minValue, float maxValue, string label, Color color, string description = null)
        {
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Label = label;
            this.Color = color;
            this.Description = description;
        }

        /// <summary>
        /// Retrieve a string representation of this class
        /// </summary>
        public override string ToString()
        {
            return MinValue.ToString() + " - " + MaxValue.ToString();
        }

        /// <summary>
        /// Compares maxvalue of this RangeLegendClass object with that of another object
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(RangeLegendClass other)
        {
            return this.MaxValue.CompareTo(other.MaxValue);
        }
    }
}
