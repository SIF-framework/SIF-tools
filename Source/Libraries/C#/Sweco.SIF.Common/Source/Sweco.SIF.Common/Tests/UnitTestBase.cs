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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common.Tests
{
    public class UnitTestBase
    {
        protected int RunAndGetOutput(SIFToolBase sifTool, out string[] outputLines)
        {
            // Store standard output stream
            TextWriter defOut = Console.Out;
            StringWriter sw = new StringWriter();

            // Redirect standard output
            Console.SetOut(sw);

            // Run tool
            int exitcode = sifTool.Run(true);

            // Reset standard output
            Console.SetOut(defOut);

            // Write tool output to Console
            Console.Write(sw.ToString());

            // Split tool output to single lines
            outputLines = sw.ToString().Replace("\r", string.Empty).Split(new char[] { '\n' });

            return exitcode;
        }

        public string GetToolName()
        {
            return Assembly.GetCallingAssembly().GetName().Name.Replace(".Tests", string.Empty).Replace("Sweco.SIF.", string.Empty).Replace("Plus", string.Empty);
        }
    }
}
