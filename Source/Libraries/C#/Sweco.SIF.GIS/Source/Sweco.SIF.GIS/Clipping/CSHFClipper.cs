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

namespace Sweco.SIF.GIS.Clipping
{
    /// <summary>
    /// Class that provides methods for clipping features based on Cohen–Sutherland, Sutherland–Hodgman and Franklin's algorithms:
    /// - Polygons are clipped with the Sutherland–Hodgman algorithm which can find the polygon that is the intersection between an arbitrary polygon (the “subject polygon”) and a convex polygon (the “clip polygon”).
    ///   Note: if the subject polygon is concave at vertices outside the clipping polygon, the new polygon may have coincident (i.e., overlapping) edges (acceptable for rendering, but not for other applications)
    /// - Lines are clipped with the Cohen–Sutherland algorithm which can clip a linesegment efficiently against a clip extent
    /// - Points are clipped against a polygon with Franklin's pnpoly-algorithm.
    /// </summary>
    /// <remarks>
    /// See: https://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
    /// See: https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
    /// See: https://wrfranklin.org/Research/Short_Notes/pnpoly.html
    ///  
    /// 
    /// </remarks>
    public class CSHFClipper
    {
        /// <summary>
        /// Cohen–Sutherland clipping algorithm clips a line from P1 = (x1, y1) to P2 = (x2, y2) against a rectangle with diagonal from clipExtent.
        /// </summary>
        /// <param name="lineSegment"></param>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public static LineSegment ClipLine(LineSegment lineSegment, Extent clipExtent)
        {
            // Code based on C/C++ implementation in: https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm 

            double x1 = lineSegment.P1.X;
            double y1 = lineSegment.P1.Y;
            double x2 = lineSegment.P2.X;
            double y2 = lineSegment.P2.Y;

            // compute outcodes for P1, P2, and whatever point lies outside the clip rectangle
            int outcode1 = ComputeOutCode(x1, y1, clipExtent);
            int outcode2 = ComputeOutCode(x2, y2, clipExtent);
            bool accept = false;

            while (true)
            {
                if ((outcode1 | outcode2) == 0)
                {
                    // Bitwise OR is 0. Trivially accept and get out of loop
                    accept = true;
                    break;
                }
                else if ((outcode1 & outcode2) != 0)
                {
                    // Bitwise AND is not 0. (implies both end points are in the same region outside the window). Reject and get out of loop
                    break;
                }
                else
                {
                    // failed both tests, so calculate the line segment to clip
                    // from an outside point to an intersection with clip edge
                    double x = double.NaN;
                    double y = double.NaN;

                    // At least one endpoint is outside the clip rectangle; pick it.
                    int outcodeOut = (outcode1 != 0) ? outcode1 : outcode2;

                    // Now find the intersection point;
                    // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)
                    if ((outcodeOut & TOP) != 0)
                    {           // point is above the clip rectangle
                        x = x1 + (x2 - x1) * (clipExtent.ury - y1) / (y2 - y1);
                        y = clipExtent.ury;
                    }
                    else if ((outcodeOut & BOTTOM) != 0)
                    { // point is below the clip rectangle
                        x = x1 + (x2 - x1) * (clipExtent.lly - y1) / (y2 - y1);
                        y = clipExtent.lly;
                    }
                    else if ((outcodeOut & RIGHT) != 0)
                    {  // point is to the right of clip rectangle
                        y = y1 + (y2 - y1) * (clipExtent.urx - x1) / (x2 - x1);
                        x = clipExtent.urx;
                    }
                    else if ((outcodeOut & LEFT) != 0)
                    {   // point is to the left of clip rectangle
                        y = y1 + (y2 - y1) * (clipExtent.llx - x1) / (x2 - x1);
                        x = clipExtent.llx;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcode1)
                    {
                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(x1, y1, clipExtent);
                    }
                    else
                    {
                        x2 = x;
                        y2 = y;
                        outcode2 = ComputeOutCode(x2, y2, clipExtent);
                    }
                }
            }
            LineSegment clippedLineSegment = null;
            if (accept)
            {
                clippedLineSegment = new LineSegment(new DoublePoint(x1, y1), new DoublePoint(x2, y2));
            }
            return clippedLineSegment;
        }

