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
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages.Files
{
    public class IPFPackageFile : PackageFile
    {
        protected IPFFile ipfFile;
        public virtual IPFFile IPFFile
        {
            get
            {
                // use lazy reading
                if (ipfFile == null)
                {
                    if (File.Exists(FName))
                    {
                        ReadFile(true, null, 0, package.Model.GetExtent());
                    }
                    else
                    {
                        // leave null
                    }
                }
                return ipfFile;
            }
            set { ipfFile = value; }
        }
        public override string FName
        {
            get
            {
                return fname;
            }
            set
            {
                fname = Path.Combine(Path.GetDirectoryName(value), Path.GetFileNameWithoutExtension(value) + ".IPF");
                if (IMODFile != null)
                {
                    ipfFile.Filename = fname;
                }
            }
        }
        public override IMODFile IMODFile
        {
            get { return IPFFile; }
        }

        public IPFPackageFile(Package package, string fname, int ilay, float fct, float imp, StressPeriod stressPeriod = null)
            : base(package, fname, ilay, fct, imp, stressPeriod)
        {
        }

        public override void ReadFile(bool useLazyLoading = false, Log log = null, int logIndentLevel = 0, Extent extent = null)
        {
            if (log != null)
            {
                log.AddMessage(LogLevel.Trace, "Reading IPF-file " + FName + " ...", logIndentLevel);
            }
            IPFFile.UserDefinedListSeperators = iMODValidatorSettingsManager.Settings.DefaultIPFListSeperators;
            ipfFile = IPFFile.ReadFile(FName, useLazyLoading, log, logIndentLevel);
        }

        public override bool Exists()
        {
            return File.Exists(FName);
        }

        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (ipfFile != null)
            {
                ipfFile = null;
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        public override PackageFile Copy(string copiedFilename)
        {
            IPFPackageFile copiedIPFPackageFile = new IPFPackageFile(this.package, this.FName, this.ilay, this.fct, this.imp, this.stressPeriod);
            copiedIPFPackageFile.ipfFile = IPFFile.CopyIPF(copiedFilename);
            return copiedIPFPackageFile;
        }

        public override PackageFile Clip(Extent extent)
        {
            IPFPackageFile clippedIPFPackageFile = new IPFPackageFile(this.package, this.FName, this.ilay, this.fct, this.imp, this.stressPeriod);
            clippedIPFPackageFile.ipfFile = IPFFile.ClipIPF(extent);
            return clippedIPFPackageFile;
        }

        public override void WriteFile(Metadata metadata = null, Log log = null, int logIndentLevel = 0)
        {
            ipfFile.WriteFile(metadata);
        }

        public override Metadata CreateMetadata(Model model)
        {
            Metadata metadata = model.CreateMetadata();
            metadata.IMODFilename = FName;
            metadata.Type = "IPF";
            return metadata;
        }
    }
}
