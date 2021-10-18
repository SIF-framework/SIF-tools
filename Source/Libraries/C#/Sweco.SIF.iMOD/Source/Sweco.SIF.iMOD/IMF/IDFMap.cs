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
        /// Flags for IDF Map-files as properties in IMF-file. Check iMOD-manual for details.
        /// </summary>
        public enum PRFTypeFlag
        {
            None,
            Active,
            Line,
            Point,
            Fill,
            Clr,
            Tinv,
            Legend,
        }

        private const int PRFTYPE_NONE = 0;
        private const int PRFTYPE_ACT = 1;
        private const int PRFTYPE_LINE = 2;
        private const int PRFTYPE_POINT = 4;
        private const int PRFTYPE_FILL = 8;
        private const int PRFTYPE_CLR = 16;
        private const int PRFTYPE_Tinv = 32;
        private const int PRFTYPE_LEG = 64;

        /// <summary>
        /// Specifies if IDF-file is selected in IMF-file
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// PRFType in IMFFile as a combination of PRFTypeFlag-values
        /// </summary>
        public int PRFType { get; set; }

        /// <summary>
        /// SCOLOR-value in IMF-file
        /// </summary>
        public long SColor { get; set; }

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
            this.ClearPRFTypeFlags();
            this.SetPRFType(PRFTypeFlag.Line);
            this.AddPRFTypeFlag(PRFTypeFlag.Line);
            this.SColor = 4410933;
            this.LineThickness = 1;
            Selected = false;
        }

        /// <summary>
        /// Remove all existing PRFType flags
        /// </summary>
        public void ClearPRFTypeFlags()
        {
            this.PRFType = PRFTYPE_NONE;
        }

        /// <summary>
        /// Set PRFType to specified PRFTypeFlag
        /// </summary>
        public void SetPRFType(PRFTypeFlag prfTypeFlag)
        {
            this.PRFType = PRFTypeToInt(prfTypeFlag);
        }

        /// <summary>
        /// Add specified PRFTypeFlag to existing PRFType 
        /// </summary>
        public void AddPRFTypeFlag(PRFTypeFlag prfTypeFlag)
        {
            this.PRFType |= PRFTypeToInt(prfTypeFlag);
        }

        /// <summary>
        /// Creates IDFMap instance with empty legend and without a filename
        /// </summary>
        /// <param name="description"></param>
        public IDFMap(string description) : this()
        {
            this.Legend = new IDFLegend(description);
            ((IDFLegend)this.Legend).ClassList = new List<RangeLegendClass>();
            Selected = false;
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

            foreach (RangeLegendClass rangeClass in this.IDFLegend.ClassList)
            {
                newIDFMap.IDFLegend.AddClass(new RangeLegendClass(rangeClass.MinValue, rangeClass.MaxValue, rangeClass.Label, rangeClass.Color, rangeClass.Description));
            }

            return newIDFMap;
        }

        /// <summary>
        /// Convert PRFTypeFlag enum to integer as used in IMF-file
        /// </summary>
        /// <param name="prfType"></param>
        /// <returns></returns>
        public static int PRFTypeToInt(PRFTypeFlag prfType)
        {
            switch (prfType)
            {
                case PRFTypeFlag.None:
                    return PRFTYPE_NONE;
                case PRFTypeFlag.Active:
                    return PRFTYPE_ACT;
                case PRFTypeFlag.Line:
                    return PRFTYPE_LINE;
                case PRFTypeFlag.Point:
                    return PRFTYPE_POINT;
                case PRFTypeFlag.Fill:
                    return PRFTYPE_FILL;
                case PRFTypeFlag.Clr:
                    return PRFTYPE_CLR;
                case PRFTypeFlag.Tinv:
                    return PRFTYPE_Tinv;
                case PRFTypeFlag.Legend:
                    return PRFTYPE_LEG;
                default:
                    throw new Exception("Undefined PRFType: " + prfType);
            }
        }

    }

}
