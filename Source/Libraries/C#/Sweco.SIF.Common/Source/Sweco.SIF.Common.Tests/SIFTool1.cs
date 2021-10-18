// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common.Tests
{
    public class SIFTool1 : SIFToolBase
    {
        public SIFTool1(SIFToolSettings1 settings) : base(settings) 
        {
            settings.RegisterSIFTool(this);
        }

        protected override void DefineToolProperties()
        {
            Authors = new string[] { "PersonX", "PersonY" };
            ToolPurpose = "Tool for handling XXX-files ...";
        }

        protected override int StartProcess()
        {
            Log.AddInfo("Processing ...", 0);
            Log.AddInfo("XX1.XXX", 1);
            Log.AddInfo("XX2.XXX", 1);

            return 0;
        }
    }
}
