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
using Sweco.SIF.iMOD.IDF;
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
    class IPFSampler 
    {
        public static float NoDataValue { get; set; }

        protected const double ACCEPTED_ERROR = 0.0001;
        protected static CultureInfo dutchCultureInfo = new CultureInfo("nl-NL", false);
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public static void RetrieveValues(string inputFilename, string newFilename, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            string inputIPFFilename = inputFilename;
            string valueFilename = settings.ValueGrid;
            string outputIPFFilename = newFilename;
            string valueColumnName = (settings.ValueColumnname != null) ? settings.ValueColumnname : Path.GetFileNameWithoutExtension(valueFilename);
            string observationColString = settings.ObservationColString;
            string statPrefix = settings.StatPrefix;
            string csvStatsFilename = settings.CSVStatsFilename;

            try
            {
                if (!File.Exists(inputIPFFilename))
                {
                    throw new ToolException("IPF file doesn't exist: " + inputIPFFilename);
                }

                if (!File.Exists(valueFilename))
                {
                    throw new ToolException("Value filedoesn't exist: " + valueFilename);
                }

                if (File.Exists(outputIPFFilename) && !settings.IsOverwrite)
                {
                    throw new ToolException("Outputfile exists, use overwrite option. Sampling is aborted.");
                }

                if (!Directory.Exists(Path.GetDirectoryName(outputIPFFilename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputIPFFilename));
                }

                log.AddInfo("Reading IPF file " + Path.GetFileName(inputIPFFilename) + "... ", logIndentLevel);
                IPFFile inputIPFFile = IPFFile.ReadFile(inputIPFFilename, false, null);
                int observationColIdx = -1;
                if (observationColString != null)
                {
                    if (!int.TryParse(observationColString, out observationColIdx))
                    {
                        observationColIdx = inputIPFFile.FindColumnName(observationColString, true, false) + 1;
                        if (observationColIdx == 0)
                        {
                            throw new ToolException("Specified column name not found in input IPF-file: " + observationColString);
                        }
                    }
                    if (observationColIdx > inputIPFFile.ColumnCount)
                    {
                        throw new ToolException("Specified measurement column index is larger than number of columns in " + Path.GetFileName(inputIPFFilename));
                    }
                }

                IDFFile valueIDFFile = null;
                if (Path.GetExtension(valueFilename).ToLower().Equals(".idf"))
                {
                    log.AddInfo("Reading IDF file " + Path.GetFileName(valueFilename) + "...", logIndentLevel);
                    valueIDFFile = IDFFile.ReadFile(valueFilename);
                }
                else
                {
                    log.AddInfo("Reading ASC file " + Path.GetFileName(valueFilename) + "...", logIndentLevel);
                    // ASCFile ascFile = ASCFile.ReadAscFile(valueFilename, englishCultureInfo);
                    valueIDFFile = IDFFile.ReadFile(valueFilename);
                }

                double measuredValue = NoDataValue;
                double residual = NoDataValue;
                double absResidual = NoDataValue;
                double sumResiduals = 0;
                double sumAbsResiduals = 0;
                double avgResidual = 0;
                double avgAbsResidual = 0;

                string numberFormatString = "0";
                if (settings.DecimalCount > 0)
                {
                    numberFormatString += ".";
                    for (int i = 0; i < settings.DecimalCount; i++)
                    {
                        numberFormatString += "0";
                    }
                }

                if (statPrefix == null)
                {
                    statPrefix = string.Empty;
                }

                List<string> columnNameList = new List<string>(inputIPFFile.ColumnNames);
                columnNameList.Add(statPrefix + valueColumnName);
                if (settings.ObservationColString != null)
                {
                    columnNameList.AddRange(new string[] { statPrefix + "RES", statPrefix + "ABSRES" });
                }

                IPFFile outputIPFFile = new IPFFile();
                outputIPFFile.ColumnNames = columnNameList;

                int skippedPointCount = 0;
                int nanPointCount = 0;
                foreach (IPFPoint ipfPoint in inputIPFFile.Points)
                {
                    float x = (float) ipfPoint.X;
                    float y = (float) ipfPoint.Y;

                    // Check if points outside IDF-extent should be skipped
                    if (!settings.IsSkippingOutsideIDFExtent || valueIDFFile.Extent.Contains(x, y))
                    {
                        float value = settings.IsInterpolated ? valueIDFFile.GetInterpolatedValue(x, y) : valueIDFFile.GetValue(x, y);

                        // Check if points in NoData-cells should be skipped
                        if (!settings.IsSkippingNoDataCells || !value.Equals(valueIDFFile.NoDataValue))
                        {
                            if ((value.Equals(float.NaN) || Math.Abs(value - valueIDFFile.NoDataValue) <= ACCEPTED_ERROR))
                            {
                                // NoData-value or outside IDF-extent, set value and statistics to NoData
                                value = NoDataValue;
                                measuredValue = NoDataValue;
                                residual = NoDataValue;
                                absResidual = NoDataValue;
                            }
                            else
                            {
                                if (settings.ObservationColString != null)
                                {
                                    try
                                    {
                                        measuredValue = double.Parse(ipfPoint.ColumnValues[observationColIdx - 1].Replace(".", ","), dutchCultureInfo);
                                    }
                                    catch (Exception ex)
                                    {
                                        log.AddWarning("Invalid value in observation column (" + observationColIdx + "): " + ipfPoint.ColumnValues[observationColIdx - 1] + " for point (" + ipfPoint.X + "," + ipfPoint.Y + "): " + ex.GetBaseException().Message, logIndentLevel);
                                        measuredValue = float.NaN;
                                    }
                                    if (measuredValue.Equals(float.NaN))
                                    {
                                        nanPointCount++;
                                        continue;
                                    }
                                    if (Math.Abs(measuredValue - NoDataValue) <= ACCEPTED_ERROR)
                                    {
                                        measuredValue = NoDataValue;
                                        residual = NoDataValue;
                                        absResidual = NoDataValue;
                                    }
                                    else
                                    {
                                        residual = value - measuredValue;
                                        absResidual = Math.Abs(residual);
                                        sumResiduals += residual;
                                        sumAbsResiduals += absResidual;
                                    }
                                }
                            }

                            List<string> outputColumnValues = new List<string>(ipfPoint.ColumnValues);

                            outputColumnValues.Add(value.ToString(numberFormatString, englishCultureInfo));
                            if (settings.ObservationColString != null)
                            {
                                outputColumnValues.Add(residual.ToString(numberFormatString, englishCultureInfo));
                                outputColumnValues.Add(absResidual.ToString(numberFormatString, englishCultureInfo));
                            }
                            IPFPoint outputIPFPoint = new IPFPoint(outputIPFFile, ipfPoint, outputColumnValues);
                            outputIPFFile.AddPoint(outputIPFPoint);
                        }
                    }
                    else
                    {
                        skippedPointCount++;
                    }
                }

                if (skippedPointCount > 0)
                {
                    log.AddWarning(skippedPointCount.ToString() + " points were outside IDF-extent and are skipped in output", logIndentLevel);
                }
                if (nanPointCount > 0)
                {
                    log.AddWarning(nanPointCount.ToString() + " points had invalid measurement values and are skipped in output", logIndentLevel);
                }

                if (!settings.IsCSVFileWritten)
                {
                    log.AddInfo("Writing output IPF file " + Path.GetFileName(outputIPFFilename) + " ...", logIndentLevel);
                    outputIPFFile.WriteFile(outputIPFFilename);
                }
                else
                {
                    log.AddInfo("Writing output CSV file " + Path.GetFileName(outputIPFFilename) + " ...", logIndentLevel);
                    string outputCSVFilename = Path.Combine(Path.GetDirectoryName(outputIPFFilename), Path.GetFileNameWithoutExtension(outputIPFFilename) + ".csv");

                    //TODO check if decimale seperator ',' is needed to define
                    outputIPFFile.WriteCSVFile(outputCSVFilename, ',');
                    
                }

                if (settings.ObservationColString != null)
                {
                    IPFStatistics ipfStats = new IPFStatistics(outputIPFFile, logIndentLevel);
                    ipfStats.WriteMessages = true;
                    ipfStats.TabCount = 1;
                    List<float> pvalues = new List<float>();

                    string[] pvalueStrings = { "10", "50", "90" };
                    foreach (string pvalueString in pvalueStrings)
                    {
                        float pvalue = float.NaN;
                        if (!float.TryParse(pvalueString, out pvalue))
                        {
                            throw new ToolException("Invalid p-value specified" + pvalues.ToString());
                        }
                        pvalues.Add(pvalue);
                    }
                    ipfStats.PValues = pvalues;
                    ipfStats.CalculateStatistics(observationColIdx - 1, outputIPFFile.ColumnCount - 3, log);

                    if (csvStatsFilename == null)
                    {
                        csvStatsFilename = Path.Combine(Path.GetDirectoryName(outputIPFFilename), Path.GetFileNameWithoutExtension(outputIPFFilename) + "_stats.csv");
                    }

                    //TODO remove line, old line
                    //ipfStats.WriteCSV(Path.Combine(Path.GetDirectoryName(outputIPFFilename), Path.GetFileNameWithoutExtension(outputIPFFilename) + "_" + Properties.Settings.Default.StatFilePostfix + ".csv"), CultureInfo.CurrentCulture.TextInfo.ListSeparator, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, decimalCount);
                    ipfStats.WriteCSV(csvStatsFilename, englishCultureInfo.TextInfo.ListSeparator, englishCultureInfo.NumberFormat.NumberDecimalSeparator, settings.DecimalCount, settings);

                    avgResidual = sumResiduals / outputIPFFile.PointCount;
                    avgAbsResidual = sumAbsResiduals / outputIPFFile.PointCount;
                    log.AddInfo("Average residual = " + avgResidual.ToString(numberFormatString) + ", absolute average residual = " + avgAbsResidual.ToString("0.000") + "\r\n", logIndentLevel);
                }
            }
            catch (ToolException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error during sampling", ex);
            }
        }

        private IPFPoint FetchPoint(Dictionary<IPFPoint, List<string>> mergeIPFMap, IPFPoint basisIPFPoint)
        {
            foreach (IPFPoint mergePoint in mergeIPFMap.Keys)
            {
                if (basisIPFPoint.Equals(mergePoint))
                {
                    return mergePoint;
                }
            }

            return null;
        }

    }
}
