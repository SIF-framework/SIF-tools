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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GIS
{
    /// Base class for storing and comparing 2D points.
    public abstract class Point : IPoint, IEquatable<Point>, IComparer<Point>
    {
        /// <summary>
        /// The culture info object that defines the english language settings for parsing and formatting point values
        /// </summary>
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// X-coordinate as a double 
        /// </summary>
        public abstract double X { get; set; }

        /// <summary>
        /// Y-coordinate as a double
        /// </summary>
        public abstract double Y { get; set; }

        /// <summary>
        /// X-coordinate as a string
        /// </summary>
        public abstract string XString { get; set; }

        /// <summary>
        /// Y-coordinate as a string 
        /// </summary>
        public abstract string YString { get; set; }

        /// <summary>
        /// The tolerance/margin for errors/differences in determining a match of Points: coordinates that differ more than tolerance are considered different
        /// </summary>
        public static double Tolerance
        {
            get { return tolerance; }
            set
            {
                tolerance = value;
                ToleranceSquare = tolerance * tolerance;
            }
        }
        private static double tolerance = 0.0001;

        /// <summary>
        /// The square of the tolerance/margin for errors/differences, precalculated for faster performance
        /// </summary>
        public static double ToleranceSquare { get; private set; } = tolerance * tolerance;

        /// <summary>
        /// The number of decimals that floating point values are rounded to when formatted as a string, or when comparing 
        /// The precision is always less than the number of decimals in the tolerance. When the tolerance is made smaller,
        /// the precision is adjusted for this if necessary.
        /// </summary>
        public static int Precision { get; set; } = 3;

        /// <summary>
        /// The tolerance/margin for equality as used in for example IndexOf-method to really retrieve this (unique) point from a list.
        /// </summary>
        public static double IdentityTolerance { get; } = 0.000000001;

        /// <summary>
        /// The square of tolerance/margin for equality, precalculated for faster performance
        /// </summary>
        protected static double IdentityToleranceSquare { get; } = IdentityTolerance * IdentityTolerance;

        /// <summary>
        /// Converts the specified tolerance to a precision: -log10(tolerance) rounded to an integer
        /// This will return 0 for 1, 1 for 0.1 and 2 for 0.01, etc. For tolerance 0, 0 is returned
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static int GetPrecision(double tolerance)
        {
            if (tolerance.Equals(0) || (tolerance >= 1))
            {
                return 0;
            }
            else
            {
                return (int)Math.Round(-Math.Log10(tolerance), 0);
            }
        }

        /// <summary>
        /// Calculates vector from point p2 to p1
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Vector operator -(Point p1, Point p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y);
        }

        /// <summary>
        /// Moves point p over vector v
        /// </summary>
        /// <param name="p">Point</param>
        /// <param name="v">Vector</param>
        /// <returns></returns>
        public static Point operator +(Point p, Vector v)
        {
            Point newPoint = (Point)p.Copy();
            newPoint.X = p.X + v.dX;
            newPoint.Y = p.Y + v.dY;
            return newPoint;
        }

        /// <summary>
        /// Initializes this Point-object to given string coordinates
        /// </summary>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        protected void Initialize(string xString, string yString)
        {
            this.XString = xString;
            this.YString = yString;
        }

        /// <summary>
        /// Formats the point coordinates as "(x,y)" with Point.Precision decimals
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToString(Precision);
        }

        /// <summary>
        /// Formats the point coordinates as "(x,y)" with the given number of decimals
        /// </summary>
        /// <param name="decimalcount"></param>
        /// <returns></returns>
        public virtual string ToString(int decimalcount)
        {
            return "(" + Math.Round(X, Precision).ToString("F" + decimalcount, englishCultureInfo) + "," + Math.Round(Y, Precision).ToString("F" + decimalcount, englishCultureInfo) + ")";
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        /// <summary>
        /// Checks if this Point object is contained within the given extent
        /// </summary>
        /// <param name="extent"></param>
        /// <returns></returns>
        public bool IsContainedBy(Extent extent)
        {
            return ((X >= extent.llx) && (Y >= extent.lly) && (X < extent.urx) && (Y < extent.ury));
        }

        /// <summary>
        /// Checks for equality between this and another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (!(obj is Point))
            {
                return false;
            }
            else
            {
                Point other = (Point)obj;
                return Equals(other);
            }
        }

        /// <summary>
        /// Compares this Point object with another object: -1 is returned when the other object coordinates is smaller or null, 0 is return if object coordinates are equal, 1 is returned otherwise
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return -1;
            }
            else if (!(obj is Point))
            {
                return -1;
            }
            else
            {
                Point other = (Point)obj;
                return Compare(this, other);
            }
        }

        /// <summary>
        /// Checks for equality between this object's and given point object's coordinates: distances more than Point.Tolerance result in a difference
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(Point other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                double dx = (this.X - other.X);
                double dy = (this.Y - other.Y);
                return ((dx * dx) + (dy * dy)) <= ToleranceSquare;
            }
        }

        /// <summary>
        /// Checks for simularity between this object's and given point object's coordinates: distances more than Point.Tolerance result in a difference
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool IsSimilarTo(Point other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                double dx = (this.X - other.X);
                double dy = (this.Y - other.Y);
                return ((dx * dx) + (dy * dy)) <= ToleranceSquare;
            }
        }

        /// <summary>
        /// Checks for equality between this object's and given point object's coordinates: distances more than Point.IdentityTolerance result in a difference
        /// Note: the specified Point.Tolerance is used to determine equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool IsIdenticalTo(Point other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                double dx = (this.X - other.X);
                double dy = (this.Y - other.Y);
                return ((dx * dx) + (dy * dy)) <= IdentityToleranceSquare;
            }
        }

        /// <summary>
        /// Compares two Point objects: -1 is returned when point1 is null or the coordinates of point1 are smaller, 0 is return if objects ()coordinates are equal, 1 is returned otherwise
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public virtual int Compare(Point point1, Point point2)
        {
            if (point1 == null)
            {
                return -1;
            }
            else if (point2 == null)
            {
                return 1;
            }
            else
            {
                // Compare first to X, and if equal to Y-coordinate
                if (point1.Equals(point2))
                {
                    return 0;
                }
                else if ((Math.Abs(point1.X - point2.X) > tolerance))
                {
                    return point1.X.CompareTo(point2.X);
                }
                else if ((Math.Abs(point1.Y - point2.Y) > tolerance))
                {
                    return point1.Y.CompareTo(point2.Y);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Creates a copy of this Point instance
        /// </summary>
        /// <returns></returns>
        public abstract Point Copy();

        /// <summary>
        /// Calculates distance between this and other point
        /// </summary>
        /// <param name="otherPoint"></param>
        /// <returns></returns>
        public virtual double GetDistance(Point otherPoint)
        {
            double dx = (this.X - otherPoint.X);
            double dy = (this.Y - otherPoint.Y);
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Calculates distance between this and other point with specified number of decimals
        /// </summary>
        /// <param name="otherPoint"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public virtual double GetDistance(Point otherPoint, int precision)
        {
            double dx = (this.X - otherPoint.X);
            double dy = (this.Y - otherPoint.Y);
            return Math.Round(Math.Sqrt((dx * dx) + (dy * dy)), precision);
        }

        /// <summary>
        /// Calculates distance between this and other point (with specified x and y-coordinates)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public double GetDistance(float x, float y)
        {
            double dx = (this.X - x);
            double dy = (this.Y - y);
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Calculates distance between this and other point (with specified x and y-coordinates) with specified number of decimals
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public double GetDistance(float x, float y, int precision)
        {
            double dx = (this.X - x);
            double dy = (this.Y - y);
            return Math.Round(Math.Sqrt((dx * dx) + (dy * dy)), precision);
        }

        /// <summary>
        /// Return the point on the specified line that is closest to this point
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public Point SnapToRay(Point P1, Point P2)
        {
            Vector v = P2 - P1;
            Vector w = this - P1;

            double c1 = w * v;
            double c2 = v * v;
            double b = c1 / c2;

            Point Pb = P1 + b * v;
            return Pb;
        }

        /// <summary>
        /// Return the point on the specified linesegment that is closest to this point
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public Point SnapToLineSegment(Point P1, Point P2)
        {
            Vector v = P2 - P1;
            Vector w = this - P1;

            double c1 = w * v;
            if (c1 <= 0)
            {
                return P1;
            }

            double c2 = v * v;
            if (c2 <= c1)
            {
                return P2;
            }

            double b = c1 / c2;

            Point Pb = P1 + b * v;
            return Pb;
        }

        /// <summary>
        /// Return the point on the specified linesegment that is closest to this point
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public Point SnapToLineSegmentOptimized(Point P1, Point P2)
        {
            double dxV = P2.X - P1.X; // Vector v = P2 - P1;
            double dyV = P2.Y - P1.Y;
            double dxW = X - P1.X; // Vector w = this - P1;
            double dyW = Y - P1.Y;

            double c1 = dxV * dxW + dyV * dyW; // w * v;
            if (c1 <= 0)
            {
                return P1;
            }

            double c2 = dxV * dxV + dyV * dyV; // v * v;
            if (c2 <= c1)
            {
                return P2;
            }

            double b = c1 / c2;

            Point Pb = P1.Copy();
            Pb.X = P1.X + b * dxV; // P1 + b * v;
            Pb.Y = P1.Y + b * dyV;
            return Pb;
        }

        /// <summary>
        /// Checks if the perpendicular from this point to the specified segement intersects with that segment
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public bool HasPerpendicularIntersection(Point P1, Point P2)
        {
            double dxV = P2.X - P1.X; // Vector v = P2 - P1;
            double dyV = P2.Y - P1.Y;
            double dxW = X - P1.X; // Vector w = this - P1;
            double dyW = Y - P1.Y;

            double c1 = dxV * dxW + dyV * dyW; // w * v;
            if (c1 < 0)
            {
                return false;
            }

            double c2 = dxV * dxV + dyV * dyV; // v * v;
            if (c2 < c1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates snap distance of point when snapped to specified line segment
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public double GetSnapDistance(LineSegment segment)
        {
            Point snappedPoint = SnapToLineSegmentOptimized(segment.P1, segment.P2);
            return GetDistance(snappedPoint);
        }

        /// <summary>
        /// Get distance from a point to a perpendicular snap of the point to the specified segment
        /// </summary>
        /// <param name="segment">segment to snap to</param>
        /// <returns>float.NaN is no perpendicular snap can be made</returns>
        public double GetPerpendicularSnapDistance(LineSegment segment)
        {
            return GetPerpendicularSnapDistance(segment.P1, segment.P2);
        }

        /// <summary>
        /// Get distance from a point to a perpendicular snap of the point to the specified segment
        /// </summary>
        /// <param name="p1">first point of segment to snap to</param>
        /// <param name="p2">second point of segment to snap to</param>
        /// <returns>float.NaN is no perpendicular snap can be made</returns>
        public double GetPerpendicularSnapDistance(Point p1, Point p2)
        {
            if (HasPerpendicularIntersection(p1, p2))
            {
                Point snappedPoint = SnapToLineSegmentOptimized(p1, p2);
                return GetDistance(snappedPoint);
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Get distance from a point to a perpendicular snap of the point to the specified segment with specified number of decimals
        /// </summary>
        /// <param name="p1">first point of segment to snap to</param>
        /// <param name="p2">second point of segment to snap to</param>
        /// <param name="precision">precision of result: number of decimals to round to</param>
        /// <returns>float.NaN is no perpendicular snap can be made</returns>
        public double GetPerpendicularSnapDistance(Point p1, Point p2, int precision)
        {
            if (HasPerpendicularIntersection(p1, p2))
            {
                Point snappedPoint = SnapToLineSegmentOptimized(p1, p2);
                return GetDistance(snappedPoint, precision);
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Tests if a point is Left|On|Right of an infinite line.
        /// </summary>
        /// <param name="P0">first point of line</param>
        /// <param name="P1">second point of line</param>
        /// <param name="P2">point to check for</param>
        /// <returns>larger than 0 if P2 is left of the line through P0 and P1; equal to 0 if P2 is on the line; less than 0 if P2 is right of the line</returns>
        protected double IsLeftOf(Point P0, Point P1, Point P2)
        {
            return ((P1.X - P0.X) * (P2.Y - P0.Y) - (P2.X - P0.X) * (P1.Y - P0.Y));
        }

        /// <summary>
        /// Tests if specified point is inside a polygon with algorithm W.R. Franklin. The polygon may be concave. The direction that you list the polygon vertices (clockwise or counterclockwise) does not matter.
        /// It is optional to repeat the first vertex at the end. The polygon may contain multiple seperate components and/or holes. Check following website of Franklin for details. If a point is very close to an edge beware of roundoff errors.
        /// If you want to know when a point is exactly on the boundary, you need another algorithm. Any particular point is always classified consistently the same way. 
        /// Depending on internal roundoff errors, PNPOLY may say that a point inside or outside. However it will always give the same answer when tested against the same lines. 
        /// Algorithm is a C# implementation of W.R. Franklin's pnpoly-algorithm of 12/11/2018, see: https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html#The%20C%20Code.
        /// </summary>
        /// <param name="points">polygon points</param>
        /// <returns></returns>
        public bool IsInside(List<Point> points)
        {
            // Copyright(c) 1970 - 2003, Wm.Randolph Franklin
            // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/ or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
            // 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimers.
            // 2. Redistributions in binary form must reproduce the above copyright notice in the documentation and / or other materials provided with the distribution.
            // 3. The name of W. Randolph Franklin may not be used to endorse or promote products derived from this Software without specific prior written permission.
            // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

            bool isInside = false;
            int i, j;
            int nvert = points.Count;
            double testy = this.Y;
            double testx = this.X;

            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((points[i].Y > testy) != (points[j].Y > testy)) &&
                    (testx < (points[j].X - points[i].X) * (testy - points[i].Y) / (points[j].Y - points[i].Y) + points[i].X))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        /// <summary>
        /// Interpolate value at this point from values at specified points using Inverse Distance Weighted (IDW) method.
        /// If the smoothing is zero and the power is high, the interpolation changes a lot around the points to give them their exact value.
        /// If the smoothing is high and the power is one, the result is much smoother, but the values at the points are not maintained.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="values"></param>
        /// <param name="power">power to apply to distance to weight it</param>
        /// <param name="smoothing">higher value gives more distance than actual distance, lowering influence around points</param>
        /// <param name="maxDistance"></param>
        /// <returns>float if denominator is zero</returns>
        public float InterpolateIDW(List<Point> points, List<float> values, float power = 2, float smoothing = 0, float maxDistance = float.NaN)
        {
            // code based on: http://geoexamples.blogspot.com/2012/05/creating-grid-from-scattered-data-using.html
            // The distance is the Cartesian one, plus the smoothing factor. 
            // If the distance is close to the precision of the float numbers, the source data value is used instead of the interpolated one, to avoid strange results.
            double nominator = 0;
            double denominator = 0;
            double dist;
            double distPow;
            double smoothingSquared = smoothing * smoothing;
            bool isMaxDistanceDefined = !maxDistance.Equals(float.NaN);

            for (int i = 0; i < points.Count; i++)
            {
                Point point = points[i];
                dist = Math.Sqrt((X - point.X) * (X - point.X) + (Y - point.Y) * (Y - point.Y) + smoothingSquared);
                
                if (dist < 0.0000000001)
                {
                    // If the point is really close to one of the data points, return the data point value to avoid singularities
                    return values[i];
                }
                if (isMaxDistanceDefined && (dist > maxDistance))
                {
                    continue;
                }
                distPow = Math.Pow(dist, power);
                nominator = nominator + (values[i] / distPow);
                denominator = denominator + (1 / distPow);
            }

            // Return NoData if the denominator is zero
            if (denominator > 0)
            {
                return (float) (nominator / denominator);
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Retrieve minimum distance to specified extent
        /// </summary>
        /// <param name="extent"></param>
        /// <returns></returns>
        public double GetDistance(Extent extent)
        {
            double minX = extent.llx;
            double minY = extent.lly;
            if (X > minX)
            {
                if (X > extent.urx)
                {
                    minX = extent.urx;
                }
                else
                {
                    minX = X;
                }
            }
            if (Y > minY)
            {
                if (Y > extent.ury)
                {
                    minY = extent.ury;
                }
                else
                {
                    minY = Y;
                }
            }

            return GetDistance((float)minX, (float)minY);
        }
    }
}
