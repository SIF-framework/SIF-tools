// Sweco.SIF.Spreadsheets is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Spreadsheets.
// 
// Sweco.SIF.Spreadsheets is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Spreadsheets is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Spreadsheets. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets
{
    /// <summary>
    /// Interface for chart
    /// </summary>
    public interface IChart
    {
        /// <summary>
        /// Title of chart
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Get or set the visibity of the line around the chart
        /// </summary>
        bool IsLineVisible { get; set; }

        /// <summary>
        /// Delete the current legend of this chart
        /// </summary>
        void DeleteLegend();

        /// <summary>
        /// Set marker color for specified series 
        /// </summary>
        /// <param name="seriesIdx">zero-based index of series</param>
        /// <param name="color"></param>
        void SetMarkerColor(int seriesIdx, Color color);

        /// <summary>
        /// Set marker size for specified series 
        /// </summary>
        /// <param name="seriesIdx">zero-based index of series</param>
        /// <param name="size"></param>
        void SetMarkerSize(int seriesIdx, int size);

        /// <summary>
        /// Set marker style for specified series 
        /// </summary>
        /// <param name="seriesIdx">zero-based index of series</param>
        /// <param name="markerStyle"></param>
        void SetMarkerStyle(int seriesIdx, MarkerStyle markerStyle);

        /// <summary>
        /// Set with between bars for Bar charts
        /// </summary>
        /// <param name="width"></param>
        void SetBarGapWidth(int width);

        /// <summary>
        /// Add data for x- and y-axis from specified ranges
        /// </summary>
        /// <param name="xRange"></param>
        /// <param name="yRange"></param>
        void AddData(Range xRange, Range yRange);

        /// <summary>
        /// Add data for x- and y-axis from specified arrays
        /// </summary>
        /// <param name="xValues"></param>
        /// <param name="yValues"></param>
        void AddData(double[] xValues, double[] yValues);

        /// <summary>
        /// Add a default trend line to the chart for specified series
        /// </summary>
        /// <param name="seriesIdx"></param>
        void AddTrendLine(int seriesIdx);

        /// <summary>
        /// Define settings for x-axis
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="title"></param>
        void SetXAxis(double minValue, double maxValue, string title);

        /// <summary>
        /// Define settings for y-axis
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="title"></param>
        void SetYAxis(double minValue, double maxValue, string title);
    }
}
