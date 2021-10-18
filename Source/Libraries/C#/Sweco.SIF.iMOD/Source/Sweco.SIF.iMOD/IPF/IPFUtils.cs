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
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Common utilities for processing IPF-files
    /// </summary>
    public class IPFUtils
    {
        /// <summary>
        /// Removed specified IPF-points from the specified IPF-point list
        /// </summary>
        /// <param name="points"></param>
        /// <param name="removedPoints"></param>
        public static void RemovePoints(List<IPFPoint> points, List<IPFPoint> removedPoints)
        {
            foreach (IPFPoint point in removedPoints)
            {
                points.Remove(point);
            }
        }
    }
}
