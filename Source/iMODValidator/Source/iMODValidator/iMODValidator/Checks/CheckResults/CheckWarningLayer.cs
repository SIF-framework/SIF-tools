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
using Sweco.SIF.iMODPlus.IDF;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks.CheckResults
{
    public class CheckWarningLayer : CheckResultLayer
    {
        protected CheckWarningLayer()
        {
        }

        /// <summary>
        /// Constuctor for a CheckWarningLayer with an underlying IDF ResultFile object
        /// </summary>
        /// <param name="check"></param>
        /// <param name="package"></param>
        /// <param name="subString"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="startDate"></param>
        /// <param name="extent"></param>
        /// <param name="cellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        /// <param name="useSparseGrid"></param>
        public CheckWarningLayer(Check check, Package package, string subString, int kper, int ilay, DateTime? startDate, Extent extent, float cellsize, float noDataValue, string outputPath, Legend legend, bool useSparseGrid = false)
            : base(check, package, subString, kper, ilay, startDate, extent, cellsize, noDataValue, outputPath, legend, useSparseGrid)
        {
        }

        /// <summary>
        /// Constuctor for a CheckWarningLayer with an underlying IPF ResultFile object
        /// </summary>
        /// <param name="check"></param>
        /// <param name="package"></param>
        /// <param name="subString"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="startDate"></param>
        /// <param name="columnNames"></param>
        /// <param name="textFileColumnIndex"></param>
        /// <param name="valueColumnIndex"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        public CheckWarningLayer(Check check, Package package, string subString, int kper, int ilay, DateTime? startDate, string outputPath, Legend legend = null)
            : base(check, package, subString, kper, ilay, startDate, outputPath, legend)
        {
        }

        public override string ResultType
        {
            get { return CheckWarning.TYPENAME; }
        }

        public void AddWarning(float x, float y, CheckWarning result)
        {
            base.AddResult(x, y, result);
        }

        public override ResultLayer Copy()
        {
            CheckResultLayer resultLayer = null;
            if (resultFile is IDFFile)
            {
                IDFFile idfFile = (IDFFile)resultFile;
                resultLayer = new CheckWarningLayer(check, package, substring, kper, ilay, startDate, idfFile.Extent, idfFile.XCellsize, idfFile.NoDataValue, outputPath, Legend, idfFile is SparseIDFFile);
                resultLayer.AddSourceFiles(sourceFiles);
                return resultLayer;
            }
            else if (resultFile is IPFFile)
            {
                IPFFile ipfFile = (IPFFile)resultFile;
                resultLayer = new CheckWarningLayer(check, package, substring, kper, ilay, startDate, outputPath, Legend);
                resultLayer.AddSourceFiles(sourceFiles);
                return resultLayer;
            }
            else
            {
                throw new Exception("Unsupported iMOD-file '" + resultFile.GetType().ToString() + "' for ResultLayer");
            }
        }
    }
}
