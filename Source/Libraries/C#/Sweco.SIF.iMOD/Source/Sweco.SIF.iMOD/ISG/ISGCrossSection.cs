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
    public class ISGCrossSection
    {
        /// <summary>
        /// Byte length for single precision ISG-files
        /// </summary>
        public const int SingleByteLength = 44;
        /// <summary>
        /// Byte length for double precision ISG-files
        /// </summary>
        public const int DoubleByteLength = 48;

        /// <summary>
        /// Name of the cross-section.
        /// </summary>
        public string CNAME;

        /// <summary>
        ///  Distance (meters) measured from the beginning of the segment (node 1) that locates the cross-section
        /// </summary>
        public float DIST;

        /// <summary>
        ///   The meaning of this attribute is twofold:
        ///   - the absolute value defines the number of data records in the ISC2-file
        ///   - N greater than 0: specifies a 1D-cross section, with a distance, bottom and manning coefficient along the cross section
        ///   - N smaller than 0: specifies a 2D-cross section, which describes the riverbed as a collection of x,y,z points,
        ///                       including an extra record to describe the dimensions (DX,DY) and HREF-level of the network.
        /// </summary>
        public int N;

        /// <summary>
        ///  Record number within the ISC2-file for the ﬁrst data record that describes the cross-section.
        /// </summary>
        public int IREF;

        /// <summary>
        /// Data record that defines the actual cross section (which depends on the cross section type as defined by N)
        /// </summary>
        public ISGCrossSectionData Data { get; set; }

        /// <summary>
        /// Constructor for new cross section object without actual data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dist"></param>
        /// <param name="n"></param>
        /// <param name="iref"></param>
        public ISGCrossSection(string name, float dist, int n, int iref)
        {
            this.CNAME = name;
            this.DIST = dist;
            this.N = n;
            this.IREF = iref;
            this.Data = null;
        }

        /// <summary>
        /// Creates copy of cross section object
        /// </summary>
        /// <returns></returns>
        public ISGCrossSection Copy()
        {
            ISGCrossSection csCopy = new ISGCrossSection(CNAME, DIST, N, IREF);
            if (Data != null)
            {
                csCopy.Data = Data.Copy();
            }

            return csCopy;
        }
    }
}
