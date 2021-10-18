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
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks.CheckResults
{
    /// <summary>
    /// Stores temporary validation results and allow export to iMOD IMF and/or table-format
    /// </summary>
    public class CheckResultHandler : ResultHandler
    {

        public CheckResultHandler(Model model, float resultNoDataValue, Extent checkExtent, string creator)
            : base(model, resultNoDataValue, checkExtent, creator)
        {
        }

        /// <summary>
        /// Adds the given checkresult for all locations in the selectionIDFFile that are unequal to NoData
        /// </summary>
        /// <param name="resultLayer"></param>
        /// <param name="selectionIDFFile"></param>
        /// <param name="checkResult"></param>
        public void AddIDFCheckResult(CheckResultLayer resultLayer, IDFFile selectionIDFFile, CheckResult checkResult)
        {
            EnsureAddedLayer(resultLayer);

            if (resultLayer != null)
            {
                IDFCellIterator idfCellIterator = new IDFCellIterator();
                idfCellIterator.AddIDFFile(((IDFFile)resultLayer.ResultFile));
                idfCellIterator.AddIDFFile(selectionIDFFile);
                idfCellIterator.Reset();
                while (idfCellIterator.IsInsideExtent())
                {
                    float selectionValue = idfCellIterator.GetCellValue(selectionIDFFile);
                    if (!selectionValue.Equals(selectionIDFFile.NoDataValue))
                    {
                        resultLayer.AddCheckResult(idfCellIterator.X, idfCellIterator.Y, checkResult);
                        AddSummaryResult(resultLayer.ResultType, idfCellIterator.X, idfCellIterator.Y, checkResult);
                    }
                    idfCellIterator.MoveNext();
                }
            }
        }

        public void AddCheckResult(CheckResultLayer resultLayer, float x, float y, CheckResult someCheckResult)
        {
            if (resultLayer != null)
            {
                EnsureAddedLayer(resultLayer);
                resultLayer.AddCheckResult(x, y, someCheckResult);
                AddSummaryResult(resultLayer.ResultType, x, y, someCheckResult);
            }
        }

        public void AddCheckDetail(CheckDetailLayer checkDetailLayer, float x, float y, CheckDetail checkDetail)
        {
            if (checkDetailLayer != null)
            {
                EnsureAddedLayer(checkDetailLayer);
                checkDetailLayer.AddCheckDetail(x, y, checkDetail);
            }
        }

        protected override void AddSummaryResult(string resultType, Result checkResult)
        {
            if (summaryLayerDictionary.ContainsKey(resultType))
            {
                ResultLayer summaryLayer = summaryLayerDictionary[resultType];
                summaryLayer.ResultCount++;
            }
            else
            {
                throw new Exception("No summarylayer found for resultType " + resultType);
            }
        }

        protected void AddSummaryResult(string resultType, float x, float y, CheckResult checkResult)
        {
            // Let base class handle general summary handling 
            AddSummaryResult(resultType, checkResult);

            // Handle IDF-part of adding here
            ResultLayer idfSummaryLayer = (ResultLayer)summaryLayerDictionary[resultType];
            IDFFile idfFile = (IDFFile)idfSummaryLayer.ResultFile;
            if ((idfFile != null) && idfFile.Extent.Contains(x, y))
            {
                idfFile.AddValue(x, y, 1);
            }
        }
    }
}
