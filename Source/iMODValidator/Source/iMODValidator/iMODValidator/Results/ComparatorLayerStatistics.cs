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
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    public class ComparatorLayerStatistics : LayerStatistics
    {
        public ComparatorLayerStatistics(string layername, string mainType, string subType, string messageType, int ilay)
            : base(layername, mainType, subType, messageType, ilay)
        {
        }

        public ComparatorLayerStatistics(string layername, string mainType, string subType, string messageType, int ilay, string stressperiodString)
            : base(layername, mainType, subType, messageType, ilay, stressperiodString)
        {
        }

        public override string[] GetResultColumnHeaders(string totalCountPrefix, List<string> resultTypes)
        {
            string resultType = resultTypes[0];
            return new string[] {
            totalCountPrefix + "this modelresults",
            totalCountPrefix + "other modelresults",
            totalCountPrefix + resultType.ToLower(),
            totalCountPrefix + resultType.ToLower() + "loc's" };
        }

        public override long[] GetResultValues(List<string> resultTypes)
        {
            long[] resultValues = null;
            ResultLayerStatistics resultTypeStatistic = GetResultTypeStatistics(resultTypes[0]);
            if (resultTypeStatistic != null)
            {
                resultValues = new long[] {
                    resultTypeStatistic.Value1,
                    resultTypeStatistic.Value2,
                    resultTypeStatistic.ResultCount,
                    resultTypeStatistic.ResultLocationCount };
            }
            else
            {
                resultValues = new long[] { 0, 0, 0, 0 };
            }
            return resultValues;
        }
    }
}
