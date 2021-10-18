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
    /// 3D vector implementation
    /// </summary>
    public class Vector3D : Vector, IVector
    {
        /// <summary>
        /// Vector length in z-direction
        /// </summary>
        public double dZ;

        /// <summary>
        /// Create an empty 3D vector
        /// </summary>
        protected Vector3D()
        {
            this.dX = double.NaN;
            this.dY = double.NaN;
            this.dZ = double.NaN;
        }

        /// <summary>
        /// Create an empty 3D vector with specified vector lengths
        /// </summary>
        public Vector3D(double dx, double dy, double dz)
        {
            this.dX = dx;
            this.dY = dy;
            this.dZ = dz;
        }

        /// <summary>
        /// Multiplies specified 3D vectors
        /// </summary>
        public static double operator *(Vector3D v1, Vector3D v2)
        {
            return v1.dX * v2.dX + v1.dY * v2.dY + v1.dZ * v2.dZ;
        }

        /// <summary>
        /// Multplies 3D vector with a value
        /// </summary>
        public static Vector3D operator *(Vector3D v, double value)
        {
            return new Vector3D(value * v.dX, value * v.dY, value * v.dZ);
        }

        /// <summary>
        /// Multiplies value and 3D vector
        /// </summary>
        public static Vector3D operator *(double value, Vector3D v)
        {
            return new Vector3D(value * v.dX, value * v.dY, value * v.dZ);
        }
    }
}
