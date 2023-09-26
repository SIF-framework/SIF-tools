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

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// Used for storing ResultLayer statistics. A layer is defined here as an actual modellayer or 
    /// modelsystem for some stressperiod. So each stressperiod gives another layer. 
    /// In a LayerStatistics object all resultTypes are stored together in one object
    /// </summary>
    public class LayerStatistics : IEquatable<LayerStatistics>, IComparable<LayerStatistics>
    {
        protected string layerName;
        /// <summary>
        /// A unique layername for this layer statistic
        /// </summary>
        public string LayerName
        {
            get { return layerName; }
            set { layerName = value; }
        }

        protected string mainType;
        /// <summary>
        ///  The main type of this layerstatistic, e.g. the checkname
        /// </summary>
        public string MainType
        {
            get { return mainType; }
            set { mainType = value; }
        }

        protected string subType;
        /// <summary>
        /// A secondary type of this layerstatistic, e.g. packagename or resultype
        /// </summary>
        public string SubType
        {
            get { return subType; }
            set { subType = value; }
        }

        protected string messageType;
        /// <summary>
        /// The type used in the messagetables, e.g. one of the other type's or a combination
        /// </summary>
        public string MessageType
        {
            get { return messageType; }
            set { messageType = value; }
        }

        protected int ilay;
        /// <summary>
        /// The layer number
        /// </summary>
        public int Ilay
        {
            get { return ilay; }
            set { ilay = value; }
        }

        protected string stressperiodString;
        /// <summary>
        /// The stressperiod for this layer statistic
        /// </summary>
        public string StressperiodString
        {
            get { return stressperiodString; }
            set { stressperiodString = value; }
        }

        public SortedDictionary<string, ResultLayerStatistics> resultLayerStatisticsDictionary = null;
        /// <summary>
        /// Dictionary with statistics per ResultType for some ResultLayer-object
        /// </summary>
        public SortedDictionary<string, ResultLayerStatistics> ResultLayerStatisticsDictionary
        {
            get { return resultLayerStatisticsDictionary; }
            set { resultLayerStatisticsDictionary = value; }
        }

        /// <summary>
        /// Creates a LayerStatistics object
        /// </summary>
        /// <param name="layerName">a unique name of the layer, e.g. a filename</param>
        /// /// <param name="mainType">The main type of this layerstatistic, e.g. the checkname</param>
        /// <param name="subType">A secondary type of this layerstatistic, e.g. packagename or resultype</param>
        /// <param name="messageType">The type used in the messagetables, e.g. one of the other type's or a combination</param>
        /// <param name="ilay">The layer number</param>
        public LayerStatistics(string layerName, string mainType, string subType, string messageType, int ilay)
        {
            this.layerName = layerName;
            this.mainType = mainType;
            this.subType = subType;
            this.messageType = messageType;
            this.ilay = ilay;
            ResultLayerStatisticsDictionary = new SortedDictionary<string, ResultLayerStatistics>();
        }

        /// <summary>
        /// Creates a LayerStatistics object
        /// </summary>
        /// <param name="layerName">a unique name of the layer, e.g. a filename</param>
        /// /// <param name="mainType">The main type of this layerstatistic, e.g. the checkname</param>
        /// <param name="subType">A secondary type of this layerstatistic, e.g. packagename or resultype</param>
        /// <param name="messageType">The type used in the messagetables, e.g. one of the other type's or a combination</param>
        /// <param name="ilay">The layer number</param>
        /// <param name="stressperiodString">The stressperiod for this layer statistic</param>
        public LayerStatistics(string layerName, string mainType, string subType, string messageType, int ilay, string stressperiodString)
        {
            this.layerName = layerName;
            this.mainType = mainType;
            this.subType = subType;
            this.messageType = messageType;
            this.ilay = ilay;
            this.stressperiodString = stressperiodString;
            ResultLayerStatisticsDictionary = new SortedDictionary<string, ResultLayerStatistics>();
        }

        public ResultLayerStatistics GetResultTypeStatistics(string resultType)
        {
            if (ResultLayerStatisticsDictionary.ContainsKey(resultType))
            {
                return ResultLayerStatisticsDictionary[resultType];
            }
            else
            {
                return null;
            }
        }

        public bool Equals(LayerStatistics other)
        {
            return (this.layerName != null) && this.layerName.Equals(other.layerName) && this.mainType.Equals(other.mainType) && this.subType.Equals(other.subType) && (this.ilay == other.ilay) && (this.stressperiodString.Equals(other.stressperiodString));
        }

        //public string CreateFilename(string resultType)
        //{
        //    string filename = abbreviation + "_L" + ilay + "_" + resultType + "s";
        //    if ((stressperiodString != null) && (stressperiodString.Length > 0))
        //    {
        //        filename += "_" + stressperiodString;
        //    }
        //    return filename;
        //}

        /// <summary>
        /// Return string representation of LayerStatistics by Maintype, Subtype, Ilay and StressperiodString values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(" + mainType + "," + subType + "," + ilay + "," + stressperiodString + ")";
        }

        /// <summary>
        /// Compare values with other LayerStatistics by comparing strings values of internal properties
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(LayerStatistics other)
        {
            if (this.layerName != null)
            {
                if (this.layerName.Equals(other.layerName))
                {
                    if (this.mainType.Equals(other.mainType))
                    {
                        if (this.subType.Equals(other.subType))
                        {
                            if (this.stressperiodString.Equals(other.stressperiodString))
                            {
                                return this.ilay.CompareTo(other.ilay);
                            }
                            else
                            {
                                return this.stressperiodString.CompareTo(other.stressperiodString);
                            }
                        }
                        else
                        {
                            return this.subType.CompareTo(other.subType);
                        }
                    }
                    else
                    {
                        return this.mainType.CompareTo(other.mainType);
                    }
                }
                else
                {
                    return this.layerName.CompareTo(other.layerName);
                }
            }
            else
            {
                return -1;
            }
        }

        public bool Contains(string resultType)
        {
            return ResultLayerStatisticsDictionary.ContainsKey(resultType);
        }

        public void Add(string resultType, ResultLayerStatistics resultLayerStatistics)
        {
            ResultLayerStatisticsDictionary.Add(resultType, resultLayerStatistics);
        }

        public virtual string[] GetResultColumnHeaders(string totalCountPrefix, List<string> resultTypes)
        {
            string[] columnHeaders = new string[resultTypes.Count * 2];
            for (int resultTypeIdx = 0; resultTypeIdx < resultTypes.Count; resultTypeIdx++)
            {
                string resultType = resultTypes[resultTypeIdx];
                columnHeaders[resultTypeIdx * 2] = totalCountPrefix + resultType.ToLower();
                columnHeaders[resultTypeIdx * 2 + 1] = totalCountPrefix + resultType.ToLower() + "loc's";
            }
            return columnHeaders;
        }

        public virtual long[] GetResultValues(List<string> resultTypes)
        {
            long[] resultValues = new long[resultTypes.Count * 2];
            for (int resultTypeIdx = 0; resultTypeIdx < resultTypes.Count; resultTypeIdx++)
            {
                string resultType = resultTypes[resultTypeIdx];
                ResultLayerStatistics resultTypeStatistic = GetResultTypeStatistics(resultType);
                resultValues[resultTypeIdx * 2] = (resultTypeStatistic != null) ? resultTypeStatistic.ResultCount : 0;
                resultValues[resultTypeIdx * 2 + 1] = (resultTypeStatistic != null) ? resultTypeStatistic.ResultLocationCount : 0;
            }
            return resultValues;
        }
    }
}
