// iMODmetadata is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODmetadata.
// 
// iMODmetadata is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODmetadata is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODmetadata. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.Common.Tests;
using Sweco.SIF.iMODmetadata.Tests;

namespace Sweco.SIF.iMODmetadata.Tests.Tests
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
            string targetIDFFilename = @"..\..\..\..\..\..\Test\Output\TESTFILE.IDF";
            string targetMETFilename = @"..\..\..\..\..\..\Test\Output\TESTFILE.MET";
            if (File.Exists(targetMETFilename))
            {
                File.Delete(targetMETFilename);
            }

            string commandLine = "/o " + targetIDFFilename + " \"\" 23-5-2014 \"\" =ProjectXXX =\"Soome IDF-file\" =Sweco \"\" m \"\" \"Boundary IDF-files of modelX v2, E:\\IMOD\\DBASE_V2\\BND\\VERSION_1; WORKIN\\HUIDIG0\\00_BNDcorr; WORKIN\\BASIS0\\00_BNDcorr\\00_IDFbnd.bat\"  \"Modelling ZoneX3\" 1:50.000 =Sweco =www.sweco.nl =\"X van de XXX\" =xxx@sweco.nl";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();
            int exitcode = sifTool.Run(true);
            Assert.IsTrue(exitcode == 0);
            Assert.IsTrue(File.Exists(targetMETFilename));
        }
    }
}
