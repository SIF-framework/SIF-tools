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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;
using Sweco.SIF.GIS.Clipping;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class for GEN line feature objects
    /// </summary>
    public class GENLine : GENFeature
    {
        /// <summary>
        /// Creates an empty GENLine object
        /// </summary>
        /// <param name="genFile"></param>
        protected GENLine(GENFile genFile) : base(genFile)
        {
            Points = new List<Point>();
        }

        /// <summary>
        /// Creates an empty GENLine object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        public GENLine(GENFile genFile, int id) : this(genFile, id.ToString())
        {
        }

        /// <summary>
        /// Creates a GENLine object with specified ID and points
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">value ID</param>
        /// <param name="points"></param>
        public GENLine(GENFile genFile, int id, List<Point> points) : this(genFile, id.ToString(), points)
        {
        }

        /// <summary>
        /// Creates an empty GENLine object with specified ID
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        public GENLine(GENFile genFile, string id) : base(genFile, id)
        {
            Points = new List<Point>();
        }

        /// <summary>
        /// Creates a GENLine object with specified ID and points
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id">string ID</param>
        /// <param name="points"></param>
        public GENLine(GENFile genFile, string id, List<Point> points) : base(genFile, id, points)
        {
            if (points.Count <= 1)
            {
                throw new Exception("Points (" + ToString() + ") does not represent a line: less than two points");
            }
        }

        /// <summary>
        /// Adds a point to the current line
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(Point point)
        {
            Points.Add(point);
        }

        /// <summary>
        /// Calculate total length of GEN-line
        /// </summary>
        /// <returns></returns>
        public virtual double CalculateLength()
        {
            double length = 0;
            if (Points.Count > 0)
            {
                Point prevPoint = Points[0];
                for (int i = 1; i < Points.Count; i++)
                {
                    Point point = Points[i];
                    double distance = (double)Math.Sqrt((point.X - prevPoint.X) * (point.X - prevPoint.X) + (point.Y - prevPoint.Y) * (point.Y - prevPoint.Y));
                    length += distance;
                    prevPoint = point;
                }
            }
            return length;
        }

        /// <summary>
        /// Calculate total length of GEN-line
        /// </summary>
        /// <returns></returns>
        public override double CalculateMeasure()
        {
            return CalculateLength();
        }

        /// <summary>
        /// Copy this GENLine object
        /// </summary>
        /// <returns></returns>
        public override GENFeature Copy()
        {
            // Create copy of list with points
            GENLine genLine = new GENLine(null, ID, Points.ToList());
            genLine.CopyDATRow(GENFile, this.ID);
            return genLine;
        }

        /// <summary>
        /// Clip this GENLine object to specified extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public override List<GENFeature> Clip(Extent clipExtent)
        {
            List<GENFeature> clippedGENFeatures = new List<GENFeature>();
            clippedGENFeatures.AddRange(ClipLine(clipExtent));
            return clippedGENFeatures;
        }

        /// <summary>
        /// Clip GEN-line to specified extent. When GEN-file has a DAT-file, a SourceID column is added with the source ID.
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public List<GENLine> ClipLine(Extent clipExtent)
        {
            List<GENLine> clippedGENLines = new List<GENLine>();
            GENFile clippedGENFile = new GENFile();

            // Add DATFile
            DATFile clippedDATFile = CreateDATFile(clippedGENFile, this.GENFile, DATFile.SourceIDColumnName);
            int sourceIDColIdx = clippedDATFile.GetColIdx(DATFile.SourceIDColumnName);

            // Check if feature is completely inside or outside specified extent
            Extent featureExtent = this.RetrieveExtent();
            if (clipExtent.Contains(featureExtent))
            {
                // Feature is completely inside clip extent, copy feature and data completely
                GENLine clippedLine = new GENLine(clippedGENFile, "1", this.Points);
                DATRow datRow = clippedLine.AddDATRow(this);
                datRow[sourceIDColIdx] = this.ID;
                clippedGENFile.AddFeature(clippedLine);
                clippedGENLines.Add(clippedLine);
            }
            else if (!clipExtent.Intersects(featureExtent))
            {
                // Feature is completely outside specified extent, return empty list
            }
            else
            {
                // Clip feature
                List<Point> clippedPointList = new List<Point>();
                int subIdx = 1;
                for (int featurePointIdx = 1; featurePointIdx < Points.Count; featurePointIdx++)
                {
                    LineSegment segment = new LineSegment(Points[featurePointIdx - 1], Points[featurePointIdx]);
                    LineSegment clippedSegment = null;
                    if (clipExtent != null)
                    {
                        clippedSegment = CSHFClipper.ClipLine(segment, clipExtent);
                    }
                    else
                    {
                        clippedSegment = new LineSegment(segment.P1.Copy(), segment.P2.Copy());
                    }
                    if (clippedSegment != null)
                    {
                        // Check that first point of clipped segment is equal to last point of previous segment
                        if ((clippedPointList.Count > 0) && !clippedPointList[clippedPointList.Count - 1].Equals(clippedSegment.P1))
                        {
                            // Store previous part of line
                            string clipFeatureId = ID.Equals(string.Empty) ? subIdx.ToString() : ID + "-" + subIdx;
                            GENLine clippedLine = new GENLine(clippedGENFile, (clippedGENLines.Count + 1).ToString(), clippedPointList);
                            DATRow datRow = clippedLine.AddDATRow(this);
                            datRow[sourceIDColIdx] = this.ID;
                            clippedGENLines.Add(clippedLine);

                            clippedPointList = new List<Point>();
                            subIdx++;
                        }

                        if (clippedPointList.Count == 0)
                        {
                            clippedPointList.Add(clippedSegment.P1);
                        }
                        clippedPointList.Add(clippedSegment.P2);
                    }
                }

                // Add last clipped part
                if ((clippedPointList != null) && (clippedPointList.Count > 0))
                {
                    GENLine clippedLine = new GENLine(clippedGENFile, (clippedGENLines.Count + 1).ToString(), clippedPointList);
                    DATRow datRow = clippedLine.AddDATRow(this);
                    datRow[sourceIDColIdx] = this.ID;
                    clippedGENLines.Add(clippedLine);
                }
            }

            return clippedGENLines;
        }

        public GENFeature Snap(GENFile genFile2, SnapSettings snapSettings = null)
        {
            if (snapSettings == null)
            {
                snapSettings = SnapSettings.DefaultSnapSettings;
            }
            double snapTolerance = snapSettings.SnapTolerance;

            int snapToDistanceColIdx = -1;
            int snapToFeatureIdColIdx = -1;
            int snappedToFileColIdx = -1;
            int snapFromDistanceColIdx = -1;
            int snapFromFeatureIdColIdx = -1;
            int snappedFromFileColIdx = -1;
            if (snapSettings.IsSnappedIPFPointAdded)
            {
                if ((snapSettings.SnappedToPointsIPFFile == null) || (snapSettings.SnappedToPointsIPFFile == null))
                {
                    throw new Exception("A To- or From-IPFFile object should be specified when snapped points have to be added");
                }

                if (snapSettings.SnappedToPointsIPFFile != null)
                {
                    if (!snapSettings.SnappedToPointsIPFFile.ColumnNames.Contains(snapSettings.SnapDistanceColName))
                    {
                        snapSettings.SnappedToPointsIPFFile.AddColumn(snapSettings.SnapDistanceColName);
                    }

                    if (!snapSettings.SnappedToPointsIPFFile.ColumnNames.Contains(snapSettings.FeatureIdColName))
                    {
                        snapSettings.SnappedToPointsIPFFile.AddColumn(snapSettings.FeatureIdColName);
                    }

                    if (!snapSettings.SnappedToPointsIPFFile.ColumnNames.Contains(snapSettings.SnappedFileColName))
                    {
                        snapSettings.SnappedToPointsIPFFile.AddColumn(snapSettings.SnappedFileColName);
                    }

                    snapToDistanceColIdx = snapSettings.SnappedToPointsIPFFile.ColumnNames.IndexOf(snapSettings.SnapDistanceColName);
                    snapToFeatureIdColIdx = snapSettings.SnappedToPointsIPFFile.ColumnNames.IndexOf(snapSettings.FeatureIdColName);
                    snappedToFileColIdx = snapSettings.SnappedToPointsIPFFile.ColumnNames.IndexOf(snapSettings.SnappedFileColName);
                }

                if (snapSettings.SnappedFromPointsIPFFile != null)
                {
                    if (!snapSettings.SnappedFromPointsIPFFile.ColumnNames.Contains(snapSettings.SnapDistanceColName))
                    {
                        snapSettings.SnappedFromPointsIPFFile.AddColumn(snapSettings.SnapDistanceColName);
                    }

                    if (!snapSettings.SnappedFromPointsIPFFile.ColumnNames.Contains(snapSettings.FeatureIdColName))
                    {
                        snapSettings.SnappedFromPointsIPFFile.AddColumn(snapSettings.FeatureIdColName);
                    }

                    if (!snapSettings.SnappedFromPointsIPFFile.ColumnNames.Contains(snapSettings.SnappedFileColName))
                    {
                        snapSettings.SnappedFromPointsIPFFile.AddColumn(snapSettings.SnappedFileColName);
                    }

                    snapFromDistanceColIdx = snapSettings.SnappedFromPointsIPFFile.ColumnNames.IndexOf(snapSettings.SnapDistanceColName);
                    snapFromFeatureIdColIdx = snapSettings.SnappedFromPointsIPFFile.ColumnNames.IndexOf(snapSettings.FeatureIdColName);
                    snappedFromFileColIdx = snapSettings.SnappedFromPointsIPFFile.ColumnNames.IndexOf(snapSettings.SnappedFileColName);
                }
            }

            // Create an empty line feature 
            GENLine snappedLine = new GENLine(null, this.ID);
            snappedLine.CopyDATRow(GENFile, this.ID);
            if (snapSettings.IsSnapDistanceAdded && (snapSettings.MinSnapDistanceColName != null))
            {
                snappedLine.GENFile.DATFile.AddColumn(snapSettings.MinSnapDistanceColName);
            }
            if (snapSettings.IsSnapDistanceAdded && (snapSettings.MaxSnapDistanceColName != null))
            {
                snappedLine.GENFile.DATFile.AddColumn(snapSettings.MaxSnapDistanceColName);
            }

            // Initialize statistics for snapped snapped distance of this feature
            double minSnapDistance = double.MaxValue;
            double maxSnapDistance = double.MinValue;

            // Loop through all points, find nearest line segment and snap point to nearest line segment
            // Inbetween points are checked as well and may be added also

            // The feature that is nearest to the current point (the first point of the current feature)
            GENFeature nearestFeature = genFile2.FindBestMatchingFeature(this, 0, snapTolerance);
            GENFeature prevNearestFeature = null;   // The feature that was nearest in the previous loop
            Point prevNearestFeaturePoint = null;   // The point that was nearest in the previous loop
            int prevNearestFeaturePointIdx = -1;    // The index of the point (into the pointlist of the nearest feature) that was nearest in the previous loop

            for (int currentPointIdx = 0; currentPointIdx < Points.Count(); currentPointIdx++)
            {
                Point currentPoint = Points[currentPointIdx];

                // First find the feature that is closest to this feature
                nearestFeature = genFile2.FindNearestSegmentFeature(currentPoint, (float)snapTolerance, null, snappedLine.Points, nearestFeature, Point.Tolerance);
                if (nearestFeature != null)
                {
                    Point nearestP1 = nearestFeature.FindNearestPoint(currentPoint, float.MaxValue);
                    int nearestP1Idx = nearestFeature.IndexOf(nearestP1);

                    // Check if the current point is within equality tolerance distance of the found nearest point of the other feature

                    // Identify the nearest segment of the other feature to the current point 
                    // of this feature, and snap the current point to that segment.

                    // Take the next closest point in nearest feature to create linesegment
                    // This may be the point with an index before or after the nearest point 
                    Point snappedCurrentPoint = null;
                    Point nearestP2 = null;
                    int nearestP2Idx = -1;
                    Point snappedCurrentPointb = null;
                    Point nearestP2b = null;
                    int nearestP2bIdx = -1;

                    int prevOtherPointIdx = -1;
                    int nextOtherPointIdx = -1;
                    Point prevOtherPoint = null;
                    Point nextOtherPoint = null;

                    // First check if the current point (or actually the snapped line from it) is perpendicular to the segment 
                    // from the nearest point (in the other feature) to the previous point in the pointlist (in the other feature) .
                    if (nearestP1Idx > 0)
                    {
                        prevOtherPointIdx = nearestP1Idx - 1;
                        prevOtherPoint = nearestFeature.Points[prevOtherPointIdx];
                        if (currentPoint.HasPerpendicularIntersection(prevOtherPoint, nearestP1))
                        {
                            nearestP2 = prevOtherPoint;
                            nearestP2Idx = prevOtherPointIdx;
                            snappedCurrentPoint = currentPoint.SnapToLineSegmentOptimized(prevOtherPoint, nearestP1);
                        }
                    }

                    // Try the segment to the next point (in the other feature) as well, since that segment may lie closer to the current point
                    if ((nearestP1Idx < nearestFeature.Points.Count - 1))
                    {
                        nextOtherPointIdx = nearestP1Idx + 1;
                        nextOtherPoint = nearestFeature.Points[nextOtherPointIdx];
                        if (currentPoint.HasPerpendicularIntersection(nearestP1, nextOtherPoint))
                        {
                            nearestP2b = nextOtherPoint;
                            nearestP2bIdx = nextOtherPointIdx;
                            snappedCurrentPointb = currentPoint.SnapToLineSegmentOptimized(nearestP1, nextOtherPoint);
                        }
                    }

                    // Select segment with nearest snapped point-
                    if ((snappedCurrentPoint == null) || ((snappedCurrentPointb != null) && (currentPoint.GetDistance(snappedCurrentPointb) < currentPoint.GetDistance(snappedCurrentPoint))))
                    {
                        nearestP2 = nearestP2b;
                        nearestP2Idx = nearestP2bIdx;
                        snappedCurrentPoint = snappedCurrentPointb;
                    }

                    // If no perpendicular line could be made from the current point to the other feature (which is the nearest feature)
                    // The point is beyond the other (line) feature and should be snapped to the closest point on the other feature: which should be nearestP1.
                    if (snappedCurrentPoint == null)
                    {
                        // Both lines don't have a perpendicular to the current point, which may indicate the beginning/end of a line
                        // Take the linesegment that is not null
                        if (prevOtherPoint != null)
                        {
                            nearestP2 = prevOtherPoint;
                            nearestP2Idx = prevOtherPointIdx;
                            snappedCurrentPoint = currentPoint.SnapToLineSegmentOptimized(prevOtherPoint, nearestP1);
                        }
                        else
                        {
                            nearestP2 = nextOtherPoint;
                            nearestP2Idx = nextOtherPointIdx;
                            snappedCurrentPoint = currentPoint.SnapToLineSegmentOptimized(nearestP1, nextOtherPoint);
                        }

                    }

                    double snappedDistance = currentPoint.GetDistance(snappedCurrentPoint);

                    // Only snap when the snapDistance is within the specified tolerance
                    if (snappedDistance < snapTolerance)
                    {
                        // Only add a point if it isn't snapped to the same point as the previous point
                        if ((snappedLine.Points.Count == 0) || !snappedCurrentPoint.Equals(snappedLine.Points[snappedLine.Points.Count - 1]))
                        {
                            Point nearestFeaturePoint = null;
                            int nearestFeaturePointIdx = -1;
                            // Check if points of the same nearest feature were skipped: check points between current nearestfeaturePoint and previous nearestfeaturePoint
                            if ((prevNearestFeaturePoint != null) && (prevNearestFeature != null) && (prevNearestFeature.Equals(nearestFeature)))
                            {
                                // Take point on nearest line segment that is closest to previously added point from other feature
                                if (Math.Abs(nearestP1Idx - prevNearestFeaturePointIdx) < Math.Abs(nearestP2Idx - prevNearestFeaturePointIdx))
                                {
                                    nearestFeaturePoint = nearestP1;
                                    nearestFeaturePointIdx = nearestP1Idx;
                                }
                                else
                                {
                                    nearestFeaturePoint = nearestP2;
                                    nearestFeaturePointIdx = nearestP2Idx;
                                }

                                // Actual check for skipped points
                                //if (Math.Abs(nearestFeaturePointIdx - prevNearestFeaturePointIdx) >= 1)
                                //{
                                Point lastAddedPoint = snappedLine.Points[snappedLine.Points.Count - 1];

                                // Add skipped points on otherfeature, between previous, snapped point and the currently snapped point
                                if (prevNearestFeaturePointIdx < nearestFeaturePointIdx)
                                {
                                    for (int idx = prevNearestFeaturePointIdx; idx <= nearestFeaturePointIdx; idx++)
                                    {
                                        Point skippedPoint = nearestFeature.Points[idx];
                                        double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                        if (snappedSkippedPointDistance > 0)
                                        {
                                            HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int idx = prevNearestFeaturePointIdx; idx >= nearestFeaturePointIdx; idx--)
                                    {
                                        Point skippedPoint = nearestFeature.Points[idx];
                                        double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                        if (snappedSkippedPointDistance > 0)
                                        {
                                            HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                        }
                                    }
                                }
                                //}
                            }
                            else
                            {
                                // There's no previous point yet for the current nearest feature, meaning a new feature was found close to the current point.
                                // Check if some of the points of the previous nearest feature, after the last added point, are closer to the lastly added 
                                // point (to the snappedLine) than the current snapped point. This can happen when there is a large distance between the 
                                // the current point to the next point in this line feature (the instance). In that case some points of the other nearest
                                // feature could be skipped. 

                                if (snappedLine.Points.Count > 0)
                                {
                                    Point lastAddedPoint = snappedLine.Points[snappedLine.Points.Count - 1];
                                    double snappedToLastAddedDistance = snappedCurrentPoint.GetDistance(lastAddedPoint);
                                    SortedDictionary<float, Point> distancePointDictionary = new SortedDictionary<float, Point>();

                                    // First add all of the points of the other previous nearest feature, that are nearer to the lastly added point than the snappedDistance, ordered by distance
                                    if (prevNearestFeature != null)
                                    {
                                        // Find part of previous feature that is not yet processed

                                        // Find nearest point of prev nearest feature to current point
                                        Point prevNearestFeatureNearestPoint = prevNearestFeature.FindNearestPoint(currentPoint, float.MaxValue);
                                        int prevNearestFeatureNearestPointIdx = prevNearestFeature.IndexOf(prevNearestFeatureNearestPoint);
                                        if (prevNearestFeaturePointIdx < prevNearestFeatureNearestPointIdx)
                                        {
                                            for (int idx = prevNearestFeaturePointIdx; idx <= prevNearestFeatureNearestPointIdx; idx++)
                                            {
                                                Point skippedPoint = prevNearestFeature.Points[idx];
                                                double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                                if (snappedSkippedPointDistance > 0)
                                                {
                                                    HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int idx = prevNearestFeaturePointIdx; idx >= prevNearestFeatureNearestPointIdx; idx--)
                                            {
                                                Point skippedPoint = prevNearestFeature.Points[idx];
                                                double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                                if (snappedSkippedPointDistance > 0)
                                                {
                                                    HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                                }
                                            }
                                        }
                                    }

                                    // Check for inbetween lines
                                    // TODO

                                    // Now select the possibly skipped points of the other current nearest feature, that are nearer to the lastly added point than the snappedDistance, ordered by distance
                                    lastAddedPoint = snappedLine.Points[snappedLine.Points.Count - 1];

                                    // Find nearest point of nearest feature to last added point
                                    Point nearestFeatureStartPoint = nearestFeature.FindNearestPoint(lastAddedPoint, float.MaxValue);
                                    int nearestFeatureStartPointIdx = nearestFeature.IndexOf(nearestFeatureStartPoint);
                                    if (nearestFeatureStartPointIdx < nearestP1Idx)
                                    {
                                        for (int idx = nearestFeatureStartPointIdx; idx <= nearestP1Idx; idx++)
                                        {
                                            Point skippedPoint = nearestFeature.Points[idx];
                                            double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                            if (snappedSkippedPointDistance > 0)
                                            {
                                                HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int idx = nearestFeatureStartPointIdx; idx >= nearestP1Idx; idx--)
                                        {
                                            Point skippedPoint = nearestFeature.Points[idx];
                                            double snappedSkippedPointDistance = SnapSkippedPoint(skippedPoint, snappedLine, lastAddedPoint, currentPoint, snappedCurrentPoint, snapTolerance);
                                            if (snappedSkippedPointDistance > 0)
                                            {
                                                HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedSkippedPointDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);
                                            }
                                        }
                                    }
                                }

                                nearestFeaturePoint = nearestP1;
                                nearestFeaturePointIdx = nearestP1Idx;
                            }

                            snappedLine.AddPoint(snappedCurrentPoint);
                            HandleSnappedPoint(currentPoint, nearestFeature, snappedCurrentPoint, snappedDistance, snapSettings, snappedFromFileColIdx, snappedToFileColIdx, snapFromDistanceColIdx, snapToDistanceColIdx, snapFromFeatureIdColIdx, snapToFeatureIdColIdx, ref minSnapDistance, ref maxSnapDistance);

                            // Store previous nearest point to check missing points between this point and the next nearest point
                            prevNearestFeature = nearestFeature;
                            prevNearestFeaturePoint = nearestFeaturePoint;
                            prevNearestFeaturePointIdx = nearestFeaturePointIdx;
                        }
                    }
                    else
                    {
                        // Add original, not snapped, point
                        snappedLine.AddPoint(currentPoint.Copy());

                        // Reset previous nearest point of check for missing point between this point and the next nearest point
                        prevNearestFeature = null;
                        prevNearestFeaturePoint = null;
                        prevNearestFeaturePointIdx = -1;
                    }
                }
            }

            // Check if original line ended into another feature, in that case continue with last nearest feature 
            // TODO?

            if (snappedLine.Points.Count > 0)
            {
                snappedLine.RemoveRedundantPoints(genFile2.Features);
                if (snapSettings.IsSnapDistanceAdded)
                {
                    if (snapSettings.MinSnapDistanceColName != null)
                    {
                        if (minSnapDistance.Equals(double.MaxValue))
                        {
                            minSnapDistance = 0;
                        }
                        snappedLine.SetColumnValue(snapSettings.MinSnapDistanceColName, minSnapDistance.ToString("F" + Point.Precision, EnglishCultureInfo));
                    }
                    if (snapSettings.MaxSnapDistanceColName != null)
                    {
                        if (maxSnapDistance.Equals(double.MinValue))
                        {
                            maxSnapDistance = 0;
                        }
                        snappedLine.SetColumnValue(snapSettings.MaxSnapDistanceColName, maxSnapDistance.ToString("F" + Point.Precision, EnglishCultureInfo));
                    }
                }

                return snappedLine;
            }
            else
            {
                return null;
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
            if ((matchPointIdx < 0) || (matchPointIdx > Points.Count))
            {
                throw new Exception("Specified point index (" + matchPointIdx + ") is out of range for specified feature");
            }

            if (otherFeature is GENPoint)
            {
                Point matchPoint = this.Points[matchPointIdx];
                if (matchPoint.GetDistance(otherFeature.Points[0]) < tolerance)
                {
                    return otherFeature;
                }
            }

            if (otherFeature is GENPolygon)
            {
                // treat polygon as a line 
                otherFeature = new GENLine(otherFeature.GENFile, otherFeature.ID, otherFeature.Points);
            }

            if (!(otherFeature is GENLine))
            {
                throw new Exception("Unsuppored GENFeature: " + otherFeature.GetType().Name);
            }

            GENLine otherLine = (GENLine)otherFeature;
            GENLine snappedLine = new GENLine(null, this.ID);
            List<LineSegment> snappedSegments = new List<LineSegment>();

            // Find direction for iteration through pointList of this feature and other feature
            int currentSegmentP1Idx = matchPointIdx;
            Point currentSegmentP1 = this.Points[currentSegmentP1Idx];
            Point currentSegmentP2 = null;
            LineSegment nearestSegment = null;
            Point snappedCurrentP1 = null;
            Point snappedCurrentP2 = null;
            double distance = float.MaxValue;

            // Try two segments in this featurethat contain the specified matchPoint 
            Point currentSegmentP21 = (currentSegmentP1Idx < Points.Count - 1) ? Points[currentSegmentP1Idx + 1] : null;
            Point currentSegmentP22 = (currentSegmentP1Idx > 1) ? Points[currentSegmentP1Idx - 1] : null;
            LineSegment currentSegment1 = null;
            LineSegment currentSegment2 = null;
            int step1 = 0;
            int step2 = 0;
            LineSegment nearestSegment1 = null;
            LineSegment nearestSegment2 = null;
            Point snappedCurrentP11 = null;
            Point snappedCurrentP21 = null;
            Point snappedCurrentP12 = null;
            Point snappedCurrentP22 = null;
            double distance1 = float.MaxValue;
            double distance2 = float.MaxValue;

            // Calculate distance from both segments to the nearest segment from the other feature
            if (currentSegmentP21 != null)
            {
                currentSegment1 = new LineSegment(currentSegmentP1, currentSegmentP21);
                nearestSegment1 = otherLine.FindNearestSegment(currentSegment1, float.MaxValue);
                snappedCurrentP11 = currentSegmentP1.SnapToLineSegmentOptimized(nearestSegment1.P1, nearestSegment1.P2);
                snappedCurrentP21 = currentSegmentP21.SnapToLineSegmentOptimized(nearestSegment1.P1, nearestSegment1.P2);
                distance1 = currentSegmentP1.GetDistance(snappedCurrentP11) + currentSegmentP21.GetDistance(snappedCurrentP21);
            }
            if (currentSegmentP22 != null)
            {
                currentSegment2 = new LineSegment(currentSegmentP1, currentSegmentP22);
                nearestSegment2 = otherLine.FindNearestSegment(currentSegment1, float.MaxValue);
                snappedCurrentP12 = currentSegmentP1.SnapToLineSegmentOptimized(nearestSegment2.P1, nearestSegment2.P2);
                snappedCurrentP22 = currentSegmentP22.SnapToLineSegmentOptimized(nearestSegment2.P1, nearestSegment2.P2);
                distance2 = currentSegmentP1.GetDistance(snappedCurrentP21) + currentSegmentP21.GetDistance(snappedCurrentP22);
            }
            // determine iteration direction for the points of this feature
            if (distance1 < distance2)
            {
                currentSegmentP2 = currentSegmentP21;
                nearestSegment = nearestSegment1;
                snappedCurrentP1 = snappedCurrentP11;
                snappedCurrentP2 = snappedCurrentP21;
                step1 = 1;
            }
            else
            {
                currentSegmentP2 = currentSegmentP22;
                nearestSegment = nearestSegment2;
                snappedCurrentP1 = snappedCurrentP12;
                snappedCurrentP2 = snappedCurrentP22;
                distance = distance2;
                step1 = -1;
            }

            Point nearestSegmentP1 = nearestSegment.P1;
            Point nearestSegmentP2 = nearestSegment.P2;
            int nearestSegmentP1Idx = otherLine.IndexOf(nearestSegment.P1);
            int nearestSegmentP2Idx = otherLine.IndexOf(nearestSegment.P2);
            step2 = nearestSegmentP2Idx - nearestSegmentP1Idx;

            // Process segments
            distance = currentSegmentP1.GetDistance(snappedCurrentP1);
            if (distance < tolerance)
            {
                snappedLine.AddPoint(snappedCurrentP1);
            }

            // Loop through all segments as long as a snap within tolerance is found 
            // The orientation of both lines is known now and will be used to retrieve segments with the same orientation
            int precision = GENUtils.GetPrecision(Point.Tolerance);
            while ((currentSegmentP2 != null) && (nearestSegmentP2 != null))
            {
                // There are two line segments: currentSegment S1 (from the snappedLine) is snapped to nearestSegment S2 (from the snapLine). 
                // The snapresult can be represented with a most 3 parts and 4 points: A---B---C---D 
                // currentSegment S1 is from point P11 to point P12, 
                // nearestSegment S2 is from point P21 to point P22, 
                // Assume P11 has been snapped already to segment S2

                // There are 2 options for a perpendicular snap of the second point of currentSegment1 P12: 
                // A) P12 snaps perpendicular to S2: snap P12 to S2 and continue with the next segment from the snappedLine
                // B) P12 does not snap perpendicular to S2 and is beyond S2:
                // B1) P22 snaps perpendicular to S1: add P22 to the snappedLine and continue with the next segment from the snapLine
                // B2) P22 does not snap perpendicular to S1, the snapped S1 doesn't have any overlap with S2, 
                //     P12 must be before S2, ignore P12 (since P11 will already have been snapped to P21) 
                //     and continue with the next segment from the snapped Line

                if (currentSegmentP2.HasPerpendicularIntersection(nearestSegmentP1, nearestSegmentP2))
                {
                    // Situaton A: P12 snaps perpendicular to S2, snap P12 to S2 and continue with the next segment from the snappedLine
                    Point snappedP12 = currentSegmentP2.SnapToLineSegmentOptimized(nearestSegmentP1, nearestSegmentP2);
                    double snapDistance = currentSegmentP2.GetDistance(snappedP12);
                    if (snapDistance < tolerance)
                    {
                        snappedLine.AddPoint(snappedP12);

                        currentSegmentP1Idx += step1;
                        currentSegmentP1 = Points[currentSegmentP1Idx];
                        currentSegmentP2 = ((currentSegmentP1Idx + step1 >= 0) && (currentSegmentP1Idx + step1 < Points.Count)) ? Points[currentSegmentP1Idx + step1] : null;
                    }
                    else
                    {
                        // Stop snapping 
                        return snappedLine;
                    }
                }
                // Situation B: P12 does not snap perpendicular to S2 and is beyond S2
                else if (nearestSegmentP2.HasPerpendicularIntersection(currentSegmentP1, currentSegmentP2))
                {
                    // SituationB1: P22 snaps perpendicular to S1: if close enough, add P22 to the snappedLine and continue with the next segment from the snapLine
                    Point snappedP22 = nearestSegmentP2.SnapToLineSegmentOptimized(currentSegmentP1, currentSegmentP2);
                    double snapDistance = nearestSegmentP2.GetDistance(snappedP22);
                    if (snapDistance < tolerance)
                    {
                        snappedLine.AddPoint(snappedP22);

                        nearestSegmentP1Idx += step2;
                        nearestSegmentP1 = otherLine.Points[nearestSegmentP1Idx];
                        nearestSegmentP2 = ((nearestSegmentP1Idx + step2 >= 0) && (nearestSegmentP1Idx + step2 < otherLine.Points.Count)) ? otherLine.Points[nearestSegmentP1Idx + step2] : null;
                    }
                    else
                    {
                        // Stop snapping 
                        return snappedLine;
                    }
                }
                else
                {
                    // Situation B2: P22 does not snap perpendicular to S1, the snapped S1 doesn't have any overlap with S2
                    // distinguish between situations: 
                    // B2.1: S1 is oriented in same direction as S2, but is before S2: P12 is closer to P21 than P11
                    // B2.2: S1 is oriented in opposite direction as S2: P12 is further away from P21 than P11
                    // note: the same distance is not possible, a perpendicular snap from P21 to S1 would be possible then
                    if (currentSegmentP2.GetDistance(nearestSegmentP1) < currentSegmentP1.GetDistance(nearestSegmentP1))
                    {
                        // P12 is before S2, ignore P12 (since P11 will already have been snapped to P21) 
                        // and continue with the next segment from the snapped Line
                        currentSegmentP1Idx += step1;
                        currentSegmentP1 = Points[currentSegmentP1Idx];
                        currentSegmentP2 = ((currentSegmentP1Idx + step1 >= 0) && (currentSegmentP1Idx + step1 < Points.Count)) ? Points[currentSegmentP1Idx + step1] : null;
                    }
                    else
                    {
                        // Stop snapping 
                        return snappedLine;
                    }
                }
            }

            return snappedLine;
        }

        private void HandleSnappedPoint(Point currentPoint, GENFeature snapFeature, Point snappedCurrentPoint, double snappedDistance, SnapSettings snapSettings, int snappedFromFileColIdx, int snappedToFileColIdx, int snapFromDistanceColIdx, int snapToDistanceColIdx, int snapFromFeatureIdColIdx, int snapToFeatureIdColIdx, ref double minSnapDistance, ref double maxSnapDistance)
        {
            if (snapSettings.IsSnappedIPFPointAdded)
            {
                if (snapSettings.SnappedToPointsIPFFile != null)
                {
                    string[] values = new string[snapSettings.SnappedToPointsIPFFile.ColumnCount];
                    values[0] = snappedCurrentPoint.X.ToString("F3", EnglishCultureInfo);
                    values[1] = snappedCurrentPoint.Y.ToString("F3", EnglishCultureInfo);
                    //if (!snappedCurrentPoint.Z.Equals(double.NaN))
                    //{
                    //    values[2] = snappedCurrentPoint.Z.ToString("F3", EnglishCultureInfo);
                    //}
                    values[snapToDistanceColIdx] = snappedDistance.ToString("F" + Point.Precision, EnglishCultureInfo);
                    values[snapToFeatureIdColIdx] = snapFeature.ToString();
                    if ((this.GENFile != null) && (this.GENFile.Filename != null))
                    {
                        values[snappedToFileColIdx] = Path.GetFileNameWithoutExtension(this.GENFile.Filename);
                    }

                    snapSettings.SnappedToPointsIPFFile.AddPoint(new IPFPoint(snapSettings.SnappedToPointsIPFFile, snappedCurrentPoint, values));
                }

                if (snapSettings.SnappedFromPointsIPFFile != null)
                {
                    string[] values = new string[snapSettings.SnappedFromPointsIPFFile.ColumnCount];
                    values[0] = currentPoint.X.ToString("F3", EnglishCultureInfo);
                    values[1] = currentPoint.Y.ToString("F3", EnglishCultureInfo);
                    //if (!currentPoint.Z.Equals(double.NaN))
                    //{
                    //    values[2] = currentPoint.Z.ToString("F3", EnglishCultureInfo);
                    //}
                    values[snapFromDistanceColIdx] = snappedDistance.ToString("F" + Point.Precision, EnglishCultureInfo);
                    values[snapFromFeatureIdColIdx] = this.ToString();
                    if ((this.GENFile != null) && (this.GENFile.Filename != null))
                    {
                        values[snappedFromFileColIdx] = Path.GetFileNameWithoutExtension(this.GENFile.Filename);
                    }

                    snapSettings.SnappedFromPointsIPFFile.AddPoint(new IPFPoint(snapSettings.SnappedFromPointsIPFFile, snappedCurrentPoint, values));
                }
            }

            if (snappedDistance < minSnapDistance)
            {
                minSnapDistance = snappedDistance;
            }
            if (snappedDistance > maxSnapDistance)
            {
                maxSnapDistance = snappedDistance;
            }

        }

        /// <summary>
        /// Snaps a possibly skipped point and add it to the specified snappedLine, if 1) it has a perpendicular snapline to the linesegment from the previous snapped point 
        /// to the snapped currentPoint and 2) it has a snapdistance less than the snaptolerance to the linesegment from the previous snapped point to the current point
        /// </summary>
        /// <param name="skippedPoint">the is a point that may have been skipped</param>
        /// <param name="snappedLine">the is the list of point that are already snapped</param>
        /// <param name="previousSnappedPoint">this is last point from the snappedLine, that is already snapped to the snapLine</param>
        /// <param name="currentPoint">this is the current point before being snapped to the snapLine</param>
        /// <param name="snappedCurrentPoint">the is the snapped current point, snapped to the snapLine</param>
        /// <param name="snapTolerance">this is the maximum distance that is allowed for snapping</param>
        /// <returns>snapDistance, 0 if not snapped</returns>
        private double SnapSkippedPoint(Point skippedPoint, GENLine snappedLine, Point previousSnappedPoint, Point currentPoint, Point snappedCurrentPoint, double snapTolerance)
        {
            // Snap possibly skipped point back to other linesegment, to see if it is within tolerance distance
            // Require the skipped point to be perpendicular to the snapped line segment, but determine the snapdistance 
            // to the linesegment from the last snapped point to the current (not yet snapped) point
            if (skippedPoint.HasPerpendicularIntersection(previousSnappedPoint, snappedCurrentPoint))
            {
                Point snappedSkippedPoint = skippedPoint.SnapToLineSegmentOptimized(previousSnappedPoint, currentPoint);
                double snappedSkippedPointDistance = snappedSkippedPoint.GetDistance(skippedPoint);
                if (snappedSkippedPointDistance < snapTolerance)
                {
                    if (!snappedLine.HasSimilarPoint(skippedPoint))
                    {
                        snappedLine.AddPoint(skippedPoint.Copy());
                        return snappedSkippedPointDistance;
                    }
                }
            }
            return 0;
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
        public LineSegment FindNearestSegment(Point point, double tolerance = double.NaN)
        {
            LineSegment lineSegment = null;

            // Compare with all LineSegments of the given feature
            double minSegmentDistance = (tolerance.Equals(double.NaN)) ? double.MaxValue : tolerance;

            Point startPoint = Points[0];
            for (int pointIdx = 1; pointIdx < Points.Count; pointIdx++)
            {
                Point endPoint = Points[pointIdx];
                Point snappedPoint = point.SnapToLineSegmentOptimized(startPoint, endPoint);
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
        /// <param name="lineSegment"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public LineSegment FindNearestSegment(LineSegment lineSegment, float maxDistance = float.NaN)
        {
            return GENFeature.FindNearestSegment(this.Points, lineSegment, maxDistance);
        }
    }
}

