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
    /// <summary>
    /// Data for 1D cross section record
    /// </summary>
    public class ISGCrossSection1DData : ISGCrossSectionData
    {
        /// <summary>
        /// Constructor for cross section 1D data record
        /// </summary>
        /// <returns></returns>
        public ISGCrossSection1DData()
        {
            this.Points = new List<ISGCrossSection1DDataPoint>();
        }

        /// <summary>
        /// List of points along 1D cross section with level data
        /// </summary>
        public List<ISGCrossSection1DDataPoint> Points { get; set; }

        /// <summary>
        /// Copies cross section 1D data record
        /// </summary>
        /// <returns></returns>
        public override ISGCrossSectionData Copy()
        {
            ISGCrossSection1DData csData = new ISGCrossSection1DData();
            foreach (ISGCrossSection1DDataPoint csPoint in this.Points)
            {
                csData.Points.Add(csPoint.Copy());
            }

            return csData;
        }
    }

    /// <summary>
    /// Data for 1D crosssection points
    /// </summary>
    public class ISGCrossSection1DDataPoint
    {
        /// <summary>
        ///  Distance of the cross section point, measured from the center of the riverbed (minus to the left en positive to the right)
        /// </summary>
        public float DISTANCE;
        /// <summary>
        ///  Bottom level of the riverbed (meter), at the cross section point, whereby zero will be assigned to the lowest riverbed level.
        /// </summary>
        public float BOTTOM;
        /// <summary>
        ///  KManning resistance factor (-)
        /// </summary>
        public float KM;

        /// <summary>
        /// Constructor for 1D Data record of cross section
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="bottom"></param>
        /// <param name="km"></param>
        public ISGCrossSection1DDataPoint(float distance, float bottom, float km)
        {
            this.DISTANCE = distance;
            this.BOTTOM = bottom;
            this.KM = km;
        }

        /// <summary>
        /// Creates copy of point data of 2D Cross Section record
        /// </summary>
        /// <returns></returns>
        public ISGCrossSection1DDataPoint Copy()
        {
            return new ISGCrossSection1DDataPoint(DISTANCE, BOTTOM, KM);
        }
    }
}
