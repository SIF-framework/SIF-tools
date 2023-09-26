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
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class for handling legends of IPF-files
    /// </summary>
    public class IPFLegend : ClassLegend
    {
        /// <summary>
        /// Specifies the thicknes of lines
        /// </summary>
        public int Thickness { get; set; }

        /// <summary>
        /// One-based number of column that legend is used for coloring points
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Specifies if selected labels of IPF-file should be shown
        /// </summary>
        public bool IsLabelShown { get; set; }

        /// <summary>
        /// List with one-based columnnumbers in IPF-file that are selected to display labels. Note: visibility of labels is controlled with IsLabelShown property
        /// </summary>
        public List<int> SelectedLabelColumns { get; set; }

        /// <summary>
        /// Textsize of labels to shown, use 0 to hide labels
        /// </summary>
        public int TextSize { get; set; }

        /// <summary>
        /// Create empty IPF-legend
        /// </summary>
        protected IPFLegend()
        {
        }

        /// <summary>
        /// Create default IPF-legend with specified description: thickness is 1, no labels shown, column number for legend is 3, empty class list
        /// </summary>
        public IPFLegend(string description)
        {
            this.Description = description;
            ClassList = new List<RangeLegendClass>();
            Thickness = 1;
            ColumnNumber = 3;
            SelectedLabelColumns = null;
            this.TextSize = 7;
        }

        /// <summary>
        /// Create IPFLegend with specified description, label and (single) color
        /// </summary>
        /// <param name="description"></param>
        /// <param name="label"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static IPFLegend CreateLegend(string description, string label, Color color)
        {
            IPFLegend legend = new IPFLegend(description);
            // Note: use min and max slightly smaller than C# float.MinValue/MaxValue since iMOD allows less digits in IMF-files
            legend.AddClass(new RangeLegendClass(-3.4E+38f, 3.4E+38f, label, color));
            return legend;
        }

        /// <summary>
        /// Create IPFLegend with classes of equal size between specified min- and maxvalue, plus a class below and above those values.The colors used are defined by the Colors property.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="classCount">number of classes, including class above and below min/max-values; if 0, the number of defined colors is used</param>
        /// <returns></returns>
        public static IPFLegend CreateLegend(string description, float minValue, float maxValue, int classCount = 0)
        {
            IPFLegend legend = new IPFLegend(description);
            legend.AddLegendClasses(minValue, maxValue, classCount);
            return legend;
        }

        /// <summary>
        /// Removes legend classes without values in specified IPF-file
        /// </summary>
        /// <param name="ipfFile">the IPF-file that is checked for values</param>
        /// <param name="valueColIdx">index of column to check values for</param>
        /// <param name="classLabelSubString"></param>
        public void CompressLegend(IPFFile ipfFile, int valueColIdx, string classLabelSubString = null)
        {
            for (int idx = 0; idx < ClassList.Count; idx++)
            {
                string classLabel = ClassList[idx].Label;
                if ((classLabelSubString == null) || classLabel.Contains(classLabelSubString))
                {
                    if (!((IPFFile)ipfFile).HasValues(valueColIdx, ClassList[idx].MinValue, ClassList[idx].MaxValue))
                    {
                        // Remove classes without values;
                        ClassList.RemoveAt(idx);
                    }
                }
            }
        }

        /// <summary>
        /// Copies this IPFLegend object
        /// </summary>
        /// <returns></returns>
        public override Legend Copy()
        {
            ClassLegend legend = new IPFLegend(this.Description);
            legend.Description = this.Description;
            legend.ClassList.AddRange(this.ClassList);
            ((IPFLegend)legend).IsLabelShown = this.IsLabelShown;
            ((IPFLegend)legend).SelectedLabelColumns = (this.SelectedLabelColumns != null) ? new List<int>(this.SelectedLabelColumns) : null;
            ((IPFLegend)legend).TextSize = this.TextSize;

            return legend;
        }
    }
}
