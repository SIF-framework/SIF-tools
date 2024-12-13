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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMODPlus;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Results;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator
{
    public class ModelComparator
    {
        // Define packages for which files will be always matched by entry index. Other packages are only matched by entry index if the number of entries match for both models, otherwise a match on names is tried.
        private static List<string> MatchedByEntryIdxPackages = new List<string>() {
            TOPPackage.DefaultKey, BOTPackage.DefaultKey, KHVPackage.DefaultKey, KVVPackage.DefaultKey, KVAPackage.DefaultKey, KDWPackage.DefaultKey, VCWPackage.DefaultKey, ANIPackage.DefaultKey, BNDPackage.DefaultKey, SHDPackage.DefaultKey, CHDPackage.DefaultKey, STOPackage.DefaultKey
        };

        public string Description
        {
            get { return "Compares basemodelfiles with files from another model"; }
        }

        /// <summary>
        /// Value used instead of NoData for comparison between two cells with one or two NoData-values. 
        /// float.NaN will result in NoData in a cell when one of the IDF-files has a NoData-value in that cell.</param>
        /// </summary>
        public float NoDataComparisonValue { get; set; } = 0;

        public void Run(Model model, Model comparedModel, ModelComparerResultHandler resultHandler, string comparisonFilesSubdirName, Log log)
        {
            string outputFoldername = model.ToolOutputPath;
            string comparisonOutputFoldername = Path.Combine(outputFoldername, comparisonFilesSubdirName);

            // Delete previous output files, but check path to prevent deletion from rootfolders, e.g. c:\tmp\diff... should be allowed as a minimum
            if ((Directory.Exists(comparisonOutputFoldername)) && (comparisonOutputFoldername.Length > 4) &&
                (comparisonOutputFoldername.Split(new char[] { '\\', '/' }).Length > 2))
            {
                Directory.Delete(comparisonOutputFoldername, true);
            }

            string baseModelname = Path.GetFileNameWithoutExtension(model.RUNFilename);
            string comparedModelname = Path.GetFileNameWithoutExtension(comparedModel.RUNFilename);
            log.AddInfo("Starting comparison with " + Path.GetFileNameWithoutExtension(comparedModel.RUNFilename) + " within extent " + resultHandler.Extent.ToString() + " ...");

            int differenceCount = 0;

            // Actually read all data from modelfiles
            foreach (Package package in model.Packages)
            {
                log.AddMessage(LogLevel.Debug, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.");

                // read packages (don't use lazy loading mechanism)
                Package comparedPackage = comparedModel.GetPackage(package.Key);
                if (comparedPackage == null)
                {
                    log.AddWarning(package.Key, resultHandler.Model.RUNFilename, package.Key + "-package not found in compared model!");
                }
                else
                {
                    // Compare all package files
                    log.AddInfo("Comparing package " + package.Key + " ...");
                    comparedPackage.ReadFiles(log, iMODValidatorSettingsManager.Settings.UseLazyLoading, model.GetExtent(), 2, resultHandler.MinEntryNumber - 1, resultHandler.MaxEntryNumber - 1);   //

                    int maxKPER = package.GetMaxKPER();
                    int comparedMaxKPER = comparedPackage.GetMaxKPER();
                    if (maxKPER != comparedMaxKPER)
                    {
                        log.AddInfo("Different number of stressperiods (KPER) in both packages: " + maxKPER + " vs. " + comparedMaxKPER + ". Comparing base model KPER-values", 1);
                    }

                    for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= maxKPER) && (kper <= resultHandler.MaxKPER); kper++)
                    {
                        StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                        string SNAME = stressPeriod.SNAME;

                        if (package.GetEntryCount(kper) > 0)
                        {
                            if (model.NPER > 1)
                            {
                                log.AddInfo("Checking stress period " + kper + ": " + SNAME + "...", 1);
                                // log.AddMessage(LogLevel.Trace, "Checking stress period " + kper + " " + SNAME + "...", 1);
                            }
                        }

                        StressPeriod comparedStressPeriod = null;
                        if (stressPeriod.DateTime == null)
                        {
                            comparedStressPeriod = comparedModel.RetrieveStressPeriod(stressPeriod.SNAME);
                        }
                        else
                        {
                            comparedStressPeriod = comparedModel.RetrieveStressPeriod(stressPeriod.DateTime);
                        }

                        List<PackageFile> kperLeftoverComparedPackageFiles = null;
                        if (comparedStressPeriod != null)
                        {
                            // Keep track of files in compared runfile which are not in base runfile
                            kperLeftoverComparedPackageFiles = comparedPackage.GetPackageFilesForPeriod(comparedStressPeriod, resultHandler.MinEntryNumber - 1, resultHandler.MaxEntryNumber - 1);

                            // Check if number of package files are equal
                            if (package.GetEntryCount(kper) != comparedPackage.GetEntryCount(comparedStressPeriod.KPER))
                            {
                                log.AddInfo("Different number of package files for stress period " + kper
                                    + " (" + model.RetrieveSNAME(kper) + "): " + package.GetEntryCount(kper)
                                    + " vs. " + comparedPackage.GetEntryCount(comparedStressPeriod.KPER) + ". Only comparing base model files.", 1);
                            }
                        }
                        else
                        {
                            // log.AddInfo("Stress period (" + kper + " " + model.RetrieveSNAME(kper) + ") not found for compared model, missing " + package.GetEntryCount(kper) + " entries", 1);
                        }

                        // Check base model packagefiles
                        if (package.GetEntryCount(kper) > 0)
                        {
                            // Process all specified entries
                            // Match files by entry index when the number of entries in both models is exactly the same, otherwise try to match by filename (first try) or content (second try)
                            bool isMatchedByEntryIdx = IsMatchedByEntryIdx(package, comparedPackage, kper);
                            // Keep a list of package entries that have been matched already, so that these don't have to be searched for in a second round to match by content
                            // List<PackageFile> foundPackageFiles = new List<PackageFile>(); // Don't keep list of package files, these may be confused when empty
                            List<float> foundPackageEntries = new List<float>(); // integer part stores entry index, decimal part stores part index
                            CompareKPER(resultHandler, model, baseModelname, comparedModelname, package, comparedPackage, stressPeriod, comparedStressPeriod, kperLeftoverComparedPackageFiles, isMatchedByEntryIdx, !isMatchedByEntryIdx, !isMatchedByEntryIdx, comparisonOutputFoldername, log, ref differenceCount, ref foundPackageEntries);
                            if (!isMatchedByEntryIdx)
                            {
                                // Run again to compare leftoverfiles (that where not found by filename)
                                CompareKPER(resultHandler, model, baseModelname, comparedModelname, package, comparedPackage, stressPeriod, comparedStressPeriod, kperLeftoverComparedPackageFiles, false, true, false, comparisonOutputFoldername, log, ref differenceCount, ref foundPackageEntries);
                            }
                        }

                        // Report files from compared model that are not present in base model
                        if ((kperLeftoverComparedPackageFiles != null) && (kperLeftoverComparedPackageFiles.Count > 0))
                        {
                            log.AddInfo("The following files/values in the compared model were NOT FOUND FOR BASE MODEL: ", 1);
                            bool hasIgnoredConstantValues = false;
                            foreach (PackageFile comparedPackageFile in kperLeftoverComparedPackageFiles)
                            {
                                if (comparedPackageFile is ConstantIDFPackageFile)
                                {
                                    log.AddInfo("- constant value " + ((ConstantIDFPackageFile)comparedPackageFile).ConstantValue.ToString(SIFTool.EnglishCultureInfo), 1);
                                    hasIgnoredConstantValues = true;
                                }
                                else if (File.Exists(comparedPackageFile.FName))
                                {
                                    string leftoverFilename = Path.GetFileName(comparedPackageFile.FName);
                                    log.AddInfo("- " + leftoverFilename, 1);
                                    differenceCount++;

                                    // Write full comparedModel file as a difference file
                                    PackageFile diffPackageFile = comparedPackageFile.Clip(resultHandler.Extent);
                                    string addedFilename = Path.Combine(comparisonOutputFoldername, "added_" + leftoverFilename);
                                    diffPackageFile.FName = addedFilename;
                                    Metadata diffMetadata = comparedModel.CreateMetadata();
                                    diffMetadata.IMODFilename = addedFilename;
                                    diffMetadata.Modelversion = baseModelname + " - " + comparedModelname;
                                    diffMetadata.Description = "Difference for KPER " + kper + ", ilay " + comparedPackageFile.ILAY + ": "
                                        + leftoverFilename + " only present in model " + comparedModelname;
                                    diffMetadata.Source = leftoverFilename + " in models " + baseModelname + " and " + comparedModelname;
                                    diffPackageFile.WriteFile(diffMetadata, log, 2);

                                    int partIdx = comparedPackage.GetPackageFilePartIdx(kper, comparedPackageFile);
                                    ComparatorResultLayer resultLayer = new ComparatorResultLayer(package.Key, package.PartAbbreviations[partIdx], null, stressPeriod, diffPackageFile.ILAY, model.ToolOutputPath);
                                    ComparatorResult comparisonResult = new ComparatorResult(diffPackageFile.IMODFile);
                                    comparisonResult.ShortDescription = "file added in other model";
                                    resultLayer.AddSourceFile(comparedPackageFile.IMODFile);
                                    resultHandler.SetComparisonResult(resultLayer, comparisonResult);
                                    if (diffPackageFile.IMODFile != null)
                                    {
                                        resultLayer.Legend = diffPackageFile.IMODFile.CreateDifferenceLegend(Color.DarkBlue);
                                    }

                                    diffPackageFile.ReleaseMemory(false);
                                    resultLayer.ReleaseMemory(true);
                                }
                                else
                                {
                                    log.AddError("File in compared RUN-file, for package " + comparedPackage.Key + ", stress period " + kper + " not found: " + comparedPackageFile.FName, 1);
                                }
                            }

                            if (hasIgnoredConstantValues)
                            {
                                log.AddWarning(package.Key, resultHandler.ComparedModel.RUNFilename, "Constant values in compared model found that could not be compared", 1);
                            }
                        }

                        // Perform package specific comparisons
                        if (package is CAPPackage)
                        {
                            CompareExtraFiles((CAPPackage)package, (CAPPackage)comparedPackage, log, 2);
                        }

                        log.Flush();
                    }

                    comparedPackage.ReleaseMemory(true);
                }

                package.ReleaseMemory(false);
                log.AddMessage(LogLevel.Debug, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.");
            }

            log.AddInfo();
            if (differenceCount > 0)
            {
                log.AddInfo("Models have differences in " + differenceCount + " files of known type");
                log.AddInfo("See messages above, logfile and outputfolder " + comparisonOutputFoldername, 1);
            }
            else
            {
                log.AddInfo("No differences found in known files");
            }
            log.AddInfo(string.Empty);
        }

        protected void CompareExtraFiles(CAPPackage capPackage, CAPPackage comparedCAPPackage, Log log, int logIndentLevel)
        {
            List<string> extraFilenames = capPackage.ExtraFilenames;
            List<string> comparedExtraFilenames = comparedCAPPackage.ExtraFilenames;
            if (extraFilenames.Count != comparedExtraFilenames.Count)
            {
                log.AddError(CAPPackage.DefaultKey, "ExtraFiles", "Number of extra files is differing between between model (" + extraFilenames.Count + ") and compared model (" + comparedExtraFilenames.Count + ")");
            }
            else
            {
                for (int entryIdx = 0; entryIdx < extraFilenames.Count; entryIdx++)
                {
                    string extraFilename = extraFilenames[entryIdx];
                    string comparedExtraFilename = comparedExtraFilenames[entryIdx];

                    log.AddInfo("Comparing extra file '" + Path.GetFileName(extraFilename) + "' at entry " + (entryIdx + 1) + " ... ", logIndentLevel, false);
                    if (!File.Exists(extraFilename))
                    {
                        log.AddInfo("NOT FOUND");
                        log.AddError(CAPPackage.DefaultKey, extraFilename, "Extra file in CAP-package (MetaSWAP) does not exist: " + extraFilename);
                    }
                    else if (!File.Exists(comparedExtraFilename))
                    {
                        log.AddInfo("NOT FOUND");
                        log.AddError(CAPPackage.DefaultKey, comparedExtraFilename, "Extra file in CAP-package (MetaSWAP) in compared model does not exist: " + comparedExtraFilename);
                    }
                    else
                    {
                        string extraFileText = FileUtils.ReadFile(extraFilename);
                        string comparedeExtraFileText = FileUtils.ReadFile(extraFilename);

                        if (!extraFileText.Equals(comparedeExtraFileText))
                        {
                            log.AddInfo("DIFFERENT!");
                            log.AddError(CAPPackage.DefaultKey, extraFilename, "Extra file in CAP-package (MetaSWAP) is not equal to compared version");
                        }
                        else
                        {
                            log.AddInfo("OK");
                        }
                    }
                }
            }
        }

        private bool IsMatchedByEntryIdx(Package package, Package comparedPackage, int kper)
        {
            string extension = Path.GetExtension(package.Model.RUNFilename).ToUpper();
            string otherExtension = Path.GetExtension(comparedPackage.Model.RUNFilename).ToUpper();
            if ((package is CAPPackage) && !extension.Equals(otherExtension))
            {
                return false;
            }
            else
            {
                return (package.GetEntryCount(kper) == comparedPackage.GetEntryCount(kper)) || MatchedByEntryIdxPackages.Contains(package.Key);
            }
        }

        /// <summary>
        /// Compares package entries of two models for specified package and KPER.
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="model"></param>
        /// <param name="baseModelname"></param>
        /// <param name="comparedModelname"></param>
        /// <param name="package"></param>
        /// <param name="comparedPackage"></param>
        /// <param name="stressPeriod"></param>
        /// <param name="comparedStressPeriod"></param>
        /// <param name="kperLeftoverComparedPackageFiles"></param>
        /// <param name="isMatchedByEntryIdx"></param>
        /// <param name="isMatchedByName"></param>
        /// <param name="isSkippedNotFound">Skips handling/messages for packagefiles that are not found</param>
        /// <param name="comparisonOutputFoldername"></param>
        /// <param name="log"></param>
        /// <param name="differenceCount"></param>
        /// <param name="foundPackageEntries">list of found entry/part-idx pairs coded as a float value</param>
        private void CompareKPER(ModelComparerResultHandler resultHandler, Model model, string baseModelname, string comparedModelname, Package package, Package comparedPackage, 
           StressPeriod stressPeriod, StressPeriod comparedStressPeriod, List<PackageFile> kperLeftoverComparedPackageFiles, bool isMatchedByEntryIdx, bool isMatchedByName, bool isSkippedNotFound, 
           string comparisonOutputFoldername, Log log, ref int differenceCount, ref List<float> foundPackageEntries)
        {
            int kper = stressPeriod.KPER;
            int comparedKPER = (comparedStressPeriod != null) ? comparedStressPeriod.KPER : -1;

            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < resultHandler.MaxEntryNumber) && (entryIdx < package.GetEntryCount(kper)); entryIdx++)
            {
                for (int partIdx = 0; partIdx < package.MaxPartCount; partIdx++)
                {
                    CheckManager.Instance.CheckForAbort();

                    PackageFile packageFile = package.GetPackageFile(entryIdx, partIdx, kper);
                    if ((packageFile != null) && !foundPackageEntries.Contains(Utils.IntegersToDecimal(entryIdx, 1000 * partIdx + 1)))
                    {
                        if (!isMatchedByEntryIdx && (packageFile is ConstantIDFPackageFile))
                        {
                            // Constant files can only be matched by entry index, skip other match methods
                            if (isSkippedNotFound)
                            {
                                // Log ignored comparison for constant values
                                LogEntryComparison(package, entryIdx, partIdx, packageFile, log, (stressPeriod.DateTime != null) ? 2 : 1);
                                log.AddInfo("ignored");
                                float constantValue = ((ConstantIDFPackageFile)packageFile).ConstantValue;
                                log.AddWarning(package.Key, constantValue.ToString(SIFTool.EnglishCultureInfo), "Constant value (" + constantValue.ToString(SIFTool.EnglishCultureInfo) + ") for entry,part-index (" + entryIdx + "," + partIdx + ") is ignored for this package", 3);
                            }

                            continue;
                        }

                        if (!isSkippedNotFound)
                        {
                            // Log start of comparison at this point, before it is known if the comparison file is found or not
                            LogEntryComparison(package, entryIdx, partIdx, packageFile, log, (stressPeriod.DateTime != null) ? 2 : 1);
                        }

                        // Retrieve corresponding file from compared package
                        PackageFile comparedPackageFile = null;
                        if (comparedKPER != -1)
                        {

                            if (isMatchedByEntryIdx)
                            {
                                // Use file at corresponding position in package to match packagefile
                                comparedPackageFile = comparedPackage.GetPackageFile(entryIdx, partIdx, comparedKPER);
                                // Remove from compared package leftover list if present
                                if (kperLeftoverComparedPackageFiles.Contains(comparedPackageFile))
                                {
                                    kperLeftoverComparedPackageFiles.Remove(comparedPackageFile);
                                }
                            }
                            else
                            {
                                // Try to find corrsponding equal item (by kper, ilay and content) regardless of order in list
                                if (kperLeftoverComparedPackageFiles.Contains(packageFile))
                                {
                                    // File from compared model has been found for base model, use and remove from leftover list
                                    comparedPackageFile = kperLeftoverComparedPackageFiles[kperLeftoverComparedPackageFiles.IndexOf(packageFile)];
                                    kperLeftoverComparedPackageFiles.Remove(packageFile);
                                }
                                else
                                {
                                    if (isMatchedByName)
                                    {
                                        // Try to match by name, kper and ilay regardless of order in list
                                        if (packageFile.HasPackageFileWithEqualFilename(kperLeftoverComparedPackageFiles))
                                        {
                                            comparedPackageFile = packageFile.RetrievePackageFileWithEqualFilename(kperLeftoverComparedPackageFiles);
                                        }
                                    }
                                    else
                                    {
                                        // Try to find corrsponding equal item (by kper and ilay) regardless of order in list
                                        if (packageFile.HasCorrespondingPackageFile(kperLeftoverComparedPackageFiles))
                                        {
                                            comparedPackageFile = packageFile.RetrieveCorrespondingPackageFile(kperLeftoverComparedPackageFiles);
                                        }
                                        else
                                        {
                                            // A corresponding package file with the same name has not been found, try file at corresponding position in package 
                                            comparedPackageFile = comparedPackage.GetPackageFile(entryIdx, partIdx, comparedKPER);
                                        }
                                    }

                                    // Remove from compared package leftover list if present
                                    if (kperLeftoverComparedPackageFiles.Contains(comparedPackageFile))
                                    {
                                        kperLeftoverComparedPackageFiles.Remove(comparedPackageFile);
                                    }
                                }
                            }
                        }

                        if (comparedPackageFile == null)
                        {
                            // Current file is not found in other model
                            if (!isSkippedNotFound)
                            {
                                differenceCount++;
                                if (log.ListenerLogLevels[0] >= LogLevel.Info)
                                {
                                    log.AddInfo("NOT FOUND!");
                                }
                                else
                                {
                                    log.AddInfo("File " + Path.GetFileName(packageFile.FName) + " not found in compared model!");
                                }

                                // write full base model file as a difference file
                                string diffFilename = Path.Combine(comparisonOutputFoldername, "deleted_" + Path.GetFileName(packageFile.FName));
                                Metadata diffMetadata = packageFile.CreateMetadata(model);
                                diffMetadata.Modelversion = baseModelname + " - " + comparedModelname;
                                diffMetadata.Description = "Difference for KPER " + kper + ", entry " + (entryIdx + 1) + ", ilay " + packageFile.ILAY + ": "
                                    + Path.GetFileName(packageFile.FName) + " only present in model " + baseModelname;
                                diffMetadata.Source = Path.GetFileName(packageFile.FName) + " in models " + baseModelname + " and " + comparedModelname;
                                if (File.Exists(packageFile.FName))
                                {
                                    PackageFile diffPackageFile = packageFile.Copy(diffFilename);
                                    if ((diffPackageFile.IMODFile != null) && (diffPackageFile.IMODFile.Legend == null))
                                    {
                                        // Create default legend if no difference legend has been created yet
                                        diffPackageFile.IMODFile.Legend = diffPackageFile.IMODFile.CreateDifferenceLegend(null, true);
                                    }
                                    diffPackageFile.WriteFile(diffMetadata);

                                    ComparatorResultLayer resultLayer = new ComparatorResultLayer(package.Key, package.PartAbbreviations[partIdx], null, stressPeriod, entryIdx + 1, model.ToolOutputPath, partIdx);
                                    ComparatorResult comparisonResult = new ComparatorResult(diffPackageFile.IMODFile);
                                    comparisonResult.ShortDescription = "file deleted in other model";
                                    resultLayer.AddSourceFile(packageFile.IMODFile);
                                    // resultHandler.AddResultLayer(resultLayer);
                                    resultHandler.SetComparisonResult(resultLayer, comparisonResult);

                                    diffPackageFile.ReleaseMemory(true);
                                    resultLayer.ReleaseMemory(true);
                                }
                                else
                                {
                                    diffMetadata.Description += "; NOTE: Source file was not found: " + packageFile.FName;
                                    diffMetadata.IMODFilename = packageFile.FName;
                                    diffMetadata.METFilename = Path.ChangeExtension(diffFilename, "MET");
                                    if (!Directory.Exists(Path.GetDirectoryName(diffMetadata.METFilename)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(diffMetadata.METFilename));
                                    }
                                    diffMetadata.WriteMetaFile();
                                }
                            }
                        }
                        else
                        {
                            // Current file is found in other model

                            if (isSkippedNotFound)
                            {
                                // Log start of comparison at this point, when it is known that the comparison file is found
                                LogEntryComparison(package, entryIdx, partIdx, packageFile, log, (stressPeriod.DateTime != null) ? 2 : 1);
                            }

                            // Add entryIdx/partIdx-pair to list of found package entries; these are coded as a float to search the combinations of two values in a list
                            foundPackageEntries.Add(Utils.IntegersToDecimal(entryIdx, 1000 * partIdx + 1));

                            if ((packageFile is IDFPackageFile) || (packageFile is IPFPackageFile) || (packageFile is ISGPackageFile) || (packageFile is GENPackageFile))
                            {
                                if (!packageFile.HasEqualContent(comparedPackageFile, model.GetExtent(), false))
                                {
                                    differenceCount++;
                                    if (log.ListenerLogLevels[0] >= LogLevel.Info)
                                    {
                                        log.AddInfo("DIFFERENT!");
                                    }
                                    else
                                    {
                                        log.AddMessage(LogLevel.Trace, "File " + Path.GetFileName(packageFile.FName) + " is different in compared model!");
                                    }

                                    PackageFile diffPackageFile = comparedPackageFile.CreateDifferenceFile(packageFile, true, comparisonOutputFoldername, model.GetExtent(), NoDataComparisonValue, log, 2);
                                    if (diffPackageFile != null)
                                    {
                                        Metadata diffMetadata = diffPackageFile.CreateMetadata(model);
                                        diffMetadata.Modelversion = baseModelname + " - " + comparedModelname;
                                        diffMetadata.Description = "Differencefile " + Path.GetFileName(diffPackageFile.FName)
                                            + " for KPER " + kper + ", entry " + (entryIdx + 1) + ", ilay " + packageFile.ILAY
                                            + " between files " + Path.GetFileName(packageFile.FName) + " and " + Path.GetFileName(comparedPackageFile.FName)
                                            + " of models " + baseModelname + " and " + comparedModelname;
                                        diffMetadata.Source = Path.GetFileName(packageFile.FName) + " and " + Path.GetFileName(comparedPackageFile.FName)
                                            + " of models " + baseModelname + " and " + comparedModelname;

                                        diffPackageFile.WriteFile(diffMetadata, log, 2);
                                        if ((diffPackageFile.IMODFile != null) && (diffPackageFile.IMODFile.Legend == null))
                                        {
                                            // Create default legend if no difference legend has been created yet
                                            diffPackageFile.IMODFile.Legend = diffPackageFile.IMODFile.CreateDifferenceLegend(null, true);
                                        }

                                        ComparatorResultLayer resultLayer = new ComparatorResultLayer(package.Key, package.PartAbbreviations[partIdx], null, stressPeriod, entryIdx + 1, model.ToolOutputPath, partIdx);
                                        ComparatorResult comparisonResult = new ComparatorResult(diffPackageFile.IMODFile);
                                        comparisonResult.ShortDescription = "different values for both modelfiles";
                                        resultLayer.AddSourceFile(packageFile.IMODFile);
                                        resultLayer.AddSourceFile(comparedPackageFile.IMODFile);
                                        resultHandler.SetComparisonResult(resultLayer, comparisonResult);

                                        diffPackageFile.ReleaseMemory(false);
                                        resultLayer.ReleaseMemory(true);
                                    }
                                    else
                                    {
                                        ComparatorResultLayer resultLayer = new ComparatorResultLayer(package.Key, package.PartAbbreviations[partIdx], null, stressPeriod, entryIdx + 1, model.ToolOutputPath, partIdx);
                                        ComparatorResult comparisonResult = new ComparatorResult(null);
                                        comparisonResult.ShortDescription = "One or both of the comparison files doesn't exist";
                                        if (!resultHandler.HasResultLayer(resultLayer))
                                        {
                                            resultHandler.AddResultLayer(resultLayer);
                                        }
                                        resultHandler.SetComparisonResult(resultLayer, comparisonResult);

                                        resultLayer.ReleaseMemory(true);
                                    }
                                }
                                else
                                {
                                    if ((packageFile.IMODFile != null) && (comparedPackageFile.IMODFile != null))
                                    {
                                        log.AddInfo("OK");
                                    }
                                    else
                                    {
                                        log.AddInfo("UNDEFINED");
                                    }
                                }
                            }
                            else
                            {
                                log.AddInfo("Comparison not yet implemented");
                            }

                            comparedPackageFile.ReleaseMemory(true);
                        }

                        packageFile.ReleaseMemory(true);
                    }
                }
            }
        }

        private void LogEntryComparison(Package package, int entryIdx, int partIdx, PackageFile packageFile, Log log, int logIndentLevel)
        {
            if (log.ListenerLogLevels[0] >= LogLevel.Trace)
            {
                if (package.MaxPartCount > 1)
                {
                    if (partIdx == 0)
                    {
                        log.AddInfo("Comparing entry " + (entryIdx + 1) + " ... ", logIndentLevel);
                    }
                    if (packageFile is ConstantIDFPackageFile)
                    {
                        log.AddInfo("Comparing constant value " + ((ConstantIDFPackageFile)packageFile).ConstantValue.ToString(SIFTool.EnglishCultureInfo) + " at entry " + (entryIdx + 1) + " ... ", logIndentLevel + 1, false);
                    }
                    else
                    {
                        log.AddInfo("Comparing file " + Path.GetFileName(packageFile.FName) + " ... ", logIndentLevel + 1, false);
                    }
                }
                else
                {
                    if (packageFile is ConstantIDFPackageFile)
                    {
                        log.AddInfo("Comparing constant value " + ((ConstantIDFPackageFile)packageFile).ConstantValue.ToString(SIFTool.EnglishCultureInfo) + " at entry " + (entryIdx + 1) + "... ", logIndentLevel, false);
                    }
                    else
                    {
                        log.AddInfo("Comparing entry " + (entryIdx + 1) + ": " + Path.GetFileName(packageFile.FName) + " ... ", logIndentLevel, false);
                    }
                }
            }
        }
    }
}
