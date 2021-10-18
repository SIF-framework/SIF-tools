// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.Common.Tests;
using Sweco.SIF.HydroMonitorIPFconvert.Tests;

namespace Sweco.SIF.HydroMonitorIPFconvert.Tests.Tests
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
            string commandLine = "/c ..\\..\\..\\..\\..\\..\\Test\\Input *.xlsx ..\\..\\..\\..\\..\\..\\Test\\Output";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();
            int exitcode = sifTool.Run(true);
            Assert.IsTrue(exitcode == 0);
        }
    }
}
