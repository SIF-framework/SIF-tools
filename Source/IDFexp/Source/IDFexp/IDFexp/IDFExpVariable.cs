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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFexp
{
    /// <summary>
    /// Class for storing properties of IDF-variables
    /// </summary>
    public class IDFExpVariable
    {
        /// <summary>
        /// Decimal count for rounding variable values. If negative no rounding is done.
        /// </summary>
        public static int DecimalCount = -1;

        /// <summary>
        /// Name of IDF-variable
        /// </summary>
        public string Name;

        /// <summary>
        /// Corresponding IDF-file
        /// </summary>
        public IDFFile IDFFile;

        /// <summary>
        /// Type of expression that was evaluated for this IDFExp-variable, i.e. IDF-file or expression
        /// </summary>
        public IDFExpressionType ExpressionType;

        /// <summary>
        /// Any prefix string before variable that defines type or subdirectory to write IDF-file to 
        /// </summary>
        public string Prefix;

        /// <summary>
        /// Optional metadata about IDFExp-variable, or null if not defined
        /// </summary>
        public Metadata Metadata;

        /// <summary>
        /// Specifies if this variable has been stored on disk or does not need storing (i.e. a constant value)
        /// </summary>
        public bool IsPersisted;

        /// <summary>
        /// Create new IDFExp-variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idfFile"></param>
        /// <param name="expressionType"></param>
        /// <param name="prefix"></param>
        /// <param name="metadata"></param>
        public IDFExpVariable(string name, IDFFile idfFile, IDFExpressionType expressionType, string prefix = null, Metadata metadata = null)
        {
            this.Name = name;
            this.IDFFile = idfFile;
            this.ExpressionType = expressionType;
            this.Prefix = prefix;
            this.Metadata = metadata;

            // The variable is already persisted if it is a constant or if it was read from an existing file
            IsPersisted = (IDFFile is ConstantIDFFile) || (expressionType == IDFExpressionType.Constant) || (expressionType == IDFExpressionType.File) || (expressionType == IDFExpressionType.Undefined);
        }

        /// <summary>
        /// Releases memory for lazy loaded variables. Should only be called after data has been written, otherwise lazy loading does not work correctly.
        /// </summary>
        public virtual void ReleaseMemory()
        {
            if (IDFFile.values != null)
            {
                IDFFile.ReleaseMemory(false);
                if (IDFExpParser.IsDebugMode)
                {
                    IDFExpParser.Log.AddInfo("Released memory for: " + IDFFile.Filename, 1);
                }
            }
        }

        /// <summary>
        /// Write data to disk if not yet persisted
        /// </summary>
        /// <param name="IsMemoryManaged"></param>
        /// <param name="log"></param>
        public virtual void Persist(bool IsMemoryManaged, Log log = null)
        {
            if (!IsPersisted && (IDFFile != null))
            {
                IDFFile.WriteFile(IDFFile.Filename, Metadata);
                IsPersisted = true;

                if (log != null)
                {
                    log.AddInfo("Expression result has been written to: " + Path.GetFileName(IDFFile.Filename), 1);
                }
            }
        }
    }
}
