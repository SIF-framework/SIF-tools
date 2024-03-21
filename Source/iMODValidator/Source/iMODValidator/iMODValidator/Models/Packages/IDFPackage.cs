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
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public abstract class IDFPackage : Package
    {
        public IDFPackage()
            : base()
        {
        }

        public IDFPackage(string packageKey) : base(packageKey)
        {
        }

        /// <summary>
        /// Retrieves the IDF-file for the given entry (entryIdx, zero-based), 
        /// the given part (partIdx) and the given timestep (kper).
        /// For missing (-1) packagedefinitions null will be returned.
        /// </summary>
        /// <param name="entryIdx"></param>
        /// <param name="partIdx"></param>
        /// <param name="kper"></param>
        /// <returns></returns>
        public IDFFile GetIDFFile(int entryIdx, int partIdx = 0, int kper = 0, bool isEvaluated = true)
        {
            IDFPackageFile idfPackageFile = GetIDFPackageFile(entryIdx, partIdx, kper);
            IDFFile idfFile = null;
            if (idfPackageFile != null)
            {
                idfFile = idfPackageFile.IDFFile;
                if (isEvaluated && (idfFile != null))
                {
                    if (!idfPackageFile.IMP.Equals(0) || !idfPackageFile.FCT.Equals(1.0))
                    {
                        // Evaluate fct and imp values 
                        IDFFile evaluatedIDFFile = idfFile.CopyIDF(idfFile.Filename); // should another filename be used?
                        evaluatedIDFFile.Multiply(idfPackageFile.FCT);
                        evaluatedIDFFile.Add(idfPackageFile.IMP);
                        idfFile = evaluatedIDFFile;
                    }
                }
            }
            return idfFile;
        }

        /// Retrieves the IDF-packagefile for the given entry (entryIdx, zero-based), 
        /// the given part (partIdx) and the given timestep (kper).
        /// For missing (-1) packagedefinitions null will be returned.
        public IDFPackageFile GetIDFPackageFile(int entryIdx, int partIdx = 0, int kper = 0)
        {
            PackageFile packageFile = GetPackageFile(entryIdx, partIdx, kper);
            IDFPackageFile idfPackageFile = null;
            if (packageFile is IDFPackageFile)
            {
                idfPackageFile = (IDFPackageFile)packageFile;
            }
            return idfPackageFile;
        }

        /// <summary>
        /// Checks if the combination of IDF-files in the given checkList is present
        /// in this package before the given checkKPER
        /// </summary>
        /// <param name="package"></param>
        /// <param name="checkKPER"></param>
        /// <param name="checkFileList"></param>
        /// <returns></returns>
        public bool IsFileListPresent(List<IDFFile> checkFileList, int minKPER = 1, int checkKPER = 0)
        {
            foreach (IDFFile checkFile in checkFileList)
            {
                if (!IsFilePresent(checkFile, minKPER, checkKPER))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the given IDF-checkfile is present before the given checkKPER
        /// </summary>
        /// <param name="package"></param>
        /// <param name="maxKPER"></param>
        /// <returns></returns>
        public bool IsFilePresent(IDFFile checkFile, int minKPER = 1, int maxKPER = 0)
        {
            if (maxKPER == 0)
            {
                maxKPER = packageFiles.Length;
            }

            if (checkFile != null)
            {
                for (int kper = 1; kper < maxKPER; kper++)
                {
                    if (GetEntryCount(kper) > 0)
                    {
                        for (int layerIdx = 0; layerIdx < GetEntryCount(kper); layerIdx++)
                        {
                            for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
                            {
                                PackageFile packageFile = GetPackageFile(layerIdx, partIdx, kper);
                                if (packageFile != null)
                                {
                                    if (packageFile.IMODFile != null)
                                    {
                                        if (packageFile.IMODFile.Filename.ToLower().Equals(checkFile.Filename.ToLower()))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public float GetMinValue(int minEntryIdx, int maxEntryIdx, int kper = 1)
        {
            float minValue = float.MaxValue;
            for (int entryIdx = minEntryIdx; entryIdx < maxEntryIdx; entryIdx++)
            {
                for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
                {
                    IDFPackageFile packageFile = GetIDFPackageFile(entryIdx, partIdx, kper);
                    if (packageFile != null)
                    {
                        if (packageFile.IDFFile != null)
                        {
                            if (packageFile.IDFFile.MinValue < minValue)
                            {
                                minValue = packageFile.IDFFile.MinValue;
                            }
                        }
                    }
                }
            }
            return minValue.Equals(float.MaxValue) ? float.NaN : minValue;
        }

        public float GetMaxValue(int minEntryIdx, int maxEntryIdx, int kper = 1)
        {
            float maxValue = float.MinValue;
            for (int entryIdx = minEntryIdx; entryIdx < maxEntryIdx; entryIdx++)
            {
                for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
                {
                    IDFPackageFile packageFile = GetIDFPackageFile(entryIdx, partIdx, kper);
                    if (packageFile != null)
                    {
                        if (packageFile.IDFFile != null)
                        {
                            if (packageFile.IDFFile.MaxValue > maxValue)
                            {
                                maxValue = packageFile.IDFFile.MaxValue;
                            }
                        }
                    }
                }
            }
            return maxValue.Equals(float.MinValue) ? float.NaN : maxValue;
        }

        public abstract override Package CreateInstance();
    }
}
