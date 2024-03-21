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
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODValidator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    public class SummaryResultLayer : ResultLayer
    {
        protected const string Name = "Summary";

        public override string ResultType
        {
            get { return SummaryResultLayer.Name; }
        }
        protected SummaryResultLayer()
            : base()
        {
            this.isResultReAddingAllowed = true;
        }

        protected SummaryResultLayer(StressPeriod stressPeriod, int ilay, string outputPath)
            : base(SummaryResultLayer.Name, null, null, stressPeriod, ilay, outputPath)
        {
            this.isResultReAddingAllowed = true;
        }

        public SummaryResultLayer(StressPeriod stressPeriod, int ilay, Extent extent, float cellsize, float noDataValue, string outputPath, ClassLegend legend = null)
            : base(SummaryResultLayer.Name, null, null, stressPeriod, ilay, extent, cellsize, noDataValue, outputPath, legend)
        {
            this.isResultReAddingAllowed = true;
        }

        protected SummaryResultLayer(Model model, StressPeriod stressPeriod, int ilay)
            : base(SummaryResultLayer.Name, null, null, stressPeriod, ilay, model.ToolOutputPath)
        {
            this.isResultReAddingAllowed = true;
        }

        public SummaryResultLayer(Model model, StressPeriod stressPeriod, int ilay, Extent extent, float cellsize, float noDataValue, ClassLegend legend = null)
            : base(SummaryResultLayer.Name, null, null, stressPeriod, ilay, extent, cellsize, noDataValue, model.ToolOutputPath, legend)
        {
            this.isResultReAddingAllowed = true;
        }

        public override ResultLayer Copy()
        {
            SummaryResultLayer copiedLayer = new SummaryResultLayer(StressPeriod, ilay, outputPath);
            copiedLayer.resultFile = this.resultFile.Copy(this.resultFile.Filename);
            copiedLayer.description = description;
            copiedLayer.processDescription = processDescription;
            copiedLayer.resultCount = resultCount;
            return copiedLayer;
        }
    }
}
