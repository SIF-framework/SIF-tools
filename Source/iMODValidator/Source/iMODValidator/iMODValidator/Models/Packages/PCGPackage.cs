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
    public class PCGPackage : Package
    {
        public static string DefaultKey
        {
            get { return "PCG"; }
        }

        public PCGPackage(string packageKey) : base(packageKey)
        {
        }

        public override Package CreateInstance()
        {
            return new PCGPackage(key);
        }

        /// <summary>
        /// Read/parse entries in PRJ-file for PCG-package. 
        /// Note: when started, the second line of this package must not yet have been read; after finishing, first line from the next package will not yet have been read.
        /// </summary>
        /// <param name="prjFile"></param>
        /// <param name="maxKPER"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public override void ParsePRJFilePackageFiles(PRJFile prjFile, int maxKPER, Log log, int logIndentLevel)
        {
            // Just read line with PCG-settings; ignore settings, these are currently not used/checked by the iMODValidator
            log.AddInfo("Skipping settings of " + Key + "-package ...", logIndentLevel);
            string wholeLine = prjFile.ReadLine();

            // Keep skipping lines with PCG-settings
            while (prjFile.PeekLine().Contains("="))
            {
                wholeLine = prjFile.ReadLine();
            }
        }
    }
}
