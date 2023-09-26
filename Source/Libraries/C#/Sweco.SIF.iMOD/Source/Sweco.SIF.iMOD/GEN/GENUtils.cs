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
    /// Common utilities for GEN-file processing
    /// </summary>
    public class GENUtils
    {
        /// <summary>
        /// Whitespace characters for which GEN columnstrings are evaluted to check if surrounding with quotes is needed
        /// </summary>
        public static char[] CheckedWhiteSpaceChars = new char[] { ' ', '\t', ',' };

        /// <summary>
        /// Corrects a string for a DATRow by adding single quotes around strings that contain
        /// one or more spaces, tabs, comma's. Backslashes (single or double) are replaced by forward slashes.
        /// </summary>
        /// <param name="someValue"></param>
        /// <returns></returns>
        public static string CorrectString(string someValue)
        {
            if (someValue != null)
            {
                if (someValue.IndexOfAny(CheckedWhiteSpaceChars) >= 0)
                {
                    if (!someValue.StartsWith("'"))
                    {
                        someValue = "'" + someValue;
                    }
                    if (!someValue.EndsWith("'"))
                    {
                        someValue = someValue + "'";
                    }
                }
                if (someValue.Contains('\\'))
                {
                    someValue = someValue.Replace("\\\\", "/");
                    someValue = someValue.Replace('\\', '/');
                }
            }
            return someValue;
        }

        /// <summary>
        /// Convert ClipperLib polygpn (see Sweco.SIF.GIS) to GENPolygon object
        /// </summary>
        /// <param name="clipperPolygon"></param>
        /// <param name="polygonIdx"></param>
        /// <returns></returns>
        public static GENPolygon ConvertPolygonClipperToGEN(List<List<ClipperLib.IntPoint>> clipperPolygon, int polygonIdx = 0)
        {
            GENPolygon genPolygon = null;
            if ((clipperPolygon != null) && (clipperPolygon.Count > polygonIdx))
            {
                genPolygon = new GENPolygon(null, "");
                List<ClipperLib.IntPoint> pointList = clipperPolygon[polygonIdx];
                for (int pointIdx = 0; pointIdx < pointList.Count(); pointIdx++)
                {
                    ClipperLib.IntPoint point = pointList[pointIdx];
                    genPolygon.Points.Add(new DoublePoint(point.X, point.Y));
                }
                if (genPolygon.Points.Count > 2)
                {
                    genPolygon.Points.Add(genPolygon.Points[0]);
                }
            }
            return genPolygon;
        }

        /// <summary>
        /// Convert GENPolygon to ClipperLib polygpn (see Sweco.SIF.GIS)
        /// </summary>
        /// <param name="genPolygon"></param>
        /// <returns></returns>
        public static List<List<ClipperLib.IntPoint>> ConvertPolygonGENToClipper(GENPolygon genPolygon)
        {
            List<List<ClipperLib.IntPoint>> polygon = null;
            if (genPolygon != null)
            {
                polygon = new List<List<ClipperLib.IntPoint>>();
                List<ClipperLib.IntPoint> pointList = new List<ClipperLib.IntPoint>();
                for (int pointIdx = 0; pointIdx < genPolygon.Points.Count; pointIdx++)
                {
                    Point point = genPolygon.Points[pointIdx];
                    pointList.Add(new ClipperLib.IntPoint(point.X, point.Y));

                }
                polygon.Add(pointList);
            }
            return polygon;
        }

        /// <summary>
        /// Generic method to write a list of GENFeatures to a GEN-file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="genFeatures"></param>
        /// <param name="genFilename"></param>
        /// <param name="metadata"></param>
        public static void WriteFeatures<T>(List<T> genFeatures, string genFilename, Metadata metadata = null) where T : GENFeature
        {
            GENFile genFile = new GENFile(genFeatures.Count);

            List<GENFeature> tmpGENFeatures = new List<GENFeature>();
            foreach (T genFeatureObject in genFeatures)
            {
                GENFeature genFeature = (GENFeature)genFeatureObject;
                tmpGENFeatures.Add(genFeature);
            }

            genFile.AddFeatures(tmpGENFeatures);
            genFile.WriteFile(genFilename, metadata);
        }

        /// <summary>
        /// Converts the specified tolerance to a precision: -log10(tolerance) rounded to an integer
        /// This will return 0 for 1, 1 for 0.1 and 2 for 0.01, etc. For tolerance 0, 0 is returned
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static int GetPrecision(double tolerance)
        {
            if (tolerance.Equals(0) || (tolerance >= 1))
            {
                return 0;
            }
            else
            {
                return (int)Math.Round(-Math.Log10(tolerance), 0);
            }
        }
    }

    /// <summary>
    /// Extension for Point class with utitilies for GEN-file objects
    /// </summary>
    public static class PointExtenxion
    {
        /// <summary>
        /// Return the point on the specified line that is closest to this point
        /// </summary>
        /// <param name="L"></param>
        /// <returns>the closest point on the line</returns>
        public static Point SnapToLineSegment(this Point point, LineSegment L)
        {
            return point.SnapToLineSegmentOptimized(L.P1, L.P2);
        }

        //public static Point Snap(this Point point, GENLine genLine, SnapSettings snapSettings = null)
        //{
        //    throw new NotImplementedException();
        //}

        /// <summary>
        /// Check if the specified point is present in this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool HasSimilarPoint(this List<Point> pointList, Point point)
        {
            return pointList.IndexOfSimilarPoint(point) >= 0;
        }

        /// <summary>
        /// Retrieves the index of the first similar point in the list of points of this feature
        /// </summary>
        /// <param name="point"></param>
        /// <returns>-1 if not found</returns>
        public static int IndexOfSimilarPoint(this List<Point> pointList, Point point)
        {
            for (int i = 0; i < pointList.Count; i++)
            {
                if (pointList[i].IsSimilarTo(point))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
