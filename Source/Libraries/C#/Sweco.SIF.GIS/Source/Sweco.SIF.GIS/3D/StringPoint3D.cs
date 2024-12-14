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
    /// Class for storing point coordinates with a string datastructure. This datastructure won't give fast access and comparison speed,
    /// but can guarantee that a retrieved value is exactly equal to the defined value. The strings are formatted as english floating points.
    /// </summary>
    public class StringPoint3D : Point3D
    {
        /// <summary>
        /// The string variable x for direct access of the x-coordinate
        /// </summary>
        protected string xString;
        /// <summary>
        /// Get or set the value of x as a string. The string is formatted as an english floating point number.
        /// </summary>
        public override string XString
        {
            get { return xString; }
            set
            {
                xString = value;
                // Check if string represents a valid double value
                if (!double.TryParse(xString, NumberStyles.Float, englishCultureInfo, out double xValue))
                {
                    throw new Exception("Invalid x-value for point: " + xString);
                }
            }
        }

        /// <summary>
        /// The string variable y for direct access of the x-coordinate
        /// </summary>
        protected string yString;
        /// <summary>
        /// Get or set the value of y as a string. The string is formatted as an english floating point number.
        /// </summary>
        public override string YString
        {
            get { return yString; }
            set
            {
                yString = value;
                // Check if string represents a valid double value
                if (!double.TryParse(yString, NumberStyles.Float, englishCultureInfo, out double yValue))
                {
                    throw new Exception("Invalid y-value for point: " + yString);
                }
            }
        }

        /// <summary>
        /// The string variable y for direct access of the x-coordinate
        /// </summary>
        protected string zString;
        /// <summary>
        /// Get or set the value of z as a string. The string is formatted as an english floating point number.
        /// </summary>
        public override string ZString
        {
            get { return zString; }
            set
            {
                zString = value;
                // Check if string represents a valid double value
                if ((zString != null) && !double.TryParse(zString, NumberStyles.Float, englishCultureInfo, out double zValue))
                {
                    throw new Exception("Invalid z-value for point: " + zString);
                }
            }
        }

        /// <summary>
        /// Get or set the value of x as a floating point.
        /// </summary>
        public override double X
        {
            get { return double.Parse(xString, englishCultureInfo); }
            set { xString = Math.Round(value, Precision).ToString("F" + Precision, englishCultureInfo); }
        }

        /// <summary>
        /// Get or set the value of y as a floating point.
        /// </summary>
        public override double Y
        {
            get { return double.Parse(yString, englishCultureInfo); }
            set { yString = Math.Round(value, Precision).ToString("F" + Precision, englishCultureInfo); }
        }

        /// <summary>
        /// Get or set the value of z as a floating point.
        /// </summary>
        public override double Z
        {
            get { return double.Parse(zString, englishCultureInfo); }
            set { zString = Math.Round(value, Precision).ToString("F" + Precision, englishCultureInfo); }
        }

        /// Constructor for StringPoint object at (0,0)
        protected StringPoint3D()
        {
            this.xString = "NaN";
            this.yString = "NaN";
            this.zString = "NaN";
        }

        /// <summary>
        /// Constructor for StringPoint object with x, y and z-coordinates defined as floating point values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public StringPoint3D(double x, double y, double z = double.NaN)
        {
            Initialize(
                Math.Round(x, Precision).ToString("F" + Precision, englishCultureInfo),
                Math.Round(y, Precision).ToString("F" + Precision, englishCultureInfo),
                Math.Round(z, Precision).ToString("F" + Precision, englishCultureInfo));
        }

        /// <summary>
        /// Constructor for DoublePoint object with x, y and z-coordinates defined as strings
        /// Coordinate strings should be in english notation (decimalseperator is a point)
        /// </summary>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        /// <param name="zString"></param>
        public StringPoint3D(string xString, string yString, string zString = null)
        {
            Initialize(xString, yString, zString);
        }

        /// <summary>
        /// Returns hashcode for this object based on x, y and z
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return xString.GetHashCode() ^ yString.GetHashCode() ^ zString.GetHashCode();
        }

        /// <summary>
        /// Creates a copy of this StringPoint3D object
        /// </summary>
        /// <returns>StringPoint object</returns>
        public override Point Copy()
        {
            return new StringPoint3D(xString, yString, zString);
        }

        /// <summary>
        /// Creates a copy of this StringPoint3D object
        /// </summary>
        /// <returns>StringPoint3D object</returns>
        public override Point3D Copy3D()
        {
            return new StringPoint3D(xString, yString, zString);
        }
    }
}
