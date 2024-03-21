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
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks.CheckResults
{
    /// <summary>
    /// The abstract class CheckResultLayer stores specific results for iMODValidator-checks, .i.e. the check and package that were used.
    /// </summary>
    public abstract class CheckResultLayer : ResultLayer
    {
        /// <summary>
        /// When the result is stored in an IPFFile, the result values are always stored in column 3, messages in the columns after that
        /// </summary>
        protected const int IPFValueColIdx = 2;

        protected Check check;
        protected Package package;

        protected CheckResultLayer() : base()
        {
        }

        /// <summary>
        /// Constuctor for a CheckResultLayer with an underlying IDF ResultFile object
        /// </summary>
        /// <param name="check"></param>
        /// <param name="package"></param>
        /// <param name="subString">extra substring to add after package name to (file)name of ResultLayer, use null to ignore</param>
        /// <param name="stressPeriod"></param>
        /// <param name="ilay"></param>
        /// <param name="extent"></param>
        /// <param name="cellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        /// <param name="useSparseGrid"></param>
        public CheckResultLayer(Check check, Package package, string subString, StressPeriod stressPeriod, int ilay, Extent extent, float cellsize, float noDataValue, string outputPath, Legend legend, bool useSparseGrid = false)
            : base(check.Name, package?.Key, subString, stressPeriod, ilay, extent, cellsize, noDataValue, outputPath, legend, useSparseGrid)
        {
            this.check = check;
            this.package = package;
            this.StressPeriod = stressPeriod;
            description = CreateLayerDescription((package != null) ? package.Key : "Missing package", ilay);
            processDescription = this.Description;

            // Recreate iMOD-filename for this checkresultlayer
            if (this.ResultFile != null)
            {
                this.ResultFile.Filename = CreateResultFilename();
            }

            CheckManager.Instance.CheckForAbort();
        }

        /// <summary>
        /// Constuctor for a CheckResultLayer with an underlying IPF ResultFile object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id2"></param>
        /// <param name="subString"></param>
        /// <param name="stressPeriod"></param>
        /// <param name="ilay"></param>
        /// <param name="columnNames"></param>
        /// <param name="textFileColumnIndex"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        public CheckResultLayer(string id, string id2, string subString, StressPeriod stressPeriod, int ilay, List<string> columnNames, int textFileColumnIndex, string outputPath, Legend legend = null)
            : base(id, id2, subString, stressPeriod, ilay, outputPath)
        {
            InitializeIPF(columnNames, textFileColumnIndex, legend);
        }

        /// <summary>
        /// Constuctor for a CheckResultLayer with an underlying IPF ResultFile object
        /// </summary>
        /// <param name="check"></param>
        /// <param name="package"></param>
        /// <param name="stressPeriod"></param>
        /// <param name="ilay"></param>
        /// <param name="columnNames"></param>
        /// <param name="textFileColumnIndex"></param>
        /// <param name="valueColumnIndex"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        public CheckResultLayer(Check check, Package package, string subString, StressPeriod stressPeriod, int ilay, string outputPath, Legend legend = null)
            : this(check.Name, package?.Key, subString, stressPeriod, ilay, new List<string>() { "X", "Y", "Value", "Message" }, -1, outputPath, legend)
        {
            this.check = check;
            this.package = package;
            this.StressPeriod = stressPeriod;
            description = CreateLayerDescription((package != null) ? package.Key : "Missing package", ilay);
            processDescription = this.Description;

            // Recreate iMOD-filename for this checkresultlayer
            if (this.ResultFile != null)
            {
                this.ResultFile.Filename = CreateResultFilename();
            }

            CheckManager.Instance.CheckForAbort();
        }

        private void InitializeIPF(List<string> columnNames, int textFileColumnIndex, Legend legend)
        {
            IPFFile ipfFile = new IPFFile();
            this.resultFile = ipfFile;
            ipfFile.ColumnNames = columnNames;
            ipfFile.AssociatedFileColIdx = textFileColumnIndex;
            this.Legend = legend?.Copy();
            ipfFile.Filename = CreateResultFilename();
            ipfFile.UseLazyLoading = iMODValidatorSettingsManager.Settings.UseLazyLoading;
            ipfFile.NoDataValue = float.NaN;
            this.resolution = "-";
            if ((legend != null) && (legend is IPFLegend))
            {
                ((IPFLegend)this.Legend).SelectedLabelColumns = new List<int>();
                ((IPFLegend)this.Legend).SelectedLabelColumns.Add(IPFValueColIdx + 2);
            }
        }

        public void AddCheckResult(float x, float y, CheckResult someCheckResult)
        {
            AddResult(x, y, someCheckResult);
        }

        protected virtual void AddResult(float x, float y, Result result)
        {
            float resultValue = result.ResultValue;
            float currentValue = GetResultValue(x, y);
            // Check if for this Resultlayer it is allowed to add more than one result for an xy-location
            if (isResultReAddingAllowed || (((int)currentValue & (int)resultValue) == 0) || (currentValue.Equals(resultFile.NoDataValue)))
            {
                // adding the same result more than once is allowed or a new result is added for this location

                // Store statistics that are not related to the specified xy-location
                AddResult(result);

                // Store result at xy-location in the iMODfile of this resultlayer
                if (resultFile is IDFFile)
                {
                    ((IDFFile)resultFile).AddValue(x, y, resultValue);
                }
                else if (resultFile is IPFFile)
                {
                    IPFFile ipfResultFile = ((IPFFile)resultFile);
                    AddIPFValue(ipfResultFile, x, y, result);
                }
                else
                {
                    throw new Exception("Unsupported iMOD-file '" + resultFile.GetType().ToString() + "' for ResultLayer");
                }

            }
            else
            {
                // result already present and readding same result not allowed, ignore
            }
        }

        private float GetResultValue(float x, float y)
        {
            if (resultFile is IDFFile)
            {
                return ((IDFFile)resultFile).GetValue(x, y);
            }
            else if (resultFile is IPFFile)
            {

                List<IPFPoint> ipfPoints = ((IPFFile)resultFile).GetPoints(x, y, IPFXYMargin);
                if ((ipfPoints == null) || (ipfPoints.Count == 0))
                {
                    return float.NaN;
                }

                if (ipfPoints.Count > 1)
                {
                    throw new Exception("More than 1 point in IPF ResultLayer at xy-coordinates (" + x.ToString("F3", SIFTool.EnglishCultureInfo) + "," + y.ToString("F3", SIFTool.EnglishCultureInfo) + ") +/-" + IPFXYMargin);
                }
                if (ipfPoints.Count == 1)
                {
                    // Retrieve current value
                    IPFPoint ipfPoint = ipfPoints[0];
                    return float.Parse(ipfPoint.ColumnValues[IPFValueColIdx]);
                }
                return float.NaN;
            }
            else
            {
                throw new Exception("Unsupported iMOD-file '" + resultFile.GetType().ToString() + "' for ResultLayer");
            }
        }

        /// <summary>
        /// Adds a result to the specified IPFFile.  
        /// </summary>
        /// <param name="resultFile"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="result"></param>
        protected void AddIPFValue(IPFFile resultFile, float x, float y, Result result)
        {
            // Retrieve point at given x,y-coordinate
            List<IPFPoint> ipfPoints = resultFile.GetPoints(x, y, IPFXYMargin);
            IPFPoint ipfPoint = null;
            if ((ipfPoints == null) || (ipfPoints.Count == 0))
            {
                List<string> values = new List<string>();
                values.Add(x.ToString());
                values.Add(y.ToString());
                for (int i = 2; i < IPFValueColIdx; i++)
                {
                    values.Add(IPFFile.EmptyValue);
                }
                // Add a default zero result value
                values.Add(0.ToString());
                // Add empty message strings
                for (int i = IPFValueColIdx + 1; i < resultFile.ColumnCount; i++)
                {
                    values.Add(IPFFile.EmptyValue);
                }

                ipfPoint = new IPFPoint(resultFile, new FloatPoint(x, y), values);
                resultFile.AddPoint(ipfPoint);
            }
            else if (ipfPoints.Count > 1)
            {
                throw new Exception("More than 1 point in IPF ResultLayer at xy-coordinates (" + x.ToString("F3", SIFTool.EnglishCultureInfo) + "," + y.ToString("F3", SIFTool.EnglishCultureInfo) + ") +/-" + IPFXYMargin);
            }
            else
            {
                ipfPoint = ipfPoints[0];
            }

            // Retrieve current value and add specified resultValue
            float currentValue = float.Parse(ipfPoint.ColumnValues[IPFValueColIdx]);
            // assume current value is a number and never NoData or NaN
            ipfPoint.ColumnValues[IPFValueColIdx] = (currentValue + result.ResultValue).ToString();

            // Add resultmessage to IPFpoint
            int colIdx = IPFValueColIdx + 1;
            while (colIdx < ipfPoint.ColumnValues.Count)
            {
                string ipfString = ipfPoint.ColumnValues[colIdx];
                if ((ipfString == null) || ipfString.Equals(string.Empty) || ipfString.Equals(IPFFile.EmptyValue))
                {
                    // empty column found, message is not yet present
                    ipfPoint.ColumnValues[colIdx] = result.ShortDescription;
                    return;
                }
                else if (ipfString.Equals(result.ShortDescription))
                {
                    // message is already present
                    return;
                }
                colIdx++;
            }
            // message is not yet present in any one of the available columns, add a new column for this message
            resultFile.AddColumn("Message" + (colIdx - IPFValueColIdx));
            ipfPoint.ColumnValues[colIdx] = result.ShortDescription;
            ((IPFLegend)this.Legend).SelectedLabelColumns.Add(resultFile.ColumnCount);
        }

        /// <summary>
        /// Creates a string with the checkedparametername, resulttype and ilay-number
        /// </summary>
        /// <param name="checkedParameterName"></param>
        /// <param name="ilay"></param>
        /// <returns></returns>
        public string CreateLayerDescription(string checkedParameterName, int ilay)
        {
            return checkedParameterName + " " + ResultType.ToLower() + "-file for layer " + ilay;
        }

        /// <summary>
        /// Remove unused legend classes, for which no values are present in the iMOD result file
        /// </summary>
        /// <param name="classLabelSubString">if specified, only classes with the specified substring in its label are checked for remova;</param>
        public override void CompressLegend(string classLabelSubString = null)
        {
            if (resultFile is IPFFile)
            {
                ((IPFLegend)resultFile.Legend).CompressLegend((IPFFile)resultFile, IPFValueColIdx, classLabelSubString);
            }
            else
            {
                base.CompressLegend(classLabelSubString);
            }
        }
    }
}
