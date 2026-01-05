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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Joins
{
    /// <summary>
    /// Class for storing data to support join operations
    /// </summary>
    public class JoinInfo
    {
        /// <summary>
        /// List of key-values for each point in ipfFile1 (file that is joined to)
        /// </summary>
        public IDictionary<string, List<int>> KeyDictionary1 { get; set; }

        /// <summary>
        /// List of key-values for each point in joinFile2 (file that is joined)
        /// </summary>
        public IDictionary<string, List<int>> KeyDictionary2;

        /// <summary>
        /// Indices of selected columns from joinFile2 (file that is joined)
        /// </summary>
        public List<int> SelectedColIndices2 { get; set; }

        /// <summary>
        /// Create empty JoinInfo object
        /// </summary>
        public JoinInfo()
        {
            KeyDictionary1 = null;
            KeyDictionary2 = null;
            SelectedColIndices2 = null;
        }

        /// <summary>
        /// Update datastructures for joining for specified joinFile1 and/or joinFile2
        /// </summary>
        /// <param name="joinFile1"></param>
        /// <param name="joinFile2"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public virtual void UpdateJoinInfo(JoinFile joinFile1, JoinFile joinFile2, JoinSettings settings, Log log, int logIndentLevel)
        {
            List<int> keyIndices2 = null;
            if (joinFile1 == null)
            {
                // JoinFile1 is not specified, just initialize JoinFile2 now
                if (settings.KeyString2 != null)
                {
                    keyIndices2 = joinFile2.RetrieveKeyIndices(settings.KeyString2);
                    KeyDictionary2 = joinFile2.RetrieveKeyDictionary(keyIndices2);
                }
                else
                {
                    // No keys are present, use Natural Join (which selects identical column names); for now start with all columns
                }
                SelectedColIndices2 = joinFile2.GetColumnIndices(settings, keyIndices2);
            }
            else
            {
                List<int> keyIndices1 = null;
                if (settings.KeyString1 != null)
                {
                    keyIndices1 = joinFile1.RetrieveKeyIndices(settings.KeyString1);
                    KeyDictionary1 = joinFile1.RetrieveKeyDictionary(keyIndices1);
                }
                else
                {
                    string keyString = string.Empty;

                    // Keys are defined automatically from common columns in file 1 and 2
                    foreach (string columnName1 in joinFile1.ColumnNames)
                    {
                        if ((joinFile1.FindColumnName(columnName1) >= 0) && (joinFile2.FindColumnName(columnName1) >= 0))
                        {
                            keyString += columnName1 + ",";
                        }
                    }
                    if (!keyString.Equals(string.Empty))
                    {
                        keyString = keyString.Substring(0, keyString.Length - 1);
                        keyIndices1 = joinFile1.RetrieveKeyIndices(keyString);
                        keyIndices2 = joinFile2.RetrieveKeyIndices(keyString);
                        KeyDictionary1 = joinFile1.RetrieveKeyDictionary(keyIndices1);
                        KeyDictionary2 = joinFile2.RetrieveKeyDictionary(keyIndices2);

                        SelectedColIndices2 = joinFile2.GetColumnIndices(settings, keyIndices2);

                        log.AddInfo("Automatically retrieved key(s) for file 1 and 2: " + keyString, logIndentLevel);
                    }
                    else
                    {
                        SelectedColIndices2 = joinFile2.GetColumnIndices(settings);
                    }
                }
            }
        }
    }
}
