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
    /// <summary>
    /// Class for storing 3D point coordinates with a (double) floating point datastructure. This datastructure will give fast access and comparison speed,
    /// but will use large amount of memory
    /// </summary>
    public class DoublePoint3D : Point3D, IEquatable<DoublePoint3D>, IComparer<DoublePoint3D>
    {
        /// <summary>
        /// The floating point variable x for direct access of the x-coordinate
        /// </summary>
        protected double x;
        /// <summary>
        /// Get or set the value of x as a floating point number.
        /// </summary>
        public override double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// The floating point variable y for direct access of the y-coordinate
        /// </summary>
        protected double y;
        /// <summary>
        /// Get or set the value of y as a floating point number.
        /// </summary>
        public override double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Optional floating point variable z for direct access of the z-coordinate. Has a double.Nan value when not used.
        /// </summary>
        protected double z;
        /// <summary>
        /// Get or set the value of z as a floating point number.
        /// </summary>
        public override double Z
        {
            get { return z; }
            set { z = value; }
        }

        /// <summary>
        /// Get or set the value of x as a string. A get results in a string with Point.Precision decimals.
        /// The string should be formatted as an english floating point number.
        /// </summary>
        public override string XString
        {
            get { return Math.Round(x, Precision).ToString("F" + Precision, englishCultureInfo); }
            set
            {
                if (!double.TryParse(value, NumberStyles.Float, englishCultureInfo, out x))
                {
                    throw new Exception("Invalid x-value for point: " + value);
                }
            }
        }

        /// <summary>
        /// Get or set the value of y as a string. A get results in a string with Point.Precision decimals.
        /// The string should be formatted as an english floating point number.
        /// </summary>
        public override string YString
        {
            get { return Math.Round(y, Precision).ToString("F" + Precision, englishCultureInfo); }
            set
            {
                if (!double.TryParse(value, NumberStyles.Float, englishCultureInfo, out y))
                {
                    throw new Exception("Invalid y-value for point: " + value);
                }
            }
        }

        /// <summary>
        /// Get or set the value of z as a string. A get results in a string with Point.Precision decimals.
        /// The string should be formatted as an english floating point number.
        /// </summary>
        public override string ZString
        {
            get { return Math.Round(z, Precision).ToString("F" + Precision, englishCultureInfo); }
            set
            {
                if (!double.TryParse(value, NumberStyles.Float, englishCultureInfo, out z))
                {
                    throw new Exception("Invalid z-value for point: " + value);
                }
            }
        }

        /// <summary>
        /// Constructor for DoublePoint object at (0,0,0)
        /// </summary>
        public DoublePoint3D()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }

        /// <summary>
        /// Constructor for DoublePoint object with x, y and z-coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public DoublePoint3D(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// Constructor for DoublePoint object with x, y and z-coordinates defined as strings. Coordinate strings should be in english notation (decimalseperator is a point)
        /// </summary>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        /// <param name="zString"></param>
        public DoublePoint3D(string xString, string yString, string zString)
        {
            this.XString = xString;
            this.YString = yString;
            this.ZString = zString;
        }

        /// <summary>
        /// Creates a copy of this DoublePoint object
        /// </summary>
        /// <returns></returns>
        public override Point Copy()
        {
            return Copy3D();
        }

        /// <summary>
        /// Creates a copy of this DoublePoint object
        /// </summary>
        /// <returns></returns>
        public override Point3D Copy3D()
        {
            return new DoublePoint3D(x, y, z);
        }

        /// <summary>
        /// Checks if this instance equals the specified other DoublePoint object based on maximum Point.Tolerance distance between coordinates
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(DoublePoint3D other)
        {
            // A specialized DoublePoint equality method is defined for fast comparison
            if (other == null)
            {
                return false;
            }
            else
            {
                return ((Math.Abs(other.x - x) < Tolerance) && (Math.Abs(other.y - y) < Tolerance) && (Math.Abs(other.z - z) < Tolerance));
            }
        }

        /// <summary>
        /// Return -1,0 or 1 if X, Y and Z of point1 are smaller, equal or larger (within Point.Tolerance distance) than coordinates of point2
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public virtual int Compare(DoublePoint3D point1, DoublePoint3D point2)
        {
            // A specialized DoublePoint fast comparison method is defined
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
                if ((Math.Abs(point1.x - point2.x) > Tolerance))
                {
                    return point1.x.CompareTo(point2.x);
                }
                else if ((Math.Abs(point1.y - point2.y) > Tolerance))
                {
                    return point1.y.CompareTo(point2.y);
                }
                else if (!point1.z.Equals(double.NaN) && !point2.z.Equals(double.NaN))
                {
                    if (Math.Abs(point1.z - point2.z) > Tolerance)
                    {
                        return point1.z.CompareTo(point2.z);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (point1.z.Equals(double.NaN))
                {
                    return -1;
                }
                else if (point2.z.Equals(double.NaN))
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
