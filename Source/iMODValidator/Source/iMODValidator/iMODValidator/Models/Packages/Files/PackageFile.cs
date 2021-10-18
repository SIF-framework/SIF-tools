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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages.Files
{
    /// <summary>
    /// Base class for model packages
    /// </summary>
    public abstract class PackageFile : IEquatable<PackageFile>
    {
        public int ilay;
        public float fct;
        public float imp;
        protected string fname;
        public virtual string FName
        {
            get { return fname; }
            set
            {
                fname = value;
                if (IMODFile != null)
                {
                    IMODFile.Filename = fname;
                }
            }
        }
        public StressPeriod stressPeriod;
        public Package package;

        /// <summary>
        /// The actual iMOD-file that is referenced by this PackageFile
        /// </summary>
        public abstract IMODFile IMODFile
        {
            get;
        }

        public PackageFile()
        {
        }

        public PackageFile(Package package, string fname, int ilay, float fct, float imp, StressPeriod stressPeriod = null)
        {
            this.package = package;
            this.ilay = ilay;
            this.fct = fct;
            this.imp = imp;
            this.FName = fname;
            this.stressPeriod = stressPeriod;
        }

        public abstract bool Exists();
        public abstract void ReadFile(bool useLazyLoading = false, Log log = null, int logIndentLevel = 0, Extent extent = null);
        public abstract void WriteFile(Metadata metadata = null, Log log = null, int logIndentLevel = 0);

        public abstract void ReleaseMemory(bool isMemoryCollected = true);

        public abstract PackageFile Copy(string copiedFilename);
        public abstract PackageFile Clip(Extent extent);

        public abstract Metadata CreateMetadata(Model model);

        public virtual bool Equals(PackageFile other)
        {
            return HasEqualContent(other, null, true);
        }

        /// <summary>
        /// Checks if packagefile definition is equal (apart from filename directory)
        /// I.e. object equalility or equal ilay and equal stressperiod, result in true
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool IsCorresponding(PackageFile other)
        {
            if (other.GetType().Equals(this.GetType()))
            {
                if (other == this)
                {
                    return true;
                }
                else
                {
                    // Check that packagefile definition is equal (apart from filename directory)

                    // First check ilay
                    if (other.ilay.Equals(this.ilay))
                    {
                        // Now check stressperiod (which can be null)
                        return (((other.stressPeriod == null) && (this.stressPeriod == null))
                            || ((other.stressPeriod != null) && (this.stressPeriod != null) && other.stressPeriod.Equals(this.stressPeriod)));
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        public virtual bool HasEqualContent(PackageFile other, Extent extent, bool isNoDataCompared)
        {
            if (IsCorresponding(other))
            {
                // Now check value-related properties
                if ((other.fct.Equals(this.fct)) && (other.imp.Equals(this.imp)))
                {
                    if (other.FName.ToLower().Equals(this.FName.ToLower()))
                    {
                        return true;
                    }
                    else
                    {
                        // If packagefile definition is the same, actually compare idffile contents
                        IMODFile thisIMODFile = this.IMODFile;
                        IMODFile otherIMODFile = other.IMODFile;
                        if ((thisIMODFile != null) && (other.IMODFile != null))
                        {
                            return thisIMODFile.HasEqualContent(otherIMODFile, extent, isNoDataCompared);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a packagefile exists with equal filename and definition (apart from directory).
        /// This method checks for object equalility or equal filename, ilay and equal stressperiod.
        /// </summary>
        /// <param name="packageFiles"></param>
        /// <returns></returns>
        public virtual bool HasPackageFileWithEqualFilename(List<PackageFile> packageFiles)
        {
            return (RetrievePackageFileWithEqualFilename(packageFiles) != null);
        }

        /// <summary>
        /// Retrieves first packagefile with equal filename and definition (apart from directory)
        /// This method checks for object equalility or equal filename, ilay and equal stressperiod
        /// </summary>
        /// <param name="packageFiles"></param>
        /// <returns></returns>
        public virtual PackageFile RetrievePackageFileWithEqualFilename(List<PackageFile> packageFiles)
        {
            foreach (PackageFile packageFile in packageFiles)
            {
                if (Path.GetFileName(this.fname).ToLower().Equals(Path.GetFileName(packageFile.fname).ToLower())
                    && IsCorresponding(packageFile))
                {
                    return packageFile;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a packagefile exists with equal definition (apart from filename and directory).
        /// This method checks for object equalility or equal ilay and equal stressperiod.
        /// </summary>
        /// <param name="packageFiles"></param>
        /// <returns></returns>
        public virtual bool HasCorrespondingPackageFile(List<PackageFile> packageFiles)
        {
            return (RetrieveCorrespondingPackageFile(packageFiles) != null);
        }

        /// <summary>
        /// Retrieves first packagefile with equal definition (apart from filename and directory)
        /// This method checks for object equalility or equal ilay and equal stressperiod
        /// </summary>
        /// <param name="packageFiles"></param>
        /// <returns></returns>
        public virtual PackageFile RetrieveCorrespondingPackageFile(List<PackageFile> packageFiles)
        {
            foreach (PackageFile packageFile in packageFiles)
            {
                if (IsCorresponding(packageFile))
                {
                    return packageFile;
                }
            }
            return null;
        }
    }
}
