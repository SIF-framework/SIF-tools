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
        public const int ByteLength = 44;

        /// <summary>
        ///   The meaning of this attribute is twofold:
        ///   - greater than 0: Number of data records in the ISC2-ﬁle that describes the actual cross-section.
        ///   - smaller than 0: The absolute number of data records in the ISC2-ﬁle that describes the riverbed 
        ///     as a collection of x,y,z points including an extra record to describe the dimensions (DX,DY ) of
        ///     the network that captured the x,y,z points.
        /// </summary>
        public int N;
        /// <summary>
        ///  Record number within the ISC2-ﬁle for the ﬁrst data record that describes the cross-section.
        /// </summary>
        public int IREF;
        /// <summary>
        ///  Distance (meters) measured from the beginning of the segment (node 1) that locates the cross-section
        /// </summary>
        public float DIST;
        /// <summary>
        /// Name of the cross-section.
        /// </summary>
        public string CNAME;

        private ISGCrossSectionData[] definitions;
        public ISGCrossSectionData[] Definitions
        {
            get { return definitions; }
            set { definitions = value; }
        }

        public ISGCrossSection Copy()
        {
            ISGCrossSection csCopy = new ISGCrossSection();
            csCopy.CNAME = CNAME;
            csCopy.DIST = DIST;
            csCopy.N = N;
            csCopy.IREF = 0;
            csCopy.definitions = new ISGCrossSectionData[definitions.Count()];
            for (int defIdx = 0; defIdx < definitions.Count(); defIdx++)
            {
                csCopy.definitions[defIdx] = (definitions[defIdx].Copy());
            }
            return csCopy;
        }
    }
}
