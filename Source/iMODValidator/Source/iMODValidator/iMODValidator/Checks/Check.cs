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
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.iMODValidator.Actions;
using Sweco.SIF.iMODValidator.Checks.CheckResults;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    /// <summary>
    /// Base Check class, to be overridden
    /// </summary>
    public abstract class Check : ValidatorAction, IEquatable<ValidatorAction>, IComparable<ValidatorAction>
    {
        public const string IMODFilesSubDir = "checks-imodfiles";
        public const string CombinedResultLabel = "Combined result";

        public override string ActionType
        {
            get { return "Check"; }
        }

        /// <summary>
        /// number of CheckError objects that have been created by this Check-object.
        /// the dictionary key specifies a unique index for each new ErrorLayer
        /// </summary>
        protected Dictionary<int, int> definedErrorCountDictionary;
        /// <summary>
        /// number of CheckWarning objects that have been created by this Check-object.
        /// the dictionary key specifies a unique index for each new WarningLayer
        /// </summary>
        protected Dictionary<int, int> definedWarningCountDictionary;

        public abstract CheckSettings Settings { get; set; }

        /// <summary>
        /// Resets parameters to initial values before starting a new run
        /// </summary>
        public override void Reset()
        {
            definedErrorCountDictionary = null;
            definedWarningCountDictionary = null;
        }

        protected CheckError CreateCheckError(string shortDescription, string detailedDescription = "", int errorLayerIndex = 0)
        {
            if (definedErrorCountDictionary == null)
            {
                definedErrorCountDictionary = new Dictionary<int, int>();
            }
            if (!definedErrorCountDictionary.ContainsKey(errorLayerIndex))
            {
                definedErrorCountDictionary.Add(errorLayerIndex, 0);
            }
            int resultValue = (int)Math.Pow(2, definedErrorCountDictionary[errorLayerIndex]);
            CheckError checkError = new CheckError(resultValue, shortDescription, detailedDescription);
            definedErrorCountDictionary[errorLayerIndex]++;
            return checkError;
        }

        protected CheckWarning CreateCheckWarning(string shortDescription, string detailedDescription = "", int warningLayerIndex = 0)
        {
            if (definedWarningCountDictionary == null)
            {
                definedWarningCountDictionary = new Dictionary<int, int>();
            }
            if (!definedWarningCountDictionary.ContainsKey(warningLayerIndex))
            {
                definedWarningCountDictionary.Add(warningLayerIndex, 0);
            }
            int resultValue = (int)Math.Pow(2, definedWarningCountDictionary[warningLayerIndex]);
            CheckWarning checkWarning = new CheckWarning(resultValue, shortDescription, detailedDescription);
            definedWarningCountDictionary[warningLayerIndex]++;
            return checkWarning;
        }

        /// <summary>
        /// Create CheckErrorLayer with IDF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="package"></param>
        /// <param name="subString"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="cellSize"></param>
        /// <param name="legend"></param>
        /// <returns></returns>
        protected CheckErrorLayer CreateErrorLayer(CheckResultHandler resultHandler, Package package, string subString, int kper, int ilay, float cellSize, ClassLegend legend)
        {
            CheckErrorLayer checkErrorLayer = new CheckErrorLayer(this, package, subString, kper, ilay, resultHandler.Model.StartDate, resultHandler.Extent, cellSize, resultHandler.NoDataValue, GetIMODFilesPath(resultHandler.Model), legend, resultHandler.UseSparseGrids);
            resultHandler.AddResultLayer(checkErrorLayer);
            return checkErrorLayer;
        }

        /// <summary>
        /// Create CheckErrorLayer with IPF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="package"></param>
        /// <param name="substring"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="legend"></param>
        /// <returns></returns>
        protected CheckErrorLayer CreateErrorLayer(CheckResultHandler resultHandler, Package package, string substring, int kper, int ilay, ClassLegend legend)
        {
            CheckErrorLayer checkErrorLayer = new CheckErrorLayer(this, package, substring, kper, ilay, resultHandler.Model.StartDate, GetIMODFilesPath(resultHandler.Model), legend);
            resultHandler.AddResultLayer(checkErrorLayer);
            return checkErrorLayer;
        }

        /// <summary>
        /// Create CheckWarningLayer with IPF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="package"></param>
        /// <param name="substring"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="cellSize"></param>
        /// <param name="legend"></param>
        /// <returns></returns>
        protected CheckWarningLayer CreateWarningLayer(CheckResultHandler resultHandler, Package package, string substring, int kper, int ilay, float cellSize, ClassLegend legend)
        {
            CheckWarningLayer checkWarningLayer = new CheckWarningLayer(this, package, substring, kper, ilay, resultHandler.Model.StartDate, resultHandler.Extent, cellSize, resultHandler.NoDataValue, GetIMODFilesPath(resultHandler.Model), legend, resultHandler.UseSparseGrids);
            resultHandler.AddResultLayer(checkWarningLayer);
            return checkWarningLayer;
        }

        /// <summary>
        /// Create CheckWarningLayer with IPF-files as the underlying resultFile
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="package"></param>
        /// <param name="substring"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="legend"></param>
        /// <returns></returns>
        protected CheckWarningLayer CreateWarningLayer(CheckResultHandler resultHandler, Package package, string substring, int kper, int ilay, ClassLegend legend)
        {
            CheckWarningLayer checkWarningLayer = new CheckWarningLayer(this, package, substring, kper, ilay, resultHandler.Model.StartDate, GetIMODFilesPath(resultHandler.Model), legend);
            resultHandler.AddResultLayer(checkWarningLayer);
            return checkWarningLayer;
        }

        /// <summary>
        /// Create CheckDetailLayer with IPF-file as underlying iMOD-file: an extra file with details about issues that can not be stored in the result iMOD-file (IDF or IPF)
        /// </summary>
        /// <param name="resultHandler"></param>
        /// <param name="package"></param>
        /// <param name="subString"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="legend"></param>
        /// <returns></returns>
        protected CheckDetailLayer CreateDetailLayer(CheckResultHandler resultHandler, Package package, string subString, int kper, int ilay, ClassLegend legend = null)
        {
            CheckDetailLayer checkDetailLayer = new CheckDetailLayer(resultHandler.Model, this, package, subString, kper, ilay, legend);
            resultHandler.AddResultLayer(checkDetailLayer);
            return checkDetailLayer;
        }

        protected IDFLegend CreateIDFLegend(bool addDefaultZeroClass = true)
        {
            IDFLegend legend = new IDFLegend("Legend for " + this.Name);
            if (addDefaultZeroClass)
            {
                legend.AddClass(new ValueLegendClass(0, "Nothing found", Color.White));
            }
            return legend;
        }

        protected IPFLegend CreateIPFLegend(bool addDefaultZeroClass = true)
        {
            IPFLegend legend = new IPFLegend("Legend for " + this.Name);
            if (addDefaultZeroClass)
            {
                legend.AddClass(new ValueLegendClass(0, "Nothing found", Color.White));
            }
            return legend;
        }

        protected void CheckMaxChange(CheckResultHandler resultHandler, IDFFile checkedIDFFile, CheckSettings settings, string maxRelChangeLevelSetting, string maxAbsChangeLevelSetting, string minCellCountSetting, string maxCellCountSetting, CheckWarningLayer warningLayer, CheckWarning warning, Log log, int logIndentLevel = 0)
        {
            // Retrieve setting-values
            IDFFile maxRelChangeLevelIDFFile = settings.GetIDFFile(maxRelChangeLevelSetting, log, 1);
            IDFFile maxAbsChangeLevelIDFFile = settings.GetIDFFile(maxAbsChangeLevelSetting, log, 1);
            IDFFile minCellCountIDFFile = settings.GetIDFFile(minCellCountSetting, log, 1);
            IDFFile maxCellCountIDFFile = settings.GetIDFFile(maxCellCountSetting, log, 1);
            CheckMaxChange(resultHandler, checkedIDFFile, maxRelChangeLevelIDFFile, maxAbsChangeLevelIDFFile, minCellCountIDFFile, maxCellCountIDFFile, warningLayer, warning, log, logIndentLevel);
        }

        protected void CheckMaxChange(CheckResultHandler resultHandler, IDFFile checkedIDFFile, IDFFile maxRelChangeIDFFile, IDFFile maxAbsChangeIDFFile, IDFFile minCellCountIDFFile, IDFFile maxCellCountIDFFile, CheckWarningLayer warningLayer, CheckWarning warning, Log log, int logIndentLevel = 0)
        {
            IDFCellIterator idfCellIterator = new IDFCellIterator(resultHandler.Extent);
            //idfCellIterator.AddIDFFile(surfacelevelIDFFile);
            idfCellIterator.AddIDFFile(checkedIDFFile);
            idfCellIterator.AddIDFFile(maxRelChangeIDFFile);
            idfCellIterator.AddIDFFile(maxAbsChangeIDFFile);
            idfCellIterator.AddIDFFile(minCellCountIDFFile);
            idfCellIterator.AddIDFFile(maxCellCountIDFFile);

            if (idfCellIterator.IsEmptyExtent())
            {
                idfCellIterator.CheckExtent(log, logIndentLevel + 1, LogLevel.Warning, ": check extent of RIV-files and/or surface level file");
                return;
            }
            idfCellIterator.CheckExtent(log, 2, LogLevel.Warning);

            int cellDistance = 1;
            int neighbourCellCount = (2 * cellDistance + 1) * (2 * cellDistance + 1) - 1;

            // Iterate through cells
            idfCellIterator.Reset();
            while (idfCellIterator.IsInsideExtent())
            {
                //float surfacelevelValue = idfCellIterator.GetCellValue(s
                float cellValue = idfCellIterator.GetCellValue(checkedIDFFile);
                float x = idfCellIterator.X;
                float y = idfCellIterator.Y;
                float maxRelChangeLevel = idfCellIterator.GetCellValue(maxRelChangeIDFFile);
                float maxAbsChangeLevel = idfCellIterator.GetCellValue(maxAbsChangeIDFFile);
                float maxChangeLevelPerCell = Math.Min(maxRelChangeLevel * idfCellIterator.XStepsize, maxAbsChangeLevel);
                float minCellCount = idfCellIterator.GetCellValue(minCellCountIDFFile);
                float maxCellCount = idfCellIterator.GetCellValue(maxCellCountIDFFile);

                if (!cellValue.Equals(float.NaN) && !cellValue.Equals(checkedIDFFile.NoDataValue))
                {
                    float[][] neighbourCellValues = idfCellIterator.GetCellValues(checkedIDFFile, cellDistance);

                    int changedValueCellCount = 0;
                    int noDataCellCount = 0;
                    for (int rowIdx = 0; rowIdx < neighbourCellValues.Length; rowIdx++)
                    {
                        for (int colIdx = 0; colIdx < neighbourCellValues.Length; colIdx++)
                        {
                            float neighbourCellValue = neighbourCellValues[rowIdx][colIdx];
                            if (!neighbourCellValue.Equals(float.NaN) && !neighbourCellValue.Equals(checkedIDFFile.NoDataValue))
                            {
                                float valueChange = Math.Abs(neighbourCellValue - cellValue);
                                if (valueChange > maxChangeLevelPerCell)
                                {
                                    changedValueCellCount++;
                                }
                            }
                            else
                            {
                                noDataCellCount++;
                            }
                        }
                    }

                    // Check that there are non-NoData values around the cell and that at least one of these differed more than the specified maximum value
                    if ((noDataCellCount < neighbourCellCount) && (changedValueCellCount > 0))
                    {
                        changedValueCellCount += noDataCellCount;
                        if ((changedValueCellCount > minCellCount) && (changedValueCellCount < maxCellCount))
                        {
                            resultHandler.AddCheckResult(warningLayer, x, y, warning);
                        }
                    }
                }

                idfCellIterator.MoveNext();
            }
        }

        /// <summary>
        /// Checks if the given list of lists containt the given list of IDF-files
        /// </summary>
        /// <param name="IDFFileLists"></param>
        /// <param name="idfFiles"></param>
        /// <returns></returns>
        protected static bool IsIDFFileListInLists(List<List<IDFFile>> IDFFileLists, List<IDFFile> idfFiles)
        {
            bool isChecked = false;
            foreach (List<IDFFile> someList in IDFFileLists)
            {
                if (IsIDFFileListInList(someList, idfFiles))
                {
                    return true;
                }
            }
            return isChecked;
        }

        protected static bool IsIDFFileListInList(List<IDFFile> idfList, List<IDFFile> idfFiles)
        {
            bool containsFiles = true;
            foreach (IDFFile someIDFFile in idfFiles)
            {
                if (!idfList.Contains(someIDFFile))
                {
                    return false;
                }
            }
            return containsFiles;
        }

        protected string[] SelectResultFileSubset(string[] resultFilenames, List<int> yearSelection = null, List<int> monthSelection = null, List<int> daySelection = null)
        {
            List<string> resultFileSubset = new List<string>();
            try
            {
                // Get first and last dates
                string firstDateString = IMODUtils.GetStressPeriodString(resultFilenames[0]);
                DateTime firstDate = IMODUtils.ParseStressPeriodString(firstDateString);
                string lastDateString = IMODUtils.GetStressPeriodString(resultFilenames[resultFilenames.Length - 1]);
                DateTime lastDate = IMODUtils.ParseStressPeriodString(lastDateString);
                int yearCount = (int)Math.Round(lastDate.Subtract(firstDate).Days / 365f, 0);

                if ((yearSelection == null) || (yearSelection.Count() == 0))
                {
                    yearSelection = new List<int>();
                    if (yearCount > 2)
                    {
                        // yearSelection.Add(firstDate.Year + (int)(yearCount * 0.2f));
                        yearSelection.Add(firstDate.Year + (int)(yearCount * 0.5f));
                        yearSelection.Add(firstDate.Year + yearCount - 1);
                    }
                    else
                    {
                        yearSelection.Add(firstDate.Year);
                        yearSelection.Add(firstDate.Year + 1);
                    }
                }

                if ((monthSelection == null) || (monthSelection.Count() == 0))
                {
                    monthSelection = new List<int>() { 1, 3, 5, 7, 9, 11 };
                }

                if ((daySelection == null) || (daySelection.Count() == 0))
                {
                    daySelection = new List<int>() { 14 };
                }

                foreach (string resultFilename in resultFilenames)
                {
                    try
                    {
                        int ilay = IMODUtils.GetLayerNumber(resultFilename);
                        string stressperiodString = IMODUtils.GetStressPeriodString(resultFilename);
                        DateTime date = IMODUtils.ParseStressPeriodString(stressperiodString);
                        if (yearSelection.Contains(date.Year) && monthSelection.Contains(date.Month) && daySelection.Contains(date.Day))
                        {
                            resultFileSubset.Add(resultFilename);
                        }
                    }
                    catch (Exception)
                    {
                        // cannot parse datestring, skip file
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not parse and select resultfiles in " + Path.GetDirectoryName(resultFilenames[0]), ex);
            }

            return resultFileSubset.ToArray();
        }

        public override void Run(Model model, ResultHandler resultHandler, Log log)
        {
            if (resultHandler is CheckResultHandler)
            {
                Run(model, (CheckResultHandler)resultHandler, log);
            }
            else
            {
                throw new Exception("Check can only be run with an " + typeof(CheckResultHandler).Name + ", not a: " + resultHandler.GetType().Name);
            }
        }

        /// <summary>
        /// Retrieve path to store results of this check
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override string GetIMODFilesPath(Model model)
        {
            return Path.Combine(model.ToolOutputPath, Check.IMODFilesSubDir);
        }

        abstract public void Run(Model model, CheckResultHandler resultHandler, Log log);

        public bool Equals(Check other)
        {
            return base.Equals(other);
        }

        public int CompareTo(Check other)
        {
            return base.CompareTo(other);
        }
    }
}

