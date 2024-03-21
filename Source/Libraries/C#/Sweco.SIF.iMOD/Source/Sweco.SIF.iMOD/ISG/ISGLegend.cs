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
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    /// <summary>
    /// Class for handling legends of ISG-files
    /// </summary>
    public class ISGLegend : Legend
    {
        /// <summary>
        /// Specifies the thicknes of lines
        /// </summary>
        public int Thickness { get; set; }

        /// <summary>
        /// Specifie the Color of features
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Create default ISG-legend: thickness is 1, color is light blue.
        /// </summary>
        public ISGLegend()
        {
            this.Thickness = 1;
            this.Color = Color.LightBlue;
        }

        /// <summary>
        /// Create ISG-legend with specified description, thickness, and color.
        /// </summary>
        public ISGLegend(string description, int thickness, Color color)
        {
            this.Description = description;
            this.Thickness = thickness;
            this.Color = color;
        }

        /// <summary>
        /// Create ISG-legend with specified description, thickness and color
        /// </summary>
        public static Legend CreateLegend(string description, int thickness, Color color)
        {
            Legend legend = new ISGLegend(description, thickness, color);
            return legend;
        }

        /// <summary>
        /// Copies this ISGLegend object
        /// </summary>
        /// <returns></returns>
        public override Legend Copy()
        {
            return CreateLegend(Description, Thickness, Color);
        }

        /// <summary>
        /// Returns a short string that describes this ISGLegend object, description, color and thickness
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string legendString = CreateLegendHeader();
            legendString += "Color: " + Color.ToString() + "; Thickness: " + Thickness + "\r\n";
            return legendString;
        }

        /// <summary>
        /// Returns a longer string that describes this ISGLegend object
        /// </summary>
        /// <returns></returns>
        public override string ToLongString()
        {
            // Currently there's no difference between ToLongString() and ToString()
            return ToString();
        }
    }
}
