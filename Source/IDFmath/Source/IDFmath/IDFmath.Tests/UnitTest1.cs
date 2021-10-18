// IDFmath is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFmath.
// 
// IDFmath is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFmath is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFmath. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.Common.Tests;
using Sweco.SIF.IDFmath.Tests;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.IDFmath.Tests.Tests
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
            string idf1Filename = @"..\..\..\..\..\..\Test\Input\basis25.IDF";
            string idf2Filename = @"..\..\..\..\..\..\Test\Input\plus100.IDF";
            string outputFilename = @"..\..\..\..\..\..\Test\Output\som.IDF";
            if (File.Exists(outputFilename))
            {
                File.Delete(outputFilename);
            }

            string commandLine = "/o /v:,0 " + idf1Filename + " + " + idf2Filename + " " + outputFilename;
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + sifTool.ToString() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();
            int exitcode = sifTool.Run(true);
            Assert.IsTrue(exitcode == 0);
            Assert.IsTrue(File.Exists(outputFilename));

            IDFFile idfFile1 = IDFFile.ReadFile(idf1Filename);
            IDFFile idfFile2 = IDFFile.ReadFile(idf2Filename);
            IDFFile idfFile3 = IDFFile.ReadFile(outputFilename);
            float value1 = idfFile1.GetValue(195562.5f, 530562.5f);
            float value2 = idfFile2.GetValue(195562.5f, 530562.5f);
            float value3 = idfFile3.GetValue(195562.5f, 530562.5f);

            Assert.IsTrue((value1 + value2).Equals(value3));
        }
    }
}
