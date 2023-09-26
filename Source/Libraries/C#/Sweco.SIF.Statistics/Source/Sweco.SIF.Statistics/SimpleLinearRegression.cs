// Sweco.SIF.Statistics is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Statistics.
// 
// Sweco.SIF.Statistics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Statistics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Statistics. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Statistics
{
    public class SimpleLinearRegression
    {
        public static void Fit(List<DateTime> timestamps, List<float> values, out double rSquared, out double yIntercept, out double slope)
        {
            double[] x = new double[timestamps.Count];
            double[] y = new double[timestamps.Count];

            for (int idx = 0; idx < timestamps.Count; idx++)
            {
                x[idx] = timestamps[idx].Subtract(timestamps[0]).TotalDays;
                y[idx] = values[idx];
            }

            Fit(x, y, out rSquared, out yIntercept, out slope);
        }

        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="rSquared">The r^2 value of the line.</param>
        /// <param name="yIntercept">The y-intercept value of the line (i.e. y = ax + b, yIntercept is b).</param>
        /// <param name="slope">The slop of the line (i.e. y = ax + b, slope is a).</param>
        public static void Fit(double[] xVals, double[] yVals, out double rSquared, out double yIntercept, out double slope)
        {
            // Source code from 'LinearRegression.cs' by NikolayIT at GitHub
            // URL: https://gist.github.com/NikolayIT/d86118a3a0cb3f5ed63d674a350d75f2

            if (xVals.Length != yVals.Length)
            {
                throw new Exception("Input values should be with the same length.");
            }

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;

            for (var i = 0; i < xVals.Length; i++)
            {
                var x = xVals[i];
                var y = yVals[i];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            var count = xVals.Length;
            var ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            // var ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            var rNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            var rDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            var sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            var meanX = sumOfX / count;
            var meanY = sumOfY / count;
            var dblR = rNumerator / Math.Sqrt(rDenom);

            rSquared = dblR * dblR;
            yIntercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public static void bestApproximate(List<DateTime> timestamps, List<float> values, out double m, out double c)
        {
            double[] x = new double[timestamps.Count];
            double[] y = new double[timestamps.Count];

            for (int idx = 0; idx < timestamps.Count; idx++)
            {
                x[idx] = timestamps[idx].Subtract(timestamps[0]).TotalDays;
                y[idx] = values[idx];
            }

            bestApproximate(x, y, out m, out c);
        }

        // function to calculate m and c that
        // best fit points represented by x[] and y[]
        public static void bestApproximate(double[] x, double[] y, out double m, out double c)
        {
            // Author: article contributed by Mrigendra Singh. 
            // Source code from URL: https://www.geeksforgeeks.org/represent-given-set-points-best-possible-straight-line/
            // Time Complexity : O(n)
            int n = x.Length;
            double sum_x = 0, sum_y = 0,
                         sum_xy = 0, sum_x2 = 0;

            for (int i = 0; i < n; i++)
            {
                sum_x += x[i];
                sum_y += y[i];
                sum_xy += x[i] * y[i];
                sum_x2 += Math.Pow(x[i], 2);
            }

            m = (n * sum_xy - sum_x * sum_y) / (n * sum_x2 - Math.Pow(sum_x, 2));

            c = (sum_y - m * sum_x) / n;

            Console.WriteLine("m = " + m);
            Console.WriteLine("c = " + c);
        }
    }
}
