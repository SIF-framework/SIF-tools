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
using Sweco.SIF.GIS.Clipping;

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

        /// <summary>
        /// Clip GEN-polygon to specified extent, which may result in or more smaller polygons. When GEN-file has a DAT-file, a SourceID column is added with the source ID.
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public override List<GENFeature> Clip(Extent clipExtent)
        {
            List<GENFeature> clippedGENFeatures = new List<GENFeature>();
            clippedGENFeatures.AddRange(ClipPolygon(clipExtent));
            return clippedGENFeatures;
        }

        /// <summary>
        /// Clips this polygon and its DATRow (if existing), to specified clipPolygon. Note: ensure order of points is defined clockwise as this defines inside/outside.
        /// </summary>
        /// <param name="clipPolygon"></param>
        /// <returns></returns>
        public List<GENPolygon> ClipPolygon(GENPolygon clipPolygon)
        {
            List<GENPolygon> clippedGENPolygons = new List<GENPolygon>();
            GENFile clippedGENFile = new GENFile();

            // Add DATFile
            DATFile clippedDATFile = CreateDATFile(clippedGENFile, this.GENFile, DATFile.SourceIDColumnName);
            int sourceIDColIdx = clippedDATFile.GetColIdx(DATFile.SourceIDColumnName);

            // Clip feature: CSHFClipper requires not to close polygons; make copy of point list to prevent removal of points in source features
            List<Point> pointList1 = this.Points.ToList();
            pointList1.RemoveAt(pointList1.Count - 1);
            List<Point> pointList2 = clipPolygon.Points.ToList();
            pointList2.RemoveAt(pointList2.Count - 1);
            List<Point> clippedPointList = CSHFClipper.ClipPolygon(pointList1, pointList2);

            if ((clippedPointList != null) && (clippedPointList.Count() > 0))
            {
                clippedPointList.Add(clippedPointList[0]);
                GENPolygon clippedGENPolygon = new GENPolygon(clippedGENFile, ID, clippedPointList);

                // Add DATRow
                DATRow datRow = clippedGENPolygon.AddDATRow(this);
                datRow[sourceIDColIdx] = this.ID;

                clippedGENPolygons.Add(clippedGENPolygon);
            }

            return clippedGENPolygons;
        }

        /// <summary>
        /// Clips this polygon without DATRow to specified clip extent. Note: ensure order of points is defined clockwise as this defines inside/outside.
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isClockwise">specify boolean true (or false) if points are known to be clockwise (or not), use null if uncertain and points should be checked</param>
        /// <returns></returns>
        public List<GENPolygon> ClipPolygonWithoutDATRow(Extent clipExtent, bool? isClockwise = null)
        {
            List<GENPolygon> clippedGENPolygons = new List<GENPolygon>();
            GENFile clippedGENFile = new GENFile();

            // Check if feature is completely inside or outside specified extent
            Extent featureExtent = this.RetrieveExtent();
            if (clipExtent.Contains(featureExtent))
            {
                // Feature is completely inside clip extent, copy feature and data completely
                GENPolygon clippedPolygon = new GENPolygon(clippedGENFile, "1", this.Points);
                clippedGENFile.AddFeature(clippedPolygon);
                clippedGENPolygons.Add(clippedPolygon);
            }
            else if (!clipExtent.Intersects(featureExtent))
            {
                // Feature is completely outside specified extent, return empty list
            }
            else
            {
                bool isSourcePolygonClockWise = (isClockwise != null) ? ((bool)isClockwise) : GISUtils.IsClockwise(this.Points);

                // Clip feature
                List<Point> extentPoints = clipExtent.ToPointList();
                extentPoints.RemoveAt(extentPoints.Count - 1);
                List<Point> clippedPointList = CSHFClipper.ClipPolygon(this.Points, extentPoints, isClockwise); // pointList1
                if ((clippedPointList != null) && (clippedPointList.Count > 0))
                {
                    clippedPointList.Add(clippedPointList[0]);
                    GENPolygon clippedGENPolygon = new GENPolygon(clippedGENFile, ID, clippedPointList);
                    clippedGENPolygon.RemoveDuplicatePoints();
                    if (isSourcePolygonClockWise != GISUtils.IsClockwise(clippedGENPolygon.Points))
                    {
                        // Ensure order of clipped points is same as source polygon
                        clippedGENPolygon.ReversePoints();
                    }

                    if (clippedGENPolygon.CalculateArea() > 0)
                    {
                        clippedGENPolygons.Add(clippedGENPolygon);
                    }
                }
            }

            return clippedGENPolygons;
        }

        /// <summary>
        /// Check if specified point is inside this GEN-polygon
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Point point)
        {
            return point.IsInside(this.Points);
        }

        /// <summary>
        /// Clips this polygon and its DATRow (if existing), to specified clip extent. Note: ensure order of points is defined clockwise as this defines inside/outside.
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public List<GENPolygon> ClipPolygon(Extent clipExtent)
        {
            List<GENPolygon> clippedGENPolygons = new List<GENPolygon>();
            GENFile clippedGENFile = new GENFile();

            // Add DATFile
            DATFile clippedDATFile = clippedDATFile = CreateDATFile(clippedGENFile, this.GENFile, DATFile.SourceIDColumnName);
            int sourceIDColIdx = clippedDATFile.GetColIdx(DATFile.SourceIDColumnName);

            // Check if feature is completely inside or outside specified extent
            Extent featureExtent = this.RetrieveExtent();
            if (clipExtent.Contains(featureExtent))
            {
                // Feature is completely inside clip extent, copy feature and data completely
                GENPolygon clippedPolygon = new GENPolygon(clippedGENFile, "1", this.Points);
                DATRow datRow = clippedPolygon.AddDATRow(this);
                datRow[sourceIDColIdx] = this.ID;
                clippedGENFile.AddFeature(clippedPolygon);
                clippedGENPolygons.Add(clippedPolygon);
            }
            else if (!clipExtent.Intersects(featureExtent))
            {
                // Feature is completely outside specified extent, return empty list
            }
            else
            {
                // Clip feature
                List<Point> extentPoints = clipExtent.ToPointList();
                extentPoints.RemoveAt(extentPoints.Count - 1);
                List<Point> clippedPointList = CSHFClipper.ClipPolygon(this.Points, extentPoints); // pointList1
                if ((clippedPointList != null) && (clippedPointList.Count > 0))
                {
                    clippedPointList.Add(clippedPointList[0]);
                    GENPolygon clippedGENPolygon = new GENPolygon(clippedGENFile, ID, clippedPointList);
                    clippedGENPolygon.RemoveDuplicatePoints();
                    if (GISUtils.IsClockwise(this.Points) != GISUtils.IsClockwise(clippedGENPolygon.Points))
                    {
                        // Ensure order of clipped points is same as source polygon
                        clippedGENPolygon.ReversePoints();
                    }

                    // Skip empty polygons, but do add polygons with negative area which should be islands
                    if (!clippedGENPolygon.CalculateArea().Equals(0))
                    {
                        // Add DATRow
                        DATRow datRow = clippedGENPolygon.AddDATRow(this);
                        datRow[sourceIDColIdx] = this.ID;

                        clippedGENPolygons.Add(clippedGENPolygon);
                    }
                }
            }

            return clippedGENPolygons;
        }

        /// <summary>
        /// Snaps this feature, starting from the given pointIdx to the otherFeature as long as within the specified tolerance distance
        /// Note: This is currently not implemented for polygons
        /// </summary>
        /// <param name="matchPointIdx"></param>
        /// <param name="otherFeature">the snapLine to which this feature (the snappedLine) has to be snapped</param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public override GENFeature SnapPart(int matchPointIdx, GENFeature otherFeature, double tolerance)
        {
            return null;
        }
    }
}
