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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GISPlus
{
    /// <summary>
    /// Implementation of a 3D line segment between two 3D points, P1 and P2
    /// </summary>
    public class LineSegment3D : ILineSegment, IEquatable<LineSegment3D>
    {
        /// <summary>
        /// First point of 3D line segment
        /// </summary>
        public Point3D P1;

        /// <summary>
        /// Second point of 3D line segment
        /// </summary>
        public Point3D P2;

        /// <summary>
        /// Creates an empty 3D line segment
        /// </summary>
        public LineSegment3D()
        {
            this.P1 = null;
            this.P2 = null;
        }

        /// <summary>
        /// Creates a 3D line segment from two 3D points
        /// </summary>
        public LineSegment3D(Point3D p1, Point3D p2)
        {
            this.P1 = p1;
            this.P2 = p2;
        }

        /// <summary>
        /// Calculate length of this line segment
        /// </summary>
        public double Length
        {
            get { return P1.GetDistance(P2); }
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
        public virtual bool Equals(LineSegment3D other)
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
        /// /Intersects line segments.
        /// </summary>
        /// <param name="lineSegment"></param>
        /// <returns></returns>
        public Point3D Intersect(LineSegment3D lineSegment)
        {
            return Intersect(this, lineSegment);
        }

        /// <summary>
        /// /Intersects line segments. Currently not implemented.
        /// </summary>
        /// <param name="segment1"></param>
        /// <param name="segment2"></param>
        /// <returns></returns>
        public static Point3D Intersect(LineSegment3D segment1, LineSegment3D segment2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns hashcode for this object, based on P1, P2 and length
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // Automatically generated code by Visual Studio Express 2017 for calculating hascode
            var hashCode = -996201842;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point3D>.Default.GetHashCode(P1);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point3D>.Default.GetHashCode(P2);
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            return hashCode;
        }
    }
}
