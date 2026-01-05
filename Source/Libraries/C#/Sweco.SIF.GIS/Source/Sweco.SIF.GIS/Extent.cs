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
using Sweco.SIF.Common;

namespace Sweco.SIF.GIS
{
    /// <summary>
    /// Class with implementation of GIS extent
    /// </summary>
    public class Extent : IEquatable<Extent>
    {
        /// <summary>
        /// The culture info object that defines the english language settings for parsing and formatting point values
        /// </summary>
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// Lower left x-coordinate
        /// </summary>
        public float llx;
        /// <summary>
        /// Lower left y-coordinate
        /// </summary>
        public float lly;
        /// <summary>
        /// Upper right x-coordinate
        /// </summary>
        public float urx;
        /// <summary>
        /// Upper right y-coordinate
        /// </summary>
        public float ury;

        /// <summary>
        /// Create undefined Extent instance
        /// </summary>
        public Extent()
        {
            llx = float.NaN;
            urx = float.NaN;
            lly = float.NaN;
            ury = float.NaN;
        }

        /// <summary>
        /// Create Extent instance with specified coordinates
        /// </summary>
        public Extent(float llx, float lly, float urx, float ury)
        {
            this.llx = llx;
            this.lly = lly;
            this.urx = urx;
            this.ury = ury;
        }

        /// <summary>
        /// Create Extent instance as bounding box around specified points
        /// </summary>
        /// <param name="points"></param>
        public Extent(List<Point> points)
        {
            llx = float.MaxValue;
            lly = float.MaxValue;
            urx = float.MinValue;
            ury = float.MinValue;
            foreach (Point point in points)
            {
                if (point.X < llx)
                {
                    llx = (float)point.X;
                }
                else if (point.X > urx)
                {
                    urx = (float)point.X;
                }
                if (point.Y < lly)
                {
                    lly = (float)point.Y;
                }
                else if (point.Y > ury)
                {
                    ury = (float)point.Y;
                }
            }
        }

        /// <summary>
        /// Check that extent actually contains some area in both directions
        /// </summary>
        /// <returns></returns>
        public bool IsValidExtent()
        {
            return ((urx - llx) > 0) && ((ury - lly) > 0);
        }

        /// <summary>
        /// Checks for equal extent coordinates, based on float.Equals() method
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Extent other)
        {
            return (other != null) && (llx.Equals(other.llx) && lly.Equals(other.lly) && urx.Equals(other.urx) && ury.Equals(other.ury));
        }

