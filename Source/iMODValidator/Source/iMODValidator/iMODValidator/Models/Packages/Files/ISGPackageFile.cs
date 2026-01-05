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
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMOD.Utils;
using System;
using System.IO;

namespace Sweco.SIF.iMODValidator.Models.Packages.Files
{
    public class ISGPackageFile : PackageFile
    {
        protected ISGFile isgFile;
        public virtual ISGFile ISGFile
        {
            get
            {
                // use lazy reading
                if (isgFile == null)
                {
                    if (File.Exists(FName))
                    {
                        ReadFile(true, null, 0, Package.Model.GetExtent());
                    }
                    else
                    {
                        // leave null
                    }
                }
                return isgFile;
            }
            set
            {
                isgFile = value;
                imodFile = value;
            }
        }

        public override string FName
        {
            get
            {
                return fname;
            }
            set
            {
                fname = Path.Combine(Path.GetDirectoryName(value), Path.GetFileNameWithoutExtension(value) + ".ISG");
                if (isgFile != null)
                {
                    isgFile.Filename = fname;
                }
            }
        }

        protected IMODFile imodFile;
        public override IMODFile IMODFile
        {
            get { return imodFile; }
        }

        public ISGPackageFile(Package package, string fname, int ilay, float fct, float imp, StressPeriod stressPeriod = null)
            : base(package, fname, ilay, fct, imp, stressPeriod)
        {
        }

        public override bool Exists()
        {
            return File.Exists(FName);
        }

        public override void ReadFile(bool useLazyLoading = false, Log log = null, int logIndentLevel = 0, Extent extent = null)
        {
            isgFile = ISGFile.ReadFile(FName, useLazyLoading, log, logIndentLevel);
            if ((extent != null) && (isgFile.Extent != null) && !extent.Contains(isgFile.Extent))
            {
                string filename = isgFile.Filename;
                isgFile = isgFile.ClipISG(extent);
                isgFile.Filename = filename;
            }
            imodFile = isgFile;
        }

        public override void WriteFile(Metadata metadata = null, Log log = null, int logIndentLevel = 0)
        {
            // For now just write GEN-file with ISG-segments
            GENFile isgGENFile = IMODUtils.ConvertISGToGEN(isgFile);
            if (log != null)
            {
                log.AddWarning("ISG", isgFile.Filename, "ISG-file comparison is currently only performed on ISG-segments. Difference is written as GEN-file.", logIndentLevel);
            }
            isgGENFile.WriteFile(Path.ChangeExtension(isgFile.Filename, "GEN"), metadata);
            this.imodFile = isgGENFile;

            // isgFile.WriteFile(metadata);
        }

        public override Metadata CreateMetadata(Model model)
        {
            Metadata metadata = model.CreateMetadata();
            metadata.IMODFilename = FName;
            metadata.Type = "ISG";
            return metadata;
        }

        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (isgFile != null)
            {
                isgFile = null;
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        public override PackageFile Copy(string copiedFilename)
        {
            ISGPackageFile copiedISGPackageFile = new ISGPackageFile(this.Package, this.FName, this.ILAY, this.FCT, this.IMP, this.StressPeriod);
            copiedISGPackageFile.isgFile = null;
            if (isgFile != null)
            {
                copiedISGPackageFile.isgFile = isgFile.CopyISG(copiedFilename);
                copiedISGPackageFile.imodFile = copiedISGPackageFile.isgFile;
            }
            return copiedISGPackageFile;
        }

        public override PackageFile Clip(Extent extent)
        {
            ISGPackageFile clippedISGPackageFile = new ISGPackageFile(this.Package, this.FName, this.ILAY, this.FCT, this.IMP, this.StressPeriod);
            clippedISGPackageFile.isgFile = isgFile.ClipISG(extent);
            return clippedISGPackageFile;
        }

        public override PackageFile CreateDifferenceFile(PackageFile comparedPackageFile, bool useLazyLoading, string OutputFoldername, ComparisonMethod comparisonMethod, float noDataCalculationValue, Log log, int indentLevel = 0)
        {
            return CreateDifferenceFile(comparedPackageFile, useLazyLoading, OutputFoldername, null, comparisonMethod, noDataCalculationValue, log, indentLevel);
        }

        public ISGFile CreateDifferenceFile(ISGFile otherISGFile, string outputPath, float noDataCalculationValue, Extent comparedExtent = null)
        {
            return this.ISGFile.CreateDifferenceFile(otherISGFile, outputPath, noDataCalculationValue, comparedExtent);
        }

        public override PackageFile CreateDifferenceFile(PackageFile comparedPackageFile, bool useLazyLoading, string OutputFoldername, Extent extent, ComparisonMethod comparisonMethod, float noDataCalculationValue, Log log, int indentLevel = 0)
        {
            if (!this.Exists())
            {
                log.AddInfo("Difference cannot be created, input file doesn't exist: " + this.fname, indentLevel);
                return null;
            }
            if (!comparedPackageFile.Exists())
            {
                log.AddInfo("Difference cannot be created, input file doesn't exist: " + comparedPackageFile.FName, indentLevel);
                return null;
            }

            if (comparedPackageFile is ISGPackageFile)
            {
                ISGPackageFile diffISGPackageFile = new ISGPackageFile(this.Package, this.FName, this.ILAY, this.FCT, this.IMP, this.StressPeriod);
                diffISGPackageFile.isgFile = CreateDifferenceFile(((ISGPackageFile)comparedPackageFile).isgFile, OutputFoldername, noDataCalculationValue, extent);
                if (diffISGPackageFile.isgFile != null)
                {
                    diffISGPackageFile.isgFile.UseLazyLoading = useLazyLoading;
                    return diffISGPackageFile;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                log.AddWarning("Source type is " + this.GetType().Name + ", compared type is " + comparedPackageFile.GetType().Name + ", files cannot be compared ", indentLevel);
                return null;
            }
        }
    }
}
