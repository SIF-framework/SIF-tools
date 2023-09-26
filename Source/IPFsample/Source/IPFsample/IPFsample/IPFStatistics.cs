// IPFsample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFsample.
// 
// IPFsample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFsample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFsample. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFsample
{
    class IPFStatistics
    {
        IPFFile ipfFile;
        int valueCount;
        int pointCount;
        float avgMeasured;
        float sdMeasured;
        float medMeasured;
        float avgModeled;
        float sdModeled;
        float medModeled;
        float diffAvgMeasuredModeled;    // difference between average of modeled and average of measured values
        float avgRes;
        float sdRes;
        float medRes;
        float avgAbsRes;
        float sdAbsRes;
        float medAbsRes;
        float RMSE;               // root mean squared error (standarddeviation)
        //float cd;               // coefficient of determination
        //float ef;               // modelling efficiency
        //float crm;              //coefficient of residual mass 
        //float r2;               // correlation coefficient
        //float gf;               // goodness of fit 
        float minMeasured;        // min of measuremed values
        float maxMeasured;        // min of measuremed values
        float minModeled;         // min of modeled values
        float maxModeled;         // min of modeled values
        float diffMaxMinMeasured;
        float diffMaxMinModeled;
        List<float> percentilesMeasured;
        List<float> percentilesModeled;
        List<float> percentilesRes;
        List<float> percentilesAbsRes;

        private bool writeMessages;
        public bool WriteMessages
        {
            set { writeMessages = value; }
            get { return writeMessages; }
        }
        private int tabCount;
        public int TabCount
        {
            set { tabCount = value; }
            get { return tabCount; }
        }
        private List<float> pvalues;
        public List<float> PValues
        {
            set { pvalues = value; }
            get { return pvalues; }
        }

        //TODO remove, defulat folders
        public const string AbsResPostfix = "AbsRes";
        public const string AvgAbsResColumnName = "AvgAbsRes";
        public const string AvgResColumnName = "AvgRes";
        //public const string DefaultInputIPFFolder = "C:\\Data\\Tools\\Tools\\iMOD Tools\\IPFSampler\\IPFFiles";
        //public const string DefaultInputValueFolder = "C:\\Data\\Tools\\Tools\\iMOD Tools\\IPFSampler\\ModellayerFiles";
        //public const string DefaultOutputFile = "IPFValues\\IPFRetrievedValues.IPF";
        public const string FilenameColumnName = "Filename";
        //public const string IPFNoDataValue = "-999.990";
        //public const string IsOverwrite = "AbsRes";
        public const string ResPostfix = "Res";
        public const string RMSEColumnName = "RMSE";
        public const string SdAbsResColumnName = "SdAbsRes";
        public const string SdResColumnName = "SdRes";
        //public const string StatFilePostfix = "stats";
        public const string ValueCountColumnName = "N";

        private static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public IPFStatistics(IPFFile ipfFile, int logIndentlevel)
        {
            this.ipfFile = ipfFile;
            writeMessages = false;
            tabCount = logIndentlevel;
            valueCount = 0;
            pointCount = 0;
            percentilesMeasured = new List<float>();
            percentilesModeled = new List<float>();
            percentilesRes = new List<float>();
            percentilesAbsRes = new List<float>();
        }

        public void CalculateStatistics(int measuredValueColIdx, int modeledValueColIdx, Log log)
        {
            float sumMeasuredValues = 0;
            float sumModeledValues = 0;
            float minMeasured = float.MaxValue;
            float maxMeasured = float.MinValue;
            float minModeled = float.MaxValue;
            float maxModeled = float.MinValue;
            float sumRes = 0;
            float sumAbsRes = 0;
            List<float> measuredValues = new List<float>();
            List<float> modeledValues = new List<float>();
            List<float> resValues = new List<float>();
            List<float> absResValues = new List<float>();
            foreach (IPFPoint ipfPoint in ipfFile.Points)
            {
                pointCount++;
                List<string> columnValues = ipfPoint.ColumnValues;
                float measuredValue = float.NaN;
                if (!float.TryParse(columnValues[measuredValueColIdx], NumberStyles.Float, englishCultureInfo, out measuredValue))
                {
                    log.AddWarning("Invalid measured value in column " + (measuredValueColIdx + 1) + ", point " + pointCount + " " + ipfPoint.ToString() + ": " + columnValues[measuredValueColIdx]);
                }
                float modeledValue = float.NaN;
                if (!float.TryParse(columnValues[modeledValueColIdx], NumberStyles.Float, englishCultureInfo, out modeledValue))
                {
                    log.AddWarning("Invalid measured value in column " + (modeledValueColIdx + 1) + ", point " + pointCount + " " + ipfPoint.ToString() + ": " + columnValues[modeledValueColIdx]);
                }
                if (measuredValue.Equals(float.NaN) || modeledValue.Equals(float.NaN) ||
                    measuredValue.Equals(IPFSampler.NoDataValue) || modeledValue.Equals(IPFSampler.NoDataValue))
                {
                    if (writeMessages)
                    {
                        log.AddInfo("Point " + pointCount + " (" + ipfPoint.XString + "," + ipfPoint.YString + ") skipped, NaN-value(s) found: " + ToString(columnValues), tabCount);
                    }
                }
                else
                {
                    float res = modeledValue - measuredValue;
                    float absRes = Math.Abs(res);

                    measuredValues.Add(measuredValue);
                    modeledValues.Add(modeledValue);
                    resValues.Add(res);
                    absResValues.Add(absRes);
                    valueCount++;

                    sumMeasuredValues += measuredValue;
                    sumModeledValues += modeledValue;
                    sumRes += res;
                    sumAbsRes += absRes;

                    if (measuredValue < minMeasured)
                    {
                        minMeasured = measuredValue;
                    }
                    if (measuredValue > maxMeasured)
                    {
                        maxMeasured = measuredValue;
                    }
                    if (modeledValue < minModeled)
                    {
                        minModeled = modeledValue;
                    }
                    if (modeledValue > maxModeled)
                    {
                        maxModeled = modeledValue;
                    }
                }
            }

            avgMeasured = sumMeasuredValues / valueCount;
            avgModeled = sumModeledValues / valueCount;
            avgRes = sumRes / valueCount;
            avgAbsRes = sumAbsRes / valueCount;

            measuredValues.Sort();
            modeledValues.Sort();
            resValues.Sort();
            absResValues.Sort();
            if ((measuredValues.Count > 0) && (modeledValues.Count > 0))
            {
                if ((valueCount % 2) == 0)
                {
                    medMeasured = ((measuredValues[(int)(valueCount / 2) - 1]) + (measuredValues[(int)(valueCount / 2)])) / 2;
                    medModeled = ((modeledValues[(int)(valueCount / 2) - 1]) + (modeledValues[(int)(valueCount / 2)])) / 2;
                    medRes = ((resValues[(int)(valueCount / 2) - 1]) + (resValues[(int)(valueCount / 2)])) / 2;
                    medAbsRes = ((absResValues[(int)(valueCount / 2) - 1]) + (absResValues[(int)(valueCount / 2)])) / 2;
                }
                else
                {
                    medMeasured = measuredValues[(int)(valueCount / 2)];
                    medModeled = modeledValues[(int)(valueCount / 2)];
                    medRes = resValues[(int)(valueCount / 2)];
                    medAbsRes = absResValues[(int)(valueCount / 2)];
                }
            }

            double sumMeasuredDiffSquares = 0;
            double sumModeledDiffSquares = 0;
            double sumResDiffSquares = 0;
            double sumAbsResDiffSquares = 0;
            double sumResSquares = 0;
            for (int i = 0; i < measuredValues.Count; i++)
            {
                float measuredValue = measuredValues[i];
                float modeledValue = modeledValues[i];
                float res = resValues[i];
                float absRes = absResValues[i];
                sumMeasuredDiffSquares += (measuredValue - avgMeasured) * (measuredValue - avgMeasured);
                sumModeledDiffSquares += (modeledValue - avgModeled) * (modeledValue - avgModeled);
                sumResDiffSquares += (res - avgRes) * (res - avgRes);
                sumAbsResDiffSquares += (absRes - avgAbsRes) * (absRes - avgAbsRes);
                sumResSquares += res * res;
            }
            sdMeasured = (float)Math.Sqrt(sumMeasuredDiffSquares / (valueCount - 1));
            sdModeled = (float)Math.Sqrt(sumModeledDiffSquares / (valueCount - 1));
            sdRes = (float)Math.Sqrt(sumResDiffSquares / (valueCount - 1));
            sdAbsRes = (float)Math.Sqrt(sumAbsResDiffSquares / (valueCount - 1));
            RMSE = (float)Math.Sqrt((sumResSquares / valueCount));

            percentilesMeasured.Clear();
            percentilesModeled.Clear();
            percentilesRes.Clear();
            percentilesAbsRes.Clear();
            if ((measuredValues.Count > 0) && (modeledValues.Count > 0))
            {
                if (pvalues != null)
                {
                    for (int i = 0; i < pvalues.Count; i++)
                    {
                        percentilesMeasured.Add(CalculatePercentile(measuredValues, pvalues[i]));
                        percentilesModeled.Add(CalculatePercentile(modeledValues, pvalues[i]));
                        percentilesRes.Add(CalculatePercentile(resValues, pvalues[i]));
                        percentilesAbsRes.Add(CalculatePercentile(absResValues, pvalues[i]));
                    }
                }
            }
        }

        private float CalculatePercentile(List<float> values, float percentage)
        {
            // Use Excel's algorithm: http://en.wikipedia.org/wiki/Percentile
            float rank = ((percentage / 100f) * (values.Count - 1)) + 1;
            int k = (int)rank;
            float f = rank % 1;
            if (k == 0)
            {
                return values[0];
            }
            else if (k >= values.Count)
            {
                return values[k - 1];
            }
            else
            {
                return values[k - 1] + (f * (values[k] - values[k - 1]));
            }
        }

        public void WriteCSV(string csvFilename, string listSeperator, string decimalSeperator, int decimalCount, SIFToolSettings settings)
        {
            StreamWriter sw = null;
            try
            {
                bool isNewCsvFile = (!File.Exists(csvFilename));
                sw = new StreamWriter(csvFilename, true);
                if (isNewCsvFile)
                {
                    // Add columnheaders
                    sw.Write(FilenameColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    sw.Write(AvgResColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    sw.Write(SdResColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    sw.Write(AvgAbsResColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    sw.Write(SdAbsResColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    sw.Write(RMSEColumnName + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    if (pvalues != null)
                    {
                        for (int i = 0; i < pvalues.Count; i++)
                        {
                            sw.Write("P" + pvalues[i].ToString() + "_" +  ResPostfix + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                            sw.Write("P" + pvalues[i].ToString() + "_" +  AbsResPostfix + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                        }
                    }
                    sw.WriteLine(ValueCountColumnName);
                }
                sw.Write(ipfFile.Filename + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                sw.Write(avgRes.ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                sw.Write(sdRes.ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                sw.Write(avgAbsRes.ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                sw.Write(sdAbsRes.ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                sw.Write(RMSE.ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                if (pvalues != null)
                {
                    for (int i = 0; i < pvalues.Count; i++)
                    {
                        if (i < percentilesRes.Count)
                        {
                            sw.Write(percentilesRes[i].ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                        }
                        if (i < percentilesRes.Count)
                        {
                            sw.Write(percentilesAbsRes[i].ToString("F" + decimalCount, CultureInfo.CurrentCulture) + CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                        }
                    }
                }
                sw.WriteLine(valueCount.ToString(CultureInfo.CurrentCulture));
            }
            catch (Exception ex)
            {
                throw new ToolException("Error while writing statisticsfile: " + csvFilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        private string GetTabs(int tabCount)
        {
            string tabString = string.Empty;
            for (int i = 0; i < tabCount; i++)
            {
                tabString += "\t";
            }
            return tabString;
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <returns>empty string foe empty or null list</returns>
        public static string ToString(List<string> list)
        {
            string listString = string.Empty;
            if (list != null)
            {
                for (int colIdx = 0; colIdx < list.Count; colIdx++)
                {
                    listString += list[colIdx];
                    if (colIdx < list.Count - 1)
                    {
                        listString += ",";
                    }
                }
            }
            return listString;
        }

    }
}
