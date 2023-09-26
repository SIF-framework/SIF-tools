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
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IMF
{
    /// <summary>
    /// For storing iMOD-files in Maps section of IMF-file
    /// </summary>
    public class Map
    {
        /// <summary>
        /// Flags for IDF Map-files as properties in IMF-file. Check iMOD-manual for details.
        /// </summary>
        public enum PRFTypeFlag
        {
            None,
            Active,
            Line,
            Point,
            Fill,
            Clr,
            Tinv,
            Legend,
        }

        protected const int PRFTYPE_NONE = 0;
        protected const int PRFTYPE_ACT = 1;
        protected const int PRFTYPE_LINE = 2;
        protected const int PRFTYPE_POINT = 4;
        protected const int PRFTYPE_FILL = 8;
        protected const int PRFTYPE_CLR = 16;
        protected const int PRFTYPE_Tinv = 32;
        protected const int PRFTYPE_LEG = 64;

        /// <summary>
        /// Filename of Map file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Legend for displaying Map file
        /// </summary>
        public Legend Legend { get; set; }

        /// <summary>
        /// Specifies if file is selected
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// SCOLOR-value in IMF-file
        /// </summary>
        public long SColor { get; set; }

        /// <summary>
        /// PRFType in IMFFile as a combination of PRFTypeFlag-values
        /// </summary>
        public int PRFType { get; set; }

        /// <summary>
        /// Hidden constructor
        /// </summary>
        protected Map()
        {
            Filename = null;
            Legend = null;
            Selected = false;
            SColor = 4410933;
        }

        /// <summary>
        /// Creates a new Map instance for specified legend and filename
        /// </summary>
        /// <param name="legend"></param>
        /// <param name="filename"></param>
        public Map(Legend legend, string filename)
        {
            Legend = legend;
            Filename = filename;
        }

        /// <summary>
        /// Remove all existing PRFType flags
        /// </summary>
        public void ClearPRFTypeFlags()
        {
            PRFType = PRFTYPE_NONE;
        }

        /// <summary>
        /// Set PRFType to specified integer value (check PRFType flag for valid (unmerged) values)
        /// </summary>
        public void SetPRFType(int prfType)
        {
            PRFType = prfType;
        }

        /// <summary>
        /// Set PRFType to specified PRFTypeFlag
        /// </summary>
        public void SetPRFType(PRFTypeFlag prfTypeFlag)
        {
            PRFType = PRFTypeToInt(prfTypeFlag);
        }

        /// <summary>
        /// Add specified PRFTypeFlag to existing PRFType 
        /// </summary>
        public void AddPRFTypeFlag(PRFTypeFlag prfTypeFlag)
        {
            PRFType |= PRFTypeToInt(prfTypeFlag);
        }
        
        /// <summary>
        /// Convert PRFTypeFlag enum to integer as used in IMF-file
        /// </summary>
        /// <param name="prfType"></param>
        /// <returns></returns>
        public static int PRFTypeToInt(PRFTypeFlag prfType)
        {
            switch (prfType)
            {
                case PRFTypeFlag.None:
                    return PRFTYPE_NONE;
                case PRFTypeFlag.Active:
                    return PRFTYPE_ACT;
                case PRFTypeFlag.Line:
                    return PRFTYPE_LINE;
                case PRFTypeFlag.Point:
                    return PRFTYPE_POINT;
                case PRFTypeFlag.Fill:
                    return PRFTYPE_FILL;
                case PRFTypeFlag.Clr:
                    return PRFTYPE_CLR;
                case PRFTypeFlag.Tinv:
                    return PRFTYPE_Tinv;
                case PRFTypeFlag.Legend:
                    return PRFTYPE_LEG;
                default:
                    throw new Exception("Undefined PRFType: " + prfType);
            }
        }
    }
}
