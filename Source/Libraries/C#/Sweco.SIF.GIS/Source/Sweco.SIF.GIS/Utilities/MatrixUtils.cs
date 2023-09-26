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

namespace Sweco.SIF.GIS.Utilities
{
    /// <summary>
    /// Class for manipulation and/or analysis of matrices
    /// </summary>
    public class MatrixUtils
    {
        /// <summary>
        /// Copies specified matrix and replaces specified value by 1 and all other values by 0.
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int[][] Initialize01Matrix(int[][] matrix, int value)
        {
            int R = matrix.Length;
            int C = matrix[0].Length;

            int[][] A = new int[R][];
            for (int rowIdx = 0; rowIdx < R; rowIdx++)
            {
                A[rowIdx] = new int[C];
                for (int colIdx = 0; colIdx < C; colIdx++)
                {
                    A[rowIdx][colIdx] = (matrix[rowIdx][colIdx].Equals(value)) ? 1 : 0;
                }
            }

            return A;
        }

        /// <summary>
        /// Finds largest rectangle (submatrix) with all 1s in specified matrix. Llower left cell coordinates, width and height of the submatrix are returned.
        /// Algorithm from: https://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s/
        /// Author: Sanjiv Kumar
        /// Adapted to retrieve width, height and indices of lower left cell
        /// Only one traversal of the matrix is required, so the time complexity is O(R X C)
        /// Stack is required to store the columns, so space complexity is O(C)
        /// </summary>
        /// <param name="matrix">matrix with values 1 and 0; note: values will be modified by algorithm</param>
        /// <param name="llRow"></param>
        /// <param name="llCol"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        public static void FindMaxRectangle(int[][] matrix, out int llRow, out int llCol, out int maxWidth, out int maxHeight)
        {
            // Alorithm: Maximum size rectangle binary sub-matrix with all 1s
            // Difficulty Level : Hard; Last Updated : 30 Jun, 2022
            // Time Complexity: O(R x C), since only one traversal of the matrix is required.
            // -----------------------------------------------------------------
            // Description from: https://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s
            // If the height of bars of the histogram is given then the largest area of the histogram can be found. This way in each row, 
            // the largest area of bars of the histogram can be found. To get the largest rectangle full of 1’s, update the next row 
            // with the previous row and find the largest area under the histogram, i.e.consider each 1’s as filled squares and 0’s 
            // with an empty square and consider each row as the base.
            // Algorithm: 
            // 1. Run a loop to traverse through the rows.
            // 2. Now If the current row is not the first row then update the row as follows, 
            //    if matrix[i][j] is not zero then matrix[i][j] = matrix[i - 1][j] + matrix[i][j].
            // 3. Find the maximum rectangular area under the histogram, consider the ith row as heights of bars of a histogram.
            //    This can be calculated as given in this article Largest Rectangular Area in a Histogram (algorithm II below)
            //    https://www.geeksforgeeks.org/largest-rectangle-under-histogram 
            // 4. Do the previous two steps for all rows and print the maximum area of all the rows.

            // copy matrix and change value to 1 and all other values to 0;
            int R = matrix.Length;
            int C = matrix[0].Length;

            llRow = -1;
            llCol = -1;
            maxWidth = -1;
            maxHeight = -1;
            int width = -1;
            int height = -1;
            int col = -1;

            // Calculate area for first row and initialize it as result
            int result = MaxHistogramArea(matrix[0], out width, out height, out col);

            // iterate over row to find maximum rectangular area considering each row as histogram
            for (int i = 1; i < R; i++)
            {
                for (int j = 0; j < C; j++)
                {
                    // if A[i][j] is 1 then add A[i -1][j]
                    if (matrix[i][j] == 1)
                    {
                        matrix[i][j] += matrix[i - 1][j];
                    }
                }

                // PrintMatrix(A);

                // Update result if area with current row (as last row of rectangle) is more
                int tmpResult = MaxHistogramArea(matrix[i], out width, out height, out col);
                if (tmpResult > result)
                {
                    result = tmpResult;
                    maxWidth = width;
                    maxHeight = height;
                    llRow = i;
                    llCol = col;
                }
            }
        }

