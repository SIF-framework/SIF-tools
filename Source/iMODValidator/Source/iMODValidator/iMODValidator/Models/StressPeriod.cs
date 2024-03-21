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
    /// Class to define a stress period in a modelrun
    /// </summary>
    public class StressPeriod : IEquatable<StressPeriod>
    {
        /// <summary>
        /// Special SNAME keyword that is used for steady-state stress period
        /// </summary>
        public const string SteadyStateSNAME = "STEADY-STATE";

        public int KPER;
        public float DELT;
        public string SNAME;
        public int ISAVE;
        public DateTime? DateTime;

        /// <summary>
        /// Create a steady-state StressPeriod object with KPER=0 and SNAME 'STEADY-STATE'. 
        /// DateTime property will be null. DELT and ISAVE will be -1.
        /// </summary>
        public StressPeriod()
        {
            this.KPER = 0;
            this.DELT = 1;
            this.SNAME = SteadyStateSNAME;
            this.ISAVE = 1;
            this.DateTime = null;
        }

        /// <summary>
        /// Create a steady-state StressPeriod object with KPER=0 and SNAME 'STEADY-STATE'. 
        /// DateTime property will be null. DELT will be -1.
        /// </summary>
        /// <param name="ISAVE"></param>
        public StressPeriod(int ISAVE)
        {
            this.KPER = 0;
            this.SNAME = SteadyStateSNAME;
            this.DELT = -1;
            this.ISAVE = ISAVE;
            this.DateTime = null;
        }

        private static StressPeriod steadyState;
        public static StressPeriod SteadyState
        {
            get
            {
                if (steadyState == null)
                {
                    steadyState = new StressPeriod();
                }
                return steadyState;
            }
        }

        /// <summary>
        /// Create a StressPeriod object for specified KPER, SNAME and date.
        /// </summary>
        /// <param name="KPER"></param>
        /// <param name="DELT"></param>
        /// <param name="SNAME"></param>
        /// <param name="datetime"></param>
        /// <param name="ISAVE"></param>
        public StressPeriod(int KPER, float DELT, string SNAME, DateTime? datetime, int ISAVE)
        {
            this.KPER = KPER;
            this.DELT = DELT;
            this.SNAME = SNAME;
            this.DateTime = datetime;
            this.ISAVE = ISAVE;
        }

        /// <summary>
        /// Create a StressPeriod object for specified KPER, SNAME and date. DELT and ISAVE will be -1.
        /// </summary>
        /// <param name="KPER"></param>
        /// <param name="SNAME"></param>
        /// <param name="dateTime"></param>
        public StressPeriod(int KPER, string SNAME, DateTime? dateTime)
        {
            this.KPER = KPER;
            this.SNAME = SNAME;
            this.DateTime = dateTime;

            this.DELT = -1;
            this.ISAVE = -1;
        }

        /// <summary>
        /// Check if stress period properties are equal to properties of other stress period, except for ISAVE
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(StressPeriod other)
        {
            if (this == other)
            {
                return true;
            }
            else
            {
                return this.KPER.Equals(other.KPER) && this.SNAME.Equals(other.SNAME) 
                    && (((this.DateTime == null) && (other.DateTime == null)) || (this.DateTime.Equals(other.DateTime)));
            }
        }

        public override string ToString()
        {
            return "stress period " + this.SNAME;
        }

        /// <summary>
        /// Parse string with "yyyymmdd"-format into DateTime object with specified format (e.g. 'yyyy-MM-dd hh:mm:ss')
        /// </summary>
        /// <param name="SNAME"></param>
        /// <param name="format"></param>
        /// <returns>DateTime(0) when a parse error occurs</returns>
        public static DateTime ParseSNAME(string SNAME, string format = "yyyyMMdd")
        {
            try
            {
                return System.DateTime.ParseExact(SNAME, format, SIFTool.EnglishCultureInfo, System.Globalization.DateTimeStyles.None);
            }
            catch (Exception)
            {
                return new DateTime(0);
            }
        }
    }
}
