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
using OrderedPropertyGrid;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    [TypeConverter(typeof(PropertySorter))]
    class ANICheckSettings : CheckSettings
    {
        private string minAngle;
        private string maxAngle;
        private string minFactor;
        private string maxFactor;
        private UpscaleMethodEnum upscaleMethod;

        [Category("Warning-properties"), Description("The minimum valid anisotropy angle for this region (within range [0.0, 360.0])"), PropertyOrder(20)]
        public string MinAngle
        {
            get { return minAngle; }
            set
            {
                try
                {
                    float minAngleValue = float.Parse(value, englishCultureInfo);
                    if ((minAngleValue >= 0f) && (minAngleValue <= 360f) && (minAngleValue < float.Parse(maxAngle, englishCultureInfo)))
                    {
                        minAngle = minAngleValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        [Category("Warning-properties"), Description("The maximum valid anisotropy angle for this region (within range [0.0, 360.0])"), PropertyOrder(21)]
        public string MaxAngle
        {
            get { return maxAngle; }
            set
            {
                try
                {
                    float maxAngleValue = float.Parse(value, englishCultureInfo);
                    if ((maxAngleValue >= 0f) && (maxAngleValue <= 360f) && (maxAngleValue > float.Parse(minAngle, englishCultureInfo)))
                    {
                        maxAngle = maxAngleValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        [Category("Warning-properties"), Description("The minimum valid anisotropy factor for this region (within range [0.0,1.0])"), PropertyOrder(22)]
        public string MinFactor
        {
            get { return minFactor; }
            set
            {
                try
                {
                    float minFactorValue = float.Parse(value, englishCultureInfo);
                    if ((minFactorValue >= 0f) && (minFactorValue <= 1f) && (minFactorValue < float.Parse(maxFactor, englishCultureInfo)))
                    {
                        minFactor = minFactorValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        [Category("Warning-properties"), Description("The maximum valid anisotropy factor for this region (within range [0.0,1.0])"), PropertyOrder(23)]
        public string MaxFactor
        {
            get { return maxFactor; }
            set
            {
                try
                {
                    float maxFactorValue = float.Parse(value, englishCultureInfo);
                    if ((maxFactorValue >= 0f) && (maxFactorValue <= 1f) && (maxFactorValue > float.Parse(minFactor, englishCultureInfo)))
                    {
                        maxFactor = maxFactorValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        [Category("Scale-properties"), Description("Upscale method in case of resolution differences. Use 'Maximum' to only find coarse cells with problem for all finer cells inside"), PropertyOrder(10)]
        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        public ANICheckSettings(string checkName)
            : base(checkName)
        {
            minAngle = "0.0";
            maxAngle = "360.0";
            minFactor = "0.0";
            maxFactor = "1.0";
            upscaleMethod = UpscaleMethodEnum.Maximum;
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum angle: " + minAngle + " degrees", logIndentLevel);
            log.AddInfo("Maximum angle: " + maxAngle + " degrees", logIndentLevel);
            log.AddInfo("Minimum factor: " + MinFactor, logIndentLevel);
            log.AddInfo("Maximum factor: " + MaxFactor, logIndentLevel);
            log.AddInfo("Upscale method: " + upscaleMethod.ToString(), logIndentLevel);
        }
    }

    class ANICheck : Check
    {
        public override string Abbreviation
        {
            get { return "ANI"; }
        }

        public override string Description
        {
            get { return "Checks anisotropy angle and factor per model layer"; }
        }

        private ANICheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is ANICheckSettings)
                {
                    settings = (ANICheckSettings)value;
                }
            }
        }

        public ANICheck()
        {
            settings = new ANICheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking ANI-package...");
                settings.LogSettings(log, 1);
                RunANICheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }
        /// <summary>
        /// - Checks for inconsistency within ANI-files or with khv or kD-layers  (undefined/NoData, zero-thickness is currently not seen as an error or warning)
        /// - Checks for invalid/unexpected values
        /// - Checks file size and extent equal and also compared with khv/kD
        /// - Checks for NoData definitions of 0 (for factor and angle) !!! This will deactivate the package, see manual
        /// </summary>
        /// <param name="model"></param>
        /// <param name="resultHandler"></param>
        /// <param name="log"></param>
        protected virtual void RunANICheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            // Note: iMOD/imodflow manual: For those situations where a single model cell contains more than 
            // one of these anisotropic parameters, they will be up-scaled to the model cell. For the anisotropic angle, 
            // the most frequent occurrence will be used, as for the anisotropic factor, a mean value will be computed. 
            // This seems to be the most robust and fair trade-off between a coarsened model network and loss in detail.

            // NOTE: The ANI can be entered by means of IDF and GEN ﬁles!! 
            // Anisotropy can be entered by means of lines in GEN files (*.gen). The shape of the
            // line will be translated in anisotropy angles, and resistances that are read from a
            // associated *.dat file will be used to compute the anisotropy factor (see
            // parameter ANI in Data Set 10).
            // e.g. 
            // 2, (ANI)
            // 1,1.0,0.0,c:\model\anisotropy\imodflow_anifct1.idf
            // 1,1.0,0.0,c:\model\anisotropy\imodflow_aniangle1.idf
            // 2,1.0,0.0,c:\model\anisotropy\imodflow_aniline.gen
            // For now ignore this option...

            ///////////////////////
            // Retrieve Packages //
            ///////////////////////

            // Retrieve ANI-package, note: ANI-files can be either IDF (angle/factor) or GEN-files
            Package aniPackage = model.GetPackage(ANIPackage.DefaultKey);
            if ((aniPackage == null) || !aniPackage.IsActive)
            {
                log.AddWarning(this.Name, model.Runfilename, "ANI-package is not active. " + this.Name + " is skipped.", 1);
                return;
            }

            // Retrieve kD or kHV-package
            IDFPackage kdPackage = (IDFPackage)model.GetPackage(KDWPackage.DefaultKey);
            IDFPackage khvPackage = (IDFPackage)model.GetPackage(KHVPackage.DefaultKey);
            bool hasKDWPackage = model.HasActivePackage(KDWPackage.DefaultKey);
            bool hasKHVPackage = model.HasActivePackage(KHVPackage.DefaultKey);
            if ((hasKDWPackage && hasKHVPackage) || (!hasKDWPackage && !hasKHVPackage))
            {
                log.AddError(aniPackage.Key, model.Runfilename, "ANI-package is active, but neither or both KDW and KHV-packages are defined...", 2);
            }

            //            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            //            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            CheckError ZeroNoDataValueError1 = new CheckError(1, "NoData defined as zero", "NoData defined as zero, which disactivates package");
            CheckError InconsistentANIFilesError2 = new CheckError(2, "Inconsistent ANI-files", "Inconsistent ANI-files");
            CheckError InvalidAngleError2 = new CheckError(4, "Invalid ANI-angle", "The anisotropy angle is outside [0, 360]");
            CheckError InvalidFactorError3 = new CheckError(8, "Invalid ANI-factor", "The anisotropy factor is outside [0, 1]");
            CheckError MissingKDHValueError4 = new CheckError(16, "Missing kD- or kH-value", "Missing kD- or kH-value for defined ANI-values");

            IDFLegend errorLegend = CreateIDFLegend();
            errorLegend.AddClass(ZeroNoDataValueError1.CreateLegendValueClass(Color.Gold));
            errorLegend.AddClass(InconsistentANIFilesError2.CreateLegendValueClass(Color.Orange));
            errorLegend.AddClass(InvalidAngleError2.CreateLegendValueClass(Color.Red));
            errorLegend.AddClass(InvalidFactorError3.CreateLegendValueClass(Color.DarkRed));
            errorLegend.AddClass(MissingKDHValueError4.CreateLegendValueClass(Color.Purple));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            CheckWarning AngleRangeWarning1 = new CheckWarning(1, "ANI-angle outside defined region-range", "ANI-angle is outside expected range ["
                + settings.MinAngle.ToString() + "," + settings.MaxAngle.ToString() + "]");
            CheckWarning FactorRangeWarning2 = new CheckWarning(2, "ANI-factor outside defined region-range", "ANI-factor is outside the expected range ["
                + settings.MinFactor.ToString() + "," + settings.MaxFactor.ToString() + "]");
            //            CheckWarning AnisotropyInDummyLayerWarning3 = new CheckWarning(4, "Anisotropy found in dummy layer", "Anisotropy values found in dummy layer.");

            IDFLegend warningLegend = CreateIDFLegend();
            warningLegend.AddClass(AngleRangeWarning1.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(FactorRangeWarning2.CreateLegendValueClass(Color.Red, true));
            //            warningLegend.AddClass(AnisotropyInDummyLayerWarning3.CreateLegendValueClass(Color.Blue, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minFactorSettingIDFFile = settings.GetIDFFile(settings.MinFactor, log, 1);
            IDFFile maxFactorSettingIDFFile = settings.GetIDFFile(settings.MaxFactor, log, 1);
            IDFFile minAngleSettingIDFFile = settings.GetIDFFile(settings.MinAngle, log, 1);
            IDFFile maxAngleSettingIDFFile = settings.GetIDFFile(settings.MaxAngle, log, 1);

            // Process all ANI-entries
            int kper = 1;
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < aniPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                // Retrieve files for current entry: note for ANI-files two types of specification are allowed in the runfile: Factor/Angle and GEN-files
                IDFFile aniFactorIDFFile = null;
                IDFFile aniAngleIDFFile = null;
                GENFile aniGENFile = null;

                // Check file type
                PackageFile aniFile = aniPackage.GetPackageFile(entryIdx, 0, kper);
                if (aniFile is IDFPackageFile)
                {
                    aniFactorIDFFile = (IDFFile)aniPackage.GetIMODFile(entryIdx, ANIPackage.FactorPartIdx, kper);
                    aniAngleIDFFile = (IDFFile)aniPackage.GetIMODFile(entryIdx, ANIPackage.AnglePartIdx, kper);
                }
                else
                {
                    aniGENFile = (GENFile)aniPackage.GetIMODFile(entryIdx, 0, kper);
                    log.AddWarning(aniPackage.Key, aniGENFile.Filename, "ANI-files of type " + aniFile.GetType().Name + " are currently not supported", 1);

                    // TODO: convert GEN-file to factor/angle
                }

                IDFFile kdIDFFile = null;
                IDFFile khIDFFile = null;
                if (hasKDWPackage)
                {
                    kdIDFFile = kdPackage.GetIDFFile(entryIdx);
                }
                if (hasKHVPackage)
                {
                    khIDFFile = khvPackage.GetIDFFile(entryIdx);
                }

                //                IDFFile topIDFFile = null;
                //                IDFFile botIDFFile = null;
                //                if ((topPackage != null) && (botPackage != null))
                //                {
                //                    topIDFFile = topPackage.GetIDFFile(entryIdx);
                //                    botIDFFile = botPackage.GetIDFFile(entryIdx);
                //                }

                if ((aniFactorIDFFile == null) || (aniAngleIDFFile == null))
                {
                    log.AddError(aniPackage.Key, null, "One or more ANI-files are missing, ANI-check is skipped for entry " + (entryIdx + 1), 1);
                }
                else
                {
                    List<IDFFile> aniPackageIDFFiles = new List<IDFFile>() { aniFactorIDFFile, aniAngleIDFFile };
                    int constantFileCount = Check.GetConstantIDFFileCount(aniPackageIDFFiles);

                    IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                    idfCellIterator.AddIDFFile(aniFactorIDFFile);
                    idfCellIterator.AddIDFFile(aniAngleIDFFile);
                    idfCellIterator.AddIDFFile(kdIDFFile);
                    idfCellIterator.AddIDFFile(khIDFFile);
                    //                    idfCellIterator.AddIDFFile(topIDFFile);
                    //                    idfCellIterator.AddIDFFile(botIDFFile);

                    // Check that ANI-files have equal extent
                    idfCellIterator.CheckExtent(log, 2);
                    if (idfCellIterator.IsEmptyExtent())
                    {
                        return;
                    }
                    else
                    {
                        // Create error IDFfiles for current layer
                        CheckErrorLayer aniErrorLayer = CreateErrorLayer(resultHandler, aniPackage, kper, entryIdx + 1, idfCellIterator.XStepsize, errorLegend);
                        aniErrorLayer.AddSourceFiles(aniPackageIDFFiles);

                        // Create warning IDFfiles for current layer
                        CheckWarningLayer aniWarningLayer = CreateWarningLayer(resultHandler, aniPackage, kper, entryIdx + 1, idfCellIterator.XStepsize, warningLegend);
                        aniWarningLayer.AddSourceFiles(aniPackageIDFFiles);

                        // Iterate through cells
                        idfCellIterator.Reset();
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float aniFactorValue = idfCellIterator.GetCellValue(aniFactorIDFFile);
                            float aniAngleValue = idfCellIterator.GetCellValue(aniAngleIDFFile);
                            float kdValue = idfCellIterator.GetCellValue(kdIDFFile);
                            float khValue = idfCellIterator.GetCellValue(khIDFFile);
                            //                            float topValue = idfCellIterator.GetCellValue(topIDFFile);
                            //                            float botValue = idfCellIterator.GetCellValue(botIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            float minFactor = minFactorSettingIDFFile.GetValue(x, y);
                            float maxFactor = maxFactorSettingIDFFile.GetValue(x, y);
                            float minAngle = minAngleSettingIDFFile.GetValue(x, y);
                            float maxAngle = maxAngleSettingIDFFile.GetValue(x, y);

                            int partCount = 0;
                            if (!aniFactorValue.Equals(aniFactorIDFFile.NoDataValue))
                            {
                                partCount++;
                                if ((aniFactorValue < 0f) || (aniFactorValue > 1f))
                                {
                                    resultHandler.AddCheckResult(aniErrorLayer, x, y, InvalidFactorError3);
                                }
                                else if ((aniFactorValue < minFactor) || (aniFactorValue > maxFactor))
                                {
                                    resultHandler.AddCheckResult(aniWarningLayer, x, y, FactorRangeWarning2);
                                }
                            }
                            else if (aniFactorIDFFile.NoDataValue == 0)
                            {
                                resultHandler.AddCheckResult(aniErrorLayer, x, y, ZeroNoDataValueError1);
                            }
                            if (!aniAngleValue.Equals(aniAngleIDFFile.NoDataValue))
                            {
                                partCount++;
                                if ((aniAngleValue < 0f) || (aniAngleValue > 360f))
                                {
                                    resultHandler.AddCheckResult(aniErrorLayer, x, y, InvalidAngleError2);
                                }
                                else if ((aniAngleValue < minAngle) || (aniAngleValue > maxAngle))
                                {
                                    resultHandler.AddCheckResult(aniWarningLayer, x, y, AngleRangeWarning1);
                                }
                            }
                            else if (aniAngleIDFFile.NoDataValue == 0)
                            {
                                resultHandler.AddCheckResult(aniErrorLayer, x, y, ZeroNoDataValueError1);
                            }

                            // correct for constantFiles: since constant file have a value everywhere, correct for cells that have no other ANI-values defined
                            if (constantFileCount > 0)
                            {
                                partCount -= constantFileCount;
                            }

                            if (partCount > 0)
                            {
                                if (partCount != aniPackage.MaxPartCount)
                                {
                                    resultHandler.AddCheckResult(aniErrorLayer, x, y, InconsistentANIFilesError2);
                                }
                                // Check for defined ANI-cells without defined kD- or kH-cells. 
                                if (hasKDWPackage)
                                {
                                    if (kdValue.Equals(float.NaN) || kdValue.Equals(kdIDFFile.NoDataValue))
                                    {
                                        resultHandler.AddCheckResult(aniErrorLayer, x, y, InconsistentANIFilesError2);
                                        if (!aniErrorLayer.SourceFiles.Contains(kdIDFFile))
                                        {
                                            aniErrorLayer.AddSourceFile(kdIDFFile);
                                        }
                                    }
                                }
                                if (hasKHVPackage)
                                {
                                    if (khValue.Equals(float.NaN) || khValue.Equals(khIDFFile.NoDataValue))
                                    {
                                        resultHandler.AddCheckResult(aniErrorLayer, x, y, InconsistentANIFilesError2);
                                        if (!aniErrorLayer.SourceFiles.Contains(khIDFFile))
                                        {
                                            aniErrorLayer.AddSourceFile(khIDFFile);
                                        }
                                    }
                                }
                            }

                            idfCellIterator.MoveNext();
                        }

                        // Write errorfiles and add files to error handler
                        if (aniErrorLayer.HasResults())
                        {
                            aniErrorLayer.CompressLegend("Combined");
                            aniErrorLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(aniErrorLayer.SourceFiles);
                        }

                        // Write warningfiles
                        if (aniWarningLayer.HasResults())
                        {
                            aniWarningLayer.CompressLegend(CombinedResultLabel);
                            aniWarningLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(aniWarningLayer.SourceFiles);
                        }

                        aniErrorLayer.ReleaseMemory(true);
                        aniWarningLayer.ReleaseMemory(true);
                    }
                }

                aniPackage.ReleaseMemory(true);
            }
            settings.ReleaseMemory(true);
            aniPackage.ReleaseMemory(true);
        }
    }
}
