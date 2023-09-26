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
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Sweco.SIF.iMOD.DLF
{
    /// <summary>
    /// Class to handle DLF-items in DLF-files, with label, color, description and width. A DLF-file is a Drill Legend File that defines coloring for boreholes.
    /// Note: The maximum number of classes is 250 (lines, excluding the heade
    /// </summary>
    public class DLFFile
    {
        /// <summary>
        /// Formatting and other cultureInfo of English (UK) language
        /// </summary>
        protected static CultureInfo EnglishCultureInfo { get; set; } = new CultureInfo("en-GB", false);

        /// <summary>
        /// Path and filename for this DLF-file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// List of DLF-items in this DLFFile object, with label, color, description and width
        /// </summary>
        public List<DLFClass> DLFClasses { get; }

        /// <summary>
        /// Create and initialize DLFFile object
        /// </summary>
        public DLFFile(string filename)
        {
            this.Filename = filename;
            this.DLFClasses = new List<DLFClass>();
        }

        /// <summary>
        /// Read an existing DLF-file into a DLFFile object
        /// </summary>
        /// <param name="filename"></param>
        public static DLFFile ReadFile(string filename)
        {
            if (!Path.GetExtension(filename).ToLower().Equals(".dlf"))
            {
                throw new Exception("Invalid DLF-file extension: " + filename);
            }

            if (!File.Exists(filename))
            {
                throw new Exception("DLF-file not found: " + filename);
            }

            DLFFile dlfFile = new DLFFile(filename);

            string dlfString = FileUtils.ReadFile(filename);
            StringReader sr = new StringReader(dlfString);

            // Skip header
            string wholeLine = sr.ReadLine();
            int lineNumber = 1;

            while ((wholeLine = sr.ReadLine()) != null)
            {
                lineNumber++;

                string[] lineValues = wholeLine.Split(new char[] { ',' });
                if ((lineValues == null) || (lineValues.Length != 6))
                {
                    throw new ToolException("Unexpected number of values in line " + lineNumber + " of DLF-file '" + Path.GetFileName(filename) + "':" + wholeLine);
                }

                if (!int.TryParse(lineValues[1], out int redValue))
                {
                    throw new ToolException("Invalid red color value in line " + lineNumber + " of DLF-file '" + Path.GetFileName(filename) + "':" + lineValues[1]);
                }
                if (!int.TryParse(lineValues[2], out int greenValue))
                {
                    throw new ToolException("Invalid green color value in line " + lineNumber + " of DLF-file '" + Path.GetFileName(filename) + "':" + lineValues[2]);
                }
                if (!int.TryParse(lineValues[3], out int blueValue))
                {
                    throw new ToolException("Invalid blue color value in line " + lineNumber + " of DLF-file '" + Path.GetFileName(filename) + "':" + lineValues[3]);
                }

                if (!float.TryParse(lineValues[5], NumberStyles.Float, EnglishCultureInfo, out float width))
                {
                    throw new ToolException("Invalid color value in line " + lineNumber + " of DLF-file '" + Path.GetFileName(filename) + "':" + lineValues[1]);
                }

                DLFClass item = new DLFClass(lineValues[0].Replace("\"", string.Empty), redValue, greenValue, blueValue, lineValues[4].Replace("\"", string.Empty), width);
                dlfFile.AddClass(item);
            }

            return dlfFile;
        }

        /// <summary>
        /// Add a DLFClass object to the list of items in this DLFFile
        /// </summary>
        /// <param name="item"></param>
        public void AddClass(DLFClass item)
        {
            DLFClasses.Add(item);
        }

        public DLFFile Copy()
        {
            DLFFile newDLFFile = new DLFFile(this.Filename);
            foreach (DLFClass dlfClass in this.DLFClasses)
            {
                newDLFFile.AddClass(dlfClass.Copy());
            }

            return newDLFFile;
        }
    }
}
