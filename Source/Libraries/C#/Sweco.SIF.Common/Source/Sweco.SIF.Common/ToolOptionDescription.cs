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
    /// Class to define the description of a tool option as shown in the ToolUsage block
    /// </summary>
    public class ToolOptionDescription : ToolArgumentDescription
    {
        /// <summary>
        /// Obligatory parameter names for this option
        /// </summary>
        public string[] Parameters { get; set; }

        /// <summary>
        /// Optional parameter names for this option
        /// </summary>
        public string[] OptionalParameters { get; set; }

        /// <summary>
        /// Optional parameter default values for this option
        /// </summary>
        public string[] OptionalParameterDefaults { get; set; }

        /// <summary>
        /// Optional Format string (see string.Format() for more info) to use for logging settings. 
        /// All (default) values of parameters and optional parameters will be passed as arguments to string.Format() call 
        /// and can be referred within format string with {i} with i the (zero-based) number of the parameter. 
        /// </summary>
        public string LogSettingsFormatString { get; set; }

        /// <summary>
        /// Create instance for the description of a tool option as shown in the ToolUsage block
        /// </summary>
        /// <param name="name">short, abbreviated name of option</param>
        /// <param name="description">short description of option, use '\n' to mark end-of-line</param>
        /// <param name="parameters">short (syntax) names of parameters for this option</param>
        /// <param name="optionalParameters">short (syntax) names of optional parameters for this option. Use '...' for undefined number of optional parameters.</param>
        /// <param name="optionalParameterDefaults">default values for optional parameters for this option</param>
        /// <param name="example">an example string for use of this option, or null when no example should be shown</param>
        /// <param name="logSettingsFormatString">format string (see string.Format() for more info) to use for logging settings for this option. Use {...} for remaining optionparameters.</param>
        public ToolOptionDescription(string name, string description, string[] parameters = null, string[] optionalParameters = null, string[] optionalParameterDefaults = null, string example = null, string logSettingsFormatString = null) : base(name, description, example)
        {
            this.Parameters = parameters;
            this.OptionalParameters = optionalParameters;
            this.OptionalParameterDefaults = optionalParameterDefaults;
            this.LogSettingsFormatString = logSettingsFormatString;

            // Check that tool option example starts with a slash
            if ((example != null) && !example.StartsWith("/"))
            {
                throw new Exception("Example string for tool option should start with a slash: " + example);
            }
        }

        /// <summary>
        /// Create instance for the description of a tool option, with the value actually used for this option
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public ToolOptionDescription(string name, string value) : base(name, value)
        {
            this.Parameters = null;
            this.OptionalParameters = null;
            this.OptionalParameterDefaults = null;
            this.LogSettingsFormatString = null;
        }

        /// <summary>
        /// Create instance for the description of a tool option, with the value actually used for this option
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="logSettingsFormatString">string to use for logging this settings for this option</param>
        public ToolOptionDescription(string name, string value, string logSettingsFormatString) : base(name, value)
        {
            this.Parameters = null;
            this.OptionalParameters = null;
            this.OptionalParameterDefaults = null;
            this.LogSettingsFormatString = logSettingsFormatString;
        }
    }
}
