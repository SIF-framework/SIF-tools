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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sweco.SIF.Common.Tests
{
    [TestClass]
    public class UnitTest1
    {
        public static void Main(string[] args)
        {
            UnitTest1 unitTest1 = new UnitTest1();
            unitTest1.TestMethod2();
        }

        [TestMethod]
        public void TestMethod1()
        {
            string commandLine = "info";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings1 settings = new SIFToolSettings1(args);
            SIFTool1 tool = new SIFTool1(settings);
            Console.WriteLine("Testing " + tool.ToString() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args)));
            Console.WriteLine();
            int exitCode = tool.Run(true);
            Assert.IsTrue(exitCode == 0);
        }

        [TestMethod]
        public void TestMethod2()
        {
            string commandLine = "/a:5,32 /b:1 /c:0 C:\\Test\\Input *.XXX C:\\Test\\Output";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings1 settings = new SIFToolSettings1(args);
            SIFTool1 tool = new SIFTool1(settings);
            Console.WriteLine("Testing " + tool.ToString() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args)));
            Console.WriteLine();
            int exitCode = tool.Run(true);
            Assert.IsTrue(exitCode == 0);
        }

        [TestMethod]
        public void TestMethod3()
        {
            string commandLine = "/b:1 sheet.xlsx C:\\Test";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings1 settings = new SIFToolSettings1(args);
            SIFTool1 tool = new SIFTool1(settings);
            Console.WriteLine("Testing " + tool.ToString() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args)));
            Console.WriteLine();
            int exitCode = tool.Run(true);
            Assert.IsTrue(exitCode == 0);
        }

        [TestMethod]
        public void TestMethod4()
        {
            Console.WriteLine("Testing CommonUtils ...");

            string line = "1000000,1000000,b920e90c4-5a59-79b0-e5ad-9c9ab9047655,2016-12-14,NL.IMGeo,W0155.00dd119b7bdf400db29e3e0208f8b454,2021-05-21T08:52:48,2021-05-21T10:18:19,W0155,0,0,bestaand,watervlakte,,,'meer, plas, ven, vijver',";
            string[] lineValues = CommonUtils.SplitQuoted(line, ',', '\'', true, true);
            Assert.IsTrue(lineValues.Length == 17);
        }

    }
}



