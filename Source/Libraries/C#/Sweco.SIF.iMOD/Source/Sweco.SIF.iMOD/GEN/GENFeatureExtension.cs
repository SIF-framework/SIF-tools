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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Extension class that adds methods to GENFeature objects
    /// </summary>
    public static class GENFeatureExtension
    {
        /// <summary>
        /// Retrieve the highest ID string that can be parsed to a number
        /// </summary>
        /// <param name="genFeatures"></param>
        /// <returns></returns>
        public static int GetMaxFeatureID(this List<GENFeature> genFeatures)
        {
            int maxIdx = 0;
            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                if (genFeatures[featureIdx] != null)
                {
                    int idValue;
                    if (int.TryParse(genFeatures[featureIdx].ID, out idValue))
                    {
                        if (idValue > maxIdx)
                        {
                            maxIdx = idValue;
                        }
                    }
                }
            }
            return maxIdx;
        }

        /// <summary>
        /// Find feature that has the best match with the first part of this feature and that has a point within the specified distance to the specfied point of this feature
        /// A feature that has 100% overlap with this feature and the contains the specified matchpoint has priority
        /// </summary>
        /// <param name="matchFeature"></param>
        /// <param name="matchPointIdx"></param>
        /// <param name="genFeatures"></param>
        /// <param name="tolerance"></param>
        /// <param name="excludedFeatures"></param>
        /// <returns></returns>
        public static GENFeature FindBestMatchingFeature(this GENFeature matchFeature, int matchPointIdx, List<GENFeature> genFeatures, double tolerance, List<GENFeature> excludedFeatures = null)
        {
            if ((matchPointIdx < 0) || (matchPointIdx > matchFeature.Points.Count))
            {
                throw new Exception("Specified point index (" + matchPointIdx + ") is out of range for specified feature");
            }

            // First find all features that are within tolerance distance of the specified matchedPoint
            List<GENFeature> nearFeatures = genFeatures.FindFeatures(matchFeature.Points[matchPointIdx], tolerance);

            int precision = (int)Math.Round(-Math.Log10(Point.Tolerance), 0);
            double maxOverlapMeasure = double.MinValue;
            double maxOverlapRatio = 0;
            GENFeature bestMatchingFeature = null;
            foreach (GENFeature feature in nearFeatures)
            {
                if ((excludedFeatures == null) || !excludedFeatures.Contains(feature))
                {
                    GENFeature snappedFeaturePart = matchFeature.SnapPart(matchPointIdx, feature, tolerance);
                    if (snappedFeaturePart != null)
                    {
                        double featureMeasure = feature.CalculateMeasure();
                        double overlapMeasure = snappedFeaturePart.CalculateMeasure();
                        double overlapRatio = Math.Round(overlapMeasure / featureMeasure, precision);
                        if (overlapRatio.Equals(1.0))
                        {
                            // Give priority to features that have 100% overlap with matchFeature
                            maxOverlapRatio = overlapRatio;
                            maxOverlapMeasure = overlapMeasure;
                            bestMatchingFeature = feature;
                        }
                        else if ((overlapMeasure > maxOverlapMeasure) && (!maxOverlapRatio.Equals(1.0)))
                        {
                            maxOverlapMeasure = overlapMeasure;
                            maxOverlapRatio = overlapRatio;
                            bestMatchingFeature = feature;
                        }
                    }
                }
            }

            return bestMatchingFeature;
        }


        /// <summary>
        /// Find features within the specified list of features that are within tolerance distance of the specified point
        /// </summary>
        /// <param name="genFeatures"></param>
        /// <param name="point"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static List<GENFeature> FindFeatures(this List<GENFeature> genFeatures, Point point, double tolerance)
        {
            List<GENFeature> nearFeatures = new List<GENFeature>();
            // Search within all features
            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                GENFeature feature = genFeatures[featureIdx];
                // Compare with all points of the current feature
                int pointIdx = 0;
                while (pointIdx < feature.Points.Count)
                {
                    Point otherPoint = feature.Points[pointIdx];
                    Point nearestPoint = feature.FindNearestPoint(otherPoint, float.MaxValue);
                    double distance = nearestPoint.GetDistance(otherPoint);
                    if (distance < tolerance)
                    {
                        nearFeatures.Add(feature);
                        pointIdx = feature.Points.Count;
                    }
                    else
                    {
                        pointIdx++;
                    }
                }
            }
            return nearFeatures;
        }

        /// <summary>
        /// Find feature that has the segment that is closest (perpendicular) to the specified point.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="genFeatures"></param>
        /// <param name="tolerance">Maximum distance to closest point of other feature</param>
        /// <returns></returns>
        public static GENFeature FindNearestSegmentFeature(this Point point, List<GENFeature> genFeatures, double tolerance)
        {
            GENFeature nearestFeature = null;
            double minSegmentDistance = double.MaxValue;

            // Search within all features
            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                GENFeature feature = genFeatures[featureIdx];
                Extent featureExtent = feature.RetrieveExtent();
                if (point.GetDistance(featureExtent) < tolerance)
                {
                    // Compare with all LineSegments of the given feature
                    Point startPoint = feature.Points[0];
                    for (int pointIdx = 1; pointIdx < feature.Points.Count; pointIdx++)
                    {
                        Point endPoint = feature.Points[pointIdx];

                        Point snappedPoint = point.SnapToLineSegment(startPoint, endPoint);
                        float segmentDistance = (float)snappedPoint.GetDistance(point);

                        if (segmentDistance < tolerance)
                        {
                            if (segmentDistance < minSegmentDistance)
                            {
                                minSegmentDistance = segmentDistance;
                                nearestFeature = feature;
                            }
                        }
                        startPoint = endPoint;
                    }
                }
            }
            return nearestFeature;
        }

        /// <summary>
        /// Find (first) feature that has the closest segment (perpendicular) to the specified point; also return nearest segment.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="genFeatures"></param>
        /// <param name="tolerance">Maximum distance to closest point of other feature</param>
        /// <param name="nearestSegment"></param>
        /// <returns></returns>
        public static GENFeature FindNearestSegmentFeature(this Point point, List<GENFeature> genFeatures, double tolerance, out LineSegment nearestSegment)
        {
            List<GENFeature> nearestFeatures = FindNearestSegmentFeatures(point, genFeatures, tolerance, out List<LineSegment> nearestSegments);
            if (nearestFeatures.Count > 0)
            {
                nearestSegment = nearestSegments[0];
                return nearestFeatures[0];
            }
            else
            {
                nearestSegment = null;
                return null;
            }
        }

        /// <summary>
        /// Find features that have the closest segment (perpendicular) to the specified point; also return nearest segments per feature.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="genFeatures"></param>
        /// <param name="maxDistance">Maximum distance to closest point of other feature</param>
        /// <param name="nearestSegments"></param>
        /// <returns></returns>
        public static List<GENFeature> FindNearestSegmentFeatures(this Point point, List<GENFeature> genFeatures, double maxDistance, out List<LineSegment> nearestSegments)
        {
            List<GENFeature> nearestFeatures = new List<GENFeature>();
            nearestSegments = new List<LineSegment>();
            double minSegmentDistance = maxDistance;

            // Search within all features
            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                GENFeature feature = genFeatures[featureIdx];
                Extent featureExtent = feature.RetrieveExtent();
                if (point.GetDistance(featureExtent) < maxDistance)
                {
                    // Compare with all LineSegments of the given feature
                    Point startPoint = feature.Points[0];
                    for (int pointIdx = 1; pointIdx < feature.Points.Count; pointIdx++)
                    {
                        Point endPoint = feature.Points[pointIdx];

                        Point snappedPoint = point.SnapToLineSegment(startPoint, endPoint);
                        float segmentDistance = (float)snappedPoint.GetDistance(point);
                        if (segmentDistance <= minSegmentDistance)
                        {
                            if (segmentDistance < minSegmentDistance)
                            {
                                minSegmentDistance = segmentDistance;
                                nearestFeatures.Clear();
                                nearestSegments.Clear();
                            }

                            if (!nearestFeatures.Contains(feature))
                            {
                                nearestFeatures.Add(feature);
                                nearestSegments.Add(new LineSegment(startPoint, endPoint));
                            }
                        }
                        startPoint = endPoint;
                    }
                }
            }
            return nearestFeatures;
        }

        /// <summary>
        /// Find feature that has the segment that is closest (perpendicular) to the specified point. Excluded and preferred points can be defined to define result.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="genFeatures"></param>
        /// <param name="tolerance">Maximum distance to closest point of other feature</param>
        /// <param name="excludedFeatures">List of features that should be excluded in the search</param>
        /// <param name="excludedPoints">List of Point that should be excluded in the search</param>
        /// <param name="preferredFeature">Preferred feature in case a choice should be made between points less than Point.Tolerance distance apart. Normally this is the feature that was snapped to before.</param>
        /// <param name="preferredFeatureTolerance">Maximum distance to closest point of preferred feature, default (or when NaN is specified) is Point.Tolerance</param>
        /// <returns></returns>
        public static GENFeature FindNearestSegmentFeature(this Point point, List<GENFeature> genFeatures, double tolerance, List<GENFeature> excludedFeatures, List<Point> excludedPoints = null, GENFeature preferredFeature = null, double preferredFeatureTolerance = double.NaN)
        {
            GENFeature nearestFeature = null;
            double minSegmentDistance = double.MaxValue;
            if (preferredFeatureTolerance.Equals(double.NaN))
            {
                preferredFeatureTolerance = Point.Tolerance;
            }

            // Search within all features
            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                GENFeature feature = genFeatures[featureIdx];
                Extent featureExtent = feature.RetrieveExtent();
                if (point.GetDistance(featureExtent) < tolerance)
                {
                    // Skip excluded features
                    if ((excludedFeatures == null) || !excludedFeatures.Contains(feature))
                    {
                        // Compare with all LineSegments of the given feature
                        Point startPoint = feature.Points[0];
                        for (int pointIdx = 1; pointIdx < feature.Points.Count; pointIdx++)
                        {
                            Point endPoint = feature.Points[pointIdx];

                            // Skip excluded points
                            if ((excludedPoints == null) || !excludedPoints.HasSimilarPoint(startPoint) || !excludedPoints.HasSimilarPoint(endPoint))
                            {
                                Point snappedPoint = point.SnapToLineSegment(startPoint, endPoint);
                                float segmentDistance = (float)snappedPoint.GetDistance(point); // startPointDistance + endPointDistance;

                                if (segmentDistance < tolerance)
                                {
                                    if (segmentDistance < minSegmentDistance)
                                    {
                                        if (((nearestFeature != null) && nearestFeature.Equals(preferredFeature)) && ((minSegmentDistance - segmentDistance) <= preferredFeatureTolerance))
                                        {
                                            // ignore, leave preferred feature
                                        }
                                        else
                                        {
                                            minSegmentDistance = segmentDistance;
                                            nearestFeature = feature;
                                        }
                                    }
                                    else if (feature.Equals(preferredFeature) && !nearestFeature.Equals(preferredFeature))
                                    {
                                        // If the tested feature is the preferred feature and differs only the defined preferredFeatureTolerance distance, use the preferred feature
                                        if (segmentDistance - minSegmentDistance <= preferredFeatureTolerance)
                                        {
                                            minSegmentDistance = segmentDistance;
                                            nearestFeature = feature;
                                        }
                                    }
                                }
                            }
                            startPoint = endPoint;
                        }
                    }
                }
            }
            return nearestFeature;
        }

        /// <summary>
        /// Find nearest feature to this feature in specified list of features
        /// This is the feature that has a minimum summed distance to all points of the other feature
        /// Features with points within specified tolerance always have priority
        /// </summary>
        /// <param name="thisFeature"></param>
        /// <param name="otherFeatures"></param>
        /// <param name="tolerance">Maximum distance to other feature</param>
        /// <param name="excludedFeatures"></param>
        /// <returns></returns>
        public static GENFeature FindNearestFeature(this GENFeature thisFeature, List<GENFeature> otherFeatures, double tolerance, List<GENFeature> excludedFeatures = null)
        {
            if (thisFeature is GENPoint)
            {
                return ((GENPoint)thisFeature).FindNearestFeature(otherFeatures, tolerance, excludedFeatures);
            }

            GENFeature nearestFeature = null;
            double minDistance = double.MaxValue;
            double distance = double.NaN;

            // Search within all features
            for (int otherFeatureIdx = 0; otherFeatureIdx < otherFeatures.Count; otherFeatureIdx++)
            {
                GENFeature otherFeature = otherFeatures[otherFeatureIdx];
                if ((excludedFeatures == null) || !excludedFeatures.Contains(otherFeature))
                {
                    if (otherFeature is GENPoint)
                    {
                        Point closestPoint;
                        distance = otherFeature.GetDistance(((GENPoint)otherFeature).Point, out closestPoint);
                    }
                    else
                    {
                        // Compare segmentwise to find minimal distance 
                        Point thisPoint1 = thisFeature.Points[0];
                        for (int thisPointIdx = 1; thisPointIdx < thisFeature.Points.Count; thisPointIdx++)
                        {
                            Point thisPoint2 = thisFeature.Points[thisPointIdx];
                            LineSegment thisSegment = new LineSegment(thisPoint1, thisPoint2);

                            Point otherPoint1 = otherFeature.Points[0];
                            Point closestPoint1;
                            Point closestPoint2;
                            for (int otherPointIdx = 1; otherPointIdx < otherFeature.Points.Count; otherPointIdx++)
                            {
                                Point otherPoint2 = otherFeature.Points[otherPointIdx];
                                distance = thisSegment.GetDistance(otherPoint1, otherPoint2, out closestPoint1, out closestPoint2);
                            }
                        }

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestFeature = otherFeature;
                        }
                    }
                }

                // ToDo: Check if this is correct; loop will never be executed more than once now
                return nearestFeature;
            }

            return nearestFeature;
        }

        /// <summary>
        /// Find feature from list of features that is nearest to this GENPoint, within specified distance (tolerance)
        /// </summary>
        /// <param name="thisGENPoint"></param>
        /// <param name="features"></param>
        /// <param name="tolerance"></param>
        /// <param name="excludedFeatures">list of features to exclude from search (note: currently not used)</param>
        /// <returns></returns>
        public static GENFeature FindNearestFeature(this GENPoint thisGENPoint, List<GENFeature> features, double tolerance, List<GENFeature> excludedFeatures = null)
        {
            GENFeature nearestFeature = null;
            double minDistance = double.MaxValue;
            double distance = double.NaN;
            Point thisPoint = thisGENPoint.Point;

            // Search within all features
            Point currentClosestPoint;
            for (int otherFeatureIdx = 0; otherFeatureIdx < features.Count; otherFeatureIdx++)
            {
                GENFeature otherFeature = features[otherFeatureIdx];
                if (otherFeature is GENPoint)
                {
                    distance = thisPoint.GetDistance(((GENPoint)otherFeature).Point);
                }
                else
                {
                    distance = otherFeature.GetDistance(thisPoint, out currentClosestPoint);
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestFeature = otherFeature;
                }
            }
            return nearestFeature;
        }

        /// <summary>
        /// Find nearest point to the specified point and within search distance in this GENFile 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="genFeatures"></param>
        /// <param name="tolerance"></param>
        /// <param name="excludedFeatures"></param>
        /// <returns></returns>
        public static Point FindNearestPoint(this Point point, List<GENFeature> genFeatures, double tolerance, List<GENFeature> excludedFeatures = null)
        {
            // Search within all features
            double minDistance = tolerance;
            Point nearestPoint = null;

            for (int featureIdx = 0; featureIdx < genFeatures.Count; featureIdx++)
            {
                GENFeature feature = genFeatures[featureIdx];
                Point point2 = feature.FindNearestPoint(point, tolerance);
                if (point2 != null)
                {
                    double distance = point2.GetDistance(point);
                    if (distance < minDistance)
                    {
                        if ((excludedFeatures == null) || !excludedFeatures.Contains(feature))
                        {
                            minDistance = distance;
                            nearestPoint = point2;
                        }
                    }
                }
            }
            return nearestPoint;
        }

        /// <summary>
        /// Calculate minimum distance from specified point to this feature. The closest point on the feature is returned as well
        /// </summary>
        /// <param name="thisGENFeature"></param>
        /// <param name="point"></param>
        /// <param name="closestPoint"></param>
        /// <returns></returns>
        public static double GetDistance(this GENFeature thisGENFeature, Point point, out Point closestPoint)
        {
            closestPoint = null;
            if (thisGENFeature is GENPoint)
            {
                return ((GENPoint)thisGENFeature).Point.GetDistance(point);
            }

            Point thisPoint1 = thisGENFeature.Points[0];
            double minDistance = double.MaxValue;
            Point currentClosestPoint;
            for (int thisPointIdx = 1; thisPointIdx < thisGENFeature.Points.Count; thisPointIdx++)
            {
                Point thisPoint2 = thisGENFeature.Points[thisPointIdx];
                LineSegment segment = new LineSegment(thisPoint1, thisPoint2);
                double distance = segment.GetDistance(point, out currentClosestPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = currentClosestPoint;
                }
                thisPoint1 = thisPoint2;
            }

            return minDistance;
        }

    }
}
