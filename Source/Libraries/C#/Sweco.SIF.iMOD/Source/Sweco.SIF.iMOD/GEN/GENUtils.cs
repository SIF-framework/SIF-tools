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

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Common utilities for GEN-file processing
    /// </summary>
    public class GENUtils
    {
        /// <summary>
        /// Corrects a string for a DATRow by adding single quotes around strings that contain
        /// one or more spaces, tabs, comma's. Backslashes (single or double) are replaced by forward slashes.
        /// </summary>
        /// <param name="someValue"></param>
        /// <returns></returns>
        public static string CorrectString(string someValue)
        {
            if (someValue != null)
            {
                if (someValue.IndexOfAny(new char[] { ' ', '\t', ',' }) >= 0)
                {
                    if (!someValue.StartsWith("'"))
                    {
                        someValue = "'" + someValue;
                    }
                    if (!someValue.EndsWith("'"))
                    {
                        someValue = someValue + "'";
                    }
                    if (!someValue.EndsWith("'"))
                    {
                        someValue = someValue + "'";
                    }
                }
                if (someValue.IndexOf("\\") > 0)
                {
                    someValue = someValue.Replace("\\\\", "/");
                    someValue = someValue.Replace("\\", "/");
                }
            }
            return someValue;
        }
    }
}
