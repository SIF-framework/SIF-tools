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
    class STOCheckSettings : CheckSettings
    {
        private string minSTO;
        private string maxSTO;

        [Category("Warning-properties"), Description("The minimum valid storage value for this region (0 or larger)"), PropertyOrder(10)]
        public string MinSTO
        {
            get { return minSTO; }
            set
            {
                try
                {
                    float minSTOValue = float.Parse(value, englishCultureInfo);
                    if ((minSTOValue >= 0f) && (minSTOValue < float.Parse(maxSTO, englishCultureInfo)))
                    {
                        minSTO = minSTOValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        [Category("Warning-properties"), Description("The maximum valid storage value for this region (larger than 0)"), PropertyOrder(11)]
        public string MaxSTO
        {
            get { return maxSTO; }
            set
            {
                try
                {
                    float maxSTOValue = float.Parse(value, englishCultureInfo);
                    if ((maxSTOValue > 0f) && (maxSTOValue > float.Parse(minSTO, englishCultureInfo)))
                    {
                        maxSTO = maxSTOValue.ToString(englishCultureInfo);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        public override void LogSettings(Log log, int logIndentLevel = 0)
        {
            log.AddInfo("Minimum STO-value: " + minSTO, logIndentLevel);
            log.AddInfo("Maximum STO-value: " + maxSTO, logIndentLevel);
        }

        public STOCheckSettings(string checkName) : base(checkName)
        {
            // Define default values;
            minSTO = "0";
            MaxSTO = short.MaxValue.ToString();
        }

    }

    class STOCheck : Check
    {
        private STOCheckSettings settings;
        public override CheckSettings Settings
        {
            get { return settings; }
            set
            {
                if (value is STOCheckSettings)
                {
                    settings = (STOCheckSettings)value;
                }
            }
        }

        public override string Abbreviation
        {
            get { return "STO"; }
        }

        public override string Description
        {
            get { return "Checks storage values per model layer"; }
        }

        public STOCheck()
        {
            settings = new STOCheckSettings(this.Name);
        }

        public override void Run(Model model, CheckResultHandler resultHandler, Log log)
        {
            try
            {
                log.AddInfo("Checking STO-package ...");

                IDFPackage stoPackage = (IDFPackage)model.GetPackage(STOPackage.DefaultKey);
                if ((stoPackage == null) || !stoPackage.IsActive)
                {
                    log.AddWarning(this.Name, model.Runfilename, "STO-package is not active. " + this.Name + " is skipped.", 1);
                    return;
                }

                settings.LogSettings(log, 1);
                RunSTOCheck1(model, resultHandler, log);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in " + this.Name, ex);
            }
        }

        private void RunSTOCheck1(Model model, CheckResultHandler resultHandler, Log log)
        {
            ///////////////////////
            // Retrieve Packages //
            ///////////////////////

            // Retrieve STO-package
            IDFPackage stoPackage = (IDFPackage) model.GetPackage(STOPackage.DefaultKey);

            // Retrieve TOP and BOT-packages to calculate thicknesses
            IDFPackage topPackage = (IDFPackage)model.GetPackage(TOPPackage.DefaultKey);
            IDFPackage botPackage = (IDFPackage)model.GetPackage(BOTPackage.DefaultKey);
            bool hasTOPPackage = model.HasActivePackage(TOPPackage.DefaultKey);
            bool hasBOTPackage = model.HasActivePackage(BOTPackage.DefaultKey);
            if (!hasTOPPackage || !hasBOTPackage)
            {
                log.AddWarning(this.Name, model.Runfilename, "Missing TOP- and/or BOT-package, STO-check is skipped", 1);
                return;
            }

            ////////////////////////////////
            // Define legends and results //
            ////////////////////////////////

            CheckError NegativeSTOError1 = new CheckError(1, "Negative STO-value", "STO < 0");
            CheckError ZeroSTOError2 = new CheckError(2, "Zero STO-value", "STO = 0");
            CheckError InconsistentSTOError3 = new CheckError(4, "Inconsistent STO-file", "Inconsistent STO-file (STO=NoData, thickness>0)");

            IDFLegend errorLegend = CreateIDFLegend();
            errorLegend.AddClass(NegativeSTOError1.CreateLegendValueClass(Color.Orange));
            errorLegend.AddClass(ZeroSTOError2.CreateLegendValueClass(Color.Red));
            errorLegend.AddClass(InconsistentSTOError3.CreateLegendValueClass(Color.Purple));
            errorLegend.AddUpperRangeClass(CombinedResultLabel, true);
            errorLegend.AddInbetweenClasses(CombinedResultLabel, true);

            // Define warnings
            CheckWarning STORangeWarning1 = new CheckWarning(1, "STO outside range", "STO-value outside expected range ["
                + settings.MinSTO.ToString() + "," + settings.MaxSTO.ToString() + "]");
            CheckWarning InconsistentSTOWarning2 = new CheckWarning(2, "STO>0, thickness=0");

            IDFLegend warningLegend = CreateIDFLegend();
            warningLegend.AddClass(STORangeWarning1.CreateLegendValueClass(Color.Orange, true));
            warningLegend.AddClass(InconsistentSTOWarning2.CreateLegendValueClass(Color.Red, true));
            warningLegend.AddUpperRangeClass(CombinedResultLabel, true);
            warningLegend.AddInbetweenClasses(CombinedResultLabel, true);

            ///////////////////////////
            // Retrieve settingfiles //
            ///////////////////////////
            IDFFile minSTOSettingIDFFile = settings.GetIDFFile(settings.MinSTO, log, 1);
            IDFFile maxSTOSettingIDFFile = settings.GetIDFFile(settings.MaxSTO, log, 1);

            // Process all ANI-entries
            int kper = 0;
            for (int entryIdx = resultHandler.MinEntryNumber - 1; (entryIdx < stoPackage.GetEntryCount(kper)) && (entryIdx < resultHandler.MaxEntryNumber); entryIdx++)
            {
                log.AddInfo("Checking entry " + (entryIdx + 1) + " with " + Name + " ...", 1);

                // Retrieve files for current entry
                IDFFile stoIDFFile = stoPackage.GetIDFFile(entryIdx);
                if (stoIDFFile == null)
                {
                    log.AddError(stoPackage.Key, null, "Missing STO-file, STO-check is skipped for entry " + (entryIdx + 1), 1);
                }
                else
                {
                    IDFFile topIDFFile = topPackage.GetIDFFile(entryIdx);
                    IDFFile botIDFFile = botPackage.GetIDFFile(entryIdx);

                    IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
                    idfCellIterator.AddIDFFile(stoIDFFile);
                    idfCellIterator.AddIDFFile(topIDFFile);
                    idfCellIterator.AddIDFFile(botIDFFile);

                    // Check that STO-files have equal extent
                    idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);
                    if (idfCellIterator.IsEmptyExtent())
                    {
                        return;
                    }
                    else
                    {
                        // Create error IDFfiles for current layer
                        CheckErrorLayer stoErrorLayer = CreateErrorLayer(resultHandler, stoPackage, null, kper, entryIdx + 1, idfCellIterator.XStepsize, errorLegend);
                        stoErrorLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                        // Create warning IDFfiles for current layer
                        CheckWarningLayer stoWarningLayer = CreateWarningLayer(resultHandler, stoPackage, null, kper, entryIdx + 1, idfCellIterator.XStepsize, warningLegend);
                        stoWarningLayer.AddSourceFiles(idfCellIterator.GetIDFFiles());

                        // Iterate through cells
                        idfCellIterator.Reset();
                        while (idfCellIterator.IsInsideExtent())
                        {
                            float stoValue = idfCellIterator.GetCellValue(stoIDFFile);
                            float topValue = idfCellIterator.GetCellValue(topIDFFile);
                            float botValue = idfCellIterator.GetCellValue(botIDFFile);
                            float x = idfCellIterator.X;
                            float y = idfCellIterator.Y;

                            float minSTO = minSTOSettingIDFFile.GetValue(x, y);
                            float maxSTO = maxSTOSettingIDFFile.GetValue(x, y);

                            float thickness = float.NaN;
                            if (!topValue.Equals(float.NaN) && !topValue.Equals(topIDFFile.NoDataValue))
                            {
                                if (!botValue.Equals(float.NaN) && !botValue.Equals(botIDFFile.NoDataValue))
                                {
                                    thickness = topValue - botValue;
                                }
                            }

                            if (!stoValue.Equals(stoIDFFile.NoDataValue) && (stoValue < 0))
                            {
                                resultHandler.AddCheckResult(stoErrorLayer, x, y, NegativeSTOError1);
                            }
                            else if (stoValue.Equals(0f) || stoValue.Equals(stoIDFFile.NoDataValue))
                            {
                                if (thickness > 0)
                                {
                                    resultHandler.AddCheckResult(stoErrorLayer, x, y, InconsistentSTOError3);
                                }
                            }
                            else
                            {
                                // STO > 0
                                if ((stoValue < minSTO) || (stoValue > maxSTO))
                                {
                                    resultHandler.AddCheckResult(stoWarningLayer, x, y, STORangeWarning1);
                                }

                                if (thickness.Equals(0f))
                                {
                                    resultHandler.AddCheckResult(stoWarningLayer, x, y, InconsistentSTOWarning2);
                                }
                            }

                            idfCellIterator.MoveNext();
                        }

                        // Write errorfiles and add files to error handler
                        if (stoErrorLayer.HasResults())
                        {
                            stoErrorLayer.CompressLegend("Combined");
                            stoErrorLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(stoErrorLayer.SourceFiles);
                        }

                        // Write warningfiles
                        if (stoWarningLayer.HasResults())
                        {
                            stoWarningLayer.CompressLegend(CombinedResultLabel);
                            stoWarningLayer.WriteResultFile(log);
                            resultHandler.AddExtraMapFiles(stoWarningLayer.SourceFiles);
                        }

                        stoErrorLayer.ReleaseMemory(true);
                        stoWarningLayer.ReleaseMemory(true);
                    }
                }

            }
            settings.ReleaseMemory(true);
            stoPackage.ReleaseMemory(true);
        }
    }
}
