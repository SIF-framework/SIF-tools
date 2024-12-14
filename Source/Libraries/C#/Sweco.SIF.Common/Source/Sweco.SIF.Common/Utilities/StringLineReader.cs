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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common.Utilities
{
    /// <summary>
    /// Subclass of System.IO.StringReader that keeps track of the number of the line that was read
    /// </summary>
    public class StringLineReader : StringReader
    {
        /// <summary>
        /// Number of last line that has been read with ReadLine(sr) method
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// Creates a new LineReader and its underlying StringReader object 
        /// </summary>
        /// <param name="s"></param>
        public StringLineReader(string s) : base(s)
        {
            LineNumber = 0;
        }

        /// <summary>
        /// Reads a line of characters from the current string, increases linenumber and returns the data as a string
        /// </summary>
        /// <returns>The next line from the current string, or null if the end of the string is reached</returns>
        public override string ReadLine()
        {
            LineNumber++;
            return base.ReadLine();
        }

        /// <summary>
        /// Reads a line of characters from the current string asynchronously, increases linenumber and returns the data as a string
        /// </summary>
        /// <returns>The next line from the current string, or null if the end of the string is reached</returns>
        public override Task<string> ReadLineAsync()
        {
            LineNumber++;
            return base.ReadLineAsync();
        }
    }
}
