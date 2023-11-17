// HFBmanager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HFBmanager.
// 
// HFBmanager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HFBmanager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HFBmanager. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.HFBManager
{
    /// <summary>
    /// Definition for HFB fault section with upper geological unit of fault section and weight for all units below
    /// </summary>
    public class FaultDefinition
    {
        /// <summary>
        /// Name of upper geological unit
        /// </summary>
        public string UpperUnit { get; set; }

        /// <summary>
        /// Weight (days) for fault up to this unit
        /// </summary>
        public float Weight { get; set; }

        public FaultDefinition(string upperUnit, float weight)
        {
            this.UpperUnit = upperUnit;
            this.Weight = weight;
        }

        public override string ToString()
        {
            return "(" + UpperUnit + "," + Weight.ToString(SIFTool.EnglishCultureInfo) + ")";
        }
    }
}
