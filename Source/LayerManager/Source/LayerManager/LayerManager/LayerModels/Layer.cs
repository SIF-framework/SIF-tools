// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.LayerModels
{
    /// <summary>
    /// Available types of layers
    /// </summary>
    public enum LayerType
    {
        Aquifer,
        Aquitard,
        Complex
    }

    /// <summary>
    /// Class for storing properties of an iMODLayer (aquifer or aquitard)
    /// </summary>
    public class Layer
    {
        public const int Warning1Code_unexpectedKDC = 1;
        public const string Warning1String_unexpectedKDC = "Unexpected kD/c-value (TOP=BOT, kDc<>0/noData)";
        public const int Warning2Code_missingKDC = 2;
        public const string Warning2String_missingKDC = "Missing kD/c-value (TOP>BOT, kDc=Default)";
        public const int Error1Code_layerInconsistency = 1;
        public const string Error1String_layerInconsistency = "Layer inconsistency (TOP<BOT)";
        public const int Error2Code_missingKDC = 2;
        public const string Error2String_missingKDC = "Missing kD/c-value (TOP>BOT, kDc=0/noData)";
        public const int Error3Code_invalidKDC = 4;
        public const string Error3String_invalidKDC = "Invalid kD/c-value (Inf,NaN or negative)";
        public const int Error4Code_unexpectedLevelInequality = 8;
        public const string Error4String_unexpectedLevelInequality = "Unexpected level inequality (BOT_L(i-1)<>TOP_L(i))";

        /// <summary>
        /// Defines maximum difference between to layer levels to be regarded as equal levels
        /// </summary>
        public static float LevelTolerance = Properties.Settings.Default.LevelTolerance;

        /// <summary>
        /// Define if checks should report unexpected kdc values with default values in dummy layers.
        /// Often these values are set in the model just to prevent a zero value in dummy layers
        /// </summary>
        public static bool IsDummyDefaultKDCSkipped = true;

        public string LayerName { get; set; }
        public int ModellayerNumber { get; set; }
        public LayerType LayerType { get; set; }
        public IDFFile TOPIDFFile { get; set; }
        public IDFFile BOTIDFFile { get; set; }
        public IDFFile KDCIDFFile { get; set; }
        public bool HasFixedSurfaceTop { get; set; }

        protected LayerModel LayerModel;

        public Layer(LayerModel layerModel)
        {
            this.LayerModel = layerModel;
            this.LayerName = null;
        }

        public Layer(string layerName, int modellayerNumber, LayerType layerType, IDFFile topIDFFile, IDFFile botIDFFile, IDFFile kdcIDFFile)
        {
            this.LayerModel = null;
            this.LayerName = layerName;
            this.ModellayerNumber = modellayerNumber;
            this.LayerType = layerType;
            this.TOPIDFFile = topIDFFile;
            this.BOTIDFFile = botIDFFile;
            this.KDCIDFFile = kdcIDFFile;
        }

        public Layer(LayerModel layerModel, int modellayerNumber, LayerType layerType, IDFFile topIDFFile, IDFFile botIDFFile, IDFFile kdcIDFFile)
        {
            this.LayerModel = layerModel;
            this.LayerName = null;
            this.ModellayerNumber = modellayerNumber;
            this.LayerType = layerType;
            this.TOPIDFFile = topIDFFile;
            this.BOTIDFFile = botIDFFile;
            this.KDCIDFFile = kdcIDFFile;
        }

        public static bool IsEqualHeight(float height1, float height2)
        {
            return (Math.Abs(height1 - height2) <= LevelTolerance);
        }

        public static string RetrieveShortWarningLabel(int warningCode)
        {
            switch (warningCode)
            {
                case 0:
                    return string.Empty;
                case Warning1Code_unexpectedKDC:
                    return "Unexepected KDC";
                case Warning2Code_missingKDC:
                    return "Missing KDC";
                default:
                    throw new Exception("Unknown warning code: " + warningCode);
            }
        }

        public static string RetrieveShortErrorLabel(int errorCode)
        {
            switch (errorCode)
            {
                case 0:
                    return string.Empty;
                case Error1Code_layerInconsistency:
                    return "Layer Inconsistency";
                case Error2Code_missingKDC:
                    return "Missing KDC";
                case Error3Code_invalidKDC:
                    return "Invalid KDC";
                case Error4Code_unexpectedLevelInequality:
                    return "Unxepected level inequality";
                default:
                    throw new Exception("Unknown warning code: " + errorCode);
            }
        }

        public static List<int> SplitIssueCodeSum(int issueCodeSum)
        {
            int MaxIssueCodeIdx = 5;
            List<int> issueCodes = new List<int>();
            int remainingIssueCodeSum = issueCodeSum;
            for (int idx = MaxIssueCodeIdx; idx >= 0; idx--)
            {
                int issueCode = (int)Math.Pow(2, idx);
                if (remainingIssueCodeSum >= issueCode)
                {
                    issueCodes.Add(issueCode);
                    remainingIssueCodeSum -= issueCode;
                }
            }
            return issueCodes;
        }

        public static string RetrieveLongWarningLabel(int warningCode)
        {
            switch (warningCode)
            {
                case Warning1Code_unexpectedKDC:
                    return Warning1String_unexpectedKDC;
                case Warning2Code_missingKDC:
                    return Warning2String_missingKDC;
                default:
                    throw new Exception("Unknown warning code: " + warningCode);
            }
        }

        public static string RetrieveLongErrorLabel(int errorCode)
        {
            switch (errorCode)
            {
                case Error1Code_layerInconsistency:
                    return Error1String_layerInconsistency;
                case Error2Code_missingKDC:
                    return Error2String_missingKDC;
                case Error3Code_invalidKDC:
                    return Error3String_invalidKDC;
                case Error4Code_unexpectedLevelInequality:
                    return Error4String_unexpectedLevelInequality;
                default:
                    throw new Exception("Unknown error code: " + errorCode);
            }
        }

        public Layer GetLayerBelow()
        {
            // As an initial guess, assume this layer is an aquifer
            int layerBelowModellayerNumber = ModellayerNumber;
            LayerType layerBelowLayerType = LayerType.Aquitard;
            if (LayerType == LayerType.Aquitard)
            {
                layerBelowModellayerNumber++;
                layerBelowLayerType = LayerType.Aquifer;
            }

            return LayerModel.GetLayer(layerBelowModellayerNumber, layerBelowLayerType);
        }

        public Layer GetLayerAbove()
        {
            // As an initial guess, assume this layer is an aquitard
            int layerAboveModellayerNumber = ModellayerNumber;
            LayerType layerAboveLayerType = LayerType.Aquifer;
            if (LayerType == LayerType.Aquifer)
            {
                layerAboveModellayerNumber--;
                layerAboveLayerType = LayerType.Aquitard;
            }

            return LayerModel.GetLayer(layerAboveModellayerNumber, layerAboveLayerType);
        }

        public void CheckDefinitions()
        {
            CheckDefinitions(TOPIDFFile, BOTIDFFile);
            if (KDCIDFFile != null)
            {
                CheckDefinitions(TOPIDFFile, KDCIDFFile);
            }
        }

        public static void CheckDefinitions(IDFFile idfFile1, IDFFile idfFile2)
        {
            Extent extent1 = idfFile1.Extent;
            Extent extent2 = idfFile2.Extent;

            if (!extent1.Equals(extent2))
            {
                throw new ToolException("Extent of " + Path.GetFileName(idfFile1.Filename) + " (" + extent1.ToString()
                    + ") should match extent of " + Path.GetFileName(idfFile2.Filename) + " (" + extent1.ToString() + ")");
            }

            if (!idfFile1.XCellsize.Equals(idfFile2.XCellsize))
            {
                throw new ToolException("Cellsize of " + Path.GetFileName(idfFile1.Filename) + " (" + idfFile1.XCellsize.ToString()
                    + ") should match cellsize of " + Path.GetFileName(idfFile2.Filename) + " (" + idfFile2.XCellsize.ToString() + ")");
            }

            if (Properties.Settings.Default.IsInconsistentNoDataChecked && !idfFile1.NoDataValue.Equals(idfFile2.NoDataValue))
            {
                throw new ToolException("NoData-value of " + Path.GetFileName(idfFile1.Filename) + " (" + idfFile1.NoDataValue.ToString()
                    + ") should match NoData-value of " + Path.GetFileName(idfFile2.Filename) + " (" + idfFile2.NoDataValue.ToString() + ")");
            }
        }

        /// <summary>
        /// Check consistency between TOP- and BOT-files
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public bool CheckTOPBOTConsistency(IDFLog log, int logIndentLevel)
        {
            long errorCount = 0;

            for (int rowIdx = 0; rowIdx < TOPIDFFile.values.Length; rowIdx++)
            {
                for (int colIdx = 0; colIdx < BOTIDFFile.values[rowIdx].Length; colIdx++)
                {
                    if (errorCount < Properties.Settings.Default.MaxWarningMessageCount)
                    {
                        if (!CheckTOPBOTConsistency(rowIdx, colIdx, log, logIndentLevel, true))
                        {
                            errorCount++;
                        }
                    }
                    else if (!CheckTOPBOTConsistency(rowIdx, colIdx, log, logIndentLevel, false))
                    {
                        errorCount++;
                    }
                }
            }

            if ((log != null) && (errorCount > Properties.Settings.Default.MaxWarningMessageCount))
            {
                log.AddWarning("Totally, " + errorCount.ToString() + " level-consistency errors found in " + GetLayerName(LayerType, ModellayerNumber), logIndentLevel);
            }

            if ((log != null) && (log.HasUnsavedIDFFileErrors || log.HasUnsavedIDFFileWarnings))
            {
                try
                {
                    // log.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel);
                    log.WriteLogIDFFiles(false);
                    IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(log);
                    idfFileStatsWriter.WriteExcelFile();
                }
                catch (Exception)
                {
                    // ignore, all files will be saved as well when finished
                }
            }

            return (errorCount == 0);
        }

        public bool CheckTOPBOTConsistency(int rowIdx, int colIdx, IDFLog log, int logIndentLevel, bool isWarningMessageWritten)
        {
            float topValue = TOPIDFFile.values[rowIdx][colIdx];
            float botValue = BOTIDFFile.values[rowIdx][colIdx];
            if (!topValue.Equals(TOPIDFFile.NoDataValue) && !botValue.Equals(BOTIDFFile.NoDataValue))
            {
                if (!Layer.IsEqualHeight(topValue, botValue) && (topValue < botValue))
                {
                    float x = BOTIDFFile.GetX(colIdx);
                    float y = BOTIDFFile.GetY(rowIdx);

                    if (log != null)
                    {
                        if (isWarningMessageWritten)
                        {
                            string errorMessage = "Layer inconsistency (TOP<BOT) in " + GetLayerName(LayerType, ModellayerNumber) + " at " + GISUtils.GetXYString(x, y);
                            log.AddError(errorMessage, x, y, GetLayerName(LayerType, ModellayerNumber), Error1Code_layerInconsistency, topValue - botValue, logIndentLevel);
                        }
                        else
                        {
                            log.AddErrorValue(x, y, GetLayerName(LayerType, ModellayerNumber), Error1Code_layerInconsistency, topValue - botValue);
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        public string GetLayerName()
        {
            if (LayerName != null)
            {
                return LayerName;
            }
            else
            {
                switch (LayerType)
                {
                    case LayerType.Aquifer:
                        return "WVP" + ModellayerNumber;
                    case LayerType.Aquitard:
                        return "SDL" + ModellayerNumber;
                    default:
                        return "L" + ModellayerNumber;
                }
            }
        }

        public string GetLayerName(LayerType layerType, int modellayerNumber)
        {
            if (LayerName != null)
            {
                return LayerName;
            }
            else
            {
                switch (layerType)
                {
                    case LayerType.Aquifer:
                        return "WVP" + modellayerNumber;
                    case LayerType.Aquitard:
                        return "SDL" + modellayerNumber;
                    default:
                        return "L" + modellayerNumber;
                }
            }
        }

        public bool CheckTOPBOTKDCValueConsistency(IDFLog log, int logIndentLevel)
        {
            long errorCount = 0;
            long warningCount = 0;

            for (int rowIdx = 0; rowIdx < TOPIDFFile.values.Length; rowIdx++)
            {
                for (int colIdx = 0; colIdx < TOPIDFFile.values[rowIdx].Length; colIdx++)
                {
                    if (errorCount < Properties.Settings.Default.MaxWarningMessageCount)
                    {
                        if (!CheckTOPBOTKDCValueConsistency(rowIdx, colIdx, ref warningCount, log, logIndentLevel, true))
                        {
                            errorCount++;
                        }
                    }
                    else if (!CheckTOPBOTKDCValueConsistency(rowIdx, colIdx, ref warningCount, log, logIndentLevel, false))
                    {
                        errorCount++;
                    }
                }
            }

            if ((log != null) && (errorCount > Properties.Settings.Default.MaxWarningMessageCount))
            {
                log.AddWarning("Totally " + errorCount.ToString() + " value-consistency errors found in " + GetLayerName(LayerType, ModellayerNumber), logIndentLevel);
            }
            if ((log != null) && (warningCount > Properties.Settings.Default.MaxWarningMessageCount))
            {
                log.AddWarning("Totally " + warningCount.ToString() + " value-consistency warnings found in " + GetLayerName(LayerType, ModellayerNumber), logIndentLevel);
            }

            if ((log != null) && (log.HasUnsavedIDFFileErrors || log.HasUnsavedIDFFileWarnings))
            {
                try
                {
                    // log.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel);
                    log.WriteLogIDFFiles(false);
                    IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(log);
                    idfFileStatsWriter.WriteExcelFile();
                }
                catch (Exception)
                {
                    // ignore, all files will be saved as well when finished
                }
            }

            return (errorCount == 0);
        }

        public bool CheckTOPBOTKDCValueConsistency(int rowIdx, int colIdx, ref long warningCount, IDFLog log, int logIndentLevel, bool isWarningMessageWritten)
        {
            float topValue = TOPIDFFile.values[rowIdx][colIdx];
            float botValue = BOTIDFFile.values[rowIdx][colIdx];
            float kdcValue = KDCIDFFile.values[rowIdx][colIdx];
            if (!topValue.Equals(TOPIDFFile.NoDataValue) && !botValue.Equals(BOTIDFFile.NoDataValue))
            {
                if (!Layer.IsEqualHeight(topValue, botValue))
                {
                    // The layer has thickness, there should be an actual kDc-value
                    if (kdcValue.Equals(0) || kdcValue.Equals(KDCIDFFile.NoDataValue))
                    {
                        float x = BOTIDFFile.GetX(colIdx);
                        float y = BOTIDFFile.GetY(rowIdx);

                        HandleMissingKDCValue(rowIdx, colIdx, x, y, topValue, botValue, isWarningMessageWritten, log, logIndentLevel);
                        return false;
                    }
                    else
                    {
                        // If specified, check for the defined minimal kDc-value)
                        if (!IsDummyDefaultKDCSkipped &&
                            (((LayerType == LayerType.Aquifer) && (kdcValue.Equals(Properties.Settings.Default.DefaultKDValue)))
                            || ((LayerType == LayerType.Aquitard) && (kdcValue.Equals(Properties.Settings.Default.DefaultCValue)))))
                        {
                            float x = BOTIDFFile.GetX(colIdx);
                            float y = BOTIDFFile.GetY(rowIdx);

                            if ((log != null) && (warningCount < Properties.Settings.Default.MaxWarningMessageCount))
                            {
                                string warningMessage = "Possibly missing kD/c-value (TOP>BOT, kDc=DefaultValue) in " + GetLayerName(LayerType, ModellayerNumber) + " at " + GISUtils.GetXYString(x, y);
                                log.AddWarning(warningMessage, x, y, GetLayerName(LayerType, ModellayerNumber), Warning2Code_missingKDC, logIndentLevel);
                            }
                            else
                            {
                                log.AddWarningValue(x, y, GetLayerName(LayerType, ModellayerNumber), Warning2Code_missingKDC, topValue - botValue);
                            }
                            warningCount++;
                        }
                    }
                }
                else if (topValue.Equals(botValue))
                {
                    // The layer has no thickness, kDc-value should be zero or NoData (or the dummy, default, minimal value)
                    // Note: no thickness is checked with equality of TOP and BOT, ignore small differences (below Layer.LevelTolerance)
                    if (!kdcValue.Equals(0) && !kdcValue.Equals(KDCIDFFile.NoDataValue))
                    {
                        if (!IsDummyDefaultKDCSkipped ||
                            (((LayerType == LayerType.Aquifer) && !(kdcValue.Equals(Properties.Settings.Default.DefaultKDValue)))
                            || ((LayerType == LayerType.Aquitard) && !(kdcValue.Equals(Properties.Settings.Default.DefaultCValue)))))
                        {
                            float x = BOTIDFFile.GetX(colIdx);
                            float y = BOTIDFFile.GetY(rowIdx);

                            HandleUnexpectedKDCValue(rowIdx, colIdx, x, y, topValue, botValue, warningCount, log, logIndentLevel);
                            warningCount++;
                        }
                    }
                }
            }
            return true;
        }

        protected virtual void HandleUnexpectedKDCValue(int rowIdx, int colIdx, float x, float y, float topValue, float botValue, long warningCount, IDFLog log, int logIndentLevel)
        {
            if ((log != null) && (warningCount < Properties.Settings.Default.MaxWarningMessageCount))
            {
                string warningMessage = "Unexpected kD/c-value (TOP=BOT, kDc<>0/noData) in " + GetLayerName(LayerType, ModellayerNumber) + " at " + GISUtils.GetXYString(x, y);
                log.AddWarning(warningMessage, x, y, GetLayerName(LayerType, ModellayerNumber), Warning1Code_unexpectedKDC, logIndentLevel);
            }
            else
            {
                log.AddWarningValue(x, y, GetLayerName(LayerType, ModellayerNumber), Warning1Code_unexpectedKDC, topValue - botValue);
            }
        }

        protected virtual void HandleMissingKDCValue(int rowIdx, int colIdx, float x, float y, float topValue, float botValue, bool isWarningMessageWritten, IDFLog log, int logIndentLevel)
        {
            if (log != null)
            {
                if (isWarningMessageWritten)
                {
                    string errorMessage = "Missing kD/c-value (TOP>BOT, kDc=0/noData) in " + GetLayerName(LayerType, ModellayerNumber) + " at " + GISUtils.GetXYString(x, y);
                    log.AddError(errorMessage, x, y, GetLayerName(LayerType, ModellayerNumber), Error2Code_missingKDC, topValue - botValue, logIndentLevel);
                }
                else
                {
                    log.AddErrorValue(x, y, GetLayerName(LayerType, ModellayerNumber), Error2Code_missingKDC, topValue - botValue);
                }
            }
        }

        public bool CheckKDCValidity(IDFLog log, int logIndentLevel)
        {
            long errorCount = 0;

            for (int rowIdx = 0; rowIdx < KDCIDFFile.values.Length; rowIdx++)
            {
                for (int colIdx = 0; colIdx < KDCIDFFile.values[rowIdx].Length; colIdx++)
                {
                    if (errorCount < Properties.Settings.Default.MaxWarningMessageCount)
                    {
                        if (!CheckKDCValidity(rowIdx, colIdx, log, logIndentLevel, true))
                        {
                            errorCount++;
                        }
                    }
                    else if (!CheckKDCValidity(rowIdx, colIdx, log, logIndentLevel, false))
                    {
                        errorCount++;
                    }
                }
            }

            if ((log != null) && (errorCount > Properties.Settings.Default.MaxWarningMessageCount))
            {
                log.AddWarning("Totally " + errorCount.ToString() + " value-validity errors found in " + GetLayerName(LayerType, ModellayerNumber), logIndentLevel);
            }

            if ((log != null) && (log.HasUnsavedIDFFileErrors || log.HasUnsavedIDFFileWarnings))
            {
                try
                {
                    // log.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel);
                    log.WriteLogIDFFiles(false);
                    IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(log);
                    idfFileStatsWriter.WriteExcelFile();
                }
                catch (Exception)
                {
                    // ignore, all files will be saved as well when finished
                }
            }

            return (errorCount == 0);
        }

        public bool CheckKDCValidity(int rowIdx, int colIdx, IDFLog log, int logIndentLevel, bool isWarningMessageWritten)
        {
            float topValue = TOPIDFFile.values[rowIdx][colIdx];
            float botValue = BOTIDFFile.values[rowIdx][colIdx];
            if (!topValue.Equals(TOPIDFFile.NoDataValue) && !botValue.Equals(BOTIDFFile.NoDataValue))
            {
                botValue = BOTIDFFile.values[rowIdx][colIdx];
            }
            float someValue = KDCIDFFile.values[rowIdx][colIdx];
            if (float.IsInfinity(someValue) || float.IsNaN(someValue) || (!someValue.Equals(KDCIDFFile.NoDataValue) && (someValue < 0)))
            {
                float x = KDCIDFFile.GetX(colIdx);
                float y = KDCIDFFile.GetY(rowIdx);

                if (log != null)
                {
                    if (isWarningMessageWritten)
                    {
                        string errorMessage = "Invalid kD/c-value (Inf,NaN or negative) in " + GetLayerName(LayerType, ModellayerNumber) + " at " + GISUtils.GetXYString(x, y) + ": " + someValue;
                        log.AddError(errorMessage, x, y, GetLayerName(LayerType, ModellayerNumber), Error3Code_invalidKDC, topValue - botValue, logIndentLevel);
                    }
                    else
                    {
                        log.AddErrorValue(x, y, GetLayerName(LayerType, ModellayerNumber), Error3Code_invalidKDC, topValue - botValue);
                    }
                }
                return false;
            }
            return true;
        }

        public bool CheckLevelEquality(Layer lowerLayer, string levelId, IDFLog log, int logIndentLevel)
        {
            long errorCount = 0;

            for (int rowIdx = 0; rowIdx < this.BOTIDFFile.values.Length; rowIdx++)
            {
                for (int colIdx = 0; colIdx < this.BOTIDFFile.values[rowIdx].Length; colIdx++)
                {
                    if (errorCount < Properties.Settings.Default.MaxWarningMessageCount)
                    {
                        if (!CheckLevelEquality(rowIdx, colIdx, lowerLayer, levelId, log, logIndentLevel, true))
                        {
                            errorCount++;
                        }
                    }
                    else if (!CheckLevelEquality(rowIdx, colIdx, lowerLayer, levelId, log, logIndentLevel, false))
                    {
                        errorCount++;
                    }
                }
            }
            if ((log != null) && (errorCount > Properties.Settings.Default.MaxWarningMessageCount))
            {
                log.AddWarning("Totally " + errorCount.ToString() + " level-equality errors found in " + levelId, logIndentLevel);
            }

            if ((log != null) && (log.HasUnsavedIDFFileErrors || log.HasUnsavedIDFFileWarnings))
            {
                try
                {
                    // log.AddInfo("Writing log, IDF and Excel-file(s) ...", logIndentLevel);
                    log.WriteLogIDFFiles(false);
                    IDFLogStatsWriter idfFileStatsWriter = new IDFLogStatsWriter(log);
                    idfFileStatsWriter.WriteExcelFile();
                }
                catch (Exception)
                {
                    // ignore, all files will be saved as well when finished
                }
            }

            return (errorCount == 0);
        }

        public bool CheckLevelEquality(int rowIdx, int colIdx, Layer lowerLayer, string levelId, IDFLog log, int logIndentLevel, bool isWarningMessageWritten)
        {
            float value1 = this.BOTIDFFile.values[rowIdx][colIdx];
            float value2 = lowerLayer.TOPIDFFile.values[rowIdx][colIdx];
            if (!value1.Equals(this.TOPIDFFile.NoDataValue) && !value2.Equals(lowerLayer.TOPIDFFile.NoDataValue))
            {
                if (!Layer.IsEqualHeight(value1, value2))
                {
                    float x = this.BOTIDFFile.GetX(colIdx);
                    float y = this.BOTIDFFile.GetY(rowIdx);

                    if (log != null)
                    {
                        if (isWarningMessageWritten)
                        {
                            string errorMessage = "Unexpected level inequality (BOT_L(i-1)<>TOP_L(i)) in " + levelId + " at " + GISUtils.GetXYString(x, y);
                            log.AddError(errorMessage, x, y, levelId, Error4Code_unexpectedLevelInequality, value1 - value2, logIndentLevel);
                        }
                        else
                        {
                            log.AddErrorValue(x, y, levelId, Error4Code_unexpectedLevelInequality, value1 - value2);
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        public bool IsDummyLayer(int rowIdx, int colIdx)
        {
            return Layer.IsEqualHeight(TOPIDFFile.values[rowIdx][colIdx], BOTIDFFile.values[rowIdx][colIdx]);
        }

        public bool CheckLayer(IDFLog log, int logIndentLevel)
        {
            bool isValid = true;

            // Check for extent and cellsize consistency
            CheckDefinitions();

            // Check TOP-BOT and TOPBOT-kDc consistency
            bool hasTOPBOTIssues = !CheckTOPBOTConsistency(log, logIndentLevel);
            if (hasTOPBOTIssues && Properties.Settings.Default.IsInconsistentModelAborted)
            {
                throw new ToolException("Fatal layer-inconsistencies for regisLayer " + Path.GetFileNameWithoutExtension(TOPIDFFile.Filename) + ", layer cannot be added");
            }
            isValid = !hasTOPBOTIssues;

            if (KDCIDFFile != null)
            {
                bool hasTOPBOTKDCValueIssues = !CheckTOPBOTKDCValueConsistency(log, logIndentLevel);
                if (hasTOPBOTKDCValueIssues && Properties.Settings.Default.IsInconsistentModelAborted)
                {
                    throw new ToolException("Fatal value-inconsistencies for regisLayer " + Path.GetFileNameWithoutExtension(TOPIDFFile.Filename) + ", layer cannot be added");
                }

                bool hasKDCValidityIssues = !CheckKDCValidity(log, logIndentLevel);
                if (hasKDCValidityIssues && Properties.Settings.Default.IsInconsistentModelAborted)
                {
                    throw new ToolException("Fatal kD/c-values for regisLayer " + Path.GetFileNameWithoutExtension(TOPIDFFile.Filename) + ", layer cannot be added");
                }

                isValid = !(hasTOPBOTIssues || hasTOPBOTKDCValueIssues || hasKDCValidityIssues);
            }

            return isValid;
        }
    }
}
