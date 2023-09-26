// IDFvoxel is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFvoxel.
// 
// IDFvoxel is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFvoxel is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFvoxel. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Globalization;

namespace Sweco.SIF.IDFvoxel
{
    public class CSVVoxel
    {
        public static float NoDataValue = 999.0f;
        public static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);
        public static int ColumnCount { get; private set; } = -1;

        private static string[] columnNames;
        public static string[] ColumnNames
        {
            get { return columnNames; }
            set
            {
                columnNames = value;
                ColumnCount = columnNames.Length;
            }
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int StratCode { get; set; }
        public int LithoKlasse { get; set; }

        /// <summary>
        /// All values from CSV-row, including x,y,z and lithoclass and stratocode values
        /// </summary>
        public float[] Values { get; set; }

        public CSVVoxel(string[] lineValues)
        {
            if (lineValues.Length != ColumnCount)
            {
                if (ColumnCount == -1)
                {
                    throw new Exception("First define CSVVoxel.ColumnNames before calling CSVVoxel(string[]) constructor");
                }
                throw new ToolException("Number of CSV-values (" + lineValues.Length + ") in array does not match number of columns (" + ColumnCount + ") for parsing CSVVoxel");
            }

            Values = new float[ColumnCount];

            // Read x, y and z
            X = float.Parse(lineValues[0], NumberStyles.Float, EnglishCultureInfo);
            Y = float.Parse(lineValues[1], NumberStyles.Float, EnglishCultureInfo);
            Z = float.Parse(lineValues[2], NumberStyles.Float, EnglishCultureInfo);

            StratCode = int.Parse(lineValues[3]);
            LithoKlasse = int.Parse(lineValues[4]);

            for (int idx = 0; idx < ColumnCount; idx++)
            {
                Values[idx] = float.Parse(lineValues[idx], NumberStyles.Float, EnglishCultureInfo);
            }
        }
    }
}
