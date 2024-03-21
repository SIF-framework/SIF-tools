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
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages.Files
{
    public class ConstantIDFPackageFile : IDFPackageFile
    {
        public float ConstantValue { get; set; }

        public override IDFFile IDFFile
        {
            get
            {
                return idffile;
            }
            set
            {
                // ignore;
            }
        }

        public ConstantIDFPackageFile(Package package, float constantValue, int ilay, float fct, float imp, StressPeriod stressPeriod = null)
            : base(package, constantValue.ToString(), ilay, fct, imp, stressPeriod)
        {
            this.ConstantValue = constantValue;
            this.idffile = new ConstantIDFFile(constantValue);
            //if (package.Model != null)
            //{
            //    this.idffile = this.idffile.ClipIDF(package.Model.GetExtent());
            //}
            if (stressPeriod == null)
            {
                this.idffile.Metadata = new Metadata("Constant file for " + package.Key + ", layer " + (ilay + 1));
            }
            else
            {
                this.idffile.Metadata = new Metadata("constant " + package.Key + "-packagefile, layer " + (ilay + 1) + ", " + stressPeriod.ToString());
            }
        }

        public override PackageFile Copy(string copiedFilename)
        {
            IDFPackageFile copiedIDFPackageFile = new ConstantIDFPackageFile(this.Package, ConstantValue, this.ILAY, this.FCT, this.IMP, this.StressPeriod);
            return copiedIDFPackageFile;
        }

        public override PackageFile Clip(Extent extent)
        {
            ConstantIDFPackageFile clippedIDFPackageFile = new ConstantIDFPackageFile(this.Package, this.ConstantValue, this.ILAY, this.FCT, this.IMP, this.StressPeriod);
            clippedIDFPackageFile.idffile = idffile.ClipIDF(extent);
            return clippedIDFPackageFile;
        }

        public override void ReadFile(bool useLazyLoading = false, Log log = null, int logIndentLevel = 0, Extent extent = null)
        {
            //  nothing to do
        }

        //public override void WriteFile(Metadata metadata = null, Log log = null, int logIndentLevel = 0)
        //{
        //    if (metadata != null)
        //    {
        //       idffile.WriteFile(metadata);
        //    }
        //    else
        //    {
        //        idffile.WriteFile();
        //    }
        //}

        public override bool Exists()
        {
            return true;
        }
    }
}
