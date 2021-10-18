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
    /// Base class for storing and comparing 3D points.
    public abstract class Point3D : Point, IEquatable<Point3D>, IComparer<Point3D>
    {
        /// <summary>
        /// Z-coordinate as a double
        /// </summary>
        public abstract double Z { get; set; }

        /// <summary>
        /// Z-coordinate as a string
        /// </summary>
        public abstract string ZString { get; set; }

        /// <summary>
        /// Calculates vector from second to first point
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Vector3D operator -(Point3D p1, Point3D p2)
        {
            return new Vector3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        /// <summary>
        /// Moves 3D point location over specified vector
        /// </summary>
        /// <param name="p"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Point3D operator +(Point3D p, Vector3D v)
        {
            Point3D newPoint = (Point3D)p.Copy();
            newPoint.X = p.X + v.dX;
            newPoint.Y = p.Y + v.dY;
            newPoint.Z = p.Z + v.dZ;
            return newPoint;
        }

        /// <summary>
        /// Initializes this Point-object to given string coordinates
        /// </summary>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        /// <param name="zString"></param>
        protected void Initialize(string xString, string yString, string zString)
        {
            this.XString = xString;
            this.YString = yString;
            this.ZString = zString;
        }

        /// <summary>
        /// Formats the point coordinates as "(x,y,z)" with Point.Precision decimals
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToString(Precision);
        }

        /// <summary>
        /// Formats the point coordinates as "(x,y,z)" with the given number of decimals
        /// </summary>
        /// <param name="decimalcount"></param>
        /// <returns></returns>
        public override string ToString(int decimalcount)
        {
            return "(" + Math.Round(X, Precision).ToString("F" + decimalcount, englishCultureInfo) + "," + Math.Round(Y, Precision).ToString("F" + decimalcount, englishCultureInfo) + ("," + Math.Round(Z, Precision).ToString("F" + decimalcount, englishCultureInfo) + ")");
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
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
            else if (!(obj is Point3D))
            {
                return false;
            }
            else
            {
                Point3D other = (Point3D)obj;
                return Equals(other);
            }
        }

        /// <summary>
        /// Compares this Point object with another object: -1 is returned when the other object coordinates is smaller or null, 0 is return if object coordinates are equal, 1 is returned otherwise
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public new int CompareTo(object obj)
        {
            if (obj == null)
            {
                return -1;
            }
            else if (!(obj is Point3D))
            {
                return -1;
            }
            else
            {
                Point3D other = (Point3D)obj;
                return Compare(this, other);
            }
        }

        /// <summary>
        /// Checks for equality between this object's and given point object's coordinates: distances more than ErrorMargin result in a difference
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(Point3D other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (Math.Abs(other.X - X) < Tolerance) && (Math.Abs(other.Y - Y) < Tolerance) && (Math.Abs(other.Z - Z) < Tolerance);
            }
        }

        /// <summary>
        /// Compares two Point objects: -1 is returned when point1 is null or the coordinates of point1 are smaller, 0 is return if objects ()coordinates are equal, 1 is returned otherwise
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public virtual int Compare(Point3D point1, Point3D point2)
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
                if ((Math.Abs(point1.X - point2.X) > Tolerance))
                {
                    return point1.X.CompareTo(point2.X);
                }
                else if ((Math.Abs(point1.Y - point2.Y) > Tolerance))
                {
                    return point1.Y.CompareTo(point2.Y);
                }
                else if (Math.Abs(point1.Z - point2.Z) > Tolerance)
                {
                    return point1.Z.CompareTo(point2.Z);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Calculates distance between this and other 3D point
        /// </summary>
        /// <param name="otherPoint"></param>
        /// <returns></returns>
        public override double GetDistance(Point otherPoint)
        {
            double dx = (this.X - otherPoint.X);
            double dy = (this.Y - otherPoint.Y);
            if (otherPoint is Point3D)
            {
                double dz = (this.Z - ((Point3D)otherPoint).Z);
                return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
            }
            else
            {
                return Math.Sqrt((dx * dx) + (dy * dy));
            }
        }

        /// <summary>
        /// Calculates distance between this and other point with specified number of decimals
        /// </summary>
        /// <param name="otherPoint"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public override double GetDistance(Point otherPoint, int precision)
        {
            double dx = (this.X - otherPoint.X);
            double dy = (this.Y - otherPoint.Y);
            if (otherPoint is Point3D)
            {
                double dz = (this.Z - ((Point3D)otherPoint).Z);
                return Math.Round(Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)), precision);
            }
            else
            {
                return Math.Round(Math.Sqrt((dx * dx) + (dy * dy)), precision);
            }
        }

        /// <summary>
        /// Calculates distance between this and other 3D point (as specified by coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public double GetDistance(float x, float y, float z)
        {
            float dx = (float)(this.X - x);
            float dy = (float)(this.Y - y);
            float dz = (float)(this.Z - z);
            return (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        /// <summary>
        /// Return the point on the specified line that is closest to this point
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public Point3D SnapToRay(Point3D P1, Point3D P2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the point on the specified linesegment that is closest to this point
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns>the closest point on the line</returns>
        public Point3D SnapToLineSegment(Point3D P1, Point3D P2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a copy of this Point3D instance
        /// </summary>
        /// <returns></returns>
        public abstract Point3D Copy3D();
    }
}
