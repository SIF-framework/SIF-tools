// WorkflowViz is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of WorkflowViz.
// 
// WorkflowViz is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// WorkflowViz is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with WorkflowViz. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.WorkflowViz.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz
{
    /// <summary>
    /// Class for general WorkflowViz utility methods
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// The string used for actually writing a tab symbol
        /// </summary>
        public static string TabString = "    ";

        /// <summary>
        /// Indent and append a string, including an added end-of-line symbol, to an existing StringBuilder object
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="line"></param>
        /// <param name="indentLevel"></param>
        public static void AppendLine(StringBuilder stringBuilder, string line, int indentLevel = 0)
        {
            string tabStrings = string.Empty;
            for (int idx = 0; idx < indentLevel; idx++)
            {
                tabStrings += TabString;
            }
            stringBuilder.AppendLine(tabStrings + line);
        }

        /// <summary>
        /// Indent and append a string, excluding an added end-of-line symbol, to an existing StringBuilder object
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="someString"></param>
        /// <param name="indentLevel"></param>
        public static void Append(StringBuilder stringBuilder, string someString, int indentLevel = 0)
        {
            string tabStrings = string.Empty;
            for (int idx = 0; idx < indentLevel; idx++)
            {
                tabStrings += TabString;
            }
            stringBuilder.Append(tabStrings + someString);
        }

        /// <summary>
        /// Correct a name of a WorkflowViz item: replace a space, minus-symbol, plus-symbol or a dot with an underscore
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CorrectName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_").Replace("+","_").Replace(".", "_");
        }

        /// <summary>
        /// Check if the name of a batchfile in a SIF-workflow refers to a settings batchfile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsSettingsName(string name)
        {
            return name.ToLower().EndsWith(Batchfile.SettingsName.ToLower());
        }

        /// <summary>
        /// Check if the name of a batchfile in a SIF-workflow refers to a Runscripts batchfile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsRunscriptsName(string name)
        {
            return name.ToLower().Contains(Batchfile.RunscriptsName.ToLower() + " ") || name.ToLower().Contains(Batchfile.RunscriptsName.ToLower() + "\\n");
        }
    }
}
