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
    public class ResultLayerStatistics : IComparable<ResultLayerStatistics>
    {
        public string ResultType;
        public string ResultFilename;
        public long ResultCount;
        public long ResultLocationCount;
        public long Value1;
        public long Value2;
        public SortedDictionary<string, long> MessageCountDictionary = null; // (message, count)

        public ResultLayerStatistics(string resultType, string resultFilename, long resultCount, long resultLocationCount, long value1 = 0, long value2 = 0)
        {
            this.ResultType = resultType;
            this.ResultFilename = resultFilename;
            this.ResultCount = resultCount;
            this.ResultLocationCount = resultLocationCount;
            this.Value1 = value1;
            this.Value2 = value2;
            this.MessageCountDictionary = new SortedDictionary<string, long>();
        }

        public int CompareTo(ResultLayerStatistics other)
        {
            return ResultType.CompareTo(other.ResultType);
        }

        public void AddMessage(string message, int resultCount)
        {
            MessageCountDictionary.Add(message, resultCount);
        }
    }
}
