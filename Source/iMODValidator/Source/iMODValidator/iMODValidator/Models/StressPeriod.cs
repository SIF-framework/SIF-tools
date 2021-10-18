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
    /// Class to define a stressperiod in a modelrun
    /// </summary>
    public class StressPeriod : IEquatable<StressPeriod>
    {
        public int KPER;
        public float DELT;
        public string SNAME;
        public int ISAVE;
        public DateTime? DateTime;

        public StressPeriod() { }

        public StressPeriod(int KPER, float DELT, string SNAME, int ISAVE)
        {
            this.KPER = KPER;
            this.DELT = DELT;
            this.SNAME = SNAME;
            this.ISAVE = ISAVE;
            this.DateTime = null;
            if ((DELT > 0) && (SNAME != null))
            {
                try
                {
                    int year = int.Parse(SNAME.Substring(0, 4));
                    int month = int.Parse(SNAME.Substring(4, 2));
                    int day = int.Parse(SNAME.Substring(6, 2));
                    this.DateTime = new DateTime(year, month, day);
                }
                catch (Exception)
                {
                    // ignore exception, leave DateTime null
                }
            }
        }

        public bool Equals(StressPeriod other)
        {
            if (this == other)
            {
                return true;
            }
            else
            {
                return this.KPER.Equals(other.KPER) && this.DELT.Equals(other.DELT)
                    && this.SNAME.Equals(other.SNAME) && this.ISAVE.Equals(other.ISAVE)
                    && (((this.DateTime == null) && (other.DateTime == null)) || (this.DateTime.Equals(other.DateTime)));
            }
        }

        public override string ToString()
        {
            return "stressperiod " + this.SNAME;
        }

        public static string ToString(StressPeriod stressPeriod)
        {
            if (stressPeriod != null)
            {
                return stressPeriod.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parse string with "yyyymmdd"-format into DateTime object
        /// </summary>
        /// <param name="stressperiodString"></param>
        /// <returns>DateTime(0) when a parse error occurs</returns>
        public static DateTime ParseStressPeriodString(string stressperiodString)
        {
            try
            {
                int year = int.Parse(stressperiodString.Substring(0, 4));
                int month = int.Parse(stressperiodString.Substring(4, 2));
                int day = int.Parse(stressperiodString.Substring(6, 2));
                return new DateTime(year, month, day);
            }
            catch (Exception)
            {
                return new DateTime(0);
            }
        }
    }
}
