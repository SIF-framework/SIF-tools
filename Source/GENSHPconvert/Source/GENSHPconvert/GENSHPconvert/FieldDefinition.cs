// GENSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENSHPconvert.
// 
// GENSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using EGIS.ShapeFileLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENSHPconvert
{
    public class FieldDefinition
    {
        public const int DefaultDecimalCount = 3;

        public string Name;
        public DbfFieldType Type;
        public int Length;
        public int Decimalcount;

        public FieldDefinition(string name, DbfFieldType type)
        {
            this.Name = name;
            this.Type = type;
            this.Length = 0;
            this.Decimalcount = DefaultDecimalCount;
        }

        public FieldDefinition(string name, DbfFieldType type, int length)
        {
            this.Name = name;
            this.Type = type;
            this.Length = length;
            this.Decimalcount = DefaultDecimalCount;
        }

        public FieldDefinition(string name, DbfFieldType type, int length, int decimalcount)
        {
            this.Name = name;
            this.Type = type;
            this.Length = length;
            this.Decimalcount = decimalcount;
        }

        public DbfFieldDesc ToDbfFieldDesc()
        {
            DbfFieldDesc fieldDesc = new DbfFieldDesc();
            fieldDesc.FieldName = Name;
            fieldDesc.FieldType = Type;
            fieldDesc.DecimalCount = Decimalcount;
            fieldDesc.FieldLength = Length;

            return fieldDesc;
        }
    }
}
