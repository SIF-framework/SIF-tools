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
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.iMOD.Tests
{
    [TestClass]
    public class IDFUnitTest1
    {
        public static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                // Check MF6TOIDF-result
                IDFFile idfMF6 = IDFFile.ReadFile(@"C:\Data\Tools\SwecoTools\iMOD-tools\IDFexp\Test\MF6Test\HEAD_STEADY-STATE_L1_ITB.IDF", true); // TEST_DBL.IDF", true);
                List<float> uniqueValues1 = idfMF6.RetrieveUniqueValues();
                uniqueValues1.RemoveRange(10, uniqueValues1.Count - 10);
                System.Console.WriteLine("MF6 IDF-file read, NoData-value: " + idfMF6.NoDataValue + ", x-cellsize: " + idfMF6.XCellsize + ", First (10) unique values :" + CommonUtils.ToString(uniqueValues1, "E03", EnglishCultureInfo));
                idfMF6.WriteFile(@"C:\Data\Tools\SwecoTools\iMOD-tools\IDFexp\Test\MF6Test\HEAD_STEADY-STATE_L1_#2.IDF");

                // Check double precision
                IDFFile idfDoubleFile = IDFFile.ReadFile(@"..\..\..\..\..\Test\UnitTest\IDFTest\DoublePrecision IDF.IDF");
                // IDFFile idfDoubleFile = IDFFile.ReadIDFFile(@"C:\Data\Tools\SwecoTools\iMOD-tools\IDFexp\Test\Input2\KLEI_DIKTE.IDF", true);
                if (idfDoubleFile.IsDoublePrecisionFile() && !idfDoubleFile.Values[0][0].Equals(idfDoubleFile.NoDataValue))
                {
                    List<float> uniqueValues = idfDoubleFile.RetrieveUniqueValues();
                    uniqueValues.RemoveRange(10, uniqueValues.Count - 10);
                    System.Console.WriteLine("Double precision IDF-file read, NoData-value: " + idfDoubleFile.NoDataValue + ", First (10) unique values :" + CommonUtils.ToString(uniqueValues, ","));
                }
                else
                {
                    throw new Exception("Not a double precision file: " + idfDoubleFile.Filename);
                }
                IDFFile idfSingleFile = IDFFile.ReadFile(@"..\..\..\..\..\Test\UnitTest\IDFTest\SinglePrecision IDF.IDF");
                if (!idfSingleFile.IsDoublePrecisionFile())
                {
                    System.Console.WriteLine("Single precision IDF-file read, NoData-value: " + idfDoubleFile.NoDataValue + ", unique values :" + CommonUtils.ToString(idfDoubleFile.RetrieveUniqueValues(), ","));
                }
                else
                {
                    throw new Exception("Not a double precision file: " + idfDoubleFile.Filename);
                }

                System.Console.WriteLine("Reading IDF-file ...");
                string idfFilename = @"..\..\..\..\..\Test\UnitTest\IFFTest\TOP_L1.IDF";
                idfFilename = Path.GetFullPath(idfFilename);
                if (!File.Exists(idfFilename))
                {
                    throw new AssertFailedException("File not found: " + idfFilename);
                }
                IDFFile idfFile = IDFFile.ReadFile(idfFilename);
                Extent srcExtent = idfFile.Extent;
                Extent clipExtent = srcExtent.Copy();
                int shift = (int)idfFile.XCellsize / 2;
                Extent newExtent = srcExtent.Move(shift, shift);
                idfFile.Extent.llx = newExtent.llx;
                idfFile.Extent.lly = newExtent.lly;
                idfFile.Extent.urx = newExtent.urx;
                idfFile.Extent.ury = newExtent.ury;
                IDFFile clippedIDFFile = idfFile.ClipIDF(clipExtent);
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(ex.GetBaseException().Message, ex);
            }
        }
    }
}
