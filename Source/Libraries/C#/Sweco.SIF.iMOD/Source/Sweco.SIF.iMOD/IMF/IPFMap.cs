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
using Sweco.SIF.iMOD.DLF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IMF
{
    /// <summary>
    /// For storing IPF-files in Maps section of IMF-file
    /// </summary>
    public class IPFMap : Map
    {
        /// <summary>
        /// Legend of this IPFMap, casted to IPFLegend Type
        /// </summary>
        public IPFLegend IPFLegend
        {
            get { return (IPFLegend)Legend; }
        }

        /// <summary>
        /// DLF-file that is used for visualisation of IPF-files in cross sections via its associated file
        /// </summary>
        public DLFFile DLFFile { get; set; }

        /// <summary>
        /// Creates IPFMap object with underlying IPFLegend object (without description)
        /// </summary>
        protected IPFMap()
        {
            IPFLegend ipfLegend = new IPFLegend(null);
            SetPRFType(PRFTypeFlag.Active);
            AddPRFTypeFlag(PRFTypeFlag.Legend);
            ipfLegend.Thickness = 1;
            ipfLegend.ColumnNumber = 3;

            Legend = ipfLegend;
            DLFFile = null;
        }

        /// <summary>
        /// Creates IPFMap instance with empty legend and without a filename
        /// </summary>
        /// <param name="description"></param>
        public IPFMap(string description) : this()
        {
            Legend.Description = description;
            IPFLegend.ClassList = new List<RangeLegendClass>();
            IPFLegend.SelectedLabelColumns = null;
        }

        /// <summary>
        /// Creates IPFMap instance with an empty legend with just a description and without a filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="selected"></param>
        public IPFMap(string description, bool selected) : this()
        {
            IPFLegend.Description = description;
            IPFLegend.ClassList = new List<RangeLegendClass>();
            IPFLegend.SelectedLabelColumns = null;
            Selected = selected;
        }

        /// <summary>
        /// Creates IPFMap instance with specified legend and filename
        /// </summary>
        /// <param name="legend"></param>
        /// <param name="filename"></param>
        public IPFMap(IPFLegend legend, string filename) : this()
        {
            Legend = legend;
            Filename = filename;
        }

        /// <summary>
        /// Creates IPFMap instance with specified description, label, color and filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="description"></param>
        /// <param name="label"></param>
        /// <param name="color"></param>
        public IPFMap(string description, string label, Color color, string filename) : this(description)
        {
            Filename = filename;
            IPFLegend.AddClass(new RangeLegendClass(float.MinValue, float.MaxValue, label, color));
            DLFFile = null;
        }

        /// <summary>
        /// Creates IPFMap instance with specified description, and legend between min and max values (with default colors) and filename
        /// </summary>
        /// <param name="description"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="imodFilename"></param>
        public IPFMap(string description, float minValue, float maxValue, string imodFilename) : this(description)
        {
            Filename = imodFilename;
            IPFLegend.AddLegendClasses(minValue, maxValue);
            DLFFile = null;
        }

        /// <summary>
        /// Copy IPFMap insstance
        /// </summary>
        /// <returns></returns>
        public IPFMap Copy()
        {
            IPFMap newIPFMap = new IPFMap(Legend.Description);
            newIPFMap.Filename = this.Filename;
            newIPFMap.Selected = this.Selected;
            newIPFMap.PRFType = this.PRFType;
            newIPFMap.SColor = this.SColor;
            newIPFMap.DLFFile = this.DLFFile.Copy();
            newIPFMap.IPFLegend.ClassList.AddRange(this.IPFLegend.ClassList);
            newIPFMap.IPFLegend.IsLabelShown = this.IPFLegend.IsLabelShown;
            newIPFMap.IPFLegend.SelectedLabelColumns = (this.IPFLegend.SelectedLabelColumns != null) ? new List<int>(this.IPFLegend.SelectedLabelColumns) : null;

            return newIPFMap;
        }
    }
}
