// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    public class SummaryLegend
    {
        public static Color LightCyanVariantColor = Color.FromArgb(0, 220, 220);
        public static Color DarkCyanVariantColor = Color.FromArgb(25, 145, 145);
        public static Color DarkestCyanVariantColor = Color.FromArgb(30, 90, 90);

        /// <summary>
        /// Creates a legend for a summary file
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static IDFLegend CreateSummaryLegend(double maxValue, string resultTypeString, string filename)
        {
            double classRange = (maxValue / 8);
            // round to lower value with 1, 2 or 5 left-digit
            double classRangeLog = Math.Floor(Math.Log10(classRange));
            long roundedClassRange = (long)Math.Pow(10, classRangeLog);
            if (roundedClassRange == 0)
            {
                roundedClassRange = 1;
            }
            if (roundedClassRange * 2 < classRange)
            {
                roundedClassRange *= 2;
            }
            if (roundedClassRange * 2.5 < classRange)
            {
                roundedClassRange = (long)(2.5 * roundedClassRange);
            }

            IDFLegend summaryLegend = new IDFLegend("Summary of " + resultTypeString.ToLower() + "s");
            summaryLegend.AddClass(new RangeLegendClass(0, 0.5f, "0 " + resultTypeString.ToLower() + "s", Color.White));
            summaryLegend.AddClass(new RangeLegendClass(0.5f, roundedClassRange, "1 - " + roundedClassRange + " " + resultTypeString.ToLower() + "s", LightCyanVariantColor));
            summaryLegend.AddClass(new RangeLegendClass(1 * roundedClassRange, 2 * roundedClassRange, 1 * roundedClassRange + " - " + 2 * roundedClassRange + " " + resultTypeString.ToLower() + "s", DarkCyanVariantColor));
            summaryLegend.AddClass(new RangeLegendClass(2 * roundedClassRange, 3 * roundedClassRange, 2 * roundedClassRange + " - " + 3 * roundedClassRange + " " + resultTypeString.ToLower() + "s", DarkestCyanVariantColor));
            summaryLegend.AddClass(new RangeLegendClass(3 * roundedClassRange, 4 * roundedClassRange, 3 * roundedClassRange + " - " + 4 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.DarkGreen));
            summaryLegend.AddClass(new RangeLegendClass(4 * roundedClassRange, 5 * roundedClassRange, 4 * roundedClassRange + " - " + 5 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.GreenYellow));
            summaryLegend.AddClass(new RangeLegendClass(5 * roundedClassRange, 6 * roundedClassRange, 5 * roundedClassRange + " - " + 6 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.Yellow));
            summaryLegend.AddClass(new RangeLegendClass(6 * roundedClassRange, 7 * roundedClassRange, 6 * roundedClassRange + " - " + 7 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.Orange));
            summaryLegend.AddClass(new RangeLegendClass(7 * roundedClassRange, 8 * roundedClassRange, 7 * roundedClassRange + " - " + 8 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.Red));
            if ((long)maxValue > (8 * roundedClassRange))
            {
                summaryLegend.AddClass(new RangeLegendClass(8 * roundedClassRange, (long)maxValue, "> " + 8 * roundedClassRange + " " + resultTypeString.ToLower() + "s", Color.DarkRed));
            }
            return summaryLegend;
        }
    }
}
