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
    /// Class to store and process dataset with points and corresponding point data
    /// </summary>
    public class PointDataset
    {
        /// <summary>
        /// List of point
        /// </summary>
        public List<PointEntry> Points { get; }

        /// <summary>
        /// Number of points in dataset
        /// </summary>
        public long Count
        {
            get { return Points.Count; }
        }

        /// <summary>
        /// Columnnames, excluding point coordinates
        /// </summary>
        public List<string> ColumnNames { get; }

        /// <summary>
        /// Create empty dataset with specified columnnames
        /// </summary>
        /// <param name="columnNames"></param>
        public PointDataset(List<string> columnNames)
        {
            this.Points = new List<PointEntry>();
            this.ColumnNames = columnNames;
        }

        /// <summary>
        /// Add a point to the dataset
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(PointEntry point)
        {
            Points.Add(point);
        }

        /// <summary>
        /// Add points to the dataset
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints(List<PointEntry> points)
        {
            Points.AddRange(points);
        }

        /// <summary>
        /// Add points without data (without column values) to the dataset
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints(List<Point> points)
        {
            foreach (Point point in points)
            {
                Points.Add(new PointEntry(point, null));
            }
        }
    }

    /// <summary>
    /// Point definition (coordinates) with corresponding data
    /// </summary>
    public class PointEntry
    {
        /// <summary>
        /// Point definition (coordinates)
        /// </summary>
        public Point Point { get; }

        /// <summary>
        /// Point data (column values)
        /// </summary>
        public List<string> ColumnValues { get; }

        /// <summary>
        /// Create a point entry for the given point coordinates and data
        /// </summary>
        /// <param name="point"></param>
        /// <param name="columnValues"></param>
        public PointEntry(Point point, List<string> columnValues)
        {
            this.Point = point;
            this.ColumnValues = columnValues;
        }
    }
}
