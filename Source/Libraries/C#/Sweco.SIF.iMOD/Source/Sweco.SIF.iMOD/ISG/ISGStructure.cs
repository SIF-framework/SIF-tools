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
    public class ISGStructure
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
        /// Number of data records in the IST2-ﬁle that describes the actual timeserie for the weir/structure.
        /// </summary>
        public int N;
        /// <summary>
        /// Record number within the IST2-ﬁle for the ﬁrst data record that describes the weirs/structure.
        /// </summary>
        public int IREF;
        /// <summary>
        /// Distance (meters) measured from the beginning of the segment (node 1) that locates the weir/structure.
        /// </summary>
        public float DIST;
        /// <summary>
        /// Name of the weir/structure.
        /// </summary>
        public string CNAME;

        public ISGStructureData[] structureDataArray;
        public ISGStructureData[] StructureDataArray
        {
            get { return structureDataArray; }
            set { structureDataArray = value; }
        }

        public ISGStructure Copy()
        {
            ISGStructure structureCopy = new ISGStructure();
            structureCopy.CNAME = CNAME;
            structureCopy.DIST = DIST;
            structureCopy.N = N;
            structureCopy.IREF = 0;
            structureCopy.structureDataArray = new ISGStructureData[N];
            for (int idx = 0; idx < N; idx++)
            {
                structureCopy.structureDataArray[idx] = structureDataArray[idx].Copy();
            }
            return structureCopy;
        }
    }
}
