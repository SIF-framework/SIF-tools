// GENcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENcreate.
// 
// GENcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENcreate. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.Common.Tests;
using Sweco.SIF.GENcreate.Tests;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;

namespace Sweco.SIF.GENcreate.Tests.Tests
{
    [TestClass]
    public class UnitTest1 : UnitTestBase
    {
        [TestMethod]
        public void TestMethod1()
        {
            string commandLine = "info"; 
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();
            int exitcode = sifTool.Run(true);
            Assert.IsTrue(exitcode == 0);
        }

        [TestMethod]
        public void TestMethod2()
        {
            string outputFilename = "..\\..\\..\\..\\..\\..\\Test\\Output\\BUFFEREXTENT.GEN";
            int llx = 181000;
            int lly = 360000;
            int urx = 222500;
            int ury = 401000;

            string commandLine = "/e:" + llx + "," + lly + "," + urx + "," + ury + " \"" + outputFilename + "\"";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();
            int exitcode = sifTool.Run(true);
            Assert.IsTrue(exitcode == 0);

            // Check resulting GEN-file
            GENFile genFile = GENFile.ReadFile(outputFilename);
            Extent extent = genFile.Extent;
            Assert.IsTrue(extent.llx.Equals(llx) && extent.lly.Equals(lly) && extent.urx.Equals(urx) && extent.ury.Equals(ury));
        }
    }
}
