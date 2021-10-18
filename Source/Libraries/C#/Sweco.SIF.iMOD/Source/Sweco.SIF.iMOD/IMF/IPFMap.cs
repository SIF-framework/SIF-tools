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
        /// Specifies if IPF-file is selected in IMF-file
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// IPFLegend of this IPFMap
        /// </summary>
        public IPFLegend IPFLegend
        {
            get { return (IPFLegend)Legend; }
        }

        /// <summary>
        /// Creates IPFMap object with underlying IPFLegend object (without description)
        /// </summary>
        protected IPFMap()
        {
            IPFLegend ipfLegend = new IPFLegend(null);
            ipfLegend.Thickness = 1;
            ipfLegend.ColumnIndex = 3;
            Selected = false;

            this.Legend = ipfLegend;
        }

        /// <summary>
        /// Creates IPFMap instance with empty legend and without a filename
        /// </summary>
        /// <param name="description"></param>
        public IPFMap(string description) : this()
        {
            this.Legend.Description = description;
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
            this.Selected = selected;
        }

        /// <summary>
        /// Creates IPFMap instance with specified legend and filename
        /// </summary>
        /// <param name="legend"></param>
        /// <param name="filename"></param>
        public IPFMap(IPFLegend legend, string filename) : this()
        {
            this.Legend = legend;
            this.Filename = filename;
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
            this.Filename = filename;
            this.IPFLegend.AddClass(new RangeLegendClass(float.MinValue, float.MaxValue, label, color));
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
            this.Filename = imodFilename;
            this.IPFLegend.AddLegendClasses(minValue, maxValue);
        }

        /// <summary>
        /// Copy IPFMap insstance
        /// </summary>
        /// <returns></returns>
        public IPFMap Copy()
        {
            IPFMap ipfMap = new IPFMap(Legend.Description);
            ipfMap.Filename = this.Filename;

            ipfMap.IPFLegend.ClassList.AddRange(this.IPFLegend.ClassList);
            ipfMap.IPFLegend.IsLabelShown = this.IPFLegend.IsLabelShown;
            ipfMap.IPFLegend.SelectedLabelColumns = (this.IPFLegend.SelectedLabelColumns != null) ? new List<int>(this.IPFLegend.SelectedLabelColumns) : null;
            return ipfMap;
        }
    }
}
