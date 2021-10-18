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
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public abstract class IPFPackage : Package
    {
        public IPFPackage()
            : base()
        {
        }

        public IPFPackage(string packageKey)
            : base(packageKey)
        {
        }

        public IPFFile GetIPFFile(int entryIdx, int partIdx = 0, int kper = 1)
        {
            IPFPackageFile idfPackageFile = GetIPFPackageFile(entryIdx, partIdx, kper);
            IPFFile idfFile = null;
            if (idfPackageFile != null)
            {
                idfFile = idfPackageFile.IPFFile;
            }
            return idfFile;
        }

        public IPFPackageFile GetIPFPackageFile(int entryIdx, int partIdx = 0, int kper = 1)
        {
            PackageFile packageFile = GetPackageFile(entryIdx, partIdx, kper);
            IPFPackageFile idfPackageFile = null;
            if (packageFile is IPFPackageFile)
            {
                idfPackageFile = (IPFPackageFile)packageFile;
            }
            return idfPackageFile;
        }

        public int GetIPFPackageFileCount(int kper = 1)
        {
            return GetEntryCount(kper);
        }
    }
}
