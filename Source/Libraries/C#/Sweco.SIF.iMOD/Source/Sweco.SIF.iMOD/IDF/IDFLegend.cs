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
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for handling legends of IDF-files
    /// </summary>
    public class IDFLegend : ClassLegend
    {
        /// <summary>
        /// Create empty IDF-legend
        /// </summary>
        protected IDFLegend()
        {
        }

        /// <summary>
        /// Create empty IDF-legend with specified description and without classes
        /// </summary>
        public IDFLegend(string description)
        {
            this.Description = description;
            ClassList = new List<RangeLegendClass>();
        }

        /// <summary>
        /// Create IDFLegend with classes of equal size between specified min- and maxvalue, plus a class below and above those values.The colors used are defined by the Colors property.
        /// </summary>
        public static ClassLegend CreateLegend(string description, float minValue, float maxValue)
        {
            ClassLegend legend = new IDFLegend(description);
            legend.AddLegendClasses(minValue, maxValue);
            return legend;
        }

        /// <summary>
        /// Create IDFLegend with default surface level legend classes, ranging from red (high) to blue (low): 23 classes equal to or greather than zero and one class for negative values
        /// </summary>
        public static ClassLegend CreateSurfaceLevelLegend(string description, float minValue, float maxValue)
        {
            ClassLegend legend = new IDFLegend(description);
            legend.AddSurfaceLevelLegendClasses(minValue, maxValue);
            return legend;
        }

        /// <summary>
        /// Create IDFLegend with default deptht legend classes to this legend: 21 classes equal to or greather than zero and one class for negative values
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ClassLegend CreateDepthLegend(string description)
        {
            ClassLegend legend = new IDFLegend(description);
            legend.AddDepthLegendClasses();
            return legend;
        }

        /// <summary>
        /// Removes legend classes without values in specified IDF-file
        /// </summary>
        /// <param name="idfFile">the IDF-file that is checked for values</param>
        /// <param name="classLabelSubString"></param>
        public void CompressLegend(IDFFile idfFile, string classLabelSubString = null)
        {
            for (int idx = 0; idx < ClassList.Count; idx++)
            {
                string classLabel = ClassList[idx].Label;
                if ((classLabelSubString == null) || classLabel.Contains(classLabelSubString))
                {
                    if (!((IDFFile)idfFile).HasValuesBetween(ClassList[idx].MinValue, ClassList[idx].MaxValue))
                    {
                        // Remove classes without values;
                        ClassList.RemoveAt(idx);
                    }
                }
            }
        }

        /// <summary>
        /// Copies this IDFLegend object
        /// </summary>
        /// <returns></returns>
        public override Legend Copy()
        {
            IDFLegend legendCopy = new IDFLegend(this.Description);
            foreach (RangeLegendClass rangeClass in ClassList)
            {
                legendCopy.AddClass(new RangeLegendClass(rangeClass.MinValue, rangeClass.MaxValue, rangeClass.Label, rangeClass.Color, rangeClass.Description));
            }
            return legendCopy;
        }
    }
}
