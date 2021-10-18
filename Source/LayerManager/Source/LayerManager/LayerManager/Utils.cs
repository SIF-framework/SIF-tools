// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager
{
    public class Utils
    {
        /// <summary>
        /// Create file pattern string from specified string by prefiing with * and postfixing with *.*
        /// </summary>
        /// <param name="someString"></param>
        /// <returns></returns>
        public static string CreateFilePatternString(string someString)
        {
            return "*" + someString + "*.*";
        }

        /// <summary>
        /// Retrieve numeric integer value from string
        /// </summary>
        /// <param name="someString"></param>
        /// <returns></returns>
        public static int GetNumericValue(string someString)
        {
            int idx = -1;
            Regex regEx = new Regex("[0-9]+");
            Match regExMatch = regEx.Match(someString);
            if (regExMatch.Index >= 0)
            {
                idx = regExMatch.Index;
            }
            return idx;
        }

        /// <summary>
        /// Select IDF- and ASC-files from specified array with filenames
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns></returns>
        public static string[] SelectIDFASCFiles(string[] filenames)
        {
            List<string> filenameList = new List<string>();
            for (int i = 0; i < filenames.Length; i++)
            {
                if (Path.GetExtension(filenames[i]).ToLower().Equals(".idf")
                    || Path.GetExtension(filenames[i]).ToLower().Equals(".asc"))
                {
                    filenameList.Add(filenames[i]);
                }
                else
                {
                    // skip file
                }
            }
            return (filenameList.Count > 0) ? filenameList.ToArray() : null;
        }
    }
}
