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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class for calculation of concave hull around IPF-points, using the 'k-nearest neighbours'-algorithm of Moreira and Santos
    /// See: http://repositorium.sdum.uminho.pt/bitstream/1822/6429/1/ConcaveHull_ACM_MYS.pdf
    /// CONCAVE HULL: A K-NEAREST NEIGHBOURS APPROACH FOR THE COMPUTATION OF THE REGION OCCUPIED BY A SET OF POINTS
    /// Adriano Moreira and Maribel Yasmina Santos, 2007,
    /// Department of Information Systems, University of Minho, Portugal
    /// GRAPP 2007 (International Conference on Computer Graphics Theory and Applications), pp 61-68
    /// </summary>
    public class ConcaveHull
    {
        private static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// Retrieve concave hull, starting with k nearest neighbours
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="k">number of neighbours</param>
        /// <param name="xyDecimalCount">defines number of decimals used for identifying points during removal of duplicates</param>
        /// <param name="log"></param>
        /// <param name="isDebugMode">if true, write intermediate files</param>
        /// <returns></returns>
        public static GENFile RetrieveConcaveHull(IPFFile ipfFile, int k = 3, int xyDecimalCount = 0, Log log = null, bool isDebugMode = false)
        {
            GENFile genFile = new GENFile();
            List<IPFPoint> ipfPointsList = ipfFile.Points;
            List<Vertex> vertexList = ConvertIPFPointsToVertices(ipfFile.Points);

            // Clean first, may have lots of duplicates
            List<Vertex> cleanedVertexList = RemoveDuplicates(vertexList, xyDecimalCount);
            if ((vertexList.Count - cleanedVertexList.Count) > 0)
            {
                log.AddInfo("Removed " + (vertexList.Count - cleanedVertexList.Count) + " duplicate points", 1);
            }

            if (k >= vertexList.Count)
            {
                throw new Exception("Value k (" + k + ") for concave hull method cannot be larger than number of points (" + vertexList.Count + ")");
            }

            while (k < vertexList.Count)
            {
                List<Vertex> concaveHull = RetrieveConcaveHull(cleanedVertexList.ToList(), k, xyDecimalCount, log, isDebugMode);
                if (concaveHull != null)
                {
                    return CreateGEN(concaveHull);
                }
                else
                {
                    k++;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve concave hull, starting with k nearest neighbours
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="k">number of neighbours</param>
        /// <param name="xyDecimalCount">defines number of decimals used for identifying points during removal of duplicates</param>
        /// <param name="log"></param>
        /// <param name="isDebugMode">if true, write intermediate files</param>
        /// <returns></returns>
        public static GENFile RetrieveConcaveHull(List<Point> pointList, int k = 3, int xyDecimalCount = 0, Log log = null, bool isDebugMode = false)
        {
            GENFile genFile = new GENFile();

            List<Vertex> vertexList = ConvertIPFPointsToVertices(pointList);

            // Clean first, may have lots of duplicates
            List<Vertex> cleanedVertexList = RemoveDuplicates(vertexList, xyDecimalCount);
            if ((vertexList.Count - cleanedVertexList.Count) > 0)
            {
                log.AddInfo("Removed " + (vertexList.Count - cleanedVertexList.Count) + " duplicate points", 1);
            }

            while (k < vertexList.Count)
            {
                List<Vertex> concaveHull = RetrieveConcaveHull(cleanedVertexList.ToList(), k, xyDecimalCount, log, isDebugMode);
                if (concaveHull != null)
                {
                    return CreateGEN(concaveHull);
                }
                else
                {
                    k++;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve concave hull, starting with k nearest neighbours
        /// </summary>
        /// <param name="vertexList"></param>
        /// <param name="k">number of points in initital hull</param>
        /// <param name="xyDecimalCount"></param>
        /// <param name="log"></param>
        /// <param name="isDebugMode">Write intermediate files if true</param>
        /// <returns></returns>
        internal static List<Vertex> RetrieveConcaveHull(List<Vertex> vertexList, int k = 3, int xyDecimalCount = 0, Log log = null, bool isDebugMode = false)
        {
            // Use fast datastructure for searching, removing
            HashSet<Vertex> vertexHashSet = new HashSet<Vertex>(vertexList);

            int step = -1;
            int i = -1;
            int j = -1;
            try
            {
                if (log != null)
                {
                    log.AddInfo("Retrieving concave hull for " + vertexList.Count + " points, with " + k + " neighbours", 1);
                }

                // make sure k>=3 
                if (k < 3)
                {
                    throw new ArgumentException("k is required to be 3 or more", "k");
                }

                // A minimum of 3 dissimilar points is required
                if (vertexList.Count < 3)
                {
                    throw new ArgumentException("At least 3 dissimilar points reqired", "points");
                }
                if (vertexList.Count == 3)
                {
                    // For a 3 points dataset, the polygon is the dataset itself 
                    // This is the hull, its already as small as it can be.
                    return vertexList;
                }

                // make sure that k neighbours can be found 
                if (vertexList.Count < k)
                {
                    throw new Exception("No hull found, resulting k is/became higher than the amount of dissimilar points");
                }

                // initialize the hull with the first point
                List<Vertex> hull = new List<Vertex>();
                Vertex firstPoint = FindMinY(vertexList);
                hull.Add(firstPoint);
                if (isDebugMode)
                {
                    WriteIPF(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "MinPoint.IPF"), log);
                }

                Vertex currentPoint = firstPoint;
                // Until the hull is of size > 3 we want to ignore the first point from nearest neighbour searches, so for now remove first point
                vertexHashSet.Remove(firstPoint);

                double previousAngle = 0;
                step = 1;

                while ((!currentPoint.Equals(firstPoint) || (step == 1)) && (hull.Count != vertexHashSet.Count))
                {
                    if (step == 4)
                    {
                        // add the firstPoint again
                        vertexHashSet.Add(firstPoint);
                    }

                    // find the nearest neighbours
                    List<Vertex> kNearestPoints = GetNearestPointList(vertexHashSet, currentPoint, k);
                    // sort the candidates (neighbours) in descending order of right-hand turn 
                    List<Vertex> candidatePoints = SortByAngle(kNearestPoints, currentPoint, previousAngle);
                    if (isDebugMode)
                    {
                        WriteIPF(kNearestPoints, Path.Combine(Path.GetDirectoryName(log.Filename), "Candidates.IPF"), log);
                    }

                    bool its = true;
                    i = 0;

                    while (its && i < candidatePoints.Count)
                    {
                        int lastPoint = 0;
                        if (candidatePoints[i].Equals(firstPoint))
                        {
                            lastPoint = 1;
                        }

                        j = 2;
                        its = false;

                        while (!its && (j < (hull.Count - lastPoint)))
                        {
                            //if (isDebugMode)
                            //{
                            //    GENFile g = new GENFile();
                            //    GENFeature f1 = new GENLine(g, "1");
                            //    f1.Points.Add(new DoublePoint(hull[step - 1].X, hull[step - 1].Y));
                            //    f1.Points.Add(new DoublePoint(candidatePoints[i].X, candidatePoints[i].Y));
                            //    GENFeature f2 = new GENLine(g, "2");
                            //    f2.Points.Add(new DoublePoint(hull[step - 1 - j].X, hull[step - 1 - j].Y));
                            //    f2.Points.Add(new DoublePoint(hull[step - j].X, hull[step - j].Y));
                            //    g.AddFeature(f1);
                            //    g.AddFeature(f2);
                            //    string gFilename = Path.Combine(Path.GetDirectoryName(log.Filename), "LijnenTmp.GEN"); // REF1_BAS_FW_TOPL3-BOTL12_CHULLBUFFER_EPsel.IPF";
                            //    g.WriteFile(gFilename);
                            //    // WriteGENLine(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "HullTmp.GEN"), log);
                            //}
                            its = IsIntersecting(hull[step - 1], candidatePoints[i], hull[step - j - 1], hull[step - j]);
                            j++;
                        }

                        if (its)
                        {
                            i++;
                        }
                    }

                    if (its)
                    {
                        // since all candidates intersect at least one edge, try again with a higher number of neighbours 
                        if (isDebugMode)
                        {
                            if (log == null)
                            {
                                throw new Exception("RetrieveConcaveHull(): log variable cannot be null for debug mode");
                            }

                            if (log.Filename == null)
                            {
                                throw new Exception("RetrieveConcaveHull(): log.Filename cannot be null for debug mode");
                            }
                            WriteGENLine(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "TmpConcaveHullIntersection.GEN"), log);
                        }

                        if (log != null)
                        {
                            log.AddInfo("Self-intersection found, after inspecting " + step + " points. Restart with extra neighbour.", 2);
                        }
                        return null;
                    }

                    // a valid candidate was found
                    currentPoint = candidatePoints[i];
                    hull.Add(currentPoint);
                    if (isDebugMode)
                    {
                        WriteGENLine(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "TmpConvexHull.GEN"), log);
                        WriteIPF(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "TmpConvexHull.IPF"), log);
                    }

                    previousAngle = Angle(hull[step], hull[step - 1]);
                    vertexHashSet.Remove(currentPoint);

                    step++;
                }

                // The original points less the points belonging to the hull need to be fully enclosed by the hull in order to return true.
                // Current hull has no self-intersections, check if all the given points are inside the computed polygon 
                bool allEnclosed = IsInsidePolygon(vertexList, hull, step, log, isDebugMode);
                if (log != null)
                {
                    if (!allEnclosed)
                    {
                        log.AddInfo("Could not create concave hull! Not all vertices are inside resulting hull. Restart with extra neighbour.", 2);
                    }
                    else
                    {
                        log.AddInfo("Successfully created concave hull for " + vertexList.Count + " points, with " + k + " neighbours", 2);
                    }
                }
                return (allEnclosed) ? hull : null;

            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error during concave hull: " + ex.GetBaseException().Message, ex); // at step, i, j, k: " + step + "," + i + "," + j + "," + k + ": " + ex.GetBaseException().Message, ex);
            }
        }

        /// <summary>
        /// Checks if specified vertices are inside (or exactly at hull edges) of specified hull/polygon or outside
        /// </summary>
        /// <param name="vertexList"></param>
        /// <param name="hull"></param>
        /// <param name="step"></param>
        /// <param name="log"></param>
        /// <param name="isDebugMode"></param>
        /// <returns>true if vertices are inside hull or at hull edge, false otherwise</returns>
        private static bool IsInsidePolygon(List<Vertex> vertexList, List<Vertex> hull, int step, Log log, bool isDebugMode = false)
        {
            List<Point> hullPoints = ConvertVerticesToPoints(hull);
            Extent boundingBox = new Extent(hullPoints);
            bool allInside = true;
            List<Vertex> verticesOutside = new List<Vertex>();
            foreach (Vertex vertex in vertexList)
            {
                // Skip points that are part of the currently proposed hull
                if (!hull.Contains(vertex))
                {
                    Point point = new DoublePoint(vertex.X, vertex.Y);
                    if (boundingBox.Contains(point.X, point.Y))
                    {
                        if (!point.IsInside(hullPoints))
                        {
                            allInside = false;
                            verticesOutside.Add(vertex);
                            break;
                        }
                    }
                    else
                    {
                        allInside = false;
                        verticesOutside.Add(vertex);
                        break;
                    }
                }
            }

            if (!allInside)
            {
                // since at least one point is out of the computed polygon, try again with a higher number of neighbours 
                if (log != null)
                {
                    log.AddInfo("Point outside current hull with " + step + " points. Restart with extra neighbour.", 2);
                }
                if (isDebugMode)
                {
                    if ((log != null) && (log.Filename == null))
                    {
                        throw new Exception("log.Filename cannot be null for debug mode");
                    }
                    log.AddInfo("Debug mode: Writing IPF-/GEN-features to " + Path.GetDirectoryName(log.Filename), 3);
                    WriteIPF(verticesOutside, Path.Combine(Path.GetDirectoryName(log.Filename), "TmpPointsOutside.IPF"), log);
                    WriteGENLine(hull, Path.Combine(Path.GetDirectoryName(log.Filename), "TmpConcaveHull.GEN"), log);
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        private static List<Vertex> ConvertIPFPointsToVertices(List<IPFPoint> list)
        {
            List<Vertex> vertices = new List<Vertex>();
            for (int idx = 0; idx < list.Count; idx++)
            {
                IPFPoint point = list[idx];
                vertices.Add(new Vertex(point.X, point.Y));
            }
            return vertices;
        }

        private static bool IsIntersecting(Vertex v1, Vertex v2, Vertex v3, Vertex v4)
        {
            // from: https://www.topcoder.com/community/data-science/data-science-tutorials/geometry-concepts-line-intersection-and-its-applications/
            // and: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection

            // Transform points in line with a form ax + by = c
            double a1 = v2.Y - v1.Y;
            double b1 = v1.X - v2.X;
            double c1 = a1 * v1.X + b1 * v1.Y;
            double a2 = v4.Y - v3.Y;
            double b2 = v3.X - v4.X;
            double c2 = a2 * v3.X + b2 * v3.Y;

            double det = a1 * b2 - a2 * b1;
            if (det == 0)
            {
                // lines are parallel
                return false;
            }
            else
            {
                // Vertex intersection = new Vertex((b2 * c1 - b1 * c2) / det, (a1 * c2 - a2 * c1) / det);
                double x = (b2 * c1 - b1 * c2) / det;
                double y = (a1 * c2 - a2 * c1) / det;

                bool on_both = true;
                on_both = on_both && (Math.Min(v1.X, v2.X) <= x) && (x <= Math.Max(v1.X, v2.X));
                on_both = on_both && (Math.Min(v1.Y, v2.Y) <= y) && (y <= Math.Max(v1.Y, v2.Y));
                on_both = on_both && (Math.Min(v3.X, v4.X) <= x) && (x <= Math.Max(v3.X, v4.X));
                on_both = on_both && (Math.Min(v3.Y, v4.Y) <= y) && (y <= Math.Max(v3.Y, v4.Y));
                return on_both;

                //// Check for a real intersection or a one touching endpoint
                //int endPointConnectionCount = ((intersection.Equals(v1) ? 1 : 0) + (intersection.Equals(v2) ? 1 : 0) + (intersection.Equals(v3) ? 1 : 0) + (intersection.Equals(v4) ? 1 : 0));
                //return (endPointConnectionCount <= 1) && HasIntersectingBoundingbox(v1, v2, v3, v4);
            }
        }

        /// <summary>
        /// Check if bounding boxes do intersect. If one bounding box touches the other, they do not intersect.
        /// </summary>
        /// <param name="p1">upper left point in first bounding box</param>
        /// <param name="p2">lower rght point in first bounding box</param>
        /// <param name="p3">upper left point in second bounding box</param>
        /// <param name="p4">lower rght point in second bounding box</param>
        /// <returns>if they intersect, false otherwise</returns>
        private static bool HasIntersectingBoundingbox(Vertex p1, Vertex p2, Vertex p3, Vertex p4)
        {
            Vertex minA = new Vertex(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
            Vertex maxA = new Vertex(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
            Vertex minB = new Vertex(Math.Min(p3.X, p4.X), Math.Min(p3.Y, p4.Y));
            Vertex maxB = new Vertex(Math.Max(p3.X, p4.X), Math.Max(p3.Y, p4.Y));

            return (minA.X <= maxB.X) && (maxA.X >= minB.X) && (minA.Y <= maxB.Y) && (maxA.Y >= minB.Y);
        }

        private static List<Vertex> GetNearestPointList(HashSet<Vertex> vertices, Vertex v, int k)
        {
            List<VertexDistance> sortedVertices = new List<VertexDistance>();
            foreach (Vertex v1 in vertices)
            {
                sortedVertices.Add(new VertexDistance(v1, EuclideanDistance(v, v1)));
            }
            sortedVertices.Sort();

            List<Vertex> nearestPoints = new List<Vertex>();
            for (int i = 0; (i < k) && (i < sortedVertices.Count); i++)
            {
                nearestPoints.Add(sortedVertices[i].Vertex);
            }
            return nearestPoints;
        }

        private static double EuclideanDistance(Vertex v1, Vertex v2)
        {
            return Math.Sqrt(Math.Pow((v1.X - v2.X), 2) + Math.Pow((v1.Y - v2.Y), 2));
        }

        private static void WriteGENLine(HashSet<Vertex> vertexList, string filename, Log log = null)
        {
            WriteGENLine(vertexList.ToList(), filename, log);
        }

        private static void WriteGENLine(List<Vertex> vertexList, string filename, Log log = null)
        {
            GENFile genFile = new GENFile();
            List<Point> pointList = ConvertVerticesToPoints(vertexList);
            GENLine genLine = new GENLine(genFile, 1, pointList);
            genFile.AddFeature(genLine);
            try
            {
                genFile.WriteFile(filename);
            }
            catch (Exception)
            {
                // ignore
                if (log != null)
                {
                    log.AddWarning("Could not write GEN-file: " + Path.GetFileName(filename));
                }
            }
        }

        private static void WriteIPF(HashSet<Vertex> vertices, string ipfFilename, Log log = null)
        {
            WriteIPF(vertices.ToList(), ipfFilename, log);
        }

        private static void WriteIPF(List<Vertex> vertices, string ipfFilename, Log log = null)
        {
            IPFFile ipfFile = new IPFFile();
            ipfFile.AddColumn("Idx");

            int count = vertices.Count;
            if ((vertices[0] == vertices[count - 1]) && (count > 1))
            {
                count--;
            }

            for (int idx = 0; idx < count; idx++)
            {
                Vertex v = vertices[idx];
                ipfFile.Points.Add(new IPFPoint(ipfFile, new FloatPoint((float)v.X, (float)v.Y), new string[] { v.X.ToString(englishCultureInfo), v.Y.ToString(englishCultureInfo), (idx + 1).ToString() }.ToArray()));
            }
            try
            {
                ipfFile.WriteFile(ipfFilename);
            }
            catch (Exception)
            {
                // ignore
                if (log != null)
                {
                    log.AddWarning("Could not write IPF-file: " + Path.GetFileName(ipfFilename));
                }
            }
        }

        private static GENFile CreateGEN(List<Vertex> vertices)
        {
            GENFile genFile = new GENFile();
            GENPolygon genPolygon = new GENPolygon(genFile, "1");
            foreach (Vertex vertex in vertices)
            {
                genPolygon.Points.Add(new DoublePoint(vertex.X, vertex.Y));
            }
            genFile.AddFeature(genPolygon);

            return genFile;
        }

        private static List<Vertex> ConvertIPFPointsToVertices(List<Point> list)
        {
            List<Vertex> vertices = new List<Vertex>();
            for (int idx = 0; idx < list.Count; idx++)
            {
                Point point = list[idx];
                vertices.Add(new Vertex(point.X, point.Y));
            }
            return vertices;
        }

        private static List<Point> ConvertVerticesToPoints(List<Vertex> vertices)
        {
            List<Point> pointList = new List<Point>();
            for (int idx = 0; idx < vertices.Count; idx++)
            {
                Vertex v = vertices[idx];
                pointList.Add(new DoublePoint(v.X, v.Y));
            }
            return pointList;
        }

        private static List<Vertex> RemoveDuplicates(List<Vertex> vertices, int decimalCount = 0)
        {
            int decimalCountPow = (int)Math.Pow(10, decimalCount);
            int xFactor = 1000000 * decimalCountPow;

            List<Vertex> cleanedVertices = new List<Vertex>();
            HashSet<long> cleanXYSet = new HashSet<long>();
            foreach (Vertex v in vertices)
            {
                long xy = (long)(v.X * xFactor) + (long)(v.Y * decimalCountPow);
                if (!cleanXYSet.Contains(xy))
                {
                    cleanXYSet.Add(xy);
                    cleanedVertices.Add(v);
                }
            }
            return cleanedVertices;
        }

        private static Vertex FindMinY(List<Vertex> vertices)
        {
            double minYPointY = double.MaxValue;
            Vertex minYVertex = null;
            foreach (Vertex vertex in vertices)
            {
                if (vertex.Y < minYPointY)
                {
                    minYVertex = vertex;
                    minYPointY = vertex.Y;
                }
            }
            return minYVertex;
        }

        private static double Angle(Vertex v1, Vertex v2)
        {
            // Atan2: an angle, θ, measured in radians, such that -π≤θ≤π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian plane. Observe the following: 
            // For (x, y) in quadrant 1, 0 < θ < π/2. 
            // For (x, y) in quadrant 2, π/2 < θ≤π. 
            // For (x, y) in quadrant 3, -π < θ < -π/2. 
            // For (x, y) in quadrant 4, -π/2 < θ < 0. 
            // For points on the boundaries of the quadrants, the return value is the following: 
            // If y is 0 and x is not negative, θ = 0. 
            // If y is 0 and x is negative, θ = π. 
            // If y is positive and x is 0, θ = π/2. 
            // If y is negative and x is 0, θ = -π/2. 
            // If y is 0 and x is 0, θ = 0. 

            double angle = Math.Atan2(v2.Y - v1.Y, v2.X - v1.X);

            // Normalize angle: return angle in range: 0 <= angle < 2PI
            if (angle < 0.0)
                return angle + Math.PI + Math.PI;
            else
                return angle;
        }

        /// <summary>
        /// Will give the angle from v1 to v2, in radians between -pi and +pi. Do not mix degrees and radians. 
        /// Suggestion is to always use radians, and only convert to degrees if necessary for human-readable output.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="v"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static List<Vertex> SortByAngle(List<Vertex> vertices, Vertex v, double angle)
        {
            // Uses List.Sort to sort the vertices from greatest to least angle difference between the vertices point and itself, and angle. 
            // The order of v1 and v2 are swapped in the input tuple to sort descending, that is, greatest difference first. 
            List<Vertex> sortedVertices = new List<Vertex>(vertices);
            sortedVertices.Sort((v1, v2) => (AngleDifference(angle, Angle(v, v2))).CompareTo(AngleDifference(angle, Angle(v, v1))));

            return sortedVertices;
        }

        private static double AngleDifference(double a, double b)
        {
            double diff = b - a;
            if (diff < 0)
            {
                diff += Math.PI * 2;
            }

            return diff;
        }
    }

    /// <summary>
    /// Class for storage of Vertices
    /// </summary>
    internal class Vertex : IEquatable<Vertex>, IComparer<Vertex>, IEqualityComparer<Vertex>
    {
        /// <summary>
        /// Used for matching points
        /// </summary>
        protected static double tolerance = 0.0001;

        public double X = 0;
        public double Y = 0;
        public Vertex() { }
        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "(" + X.ToString() + "," + Y.ToString() + ")";
        }

        public static int CompareY(Vertex a, Vertex b)
        {
            if (a.Y < b.Y)
                return -1;
            if (a.Y == b.Y)
                return 0;
            return 1;
        }

        public static int CompareX(Vertex a, Vertex b)
        {
            if (a.X < b.X)
                return -1;
            if (a.X == b.X)
                return 0;
            return 1;
        }

        public double Distance(Vertex b)
        {
            double dX = b.X - this.X;
            double dY = b.Y - this.Y;
            return Math.Sqrt((dX * dX) + (dY * dY));
        }

        public double Slope(Vertex b)
        {
            double dX = b.X - this.X;
            double dY = b.Y - this.Y;
            return dY / dX;
        }

        public static int Compare(Vertex u, Vertex a, Vertex b)
        {
            if (a.X == b.X && a.Y == b.Y) return 0;

            Vertex upper = new Vertex();
            Vertex p1 = new Vertex();
            Vertex p2 = new Vertex();
            upper.X = (u.X + 180) * 360;
            upper.Y = (u.Y + 90) * 180;
            p1.X = (a.X + 180) * 360;
            p1.Y = (a.Y + 90) * 180;
            p2.X = (b.X + 180) * 360;
            p2.Y = (b.Y + 90) * 180;
            if (p1 == upper) return -1;
            if (p2 == upper) return 1;

            double m1 = upper.Slope(p1);
            double m2 = upper.Slope(p2);

            if (m1 == m2)
            {
                return p1.Distance(upper) < p2.Distance(upper) ? -1 : 1;
            }

            if (m1 <= 0 && m2 > 0) return -1;

            if (m1 > 0 && m2 <= 0) return -1;

            return m1 > m2 ? -1 : 1;
        }

        public bool Equals(Vertex other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public bool Equals(Vertex v1, Vertex v2)
        {
            return (v1.X.Equals(v2.X) && v1.Y.Equals(v2.Y));
        }

        public int GetHashCode(Vertex obj)
        {
            return ((int)((obj.X * 100000) + obj.Y));
        }

        public int Compare(Vertex v1, Vertex v2)
        {
            if (v1 == null)
            {
                return -1;
            }
            else if (v2 == null)
            {
                return 1;
            }
            else
            {
                // Compare first to X, and if equal to Y-coordinate
                if (v1.Equals(v2))
                {
                    return 0;
                }
                else if ((Math.Abs(v1.X - v2.X) > tolerance))
                {
                    return v1.X.CompareTo(v2.X);
                }
                else if ((Math.Abs(v1.Y - v2.Y) > tolerance))
                {
                    return v1.Y.CompareTo(v2.Y);
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    internal class VertexDistance : IEqualityComparer<VertexDistance>, IComparer<VertexDistance>, IComparable<VertexDistance>
    {
        public Vertex Vertex;
        public double Distance;

        public VertexDistance(Vertex v, double distance)
        {
            this.Vertex = v;
            this.Distance = distance;
        }

        public bool Equals(VertexDistance x, VertexDistance y)
        {
            return x.Distance.Equals(y.Distance);
        }

        public int GetHashCode(VertexDistance obj)
        {
            return obj.Distance.GetHashCode();
        }

        public int Compare(VertexDistance x, VertexDistance y)
        {
            return x.Distance.CompareTo(y.Distance);
        }

        public int CompareTo(VertexDistance other)
        {
            return this.Distance.CompareTo(other.Distance);
        }
    }

    internal class Edge
    {
        public Vertex A = new Vertex(0, 0);
        public Vertex B = new Vertex(0, 0);
        public Edge() { }
        public Edge(Vertex a, Vertex b)
        {
            A = a;
            B = b;
        }
        public Edge(double ax, double ay, double bx, double by)
        {
            A = new Vertex(ax, ay);
            B = new Vertex(bx, by);
        }
    }
}
