// iMODWBalFormat is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODWBalFormat.
// 
// iMODWBalFormat is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODWBalFormat is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODWBalFormat. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iMODWBalFormat
{
    public class WBalPost : IComparable<WBalPost>
    {
        public enum FluxPosition
        {
            Unknown,
            Right,
            Front,
            Upper,
            Lower,
            Inside
        }

        public const string ExternTypeString = "Extern";
        public const string InternTypeString = "Intern";

        public string Name;
        public float In;
        public float Out;
        public float Sum;

        public WBalPost(string name, float inValue, float outValue)
        {
            this.Name = name;
            this.In = inValue;
            this.Out = outValue;
            this.Sum = inValue + outValue;
        }

        public int CompareTo(WBalPost other)
        {
            if (this.GetRank().Equals(other.GetRank()))
            {
                return this.Name.CompareTo(other.Name);
            }
            else
            {
                return (this.GetRank() - other.GetRank());
            }
        }

        private int GetRank()
        {
            // Check for strings of iMOD 4.0 and/or iMOD 4.1 and higher
            if (Name.StartsWith("RECHARGE") || Name.StartsWith("BDGRCH"))
            {
                return 1;
            }
            else if (Name.StartsWith("OVERLAND") || Name.StartsWith("BDGOLF"))
            {
                return 2;
            }
            else if (Name.StartsWith("DRAIN") || Name.StartsWith("BDGDRN"))
            {
                return 3;
            }
            else if (Name.StartsWith("RIV") || Name.StartsWith("BDGRIV"))
            {
                return 4;
            }
            else if (Name.StartsWith("WEL") || Name.StartsWith("BDGWEL"))
            {
                return 5;
            }
            else if (Name.StartsWith("CONST") || Name.StartsWith("BDGBND"))
            {
                return 6;
            }
            else if (Name.Contains("RIGHT") || Name.StartsWith("BDGFRF"))
            {
                return 7;
            }
            else if (Name.Contains("FRONT") || Name.StartsWith("BDGFFF"))
            {
                return 8;
            }
            else if (Name.Contains("UPPER") || Name.StartsWith("BDGFTF"))
            {
                return 9;
            }
            else if (Name.Contains("LOWER") || Name.StartsWith("BDGFLF"))
            {
                return 10;
            }
            else
            {
                return 8;
            }
        }

        public string GetTypeString()
        {
            // Check for strings of iMOD 4.0 and/or iMOD 4.1 and higher
            if (Name.StartsWith("RECHARGE") || Name.StartsWith("BDGRCH"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("OVERLAND") || Name.StartsWith("BDGOLF"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("DRAIN") || Name.StartsWith("BDGDRN"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("RIV") || Name.StartsWith("BDGRIV"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("RIV") || Name.StartsWith("BDGISG"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("WEL") || Name.StartsWith("BDGWEL"))
            {
                return ExternTypeString;
            }
            else if (Name.StartsWith("CONST") || Name.StartsWith("BDGBND"))
            {
                return ExternTypeString;
            }
            else if (Name.Contains("FRONT") || Name.StartsWith("BDGFFF"))
            {
                return ExternTypeString;
            }
            else if (Name.Contains("RIGHT") || Name.StartsWith("BDGFRF"))
            {
                return ExternTypeString;
            }
            else if (Name.Contains("LOWER") || Name.StartsWith("BDGFLF"))
            {
                return InternTypeString;
            }
            else if (Name.Contains("UPPER") || Name.StartsWith("BDGFTF"))
            {
                return InternTypeString;
            }
            else
            {
                return "Unknown";
            }
        }

        public FluxPosition GetFluxPosition()
        {
            // Check for strings of iMOD 4.0 and/or iMOD 4.1 and higher
            if (Name.StartsWith("RECHARGE") || Name.StartsWith("BDGRCH"))
            {
                return FluxPosition.Upper;
            }
            else if (Name.StartsWith("OVERLAND") || Name.StartsWith("BDGOLF"))
            {
                return FluxPosition.Upper;
            }
            else if (Name.StartsWith("DRAIN") || Name.StartsWith("BDGDRN"))
            {
                return FluxPosition.Inside;
            }
            else if (Name.StartsWith("RIV") || Name.StartsWith("BDGRIV"))
            {
                return FluxPosition.Inside;
            }
            else if (Name.StartsWith("WEL") || Name.StartsWith("BDGWEL"))
            {
                return FluxPosition.Inside;
            }
            else if (Name.StartsWith("CONST") || Name.StartsWith("BDGBND"))
            {
                return FluxPosition.Inside;
            }
            else if (Name.Contains("FRONT") || Name.StartsWith("BDGFFF"))
            {
                return FluxPosition.Front;
            }
            else if (Name.Contains("RIGHT") || Name.StartsWith("BDGFRF"))
            {
                return FluxPosition.Right;
            }
            else if (Name.Contains("LOWER") || Name.StartsWith("BDGFLF"))
            {
                return FluxPosition.Lower;
            }
            else if (Name.Contains("UPPER") || Name.StartsWith("BDGFTF"))
            {
                return FluxPosition.Upper;
            }
            else
            {
                return FluxPosition.Unknown;
            }
        }
    }
}
