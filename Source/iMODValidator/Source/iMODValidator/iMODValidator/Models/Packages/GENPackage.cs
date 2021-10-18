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
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public abstract class GENPackage : Package
    {
        public GENPackage()
            : base()
        {
        }

        public GENPackage(string packageKey)
            : base(packageKey)
        {
        }

        public GENFile GetGENFile(int entryIdx, int partIdx = 0, int kper = 1)
        {
            GENPackageFile idfPackageFile = GetGENPackageFile(entryIdx, partIdx, kper);
            GENFile idfFile = null;
            if (idfPackageFile != null)
            {
                idfFile = idfPackageFile.GENFile;
            }
            return idfFile;
        }

        public GENPackageFile GetGENPackageFile(int entryIdx, int partIdx = 0, int kper = 1)
        {
            PackageFile packageFile = GetPackageFile(entryIdx, partIdx, kper);
            GENPackageFile idfPackageFile = null;
            if (packageFile is GENPackageFile)
            {
                idfPackageFile = (GENPackageFile)packageFile;
            }
            return idfPackageFile;
        }

        public int GetGENPackageFileCount(int kper = 1)
        {
            return GetEntryCount(kper);
        }
    }
}
