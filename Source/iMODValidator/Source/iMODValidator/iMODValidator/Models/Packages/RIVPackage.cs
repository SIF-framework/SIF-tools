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
    public class RIVPackage : IDFPackage
    {
        public const int ConductancePartIdx = 0;
        public const int StagePartIdx = 1;
        public const int BottomPartIdx = 2;
        public const int InfFactorPartIdx = 3;
        public override string[] PartAbbreviations
        {
            get { return new string[] { "COND", "STAGE", "BOTTOM", "INFFCT" }; }
        }

        public static string DefaultKey
        {
            get { return "RIV"; }
        }

        public override int MaxPartCount
        {
            get
            {
                return 4;   // RIV-package has 4 files per entry
            }
        }

        public RIVPackage(string packageKey)
            : base(packageKey)
        {
            alternativeKeys.AddRange(new string[] { "RIVERS" });
        }

        public override Package CreateInstance()
        {
            return new RIVPackage(key);
        }
    }
}
