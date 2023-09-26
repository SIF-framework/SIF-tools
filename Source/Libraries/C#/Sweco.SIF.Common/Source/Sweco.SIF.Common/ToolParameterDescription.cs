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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// Class to define the description of a tool argument as shown in the ToolUsage block
    /// </summary>
    public class ToolParameterDescription : ToolArgumentDescription
    {
        /// <summary>
        /// Specifies if the parameter can be repeated one or more times
        /// </summary>
        public bool IsRepetitive { get; set; }

        /// <summary>
        /// Create instance for the description of a tool argument as shown in the ToolUsage block
        /// </summary>
        /// <param name="name">short, abbreviated name of parameter</param>
        /// <param name="description">short description of parameter, use '\n' to mark end-of-line</param>
        /// <param name="example">an example string for use of this parameter, or null when no example should be shown</param>
        /// <param name="isRepetitive">if true, this tool parameter is allowed to be repeated, individual values seperated by spaces</param>
        public ToolParameterDescription(string name, string description, string example, bool isRepetitive) : base(name, description, example)
        {
            this.IsRepetitive = isRepetitive;
        }

        /// <summary>
        /// Create instance for the description of a tool argument as shown in the ToolUsage block
        /// </summary>
        /// <param name="name">short, abbreviated name of parameter</param>
        /// <param name="description">short description of parameter, use '\n' to mark end-of-line</param>
        /// <param name="example">an example string for use of this parameter, or null when no example should be shown</param>
        public ToolParameterDescription(string name, string description, string example = null) : base(name, description, example)
        {
        }

        /// <summary>
        /// Create instance for the description of a tool parameter, with the value actually used for this parameter
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public ToolParameterDescription(string name, string value) :  base(name, value)
        {
        }
    }
}
