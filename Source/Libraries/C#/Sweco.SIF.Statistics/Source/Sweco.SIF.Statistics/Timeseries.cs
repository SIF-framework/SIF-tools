// Sweco.SIF.Statistics is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Statistics.
// 
// Sweco.SIF.Statistics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Statistics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Statistics. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Statistics
{
    /// <summary>
    /// This class stores a timeseries of timestamps and (single array of) float values and offers methods to compute several basic statistics
    /// </summary>
    public class Timeseries : Series
    {
        /// <summary>
        /// Array of all timestamps (dates/times) for this timeseries
        /// </summary>
        protected DateTime[] timestamps;

        /// <summary>
        /// Array of all timestamps (dates/times) for this timeseries
        /// </summary>
        public DateTime[] Timestamps
        {
            get { return timestamps; }
            set { timestamps = value; }
        }

        /// <summary>
        /// Create Timeseries object for give timestamps, valuea and optional remarks and statuses
        /// </summary>
        /// <param name="timestamps"></param>
        /// <param name="values"></param>
        /// <param name="remarks"></param>
        /// <param name="statuses"></param>
        public Timeseries(DateTime[] timestamps, float[] values, string[] remarks = null, int[] statuses = null)
        {
            if ((values.Length != timestamps.Length) || ((remarks == null) || (values.Length != remarks.Length)) || ((statuses == null) || (values.Length != statuses.Length)))
            {
                throw new Exception("Length of values, timestamps, remarks and/or statuses arrays don't match");
            }

            this.values = values;
            this.timestamps = timestamps;
            this.remarks = remarks;
            this.statuses = statuses;
        }

        /// <summary>
        /// Copy timestamps, values, remarks and statuses to a new Timeseries object
        /// </summary>
        /// <returns></returns>
        public override Series Copy()
        {
            return (Series)this.MemberwiseClone();
        }
    }
}
