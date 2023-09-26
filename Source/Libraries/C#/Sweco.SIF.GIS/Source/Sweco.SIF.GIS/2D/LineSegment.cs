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
    /// Implementation of a 2D line segment between two 2D points, P1 and P2
    /// </summary>
    public class LineSegment : IEquatable<LineSegment>
    {
        /// <summary>
        /// First point of line segment
        /// </summary>
        public Point P1;

        /// <summary>
        /// Second point of line segment
        /// </summary>
        public Point P2;

        /// <summary>
        /// Creates undefined LineSegment instance
        /// </summary>
        public LineSegment()
        {
            this.P1 = null;
            this.P2 = null;
        }

        /// <summary>
        /// Creates LineSegment instance with points P1 and P2
        /// </summary>
        public LineSegment(Point p1, Point p2)
        {
            this.P1 = p1;
            this.P2 = p2;
        }

        /// <summary>
        /// Check point is on line segment
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsInside(Point point)
        {
            bool? isLeft = IsLeftOf(point);
            if (isLeft == null)
            {
                //	Colinear points should be considered inside
                return true;
            }

            return !isLeft.Value;
        }

        /// <summary>
        /// Tells if the test point lies on the left side of the edge line
        /// </summary>
        public bool? IsLeftOf(Point point)
        {
            Vector tmp1 = point - P1; // edge.To - edge.From;
            Vector tmp2 = point - P2;

            double x = (tmp1.dX * tmp2.dY) - (tmp1.dY * tmp2.dX);		//	dot product of perpendicular?

            if (x < 0)
            {
                return false;
            }
            else if (x > 0)
            {
                return true;
            }
            else
            {
                //	Colinear points;
                return null;
            }
        }

        /// <summary>
        /// Calculate point were line segments intersect
        /// </summary>
        /// <param name="lineSegment"></param>
        /// <returns>null for parallel lines</returns>
        public Point Intersect(LineSegment lineSegment)
        {
            return Intersect(this, lineSegment);
        }

        /// <summary>
        /// Calculate point were line segments intersect
        /// </summary>
        /// <param name="segment1"></param>
        /// <param name="segment2"></param>
        /// <returns>null for parallel lines</returns>
        public static Point Intersect(LineSegment segment1, LineSegment segment2)
        {
            // from: https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/
            // and: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection

            Point p1 = segment1.P1;
            Point p2 = segment1.P2;
            Point p3 = segment2.P1;
            Point p4 = segment2.P2;

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
        /// Calculates overlap between this and the specified line segment
        /// </summary>
        /// <param name="segment2"></param>
        /// <returns></returns>
        public LineSegment Overlap(LineSegment segment2)
        {
            return Overlap(segment2.P1, segment2.P2);
        }

        /// <summary>
        /// Calculates overlap between this line segment and specified points of othter line segment
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns></returns>
        public LineSegment Overlap(Point P1, Point P2)
        {
            int precision = Point.GetPrecision(Point.Tolerance);

            // There are 4 options: 
            // A---B---C---D
            // 1) segment1 contains segment2;               e.g. P1 = A - D, P2 = B - C
            // 2) segment2 contains segment1;               e.g. P1 = B - C, P2 = A - D
            // 3) segment1 overlaps segment2 partly;        e.g. P1 = A - C, P2 = B - D  or  P1 = B - D, P2 = A - C     
            // 4) the two segment don't have any overlap;   e.g. P1 = A - B, P2 = B - D

            Point seg1P1snapped = this.P1.SnapToLineSegmentOptimized(P1, P2);
            Point seg1P2snapped = this.P2.SnapToLineSegmentOptimized(P1, P2);
            Point seg2P1snapped = P1.SnapToLineSegmentOptimized(this.P1, this.P2);
            Point seg2P2snapped = P2.SnapToLineSegmentOptimized(this.P1, this.P2);
            double seg1P1snapDistance = Math.Round(this.P1.GetDistance(seg1P1snapped), precision);
            double seg1P2snapDistance = Math.Round(this.P2.GetDistance(seg1P2snapped), precision);
            double seg2P1snapDistance = Math.Round(P1.GetDistance(seg2P1snapped), precision);
            double seg2P2snapDistance = Math.Round(P2.GetDistance(seg2P2snapped), precision);

            // Check for all options
            if (seg2P1snapDistance.Equals(0) && seg2P2snapDistance.Equals(0))
            {
                // option 1: segment1 contains segment2
                // P1 = A - D, P2 = B - C => Overlap = B - C
                return new LineSegment(P1, P2);
            }
            else if (seg1P1snapDistance.Equals(0) && seg1P2snapDistance.Equals(0))
            {
                // option 2: segment2 contains segment1, segment1 is completely removed by the difference
                // P1 = B - C, P2 = A - D => Overlap = B - C
                return new LineSegment(this.P1, this.P2);
            }
            else if (seg1P2snapDistance.Equals(0) && seg2P1snapDistance.Equals(0))
            {
                // option 3a: second part of segment1 overlaps first part of segment2: startpoint of segment1 is on segment2, endpoint of segment1 is on segment2
                // P1 = A - B - C, P2 = B - C - D  =>  Overlap = B - C,
                return new LineSegment(P1, this.P2);
            }
            else if (seg1P1snapDistance.Equals(0) && seg2P2snapDistance.Equals(0))
            {
                // option 3b: first part of segment1 overlaps second part of segment2: startpoint of segment2 is on segment1, endpoint of segment1 is on segment2
                // P1 = B - C - D, P2 = A - B - C  => Overlap = B - C
                return new LineSegment(this.P1, P2);
            }
            else
            {
                // option 4b: no overlap at all
                return null;
            }
        }

        /// <summary>
        /// Length of line segment in default (undefined) unit
        /// </summary>
        public double Length
        {
            get { return P1.GetDistance(P2); }
        }

        /// <summary>
        /// Returns angle in degrees between [0, 360). 
        /// Direction of positive x-axis is 0 degrees, direction of positive y-axis is 90 degrees
        /// direction of negative y-axis is 270 degrees and direction of negative x-axis is 180 degress
        /// </summary>
        /// <returns>float.NaN if P1==P2</returns>
        public double CalculateAngle()
        {
            double dx = P2.X - P1.X;
            double dy = P2.Y - P1.Y;
            double angle;

            if (dx.Equals(0))
            {
                if (dy.Equals(0))
                {
                    angle = float.NaN;
                }
                else
                {
                    angle = (dy > 0) ? 90 : 270;
                }
            }
            else
            {
                angle = 180 * Math.Atan(dy / dx) / Math.PI;
                if (dx < 0)
                {
                    angle += 180;
                }
                else if (dy < 0)
                {
                    angle += 360;
                }
            }

            return angle;
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
        /// Checks for equality between this object's and given point object's coordinates: distances between coordinates of more than Point.Tolerance result in a difference
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(LineSegment other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (P1.Equals(other.P1))
                {
                    return P2.Equals(other.P2);
                }
                else if (P1.Equals(other.P2))
                {
                    return P2.Equals(other.P1);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns hashcode of this object based on P1, P2 and Length
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = -996201842;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(P1);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(P2);
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Calculate the distance between point and this segment
        /// </summary>
        /// <param name="point"></param>
        /// <param name="closest"></param>
        /// <returns></returns>
        public double GetDistance(Point point, out Point closest)
        {
            // Source: http://csharphelper.com/blog/2014/08/find-the-shortest-distance-between-two-line-segments-in-c/
            // Author: Rod Stephens
            // License: none

            double dx = P2.X - P1.X;
            double dy = P2.Y - P1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = P1;
                dx = point.X - P1.X;
                dy = point.Y - P1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((point.X - P1.X) * dx + (point.Y - P1.Y) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new DoublePoint(P1.X, P1.Y);
                dx = point.X - P1.X;
                dy = point.Y - P1.Y;
            }
            else if (t > 1)
            {
                closest = new DoublePoint(P2.X, P2.Y);
                dx = point.X - P2.X;
                dy = point.Y - P2.Y;
            }
            else
            {
                closest = new DoublePoint(P1.X + t * dx, P1.Y + t * dy);
                dx = point.X - closest.X;
                dy = point.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Return the shortest distance between this linesegment (P1 --> P2) and another (p3 --> p4).
        /// </summary>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="close1"></param>
        /// <param name="close2"></param>
        /// <returns></returns>
        public double GetDistance(Point p3, Point p4, out Point close1, out Point close2)
        {
            // Source: http://csharphelper.com/blog/2014/08/find-the-shortest-distance-between-two-line-segments-in-c/
            // Author: Rod Stephens
            // License: none

            // See if the segments intersect.
            bool lines_intersect, segments_intersect;
            Point intersection;
            FindIntersection(p3, p4, out lines_intersect, out segments_intersect, out intersection, out close1, out close2);
            if (segments_intersect)
            {
                // They intersect.
                close1 = intersection;
                close2 = intersection;
                return 0;
            }

            // Find the other possible distances.
            Point closest;
            LineSegment S2 = new LineSegment(p3, p4);
            double best_dist = double.MaxValue, test_dist;

            // Try P1.
            test_dist = S2.GetDistance(P1, out closest);
            if (test_dist < best_dist)
            {
                best_dist = test_dist;
                close1 = P1;
                close2 = closest;
            }

            // Try P2.
            test_dist = S2.GetDistance(P2, out closest);
            if (test_dist < best_dist)
            {
                best_dist = test_dist;
                close1 = P2;
                close2 = closest;
            }

            // Try p3.
            test_dist = GetDistance(p3, out closest);
            if (test_dist < best_dist)
            {
                best_dist = test_dist;
                close1 = closest;
                close2 = p3;
            }

            // Try p4.
            test_dist = GetDistance(p4, out closest);
            if (test_dist < best_dist)
            {
                best_dist = test_dist;
                close1 = closest;
                close2 = p4;
            }

            return best_dist;
        }

        /// <summary>
        /// Find the point of intersection between this linesegment (P1 --> P2) and another (p3 --> p4).
        /// </summary>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="lines_intersect"></param>
        /// <param name="segments_intersect"></param>
        /// <param name="intersection"></param>
        /// <param name="close_p1"></param>
        /// <param name="close_p2"></param>
        protected void FindIntersection(Point p3, Point p4, out bool lines_intersect, out bool segments_intersect, out Point intersection, out Point close_p1, out Point close_p2)
        {
            // Source: http://csharphelper.com/blog/2014/08/find-the-shortest-distance-between-two-line-segments-in-c/
            // Author: Rod Stephens
            // License: none

            // Get the segments' parameters.
            double dx12 = P2.X - P1.X;
            double dy12 = P2.Y - P1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1;
            try
            {
                t1 = ((P1.X - p3.X) * dy34 + (p3.Y - P1.Y) * dx34) / denominator;
            }
            catch
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new DoublePoint(double.NaN, double.NaN);
                close_p1 = new DoublePoint(double.NaN, double.NaN);
                close_p2 = new DoublePoint(double.NaN, double.NaN);
                return;
            }
            lines_intersect = true;

            double t2 = ((p3.X - P1.X) * dy12 + (P1.Y - p3.Y) * dx12) / -denominator;

            // Find the point of intersection.
            intersection = new DoublePoint(P1.X + dx12 * t1, P1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new DoublePoint(P1.X + dx12 * t1, P1.Y + dy12 * t1);
            close_p2 = new DoublePoint(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }

        ///// <summary>
        ///// Checks for simularity between this object's and given point object's coordinates: distances more than Point.Tolerance result in a difference
        ///// </summary>
        ///// <param name="other"></param>
        ///// <returns></returns>
        //public virtual bool IsSimilarTo(Point other)
        //{
        //    if (other == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        double dx = (this.X - other.X);
        //        double dy = (this.Y - other.Y);
        //        return ((dx * dx) + (dy * dy)) <= Point.ToleranceSquare;
        //    }
        //}

        ///// <summary>
        ///// Checks for equality between this object's and given point object's coordinates: distances more than Point.IdentityTolerance result in a difference
        ///// Note: the specified Point.Tolerance is used to determine equality.
        ///// </summary>
        ///// <param name="other"></param>
        ///// <returns></returns>
        //public virtual bool IsIdenticalTo(Point other)
        //{
        //    if (other == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        double dx = (this.X - other.X);
        //        double dy = (this.Y - other.Y);
        //        return ((dx * dx) + (dy * dy)) <= Point.IdentityToleranceSquare;
        //    }
        //}
    }
}
