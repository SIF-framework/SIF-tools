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
using OfficeOpenXml.Drawing.Chart;
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets.Excel.EPPLUS
{
    /// <summary>
    /// Class for processing Excel charts with EPPlus-library
    /// </summary>
    public class EPPlusChart : IChart
    {
        private string title;
        private bool isLineVisible;
        internal EPPlusWorksheet epplusWorksheet;
        internal ChartType chartType;
        internal ExcelChart epplusChart;

        public EPPlusChart(EPPlusWorksheet worksheet, ChartType chartType)
        {
            this.epplusWorksheet = worksheet;
            this.chartType = chartType;

            switch (chartType)
            {
                case ChartType.LineChart:
                    epplusChart = worksheet.epplusWorksheet.Drawings.AddChart("Shape" + (worksheet.epplusWorksheet.Drawings.Count + 1), eChartType.Line);
                    break;
                case ChartType.ScatterChart:
                    epplusChart = worksheet.epplusWorksheet.Drawings.AddChart("Shape" + (worksheet.epplusWorksheet.Drawings.Count + 1), eChartType.XYScatter);
                    break;
                case ChartType.BarClustered:
                    epplusChart = worksheet.epplusWorksheet.Drawings.AddChart("Shape" + (worksheet.epplusWorksheet.Drawings.Count + 1), eChartType.BarClustered);
                    break;
                case ChartType.ColumnClustered:
                    epplusChart = worksheet.epplusWorksheet.Drawings.AddChart("Shape" + (worksheet.epplusWorksheet.Drawings.Count + 1), eChartType.ColumnClustered);
                    break;
                default:
                    throw new LibraryException("Unknown ChartType enum: " + chartType);
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                epplusChart.Title.Text = title;
            }
        }

        public void AddData(Range xRange, Range yRange)
        {
            ExcelChartSerie series = epplusChart.Series.Add(epplusWorksheet.epplusWorksheet.Cells[yRange.RowIdx1 + 1, yRange.ColIdx1 + 1, yRange.RowIdx2 + 1, yRange.ColIdx2 + 1],
                epplusWorksheet.epplusWorksheet.Cells[xRange.RowIdx1 + 1, xRange.ColIdx1 + 1, xRange.RowIdx2 + 1, xRange.ColIdx2 + 1]);


            // Format the labels
            switch (chartType)
            {
                case ChartType.LineChart:
                    ExcelLineChartSerie lineChartSeries = (ExcelLineChartSerie)series;
                    //lineChartSeries.DataLabel.Font.Bold = true;
                    //lineChartSeries.DataLabel.ShowValue = true;
                    //lineChartSeries.DataLabel.ShowPercent = true;
                    //lineChartSeries.DataLabel.ShowLeaderLines = true;
                    //lineChartSeries.DataLabel.Separator = ";";
                    //lineChartSeries.DataLabel.Position = eLabelPosition.BestFit;
                    break;
                case ChartType.ScatterChart:
                    ExcelScatterChartSerie scatterChartSeries = (ExcelScatterChartSerie)series;
                    //scatterChartSeries.DataLabel.Font.Bold = true;
                    //scatterChartSeries.DataLabel.ShowValue = true;
                    //scatterChartSeries.DataLabel.ShowPercent = true;
                    //scatterChartSeries.DataLabel.ShowLeaderLines = true;
                    //scatterChartSeries.DataLabel.Separator = ";";
                    //scatterChartSeries.DataLabel.Position = eLabelPosition.BestFit;
                    break;
                case ChartType.BarClustered:
                    ExcelBarChartSerie barChartSeries = (ExcelBarChartSerie)series;
                    // barChartSeries.DataLabel.Font.Bold = true;
                    break;
                case ChartType.ColumnClustered:
                    ExcelChartSerie columnChartSeries = (ExcelChartSerie)series;
                    // barChartSeries.DataLabel.Font.Bold = true;
                    break;
                default:
                    throw new LibraryException("Unknown ChartType enum: " + chartType);
            }

            // Format the legend
            //epplusChart.Legend.Add();
            //epplusChart.Legend.Border.Width = 0;
            //epplusChart.Legend.Font.Size = 12;
            //epplusChart.Legend.Font.Bold = true;
            //epplusChart.Legend.Position = eLegendPosition.Right;
        }

        public void AddData(double[] xValues, double[] yValues)
        {
            throw new NotImplementedException();
        }

        internal void SetPosition(Range range)
        {
            epplusChart.SetPosition(range.RowIdx1, 0, range.ColIdx1, 0);
            double height = 0;
            double width = 0;
            for (int rowIdx = range.RowIdx1; rowIdx < range.RowIdx2; rowIdx++)
            {
                height += epplusWorksheet.epplusWorksheet.Row(rowIdx + 1).Height;
            }
            for (int colIdx = range.ColIdx1; colIdx < range.ColIdx2; colIdx++)
            {
                width += epplusWorksheet.epplusWorksheet.Column(colIdx + 1).Width;
            }
            double pointToPixel = 0.75;
            height /= pointToPixel;
            width /= 0.1423;
            epplusChart.SetSize((int)width, (int)height);
        }

        public bool IsLineVisible
        {
            get
            {
                return isLineVisible;
            }
            set
            {
                isLineVisible = value;

                for (int seriesIdx = 0; seriesIdx < epplusChart.Series.Count; seriesIdx++)
                {
                    ExcelChartSerie serie = epplusChart.Series[seriesIdx];
                    if (isLineVisible)
                    {
                        switch (chartType)
                        {
                            case ChartType.LineChart:
                                ((ExcelLineChartSerie)serie).LineWidth = 1;
                                break;
                            case ChartType.ScatterChart:
                                ((ExcelScatterChartSerie)serie).LineWidth = 1;
                                break;
                            case ChartType.BarClustered:
                                ((ExcelBarChartSerie)serie).Border.Width = 1;
                                break;
                            case ChartType.ColumnClustered:
                                ((ExcelChartSerie)serie).Border.Width = 1;
                                break;
                            default:
                                throw new LibraryException("Unknown ChartType enum: " + chartType);
                        }
                    }
                    else
                    {
                        switch (chartType)
                        {
                            case ChartType.LineChart:
                                ((ExcelLineChartSerie)serie).LineWidth = 0;
                                break;
                            case ChartType.ScatterChart:
                                ((ExcelScatterChartSerie)serie).LineWidth = 0;
                                break;
                            case ChartType.BarClustered:
                                ((ExcelBarChartSerie)serie).Border.Width = 0;
                                break;
                            case ChartType.ColumnClustered:
                                ((ExcelChartSerie)serie).Border.Width = 0;
                                break;
                            default:
                                throw new LibraryException("Unknown ChartType enum: " + chartType);
                        }
                    }
                }
            }
        }

        public void DeleteLegend()
        {
            epplusChart.Legend.Remove(); ;
        }

        public void SetMarkerColor(int seriesIdx, System.Drawing.Color color)
        {
            ExcelChartSerie serie = epplusChart.Series[seriesIdx];

            switch (chartType)
            {
                case ChartType.LineChart:
                    ((ExcelLineChartSerie)serie).MarkerLineColor = color;
                    break;
                case ChartType.ScatterChart:
                    ((ExcelScatterChartSerie)serie).MarkerColor = color;
                    ((ExcelScatterChartSerie)serie).MarkerLineColor = color;
                    break;
                default:
                    throw new LibraryException("Marker color (currently) not defined for ChartType: " + chartType);
            }
        }

        public void SetBarGapWidth(int width)
        {
            switch (chartType)
            {
                case ChartType.BarClustered:
                case ChartType.ColumnClustered:
                    ((ExcelBarChart)epplusChart).GapWidth = width;
                    break;
                default:
                    throw new LibraryException("Marker color (currently) not defined for ChartType: " + chartType);
            }
        }

        public void SetMarkerSize(int seriesIdx, int size)
        {
            ExcelChartSerie serie = epplusChart.Series[seriesIdx];

            switch (chartType)
            {
                case ChartType.LineChart:
                    ((ExcelLineChartSerie)serie).MarkerSize = size;
                    break;
                case ChartType.ScatterChart:
                    ((ExcelScatterChartSerie)serie).MarkerSize = size;
                    break;
                default:
                    throw new LibraryException("Marker size (currently) not defined for ChartType: " + chartType);
            }
        }

        public void SetMarkerStyle(int seriesIdx, MarkerStyle markerStyle)
        {
            ExcelChartSerie serie = epplusChart.Series[seriesIdx];

            eMarkerStyle eMarkerStyle = eMarkerStyle.None;
            switch (markerStyle)
            {
                case MarkerStyle.Circle:
                    eMarkerStyle = eMarkerStyle.Circle;
                    break;
                case MarkerStyle.Square:
                    eMarkerStyle = eMarkerStyle.Square;
                    break;
                case MarkerStyle.Dash:
                    eMarkerStyle = eMarkerStyle.Dash;
                    break;
                case MarkerStyle.Diamond:
                    eMarkerStyle = eMarkerStyle.Diamond;
                    break;
                case MarkerStyle.Dot:
                    eMarkerStyle = eMarkerStyle.Dot;
                    break;
                case MarkerStyle.Triangle:
                    eMarkerStyle = eMarkerStyle.Triangle;
                    break;
                case MarkerStyle.Star:
                    eMarkerStyle = eMarkerStyle.Star;
                    break;
                case MarkerStyle.Plus:
                    eMarkerStyle = eMarkerStyle.Plus;
                    break;
                case MarkerStyle.None:
                    eMarkerStyle = eMarkerStyle.None;
                    break;
            }

            switch (chartType)
            {
                case ChartType.LineChart:
                    ((ExcelLineChartSerie)serie).Marker = eMarkerStyle;
                    break;
                case ChartType.ScatterChart:
                    ((ExcelScatterChartSerie)serie).Marker = eMarkerStyle;
                    break;
                default:
                    throw new LibraryException("Marker style (currently) not defined for ChartType: " + chartType);
            }
        }

        public void AddTrendLine(int seriesIdx)
        {
            ExcelChartSerie serie = epplusChart.Series[seriesIdx];
            ExcelChartTrendline trendLine = serie.TrendLines.Add(eTrendLine.Linear);
            trendLine.DisplayEquation = true;
            trendLine.DisplayRSquaredValue = true;
        }


        public void SetXAxis(double minValue, double maxValue, string title)
        {
            epplusChart.XAxis.MinValue = minValue;
            epplusChart.XAxis.MaxValue = maxValue;
            epplusChart.XAxis.Title.Text = title;
            epplusChart.XAxis.Title.Font.Size = 10;
            epplusChart.XAxis.Title.Font.Bold = true;
        }

        public void SetYAxis(double minValue, double maxValue, string title)
        {
            epplusChart.YAxis.MinValue = minValue;
            epplusChart.YAxis.MaxValue = maxValue;
            epplusChart.YAxis.Title.Text = title;
            epplusChart.YAxis.Title.Font.Size = 10;
            epplusChart.YAxis.Title.Font.Bold = true;
            //            epplusChart.YAxis.Title.Rotation = 90;
            epplusChart.YAxis.Title.TextVertical = OfficeOpenXml.Drawing.eTextVerticalType.Vertical270;
        }
    }
}
