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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMODValidator.Models.Runfiles;
using Sweco.SIF.iMODValidator.Models.Packages.Files;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public abstract class Package
    {
        protected CultureInfo englishCultureInfo = null;
        protected const int MaxLoggedMisingFilenameCount = 100;

        protected string key;
        public string Key
        {
            get { return this.key; }
            set { this.key = value; }
        }
        protected List<string> alternativeKeys;
        public List<string> AlternativeKeys
        {
            get { return this.alternativeKeys; }
            set { this.alternativeKeys = value; }
        }

        protected Model model;
        public Model Model
        {
            get { return model; }
            set { model = value; }
        }
        protected bool isActive;
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        /// <summary>
        /// Abbreviations for each of the individual parts/files of this package
        /// </summary>
        public virtual string[] PartAbbreviations
        {
            get { return new string[] { key }; }
        }

        /// <summary>
        /// 3D-array with all files for this package
        /// Array-dimensions are defined as: kper (period); entryIdx (starting with 0); partIdx (index within entry)
        /// </summary>
        protected PackageFile[][][] packageFiles;

        /// <summary>
        /// The maximum number of parts/files per entry for this package
        /// </summary>
        public virtual int MaxPartCount
        {
            get { return 1; }   // this is the default
        }

        public object PackageFileFactory { get; private set; }

        public Package()
        {
            englishCultureInfo = new CultureInfo("en-GB", false);
        }

        public Package(string packageKey)
        {
            this.key = packageKey;
            this.alternativeKeys = new List<string>();
            englishCultureInfo = new CultureInfo("en-GB", false);

            // Check if abbreviations are correctly defined
            if (PartAbbreviations.Length != MaxPartCount)
            {
                throw new Exception("PartAbbreviations has length other than MaxPartCount, check implementation of package " + key);
            }
        }

        public Package(string packageKey, Log log) : this(packageKey)
        {
        }

        public abstract Package CreateInstance();

        /// <summary>
        /// Retrieves dictionary with all <filename, PackageFile>-pairs for the give kper-value
        /// </summary>
        /// <param name="kper"></param>
        /// <returns></returns>
        public List<PackageFile> GetPackageFilesForPeriod(int kper = 0, int minEntryIdx = 0, int maxEntryIdx = 999)
        {
            List<PackageFile> packageFileList = new List<PackageFile>();

            // Process all entries
            for (int entryIdx = minEntryIdx; (entryIdx < GetEntryCount(kper)) && (entryIdx <= maxEntryIdx); entryIdx++)
            {
                for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
                {
                    PackageFile packageFile = GetPackageFile(entryIdx, partIdx, kper);
                    if (packageFile != null)
                    {
                        packageFileList.Add(packageFile);
                    }
                }
            }

            return packageFileList;
        }

        /// <summary>
        /// Retrieves the IMOD-file for the given entry, 
        /// the given part (partIdx) and the given timestep (kper).
        /// For missing (-1) packagedefinitions null will be returned.
        /// </summary>
        /// <param name="entryIdx"></param>
        /// <param name="partIdx"></param>
        /// <param name="kper"></param>
        /// <returns></returns>
        public IMODFile GetIMODFile(int entryIdx, int partIdx = 0, int kper = 0)
        {
            PackageFile packageFile = GetPackageFile(entryIdx, partIdx, kper);
            IMODFile imodFile = null;
            if (packageFile != null)
            {
                imodFile = packageFile.IMODFile;
            }
            return imodFile;
        }

        public PackageFile GetPackageFile(string filename, bool isWholeMatch, int kper = 0)
        {
            filename = filename.ToLower();
            PackageFile file = null;
            if (packageFiles != null)
            {
                if (kper < packageFiles.Length)
                {
                    if (packageFiles[kper] != null)
                    {
                        for (int entryIdx = 0; entryIdx < packageFiles[kper].Length; entryIdx++)
                        {
                            if (packageFiles[kper][entryIdx] != null)
                            {
                                for (int partIdx = 0; partIdx < packageFiles[kper][entryIdx].Length; partIdx++)
                                {
                                    file = packageFiles[kper][entryIdx][partIdx];
                                    if (isWholeMatch)
                                    {
                                        if (file.FName.Equals(filename))
                                        {
                                            return file;
                                        }
                                    }
                                    else
                                    {
                                        if (file.FName.ToLower().Contains(filename))
                                        {
                                            return file;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return file;
        }

        /// <summary>
        /// Retrieves a packagefile at index entryIdx. If the package has multiple files/parts 
        /// for each entry, the partIdx can be used to identify one of these file for the 
        /// given entry
        /// </summary>
        /// <param name="entryIdx">index of the entry for this package</param>
        /// <param name="partIdx">optional: the index of the part within the given entry</param>
        /// <param name="kper">optional: the stressperiodnumber (defaults to 0)</param>
        /// <returns></returns>
        public PackageFile GetPackageFile(int entryIdx, int partIdx = 0, int kper = 0)
        {
            if (partIdx >= MaxPartCount)
            {
                throw new Exception("Given partIdx (" + partIdx + ") cannot be larger than or equal to MaxPartCount (" + MaxPartCount + ")");
            }

            PackageFile file = null;
            if (packageFiles != null)
            {
                if (kper < packageFiles.Length)
                {
                    if (packageFiles[kper] != null)
                    {
                        if (entryIdx < packageFiles[kper].Length)
                        {
                            if (entryIdx >= 0)
                            {
                                if (packageFiles[kper][entryIdx] != null)
                                {
                                    if (partIdx < packageFiles[kper][entryIdx].Length)
                                    {
                                        file = packageFiles[kper][entryIdx][partIdx];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return file;
        }

        /// <summary>
        /// Retrieves the part-index of a specified packagefile. 
        /// </summary>
        /// <param name="entryIdx"></param>
        /// <param name="kper"></param>
        /// <returns></returns>
        public int GetPackageFilePartIdx(int kper, PackageFile packageFile)
        {
            if (packageFiles != null)
            {
                if (kper < packageFiles.Length)
                {
                    if (packageFiles[kper] != null)
                    {
                        for (int entryIdx = 0; entryIdx < packageFiles[kper].Length; entryIdx++)
                        {
                            if (packageFiles[kper][entryIdx] != null)
                            {
                                for (int partIdx = 0; partIdx < packageFiles[kper][entryIdx].Length; partIdx++)
                                {
                                    PackageFile file = packageFiles[kper][entryIdx][partIdx];
                                    if (file.FName.Equals(packageFile.FName))
                                    {
                                        return partIdx;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return -1;
        }

        public List<PackageFile> GetPackageFiles(int ilay, int partIdx = 0, int kper = 0)
        {
            if (partIdx >= MaxPartCount)
            {
                throw new Exception("Given partIdx (" + partIdx + ") cannot be larger than or equal to MaxPartCount (" + MaxPartCount + ")");
            }

            List<PackageFile> ilayPackageFiles = new List<PackageFile>();
            if (packageFiles != null)
            {
                if (kper < packageFiles.Length)
                {
                    if (packageFiles[kper] != null)
                    {
                        for (int entryIdx = 0; entryIdx < packageFiles[kper].Length; entryIdx++)
                        {
                            if (packageFiles[kper][entryIdx] != null)
                            {
                                if (partIdx < packageFiles[kper][entryIdx].Length)
                                {
                                    PackageFile file = packageFiles[kper][entryIdx][partIdx];
                                    if (file.ilay == ilay)
                                    {
                                        ilayPackageFiles.Add(file);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ilayPackageFiles;
        }

        public List<IMODFile> GetIMODFiles(int ilay, int partIdx = 0, int kper = 0)
        {
            List<PackageFile> ilayPackageFiles = GetPackageFiles(ilay, partIdx, kper);
            List<IMODFile> ilayIMODFiles = new List<IMODFile>();
            foreach (PackageFile packageFile in ilayPackageFiles)
            {
                if ((packageFile != null) && (packageFile.IMODFile != null))
                {
                    ilayIMODFiles.Add(packageFile.IMODFile);
                }
            }
            return ilayIMODFiles;
        }

        /// <summary>
        /// Retrieve number of defined entries (e.g. layers or systems) for this package for 
        /// the given timestep. For missing timesteps the number 0 is returned
        /// </summary>
        /// <param name="kper"></param>
        /// <returns></returns>
        public int GetEntryCount(int kper = 0)
        {
            int count = 0;
            if (packageFiles != null)
            {
                if (kper < packageFiles.Length)
                {
                    if (packageFiles[kper] != null)
                    {
                        count = packageFiles[kper].Length;
                    }
                }
            }
            return count;
        }

        public void ClearFiles()
        {
            ReleaseMemory(true);
            packageFiles = null;
        }

        public void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (packageFiles != null)
            {
                for (int kper = 0; kper < packageFiles.Length; kper++)
                {
                    if (packageFiles[kper] != null)
                    {
                        for (int entryIdx = 0; entryIdx < packageFiles[kper].Length; entryIdx++)
                        {
                            if (packageFiles[kper][entryIdx] != null)
                            {
                                for (int partIdx = 0; partIdx < packageFiles[kper][entryIdx].Length; partIdx++)
                                {

                                    PackageFile packageFile = packageFiles[kper][entryIdx][partIdx];
                                    if (packageFile != null)
                                    {
                                        packageFile.ReleaseMemory(false);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (isMemoryCollected)
            {
                GC.Collect();
                GC.WaitForFullGCComplete(-1);
            }
        }

        public string CreateRunfilesLines(Model model)
        {
            string runfileLines = string.Empty;
            if (packageFiles != null)
            {
                for (int kper = 0; kper < packageFiles.Length; kper++)
                {
                    if (packageFiles[kper] != null)
                    {
                        runfileLines += packageFiles[kper].Length + ", (" + this.key + ")\n";
                        for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
                        {
                            for (int entryIdx = 0; entryIdx < packageFiles[kper].Length; entryIdx++)
                            {
                                if (packageFiles[kper][entryIdx] != null)
                                {
                                    if (partIdx < packageFiles[kper][entryIdx].Length)
                                    {
                                        PackageFile packageFile = packageFiles[kper][entryIdx][partIdx];
                                        if (packageFile != null)
                                        {
                                            string filename = packageFile.FName;
                                            float fltValue;
                                            if (!float.TryParse(filename, out fltValue))
                                            {
                                                filename = CommonUtils.EnsureDoubleQuotes(filename);
                                            }
                                            runfileLines += packageFile.ilay + "," + packageFile.fct.ToString("F1", englishCultureInfo) + "," + packageFile.imp.ToString("F1", englishCultureInfo) + "," + filename + "\n";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return runfileLines;
        }

        /// <summary>
        /// Adds a file to the end of the set of this package. The maximum number of entries is given by entryCount. 
        /// Each entry can contain one or more parameter-files, which is defined by the package property MaxPartCount. 
        /// The files are expected to be added in a fixed ordering of the package-parameters. For each package-parameter all 
        /// entries (entryCount times) are added together, before the files of the next package-parameter are added.
        /// </summary>
        /// <param name="ilay">the number of the layer or system</param>
        /// <param name="fct">a constant factor to multiply the file-values with</param>
        /// <param name="imp">a constant value to be added to the file-values</param>
        /// <param name="fname">a constant value or the filename for the file with the values</param>
        /// <param name="entryCount">optional: the number of entries. For each parameter-file entryCount items should be added. Defaults to 1.</param>
        /// <param name="stressPeriod">optional: the stressperiod-details. Defaults to null. If specified at least KPER should be defined.</param>
        public void AddFile(int ilay, float fct, float imp, string fname, int entryCount = 1, StressPeriod stressPeriod = null)
        {
            int kper = 0;
            if (stressPeriod != null)
            {
                kper = stressPeriod.KPER;
            }

            // Adds a new file at the end, calculate entryIdx and partIdx
            int entryIdx = -1;
            int partIdx = 0;

            // ensure at least kper entries are available;
            if (packageFiles == null)
            {
                entryIdx = 0;
            }
            else
            {
                if (kper >= packageFiles.Length)
                {
                    entryIdx = 0;
                }
                else
                {
                    if (packageFiles[kper].Length < entryCount)
                    {
                        // leave partIdx 0, add an entry
                        entryIdx = packageFiles[kper].Length;
                    }
                    else
                    {
                        // Calculate partIdx
                        int maxPartIdx = 0;
                        // find entryIdx-array with lowest number of partIdx items
                        for (int idx = 0; idx < packageFiles[kper].Length; idx++)
                        {
                            if (packageFiles[kper][idx].Length > maxPartIdx)
                            {
                                maxPartIdx = packageFiles[kper][idx].Length;
                            }
                            // Look for the first entry with a lower partIdx-length than previous entries
                            if (packageFiles[kper][idx].Length < maxPartIdx)
                            {
                                entryIdx = idx;
                                partIdx = packageFiles[kper][idx].Length;
                                // stop for-loop
                                idx = packageFiles[kper].Length;

                            }
                        }
                        if (entryIdx == -1)
                        {
                            // all entryIdx-arrays have an equal number of items
                            if (maxPartIdx >= MaxPartCount)
                            {
                                throw new Exception("More files added than allowed (" + (entryCount * MaxPartCount) + ") for package " + this.key);
                            }
                            entryIdx = 0;
                            partIdx = packageFiles[kper][entryIdx].Length;
                        }
                    }
                }
            }

            AddFile(ilay, fct, imp, fname, entryIdx, partIdx, stressPeriod);
        }

        /// <summary>
        /// Adds a package entry at the given position
        /// </summary>
        /// <param name="ilay"></param>
        /// <param name="fct"></param>
        /// <param name="imp"></param>
        /// <param name="fname"></param>
        /// <param name="stressPeriod">stressperiod details, KPER should contain number of stressperiod</param>
        /// <param name="kper"></param>
        /// <param name="entryIdx">index of entry, starting with 0</param>
        /// <param name="partIdx">part index of packagefile in entry, maximum is MaxPartCount</param>
        public void AddFile(int ilay, float fct, float imp, string fname, int entryIdx, int partIdx, StressPeriod stressPeriod = null)
        {
            if (entryIdx < 0)
            {
                throw new Exception("entryIdx cannot be lower than 0");
            }

            PackageFile file = CreatePackageFile(fname, ilay, fct, imp, stressPeriod);

            int kper = 0;
            if (stressPeriod != null)
            {
                kper = stressPeriod.KPER;
            }

            // ensure at least kper entries are available;
            if (packageFiles == null)
            {
                packageFiles = new PackageFile[kper + 1][][];
            }
            else
            {
                if (kper >= packageFiles.Length)
                {
                    // Copy old entries and enlarge so that kper is contained
                    PackageFile[][][] oldFiles = packageFiles;
                    packageFiles = new PackageFile[kper + 1][][];
                    for (int i = 0; i < oldFiles.Length; i++)
                    {
                        packageFiles[i] = oldFiles[i];
                    }
                }
            }

            // ensure an entry for entryIdx is available
            if (packageFiles[kper] == null)
            {
                packageFiles[kper] = new PackageFile[entryIdx + 1][];
                packageFiles[kper][entryIdx] = new PackageFile[partIdx + 1];
            }
            else
            {
                if (entryIdx >= packageFiles[kper].Length)
                {
                    // if array is too small, enlarge it
                    PackageFile[][] oldFiles = packageFiles[kper];
                    packageFiles[kper] = new PackageFile[entryIdx + 1][];
                    for (int i = 0; i < oldFiles.Length; i++)
                    {
                        packageFiles[kper][i] = oldFiles[i];
                    }
                    packageFiles[kper][entryIdx] = new PackageFile[partIdx + 1];
                }
                else
                {
                    if (partIdx >= packageFiles[kper][entryIdx].Length)
                    {
                        // if array is too small, enlarge it
                        PackageFile[] oldFiles = packageFiles[kper][entryIdx];
                        packageFiles[kper][entryIdx] = new PackageFile[partIdx + 1];
                        for (int i = 0; i < oldFiles.Length; i++)
                        {
                            packageFiles[kper][entryIdx][i] = oldFiles[i];
                        }
                    }
                }
            }

            packageFiles[kper][entryIdx][partIdx] = file;
        }

        public virtual void ReadFiles(Log log, bool useLazyLoading = false, int lazyLoadLogIndentLevel = 0, int minEntryIdx = 0, int maxEntryIdx = 999)
        {
            if (minEntryIdx < 0)
            {
                minEntryIdx = 0;
            }

            List<string> missingFilenameList = new List<string>();
            if (packageFiles != null)
            {
                if (packageFiles.Length == 1)
                {
                    if (log != null)
                    {
                        log.AddMessage(LogLevel.Trace, "Reading " + Key + "-packagefiles...", 1);
                    }
                }
                for (int kper = 0; kper < packageFiles.Length; kper++)
                {
                    if (packageFiles[kper] != null)
                    {
                        if (packageFiles.Length > 1)
                        {
                            if (log != null)
                            {
                                if (model != null)
                                {
                                    log.AddMessage(LogLevel.Trace, "Reading " + Key + "-packagefiles for stressperiod " + Model.GetStressPeriodString(model.StartDate, kper) + "...", 1);
                                }
                                else
                                {
                                    log.AddMessage(LogLevel.Trace, "Reading " + Key + "-packagefiles for kper " + kper + "...", 1);
                                }
                            }
                        }
                        for (int entryIdx = minEntryIdx; (entryIdx <= maxEntryIdx) && (entryIdx < packageFiles[kper].Length); entryIdx++)
                        {
                            if (packageFiles[kper][entryIdx] != null)
                            {
                                for (int partIdx = 0; partIdx < packageFiles[kper][entryIdx].Length; partIdx++)
                                {
                                    if (packageFiles[kper][entryIdx][partIdx] != null)
                                    {
                                        if (log != null)
                                        {
                                            log.AddMessage(LogLevel.Trace, "reading " + packageFiles[kper][entryIdx][partIdx].FName + " ...", 1);
                                        }
                                        if (!packageFiles[kper][entryIdx][partIdx].Exists())
                                        {
                                            if (log != null)
                                            {
                                                string missingFilename = packageFiles[kper][entryIdx][partIdx].FName;
                                                if (!missingFilenameList.Contains(missingFilename))
                                                {
                                                    missingFilenameList.Add(missingFilename);
                                                    if (missingFilenameList.Count <= MaxLoggedMisingFilenameCount)
                                                    {
                                                        if (packageFiles[kper][entryIdx].Length == 1)
                                                        {
                                                            log.AddError(this.key, packageFiles[kper][entryIdx][partIdx].FName, "File for package " + key + ", entry " + (entryIdx + 1) + " and stressperiod " + kper + " not found: " + packageFiles[kper][entryIdx][partIdx].FName, 1);
                                                        }
                                                        else
                                                        {
                                                            log.AddError(this.key, packageFiles[kper][entryIdx][partIdx].FName, "File for package " + key + ", entry " + (entryIdx + 1) + " (" + (partIdx + 1) + ") and stressperiod " + kper + " not found: " + packageFiles[kper][entryIdx][partIdx].FName, 1);
                                                        }
                                                    }
                                                    else if (missingFilenameList.Count == (MaxLoggedMisingFilenameCount + 1))
                                                    {
                                                        log.AddWarning(this.key, model.Runfilename, "More than " + MaxLoggedMisingFilenameCount + " missing files for " + this.key + "-package, other missing files are not logged");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            packageFiles[kper][entryIdx][partIdx].ReadFile(useLazyLoading, log, lazyLoadLogIndentLevel);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (missingFilenameList.Count > MaxLoggedMisingFilenameCount)
            {
                log.AddInfo("Totally, " + missingFilenameList.Count + " files were missing for " + this.key + "-package", 1);
            }
        }

        public virtual void ParseRunfilePackageFiles(Runfile runfile, int entryCount, Log log, StressPeriod stressPeriod = null)
        {
            string wholeLine;
            string[] lineParts;
            int ilay;

            // default parsing of runfile package definition
            for (int entryIdx = 1; entryIdx <= entryCount * MaxPartCount; entryIdx++)
            {
                // ILAY,FCT,IMP,FNAME
                wholeLine = runfile.RemoveWhitespace(runfile.ReadLine());
                lineParts = wholeLine.Split(new char[] { ',' });
                try
                {
                    ilay = int.Parse(lineParts[0]);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while parsing ilay '" + lineParts[0] + "' in package " + this.key + " for line: " + wholeLine, ex);
                }

                string stressPeriodString = string.Empty;
                if (stressPeriod != null)
                {
                    stressPeriodString = " for " + stressPeriod.ToString();
                }

                if (lineParts.Length != 4)
                {
                    log.AddError(this.key, runfile.Runfilename, "Unexpected parameter count in " + Key + "-package for input file assignment: " + wholeLine);
                    AddFile(ilay, 0, 0, "undefined.idf", entryCount, stressPeriod);
                    log.AddWarning("Skipped entry " + entryIdx + " with undefined filename for package " + Key + stressPeriodString, 1);
                }
                else
                {
                    // simply add the file to the package (and model), also add non-existent file to keep order for adding the same
                    string fname = lineParts[3].Replace("\"", "").Replace("'", "");
                    AddFile(ilay, float.Parse(lineParts[1], englishCultureInfo), float.Parse(lineParts[2], englishCultureInfo), fname, entryCount, stressPeriod);
                    log.AddMessage(LogLevel.Trace, "Added file " + entryIdx + " to package " + Key + stressPeriodString + ": " + lineParts[3], 1);
                }
            }
        }

        /// <summary>
        /// Allow for definitions with MultiplePackageFileCount or less lines per entry. The expected number 
        /// is based on the file extension and defined in the specified extensionFileCountDictionary. It is assumed that
        /// all files with some extension are grouped. Within the group the files should be sorted according to the
        /// standard runfile layout (grouped by filetype, see iMOD-helpfile) for the package
        /// </summary>
        /// <param name="runfile"></param>
        /// <param name="entryCount"></param>
        /// <param name="extensionFileCountDictionary">allows pairs of file extensions (without dot) and MultiplePackageFileCounts, e.g. {(idf, 2) (gen, 1)} </param>
        /// <param name="log"></param>
        /// <param name="stressPeriod"></param>
        public virtual void ParseRunfileVariablePackageFiles(Runfile runfile, int entryCount, Dictionary<string, int> extensionFileCountDictionary, Log log, StressPeriod stressPeriod = null)
        {
            string wholeLine;
            string[] lineParts;
            int ilay;
            string fname;
            string extension;
            List<int> iLayList = new List<int>();

            // First find last line for this package and store all runfile lines per extension type
            Dictionary<string, List<string>> extTypeRunFileLineDictionary = new Dictionary<string, List<string>>();
            long startLinenumber = runfile.GetCurrentLinenumber();
            wholeLine = runfile.PeekLine();
            while ((wholeLine != null) && wholeLine.Contains(",") && (!wholeLine.Contains("(")))
            {
                wholeLine = runfile.RemoveWhitespace(runfile.ReadLine());
                lineParts = wholeLine.Split(new char[] { ',' });
                try
                {
                    // ILAY,FCT,IMP,FNAME
                    fname = lineParts[3].Replace("\"", "").Replace("'", "");
                    extension = Path.GetExtension(fname).ToLower();
                    if (!extTypeRunFileLineDictionary.ContainsKey(extension))
                    {
                        extTypeRunFileLineDictionary.Add(extension, new List<string>());
                    }
                    extTypeRunFileLineDictionary[extension].Add(wholeLine);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while parsing ilay '" + lineParts[0] + "' in package " + this.key + " for line: " + wholeLine, ex);
                }
                wholeLine = runfile.PeekLine();
            }

            // Now start again, add the filetypes in the expected order (using dummy-files if needed)
            //            long currentLineNumber = startLinenumber;
            for (int partIdx = 0; partIdx < MaxPartCount; partIdx++)
            {
                foreach (string currentExtension in extTypeRunFileLineDictionary.Keys)
                {
                    int currentExtTypeFileCount = extTypeRunFileLineDictionary[currentExtension].Count();
                    int currentExtTypeEntryCount = currentExtTypeFileCount / extensionFileCountDictionary[currentExtension];

                    // Retrieve the file at index 'partIdx' for each entry of this extType
                    for (int entryIdx = 0; entryIdx < currentExtTypeEntryCount; entryIdx++)
                    {
                        // Check if a file/part at index 'partIdx' is present for this extentionType
                        if (partIdx < extensionFileCountDictionary[currentExtension])
                        {
                            //                            int extTypePartIdx = partIdx * extensionFileCountDictionary[currentExtension] + entryIdx;
                            int extTypePartIdx = partIdx * currentExtTypeEntryCount + entryIdx;
                            wholeLine = extTypeRunFileLineDictionary[currentExtension][extTypePartIdx];
                            // ILAY,FCT,IMP,FNAME
                            //                            wholeLine = runfile.RemoveWhitespace(runfile.ReadLine(currentLineNumber));
                            //                            currentLineNumber++;
                            lineParts = wholeLine.Split(new char[] { ',' });
                            try
                            {
                                ilay = int.Parse(lineParts[0]);
                                if (partIdx == 0)
                                {
                                    // save ilay number for dummyfiles 
                                    iLayList.Add(ilay);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Error while parsing ilay '" + lineParts[0] + "' in package " + this.key + " for line: " + wholeLine, ex);
                            }
                            string stressPeriodString = string.Empty;
                            if (stressPeriod != null)
                            {
                                stressPeriodString = " for " + stressPeriod.ToString();
                            }

                            if (lineParts.Length != 4)
                            {
                                log.AddError(this.key, runfile.Runfilename, "Unexpected parameter count in " + Key + "-package for input file assignment: " + wholeLine);
                            }

                            // simply add the file to the package (and model), also add non-existent file to keep order for adding the same
                            fname = lineParts[3].Replace("\"", "").Replace("'", "");
                            AddFile(ilay, float.Parse(lineParts[1], englishCultureInfo), float.Parse(lineParts[2], englishCultureInfo), fname, entryCount, stressPeriod);
                            log.AddMessage(LogLevel.Trace, "Added " + currentExtension.Substring(1).ToUpper() + "-file " + partIdx + " to package " + Key + stressPeriodString + ": " + lineParts[3], 1);
                        }
                        else
                        {
                            // Add dummyfile to the package (and model) for filetypes with less than the maximum number of parameterfiles
                            string dummyFilename = "dummy" + currentExtension;
                            AddFile(iLayList[entryIdx], 0, 0, dummyFilename, entryCount, stressPeriod);
                        }
                    }
                }
            }
        }

        public IEnumerable<int> StressPeriods(Model model, Log log = null, int indentLevel = 0)
        {
            for (int kper = 0; kper <= model.NPER; kper++)
            {
                if (GetEntryCount(kper) > 0)
                {
                    if (log != null)
                    {
                        if (model.NPER > 1)
                        {
                            log.AddInfo("Processing stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + "...", indentLevel);
                        }
                        else
                        {
                            log.AddMessage(LogLevel.Trace, "Processing stressperiod " + kper + " " + Model.GetStressPeriodString(model.StartDate, kper) + "...", indentLevel);
                        }
                    }
                    yield return kper;
                }
            }
        }

        public bool HasKeyMatch(string packageKey)
        {
            packageKey = packageKey.ToUpper();
            string thisPackageKey = this.Key.ToUpper();
            if (thisPackageKey.Equals(packageKey))
            {
                return true;
            }
            else
            {
                // try alternative keys if defined
                foreach (string alternativeKey in alternativeKeys)
                {
                    if (alternativeKey.ToUpper().Equals(packageKey))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected virtual PackageFile CreatePackageFile(string fname, int ilay, float fct, float imp, StressPeriod stressPeriod)
        {
            return Files.PackageFileFactory.CreatePackageFile(this, fname, ilay, fct, imp, stressPeriod);
        }
    }
}
