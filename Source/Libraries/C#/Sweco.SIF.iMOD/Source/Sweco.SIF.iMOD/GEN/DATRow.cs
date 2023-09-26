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

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class to store for rows in a DAT-file as a List of strings
    /// </summary>
    public class DATRow : List<string>
    {
        /// <summary>
        /// Creates an empty DATRow object
        /// </summary>
        public DATRow() : base()
        {
        }

        /// <summary>
        /// Creates a new DATRow object with specified string values
        /// </summary>
        /// <param name="values"></param>
        public DATRow(List<string> values) : base(values)
        {
        }

        /// <summary>
        /// Creates a new DATRow object with specified string values
        /// </summary>
        /// <param name="values"></param>
        public DATRow(string[] values) : base(values)
        {
        }

        /// <summary>
        /// Creates a copy of this DATRow object
        /// </summary>
        /// <returns></returns>
        public DATRow Copy()
        {
            return new DATRow(this.ToList());
        }

        /// <summary>
        /// Combine the value of this row to a single, commaseperated string, corrected with surrounding (single) quotes for whitespace in list values
        /// </summary>
        public override string ToString()
        {
            StringBuilder rowStringBuilder = new StringBuilder();
            for (int colIdx = 0; colIdx < (Count - 1); colIdx++)
            {
                rowStringBuilder.Append(GENUtils.CorrectString(this[colIdx]));
                rowStringBuilder.Append(",");
            }
            rowStringBuilder.AppendLine(GENUtils.CorrectString(this[Count - 1]));
            return rowStringBuilder.ToString();
        }
    }
}
