// IMFcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IMFcreate.
// 
// IMFcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IMFcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IMFcreate. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IMFcreate
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string INIFilename { get; set; }
        public string OutputPath { get; set; }
        public bool IsUpdateIMODFiles { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            INIFilename = null;
            OutputPath = null;
            IsUpdateIMODFiles = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("iniFile", "INI-file with settings for creation of iMOD IMF-files", "C:\\Test\\Input.INI");
            AddToolParameterDescription("outPath", "Path for resulting IMF-file", "C:\\Test\\Output");
            AddToolOptionDescription("u", "Update min-max-values in IDF-files", "/u", "Min-max-values in IDF-files are updated");

            AddToolUsageOptionPostRemark("\n" +
                                     "The INI-file is divided into sections. A section is started with a keyword between brackets '[]'.\n" +
                                     "Each section consists of lines with key-value pairs seperated by an '=' symbol. The following sections are available:\n" +
                                     "\n" +
                                     "[PARAMETERS]                    Mandatory section/keys that define general settings\n" +
                                     "EXTENT=minx,miny,maxx,maxy      Define extent to show when iMOD is opened, leave empty to use union of mapfiles\n" +
                                     "OPENIMOD=<0|1>                  To open IMOD with created IMF choose 1, otherwise 0\n" +
                                     "IMFFILENAME=<filename>          Define a filename for the IMF file\n" +
                                     "IMODEXE=<filename>              Define the path to the iMOD executable for opening the IMF\n" +
                                     "ADDONCE=<0|1>                   To skip files with the same filename as files already added once choose 1, otherwise 0\n" +
                                     "\n" +
                                     "[CROSSSECTION]                  Optional section/keys that define files/settings for 2D cross section tool\n" +
                                     "REGIS=<path>                    Define path with REGIS IDF-files to load.\n" +
                                     "                                Note: REGIS-layers are added only when TOP-files (*-t-*.IDF) are present\n" +
                                     "REGISORDER=<filename>           Define filename for ASCI-file (txt) with ordered lines with a REGIS-prefix,\n" +
                                     "                                as a single value or the last value in multiple comma seperated values.\n" +
                                     "REGISCOLORS=TNO|AQF|<filename>  Define Excel filename (XSLX) with colors per REGIS-layer with header in row 1 and\n" +
                                     "                                rows with REGIS (sub)strings in column 1 and RGB (integer) values in columns 2 to 4\n" +
                                     "                                Or use 'TNO' for TNO REGIS-colors, or 'AQF' for yellow/green hues for aquifer/aquitards\n" +
                                     "LAYERSASLINES=<paths>           Define one or more (';' seperated) directories with " + Properties.Settings.Default.TOPFilePrefix + "/" + Properties.Settings.Default.BOTFilePrefix + " IDF-files to show as lines\n" +
                                     "LINECOLOR=r1,g1,b1[;r2,g2;b2]   Define RGB (integer values) for color of " + Properties.Settings.Default.TOPFilePrefix + "/" + Properties.Settings.Default.BOTFilePrefix + "-line. As a default red hues are used\n" +
                                     "LAYERSASPLANES=<paths>          Define one or more (';' seperated) directories with " + Properties.Settings.Default.TOPFilePrefix + "/" + Properties.Settings.Default.BOTFilePrefix + " IDF-files to show as planes\n" +
                                     "                                Colors are defined automatically with yellow for aquifers, green for aquitards\n" +
                                     "SELECTED=<0|1>                  Use 1 to select the IDF-file(s), 0 if otherwise. Default is 1 for REGIS/Modellayers.\n" +
                                     "\n" +
                                     "[MAPS]                          Optional section/keys that define paths/settings for map iMOD-files\n" +
                                     "FILE=<path>                     Specify one or more FILE-lines with <path> a filename/path (wildcards are allowed)\n" +
                                     "                                Below each FILE-line optional settings can be defined as described below\n" +
                                     "                                If the file type is equal to type of the previous file, the previous settings are reused\n" +
                                     "For IDF-files, the following optional keys are available to define settings:\n" +
                                     "LEGEND=<filename>               Define path of an iMOD legend (.LEG) file\n" +
                                     "CSLEGEND=<filename>             Define path of an iMOD Drill File Legend (.DLF) file to visualizee file in cross sections\n" +
                                     "SELECTED=<0|1>                  Use 1 to select the IDF-file, 0 if otherwise. Default is 0\n" +
                                     "LINECOLOR=r,g,b                 Define RGB color (integer values) for a line in the crosssection tool\n" +
                                     "FILLCOLOR=r,g,b                 Define RGB color (integer values) for a plane in the crosssection tool\n" +
                                     "PRFTYPE=<integer>               Define PRF-type as a combination of indidual values: 1=Active, 3=Line, 4=Points, 8=Fill, 64=Legend\n" +
                                     "\n" +
                                     "For GEN-files, the following optional keys are available to define settings:\n" +
                                     "THICKNESS=<integer>             Define thickness of line as integer\n" +
                                     "COLOR=r,g,b                     Define RGB (3 integer values) for color of GEN-line\n" +
                                     "SELECTED=<0|1>                  Use 1 to select the GEN-file, 0 if otherwise. Default is 0\n" +
                                     "\n" +
                                     "For IPF-files, the following optional keys are available to define settings:\n" +
                                     "LEGEND=<filename>               Define path of legend\n" +
                                     "COLUMN=<integer>                Define columnnumber to apply legend to, legend should be also specified\n" +
                                     "                                Use negative number to count backwards, -1 indicating the last column.\n" +
                                     "TEXTSIZE=<integer>              Define size of labelled text as an integer (use 0 to hide text)\n" +
                                     "COLOR=r,g,b                     Define RGB (3 integer values) for color of GEN-line\n" +
                                     "THICKNESS=<integer>             Define thickness of line as integer\n" +
                                     "PRFTYPE=<integer>               Define PRF-type as a combination of indidual values: 1=Active, 2=Ass.File, 64=Legend\n" +
                                     "SELECTED=<0|1>                  Use 1 to select the IPF-file, 0 if otherwise. Default is 0\n" +
                                     "\n" +
                                     "[OVERLAYS]                      Optional section/keys that define overlay GEN-files\n" +
                                     "For GEN-files, the following optional keys are available to define settings:\n" +
                                     "THICKNESS=<integer>             Define thickness of line as integer\n" +
                                     "COLOR=r,g,b                     Define RGB color (integer values) of GEN-line");

        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 2)
            {
                // Parse syntax 1:
                INIFilename = parameters[0];
                OutputPath = parameters[1];
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        /// <summary>
        /// Parse and process tool option
        /// </summary>
        /// <param name="optionName">the character(s) that identify this option</param>
        /// <param name="hasOptionParameters">true if this option has parameters</param>
        /// <param name="optionParametersString">a string with optional comma seperated parameters for this option</param>
        /// <returns>true if recognized and processed</returns>
        protected override bool ParseOption(string optionName, bool hasOptionParameters, string optionParametersString = null)
        {
            if (optionName.ToLower().Equals("r"))
            {
                IsUpdateIMODFiles = true;
            }
            else
            {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check the number of parsed arguments against the number of expected arguments. Override to check actual values.
        /// </summary>
        public override void CheckSettings()
        {
            // Perform syntax checks 
            base.CheckSettings();

            // Retrieve full paths and check existance
            if (INIFilename != null)
            {
                INIFilename = ExpandPathArgument(INIFilename);
                if (!File.Exists(INIFilename))
                {
                    throw new ToolException("INI-file does not exist: " + INIFilename);
                }
            }

            // Create output path if not yet existing
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
        }
    }
}
