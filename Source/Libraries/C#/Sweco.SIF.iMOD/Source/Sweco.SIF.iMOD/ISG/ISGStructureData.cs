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
    public class ISGStructureData
    {
        /// <summary>
        /// Byte length for single precision ISG-files
        /// </summary>
        public const int SingleByteLength = 12;
        /// <summary>
        /// Byte length for double precision ISG-files
        /// </summary>
        public const int DoubleByteLength = 28;

        /// <summary>
        /// Date for structure record (in IST2-file representation as yyyymmdd).
        /// </summary>
        public DateTime DATE;
        /// <summary>
        ///  Water level for the upstream side of the weir/structure (m+MSL).
        /// </summary>
        public float WLVL_UP;
        /// <summary>
        /// Water level for the downstream side of the weir/structure (m+MSL).
        /// </summary>
        public float WLVL_DOWN;

        public ISGStructureData()
        {
        }
        public ISGStructureData(DateTime date, float wlvlUp, float wlvlDown)
        {
            this.DATE = date;
            this.WLVL_UP = wlvlUp;
            this.WLVL_DOWN = wlvlDown;
        }

        public ISGStructureData Copy()
        {
            return new ISGStructureData(DATE, WLVL_UP, WLVL_DOWN);
        }
    }
}
