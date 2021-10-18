// Sweco.SIF.GIS is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.GIS.
// 
// Sweco.SIF.GIS is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.GIS is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.GIS. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GIS
{
    /// <summary>
    /// Interface for points with x,y-coordinates, that can be accessed as an double or a string.
    /// </summary>
    public interface IPoint : IComparable
    {
        /// <summary>
        /// x-coordinate as a double
        /// </summary>
        double X { get; set; }
        /// <summary>
        /// y-coordinate as a double
        /// </summary>
        double Y { get; set; }
        /// <summary>
        /// x-coordinate as a string
        /// </summary>
        string XString { get; set; }
        /// <summary>
        /// y-coordinate as a string
        /// </summary>
        string YString { get; set; }
        /// <summary>
        /// Format point details as a string
        /// </summary>
        /// <returns></returns>
        string ToString();
    }
}
