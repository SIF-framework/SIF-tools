// Del2Bin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Del2Bin.
// 
// Del2Bin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Del2Bin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Del2Bin. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.Common.Tests;
using Sweco.SIF.Del2Bin.Tests;

namespace Sweco.SIF.Del2Bin.Tests.Tests
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
            // Create test files/subdirectories
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\test1.txt", "test1");
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\test2.txt", "test2");
            Directory.CreateDirectory("..\\..\\..\\..\\..\\..\\Test\\Tmp\\Test");
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\Test\\test3.txt", "test3");

            // Delete files recursively
            string commandLine = "/f /s ..\\..\\..\\..\\..\\..\\Test\\Tmp *.txt";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();

            int exitcode;
            try
            {
                exitcode = sifTool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, sifTool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, sifTool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }

            Assert.IsTrue(exitcode == 3);
        }

        [TestMethod]
        public void TestMethod3()
        {
            // Create test files/subdirectories
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\test1.txt", "test1");
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\test2.txt", "test2");
            Directory.CreateDirectory("..\\..\\..\\..\\..\\..\\Test\\Tmp\\Test");
            FileUtils.WriteFile("..\\..\\..\\..\\..\\..\\Test\\Tmp\\Test\\test3.txt", "test3");

            // Delete complete subdirectory permanently
            string commandLine = "/f /s ..\\..\\..\\..\\..\\..\\Test\\Tmp";
            string[] args = CommonUtils.CommandLineToArgs(commandLine);
            SIFToolSettings settings = new SIFToolSettings(args);
            SIFTool sifTool = new SIFTool(settings);
            Console.WriteLine("Testing " + GetToolName() + " with arguments: " + CommonUtils.ToString(new List<string>(settings.Args), " "));
            Console.WriteLine();

            int exitcode;
            try
            {
                exitcode = sifTool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, sifTool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, sifTool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }

            Assert.IsTrue(exitcode == 1);
        }
    }
}
