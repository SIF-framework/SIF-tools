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
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    public class ComparatorResultLayer : ResultLayer
    {
        public const string ResultTypeString = "Difference";

        public override string ResultType
        {
            get { return ResultTypeString; }
        }

        protected int partIdx;

        protected ComparatorResultLayer() : base()
        {
        }

        public ComparatorResultLayer(string id, string id2, string subString, StressPeriod stressPeriod, int ilay, string outputPath, int partIdx = 0)
            : base(id, id2, subString, stressPeriod, ilay, outputPath)
        {
            resolution = string.Empty;
            this.partIdx = partIdx;
        }

        public ComparatorResultLayer(Package package, string subString, StressPeriod stressPeriod, int ilay, string outputPath, int partIdx = 0)
            : base(package.Key, package.PartAbbreviations[partIdx], subString, stressPeriod, ilay, outputPath)
        {
            resolution = string.Empty;
            this.partIdx = partIdx;
        }

        public ComparatorResultLayer(Model model, Package package, string subString, StressPeriod stressPeriod, int ilay, int partIdx = 0)
            : base(package.Key, package.PartAbbreviations[partIdx], subString, stressPeriod, ilay, model.ToolOutputPath)
        {
            resolution = string.Empty;
            this.partIdx = partIdx;
        }

        public void SetComparisonResult(ComparatorResult comparisonResult)
        {
            if (comparisonResult != null)
            {
                this.resultFile = comparisonResult.DifferenceFile;
                if (comparisonResult.DifferenceFile != null)
                {
                    if (comparisonResult.DifferenceFile.Filename == null)
                    {
                        this.resultFile.Filename = CreateResultFilename();
                    }
                }

                AddResult(comparisonResult);
            }
        }

        public string CreateLayerDescription(string checkedParameterName, int ilay)
        {
            return checkedParameterName + " " + ResultType.ToLower() + "-file for layer " + ilay;
        }

        public override ResultLayer Copy()
        {
            ComparatorResultLayer copiedLayer = new ComparatorResultLayer(id, id2, null, StressPeriod, ilay, outputPath, partIdx);
            copiedLayer.resultFile = ResultFile.Copy();
            return copiedLayer;
        }
    }
}
