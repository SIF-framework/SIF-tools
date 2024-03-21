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
using Sweco.SIF.GIS;
using Sweco.SIF.iMODValidator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// A table with results/statistics per modellayer. In the table a layer is defined as an 
    /// actual modellayer or modelsystem for some stress period. So each stress period gives another layer. 
    /// In the table statistics of all resultTypes are combined for a modellayer and presented in one row
    /// </summary>
    public class ResultTable
    {
        protected string baseModelFilename;
        public string BaseModelFilename
        {
            get { return baseModelFilename; }
            set { baseModelFilename = value; }
        }
        protected string baseRunfilename;
        public string BaseRunfilename
        {
            get { return baseRunfilename; }
            set { baseRunfilename = value; }
        }

        protected Extent extent;
        public Extent Extent
        {
            get { return extent; }
            set { extent = value; }
        }

        protected string mainTypeColumnName;
        public string MainTypeColumnName
        {
            get { return mainTypeColumnName; }
            set { mainTypeColumnName = value; }
        }

        protected string subTypeColumnName;
        public string SubTypeColumnName
        {
            get { return subTypeColumnName; }
            set { subTypeColumnName = value; }
        }

        protected string messageTypeColumnName;
        public string MessageTypeColumnName
        {
            get { return messageTypeColumnName; }
            set { messageTypeColumnName = value; }
        }

        protected List<LayerStatistics> layerStatisticsList;
        public List<LayerStatistics> LayerStatisticsList
        {
            get { return layerStatisticsList; }
            set { layerStatisticsList = value; }
        }

        protected string tableTitle;
        public string TableTitle
        {
            get { return tableTitle; }
            set { tableTitle = value; }
        }

        protected string tableCreator;
        public string TableCreator
        {
            get { return tableCreator; }
            set { tableCreator = value; }
        }

        protected List<LogMessage> logWarnings;
        public List<LogMessage> LogWarnings
        {
            set { logWarnings = value; }
            get { return logWarnings; }
        }

        protected List<LogMessage> logErrors;
        public List<LogMessage> LogErrors
        {
            set { logErrors = value; }
            get { return logErrors; }
        }

        protected ResultTable()
        {
            this.layerStatisticsList = new List<LayerStatistics>();
        }

        public ResultTable(string baseModelFilename, Extent extent)
        {
            this.baseModelFilename = baseModelFilename;
            this.extent = extent;
            this.tableTitle = "iMODValidator result table";
            this.layerStatisticsList = new List<LayerStatistics>();

            // Set default columnnames
            this.mainTypeColumnName = "Checkname";
            this.subTypeColumnName = "Package";
            this.messageTypeColumnName = "Package";
        }

        public LayerStatistics GetLayerStatistics(LayerStatistics layerStatistics)
        {
            if (ContainsLayerStatistic(layerStatistics))
            {
                foreach (LayerStatistics tableLayerStatistics in layerStatisticsList)
                {
                    if (tableLayerStatistics.Equals(layerStatistics))
                    {
                        return tableLayerStatistics;
                    }
                }
            }
            return null;
        }

        public LayerStatistics GetLayerStatistics(string packageName, int layerNumber, string stressPeriodString)
        {
            foreach (LayerStatistics layerStatistics in layerStatisticsList)
            {
                if (stressPeriodString == null)
                {
                    if (layerStatistics.SubType.Equals(packageName) && layerStatistics.Ilay.Equals(layerNumber))
                    {
                        return layerStatistics;
                    }
                }
                else
                {
                    if (layerStatistics.SubType.Equals(packageName) && layerStatistics.Ilay.Equals(layerNumber) && layerStatistics.StressperiodString.Equals(stressPeriodString))
                    {
                        return layerStatistics;
                    }
                }
            }
            return null;
        }

        public void AddRow(LayerStatistics layerStat)
        {
            if (ContainsLayerStatistic(layerStat))
            {
                // Replace existing rows
                layerStatisticsList.Remove(GetLayerStatistics(layerStat));
            }

            layerStatisticsList.Add(layerStat);
        }

        public virtual void Export(string exportFilename, Log log, bool isResultShown = true)
        {
            StreamWriter sw = null;
            try
            {
                exportFilename = Path.Combine(Path.GetPathRoot(exportFilename), Path.GetFileNameWithoutExtension(exportFilename) + ".txt");
                if (isResultShown)
                {
                    log.AddInfo(string.Empty);
                    log.AddInfo(ToString());
                    log.AddInfo(string.Empty);
                }

                exportFilename = Path.GetFileNameWithoutExtension(exportFilename + ".txt");
                sw = new StreamWriter(exportFilename);
                sw.WriteLine(ToString());
                log.AddInfo("Results are written to: " + exportFilename);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing table " + exportFilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
        }

        public virtual void Export(Log log)
        {
            log.AddInfo(string.Empty);
            log.AddInfo(ToString());
            log.AddInfo(string.Empty);
        }

        public override string ToString()
        {
            string resultString = "Results for model " + baseModelFilename + "\r\n";

            resultString += "\r\nSummary per check\r\n";
            resultString += "CheckName".PadRight(25) + "\tLayerAbbr\tStressperiod\tLayernr";

            // Get the unique resultTypes over all rows
            List<string> resultTypes = GetResultTypeList();

            // Finish header with resultTypes
            foreach (string resultType in resultTypes)
            {
                resultString += "\tTotal" + resultType.ToLower() + "s\t" + "Total" + resultType.ToLower() + "loc's";
            }
            resultString += "\r\n";

            // Now create rows for all statistics
            for (int i = 0; i < layerStatisticsList.Count; i++)
            {
                resultString += layerStatisticsList[i].MainType.PadRight(25) + "\t" +
                                layerStatisticsList[i].SubType + "\t" +
                                layerStatisticsList[i].StressperiodString + "\t" +
                                layerStatisticsList[i].Ilay;
                foreach (string resultType in resultTypes)
                {
                    ResultLayerStatistics resultTypeStatistic = layerStatisticsList[i].GetResultTypeStatistics(resultType);
                    if (resultTypeStatistic != null)
                    {
                        resultString += "\t\t" + resultTypeStatistic.ResultCount + "\t\t" + resultTypeStatistic.ResultLocationCount;
                    }
                    else
                    {
                        resultString += "\t\t0\t\t0";
                    }
                }
                resultString += "\r\n";
            }

            // Create tables for messages
            foreach (string resultType in resultTypes)
            {
                if (GetTotalMessageCount(resultType) > 0)
                {
                    resultString += "\r\nSummary per " + resultType.ToLower() + "message\r\n";
                    resultString += "Package\tLayernr\t" + "Errorfile".PadRight(50) + "\t" + "Errormessage".PadRight(50) + "\tTotal errors\r\n";

                    // Set the actual values of the errormessage table
                    for (int i = 0; i < layerStatisticsList.Count; i++)
                    {
                        if (layerStatisticsList[i].ResultLayerStatisticsDictionary.ContainsKey(resultType))
                        {
                            ResultLayerStatistics resultTypeStatistics = layerStatisticsList[i].ResultLayerStatisticsDictionary[resultType];
                            foreach (string message in resultTypeStatistics.MessageCountDictionary.Keys)
                            {
                                string resultFilename = resultTypeStatistics.ResultFilename;
                                long count = resultTypeStatistics.MessageCountDictionary[message];
                                resultString += layerStatisticsList[i].SubType + "\t" +
                                                layerStatisticsList[i].Ilay + "\t" +
                                                LeftString(Path.GetFileName(resultFilename), 45, true) + "\t" +
                                                LeftString(message, 50, true) + "\t" +
                                                count.ToString() + "\r\n";
                            }
                        }
                    }
                }
            }

            return resultString;
        }

        protected long GetTotalMessageCount(string resultType = null)
        {
            long totalMsgCount = 0;

            for (int i = 0; i < layerStatisticsList.Count; i++)
            {
                LayerStatistics layerStats = layerStatisticsList[i];
                if (resultType != null)
                {
                    // Retrieve totalcounts for specified resultType
                    if (layerStats.ResultLayerStatisticsDictionary.ContainsKey(resultType))
                    {
                        SortedDictionary<string, long> msgCountDictionary = layerStats.ResultLayerStatisticsDictionary[resultType].MessageCountDictionary;
                        foreach (long msgCount in msgCountDictionary.Values)
                        {
                            totalMsgCount += msgCount;
                        }
                    }
                }
                else
                {
                    // Retrieve totalcounts for all resultTypes
                    foreach (string resultType2 in layerStats.ResultLayerStatisticsDictionary.Keys)
                    {
                        SortedDictionary<string, long> msgCountDictionary = layerStats.ResultLayerStatisticsDictionary[resultType2].MessageCountDictionary;
                        foreach (long msgCount in msgCountDictionary.Values)
                        {
                            totalMsgCount += msgCount;
                        }
                    }
                }
            }

            return totalMsgCount;
        }

        protected List<string> GetResultTypeList()
        {
            List<string> resultTypes = new List<string>();
            for (int i = 0; i < layerStatisticsList.Count; i++)
            {
                foreach (string resultType in layerStatisticsList[i].ResultLayerStatisticsDictionary.Keys)
                {
                    if (!resultTypes.Contains(resultType))
                    {
                        resultTypes.Add(resultType);
                    }
                }
            }
            resultTypes.Sort();
            return resultTypes;
        }

        private string LeftString(string sourceString, int maxLength, bool isPaddedToMaxLength = false)
        {
            if (sourceString.Length >= maxLength)
            {
                return sourceString.Substring(maxLength);
            }
            else
            {
                if (isPaddedToMaxLength)
                {
                    return sourceString.PadRight(maxLength);
                }
                else
                {
                    return sourceString;
                }
            }
        }

        public bool ContainsLayerStatistic(LayerStatistics someRowLayerStats)
        {
            return layerStatisticsList.Contains(someRowLayerStats);
        }

        public DateTime RetrieveMinStressPeriod()
        {
            DateTime minStressPeriod = DateTime.MaxValue;
            foreach (LayerStatistics layerStatistics in this.layerStatisticsList)
            {
                DateTime stressPeriod = StressPeriod.ParseSNAME(layerStatistics.StressperiodString);
                if (stressPeriod < minStressPeriod)
                {
                    minStressPeriod = stressPeriod;
                }
            }
            return minStressPeriod;
        }

        public List<string> RetrieveResultFilenames(int maxKPER, int minEntryNumber, int maxEntryNumber, DateTime minStressPeriod, bool isConvertedToLowerCase = false)
        {
            List<string> filenames = new List<string>();
            foreach (LayerStatistics layerStatistics in this.layerStatisticsList)
            {
                int kper = StressPeriod.ParseSNAME(layerStatistics.StressperiodString).Subtract(minStressPeriod).Days + 1;
                int ilay = layerStatistics.Ilay;
                foreach (ResultLayerStatistics resultLayerStatistics in layerStatistics.ResultLayerStatisticsDictionary.Values)
                {
                    if ((kper <= maxKPER) && (ilay >= minEntryNumber) && (ilay <= maxEntryNumber))
                    {
                        if (resultLayerStatistics.ResultFilename != null)
                        {
                            filenames.Add(Path.Combine(layerStatistics.MainType, resultLayerStatistics.ResultFilename).ToLower());
                        }
                    }
                }
            }
            return filenames;
        }

        public LayerStatistics GetLayerStatistics(string resultFilename)
        {
            foreach (LayerStatistics layerStatistics in this.layerStatisticsList)
            {
                foreach (ResultLayerStatistics resultLayerStatistics in layerStatistics.ResultLayerStatisticsDictionary.Values)
                {
                    if (resultLayerStatistics.ResultFilename != null)
                    {
                        if (resultLayerStatistics.ResultFilename.ToLower().Equals(resultFilename.ToLower()))
                        {
                            return layerStatistics;
                        }
                    }
                }
            }
            return null;
        }

        public ResultLayerStatistics GetResultLayerStatistics(string resultFilename)
        {
            foreach (LayerStatistics layerStatistics in this.layerStatisticsList)
            {
                foreach (ResultLayerStatistics resultLayerStatistics in layerStatistics.ResultLayerStatisticsDictionary.Values)
                {
                    if ((resultLayerStatistics.ResultFilename != null) && resultLayerStatistics.ResultFilename.ToLower().Equals(resultFilename.ToLower()))
                    {
                        return resultLayerStatistics;
                    }
                }
            }
            return null;
        }
    }
}
