// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class for GEN polygon feature objects
    /// </summary>
    public class GENPolygon : GENFeature
    {
        /// <summary>
        /// Creates an empty GENPolygon object
        /// </summary>
        /// <param name="genFile"></param>
        protected GENPolygon(GENFile genFile) : base(genFile)
        {
        }

        /// <summary>
        /// Creates an empty GENPolygon object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        public GENPolygon(GENFile genFile, int id) : this(genFile, id.ToString())
        {
        }

        /// <summary>
        /// Creates an empty GENPolygon object with specified points and ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        /// <param name="points"></param>
        public GENPolygon(GENFile genFile, int id, List<Point> points) : this(genFile, id.ToString(), points)
        {
        }

        /// <summary>
        /// Creates an empty GENPolygon object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        public GENPolygon(GENFile genFile, string id) : base(genFile, id)
        {
        }

        /// <summary>
        /// Creates an empty GENPolygon object with specified ID and points 
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="points"></param>
        public GENPolygon(GENFile genFile, string id, List<Point> points) : base(genFile, id, points)
        {
            if (points.Count <= 2)
            {
                throw new Exception("Points (" + ToString() + ") does not represent a polygon: less than three points");
            }
            if (!points[0].Equals(points[points.Count - 1]))
            {
                throw new Exception("Points (" + ToString() + ") does not represent a polygon: first and last point are unequal (" + points[0].ToString() + " and " + points[points.Count - 1].ToString() + ")");
            }
        }

        /// <summary>
        /// Calculates area of polygon. This method will produce the wrong answer for self-intersecting polygons, 
        /// it will work correctly however for triangles, regular and irregular polygons, convex or concave polygons.
        /// When points are sorted counterclockwise, the result will be correct but will have a negative sign.
        /// http://www.mathopenref.com/coordpolygonarea.html
        /// http://www.mathopenref.com/coordpolygonarea2.html
        /// </summary>
        /// <returns></returns>
        public double CalculateArea()
        {
            double area = 0;
            int j = Points.Count - 2;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                area += (Points[j].X + Points[i].X) * (Points[j].Y - Points[i].Y);
                j = i;
            }
            return area / 2;
        }

        /// <summary>
        /// Calculate area of polygon. See CalculateArea() method.
        /// </summary>
        /// <returns></returns>
        public override double CalculateMeasure()
        {
            return (double)CalculateArea();
        }

        /// <summary>
        /// Check if points of polygon are defined in a clockwise direction.
        /// </summary>
        /// <returns></returns>
        public bool IsClockwise()
        {
            for (int cntr = 2; cntr < Points.Count; cntr++)
            {
                bool? isLeft = IsLeftOf(new LineSegment(Points[0], Points[1]), Points[cntr]);
                if (isLeft != null)		//	some of the points may be colinear.  That's ok as long as the overall is a polygon
                {
                    return !isLeft.Value;
                }
            }

            throw new ArgumentException("All the points in the polygon are colinear");
        }

        /// <summary>
        /// Tells if the test point lies on the left side of the edge line
        /// </summary>
        private static bool? IsLeftOf(LineSegment edge, Point point)
        {
            Vector tmp1 = point - edge.P1; // edge.To - edge.From;
            Vector tmp2 = point - edge.P2;

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
        /// Create copy of GENPolygon object
        /// </summary>
        /// <returns></returns>
        public override GENFeature Copy()
        {
            // Create copy of list with points
            GENPolygon genPolygon = new GENPolygon(null, ID, Points.ToList());
            genPolygon.CopyDATRow(GENFile, ID);
            return genPolygon;
        }
    }
}
