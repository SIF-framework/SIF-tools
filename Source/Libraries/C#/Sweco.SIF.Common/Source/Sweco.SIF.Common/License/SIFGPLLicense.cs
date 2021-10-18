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
    /// Class for SIF-basis GPL-license (GNU General Public License) for specific SIF-basis tools
    /// </summary>
    public class SIFGPLLicense : SIFLicense
    {
        /// <summary>
        /// Constructor for SIFGPLLicense instance for SIF-tools with GPL-license
        /// </summary>
        /// <param name="tool"></param>
        public SIFGPLLicense(SIFToolBase tool) : base(tool)
        {
            tool.InitializeHeaderLicenseLine("This tool is part of SIF-basis. It is open source, and free to use and distribute under the GNU GPLv3 license or later.");
        }

        /// <summary>
        /// Full license text of this SIF/license-type to accept by user
        /// </summary>
        public override string LicenseText
        {
            get
            {
                return GetLicenseText("This SIF-toolset", "this SIF-toolset");
            }
        }

        /// <summary>
        /// Full name of this SIF-license
        /// </summary>
        public override string SIFLicenseName
        {
            get { return SIFInstrumentName + "-basis (GPL-version)"; }
        }

        /// <summary>
        /// Title of license form for this license
        /// </summary>
        public override string LicenseFormTitle
        {
            get { return "License " + SIFLicenseName + " " + SIFVersion; }
        }

        /// <summary>
        /// Width of textbox in license form
        /// </summary>
        protected override int LicenseForm_TextBoxWidth
        {
            get { return 580; }
        }

        /// <summary>
        /// Height of textbox in license form
        /// </summary>
        protected override int LicenseForm_TextBoxHeight
        {
            get { return 320; }
        }

        /// <summary>
        /// Postfix (letter) that indicates this SIF/license-type and is used as a postfix for the tool version
        /// </summary>
        public override string SIFTypeVersionPostfix
        {
            get { return "g"; }
        }

        /// <summary>
        /// First part of string in license file that stores acceptance by a specific user, e.g. "SIF-license accepted by"
        /// </summary>
        protected override string AcceptancePrefix
        {
            get { return SIFLicenseName + " " + SIFVersion + ", license accepted by "; }
        }

        /// <summary>
        /// Retrieves license text for this license and toolname
        /// </summary>
        /// <param name="Toolname"></param>
        /// <param name="lowercaseToolName">optionally specify the toolname in lowercase as used within a sentence</param>
        /// <param name="isFileLineAdded">if true, a line is added that states that this file is part of the defined tool</param>
        /// <returns></returns>
        public string GetLicenseText(string Toolname, string lowercaseToolName = null, bool isFileLineAdded = false)
        {
            string fileLine = isFileLineAdded ? "This file is part of " + Toolname + ".\r\n" + "\r\n" : string.Empty;

            string licenseText = Toolname + " is part of " + SIFTypeName + ", a framework by Sweco for iMOD-modelling\r\n"
                         + "Copyright(C) 2021 Sweco Nederland B.V.\r\n"
                         + "\r\n"
                         + "All rights to this software and documentation, including intellectual\r\n"
                         + "property rights, are owned by Sweco Nederland B.V., except for third\r\n"
                         + "party code or libraries which are governed by their own license.\r\n"
                         + "\r\n"
                         + fileLine
                         + Toolname + " is free software: you can redistribute it and/or modify\r\n"
                         + "it under the terms of the GNU General Public License as published by\r\n"
                         + "the Free Software Foundation, either version 3 of the License, or\r\n"
                         + "(at your option) any later version.\r\n"
                         + "\r\n"
                         + Toolname + " is distributed in the hope that it will be useful,\r\n"
                         + "but WITHOUT ANY WARRANTY; without even the implied warranty of\r\n"
                         + "MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\r\n"
                         + "GNU General Public License for more details.\r\n"
                         + "\r\n"
                         + "You should have received a copy of the GNU General Public License\r\n"
                         + "along with " + lowercaseToolName + ". If not, see <https://www.gnu.org/licenses/>.\r\n";

            return licenseText;
        }
    }
}
