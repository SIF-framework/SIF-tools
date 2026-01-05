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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Utils;

namespace Sweco.SIF.iMOD.Legends
{
    /// <summary>
    /// Class for legends with one or more classes of values with different legend properties
    /// </summary>
    public abstract class ClassLegend : Legend
    {
        /// <summary>
        /// Encoding used for writing legend files
        /// </summary>
        public static Encoding Encoding = Encoding.Default;

        /// <summary>
        /// List with of classes for this legend
        /// </summary>
        public List<RangeLegendClass> ClassList { get; set; }

        /// <summary>
        /// List of availables colors to assign to classes in this order. Colors are recycled if there are less than number of classes.
        /// Default is 9 colors ranging from LightSteelBlue via DarkGreen and Yellow to DarkRed.
        /// </summary>
        public List<Color> Colors { get; set; } = new List<Color>();

        /// <summary>
        /// Tolerance to determine if legend values are equal
        /// </summary>
        public static float Tolerance { get; set; } = 0.0000000001f;

        /// <summary>
        /// Create empty class legend object
        /// </summary>
        public ClassLegend() : base()
        {
            ClassList = new List<RangeLegendClass>();

            Colors = new List<Color> { Color.LightSteelBlue, Color.SteelBlue, Color.DarkBlue, Color.DarkGreen, Color.GreenYellow, Color.Yellow, Color.Orange, Color.Red, Color.DarkRed };
        }

        /// <summary>
        /// Create empty class legend object with specified description
        /// </summary>
        public ClassLegend(string description) : base(description)
        {
            this.Description = description;
            ClassList = new List<RangeLegendClass>();
        }

        /// <summary>
        /// Add classes of equal size between specified min- and maxvalue, plus a class below and above those values. The colors used are defined by the Colors property.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="classCount">number of classes, including class above and below min/max-values; if 0, the number of defined colors is used</param>
        public void AddLegendClasses(float minValue, float maxValue, int classCount = 0)
        {
            if (classCount == 0)
            {
                classCount = Colors.Count - 1;
            }
            else
            {
                classCount--;
            }

            // Make simple lineair legend from minValue to maxValue
            float classRange = (maxValue - minValue) / classCount;

            AddClass(new RangeLegendClass(minValue, minValue + classRange, minValue + " - " + (minValue + classRange), Colors[0]));
            for (int classIdx = 1; classIdx < classCount; classIdx++)
            {
                AddClass(new RangeLegendClass(minValue + classIdx * classRange, minValue + (classIdx + 1) * classRange, (minValue + classIdx * classRange) + " - " + (minValue + (classIdx + 1) * classRange), Colors[classIdx % Colors.Count]));
            }
            if ((long)maxValue > (classCount * classRange))
            {
                AddClass(new RangeLegendClass(minValue + classCount * classRange, (long)maxValue, "> " + classCount * classRange, Color.DarkRed));
            }
        }

