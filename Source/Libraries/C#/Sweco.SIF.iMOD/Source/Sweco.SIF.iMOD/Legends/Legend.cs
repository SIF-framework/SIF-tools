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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Legends
{
    /// <summary>
    /// Base class for iMOD legends
    /// </summary>
    public abstract class Legend
    {
        /// <summary>
        /// Formatting and other cultureInfo of English (UK) language
        /// </summary>
        protected static CultureInfo EnglishCultureInfo { get; set; } = new CultureInfo("en-GB", false);

        /// <summary>
        /// Short description of legend
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Creates empty object
        /// </summary>
        protected Legend()
        {
            this.Description = string.Empty;
        }

        /// <summary>
        /// Creates empty IMODLegend with specified description
        /// </summary>
        /// <param name="description"></param>
        public Legend(string description)
        {
            this.Description = description;
        }

        /// <summary>
        /// Creates header lines: the legend description with a line of '-' symbols below
        /// </summary>
        /// <returns></returns>
        protected virtual string CreateLegendHeader()
        {
            string legendString = Description + "\r\n";
            for (int i = 0; i < Description.Length; i++)
            {
                legendString += "-";
            }
            legendString += "\r\n";
            return legendString;
        }

        /// <summary>
        /// Copies legend object
        /// </summary>
        /// <returns></returns>
        public abstract Legend Copy();

        /// <summary>
        /// Creates a short string representation of this legend object depending on the kind of Legend
        /// </summary>
        /// <returns>one or more lines</returns>
        public abstract override string ToString();

        /// <summary>
        /// Creates a long string representation of this legend object depending on the kind of Legend
        /// </summary>
        /// <returns>one or more lines</returns>
        public abstract string ToLongString();

    }

}
