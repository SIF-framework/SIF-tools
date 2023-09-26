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
    public class ISGCalculationPoint
    {
        public const int ByteLength = 44;
        /// <summary>
        ///  Number of data records in the ISD2-ﬁle that describes the timeseries of the calculation point.
        /// </summary>
        public int N;
        /// <summary>
        /// Record number within the ISD2-ﬁle for the ﬁrst data record that describes the timeserie 
        /// for the calculation point.
        /// </summary>
        public int IREF;
        /// <summary>
        ///  Distance (meters) measured from the beginning of the segment (node 1) that located the calculation point.
        /// </summary>
        public float DIST;
        /// <summary>
        /// Name of the calculation point.
        /// </summary>
        public string CNAME;

        public ISGCalculationPointData[] cpDataArray;
        public ISGCalculationPointData[] CPDataArray
        {
            get { return cpDataArray; }
            set { cpDataArray = value; }
        }

        public Timeseries GetWaterlevelTimeseries(DateTime? modelStartDate = null, DateTime? modelEndDate = null)
        {
            if (cpDataArray == null)
            {
                return null;
            }

            if (modelStartDate == null)
            {
                modelStartDate = cpDataArray[0].DATE;
            }
            if (modelEndDate == null)
            {
                modelEndDate = cpDataArray[cpDataArray.Count() - 1].DATE;
            }

            Timeseries waterlevelTimeseries = null;
            List<DateTime> dates = new List<DateTime>();
            List<float> values = new List<float>();
            ISGCalculationPointData prevCPData = cpDataArray[0];
            foreach (ISGCalculationPointData cpData in cpDataArray)
            {
                if (cpData.DATE >= modelStartDate)
                {
                    // If no dates have been added yet, this is the first date beyond the startdate. Also add previous data for period before this date
                    if (dates.Count == 0)
                    {
                        dates.Add(prevCPData.DATE);
                        values.Add(prevCPData.WLVL);
                    }
                    if (cpData.DATE <= modelEndDate)
                    {
                        dates.Add(cpData.DATE);
                        values.Add(cpData.WLVL);
                    }
                }
                prevCPData = cpData;
            }
            if (dates.Count() > 0)
            {
                waterlevelTimeseries = new Timeseries(dates, values);
            }
            return waterlevelTimeseries;
        }

        /// <summary>
        /// Retrieve first timeseries within specified period, but include points before and/por after if startDate and/or endDate are not found
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public Timeseries GetTimeseries(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (cpDataArray == null)
            {
                return null;
            }

            if (startDate == null)
            {
                startDate = cpDataArray[0].DATE;
            }
            if (endDate == null)
            {
                endDate = cpDataArray[cpDataArray.Count() - 1].DATE;
            }

            Timeseries timeseries = null;
            List<DateTime> dates = new List<DateTime>();
            List<List<float>> valueLists = new List<List<float>>();
            ISGCalculationPointData prevCPData = cpDataArray[0];

            // Add valuelist for waterlevel, bottom, resistance and infiltrationfactor
            for (int listIdx = 0; listIdx < 4; listIdx++)
            {
                valueLists.Add(new List<float>());
            }

            foreach (ISGCalculationPointData cpData in cpDataArray)
            {
                if (cpData.DATE >= startDate)
                {
                    if ((dates.Count == 0) && (cpData.DATE > startDate))
                    {
                        // If no dates have been added yet, this is the first date beyond the startdate. Also add previous data for period before this date
                        dates.Add(prevCPData.DATE);
                        valueLists[0].Add(prevCPData.WLVL);
                        valueLists[1].Add(prevCPData.BTML);
                        valueLists[2].Add(prevCPData.RESIS);
                        valueLists[3].Add(prevCPData.INFF);
                    }
                    if (cpData.DATE <= endDate)
                    {
                        if ((dates.Count == 0) || (cpData.DATE > prevCPData.DATE))
                        {
                            // Add date when it comes after previously added date
                            dates.Add(cpData.DATE);
                            valueLists[0].Add(cpData.WLVL);
                            valueLists[1].Add(cpData.BTML);
                            valueLists[2].Add(cpData.RESIS);
                            valueLists[3].Add(cpData.INFF);
                        }
                        else
                        {
                            // Date is before or equal to earlier data, skip
                        }
                    }
                    else
                    {
                        // Current date is outside specified period, add if previous date was before endDate
                        if (prevCPData.DATE < endDate)
                        {
                            // Add first date after specified period
                            dates.Add(cpData.DATE);
                            valueLists[0].Add(cpData.WLVL);
                            valueLists[1].Add(cpData.BTML);
                            valueLists[2].Add(cpData.RESIS);
                            valueLists[3].Add(cpData.INFF);
                        }
                    }
                }
                prevCPData = cpData;
            }
            if (dates.Count() > 0)
            {
                timeseries = new Timeseries(dates, valueLists);
            }
            return timeseries;
        }

        /// <summary>
        /// Creaste a new calculationpoint at specified distance from this calculationpoint, 
        /// based on a lineair interpolation between this and other calculation point
        /// Dates are used from this calculationpoint and retrieved from second calculationpoint by a block interpolation (e.q. use exact copy of data of last date before requested date)
        /// Note: assume the calculationpoints are on the same segment, the distance of the CP's on the segments determine the distance between them.
        /// </summary>
        /// <param name="otherCP"></param>
        /// <param name="distance">the distance from this node</param>
        /// <returns></returns>
        public ISGCalculationPoint Interpolate(ISGCalculationPoint otherCP, float distance)
        {
            float totalDistance = Math.Abs(this.DIST - otherCP.DIST);
            float fraction = distance / totalDistance;
            ISGCalculationPoint interpolatedCP = new ISGCalculationPoint();
            interpolatedCP.CNAME = this. CNAME + "-" + otherCP.CNAME;
            interpolatedCP.DIST = DIST;
            interpolatedCP.N = N; // use timeseries count of this cp
            interpolatedCP.IREF = 0;
            interpolatedCP.cpDataArray = new ISGCalculationPointData[N];

            int idx2 = 0;
            for (int idx = 0; idx < N; idx++)
            {
                ISGCalculationPointData cpData = this.cpDataArray[idx];
                interpolatedCP.cpDataArray[idx] = cpData.Copy();
                // Find last date in other CP before or equal to current date in this CP
                // First find the first date just after the date that is searched for
                while ((idx2 < otherCP.N) && (otherCP.cpDataArray[idx2].DATE <= cpData.DATE))
                {
                    idx2++;
                }
                // And now take previous date
                if (idx2 > 0)
                {
                    idx2--;
                }

                // Do the actual interpolation
                ISGCalculationPointData otherCPData = otherCP.cpDataArray[idx2];
                interpolatedCP.cpDataArray[idx].WLVL += fraction * (otherCPData.WLVL - cpData.WLVL);
                interpolatedCP.cpDataArray[idx].BTML += fraction * (otherCPData.BTML - cpData.BTML);
                interpolatedCP.cpDataArray[idx].INFF += fraction * (otherCPData.INFF - cpData.INFF);
                interpolatedCP.cpDataArray[idx].RESIS += fraction * (otherCPData.RESIS - cpData.RESIS);
            }
            return interpolatedCP;
        }

        public ISGCalculationPoint Copy()
        {
            ISGCalculationPoint cpCopy = new ISGCalculationPoint();
            cpCopy.CNAME = CNAME;
            cpCopy.DIST = DIST;
            cpCopy.N = N;
            cpCopy.IREF = 0;
            cpCopy.cpDataArray = new ISGCalculationPointData[N];
            for (int idx = 0; idx < N; idx++)
            {
                cpCopy.cpDataArray[idx] = cpDataArray[idx].Copy();
            }
            return cpCopy;
        }
    }

    public class ISGCalculationPointData
    {
        public const int ByteLength = 20;
        /// <summary>
        ///  Date for record (in ISD2-file representation as yyyymmdd).
        /// </summary>
        public DateTime DATE;
        /// <summary>
        ///  Waterlevel of the river (m+MSL)
        /// </summary>
        public float WLVL;
        /// <summary>
        ///  Bottom level of the riverbed (m+MSL).
        /// </summary>
        public float BTML;
        /// <summary>
        /// Resistance of the riverbed (days).
        /// </summary>
        public float RESIS;
        /// <summary>
        /// Inﬁltration factor (-)
        /// </summary>
        public float INFF;

        public ISGCalculationPointData()
        {
        }

        public ISGCalculationPointData(DateTime date, float wlvl, float btml, float resis, float inff)
        {
            this.DATE = date;
            this.WLVL = wlvl;
            this.BTML = btml;
            this.RESIS = resis;
            this.INFF = inff;
        }

        public ISGCalculationPointData Copy()
        {
            return new ISGCalculationPointData(DATE, WLVL, BTML, RESIS, INFF);
        }
    }
}
