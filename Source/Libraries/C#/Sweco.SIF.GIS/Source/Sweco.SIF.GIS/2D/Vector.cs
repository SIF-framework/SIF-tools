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
    /// 2D vector implementation
    /// </summary>
    public class Vector : IVector
    {
        /// <summary>
        /// Vector length in x-direction (for faster, direct access of value)
        /// </summary>
        public double dX;
        /// <summary>
        /// Vector length in y-direction
        /// </summary>
        public double dY;

        /// <summary>
        /// Create empty Vector object
        /// </summary>
        protected Vector()
        {
            this.dX = double.NaN;
            this.dY = double.NaN;
        }

        /// <summary>
        /// Create Vector object with specified dx and dy
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Vector(double dx, double dy)
        {
            this.dX = dx;
            this.dY = dy;
        }

        /// <summary>
        /// Multiplies specified vectors
        /// </summary>
        /// <param name="v1">vector 1</param>
        /// <param name="v2">vector 2</param>
        /// <returns></returns>
        public static double operator *(Vector v1, Vector v2)
        {
            return v1.dX * v2.dX + v1.dY * v2.dY;
        }

        /// <summary>
        /// Multplies vector with a value
        /// </summary>
        /// <param name="v">vector</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector operator *(Vector v, double value)
        {
            return new Vector(value * v.dX, value * v.dY);
        }

        /// <summary>
        /// Multiplies value and vector
        /// </summary>
        /// <param name="value"></param>
        /// <param name="v">vector</param>
        /// <returns></returns>
        public static Vector operator *(double value, Vector v)
        {
            return new Vector(value * v.dX, value * v.dY);
        }
    }
}
