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
    /// Abstract base class for tool arguments
    /// </summary>
    public abstract class ToolArgumentDescription
    {
        /// <summary>
        /// Name of argument as used in tool syntax description
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Short description of argument about use in the tool syntax
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// String with example argument values as used on the commnd-line for this tool argument, including an '/'-prefix for options.
        /// </summary>
        public string Example { get; set; }

        /// <summary>
        /// Actual value(s) for this argument as used in a specific run of the tool, or null if not used
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Create instance for the description of a tool argument as shown in the ToolUsage block
        /// </summary>
        /// <param name="name">short, abbreviated name of argument</param>
        /// <param name="description">short description of argument, use '\n' to mark end-of-line</param>
        /// <param name="example">an example string for use of this argument, or null when no example should be shown</param>
        public ToolArgumentDescription(string name, string description, string example = null)
        {
            this.Name = name;
            this.Description = description;
            this.Example = example;
            this.Value = null;
        }

        /// <summary>
        /// Create instance for the description of a tool argument, with the value actually used for this argument
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public ToolArgumentDescription(string name, string value)
        {
            this.Name = name;
            this.Description = null;
            this.Example = null;
            this.Value = value;
        }
    }
}
