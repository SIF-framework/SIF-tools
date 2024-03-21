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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Results;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// A specific type of resulthandler that can handle comparison results.
    /// </summary>
    public class ModelComparerResultHandler : ResultHandler
    {
        public Model ComparedModel { get; set; }
        public string ComparedModelFilename { get; set; }

        public ModelComparerResultHandler(Model model, Model comparedModel, float resultNoDataValue, Extent checkExtent, string creator) : base(model, resultNoDataValue, checkExtent, creator)
        {
            this.ComparedModel = comparedModel;
            this.ComparedModelFilename = comparedModel.RUNFilename;
        }

        public ModelComparerResultHandler(string baseModelFilename, string comparedModelFilename, DateTime? startDate, string outputPath, float resultNoDataValue, Extent checkExtent, string creator)
            : base(baseModelFilename, startDate, outputPath, resultNoDataValue, checkExtent, creator)
        {
            this.ComparedModel = null;
            this.ComparedModelFilename = comparedModelFilename;
        }

        protected override void AddSummaryResult(string resultType, Result someResult)
        {
            if (summaryLayerDictionary.ContainsKey(resultType))
            {
                ResultLayer summaryLayer = summaryLayerDictionary[resultType];
                summaryLayer.ResultCount++;
                ComparatorResult comparisonResult = (ComparatorResult)someResult;
                IMODFile differenceFile = comparisonResult.DifferenceFile;
                if (differenceFile != null)
                {
                    IDFFile summaryIDFFile = (IDFFile)(summaryLayer.ResultFile);

                    IDFFile diffIDFFile = IMODUtils.ConvertToIDF(differenceFile, extent, summaryIDFFile.XCellsize, noDataValue);
                    diffIDFFile = diffIDFFile.IsGreaterEqual(0.001f);
                    diffIDFFile.NoDataCalculationValue = 0;
                    diffIDFFile.ReplaceValues(0, diffIDFFile.NoDataValue);
                    // diffIDFFile.ReplaceValues(diffIDFFile, 1);
                    string summaryFilename = summaryIDFFile.Filename;
                    summaryLayer.ResultFile = summaryIDFFile + diffIDFFile;
                    summaryLayer.ResultFile.Filename = summaryFilename;
                }
            }
            else
            {
                throw new Exception("No summarylayer found for resultType " + resultType);
            }
        }

        public void SetComparisonResult(ComparatorResultLayer resultLayer, ComparatorResult comparisonResult)
        {
            if (resultLayer != null)
            {
                EnsureAddedLayer(resultLayer);
                resultLayer.SetComparisonResult(comparisonResult);
                AddSummaryResult(resultLayer.ResultType, comparisonResult);
            }
        }

        /// <summary>
        /// Creates a table with a summary of the results: a row per differencefile
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        protected override ResultTable CreateSummaryTable(Log log)
        {
            // Create summary table
            ResultTable summaryResultTable = base.CreateSummaryTable(log);
            summaryResultTable.TableTitle = "iMODValidator Comparison";
            summaryResultTable.MainTypeColumnName = "Package";
            summaryResultTable.SubTypeColumnName = "Part";
            summaryResultTable.MessageTypeColumnName = "Package (part)";

            return summaryResultTable;
        }

        public override LayerStatistics CreateLayerStatistics(ResultLayer resultLayer, string stressperiodString)
        {
            return new ComparatorLayerStatistics((resultLayer.ResultFile != null) ? resultLayer.ResultFile.Filename : null, RetrieveLayerStatisticsMainType(resultLayer), RetrieveLayerStatisticsSubType(resultLayer), RetrieveLayerStatisticsMessageType(resultLayer), resultLayer.ILay, stressperiodString);
        }

        protected override string RetrieveLayerStatisticsMainType(ResultLayer resultLayer)
        {
            return resultLayer.Id;
        }

        protected override string RetrieveLayerStatisticsSubType(ResultLayer resultLayer)
        {
            return resultLayer.Id2;
        }

        protected override string RetrieveLayerStatisticsMessageType(ResultLayer resultLayer)
        {
            return resultLayer.Id + " (" + resultLayer.Id2 + ")";
        }

        protected override string[] GetSelectedResulTypes()
        {
            return new string[] { ComparatorResultLayer.ResultTypeString };
        }

        public override ResultSheet CreateExcelResultSheet()
        {
            return new ComparatorResultSheet(ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus), baseModelFilename, ComparedModelFilename, extent);
        }
    }
}
