// Sweco.SIF.GIS is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.GIS.
// 
// Sweco.SIF.GIS is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.GIS is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.GIS. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GIS
{
    /// <summary>
    /// SIF utilities for GIS-processing
    /// </summary>
    public class GISUtils
    {
        /// <summary>
        /// Create xy-string with format (x,y) for specified xy-coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static string GetXYString(float x, float y)
        {
            return "(" + x.ToString("F0") + "," + y.ToString("F0") + ")";
        }

        /// <summary>
        /// Check if points of polygon are defined in a clockwise direction.
        /// </summary>
        /// <param name="polygonPoints">list of polygon point, either closed or not closed</param>
        /// <returns></returns>
        public static bool IsClockwise(List<Point> polygonPoints)
        {
            double sum = 0.0;
            Point vector1 = polygonPoints[polygonPoints.Count - 1];
            for (int i = 0; i < polygonPoints.Count; i++)
            {
                Point vector2 = polygonPoints[i];
                sum += (vector2.X - vector1.X) * (vector2.Y + vector1.Y);
                vector1 = vector2;
            }
            return sum > 0.0;
        }

        /// <summary>
        /// Retrieves intersection of two linesegments defined by points P1-P2 and with P3-P4
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <returns>null if linesegments do not intersect</returns>
        public static Point Intersect(Point p1, Point p2, Point p3, Point p4)
        {
            // from: https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/
            // and: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection

            // Transform points in line with a form ax + by = c
            double a1 = p2.Y - p1.Y;
            double b1 = p1.X - p2.X;
            double c1 = a1 * p1.X + b1 * p1.Y;
            double a2 = p4.Y - p3.Y;
            double b2 = p3.X - p4.X;
            double c2 = a2 * p3.X + b2 * p3.Y;

            double det = a1 * b2 - a2 * b1;
            if (det == 0)
            {
                // lines are parallel
                return null;
            }
            else
            {
                return new DoublePoint((b2 * c1 - b1 * c2) / det, (a1 * c2 - a2 * c1) / det);
            }
        }

        /// <summary>
        /// Tests if specified point is inside a polygon with algorithm W.R. Franklin. The polygon may be concave. The direction that you list the polygon vertices (clockwise or counterclockwise) does not matter.
        /// It is optional to repeat the first vertex at the end. The polygon may contain multiple seperate components and/or holes. Check following website of Franklin for details. If a point is very close to an edge beware of roundoff errors.
        /// If you want to know when a point is exactly on the boundary, you need another algorithm. Any particular point is always classified consistently the same way. 
        /// Depending on internal roundoff errors, PNPOLY may say that a point inside or outside. However it will always give the same answer when tested against the same lines. 
        /// Algorithm is a C# implementation of W.R. Franklin's pnpoly-algorithm of 12/11/2018, see: https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html#The%20C%20Code. License: see above.
        /// </summary>
        /// <param name="testx">x-coordinate of the test point</param>
        /// <param name="testy">y-coordinate of the test point</param>
        /// <param name="polygonXArray">array containing the x-coordinates of the polygon's vertices</param>
        /// <param name="polygonYArray">array containing the x-coordinates of the polygon's vertices</param>
        /// <returns></returns>
        public static bool IsPointInPolygon(double testx, double testy, double[] polygonXArray, double[] polygonYArray)
        {
            /// License to Use - pnpoly algorithm W.R. Franklin
            /// Copyright (c) 1970-2003, Wm. Randolph Franklin
            /// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
            /// 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
            /// 2. Redistributions in binary form must reproduce the above copyright notice in the documentation and/or other materials provided with the distribution.
            /// 3. The name of W. Randolph Franklin may not be used to endorse or promote products derived from this Software without specific prior written permission.
            /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
            /// https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html#Converting%20the%20Code%20to%20All%20Integers

            bool isInside = false;
            int i, j;
            int nvert = polygonXArray.Length;

            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((polygonYArray[i] > testy) != (polygonYArray[j] > testy)) &&
                    (testx < (polygonXArray[j] - polygonXArray[i]) * (testy - polygonYArray[i]) / (polygonYArray[j] - polygonYArray[i]) + polygonXArray[i]))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        /// <summary>
        /// Tests if specified point is inside a polygon with algorithm W.R. Franklin. The polygon may be concave. The direction that you list the polygon vertices (clockwise or counterclockwise) does not matter.
        /// It is optional to repeat the first vertex at the end. The polygon may contain multiple seperate components and/or holes. Check following website of Franklin for details. If a point is very close to an edge beware of roundoff errors.
        /// If you want to know when a point is exactly on the boundary, you need another algorithm. Any particular point is always classified consistently the same way. 
        /// Depending on internal roundoff errors, PNPOLY may say that a point inside or outside. However it will always give the same answer when tested against the same lines. 
        /// This algorithm is a C# implementation of W.R. Franklin's pnpoly-algorithm of 12/11/2018, see: https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html#The%20C%20Code.
        /// </summary>
        /// <param name="testx">x-coordinate of the test point</param>
        /// <param name="testy">y-coordinate of the test point</param>
        /// <param name="polygonXArray">array containing the x-coordinates of the polygon's vertices</param>
        /// <param name="polygonYArray">array containing the x-coordinates of the polygon's vertices</param>
        /// <returns></returns>
        private static bool IsPointInPolygon(float testx, float testy, float[] polygonXArray, float[] polygonYArray)
        {
            /// License to Use - pnpoly algorithm W.R. Franklin
            /// Copyright (c) 1970-2003, Wm. Randolph Franklin
            /// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
            /// 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
            /// 2. Redistributions in binary form must reproduce the above copyright notice in the documentation and/or other materials provided with the distribution.
            /// 3. The name of W. Randolph Franklin may not be used to endorse or promote products derived from this Software without specific prior written permission.
            /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
            /// https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html#Converting%20the%20Code%20to%20All%20Integers

            bool isInside = false;
            int i, j;
            int nvert = polygonXArray.Length;

            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((polygonYArray[i] > testy) != (polygonYArray[j] > testy)) &&
                    (testx < (polygonXArray[j] - polygonXArray[i]) * (testy - polygonYArray[i]) / (polygonYArray[j] - polygonYArray[i]) + polygonXArray[i]))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }
    }
}
