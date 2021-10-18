// iMODmetadata is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODmetadata.
// 
// iMODmetadata is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODmetadata is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODmetadata. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.iMODmetadata
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string iMODFilename { get; set; }
        public Metadata NewMetadata { get; set; }

        public bool IsOverwrite { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            iMODFilename = null;
            IsOverwrite = false;
            NewMetadata = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("imodFile", "Filename (incl. extension) of iMOD-file to create or add metadata for", "C:\\Test\\TESTFILE.IDF");
            AddToolParameterDescription("metadata", "One or more of the following metadata parameters, in this order:\n"
                                                    + "  location (is overwritten, not merged)\n"
                                                    + "  publicationDate (is overwritten, not merged)\n"
                                                    + "  version (is overwritten, not merged)\n"
                                                    + "  modelversion (is overwritten, not merged)\n"
                                                    + "  description\n"
                                                    + "  producer\n"
                                                    + "  type\n"
                                                    + "  unit\n"
                                                    + "  resolution\n"
                                                    + "  source\n"
                                                    + "  processDescription\n"
                                                    + "  scale\n"
                                                    + "  organisation\n"
                                                    + "  website\n"
                                                    + "  contact\n"
                                                    + "  emailaddress\n"
                                                    + "When fields are merged, equal data is not added again. If new version is empty the current version (if\n"
                                                    + "numeric) is increased with one. Surround empty values or values with spaces with double quotes (\"), but a\n"
                                                    + "\"-symbol should not be preceded by an \\-symbol. To overwrite existing values, prefix with a \"" + MetadataSettings.Instance.OverwritePrefix + "\" symbol.",
                                                    "23-5-2014 1 ProjectX " + MetadataSettings.Instance.OverwritePrefix + "\"Some IDF-file\" " + MetadataSettings.Instance.OverwritePrefix + "Sweco IDF m \"\" \"modelv1.1\\XXX\" \"Some dataprocessing\" 1:50.000 Sweco www.sweco.nl \"X van de XXX\" \"xxx@sweco.nl\"",
                                                    true);

            AddToolOptionDescription("o", "Overwrite existing MET-file. If not specified metadata is merged.","/o", "Existing MET-file is overwritten");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length >= 1)
            {
                // Parse syntax 1:
                iMODFilename = parameters[0];

                NewMetadata = ParseMetadata(iMODFilename, parameters);

                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        protected Metadata ParseMetadata(string iMODFilename, string[] parameters)
        {
            Metadata newMetadata = new Metadata();
            newMetadata.IMODFilename = iMODFilename;

            if (Path.GetExtension(iMODFilename).ToLower().Equals(".met"))
            {
                // When MET-extension is specified directly, keep same lowercase/uppercase extension
                if (Path.GetExtension(iMODFilename).StartsWith(".m"))
                {
                    newMetadata.METFilename = Path.Combine(Path.GetDirectoryName(iMODFilename), Path.GetFileNameWithoutExtension(iMODFilename) + ".met");
                }
                else
                {
                    newMetadata.METFilename = Path.Combine(Path.GetDirectoryName(iMODFilename), Path.GetFileNameWithoutExtension(iMODFilename) + ".MET");
                }
            }
            else
            {
                newMetadata.METFilename = Path.Combine(Path.GetDirectoryName(iMODFilename), Path.GetFileNameWithoutExtension(iMODFilename) + ".MET");
            }

            int idx = 0;
            newMetadata.Location = (parameters.Length > 1) ? parameters[idx + 1] : null;
            if ((newMetadata.Location == null) || (newMetadata.Location.Equals(string.Empty)))
            {
                newMetadata.Location = Path.GetDirectoryName(iMODFilename);
            }

            if (parameters.Length > idx + 2)
            {
                string dateTimeString = parameters[idx + 2].Trim();
                if ((dateTimeString != null) && !dateTimeString.Equals(string.Empty))
                {
                    DateTime datetime;
                    if (DateTime.TryParse(parameters[idx + 2], out datetime))
                    {
                        newMetadata.PublicationDate = (DateTime?)datetime;
                    }
                    else
                    {
                        throw new ToolException("Publication date could not be parsed: " + parameters[idx + 2]);
                    }
                }
                else
                {
                    newMetadata.PublicationDate = null;
                }
            }
            else
            {
                newMetadata.PublicationDate = null;
            }

            if (newMetadata.PublicationDate.Equals(null))
            {
                newMetadata.PublicationDate = DateTime.Now;
            }

            newMetadata.Version = (parameters.Length > idx + 3) ? parameters[idx + 3] : string.Empty;
            newMetadata.Modelversion = (parameters.Length > idx + 4) ? parameters[idx + 4] : string.Empty;
            newMetadata.Description = (parameters.Length > idx + 5) ? RemoveTextQuotes(parameters[idx + 5]) : string.Empty;
            newMetadata.Producer = (parameters.Length > idx + 6) ? RemoveTextQuotes(parameters[idx + 6]) : string.Empty;
            newMetadata.Type = (parameters.Length > idx + 7) ? RemoveTextQuotes(parameters[idx + 7]) : string.Empty;
            if ((newMetadata.Type == null) || newMetadata.Type.Trim().Equals(string.Empty))
            {
                // If not defined explicitly, use file extension without the dot
                string ext = Path.GetExtension(iMODFilename);
                if ((ext != null) && (ext.Length > 1))
                {
                    newMetadata.Type = ext.Substring(1).ToUpper();
                }
                else
                {
                    newMetadata.Type = string.Empty;
                }
            }

            newMetadata.Unit = (parameters.Length > idx + 8) ? RemoveTextQuotes(parameters[idx + 8]) : string.Empty;
            newMetadata.Resolution = (parameters.Length > idx + 9) ? RemoveTextQuotes(parameters[idx + 9]) : string.Empty;
            if ((newMetadata.Resolution == null) || newMetadata.Resolution.Trim().Equals(string.Empty))
            {
                if (Path.GetExtension(iMODFilename).Substring(1).ToUpper().Equals("IDF"))
                {
                    if (File.Exists(iMODFilename))
                    {
                        IDFFile idfFile = IDFFile.ReadFile(iMODFilename, true);
                        if (idfFile != null)
                        {
                            newMetadata.Resolution = idfFile.XCellsize.ToString(EnglishCultureInfo) + "x" + idfFile.YCellsize.ToString(EnglishCultureInfo);
                        }
                    }
                }
            }
            newMetadata.Source = (parameters.Length > idx + 10) ? RemoveTextQuotes(parameters[idx + 10]) : string.Empty;
            newMetadata.ProcessDescription = (parameters.Length > idx + 11) ? RemoveTextQuotes(parameters[idx + 11]) : string.Empty;
            newMetadata.Scale = (parameters.Length > idx + 12) ? RemoveTextQuotes(parameters[idx + 12]) : string.Empty;
            newMetadata.Organisation = (parameters.Length > idx + 13) ? RemoveTextQuotes(parameters[idx + 13]) : string.Empty;
            newMetadata.Website = (parameters.Length > idx + 14) ? RemoveTextQuotes(parameters[idx + 14]) : string.Empty;
            newMetadata.Contact = (parameters.Length > idx + 15) ? RemoveTextQuotes(parameters[idx + 15]) : string.Empty;
            newMetadata.Emailaddress = (parameters.Length > idx + 16) ? RemoveTextQuotes(parameters[idx + 16]) : string.Empty;

            // Check for leftover parameters
            if (parameters.Length > idx + 17)
            {
                throw new ToolException("Invalid argumentcount, more arguments present (" + parameters.Length + ") than expected (" + (idx + 17) + "):\n" + CommonUtils.ToString(parameters.ToList(), " "));
            }

            return newMetadata;
        }

        protected string RemoveTextQuotes(string someText)
        {
            if (someText.StartsWith("\""))
            {
                someText = someText.Substring(2);
            }
            if (someText.EndsWith("\""))
            {
                someText = someText.Substring(1, someText.Length - 1);
            }
            return someText;
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
            if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
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
            if (iMODFilename != null)
            {
                iMODFilename = ExpandPathArgument(iMODFilename);
            }

            if (!Directory.Exists(Path.GetDirectoryName(iMODFilename)))
            {
                System.Console.WriteLine("Creating directory: " + Path.GetDirectoryName(iMODFilename) + " ...");
                Directory.CreateDirectory(Path.GetDirectoryName(iMODFilename));
            }
        }
    }
}