        /// <summary>
        /// Add default surface level legend classes to this legend, ranging from red (high) to blue (low): 23 classes equal to or greater than zero and one class for negative values
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        public void AddSurfaceLevelLegendClasses(float minValue, float maxValue)
        {
            // Make simple lineair legend from minValue to maxValue
            float classRange = (maxValue - minValue) / 24;
            int d = CommonUtils.Max(0, -1 * (int)Math.Floor(Math.Log(classRange))) + 1;
            // Since Math.Round only allows maximum 15 decimals, limit to 15
            d = CommonUtils.Min(d, 15);

            AddClass(new RangeLegendClass(Round(minValue, d), Round(minValue + classRange, d), Round(minValue, d) + " - " + Round((minValue + classRange), d), Color.FromArgb(0, 77, 169)));
            AddClass(new RangeLegendClass(Round(minValue + 1 * classRange, d), Round(minValue + 2 * classRange, d), Round((minValue + 1 * classRange), d) + " - " + Round((minValue + 2 * classRange), d), Color.FromArgb(0, 125, 192)));
            AddClass(new RangeLegendClass(Round(minValue + 2 * classRange, d), Round(minValue + 3 * classRange, d), Round((minValue + 2 * classRange), d) + " - " + Round((minValue + 3 * classRange), d), Color.FromArgb(0, 173, 215)));
            AddClass(new RangeLegendClass(Round(minValue + 3 * classRange, d), Round(minValue + 4 * classRange, d), Round((minValue + 3 * classRange), d) + " - " + Round((minValue + 4 * classRange), d), Color.FromArgb(0, 216, 236)));
            AddClass(new RangeLegendClass(Round(minValue + 4 * classRange, d), Round(minValue + 5 * classRange, d), Round((minValue + 4 * classRange), d) + " - " + Round((minValue + 5 * classRange), d), Color.FromArgb(1, 255, 247)));
            AddClass(new RangeLegendClass(Round(minValue + 5 * classRange, d), Round(minValue + 6 * classRange, d), Round((minValue + 5 * classRange), d) + " - " + Round((minValue + 6 * classRange), d), Color.FromArgb(1, 255, 179)));
            AddClass(new RangeLegendClass(Round(minValue + 6 * classRange, d), Round(minValue + 7 * classRange, d), Round((minValue + 6 * classRange), d) + " - " + Round((minValue + 7 * classRange), d), Color.FromArgb(1, 255, 117)));
            AddClass(new RangeLegendClass(Round(minValue + 7 * classRange, d), Round(minValue + 8 * classRange, d), Round((minValue + 7 * classRange), d) + " - " + Round((minValue + 8 * classRange), d), Color.FromArgb(1, 255, 49)));
            AddClass(new RangeLegendClass(Round(minValue + 8 * classRange, d), Round(minValue + 9 * classRange, d), Round((minValue + 8 * classRange), d) + " - " + Round((minValue + 9 * classRange), d), Color.FromArgb(8, 255, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 9 * classRange, d), Round(minValue + 10 * classRange, d), Round((minValue + 9 * classRange), d) + " - " + Round((minValue + 10 * classRange), d), Color.FromArgb(76, 255, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 10 * classRange, d), Round(minValue + 11 * classRange, d), Round((minValue + 10 * classRange), d) + " - " + Round((minValue + 11 * classRange), d), Color.FromArgb(144, 255, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 11 * classRange, d), Round(minValue + 12 * classRange, d), Round((minValue + 11 * classRange), d) + " - " + Round((minValue + 12 * classRange), d), Color.FromArgb(206, 255, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 12 * classRange, d), Round(minValue + 13 * classRange, d), Round((minValue + 12 * classRange), d) + " - " + Round((minValue + 13 * classRange), d), Color.FromArgb(255, 248, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 13 * classRange, d), Round(minValue + 14 * classRange, d), Round((minValue + 13 * classRange), d) + " - " + Round((minValue + 14 * classRange), d), Color.FromArgb(255, 215, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 14 * classRange, d), Round(minValue + 15 * classRange, d), Round((minValue + 14 * classRange), d) + " - " + Round((minValue + 15 * classRange), d), Color.FromArgb(255, 185, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 15 * classRange, d), Round(minValue + 16 * classRange, d), Round((minValue + 15 * classRange), d) + " - " + Round((minValue + 16 * classRange), d), Color.FromArgb(255, 152, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 16 * classRange, d), Round(minValue + 17 * classRange, d), Round((minValue + 16 * classRange), d) + " - " + Round((minValue + 17 * classRange), d), Color.FromArgb(255, 124, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 17 * classRange, d), Round(minValue + 18 * classRange, d), Round((minValue + 17 * classRange), d) + " - " + Round((minValue + 18 * classRange), d), Color.FromArgb(255, 91, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 18 * classRange, d), Round(minValue + 19 * classRange, d), Round((minValue + 18 * classRange), d) + " - " + Round((minValue + 19 * classRange), d), Color.FromArgb(255, 57, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 19 * classRange, d), Round(minValue + 20 * classRange, d), Round((minValue + 19 * classRange), d) + " - " + Round((minValue + 20 * classRange), d), Color.FromArgb(255, 27, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 20 * classRange, d), Round(minValue + 21 * classRange, d), Round((minValue + 20 * classRange), d) + " - " + Round((minValue + 21 * classRange), d), Color.FromArgb(250, 0, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 21 * classRange, d), Round(minValue + 22 * classRange, d), Round((minValue + 21 * classRange), d) + " - " + Round((minValue + 22 * classRange), d), Color.FromArgb(203, 0, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 22 * classRange, d), Round(minValue + 23 * classRange, d), Round((minValue + 22 * classRange), d) + " - " + Round((minValue + 23 * classRange), d), Color.FromArgb(160, 0, 0)));
            AddClass(new RangeLegendClass(Round(minValue + 23 * classRange, d), Round(minValue + 24 * classRange, d), Round((minValue + 23 * classRange), d) + " - " + Round((minValue + 24 * classRange), d), Color.FromArgb(113, 0, 0)));
            if (Round(maxValue, d) > Round(minValue + 24 * classRange, d))
            {
                AddClass(new RangeLegendClass(Round(minValue + 24 * classRange, d), maxValue, "> " + Round(minValue + 24 * classRange, d), Color.FromArgb(80, 0, 0)));
            }
        }
        /// <summary>
        /// Adds default deptht legend classes to this legend: 21 classes equal to or greater than zero and one class for negative values
        /// </summary>
        public void AddDepthLegendClasses()
        {
            AddClass(new RangeLegendClass(float.MinValue, -0.0000001f, "< 0", Color.FromArgb(0, 116, 232)));
            AddClass(new RangeLegendClass(-0.0000001f, 0.0000001f, "0", Color.FromArgb(241, 241, 241)));
            AddClass(new RangeLegendClass(0.0000001f, 0.01f, "0.0-0.01", Color.FromArgb(225, 255, 200)));
            AddClass(new RangeLegendClass(0.01f, 0.1f, "0.0-0.1", Color.FromArgb(191, 252, 131)));
            AddClass(new RangeLegendClass(0.1f, 0.2f, "0.1-0.2", Color.FromArgb(118, 244, 0)));
            AddClass(new RangeLegendClass(0.2f, 0.4f, "0.2-0.4", Color.FromArgb(79, 198, 0)));
            AddClass(new RangeLegendClass(0.4f, 0.6f, "0.4-0.6", Color.FromArgb(39, 152, 0)));
            AddClass(new RangeLegendClass(0.6f, 0.8f, "0.6-0.8", Color.FromArgb(13, 121, 0)));
            AddClass(new RangeLegendClass(0.8f, 1f, "0.8-1", Color.FromArgb(0, 106, 0)));
            AddClass(new RangeLegendClass(1, 1.2f, "1.0-1.2", Color.FromArgb(152, 120, 0)));
            AddClass(new RangeLegendClass(1.2f, 1.4f, "1.2-1.4", Color.FromArgb(255, 255, 128)));
            AddClass(new RangeLegendClass(1.4f, 1.6f, "1.4-1.6", Color.FromArgb(255, 255, 0)));
            AddClass(new RangeLegendClass(1.6f, 1.8f, "1.6-1.8", Color.FromArgb(255, 185, 50)));
            AddClass(new RangeLegendClass(1.8f, 2, "1.8-2", Color.FromArgb(255, 115, 0)));
            AddClass(new RangeLegendClass(2f, 5f, "2-5", Color.FromArgb(225, 0, 0)));
            AddClass(new RangeLegendClass(5f, 10f, "5-10", Color.FromArgb(185, 0, 0)));
            AddClass(new RangeLegendClass(10, 20, "10-20", Color.FromArgb(132, 10, 0)));
            AddClass(new RangeLegendClass(20, 30, "20-30", Color.FromArgb(221, 0, 221)));
            AddClass(new RangeLegendClass(30, 40, "30-40", Color.FromArgb(164, 0, 164)));
            AddClass(new RangeLegendClass(40, 50, "40-50", Color.FromArgb(114, 1, 132)));
            AddClass(new RangeLegendClass(50, 100, "50-100", Color.FromArgb(97, 0, 147)));
            AddClass(new RangeLegendClass(100, float.MaxValue, "> 100", Color.FromArgb(64, 0, 128)));
        }

