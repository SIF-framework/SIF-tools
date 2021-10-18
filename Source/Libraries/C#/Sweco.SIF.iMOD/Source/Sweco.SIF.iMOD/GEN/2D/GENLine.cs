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

    }
}
