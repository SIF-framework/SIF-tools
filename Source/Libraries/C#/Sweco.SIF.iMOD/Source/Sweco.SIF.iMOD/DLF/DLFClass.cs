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
using System;
using System.Drawing;

namespace Sweco.SIF.iMOD.DLF
{
    /// <summary>
    /// Class for handling DLF-items/classes in an iMOD DLF-file
    /// </summary>
    public class DLFClass
    {
        /// <summary>
        /// Short label of item, i.e. "S" for sand. Note: The maximum string length of the Label-column (in iMOD) is 20 characters.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Red compoonent of color of DLF-item
        /// </summary>
        public int Red { get; set; }

        /// <summary>
        /// Green compoonent of color of DLF-item
        /// </summary>
        public int Green { get; set; }

        /// <summary>
        /// Blue compoonent of color of DLF-item
        /// </summary>
        public int Blue { get; set; }

        /// <summary>
        /// Short description of DLF-item. Note: The maximum string length (in iMOD) is 50 characters.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Width of DLF-item whan visualized
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// Create new DLFClass object
        /// </summary>
        /// <param name="label"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="description"></param>
        /// <param name="width"></param>
        public DLFClass(string label, int red, int green, int blue, string description, float width = 1.0f)
        {
            this.Label = label;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Description = description;
            this.Width = width;
        }

        /// <summary>
        /// Create new DLFClass object
        /// </summary>
        /// <param name="label"></param>
        /// <param name="color"></param>
        /// <param name="description"></param>
        /// <param name="width"></param>
        public DLFClass(string label, long color, string description, float width = 1.0f)
        {
            this.Label = label;
            Color drawingColor = CommonUtils.Long2Color(color);
            this.Red = drawingColor.R;
            this.Green = drawingColor.G;
            this.Blue = drawingColor.B;
            this.Description = description;
            this.Width = width;
        }

        /// <summary>
        /// Retrieve color of this class as a long value
        /// </summary>
        public long Color
        {
            get { return CommonUtils.Color2Long(System.Drawing.Color.FromArgb(Red, Green, Blue)); }
        }

        public DLFClass Copy()
        {
            return new DLFClass(this.Label, this.Red, this.Green, this.Blue, this.Description, this.Width);
        }
    }
}