        public void AddDifferenceLegendClasses(float minValue, float maxValue, Color? noDifferenceColor = null, bool isColorsReversed = false)
        {
            if (noDifferenceColor == null)
            {
                // use grey as default color for no difference
                noDifferenceColor = Color.FromArgb(240, 240, 240);
            }

            if (minValue.Equals(maxValue))
            {
                AddClass(new ValueLegendClass(minValue, minValue.ToString(), (Color)noDifferenceColor));
            }
            else
            {
                // Make expontional legend from minValue to maxValue with 19 classes, including 'zero'
                Color[] diffColors = new Color[] { Color.FromArgb(79, 0, 0), Color.FromArgb(108, 0, 0), Color.FromArgb(128, 0, 0), Color.FromArgb(162, 0, 0), Color.FromArgb(209, 0, 0), Color.FromArgb(255, 0, 0),
                                                   Color.FromArgb(255, 63, 0), Color.FromArgb(255, 110, 0), Color.FromArgb(255, 170, 0), (Color) noDifferenceColor,
                                                   Color.FromArgb(168, 232, 0), Color.FromArgb(50, 233, 0), Color.FromArgb(0, 203, 0), Color.FromArgb(0, 170, 0), Color.FromArgb(0, 140, 0),
                                                   Color.FromArgb(0, 114, 0), Color.FromArgb(0, 70, 0), Color.FromArgb(0, 43, 0), Color.FromArgb(0, 23, 0)};
                if (isColorsReversed)
                {
                    diffColors.Reverse();
                }

                double maxClassRangeValue = Math.Max(Math.Abs(Math.Floor(minValue)), Math.Abs(Math.Ceiling(maxValue)));
                if (double.IsInfinity(maxClassRangeValue))
                {
                    maxClassRangeValue = float.MaxValue / 1000;
                }
                double maxClassRangeMultiplier = Math.Pow(maxClassRangeValue, 1d / 9d);
                int d = (int)CommonUtils.Max(0, -1 * (int)Math.Floor(Math.Log(maxClassRangeMultiplier))) + 1;

                // Add lowest negative class
                double lowerValue = -1 * Math.Ceiling(maxClassRangeValue);
                double upperValue = lowerValue / maxClassRangeMultiplier;
                AddClass(new RangeLegendClass(float.MinValue / 1000, (float)Math.Round(upperValue, d), "< " + Math.Round(upperValue, d), diffColors[0]));
                // Add next 7 negative classes
                for (int colIdx = 1; colIdx < 8; colIdx++)
                {
                    lowerValue = upperValue;
                    upperValue = lowerValue / maxClassRangeMultiplier;
                    AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d), (float)Math.Round(upperValue, d), Math.Round(lowerValue, d) + " - " + Math.Round(upperValue, d), diffColors[colIdx]));
                }
                // Add highest negative class
                lowerValue = upperValue;
                upperValue = lowerValue / maxClassRangeMultiplier;
                int d2 = d;
                if (upperValue < -0.001)
                {
                    upperValue = -0.001;
                    d2 = 3;
                }
                AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d), (float)Math.Round(upperValue, d2), Math.Round(lowerValue, d) + " - " + Math.Round(upperValue, d2), diffColors[8]));

                // Add 'no difference' class
                lowerValue = upperValue;
                upperValue = -1 * lowerValue;
                AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d2), (float)Math.Round(upperValue, d2), Math.Round(lowerValue, d2) + " - " + Math.Round(upperValue, d2), diffColors[9]));

                // Add lowest positive class
                lowerValue = upperValue;
                upperValue = maxClassRangeMultiplier;
                AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d2), (float)Math.Round(upperValue, d), Math.Round(lowerValue, d2) + " - " + Math.Round(upperValue, d), diffColors[10]));
                // Add next 7 positive classes
                for (int colIdx = 11; colIdx < 18; colIdx++)
                {
                    lowerValue = upperValue;
                    upperValue = lowerValue * maxClassRangeMultiplier;
                    AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d), (float)Math.Round(upperValue, d), Math.Round(lowerValue, d) + " - " + Math.Round(upperValue, d), diffColors[colIdx]));
                }
                // Add highest positive class
                lowerValue = upperValue;
                AddClass(new RangeLegendClass((float)Math.Round(lowerValue, d), float.MaxValue / 1000, "> " + Math.Round(lowerValue, d), diffColors[18]));

                Description = "Differences";
            }
        }

        /// <summary>
        /// Add default difference legend classes to this legend, ranging from green (positive) to red (negative): 16 classes between -50 en 50, and two classes above and below
        /// </summary>
        /// <param name="noDifferenceColor">use specified color for the value that signifies no difference</param>
        /// <param name="isColorsReversed">if true, legendcolors (red-green for neg-pos) are reversed to (green-red)</param>
        public void AddDifferenceLegendClasses(Color? noDifferenceColor = null, bool isColorsReversed = false)
        {
            if (noDifferenceColor == null)
            {
                noDifferenceColor = Color.FromArgb(246, 246, 246);
            }

            Color[] diffColors = new Color[] { Color.FromArgb(79, 0, 0), Color.FromArgb(108, 0, 0), Color.FromArgb(128, 0, 0), Color.FromArgb(162, 0, 0), Color.FromArgb(209, 0, 0), Color.FromArgb(255, 0, 0),
                                               Color.FromArgb(255, 63, 0), Color.FromArgb(255, 110, 0), Color.FromArgb(255, 170, 0), Color.FromArgb(251,231,189), (Color) noDifferenceColor,
                                               Color.FromArgb(226,244,179), Color.FromArgb(168, 232, 0), Color.FromArgb(50, 233, 0), Color.FromArgb(0, 203, 0), Color.FromArgb(0, 170, 0), Color.FromArgb(0, 140, 0),
                                               Color.FromArgb(0, 114, 0), Color.FromArgb(0, 70, 0), Color.FromArgb(0, 43, 0), Color.FromArgb(0, 23, 0)};
            if (isColorsReversed)
            {
                diffColors.Reverse();
            }

            AddClass(new RangeLegendClass(-9999999f, -50f, "< -50.0", diffColors[0]));
            AddClass(new RangeLegendClass(-50.0f, -1.50f, "-50.0 - -1.50", diffColors[1]));
            AddClass(new RangeLegendClass(-1.50f, -1.00f, "-1.50 - -1.00", diffColors[2]));
            AddClass(new RangeLegendClass(-1.00f, -0.75f, "-1.00 - -0.75", diffColors[3]));
            AddClass(new RangeLegendClass(-0.75f, -0.50f, "-0.75 - -0.50", diffColors[4]));
            AddClass(new RangeLegendClass(-0.50f, -0.25f, "-0.50 - -0.25", diffColors[5]));
            AddClass(new RangeLegendClass(-0.25f, -0.10f, "-0.25 - -0.10", diffColors[6]));
            AddClass(new RangeLegendClass(-0.10f, -0.05f, "-0.10 - -0.05", diffColors[7]));
            AddClass(new RangeLegendClass(-0.05f, -0.001f, "-0.05 - -1E-3", diffColors[8]));
            AddClass(new RangeLegendClass(-0.001f, -0.0000001f, "-1E-3 - -1E-7", diffColors[9]));
            AddClass(new RangeLegendClass(-0.0000001f, 0.0000001f, "-1E-7 - 1E-7", diffColors[10]));
            AddClass(new RangeLegendClass(0.0000001f, 0.001f, "1E-7 - 1E-3", diffColors[11]));
            AddClass(new RangeLegendClass(0.001f, 0.05f, "1E-3 - 0.05", diffColors[12]));
            AddClass(new RangeLegendClass(0.05f, 0.10f, "0.05 - 0.10", diffColors[13]));
            AddClass(new RangeLegendClass(0.10f, 0.25f, "0.10 - 0.25", diffColors[14]));
            AddClass(new RangeLegendClass(0.25f, 0.50f, "0.25 - 0.50", diffColors[15]));
            AddClass(new RangeLegendClass(0.50f, 0.75f, "0.50 - 0.75", diffColors[16]));
            AddClass(new RangeLegendClass(0.75f, 1.00f, "0.75 - 1.00", diffColors[17]));
            AddClass(new RangeLegendClass(1.00f, 1.50f, "1.00 - 1.50", diffColors[18]));
            AddClass(new RangeLegendClass(1.50f, 50.00f, "1.50 - 50.0", diffColors[19]));
            AddClass(new RangeLegendClass(50.00f, 9999999f, "> 50.0", diffColors[20]));
            Description = "Differences";
        }

        /// <summary>
        /// Add factor legend classes for to specified legend. Use specified color for the value that signifies nodifference
        /// </summary>
        /// <param name="noDifferenceColor">color to use when there is no difference (default is white)</param>
        /// <param name="isColorsReversed">if true, legendcolors (red-green for neg-pos) are reversed to (green-red)</param>
        public void AddDivisionLegendClasses(Color? noDifferenceColor = null, bool isColorsReversed = false)
        {
            if (noDifferenceColor == null)
            {
                noDifferenceColor = Color.FromArgb(225, 225, 225);
            }

            Color[] diffColors = new Color[] { Color.FromArgb(255, 0, 0), Color.FromArgb(60, 30, 0), Color.FromArgb(85, 43, 0), Color.FromArgb(128, 64, 0), Color.FromArgb(174, 87, 0), Color.FromArgb(210, 105, 0),
                                                   Color.FromArgb(255, 128, 0), Color.FromArgb(249, 191, 57), Color.FromArgb(255, 255, 89), Color.FromArgb(255,255,157), (Color) noDifferenceColor,
                                                   Color.FromArgb(162, 254, 122), Color.FromArgb(50, 233, 0), Color.FromArgb(0, 203, 0), Color.FromArgb(0, 170, 0), Color.FromArgb(0, 140, 0),
                                                   Color.FromArgb(0, 114, 0), Color.FromArgb(0, 70, 0), Color.FromArgb(0, 43, 0), Color.FromArgb(0, 64, 128) };
            if (isColorsReversed)
            {
                diffColors.Reverse();
            }

            AddClass(new RangeLegendClass(-9999999f, 0f, "< 0.0", diffColors[0]));
            AddClass(new RangeLegendClass(0f, 0.01f, "0.00 - 0.01", diffColors[1]));
            AddClass(new RangeLegendClass(0.01f, 0.05f, "0.01 - 0.05", diffColors[2]));
            AddClass(new RangeLegendClass(0.05f, 0.10f, "0.05 - 0.10", diffColors[3]));
            AddClass(new RangeLegendClass(0.10f, 0.25f, "0.10 - 0.25", diffColors[4]));
            AddClass(new RangeLegendClass(0.25f, 0.33f, "0.25 - 0.33", diffColors[5]));
            AddClass(new RangeLegendClass(0.33f, 0.50f, "0.33 - 0.50", diffColors[6]));
            AddClass(new RangeLegendClass(0.50f, 0.66f, "0.50 - 0.66", diffColors[7]));
            AddClass(new RangeLegendClass(0.66f, 0.90f, "0.66 - 0.90", diffColors[8]));
            AddClass(new RangeLegendClass(0.90f, 0.9999f, "0.90 - 1.00", diffColors[9]));
            AddClass(new RangeLegendClass(0.9999f, 1.0001f, "1.0", diffColors[10]));
            AddClass(new RangeLegendClass(1.0001f, 1.1f, "1.0 - 1.1", diffColors[11]));
            AddClass(new RangeLegendClass(1.1f, 1.5f, "1.1 - 1.5", diffColors[12]));
            AddClass(new RangeLegendClass(1.5f, 2.0f, "1.5 - 2.0", diffColors[13]));
            AddClass(new RangeLegendClass(2.0f, 3.0f, "2.0 - 3.0", diffColors[14]));
            AddClass(new RangeLegendClass(3.0f, 4.0f, "3.0 - 4.0", diffColors[15]));
            AddClass(new RangeLegendClass(4.0f, 5.0f, "4.0 - 5.0", diffColors[16]));
            AddClass(new RangeLegendClass(5.0f, 10.0f, "5.0 - 10.0", diffColors[17]));
            AddClass(new RangeLegendClass(10.0f, 100.00f, "10.0 - 100.0", diffColors[18]));
            AddClass(new RangeLegendClass(100.00f, 9999999f, "> 100.0", diffColors[19]));
            Description = "Differences";
        }
        /// <summary>
        /// Adds the specified RangeLegendClass object to the legend
        /// </summary>
        /// <param name="item"></param>
        public void AddClass(RangeLegendClass item)
        {
            ClassList.Add(item);
            Sort();
        }

        /// <summary>
        ///  Adds a range class from the current highest value in the legend upto the specified maximum value
        /// </summary>
        /// <param name="maxValue"></param>
        /// <param name="label"></param>
        /// <param name="color"></param>
        /// <param name="description"></param>
        public void AddUpperRangeClass(float maxValue, string label, Color color, string description = null)
        {
            float highValue = float.MinValue;
            foreach (RangeLegendClass legendClass in ClassList)
            {
                if (legendClass.MaxValue > highValue)
                {
                    highValue = legendClass.MaxValue;
                }
            }
            if (maxValue <= highValue)
            {
                throw new Exception("AddUpperRange: given maxvalue (" + maxValue + ") is equal or smaller than currently defined highest value (" + highValue + ") in legend \"" + this.Description + "\"");
            }
            RangeLegendClass upperRangeClass = new RangeLegendClass(highValue, maxValue, label, description);
        }

        /// <summary>
        /// Create an upper RangeClass with max value two times the currently defined highest value, color is DarkViolet.
        /// For use when combinations of the currently defined results are expected and need a legendclass
        /// </summary>
        /// <param name="label"></param>
        /// <param name="isRangeShownInLabel"></param>
        public void AddUpperRangeClass(string label, bool isRangeShownInLabel = false)
        {
            AddUpperRangeClass(Color.DarkViolet, label, isRangeShownInLabel);
        }

        /// <summary>
        /// Create a default upper RangeClass. For use when combinations of the currently defined results are expected and need a legendclass
        /// Default maxValue is 2 times a currently defined highest value
        /// </summary>
        /// <param name="color"></param>
        /// <param name="classLabel"></param>
        /// <param name="isRangeShownInLabel"></param>
        /// <param name="classDescription"></param>
        public void AddUpperRangeClass(Color color, string classLabel, bool isRangeShownInLabel = false, string classDescription = null)
        {
            float highValue = float.MinValue;
            foreach (RangeLegendClass legendClass in ClassList)
            {
                if (legendClass.MaxValue > highValue)
                {
                    highValue = legendClass.MaxValue;
                }
            }
            float maxValue = 2 * highValue;
            if (isRangeShownInLabel)
            {
                classLabel = highValue + "-" + maxValue + " - " + classLabel;
            }
            RangeLegendClass upperRangeClass = new RangeLegendClass(highValue, maxValue, classLabel, Color.DarkViolet, classDescription);
            AddClass(upperRangeClass);
        }

        /// <summary>
        /// Add classes between defined classranges if there is a gap between classes, use color DarkViolet.
        /// </summary>
        /// <param name="classLabel"></param>
        /// <param name="isRangeShownInLabel"></param>
        /// <param name="classDescription"></param>
        public void AddInbetweenClasses(string classLabel = null, bool isRangeShownInLabel = false, string classDescription = null)
        {
            AddInbetweenClasses(Color.DarkViolet, classLabel, isRangeShownInLabel, classDescription);
        }

        /// <summary>
        /// Add classes between defined classranges if there is gap between classes, use specified color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="classLabel"></param>
        /// <param name="isRangeShownInLabel"></param>
        /// <param name="classDescription"></param>
        public void AddInbetweenClasses(Color color, string classLabel = null, bool isRangeShownInLabel = false, string classDescription = null)
        {
            Sort();
            RangeLegendClass prevLegendClass = ClassList[0];
            for (int idx = 1; idx < ClassList.Count; idx++)
            {
                RangeLegendClass legendClass = ClassList[idx];
                if ((prevLegendClass.MinValue - legendClass.MaxValue) > Tolerance)
                {
                    // a gap is found between both this and the previous class
                    string label = classLabel ?? (legendClass.MaxValue + " - " + prevLegendClass.MinValue);
                    if ((classLabel != null) && isRangeShownInLabel)
                    {
                        label = legendClass.MaxValue + "-" + prevLegendClass.MinValue + " - " + label;
                    }
                    RangeLegendClass fillClass = new RangeLegendClass(legendClass.MaxValue, prevLegendClass.MinValue, label, color, classDescription);
                    ClassList.Insert(idx, fillClass);
                }
                prevLegendClass = legendClass;
            }
        }

        /// <summary>
        /// Creates a long/wide string representation of this legend object with for each class, the class range followed by the class description
        /// </summary>
        /// <returns>one or more lines</returns>
        public override string ToLongString()
        {
            string legendString = CreateLegendHeader();

            foreach (RangeLegendClass item in ClassList)
            {
                if ((item.Description != null) && !item.Description.Equals(string.Empty))
                {
                    legendString += item.ToString() + "\t - " + item.Description + "\r\n";
                }
                else
                {
                    legendString += item.ToString() + "\t - " + item.Label + "\r\n";
                }
            }

            return legendString;
        }

        /// <summary>
        /// Creates a short string representation of this legend object with for each class, the class range followed by the class labels
        /// </summary>
        /// <returns>one or more lines</returns>
        public override string ToString()
        {
            string legendString = CreateLegendHeader();

            foreach (RangeLegendClass item in ClassList)
            {
                legendString += "[" + item.ToString() + "): " + item.Label + "\r\n";
            }

            return legendString;
        }

        /// <summary>
        /// Sorts classes from high (maxvalue) to low (maxvalue) 
        /// </summary>
        public void Sort()
        {
            // Sort from low to high
            ClassList.Sort();
            // Reverse: from high to low
            ClassList.Reverse();
        }

        /// <summary>
        /// Write iMOD LEG-file with the specified path and filename
        /// </summary>
        /// <param name="filename"></param>
        public virtual void WriteLegendFile(string filename)
        {
            string legendString = this.ClassList.Count.ToString() + ",1,1,1,1,1,1,1\r\n";
            legendString += "UPPERBND,LOWERBND,IRED,IGREEN,IBLUE,DOMAIN\r\n";
            foreach (RangeLegendClass item in ClassList)
            {
                legendString += item.MaxValue.ToString(EnglishCultureInfo) + "," + item.MinValue.ToString(EnglishCultureInfo) + "," + item.Color.R.ToString() + "," + item.Color.G.ToString() + "," + item.Color.B.ToString() + ",\"" + item.Label + "\"\r\n";
            }

            StreamWriter sw = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                sw = new StreamWriter(filename, false, Encoding);
                sw.Write(legendString);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not write legendfile " + filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
        }

        /// <summary>
        /// Import legend class from iMOD legend file
        /// </summary>
        /// <param name="legFilename"></param>
        public void ImportClasses(string legFilename)
        {
            if (!Path.GetExtension(legFilename).ToLower().Equals(".leg"))
            {
                throw new Exception("Invalid LEG-file  extension: " + legFilename);
            }

            StreamReader sr = null;
            try
            {
                sr = new StreamReader(legFilename);
                string legendWholeLine = sr.ReadLine();
                string[] split = legendWholeLine.Split(',');
                int numberClasses = int.Parse(split[0]);
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string legendClassWholeLine = sr.ReadLine();
                    string[] legendClassInput = CommonUtils.SplitQuoted(legendClassWholeLine, ',', '"', true);
                    RangeLegendClass legendLine = new RangeLegendClass(float.Parse(legendClassInput[1], EnglishCultureInfo), float.Parse(legendClassInput[0], EnglishCultureInfo),
                        legendClassInput[5], Color.FromArgb(int.Parse(legendClassInput[2]), int.Parse(legendClassInput[3]),
                        int.Parse(legendClassInput[4])));

                    AddClass(legendLine);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading " + legFilename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
        }

        /// <summary>
        /// Round float value to specified number of decimals
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalCount">number of decimals to round to</param>
        /// <returns></returns>
        protected float Round(float value, int decimalCount)
        {
            return (float)Math.Round(value, decimalCount);
        }
    }
}
