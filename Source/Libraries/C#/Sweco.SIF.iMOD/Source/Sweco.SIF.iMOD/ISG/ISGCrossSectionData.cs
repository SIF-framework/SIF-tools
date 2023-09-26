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

namespace Sweco.SIF.iMOD.ISG
{
    public abstract class ISGCrossSectionData
    {
        public const int ByteLength = 12;

        public abstract ISGCrossSectionData Copy();
    }

    /// <summary>
    /// Data for 1D crosssection
    /// </summary>
    public class ISGCrossSectionData1 : ISGCrossSectionData
    {
        /// <summary>
        ///  Distance of the cross-section measured from the center of the riverbed (minus to the left en positive to the right)
        /// </summary>
        public float DISTANCE;
        /// <summary>
        ///  Bottom level of the riverbed (meter), whereby zero will be assigned to the lowest riverbed level.
        /// </summary>
        public float BOTTOM;
        /// <summary>
        ///  KManning resistance factor (-)
        /// </summary>
        public float KM;

        public ISGCrossSectionData1()
        {
        }

        public ISGCrossSectionData1(float distance, float bottom, float km)
        {
            this.DISTANCE = distance;
            this.BOTTOM = bottom;
            this.KM = km;
        }

        public override ISGCrossSectionData Copy()
        {
            return new ISGCrossSectionData1(DISTANCE, BOTTOM, KM);
        }
    }

    /// <summary>
    /// Data for 2D crosssection
    /// </summary>
    public class ISGCrossSectionData2 : ISGCrossSectionData
    {
        /// <summary>
        ///  Width in meters of the rectangular raster that follows.
        /// </summary>
        public float DX;
        /// <summary>
        ///  Height in meters of the rectangular raster that follows.
        /// </summary>
        public float DY;
        /// <summary>
        ///  X coordinate (meter) for a riverbed “pixel”, these coordinates need to be on a rectangular network with spatial distance of DX.
        /// </summary>
        public float X;
        /// <summary>
        /// Y coordinate (meter) for a riverbed “pixel” , these coordinates need to be on a rectangular network with spatial distance of DY.
        /// </summary>
        public float Y;
        /// <summary>
        ///  Bottom level of the riverbed (meter).
        /// </summary>
        public float Z;

        public ISGCrossSectionData2()
        {
        }

        public ISGCrossSectionData2(float dx, float dy, float x, float y, float z)
        {
            this.DX = dx;
            this.DY = dy;
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override ISGCrossSectionData Copy()
        {
            return new ISGCrossSectionData2(DX, DY, X, Y, Z);
        }
    }
}
