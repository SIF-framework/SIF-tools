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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GIS
{
    /// <summary>
    /// For calculating convex hull around points, using Graham scan algorithm
    /// See: https://en.wikipedia.org/wiki/Graham_scan
    /// </summary>
    public static class ConvexHull
    {
        // Using Graham scan algorithm, converted to C# from C++ version at: https://www.geeksforgeeks.org/convex-hull-set-2-graham-scan/

        // A global point needed for sorting points with reference to the first point Used in compare function of qsort() 
        private static Point p0;

        /// <summary>
        /// Calculates convex hull around specified points
        /// </summary>
        /// <param name="pointList"></param>
        public static List<Point> RetrieveConvexHull(List<Point> pointList)
        {
            int n = pointList.Count;
            Point[] points = pointList.ToArray();
            // Find the bottommost point 
            double ymin = points[0].Y;
            int min = 0;
            for (int i = 1; i < n; i++)
            {
                double y = points[i].Y;

                // Pick the bottom-most or chose the left 
                // most point in case of tie 
                if ((y < ymin) || (ymin.Equals(y) && points[i].X < points[min].X))
                {
                    ymin = points[i].Y;
                    min = i;
                }
            }

            // Place the bottom-most point at first position 
            Swap(ref points[0], ref points[min]);

            // Sort n-1 points with respect to the first point. 
            // A point p1 comes before p2 in sorted output if p2 
            // has larger polar angle (in counterclockwise 
            // direction) than p1 
            p0 = points[0];
            QuickSort(points, 1, n - 1);

            // If two or more points make same angle with p0, 
            // Remove all but the one that is farthest from p0 
            // Remember that, in above sorting, our criteria was 
            // to keep the farthest point at the end when more than 
            // one points have same angle. 
            int m = 1; // Initialize size of modified array 
            for (int i = 1; i < n; i++)
            {
                // Keep removing i while angle of i and i+1 is same 
                // with respect to p0 
                while (i < (n - 1) && Orientation(p0, points[i], points[i + 1]) == 0)
                {
                    i++;
                }

                points[m] = points[i];
                m++;  // Update size of modified array 
            }

            // If modified array of points has less than 3 points, convex hull is not possible 
            if (m < 3)
            {
                return null;
            }

            // Create an empty stack and push first three points 
            // to it. 
            Stack<Point> S = new Stack<Point>();
            S.Push(points[0]);
            S.Push(points[1]);
            S.Push(points[2]);

            // Process remaining n-3 points 
            for (int i = 3; i < m; i++)
            {
                // Keep removing top while the angle formed by points next-to-top, top, and points[i] makes a non-left turn 
                while (Orientation(NextToTop(S), S.Peek(), points[i]) != 2)
                {
                    S.Pop();
                }
                S.Push(points[i]);
            }

            // Now stack has the output points, print contents of stack 
            //while (S.Count > 0) 
            //{ 
            //    Point p = S.Peek(); 
            //     System.Console.Write("(" + p.X + ", " + p.Y + ")"; 
            //    S.Pop(); 
            //}

            return S.ToList();
        }

        /// <summary>
        /// Quicksort, see: https://en.wikipedia.org/wiki/Quicksort
        /// from: http://csharpexamples.com/c-quick-sort-algorithm-implementation/
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private static void QuickSort(Point[] arr, int start, int end)
        {
            int i;
            if (start < end)
            {
                i = Partition(arr, start, end);

                QuickSort(arr, start, i - 1);
                QuickSort(arr, i + 1, end);
            }
        }

        private static int Partition(Point[] arr, int start, int end)
        {
            Point temp;
            Point p = arr[end];
            int i = start - 1;

            for (int j = start; j <= end - 1; j++)
            {
                // if (arr[j] <= p)
                if (Compare(arr[j], p) <= 0)
                {
                    i++;
                    temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            temp = arr[i + 1];
            arr[i + 1] = arr[end];
            arr[end] = temp;
            return i + 1;
        }

        /// <summary>
        /// A utility function to find next to top in a stack 
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        private static Point NextToTop(Stack<Point> S)
        {
            Point p = S.Pop();
            Point res = S.Peek();
            S.Push(p);
            return res;
        }

        /// <summary>
        /// A utility function to swap two points 
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        private static void Swap(ref Point p1, ref Point p2)
        {
            Point temp = p1;
            p1 = p2;
            p2 = temp;
        }

        /// <summary>
        /// A utility function to return square of distance between p1 and p2 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private static double DistSq(Point p1, Point p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int Orientation(Point p, Point q, Point r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) return 0;  // colinear 
            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }

        // The function used by QuickSort() to sort an array of points with respect to the first point 
        private static int Compare(Point p1, Point p2)
        {
            // Find orientation 
            int o = Orientation(p0, p1, p2);
            if (o == 0)
                return (DistSq(p0, p2) >= DistSq(p0, p1)) ? -1 : 1;

            return (o == 2) ? -1 : 1;
        }
    }
}