        /// <summary>
        // Finds the maximum area under the histogram represented by histogram.
        // For details: https://www.geeksforgeeks.org/largest-rectangle-under-histogram/
        /// </summary>
        /// <param name="row"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <param name="leftCol">left column index (zero based) of rectangle</param>
        /// <returns></returns>
        public static int MaxHistogramArea(int[] histogram, out int maxWidth, out int maxHeight, out int leftCol)
        {
            // Algorithm: Largest Rectangular Area in a Histogram
            // Difficulty Level : Hard; Last Updated : 22 Aug, 2022
            // Time Complexity: O(n), since every bar is pushed and popped only once
            // ------------------------------------------------------
            // Description from: https://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s/
            // Assumption for simplicity is that width of all bars is 1. For every bar ‘x’, we calculate the area with ‘x’ as the smallest bar in the rectangle. 
            // If we calculate such area for every bar ‘x’ and find the maximum of all areas, our task is done. How to calculate area with ‘x’ as smallest bar? 
            // We need to know index of the first smaller (smaller than ‘x’) bar on left of ‘x’ and index of first smaller bar on right of ‘x’. 
            // Let us call these indexes as ‘left index’ and ‘right index’ respectively. We traverse all bars from left to right, maintain a stack of bars.
            // Every bar is pushed to stack once. A bar is popped from stack when a bar of smaller height is seen.When a bar is popped, we calculate the area 
            // with the popped bar as smallest bar.How do we get left and right indexes of the popped bar – the current index tells us the ‘right index’ and 
            // index of previous item in stack is the ‘left index’. Following is the complete algorithm.
            // 1) Create an empty stack.
            // 2) Start from first bar, and do following for every bar ‘hist[i]’ where ‘i’ varies from 0 to n - 1.
            //    a) If stack is empty or hist[i] is higher than the bar at top of stack, then push ‘i’ to stack. 
            //    b) If this bar is smaller than the top of stack, then keep removing the top of stack while top of the stack is greater.
            //       Let the removed bar be hist[tp]. Calculate area of rectangle with hist[tp] as smallest bar.
            //       For hist[tp], the ‘left index’ is previous (previous to tp) item in stack and ‘right index’ is ‘i’ (current index).
            // 3) If the stack is not empty, then one by one remove all bars from stack and do step 2.b for every removed bar.


            int C = histogram.Length;

            // Create an empty stack. The stack holds indexes of hist[] array.
            // The bars stored in stack are always in increasing order of their heights.
            Stack<int> result = new Stack<int>();

            int max_area = 0;   // Initialize max area in current row (or histogram)
            int area = 0;       // Initialize area with current top
            int height = -1;    // Top of stack
            int width = -1;
            int col = -1;
            maxWidth = -1;
            maxHeight = -1;
            leftCol = -1;

            // Run through all bars of given histogram (or row)
            int i = 0;
            while (i < C)
            {
                // If this bar is higher than the bar on top stack, push it to stack
                if (result.Count == 0 || histogram[result.Peek()] <= histogram[i])
                {
                    result.Push(i++);
                }
                else
                {
                    // If this bar is lower than top of stack, then calculate area of rectangle with stack top as the smallest 
                    // (or minimum height) bar. 'i' is 'right index' for the top and element before top in stack is 'left index'
                    height = histogram[result.Pop()]; // height = top value
                    if (result.Count == 0)
                    {
                        col = 0;
                        width = i;
                    }
                    else
                    {
                        col = result.Peek() + 1;
                        width = i - result.Peek() - 1;
                    }
                    area = width * height;

                    if (area > max_area)
                    {
                        max_area = area;
                        maxWidth = width;
                        maxHeight = height;
                        leftCol = col;
                    }
                }
            }

            // Now pop the remaining bars from stack and calculate area with every popped bar as the smallest bar
            while (result.Count > 0)
            {
                col = result.Pop();
                height = histogram[col];
                if (result.Count == 0)
                {
                    width = i;
                }
                else
                {
                    width = (i - result.Peek() - 1);
                }
                area = width * height;

                if (area > max_area)
                {
                    max_area = area;
                    maxWidth = width;
                    maxHeight = height;
                    leftCol = col;
                }
            }

            return max_area;
        }

        /// <summary>
        /// Write matrix to console
        /// </summary>
        /// <param name="matrix"></param>
        public static void PrintMatrix(int[][] matrix)
        {
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    Console.Write(matrix[i][j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
