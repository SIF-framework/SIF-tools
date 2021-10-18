// iMODstats is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODstats.
// 
// iMODstats is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODstats is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODstats. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODstats
{
    /// <summary>
    /// Class for definition of processed zone
    /// </summary>
    public class ZoneDefinition
    {
        public string ID;
        public string ZoneString;
        public List<int> Values { get; }

        /// <summary>
        /// Creates a ZoneDefinition object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="zoneString">"v" or "v1-v2", with v, v1 and v2 integers</param>
        public ZoneDefinition(string id, string zoneString)
        {
            this.ID = id;
            this.ZoneString = zoneString;
            List<int> valueList = new List<int>();
            int dashIdx = zoneString.IndexOf("-");
            if (dashIdx > 0)
            {
                // form is v1-v2; add all values between v1 and v2
                string value1String = zoneString.Substring(0, dashIdx);
                string value2String = zoneString.Substring(dashIdx + 1, zoneString.Length - dashIdx - 1);
                int value1 = int.Parse(value1String);
                int value2 = int.Parse(value2String);
                for (int j = value1; j <= value2; j++)
                {
                    valueList.Add(j);
                }
            }
            else
            {
                valueList.Add(int.Parse(zoneString));
            }
            this.Values = valueList;
        }
    }
}
