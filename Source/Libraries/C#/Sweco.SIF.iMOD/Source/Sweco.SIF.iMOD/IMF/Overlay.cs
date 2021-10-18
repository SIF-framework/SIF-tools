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
using Sweco.SIF.iMOD.GEN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IMF
{
    /// <summary>
    /// For storing GEN-files in Overlays section of IMF-file
    /// </summary>
    public class Overlay
    {
        /// <summary>
        /// GENLegend object for visualisation of overlay in iMOD
        /// </summary>
        public GENLegend Legend { get; set; }

        /// <summary>
        /// Filename of overlay file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Creates a new Overlay instance for specified legend and filename
        /// </summary>
        /// <param name="legend"></param>
        /// <param name="filename"></param>
        public Overlay(GENLegend legend, string filename)
        {
            this.Legend = legend;
            this.Filename = filename;
        }
    }
}