        /// <summary>
        /// Creates string for extent with format [(llx,lly),(urx,ury)]
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[(" + llx.ToString(englishCultureInfo) + "," + lly.ToString(englishCultureInfo) + "),(" + urx.ToString(englishCultureInfo) + "," + ury.ToString(englishCultureInfo) + ")]";
        }

        /// <summary>
        /// Move extent over specified distance
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public Extent Move(float dx, float dy)
        {
            return new Extent(llx + dx, lly + dy, urx + dx, ury + dy);
        }

        /// <summary>
        /// Create a new extent enlarged with given factor (e.g. 0.1 enlarges upto 110%)
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public Extent Enlarge(float factor)
        {
            Extent enlargedExtent = this.Copy();
            float dx = (urx - llx) * factor * 0.5f;
            float dy = (ury - lly) * factor * 0.5f;
            enlargedExtent.llx -= dx;
            enlargedExtent.lly -= dy;
            enlargedExtent.urx += dx;
            enlargedExtent.ury += dy;

            return enlargedExtent;
        }

        /// <summary>
        /// Create a new extent enlarged to specified extent if  larger
        /// </summary>
        /// <param name="enlargeExtent"></param>
        /// <returns></returns>
        public Extent Enlarge(Extent enlargeExtent)
        {
            Extent enlargedExtent = this.Copy();
            if (enlargeExtent.llx < llx)
            {
                enlargedExtent.llx = enlargeExtent.llx;
            }
            if (enlargeExtent.lly < lly)
            {
                enlargedExtent.lly = enlargeExtent.lly;
            }
            if (enlargeExtent.urx > urx)
            {
                enlargedExtent.urx = enlargeExtent.urx;
            }
            if (enlargeExtent.ury > ury)
            {
                enlargedExtent.ury = enlargeExtent.ury;
            }

            // In case of incorrect extents, use lower left corner for location of empty extent
            if (enlargedExtent.lly > enlargedExtent.ury)
            {
                enlargedExtent.lly = enlargedExtent.ury;
            }
            if (enlargedExtent.llx > enlargedExtent.urx)
            {
                enlargedExtent.llx = enlargedExtent.urx;
            }

            return enlargedExtent;
        }

        /// <summary>
        /// Create copy of extent object
        /// </summary>
        /// <returns></returns>
        public Extent Copy()
        {
            return new Extent(llx, lly, urx, ury);
        }

        /// <summary>
        /// Clips this extent to the given clipExtent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public Extent Clip(Extent clipExtent)
        {
            Extent clippedExtent = this.Copy();
            if (clipExtent.llx > llx)
            {
                clippedExtent.llx = clipExtent.llx;
            }
            if (clipExtent.lly > lly)
            {
                clippedExtent.lly = clipExtent.lly;
            }
            if (clipExtent.urx < urx)
            {
                clippedExtent.urx = clipExtent.urx;
            }
            if (clipExtent.ury < ury)
            {
                clippedExtent.ury = clipExtent.ury;
            }

            // In case of incorrect extents, use lower left corner for location of empty extent
            if (clippedExtent.lly > clippedExtent.ury)
            {
                clippedExtent.lly = clippedExtent.ury;
            }
            if (clippedExtent.llx > clippedExtent.urx)
            {
                clippedExtent.llx = clippedExtent.urx;
            }

            return clippedExtent;
        }

        /// <summary>
        /// Apply union operation to this extent and specified other extent
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Extent Union(Extent other)
        {
            Extent unionExtent = new Extent();
            if (other.llx <= llx)
            {
                unionExtent.llx = other.llx;
            }
            else
            {
                unionExtent.llx = this.llx;
            }
            if (other.lly <= lly)
            {
                unionExtent.lly = other.lly;
            }
            else
            {
                unionExtent.lly = this.lly;
            }
            if (other.urx >= urx)
            {
                unionExtent.urx = other.urx;
            }
            else
            {
                unionExtent.urx = this.urx;
            }
            if (other.ury >= ury)
            {
                unionExtent.ury = other.ury;
            }
            else
            {
                unionExtent.ury = this.ury;
            }
            return unionExtent;
        }

        /// <summary>
        /// Check if this extent contains the specified point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Contains(float x, float y)
        {
            return (llx <= x) && (lly <= y) && (urx >= x) && (ury >= y);
        }

        /// <summary>
        /// Check if this extent contains the specified point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Contains(double x, double y)
        {
            return (llx <= x) && (lly <= y) && (urx >= x) && (ury >= y);
        }

        /// <summary>
        /// Check if this extent contains or equals the specified other extent 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Contains(Extent other)
        {
            return (other != null) && (llx <= other.llx) && (lly <= other.lly) && (urx >= other.urx) && (ury >= other.ury);
        }

        /// <summary>
        /// Check if this extent intersects the specified other extent. Note: touching extents do not intersect.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(Extent other)
        {
            return (other != null) && !((urx <= other.llx) || (ury <= other.lly) || (llx >= other.urx) || (lly >= other.ury));
        }

        /// <summary>
        /// Check if this extent intersects the specified other extent. Note: touching extents do not intersect, except for extents with no area (llx==urx and/or lly==ury)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects2(Extent other)
        {
            return (other != null) 
                && (((urx > other.llx) && (llx < other.urx) && (ury > other.lly) && (lly < other.ury)) 
                    || (urx.Equals(other.llx) && llx.Equals(other.urx) && (ury > other.lly) && (lly < other.ury))
                    || (ury.Equals(other.lly) && lly.Equals(other.ury) && (urx > other.llx) && (llx < other.urx)));
        }

        /// <summary>
        /// Snap individual extent coordinates to multiple of specified cellsize
        /// </summary>
        /// <param name="cellsize"></param>
        /// <param name="isEnlarged">If true, snapped extent is enlarged to ensure original extent is contained. If false, extent dimensions will not change</param>
        /// <returns></returns>
        public Extent Snap(float cellsize, bool isEnlarged = false)
        {
            return Snap(cellsize, cellsize, isEnlarged);
        }

        /// <summary>
        /// Snap coordinates to a multiple of specifies cell xy-cellsize
        /// </summary>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <param name="isEnlarged">If true, snapped extent is enlarged to ensure original extent is contained. If false, extent dimensions will not change</param>
        /// <returns></returns>
        public Extent Snap(float xCellsize, float yCellsize, bool isEnlarged = false)
        {
            Extent newExtent;
            if (isEnlarged)
            {
                newExtent = new Extent(
                    ((float)Math.Floor(llx / xCellsize)) * xCellsize,
                    ((float)Math.Floor(lly / yCellsize)) * yCellsize,
                    ((float)Math.Ceiling(urx / xCellsize)) * xCellsize,
                    ((float)Math.Ceiling(ury / yCellsize)) * yCellsize);
            }
            else
            {
                newExtent = new Extent(
                    ((float)Math.Round(llx / xCellsize, 0)) * xCellsize,
                    ((float)Math.Round(lly / yCellsize, 0)) * yCellsize,
                    ((float)Math.Round(urx / xCellsize, 0)) * xCellsize,
                    ((float)Math.Round(ury / yCellsize, 0)) * yCellsize);
            }

            return newExtent;
        }

        /// <summary>
        /// Convert coordinates of extent to a list of DoublePoint objects
        /// </summary>
        /// <returns></returns>
        public List<Point> ToPointList()
        {
            List<Point> points = new List<Point>();
            points.Add(new DoublePoint(llx, lly));
            points.Add(new DoublePoint(llx, ury));
            points.Add(new DoublePoint(urx, ury));
            points.Add(new DoublePoint(urx, lly));
            points.Add(new DoublePoint(llx, lly));
            return points;
        }

        /// <summary>
        /// Parse specified extent string with format (xll,yll,xur,yur) and english notation
        /// </summary>
        /// <param name="extentString"></param>/
        /// <param name="seperator"></param>
        /// <returns></returns>
        public static Extent ParseExtent(string extentString, char seperator = ',')
        {
            Extent extent = null;
            if (extentString != null)
            {
                string[] extentStrings = extentString.Split(seperator);
                if (extentStrings.Length != 4)
                {
                    throw new ToolException("Invalid extent: 4 coordinates expected: " + extentString);
                }
                float[] extentCoordinates = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    string coordinateString = extentStrings[i];
                    if (!float.TryParse(coordinateString, NumberStyles.Float, englishCultureInfo, out float coordinate))
                    {
                        throw new ToolException("Invalid extent coordinate: " + coordinateString);
                    }
                    extentCoordinates[i] = coordinate;
                }
                extent = new Extent(extentCoordinates[0], extentCoordinates[1], extentCoordinates[2], extentCoordinates[3]);
            }
            return extent;
        }

        /// <summary>
        /// Check if this extent is aligned with specified extent and cellsize
        /// </summary>
        /// <param name="otherExtent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="yCellsize"></param>
        /// <returns></returns>
        public bool IsAligned(Extent otherExtent, float xCellsize, float yCellsize)
        {
            if (!((this.llx - otherExtent.llx) % xCellsize).Equals(0f))
            {
                return false;
            }
            if (!((this.lly - otherExtent.lly) % yCellsize).Equals(0f))
            {
                return false;
            }
            if (!((this.urx - otherExtent.urx) % xCellsize).Equals(0f))
            {
                return false;
            }
            if (!((this.ury - otherExtent.ury) % yCellsize).Equals(0f))
            {
                return false;
            }
            return true;
        }
    }
}
