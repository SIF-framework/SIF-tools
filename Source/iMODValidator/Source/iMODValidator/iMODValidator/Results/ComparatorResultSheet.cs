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
using Sweco.SIF.iMODValidator.Results;
using Sweco.SIF.Spreadsheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    public class ComparatorResultSheet : ResultSheet
    {
        protected const string ComparisonRunfilename1Prefix = "Basemodel: ";
        protected const string ComparisonRunfilename2Prefix = "Comparisonmodel: ";

        protected string comparedModelFilename;
        public string ComparedModelFilename
        {
            get { return comparedModelFilename; }
            set { comparedModelFilename = value; }
        }

        protected string comparedRunfilename;
        public string ComparedRunfilename
        {
            get { return comparedRunfilename; }
            set { comparedRunfilename = value; }
        }

        public ComparatorResultSheet(SpreadsheetManager sheetManager, string baseModelFilename, string comparedModelFilename, Extent extent)
            : base(sheetManager, baseModelFilename, extent)
        {
            this.comparedModelFilename = comparedModelFilename;
            this.NoIssuesMessage = "No differences found";
        }

        protected override string GetModelDescription1()
        {
            return ComparisonRunfilename1Prefix + baseModelFilename;
        }

        protected override string GetModelDescription2()
        {
            return ComparisonRunfilename2Prefix + comparedModelFilename;
        }

    }
}
