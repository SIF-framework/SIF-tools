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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Checks;
using System.IO;
using Sweco.SIF.Common;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public class ISGRIVConverter
    {
        /// <summary>
        /// Dictionary with isgFilenames and corresponding converted RIV IDF-files (in order as defined for RIV-package parts)
        /// </summary>
        protected static Dictionary<string, Dictionary<string, List<string>>> isgRIVDictionary = new Dictionary<string, Dictionary<string, List<string>>>();

        /// <summary>
        /// Remove earlier ISGPackages from cache
        /// </summary>
        public static void ClearCache()
        {
            isgRIVDictionary.Clear();
        }

        /// <summary>
        /// Checks if ISG-package has any entries within min/max KPER as defined by resulthandler/model
        /// </summary>
        /// <param name="isgPackage"></param>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <returns></returns>
        public static bool HasISGPackageEntries(ISGPackage isgPackage, Model model, CheckResultHandler resultHandler)
        {
            // Process all periods
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (isgPackage.GetEntryCount(kper) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts ISGPackage object with ISG-file(s) to RIVPackage with corresponding IDF-files. 
        /// Note: earlier converted ISG-files are kept in cache and the earlier converted RIVPackage is returned.
        /// </summary>
        /// <param name="isgPackage"></param>
        /// <param name="isgRIVKey">new key for resulting RIVPackage object</param>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static RIVPackage ConvertISGtoRIVPackage(ISGPackage isgPackage, string isgRIVKey, Model model, CheckResultHandler resultHandler, Log log, int logIndentLevel)
        {
            RIVPackage isgRIVPackage = new RIVPackage(isgRIVKey);
            isgRIVPackage.Model = model;
            isgRIVPackage.IsActive = true;

            // Process all periods
            for (int kper = resultHandler.MinKPER; (kper <= model.NPER) && (kper <= resultHandler.MaxKPER); kper++)
            {
                if (isgPackage.GetEntryCount(kper) > 0)
                {
                    if (model.NPER > 1)
                    {
                        log.AddInfo("Checking ISG-conversion for stress period " + kper + " " + model.RetrieveSNAME(kper) + "...", logIndentLevel);
                    }
                    else
                    {
                        log.AddMessage(LogLevel.Trace, "Checking ISG-conversion for stress period " + kper + " " + model.RetrieveSNAME(kper) + "...", logIndentLevel);
                    }

                    DateTime? modelStartDate = null;
                    DateTime? modelEndDate = null;
                    if (model.StartDate != null)
                    {
                        modelStartDate = model.StartDate.Value;
                        modelEndDate = model.StartDate.Value.AddDays(model.NPER);
                    }

                    // Process all specified modellayers within the current period
                    int entryCount = (int)Math.Min(isgPackage.GetEntryCount(kper), resultHandler.MaxEntryNumber) - resultHandler.MinEntryNumber + 1;
                    for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < isgPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
                    {
                        CheckManager.Instance.CheckForAbort();
                        ISGPackageFile isgPackageFile = (ISGPackageFile)isgPackage.GetPackageFile(entryIdx, 0, kper);
                        if (isgPackageFile != null)
                        {
                            if (isgPackageFile.ILAY == 0)
                            {
                                isgPackageFile.ILAY = 1;
                            }

                            int ilay = isgPackageFile.ILAY;
                            ISGFile isgFile = (ISGFile)isgPackageFile.IMODFile;
                            if (isgFile != null)
                            {
                                // Ensure ISG-segments are loaded (in case ISG-file is lazy loaded)
                                isgFile.EnsureLoadedSegments();

                                if (isgFile.Extent.Union(resultHandler.Extent) != null)
                                {
                                    // log.AddInfo("Converting entry " + (entryIdx + 1) + ": " + Path.GetFileName(isgFile.Filename) + " ...", logIndentLevel + 1);
                                    if (model.StartDate == null)
                                    {
                                        log.AddInfo("Converting ISG-file " + Path.GetFileName(isgFile.Filename) + " for averageperiod ...", logIndentLevel + 2);
                                        string resultPath = FileUtils.EnsureFolderExists(Path.Combine(Path.Combine(Path.Combine(resultHandler.OutputPath, "ISG-grids"), "avg"), Path.GetFileName(isgFile.Filename)));
                                        ConvertToRIV(isgPackageFile, resultPath, false, null, null, resultHandler.Extent, isgRIVPackage, entryCount, log, logIndentLevel + 2);
                                    }
                                    else
                                    {
                                        // Retrieve label of current stress period as defined in RUN-file
                                        StressPeriod stressPeriod = model.RetrieveStressPeriod(kper);
                                        if (isgPackageFile.StressPeriod != null)
                                        {
                                            // use defined stress period from ISG-file
                                            stressPeriod = isgPackageFile.StressPeriod;
                                        }
                                        DateTime sdate = stressPeriod.DateTime.Value;
                                        string SNAME = stressPeriod.SNAME;

                                        // log.AddInfo("Converting ISG-file " + Path.GetFileName(isgFile.Filename) + " for stress period " + sdate + " ...", logIndentLevel + 2);
                                        string resultPath = FileUtils.EnsureFolderExists(Path.Combine(Path.Combine(Path.Combine(resultHandler.OutputPath, "ISG-grids"), SNAME), Path.GetFileName(isgFile.Filename)));
                                        ConvertToRIV(isgPackageFile, resultPath, true, sdate, sdate, resultHandler.Extent, isgRIVPackage, entryCount, log, logIndentLevel + 2);
                                    }
                                }
                            }
                            else
                            {
                                log.AddInfo("ISG-file has extent outside checked extent and is skipped: " + isgPackageFile.FName, logIndentLevel + 1);
                            }
                        }
                        else
                        {
                            if (kper != 0)
                            {
                                log.AddInfo("ISG-file is missing for KPER: " + kper, logIndentLevel + 1);
                            }
                            else
                            {
                                // skip missing ISG-file for this kper (which may be the case for kper=0)
                            }
                        }
                    }
                }
            }

            return isgRIVPackage;
        }

        /// <summary>
        /// Converts ISG-packagefile to IDF and add to specified package
        /// </summary>
        /// <param name="isgPackageFile"></param>
        /// <param name="resultPath"></param>
        /// <param name="usePeriod"></param>
        /// <param name="sdate">start date of period</param>
        /// <param name="edate">end date of period</param>
        /// <param name="extent">if null the modelextent is used</param>
        /// <param name="package"></param>
        /// <param name="entryCount"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected static void ConvertToRIV(ISGPackageFile isgPackageFile, string resultPath, bool usePeriod, DateTime? sdate, DateTime? edate, Extent extent, Package package, int entryCount, Log log, int logIndentLevel = 0)
        {
            ISGFile isgFile = isgPackageFile.ISGFile;
            string batchFilename = Path.Combine(resultPath, Path.GetFileNameWithoutExtension(isgFile.Filename) + "-conversion" + ".INI");
            FileUtils.EnsureFolderExists(batchFilename);

            StressPeriod startStressPeriod = package.Model.RetrieveStressPeriod(sdate);
            StressPeriod endStressPeriod = package.Model.RetrieveStressPeriod(edate);
            string postFix = "_" + Path.GetFileNameWithoutExtension(isgFile.Filename) + ((startStressPeriod != null) ? startStressPeriod.SNAME : string.Empty);

            if (extent == null)
            {
                extent = package.Model.GetExtent();
            }


            string sdateString = (sdate != null) ? sdate.Value.ToString("yyyyMMdd") : string.Empty;
            string edateString = (edate != null) ? edate.Value.ToString("yyyyMMdd") : string.Empty;
            string periodString = ((sdate != null) || (edate !=null)) ? sdateString + "-" + edateString : string.Empty;

            List<string> rivPartFilenames = null;

            if (isgRIVDictionary.ContainsKey(isgFile.Filename) && isgRIVDictionary[isgFile.Filename].ContainsKey(periodString))
            {
                rivPartFilenames = isgRIVDictionary[isgFile.Filename][periodString];
            }
            else
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(batchFilename);
                    sw.WriteLine("FUNCTION=ISGGRID");
                    sw.WriteLine("ISGFILE_IN=\"" + isgFile.Filename + "\"");
                    sw.WriteLine("CELL_SIZE=25.0");
                    sw.WriteLine("NODATA=-9999.0");
                    sw.WriteLine("WINDOW=" + extent.llx + "," + extent.lly + "," + extent.urx + "," + extent.ury);
                    sw.WriteLine("POSTFIX=" + postFix);
                    if (usePeriod)
                    {
                        sw.WriteLine("IPERIOD=2");
                        sw.WriteLine("SDATE=" + sdateString);
                        sw.WriteLine("EDATE=" + edateString);
                    }
                    else
                    {
                        sw.WriteLine("IPERIOD=1");
                    }
                    sw.WriteLine("ICDIST=0"); // obligatory line for iMOD 4.4
                    sw.WriteLine("ISAVE=1,1,1,1,0,0,0,0,0,0,0,0");
                    sw.WriteLine("OUTPUTFOLDER=\"" + resultPath + "\"");
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not create INI-file for ISG-conversion", ex);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Flush();
                        sw.Close();
                    }
                }

                int exitCode = IMODTool.StartBatchFunction(batchFilename, log, logIndentLevel, 0); // wait until finished
                if (exitCode == 0)
                {
                    if (!isgPackageFile.FCT.Equals(1.0f) || !isgPackageFile.IMP.Equals(0.0f))
                    {
                        log.AddWarning("FCT and IMP are ignored for ISG-file: " + Path.GetFileName(isgPackageFile.FName));
                    }

                    rivPartFilenames = new List<string>();
                    for (int partIdx = 0; partIdx < package.MaxPartCount; partIdx++)
                    {
                        string partFilename = Path.Combine(resultPath, package.PartAbbreviations[partIdx] + postFix + ".IDF");
                        rivPartFilenames.Add(partFilename);
                    }

                    if (!isgRIVDictionary.ContainsKey(isgFile.Filename))
                    {
                        isgRIVDictionary.Add(isgFile.Filename, new Dictionary<string, List<string>>());
                    }
                    isgRIVDictionary[isgFile.Filename].Add(periodString, rivPartFilenames);
                }
                else
                {
                    log.AddError("Unknown error: iMOD did not finish ISG-conversion for " + isgFile.Filename + ". ISG-check is skipped.");
                    return;
                }
            }

            int entryIdx = package.GetEntryCount(isgPackageFile.StressPeriod.KPER);
            AddRIVParts(package, isgPackageFile.ILAY, entryIdx, isgPackageFile.StressPeriod, rivPartFilenames);
        }

        private static void AddRIVParts(Package package, int ilay, int entryIdx, StressPeriod stressPeriod, List<string> rivPartFilenames)
        {
            for (int partIdx = 0; partIdx < package.MaxPartCount; partIdx++)
            {
                package.AddFile(ilay, 1.0f, 0.0f, rivPartFilenames[partIdx], entryIdx, partIdx, stressPeriod);
            }
        }
    }
}
