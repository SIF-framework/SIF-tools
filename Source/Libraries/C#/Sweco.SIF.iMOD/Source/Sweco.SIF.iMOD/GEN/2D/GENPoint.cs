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
    /// Class for GEN point features
    /// </summary>
    public class GENPoint : GENFeature
    {
        /// <summary>
        /// Retrieve Point object with coordinates of this GENPoint object
        /// </summary>
        public Point Point
        {
            get
            {
                if (Points.Count == 1)
                {
                    return Points[0];
                }
                else
                {
                    throw new Exception("Invalid GENPoint object, number of points is not equal to one: " + Points.Count);
                }
            }
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates as defined with specified Point object
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        /// <param name="point"></param>
        public GENPoint(GENFile genFile, int id, Point point) : this(genFile, id.ToString(), point)
        {
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public GENPoint(GENFile genFile, int id, double x, double y) : this(genFile, id.ToString(), x, y)
        {
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates as strings.
        /// Coordinate strings should be in english notation (decimalseperator is a point)
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        public GENPoint(GENFile genFile, int id, string xString, string yString) : this(genFile, id.ToString(), xString, yString)
        {
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates as defined with specified Point object
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="point"></param>
        public GENPoint(GENFile genFile, string id, Point point) : base(genFile, id, new List<Point>() { point } )
        {
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public GENPoint(GENFile genFile, string id, double x, double y) : base(genFile, id, new List<Point>() { new DoublePoint(x, y) } )
        {
        }

        /// <summary>
        /// Constructor for GENPoint object with specified ID and x, y-coordinates as strings.
        /// Coordinate strings should be in english notation (decimalseperator is a point)
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="xString"></param>
        /// <param name="yString"></param>
        public GENPoint(GENFile genFile, string id, string xString, string yString) : base(genFile, id, new List<Point>() { new DoublePoint(xString, yString) })
        {
        }

        /// <summary>
        /// Check if the specified point is present in this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override bool HasPoint(Point point)
        {
            return Points[0].IsIdenticalTo(point);
        }

        /// <summary>
        /// Retrieve point at index 0 (specified idx is ignored for GENPoints
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public override Point GetPoint(int idx)
        {
            return Points[0];
        }

        /// <summary>
        /// Check if the specified point is present in this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override bool HasSimilarPoint(Point point)
        {
            return Points[0].IsSimilarTo(point);
        }

        /// <summary>
        /// Retrieves the index of the given point in the list of points of this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns>zero-based index, -1 if not found</returns>
        public override int IndexOf(Point point)
        {
            return HasPoint(point) ? 0 : -1;
        }

        /// <summary>
        /// Retrieves the index of the first similar point in the list of points of this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns>zero-based index, -1 if not found</returns>
        public override int IndexOfSimilarPoint(Point point)
        {
            return HasSimilarPoint(point) ? 0 : -1;
        }

        /// <summary>
        /// Checks if this GENFeature contains the specified point (which for points means checking for equality)
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Point point)
        {
            return this.Equals(point);
        }

        /// <summary>
        /// Reverse internal order of points
        /// </summary>
        public override void ReversePoints()
        {
            if (Points.Count != 1)
            {
                throw new Exception("Number of points for this GENPoint (" + this.ToString() + ") is unequal to one: " + Points.Count);
            }
            else
            {
                // ignore, nothing to reverse for one point
            }
        }

        /// <summary>
        /// Creates a short string representation of this GENFeature object (ID)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ID + "(" + Points[0].ToString() + ")";
        }

        /// <summary>
        /// Note: remove point is not defined for GENPoint obejcts
        /// </summary>
        /// <param name="pointIdx"></param>
        protected override void RemovePointAt(int pointIdx)
        {
            throw new Exception("RemovePointAt() is not defined for GENPoint-objects, point cannot be removed.");
        }

        /// <summary>
        /// Calculate measure of this feature, which for points is not defined
        /// </summary>
        /// <returns>NaN</returns>
        public override double CalculateMeasure()
        {
            return double.NaN;
        }

        /// <summary>
        /// Copy GENPoint object
        /// </summary>
        /// <returns></returns>
        public override GENFeature Copy()
        {
            return new GENPoint(GENFile, ID, Points[0].Copy());
        }

        /// <summary>
        /// Clips this feature against specified extent. Only if point is inside extent it is returned
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public override List<GENFeature> Clip(Extent clipExtent)
        {
            List<GENFeature> clippedGENFeatures = new List<GENFeature>();

            GENFile clippedGENFile = new GENFile();

            // Add DATFile
            DATFile clippedDATFile = CreateDATFile(clippedGENFile, this.GENFile, DATFile.SourceIDColumnName);
            int sourceIDColIdx = clippedDATFile.GetColIdx(DATFile.SourceIDColumnName);

            // Clip feature
            if (clipExtent.Contains(this.Point.X, this.Point.Y))
            {
                GENPoint genPoint = (GENPoint) this.Copy();

                // Add DATRow
                DATRow datRow = genPoint.AddDATRow(this);
                datRow[sourceIDColIdx] = this.ID;

                clippedGENFeatures.Add(genPoint);
            }

            return clippedGENFeatures;
        }

        /// <summary>
        /// Snaps GEN point to features in specified GEN-file
        /// </summary>
        /// <param name="genFile2"></param>
        /// <param name="snapSettings"></param>
        /// <returns>snapped IPF-point, or null if snap was not possible</returns>
        public GENFeature Snap(GENFile genFile2, SnapSettings snapSettings = null)
        {
            GENPoint genPoint = new GENPoint(null, 0, Point);
            GENLine genPointLine = new GENLine(null, "999", new List<Point>() { Point, Point });
            GENFeature snappedGENFeature = genPointLine.Snap(genFile2, snapSettings);
            //GENFeature snappedGENFeature = genPoint.Snap(genFile, snapSettings); // Snaps to points
            if (snappedGENFeature != null)
            {
                return new GENPoint(null, "0", snappedGENFeature.Points[0]);
            }
            else
            {
                // No snapline found, return source point
                return genPoint;
            }
        }

        /// <summary>
        /// Snaps this feature, starting from the given pointIdx to the otherFeature as long as within the specified tolerance distance
        /// </summary>
        /// <param name="matchPointIdx"></param>
        /// <param name="otherFeature">the snapLine to which this feature (the snappedLine) has to be snapped</param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public override GENFeature SnapPart(int matchPointIdx, GENFeature otherFeature, double tolerance)
        {
            if (matchPointIdx != 0)
            {
                throw new Exception("Specified point index (" + matchPointIdx + ") is out of range for specified feature");
            }

            Point nearestPoint = Point.FindNearestPoint(new List<GENFeature>() { otherFeature }, tolerance);
            if (Point.FindNearestPoint(new List<GENFeature>() { otherFeature }, tolerance) != null)
            {
                return otherFeature;
            }
            return null;
        }
    }
}
