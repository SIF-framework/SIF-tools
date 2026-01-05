// IDFexp is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFexp.
// 
// IDFexp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFexp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFexp. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFexp
{
    /// <summary>
    /// Class to storing the definition and currrent iteration of a FOR-loop
    /// </summary>
    public class ForLoopDef
    {
        protected const string ForCountKeyword = "count";

        public int LineIdx;
        public string VarName;
        public int Idx;
        public List<string> LoopValues;

        /// <summary>
        /// Parse FOR-statement: FOR i=1 TO n
        /// </summary>
        /// <param name="wholeLine"></param>
        /// <param name="lineIdx">index of line after FOR-statement</param>
        /// <returns></returns>
        public static ForLoopDef Parse(string wholeLine, int lineIdx)
        {
            ForLoopDef forLoopDef = new ForLoopDef();
            forLoopDef.LineIdx = lineIdx;
            string forString = wholeLine.Substring(3).Trim();

            // Parse variablename
            int eqIdx = forString.IndexOf("=", 1);
            if (eqIdx < 0)
            {
                throw new ToolException("Error in FOR-expression, =-symbol missing: " + wholeLine);
            }
            forLoopDef.VarName = forString.Substring(0, eqIdx).Trim();
            
            // Parse startvalue
            forString = forString.Substring(eqIdx + 1).Trim();
            int toIdx = forString.ToLower().IndexOf(" to ");
            if (toIdx < 0)
            {
                throw new ToolException("Error in FOR-expression, TO-keyword missing: " + wholeLine);
            }
            string startIdxString = forString.Substring(0, toIdx).Trim();
            if (!int.TryParse(startIdxString, out int startIdx))
            {
                throw new ToolException("Error in FOR-expression, invalid initial index: " + startIdxString);
            }

            // Parse last index
            int lastIdx;
            forString = forString.Substring(toIdx + " to ".Length).Trim();
            if (forString.ToLower().StartsWith(ForCountKeyword + "("))
            {
                // Remove count-keyword, parenthesis and optional quotes
                string pathString = forString.Substring(ForCountKeyword.Length + 1, forString.Length - ForCountKeyword.Length - 2).Replace("\"", string.Empty).Trim();
                try
                {
                    string pathStringExpanded = Environment.ExpandEnvironmentVariables(pathString);
                    string filter;
                    string path; 
                    string ext = Path.GetExtension(pathStringExpanded);
                    if ((ext != null) && !ext.Equals(string.Empty))
                    {
                        filter = Path.GetFileName(pathStringExpanded);
                        path = Path.GetDirectoryName(pathStringExpanded);
                    }
                    else
                    {
                        filter = "*.*";
                        path = pathStringExpanded;
                    }
                    string[] filenames = Directory.GetFiles(path.Equals(string.Empty) ? Interpreter.BasePath : path, filter);
                    lastIdx = filenames.Length;
                }
                catch (Exception ex)
                {
                    throw new ToolException("Error in FOR-expression, invalid path for count()-expression: " + pathString, ex);
                }
            }
            else if (!int.TryParse(forString, out lastIdx))
            {
                throw new ToolException("Error in FOR-expression, invalid last index: " + forString);
            }

            // Add loopvalues
            forLoopDef.LoopValues = new List<string>();
            for (int idx = startIdx; idx <= lastIdx; idx += 1)
            {
                forLoopDef.LoopValues.Add(idx.ToString());
            }

            // Start with first LoopValue
            forLoopDef.Idx = 0;

            return forLoopDef;
        }
    }
}
