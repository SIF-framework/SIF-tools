// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.iMOD.Tests
{
    [TestClass]
    public class IPFUnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Log log = new Log(Log.ConsoleListener);

            log.AddInfo("IPFTest 1 started ...");

            string ipfTest1Filename = "..\\..\\..\\..\\..\\Test\\Output\\Test1.IPF";
            log.AddInfo("Creating IPF-file ...", 1);
            IPFFile ipfFile = new IPFFile(log, 1);
            ipfFile.AddXYColumns();
            ipfFile.AddColumn("TEST1");
            ipfFile.AddColumn("TEST2");
            ipfFile.AddColumn("TEST3");
            IPFPoint ipfPoint = new IPFPoint(ipfFile, new FloatPoint(10, 10), new string[] { "A", "B", "C" });
            ipfFile.AddPoint(ipfPoint);
            ipfFile.AddPoint(new IPFPoint(ipfFile, new FloatPoint(20, 20), new string[] { "D", "E", "F" } ));
            Metadata metadata = new Metadata("IPF-file for IPFTest1");
            ipfFile.WriteFile(ipfTest1Filename, metadata);

            log.AddInfo("Reading IPF-file ...", 1);
            IPFFile ipfFile2 = IPFFile.ReadFile(ipfTest1Filename, true);
            int colIdx = ipfFile2.FindColumnName("TEST");
            Assert.IsTrue(colIdx > 0);
            Assert.IsTrue(ipfFile2.PointCount == 2);
            Assert.IsTrue(ipfFile2.ColumnCount == 5);
            IPFPoint ipfPoint2 = ipfFile2.GetPoint(0);
            Assert.IsTrue(ipfPoint2.X == 10);
            List<string> columnValues = ipfPoint2.ColumnValues;
            Assert.IsTrue(columnValues[2].Equals("A"));
            log.AddInfo("IPFTest 1 finished successfully");
        }

        [TestMethod]
        public void TestMethod2()
        {
            try
            {
                Log log = new Log(Log.ConsoleListener);
                log.Filename = @"C:\Data\Tools\SwecoTools\iMOD-tools\IPFplot\Test\Input2\meetreeksen_L1.log";

                string ipfFilename = @"C:\Data\Tools\SwecoTools\iMOD-tools\IPFplot\Test\Input2\meetreeksen_L1.IPF";
                ipfFilename = Path.GetFullPath(ipfFilename);
                if (!File.Exists(ipfFilename))
                {
                    throw new AssertFailedException("IPF-file not found");
                }
                System.Console.WriteLine("Reading IPF-file ...");
                IPFFile ipfFile = IPFFile.ReadFile(ipfFilename);

                System.Console.WriteLine("Selecting on Timeseries ...");

                //IPFFile ipfFile2 = IPFUtils.SelectPoints(ipfFile, new DateTime(2010, 1, 1), new DateTime(2020, 12, 31), 0, false);
                //if (ipfFile2 != null)
                //{
                //    System.Console.WriteLine("Writing selected IPF-file ...");
                //    string ipfFilename2 = @"C:\Data\Tools\SwecoTools\iMOD-tools\IPFplot\Test\Output2\meetreeksen_L1 #2.IPF";
                //    ipfFilename2 = Path.GetFullPath(ipfFilename2);
                //    ipfFile2.WriteFile(ipfFilename2);
                //}
                //else
                //{
                //    throw new AssertFailedException("No concave hull found");
                //}
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(ex.GetBaseException().Message, ex);
            }
        }

        [TestMethod]
        public void TestMethod3()
        {
            try
            {
                IPFFile ipfFile1 = new IPFFile();
                ipfFile1.AddColumn("ID");
                ipfFile1.AssociatedFileColIdx = 2;
                IPFPoint ipfPoint = new IPFPoint(ipfFile1, new FloatPoint(1, 1), new List<string>() { "1", "1", "ID1" });
                IPFTimeseries ipfTimeseries = new IPFTimeseries(new List<DateTime>() {
                    new DateTime(2000, 3, 27), new DateTime(2000, 4, 1), new DateTime(2000, 4, 17), new DateTime(2000, 4, 19), new DateTime(2000, 4, 24), new DateTime(2000, 5, 15) },
                    new List<float>() { 1, 2, 3, float.NaN, 4, 5 });

                Timeseries timeseries1 = ipfTimeseries.Select(new DateTime(2000, 4, 1), new DateTime(2000, 5, 15));
                // Timeseries timeseries2 = ipfTimeseries.InterpolateTimeseries(new DateTime(2000, 4, 1), new DateTime(2000, 5, 1), 1, true);
                System.Console.WriteLine(timeseries1.ToString());
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(ex.GetBaseException().Message, ex);
            }
        }

    }
}
