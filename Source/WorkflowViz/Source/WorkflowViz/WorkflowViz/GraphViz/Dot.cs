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
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Currently avaiable output formats for GraphViz via this GraphViz wrapper
    /// </summary>
    public enum OutputFormat
    {
        Undefined,
        PNG,
        GIF,
        BMP,
        PDF,
        SVG,
        PS,
        CMAPX,
        HTML
    }

    /// <summary>
    /// Wrapper class around dot executable
    /// </summary>
    public class Dot
    {
        public static string DotPath { get; set; } = null;
        public static string DotOptions { get; set; }

        public Dot ()
        {

        }

        /// <summary>
        /// Runs Dot.exe for the given GraphViz textfile to create a Graph with the specified outputformat
        /// </summary>
        /// <param name="gvFilename"></param>
        /// <param name="outputFilename"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        public virtual int Run(string gvFilename, string outputFilename, OutputFormat outputFormat, string dotOptions = null)
        {
            int exitcode = 0;

            if (DotPath == null)
            {
                throw new ToolException("DotPath has not been set");
            }
            if ((DotPath == null) || !File.Exists(DotPath))
            {
                throw new ToolException("Specified DotPath does not exist: " + DotPath);
            }
            if (dotOptions == null)
            {
                dotOptions = Dot.DotOptions;
            }

            // Execute dot
            string executable = Path.GetFileName(Path.GetFullPath(DotPath));
            string executablePath = Path.GetDirectoryName(Path.GetFullPath(DotPath));
            string command = executable + ((dotOptions != null) ? (dotOptions + " ") : string.Empty) + " " + GetFormatOption(outputFormat) + " " + GetOutputOption(outputFilename) + " " + CommonUtils.EnsureDoubleQuotes(gvFilename);
            exitcode = CommonUtils.ExecuteCommand(command, 0, out string outputString, executablePath);
            if (exitcode != 0)
            {
                Console.Error.WriteLine("Some error occurred when running dot.exe: " + outputString);
                Console.WriteLine("Used command-line: " + command);
            }

            return exitcode;
        }

        /// <summary>
        /// Runs Dot with two different output formats.
        /// </summary>
        /// <param name="gvFilename"></param>
        /// <param name="outputFilename1"></param>
        /// <param name="outputFormat1"></param>
        /// <param name="outputFilename2"></param>
        /// <param name="outputFormat2"></param>
        /// <param name="dotOptions"></param>
        /// <returns></returns>
        public virtual int Run(string gvFilename, string outputFilename1, OutputFormat outputFormat1, string outputFilename2, OutputFormat outputFormat2, string dotOptions = null)
        {
            int exitcode = 0;

            if (DotPath == null)
            {
                throw new ToolException("DotPath has not been set");
            }
            if (!File.Exists(DotPath))
            {
                throw new ToolException("Specified DotPath does not exist: " + DotPath);
            }
            if (dotOptions == null)
            {
                dotOptions = Dot.DotOptions;
            }

            // Execute dot
            string executable = Path.GetFileName(Path.GetFullPath(DotPath));
            string executablePath = Path.GetDirectoryName(Path.GetFullPath(DotPath));
            string command = executable + " " + ((dotOptions != null) ? (dotOptions + " ") : string.Empty) + GetFormatOption(outputFormat1) + " " + GetOutputOption(outputFilename1) + " " + GetFormatOption(outputFormat2) + " " + GetOutputOption(outputFilename2) + " " + CommonUtils.EnsureDoubleQuotes(gvFilename);
            exitcode = CommonUtils.ExecuteCommand(command, 0, out string outputString, executablePath);
            if (exitcode != 0)
            {
                Console.Error.WriteLine("Some error occurred when running dot.exe: " + outputString);
            }

            return exitcode;
        }

        /// <summary>
        /// Retrieve corresponding string for specified output format
        /// </summary>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        public static string GetFormatString(OutputFormat outputFormat)
        {
            return outputFormat.ToString();
        }

        public static OutputFormat ParseOutputFormat(string formatString)
        {
            OutputFormat outputFormat = OutputFormat.Undefined;
            formatString = formatString.ToLower().Replace(".", string.Empty);
            if (formatString.Equals("png"))
            {
                outputFormat = OutputFormat.PNG;
            }
            else if (formatString.Equals("gif"))
            {
                outputFormat = OutputFormat.GIF;
            }
            else if (formatString.Equals("pdf"))
            {
                outputFormat = OutputFormat.PDF;
            }
            else if (formatString.Equals("ps"))
            {
                outputFormat = OutputFormat.PS;
            }
            else if (formatString.Equals("bmp"))
            {
                outputFormat = OutputFormat.BMP;
            }
            else if (formatString.Equals("svg"))
            {
                outputFormat = OutputFormat.SVG;
            }
            else if (formatString.Equals("cmapx"))
            {
                outputFormat = OutputFormat.CMAPX;
            }
            else if (formatString.Equals("html"))
            {
                outputFormat = OutputFormat.HTML;
            }
            else
            {
                outputFormat = OutputFormat.Undefined;
            }
            return outputFormat;
        }

        /// <summary>
        /// Get substring in dot command line for specified output filename
        /// </summary>
        /// <param name="outputFilename"></param>
        /// <returns></returns>
        protected static string GetOutputOption(string outputFilename)
        {
            return "-o " + CommonUtils.EnsureDoubleQuotes(outputFilename);
        }

        /// <summary>
        /// Get substring in dot command line for specified output format
        /// </summary>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        protected static string GetFormatOption(OutputFormat outputFormat)
        {
            string formatString = "-T";

            if (outputFormat == OutputFormat.Undefined)
            {
                throw new ToolException("OutputFormat is not yet defined");
            }
            formatString += GetFormatString(outputFormat).ToLower();

            return formatString;
        }
    }
}
