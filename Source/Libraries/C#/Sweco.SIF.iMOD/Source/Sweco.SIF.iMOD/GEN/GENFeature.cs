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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Base class for implementation of GEN feature objects
    /// </summary>
    public abstract class GENFeature
    {
        /// <summary>
        /// Language definition for english culture as used in SIFToolSettings
        /// </summary>
        protected static CultureInfo EnglishCultureInfo { get; set; } = new CultureInfo("en-GB", false);

        /// <summary>
        /// GEN-file that this GEN-feature is in
        /// </summary>
        public GENFile GENFile { get; set; }

        /// <summary>
        /// The main id of this GEN feature in the GEN file
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Points that define this GENFeature
        /// </summary>
        public List<Point> Points { get; protected set; }

        /// <summary>
        /// Creates an empty GENFeature object
        /// </summary>
        protected GENFeature(GENFile genFile) : this(genFile, null)
        {
        }

        /// <summary>
        /// Creates an empty GENFeature object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        public GENFeature(GENFile genFile, int id) : this (genFile, id.ToString())
        {
        }

        /// <summary>
        /// Creates an empty GENFeature object with specified ID and points
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        /// <param name="points"></param>
        public GENFeature(GENFile genFile, int id, List<Point> points) : this(genFile, id.ToString(), points)
        {
        }

        /// <summary>
        /// Creates an empty GENFeature object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        public GENFeature(GENFile genFile, string id)
        {
            this.GENFile = genFile;
            this.ID = id;
            this.Points = new List<Point>();
        }

        /// <summary>
        /// Creates an empty GENFeature object with specified ID and points
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="points"></param>
        public GENFeature(GENFile genFile, string id, List<Point> points)
        {
            this.GENFile = genFile;
            this.ID = id;
            this.Points = points ?? new List<Point>();
        }

        /// <summary>
        /// Check if the specified point is present in this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public virtual bool HasPoint(Point point)
        {
            return IndexOf(point) >= 0;
        }

        /// <summary>
        /// Retrieve point at specified (zero-based) index. If idx is out of range null is returned
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public virtual Point GetPoint(int idx)
        {
            return (idx < Points.Count) ? Points[idx] : null;
        }

        /// <summary>
        /// Check if the specified point is present in this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public virtual bool HasSimilarPoint(Point point)
        {
            return IndexOfSimilarPoint(point) >= 0;
        }

        /// <summary>
        /// Retrieves the index of the given point in the list of points of this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns>zero-based index, -1 if not found</returns>
        public virtual int IndexOf(Point point)
        {
            List<Point> points = Points;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].IsIdenticalTo(point))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Retrieves the index of the first similar point in the list of points of this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns>zero-based index, -1 if not found</returns>
        public virtual int IndexOfSimilarPoint(Point point)
        {
            List<Point> points = Points;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].IsSimilarTo(point))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Reverse internal order of points
        /// </summary>
        public virtual void ReversePoints()
        {
            this.Points.Reverse();
        }

        /// <summary>
        /// Creates a short string representation of this GENFeature object (ID)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ID;
        }

        /// <summary>
        /// Remove point at specified (zero-based) index
        /// </summary>
        /// <param name="pointIdx"></param>
        protected virtual void RemovePointAt(int pointIdx)
        {
            Points.RemoveAt(pointIdx);
            if ((GENFile != null) && GENFile.HasDATFile())
            {
                GENFile.DATFile.RemoveRow(this.ID);
            }

        }

        /// <summary>
        /// Remove duplicate consequtive points from feature (if feature has more than 2 points)
        /// </summary>
        protected virtual void RemoveDuplicatePoints()
        {
            if (Points.Count > 2)
            {
                Point prevPoint = Points[0];
                int pointIdx = 1;
                while (pointIdx < Points.Count)
                {
                    Point currentPoint = Points[pointIdx];
                    if (currentPoint.Equals(prevPoint))
                    {
                        RemovePointAt(pointIdx);
                    }
                    else
                    {
                        prevPoint = currentPoint;
                        pointIdx++;
                    }
                }
            }
        }

        /// <summary>
        /// Set value of specified column. If column does not yet exist, add the column (with en empty string for other existing features in the corresponding GEN-file).
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value">float value</param>
        public void SetColumnValue(string columnName, float value)
        {
            SetColumnValue(columnName, value.ToString(EnglishCultureInfo));
        }

        /// <summary>
        /// Set value for specified column. If column does not exist, it is added (with en empty string for other existing features in the corresponding GEN-file)
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        public void SetColumnValue(string columnName, string value)
        {
            if ((columnName != null) && !columnName.Equals(string.Empty))
            {
                if (GENFile == null)
                {
                    GENFile = new GENFile();
                }
                if (GENFile.DATFile == null)
                {
                    GENFile.AddDATFile();
                }

                DATFile datFile = GENFile.DATFile;
                int colIdx = datFile.GetColIdx(columnName);
                if (colIdx < 0)
                {
                    colIdx = datFile.AddColumn(columnName);
                }

                DATRow row = datFile.GetRow(this.ID);
                if (row == null)
                {
                    row = new DATRow();
                    for (int i = 0; i < datFile.ColumnNames.Count; i++)
                    {
                        row.Add(string.Empty);
                    }
                    row[0] = ID;
                    datFile.AddRow(row);
                }
                row[colIdx] = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Set value(s) of one or more columns for this GEN-feature. If column does not yet exist, add the column (with en empty string for other existing features in the corresponding GEN-file).
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="values">float values</param>
        public void SetColumnValues(List<string> columnNames, List<float> values)
        {
            if (columnNames.Count != values.Count)
            {
                throw new Exception("ColumnNames-count (" + columnNames.Count + ") doesn't match values-count (" + values.Count + ")");
            }

            for (int idx = 0; idx < columnNames.Count; idx++)
            {
                SetColumnValue(columnNames[idx], values[idx]);
            }
        }

        /// <summary>
        /// Set value(s) of one or more columns for this GEN-feature. If column does not yet exist, add the column (with en empty string for other existing features in the corresponding GEN-file).
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="values">string values</param>
        public void SetColumnValues(List<string> columnNames, List<string> values)
        {
            if (columnNames.Count != values.Count)
            {
                throw new Exception("ColumnNames-count (" + columnNames.Count + ") doesn't match values-count (" + values.Count + ")");
            }

            for (int idx = 0; idx < columnNames.Count; idx++)
            {
                SetColumnValue(columnNames[idx], values[idx]);
            }
        }

        /// <summary>
        /// Checks if other feature is equal to this feature
        /// </summary>
        /// <param name="otherGENFeature"></param>
        /// <returns></returns>
        public virtual bool Equals(GENFeature otherGENFeature)
        {
            if (otherGENFeature == null)
            {
                return false;
            }

            if (!otherGENFeature.GetType().Equals(this.GetType()))
            {
                return false;
            }

            if (otherGENFeature.Points.Count == Points.Count)
            {
                for (int pointIdx = 0; pointIdx < Points.Count; pointIdx++)
                {
                    if (!Points[pointIdx].Equals(otherGENFeature.Points[pointIdx]))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve extent defined by bounding box around all points of this GENFeature
        /// </summary>
        /// <returns></returns>
        public Extent RetrieveExtent()
        {
            Extent extent = null;
            if (Points.Count > 0)
            {
                Point point = Points[0];
                extent = new Extent((float)point.X, (float)point.Y, (float)point.X, (float)point.Y);
                for (int pointIdx = 0; pointIdx < Points.Count; pointIdx++)
                {
                    point = Points[pointIdx];
                    if (point.X < extent.llx)
                    {
                        extent.llx = (float)point.X;
                    }
                    if (point.Y < extent.lly)
                    {
                        extent.lly = (float)point.Y;
                    }
                    if (point.X > extent.urx)
                    {
                        extent.urx = (float)point.X;
                    }
                    if (point.Y > extent.ury)
                    {
                        extent.ury = (float)point.Y;
                    }
                }
            }
            return extent;
        }

        /// <summary>
        /// Copies DATRow of specified feature to DATRow of this GEN-feature. New columns of other GENFeature are added. Values are copied to corresponding columns (using string.Empty as a default).
        /// If specified feature does not have a DATRow, no DATRow is added and null is returned.
        /// </summary>
        /// <param name="copiedFeature"></param>
        /// <returns>Copied DATRow is added but also returned</returns>
        protected DATRow CopyDATRow(GENFeature copiedFeature)
        {
            DATRow row = null;
            if ((copiedFeature != null) && (copiedFeature.GENFile != null) && (copiedFeature.GENFile.HasDATFile() || copiedFeature.GENFile.DATFile != null))
            {
                DATFile copiedDATFile = copiedFeature.GENFile.DATFile;
                row = copiedDATFile.GetRow(copiedFeature.ID);
                if (row != null)
                {
                    if (this.GENFile == null)
                    {
                        this.GENFile = new GENFile();
                    }
                    if (GENFile.DATFile == null)
                    {
                        this.GENFile.DATFile = new DATFile(this.GENFile);
                    }
                    DATFile datFile = this.GENFile.DATFile;
                    datFile.AddColumns(copiedDATFile.ColumnNames);

                    // prepare list of values, add empty string for all old and new columns 
                    List<string> valueList = new List<string>();
                    for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                    {
                        valueList.Add(string.Empty);
                    }
                    // Set id to id of new feature
                    valueList[0] = this.ID;

                    // Find new columnindices for values of added feature
                    for (int colIdx = 1; colIdx < row.Count; colIdx++)
                    {
                        string colName = copiedDATFile.ColumnNames[colIdx];
                        int datFileColIdx = datFile.GetColIdx(colName);
                        valueList[datFileColIdx] = row[colIdx];
                    }
                    row = new DATRow(valueList);

                    datFile.AddRow(row);
                }
            }

            return row;
        }

        /// <summary>
        /// Copies DATRow of feature with specified ID in specified GEN-file to DATRow of this GEN-feature
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        /// <param name="addedValue">optional added value to DATRow for missing columns, does not ensure columnname exists for this values in the specified GEN-file</param>
        protected void CopyDATRow(GENFile genFile, string id, string addedValue)
        {
            CopyDATRow(genFile, id, new List<string>() { addedValue });
        }

        /// <summary>
        /// Adds a copy of the corresponding rowvalues from the DATFile of the specified GENFile. 
        /// Does nothing if the GENFile doesn't have a DATFile or if the row with the specified id is not found
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        /// <param name="addedValues">optional added values to DATRow for missing columns, does not ensure columnnames exist for these values in the specified GEN-file</param>
        protected void CopyDATRow(GENFile genFile, string id, List<string> addedValues = null)
        {
            if ((genFile != null) && genFile.HasDATFile())
            {
                if (this.GENFile == null)
                {
                    // Add empty genFile
                    this.GENFile = new GENFile();
                }

                if (this.GENFile.DATFile == null)
                {
                    // Add new DATFile
                    this.GENFile.DATFile = new DATFile(this.GENFile);
                    this.GENFile.DATFile.ColumnNames = genFile.DATFile.ColumnNames.ToList();
                }

                if (this.GENFile.DATFile.ContainsID(id))
                {
                    throw new Exception("Row with specified id ('" + id + "') already present in DATFile");
                }

                // Find row with given id
                DATRow row = genFile.DATFile.GetRow(id);
                if (row != null)
                {
                    if (((addedValues == null) && (row.Count != this.GENFile.DATFile.ColumnNames.Count)) || ((addedValues != null) && ((row.Count + 1) != this.GENFile.DATFile.ColumnNames.Count)))
                    {
                        throw new Exception("Different number of columns in this features DATFile (" + genFile.DATFile.ColumnNames.Count + ") and number of values in specified row (" + row.Count + ")");
                    }
                    if (addedValues != null)
                    {
                        DATRow extendedRow = row.Copy();
                        extendedRow[0] = this.ID;
                        extendedRow.AddRange(addedValues);
                        this.GENFile.DATFile.AddRow(extendedRow);
                    }
                    else
                    {
                        DATRow copiedRow = row.Copy();
                        copiedRow[0] = this.ID;
                        this.GENFile.DATFile.AddRow(row.Copy());
                    }
                }
            }
        }

        /// <summary>
        /// Create new DATFile for specified GEN-file. Optionally copy columnnames from specified source GEN-file and add unique SourceID column
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="sourceGENFile"></param>
        /// <param name="sourceIDColName">a column to add, or null to skip</param>
        /// <returns></returns>
        protected DATFile CreateDATFile(GENFile genFile, GENFile sourceGENFile = null, string sourceIDColName = null)
        {
            genFile.DATFile = new DATFile(genFile);

            if (this.GENFile.HasDATFile() || (this.GENFile.DATFile != null))
            {
                genFile.DATFile.AddColumns(this.GENFile.DATFile.ColumnNames);

                if (sourceIDColName != null)
                {
                    string sourceIDColumnName = this.GENFile.DATFile.GetUniqueColumnName(sourceIDColName);
                    genFile.DATFile.AddColumn(sourceIDColumnName);
                }
            }
            else
            {
                genFile.DATFile.AddIDColumn();

                if (sourceIDColName != null)
                {
                    genFile.DATFile.AddColumn(sourceIDColName);
                }
            }

            return genFile.DATFile;
        }

        /// <summary>
        /// Add new DATRow for this GENFeature. Optionally specify source GEN-file to copy DATRow with same ID from
        /// </summary>
        /// <param name="sourceGENFeature"></param>
        protected DATRow AddDATRow(GENFeature sourceGENFeature = null)
        {
            DATRow datRow = null;
            if (sourceGENFeature != null)
            {
                datRow = CopyDATRow(sourceGENFeature);
            }
            if (datRow == null)
            {
                DATFile datFile = GENFile.DATFile;
                string[] rowValues = new string[datFile.ColumnNames.Count];
                rowValues[0] = this.ID;
                datRow = new DATRow(rowValues);
                datFile.AddRow(datRow);
            }

            return datRow;
        }

        /// <summary>
        /// Calculates a measure for this feature, e.q. a length for a line, an area for a polygon
        /// </summary>
        /// <returns></returns>
        public abstract double CalculateMeasure();

        /// <summary>
        /// Returns a copy of this GENFeature instance
        /// </summary>
        /// <param name="isDATRowCopied">if false, corresponding DATRow is not copied</param>
        /// <returns></returns>
        public abstract GENFeature Copy(bool isDATRowCopied = true);

        /// <summary>
        /// Clip feature to specified extent, which may result in one or more smaller features
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public abstract List<GENFeature> Clip(Extent clipExtent);

        /// <summary>
        /// Snaps this feature, starting from the given pointIdx to the otherFeature as long as within the specified tolerance distance
        /// </summary>
        /// <param name="matchPointIdx"></param>
        /// <param name="otherFeature">the snapLine to which this feature (the snappedLine) has to be snapped</param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public abstract GENFeature SnapPart(int matchPointIdx, GENFeature otherFeature, double tolerance);

        /// <summary>
        /// Retrieve point from this feature (within tolerance) that is closest to specified point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns>nearest point from feature if within tolerance distance, otherwise null</returns>
        public Point FindNearestPoint(Point point, double tolerance)
        {
            double minDistance = tolerance;
            Point minPoint = null;
            for (int point2Idx = 0; point2Idx < Points.Count; point2Idx++)
            {
                Point point2 = Points[point2Idx];
                double distance = point.GetDistance(point2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minPoint = point2;
                }
            }
            return minPoint;
        }

        /// <summary>
        /// Removes redundant point: equal points following each other in the point list
        /// </summary>
        /// <param name="otherFeatures">other features that may be connected to this feature</param>
        /// <param name="tolerance">used for determining equality. Set this parameter to the minimum distance between points to be considered different (default: Point.IdentityTolerance)</param>
        /// <returns>number of redundant and removed points</returns>
        public List<Point> RemoveRedundantPoints(List<GENFeature> otherFeatures = null, double tolerance = double.NaN)
        {
            if (tolerance.Equals(double.NaN))
            {
                tolerance = Point.IdentityTolerance;
            }

            List<Point> redundantPoints = new List<Point>();

            Point prevPoint = Points[0];
            int pointIdx = 1;
            while (pointIdx < Points.Count)
            {
                Point point = Points[pointIdx];
                if (point.Equals(prevPoint))
                {
                    // Check which point to remove
                    if (otherFeatures != null)
                    {
                        LineSegment segment1 = null;
                        Point snappedPoint = null;
                        double snapDistance1 = float.MaxValue;
                        GENLine nearestLine1 = (GENLine)point.FindNearestSegmentFeature(otherFeatures, tolerance, new List<GENFeature>() { this });
                        if (nearestLine1 != null)
                        {
                            segment1 = nearestLine1.FindNearestSegment(point); //  float.MaxValue);
                        }
                        if (segment1 != null)
                        {
                            snappedPoint = point.SnapToLineSegment(segment1.P1, segment1.P2);
                        }
                        if (snappedPoint != null)
                        {
                            snapDistance1 = point.GetDistance(snappedPoint);
                        }

                        LineSegment segment2 = null;
                        Point snappedPrevPoint = null;
                        double snapDistance2 = float.MaxValue;
                        GENLine nearestLine2 = (GENLine)prevPoint.FindNearestSegmentFeature(otherFeatures, tolerance, new List<GENFeature>() { this });
                        if (nearestLine2 != null)
                        {
                            segment2 = nearestLine2.FindNearestSegment(point);
                        }
                        if (segment2 != null)
                        {
                            snappedPrevPoint = prevPoint.SnapToLineSegment(segment2.P1, segment2.P2);
                        }
                        if (snappedPrevPoint != null)
                        {
                            snapDistance2 = prevPoint.GetDistance(snappedPrevPoint);
                        }

                        if (snapDistance1 < snapDistance2)
                        {
                            // point 1 is closer to a line segment than the other point
                            redundantPoints.Add(Points[pointIdx - 1]);
                            RemovePointAt(pointIdx - 1);
                            prevPoint = point;
                        }
                        else
                        {
                            redundantPoints.Add(Points[pointIdx]);
                            RemovePointAt(pointIdx);
                        }
                    }
                    else
                    {
                        redundantPoints.Add(Points[pointIdx]);
                        RemovePointAt(pointIdx);
                    }
                }
                else
                {
                    pointIdx++;
                    prevPoint = point;
                }
            }

            return redundantPoints;
        }

        /// <summary>
        /// Retrieves segment of this feature that is closest to specified point, where distance is defined as perpendicular distance from point to segment
        /// P1 of the returned segment will be closest to P1 of the other segment 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns>the closed segment, or null if no segment is found within specified tolerance</returns>
        public LineSegment FindNearestSegment(Point point, double tolerance = double.NaN)
        {
            LineSegment lineSegment = null;

            double minSegmentDistance = (tolerance.Equals(double.NaN)) ? double.MaxValue : tolerance;

            // Compare with all LineSegments of the given feature
            Point startPoint = Points[0];
            for (int pointIdx = 1; pointIdx < Points.Count; pointIdx++)
            {
                Point endPoint = Points[pointIdx];
                Point snappedPoint = point.SnapToLineSegment(startPoint, endPoint);
                double segmentDistance = (double)snappedPoint.GetDistance(point);

                if (segmentDistance < minSegmentDistance)
                {
                    minSegmentDistance = segmentDistance;
                    lineSegment = new LineSegment(startPoint, endPoint);
                }

                startPoint = endPoint;
            }

            return lineSegment;
        }

        /// <summary>
        /// Retrieves segment of this line that is closest to specified segment, where distance is defined as
        /// sum of snapdistances from lineSegment.P1 and lineSegment.P2 to this line. 
        /// P1 of the returned segment will be closest to P1 of the other segment 
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="lineSegment"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public static LineSegment FindNearestSegment(List<Point> pointList, LineSegment lineSegment, float maxDistance = float.NaN)
        {
            LineSegment nearestLineSegment = null;

            // Compare with all LineSegments of the given feature
            double minSegmentDistance = (maxDistance.Equals(float.NaN)) ? float.MaxValue : maxDistance;

            Point startPoint = pointList[0];
            Point snappedStartPoint = startPoint.SnapToLineSegment(lineSegment.P1, lineSegment.P2);
            double startPointDistance = snappedStartPoint.GetDistance(startPoint);
            for (int pointIdx = 1; pointIdx < pointList.Count; pointIdx++)
            {
                Point endPoint = pointList[pointIdx];

                Point snappedEndPoint = endPoint.SnapToLineSegment(lineSegment.P1, lineSegment.P2);
                double endPointDistance = snappedEndPoint.GetDistance(endPoint);
                double segmentDistance = startPointDistance + endPointDistance;

                LineSegment overlap = lineSegment.Overlap(startPoint, endPoint);
                if (overlap != null)
                {
                    // Give overlapping segments priority
                    double overlapLength = overlap.Length;
                    if ((overlapLength > 0) && (overlapLength >= Point.Tolerance))
                    {
                        segmentDistance = -overlapLength;
                    }
                }

                if (!startPoint.Equals(endPoint) && (segmentDistance < minSegmentDistance))
                {
                    minSegmentDistance = segmentDistance;
                    nearestLineSegment = new LineSegment(startPoint, endPoint);
                }

                startPoint = endPoint;
                snappedStartPoint = snappedEndPoint;
                startPointDistance = endPointDistance;
            }

            if (nearestLineSegment != null)
            {
                // Check that found nearest line segment is oriented in same direction as this segment
                double distance1 = lineSegment.P1.GetDistance(nearestLineSegment.P1) + lineSegment.P2.GetDistance(nearestLineSegment.P2);
                double distance2 = lineSegment.P1.GetDistance(nearestLineSegment.P2) + lineSegment.P2.GetDistance(nearestLineSegment.P1);
                if (distance1 < distance2)
                {
                    return nearestLineSegment;
                }
                else
                {
                    // Reverse direction of line segment
                    return new LineSegment(nearestLineSegment.P2, nearestLineSegment.P1);
                }
            }
            else
            {
                return null;
            }
        }
    }
}
