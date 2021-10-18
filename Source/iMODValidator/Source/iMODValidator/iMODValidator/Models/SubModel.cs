// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models
{
    /// <summary>
    /// Class for storing properties of submodel
    /// </summary>
    public class SubModel
    {
        public int IACT;       // determines the whether a sub model need to be computed: -1 (if no result folder), 0 no, 1 yes
        public float XMIN;     // ll X-coordinate
        public float YMIN;     // ll Y-coordinate
        public float XMAX;     // ur X-coordinate
        public float YMAX;     // ur Y-coordinate
        public float CSIZE;    // Grid cell size within the area of interest and within the buffer
        public float MAXCSIZE; // maximum grid cell size within the buffer. Within the buffer the entered grid cell size CSIZE, will increase gradually up to MAXCSIZE
        public float BUFFER;   // the size of the buffer around the area of interest
        public string CSUB;    // Optional, name of result folder for current sub model, yielding [OUTPUT-FOLDER]\[CSUB]\ as result folder. Whenever no name is given, the default folder name will be submodel[i], where I represents the ith sub model within NMULT

        public SubModel()
        {
            MAXCSIZE = float.NaN;
            CSUB = null;
        }
    }
}
