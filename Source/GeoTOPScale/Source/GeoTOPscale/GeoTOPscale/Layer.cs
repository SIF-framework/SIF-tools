// GeoTOPScale is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GeoTOPScale.
// 
// GeoTOPScale is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GeoTOPScale is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GeoTOPScale. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GeoTOPscale
{
    public class Layer
    {
        public IDFFile TOPIDFFile { get; set; }
        public IDFFile BOTIDFFile { get; set; }

        public Layer(IDFFile topIDFFile, IDFFile botIDFFile)
        {
            this.TOPIDFFile = topIDFFile;
            this.BOTIDFFile = botIDFFile;
        }
    }
}
