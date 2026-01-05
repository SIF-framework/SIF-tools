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

namespace Sweco.SIF.iMOD.ISG
{
    /// <summary>
    /// Data for 2D cross section record
    /// </summary>
    public class ISGCrossSection2DData : ISGCrossSectionData
    {
        /// <summary>
        ///  Width in meters of the rectangular raster that follows. For interpretation use absolute value.
        /// </summary>
        public float DX;
        /// <summary>
        ///  Height in meters of the rectangular raster that follows. For interpretation use absolute value.
        /// </summary>
        public float DY;
        /// <summary>
        /// Reference Height in meters, this is used whenever specified (DX less than 0 and DY less than 0). 
        /// A pointer value Zp defines whether an area is inundated once the HREF is exceeded as well as the bottom elevation (Zm + Zc). 
        /// In this case the attribute Z (specified below) is organized differently as a combination of Zm, Zc and Zp.
        /// </summary>
        public float HREF;

        /// <summary>
        /// Points in 2D cross section. note: this will be one less than N as defined for the cross section object, since N also includes a record for the DX/DY/HREF-data.
        /// </summary>
        public List<ISGCrossSection2DDataPoint> Points { get; set; }

        /// <summary>
        /// Constructor for 1D Data record of cross section without HREF-level
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public ISGCrossSection2DData(float dx, float dy)
        {
            this.DX = dx;
            this.DY = dy;
            this.HREF = float.NaN;
            this.Points = new List<ISGCrossSection2DDataPoint>();
        }

        /// <summary>
        /// Constructor for 1D Data record of cross section with HREF-level
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="href"></param>
        public ISGCrossSection2DData(float dx, float dy, float href)
        {
            this.DX = dx;
            this.DY = dy;
            this.HREF = href;
            this.Points = new List<ISGCrossSection2DDataPoint>();
        }

        /// <summary>
        /// Copies cross section 2D data record
        /// </summary>
        /// <returns></returns>
        public override ISGCrossSectionData Copy()
        {
            ISGCrossSection2DData csData = new ISGCrossSection2DData(DX, DY, HREF);
            foreach (ISGCrossSection2DDataPoint csPoint in this.Points)
            {
                csData.Points.Add(csPoint.Copy());
            }

            return csData;
        }
    }

    /// <summary>
    /// Data for 2D crosssection points
    /// </summary>
    public class ISGCrossSection2DDataPoint
    {
        /// <summary>
        ///  X coordinate (meter) for a riverbed “pixel”, these coordinates need to be on a rectangular network with spatial distance of DX.
        /// </summary>
        public float X { get; private set; }
        /// <summary>
        /// Y coordinate (meter) for a riverbed “pixel” , these coordinates need to be on a rectangular network with spatial distance of DY.
        /// </summary>
        public float Y { get; private set; }
        /// <summary>
        ///  Bottom level of riverbed (meter); valid whenever DX > 0.0 and DY > 0.0
        /// </summary>
        public float Z { get; private set; }
        /// <summary>
        /// Integer part (meters) of bottom level of riverbed, e.g. bottom level is -23.43, Zm = -23; valid whenever DX less than 0 and DY less than 0
        /// </summary>
        public short Zm { get; private set; }
        /// <summary>
        /// Fractional part (centimeters) of bottom level of riverbed, e.g.bottom level is -23.43, Zm = 43; valid whenever DX less than 0 and DY less than 0
        /// For interpretation use absolute value: if the bottom level is negative, both Zm and Zc are negative. Use negative sign if Zm=0.
        /// </summary>
        public sbyte Zc { get; private set; }
        /// <summary>
        /// Integer value of area affected by HREF, e.g. areas with Zp less than 0 will be inundated only whenever the current river stage is higher than the Reference Height (HREF) and
        /// the river stage is higher than the corresponding riverbed Zm+Zc. Areas with Zp > 0, will be inundated whenever the river stage is higher than the current riverbed. 
        /// The absolute value of Zp is used as a multiplication factor for the river bed resistances for the attribute RESIS in the ISD2-file.
        /// </summary>
        public sbyte Zp { get; private set; }

        /// <summary>
        /// Constructor for 2D cross section data point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public ISGCrossSection2DDataPoint(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            if (z.Equals(float.NaN))
            {
                throw new Exception("Z-value cannot be defined as NaN for a 2D Cross section point: (" + x.ToString(IMODFile.EnglishCultureInfo) + "," + y.ToString(IMODFile.EnglishCultureInfo) + "," + z.ToString(IMODFile.EnglishCultureInfo) + ")");
            }
        }

        /// <summary>
        /// Constructor for 2D cross section data point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zm"></param>
        /// <param name="zc"></param>
        /// <param name="zp"></param>
        public ISGCrossSection2DDataPoint(float x, float y, short zm, sbyte zc, sbyte zp)
        {
            this.X = x;
            this.Y = y;
            this.Z = float.NaN;
            this.Zm = zm;
            this.Zc = zc;
            this.Zp = zp;
        }

        /// <summary>
        /// Creates copy of point data of 2D Cross Section record
        /// </summary>
        /// <returns></returns>
        public ISGCrossSection2DDataPoint Copy()
        {
            return HasZPRepresentation() ? new ISGCrossSection2DDataPoint(X, Y, Zm, Zc, Zp) : new ISGCrossSection2DDataPoint(X, Y, Z);
        }

        /// <summary>
        /// Specifies if this point has a Z-value defined or a tuple with (Zm, Zc and Zp)
        /// </summary>
        public bool HasZPRepresentation()
        {
            return Z.Equals(float.NaN);
        }

        /// <summary>
        /// Retrieve a floating point Z-value (no matter what representation is used)
        /// </summary>
        /// <returns></returns>
        public float GetZValue()
        {
            float z;
            if (Z.Equals(float.NaN))
            {
                double zm = Math.Abs(Zm);
                double zc = Math.Abs(Zc) / 100.0;
                z = (float)(zm + zc);
                if ((Zm < 0) || (Zc < 0))
                {
                    z *= -1.0f;
                }
            }
            else
            {
                z = Z;
            }

            return z;
        }

        /// <summary>
        /// Split a floating point Z-value into corresponding Zm, Zc values
        /// </summary>
        /// <param name="z"></param>
        /// <param name="zm"></param>
        /// <param name="zc"></param>
        /// <exception cref="Exception"></exception>
        public static void SplitZValue(float z, out short zm, out sbyte zc)
        {
            if (!z.Equals(float.NaN))
            {
                zm = (short) Math.Truncate(z);
                zc = (sbyte) Math.Truncate(100 * (z - zm));
            }
            else
            {
                throw new Exception("Zm and Zc cannot be retrieved from NaN z-value");
            }
        }
    }
}