        /// <summary>
        /// This clips the subject polygon against the convex clip polygon 
        /// Note: an exception is thrown if all points are collineair or for polygons with less than 3 points.
        /// </summary>
        /// <remarks>
        /// Code from: http://rosettacode.org/wiki/Sutherland-Hodgman_polygon_clipping. License: GNU Free Documentation License 1.2. 
        /// Based on the psuedocode from: http://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman
        /// </remarks>
        /// <param name="subjectPolygon">Can be concave or convex</param>
        /// <param name="clipPolygon">Must be convex</param>
        /// <returns>The intersection of the two polygons (or null)</returns>
        public static List<Point> ClipPolygon(List<Point> subjectPolygon, List<Point> clipPolygon)
        {
            if (subjectPolygon.Count < 3 || clipPolygon.Count < 3)
            {
                throw new ArgumentException(string.Format("The polygons passed in must have at least 3 points: subject={0}, clip={1}", subjectPolygon.Count.ToString(), clipPolygon.Count.ToString()));
            }

            List<Point> outputList = subjectPolygon.ToList();

            //	Make sure polygon points have clockwise order
            if (!IsClockwise(outputList))
            {
                outputList.Reverse();
            }

            //	Walk around the clip polygon clockwise
            foreach (LineSegment clipEdge in IterateEdgesClockwise(clipPolygon))
            {
                List<Point> inputList = outputList.ToList();		//	clone it
                outputList.Clear();

                if (inputList.Count == 0)
                {
                    //	Sometimes when the polygons don't intersect, this list goes to zero.  Jump out to avoid an index out of range exception
                    break;
                }

                Point S = inputList[inputList.Count - 1];
                foreach (Point E in inputList)
                {
                    if (clipEdge.IsInside(E))
                    {
                        if (!clipEdge.IsInside(S))
                        {
                            Point point = GISUtils.Intersect(S, E, clipEdge.P1, clipEdge.P2);
                            if (point == null)
                            {
                                throw new ApplicationException("Line segments don't intersect");		//	may be colinear, or may be a bug
                            }
                            else
                            {
                                outputList.Add(point);
                            }
                        }

                        outputList.Add(E);
                    }
                    else if (clipEdge.IsInside(S))
                    {
                        Point point = GISUtils.Intersect(S, E, clipEdge.P1, clipEdge.P2);
                        if (point == null)
                        {
                            throw new ApplicationException("Line segments don't intersect");		//	may be colinear, or may be a bug
                        }
                        else
                        {
                            outputList.Add(point);
                        }
                    }

                    S = E;
                }
            }

            return outputList;
        }

        /// <summary>
        /// Clip points in list with specified polygon
        /// </summary>
        /// <param name="points"></param>
        /// <param name="clipPolygon"></param>
        /// <returns></returns>
        public static List<Point> ClipPoints(List<Point> points, List<Point> clipPolygon)
        {
            // Code makes direct use of Franklin's pnpoly-algorithm, but is made more efficient for testing a lists of points against the same polygon by first testing against the polygon extent
            if (!clipPolygon[0].Equals(clipPolygon[clipPolygon.Count - 1]))
            {
                // Ensure last point equals first point
                clipPolygon = new List<Point>(clipPolygon);
                clipPolygon.Add(clipPolygon[0]);
            }

            Extent extent = new Extent(clipPolygon);

            List<Point> clippedPoints = new List<Point>();
            foreach (Point point in points)
            {
                // First do fast test based on extent
                if (extent.Contains(point.X, point.Y))
                {
                    if (point.IsInside(clipPolygon))
                    {
                        clippedPoints.Add(point);
                    }
                }
            }
            return clippedPoints;
        }

        /// <summary>
        /// Tests if specified polygon is clockwise ordered. Note: For colinear points an exception is thrown.
        /// Note2: use this method only for this CSHF-algorithm, may does not work as expected for other polygons
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static bool IsClockwise(List<Point> polygon)
        {
            for (int cntr = 2; cntr < polygon.Count; cntr++)
            {
                bool? isLeft = new LineSegment(polygon[0], polygon[1]).IsLeftOf(polygon[cntr]);
                if (isLeft != null)		//	some of the points may be colinear.  That's ok as long as the overall is a polygon
                {
                    return !isLeft.Value;
                }
            }

            throw new ArgumentException("All the points in the polygon are colinear");
        }

        /// <summary>
        /// This iterates through the edges of the polygon, always clockwise
        /// </summary>
        private static IEnumerable<LineSegment> IterateEdgesClockwise(List<Point> polygon)
        {
            if (IsClockwise(polygon))
            {
                #region Already clockwise

                for (int cntr = 0; cntr < polygon.Count - 1; cntr++)
                {
                    yield return new LineSegment(polygon[cntr], polygon[cntr + 1]);
                }

                yield return new LineSegment(polygon[polygon.Count - 1], polygon[0]);

                #endregion
            }
            else
            {
                #region Reverse

                for (int cntr = polygon.Count - 1; cntr > 0; cntr--)
                {
                    yield return new LineSegment(polygon[cntr], polygon[cntr - 1]);
                }

                yield return new LineSegment(polygon[0], polygon[polygon.Count - 1]);

                #endregion
            }
        }

        // Line Clipping
        private const int INSIDE = 0; // 0000
        private const int LEFT = 1;   // 0001
        private const int RIGHT = 2;  // 0010
        private const int BOTTOM = 4; // 0100
        private const int TOP = 8;    // 1000

        /// <summary>
        /// Compute the bit code for a point (x, y) using the clip rectangle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="extent"></param>
        /// <returns></returns>
        private static int ComputeOutCode(double x, double y, Extent extent)
        {
            int code = INSIDE;              // initialised as being inside of [[clip window]]
            if (x < extent.llx)             // to the left of clip window
                code |= LEFT;
            else if (x > extent.urx)        // to the right of clip window
                code |= RIGHT;
            if (y < extent.lly)             // below the clip window
                code |= BOTTOM;
            else if (y > extent.ury)        // above the clip window
                code |= TOP;
            return code;
        }
    }
}
