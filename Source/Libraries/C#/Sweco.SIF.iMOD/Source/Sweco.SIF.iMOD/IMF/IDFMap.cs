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
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IMF
{
    /// <summary>
    /// For storing IDF-files in Maps section of IMF-file
    /// </summary>
    public class IDFMap : Map
    {
        /// <summary>
        /// Thickness of line in IMF-crosssections
        /// </summary>
        public int LineThickness { get; set; }

        /// <summary>
        /// IDFLegend for displaying IDF-file
        /// </summary>
        public IDFLegend IDFLegend
        {
            get { return (IDFLegend)Legend; }
        }

        /// <summary>
        /// Hidden constructor
        /// </summary>
        protected IDFMap() : base()
        {
            ClearPRFTypeFlags();
            SetPRFType(PRFTypeFlag.Line);
            LineThickness = 1;
        }

        /// <summary>
        /// Creates IDFMap instance with empty legend and without a filename
        /// </summary>
        /// <param name="description"></param>
        public IDFMap(string description) : this()
        {
            this.Legend = new IDFLegend(description);
            ((IDFLegend)this.Legend).ClassList = new List<RangeLegendClass>();
        }

        /// <summary>
        /// Creates IDFMap instance with an empty legend with just a description and without a filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="selected"></param>
        public IDFMap(string description, bool selected) : this()
        {
            this.Legend = new IDFLegend(description);
            ((IDFLegend)this.Legend).ClassList = new List<RangeLegendClass>();
            Selected = selected;
        }

        /// <summary>
        /// Creates IDFMap instance with specified legend and filename
        /// </summary>
        /// <param name="legend"></param>
        /// <param name="filename"></param>
        public IDFMap(IDFLegend legend, string filename) : this()
        {
            this.Legend = legend;
            this.Filename = filename;
        }

        /// <summary>
        /// Creates IPFMap instance with specified description, and legend between min and max values (with default colors) and filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="imodFilename"></param>
        public IDFMap(string description, float minValue, float maxValue, string imodFilename) : this(description)
        {
            this.Filename = imodFilename;
            this.IDFLegend.AddLegendClasses(minValue, maxValue);
        }

        /// <summary>
        /// Creates IPFMap instance with specified description, and legend between min and max values (with default colors) and filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="imodFilename"></param>
        /// <param name="selected"></param>
        /// <returns></returns>
        public static IDFMap CreateSurfaceLevelMap(string description, float minValue, float maxValue, string imodFilename, bool selected = false)
        {
            IDFMap map = new IDFMap(description, selected);
            map.Filename = imodFilename;
            map.IDFLegend.AddSurfaceLevelLegendClasses(minValue, maxValue);
            return map;
        }

        /// <summary>
        /// Creates IPFMap instance with specified description, a default depth legend and filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="imodFilename"></param>
        /// <returns></returns>
        public static IDFMap CreateDepthMap(string description, string imodFilename)
        {
            IDFMap map = new IDFMap(description);
            map.Filename = imodFilename;
            map.IDFLegend.AddDepthLegendClasses();
            return map;
        }

        /// <summary>
        /// Copy IDFMap instance
        /// </summary>
        /// <returns></returns>
        public IDFMap Copy()
        {
            IDFMap newIDFMap = new IDFMap(Legend.Description);
            newIDFMap.Filename = this.Filename;
            newIDFMap.Selected = this.Selected;
            newIDFMap.PRFType = this.PRFType;
            newIDFMap.SColor = this.SColor;
            newIDFMap.LineThickness = this.LineThickness;
            foreach (RangeLegendClass rangeClass in this.IDFLegend.ClassList)
            {
                newIDFMap.IDFLegend.AddClass(new RangeLegendClass(rangeClass.MinValue, rangeClass.MaxValue, rangeClass.Label, rangeClass.Color, rangeClass.Description));
            }

            return newIDFMap;
        }
    }
}
