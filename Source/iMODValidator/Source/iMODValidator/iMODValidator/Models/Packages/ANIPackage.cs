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
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Models.Runfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public class ANIPackage : Package
    {
        public const int FactorPartIdx = 0;
        public const int AnglePartIdx = 1;
        public override string[] PartAbbreviations
        {
            get { return new string[] { "FACTOR", "HOEK" }; }
        }
        public static string DefaultKey
        {
            get { return "ANI"; }
        }

        public ANIPackage(string packageKey)
            : base(packageKey)
        {
            alternativeKeys.AddRange(new string[] { "ANISOTROPIE" });
        }

        public override int MaxPartCount
        {
            get
            {
                return 2;   // ANI-package has 1 (GEN) or 2 (angle and factor) files per entry
            }
        }

        /// <summary>
        /// Retrieve file extensions that are defined for this package and number of entries per extension
        /// </summary>
        /// <returns>Dictionary with extenion-count pairs; use lower case extension without dot, or null if no variable definitions are needed</returns>
        public override Dictionary<string, int> GetDefinedExtensions()
        {
            Dictionary<string, int> extensionDictionary = new Dictionary<string, int>();

            extensionDictionary.Add(".idf", 2);
            extensionDictionary.Add(".gen", 1);

            return extensionDictionary;
        }

        public override Package CreateInstance()
        {
            return new ANIPackage(key);
        }
    }
}
