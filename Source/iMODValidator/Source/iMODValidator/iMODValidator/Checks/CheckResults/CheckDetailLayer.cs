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
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks.CheckResults
{
    public class CheckDetailLayer : CheckResultLayer
    {
        public override string ResultType
        {
            get { return CheckDetail.TYPENAME; }
        }

        protected CheckDetailLayer() : base()
        {
            resolution = "-";
        }

        public CheckDetailLayer(Model model, Check check, Package package, int kper, int ilay, Legend legend)
            : this(check, package, kper, ilay, model.StartDate, check.GetIMODFilesPath(model), legend)
        {
            resolution = "-";
        }

        public CheckDetailLayer(Check check, Package package, int kper, int ilay, DateTime? StartDate, string outputPath, Legend legend)
            : base(check.Name, (package.Key != null) ? package.Key : null, kper, ilay, StartDate,
                   new List<string>() { "X", "Y", "ClassNumber", "ClassName", "Filename(s)", "Details", "MoreDetails", "Timeseries" }, 7, outputPath, legend)
        {
            this.check = check;
            this.package = package;
            description = CreateLayerDescription(package.Key, ilay);
            processDescription = this.Description;
            resolution = "-";

            if (legend == null)
            {
                IPFLegend ipfLegend = IPFLegend.CreateLegend("Details for " + check.Name + " in layer " + ilay, "Details for " + check.Name, Color.DarkOrange);
                this.Legend = ipfLegend;
            }

            if (this.Legend is IPFLegend)
            {
                ((IPFLegend)this.Legend).IsLabelShown = true;
                ((IPFLegend)this.Legend).SelectedLabelColumns = new List<int>() { 4, 5, 6, 7 };
            }

            // Recreate IDF-filename for this checkresultlayer
            if (this.resultFile != null)
            {
                this.resultFile.Filename = CreateResultFilename();
            }

            CheckManager.Instance.CheckForAbort();
        }

        public void AddCheckDetail(float x, float y, CheckDetail someCheckDetail)
        {
            AddResult(x, y, someCheckDetail);
        }

        protected virtual void AddResult(float x, float y, CheckDetail checkDetail)
        {
            IPFFile ipfFile = ((IPFFile)resultFile);
            List<IPFPoint> currentPoints = null;
            if (!isResultReAddingAllowed)
            {
                currentPoints = ipfFile.GetPoints(x, y, 0.01d);
            }

            if (isResultReAddingAllowed || (currentPoints == null))
            {
                // adding the same result more than once is allowed or a new result is added for this location
                string filenamesString = string.Empty;
                foreach (string filename in checkDetail.Filenames)
                {
                    filenamesString += filename + "; ";
                }
                base.AddResult(checkDetail);
                IPFPoint ipfPoint = new IPFPoint(ipfFile, new FloatPoint(x, y), new string[] {
                    x.ToString("F3"), y.ToString("F3"),
                    checkDetail.ClassNumber.ToString(),
                    CommonUtils.EnsureDoubleQuotes(checkDetail.ClassName),
                    CommonUtils.EnsureDoubleQuotes(filenamesString),
                    (checkDetail.Detail != null) ? CommonUtils.EnsureDoubleQuotes(checkDetail.Detail) : IPFFile.EmptyValue,
                    (checkDetail.MoreDetail != null) ? CommonUtils.EnsureDoubleQuotes(checkDetail.MoreDetail) : IPFFile.EmptyValue,
                    checkDetail.TimeseriesFilename });

                ipfPoint.Timeseries = (IPFTimeseries)checkDetail.Timeseries;
                ipfFile.AddPoint(ipfPoint);
            }
            else
            {
                // result already present and readding same result not allowed, ignore
            }
        }

        public override ResultLayer Copy()
        {
            ResultLayer resultLayer = new CheckDetailLayer(check, package, kper, ilay, startDate, outputPath, Legend);
            resultLayer.AddSourceFiles(sourceFiles);
            return resultLayer;
        }
    }
}
