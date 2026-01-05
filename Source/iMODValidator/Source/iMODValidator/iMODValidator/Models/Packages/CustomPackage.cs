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

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public class CustomPackage : Package
    {
        protected int maxPartCount;
        protected string[] partAbbreviations = new string[0];

        public CustomPackage(string packageKey)
            : this(packageKey, new string[0], 1, new string[0])
        {
        }

        public CustomPackage(string packageKey, string[] alternativeKeys)
            : this(packageKey, alternativeKeys, 1, new string[0])
        {
        }

        public CustomPackage(string packageKey, string[] alternativeKeys, int maxPartCount, string[] partAbbreviations)
            : base(packageKey)
        {
            if (alternativeKeys != null)
            {
                this.alternativeKeys.AddRange(alternativeKeys);
            }
            this.maxPartCount = maxPartCount;
            this.partAbbreviations = partAbbreviations;
        }

        public override int MaxPartCount
        {
            get { return maxPartCount; }
        }

        public override string[] PartAbbreviations
        {
            get { return partAbbreviations; }
        }

        public override Package CreateInstance()
        {
            throw new Exception("CustomPackage cannot be instantiated with CreateInstance");
        }
    }
}
