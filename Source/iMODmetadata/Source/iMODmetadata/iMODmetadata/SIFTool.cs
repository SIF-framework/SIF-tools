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
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODmetadata
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for adding or merging metadata to iMOD-files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            Metadata oldMetadata = null;
            if (File.Exists(settings.NewMetadata.METFilename) && !settings.IsOverwrite)
            {
                oldMetadata = Metadata.ReadMetaFile(settings.NewMetadata.METFilename);
            }

            if (oldMetadata != null)
            {
                System.Console.WriteLine("Adding metadata to existing MET-file: " + settings.NewMetadata.METFilename);
                oldMetadata.MergeMetadata(settings.NewMetadata);
                oldMetadata.MetadataLanguage = settings.NewMetadata.MetadataLanguage;
                oldMetadata.WriteMetaFile();
            }
            else
            {
                System.Console.WriteLine("Creating metadata for MET-file: " + settings.NewMetadata.METFilename);
                RemovePrefixes(settings.NewMetadata);
                settings.NewMetadata.WriteMetaFile();
            }

            return exitcode;
        }

        public void RemovePrefixes(Metadata metadata)
        {
            metadata.IMODFilename = Metadata.RemovePrefixes(metadata.IMODFilename);
            metadata.Location = Metadata.RemovePrefixes(metadata.Location);

            metadata.Version = Metadata.RemovePrefixes(metadata.Version);
            metadata.Modelversion = Metadata.RemovePrefixes(metadata.Modelversion);
            metadata.Description = Metadata.RemovePrefixes(metadata.Description);
            metadata.Producer = Metadata.RemovePrefixes(metadata.Producer);
            metadata.Type = Metadata.RemovePrefixes(metadata.Type);
            metadata.Unit = Metadata.RemovePrefixes(metadata.Unit);
            metadata.Resolution = Metadata.RemovePrefixes(metadata.Resolution);
            metadata.Source = Metadata.RemovePrefixes(metadata.Source);
            metadata.ProcessDescription = Metadata.RemovePrefixes(metadata.ProcessDescription);
            metadata.Scale = Metadata.RemovePrefixes(metadata.Scale);
            metadata.Organisation = Metadata.RemovePrefixes(metadata.Organisation);
            metadata.Website = Metadata.RemovePrefixes(metadata.Website);
            metadata.Contact = Metadata.RemovePrefixes(metadata.Contact);
            metadata.Emailaddress = Metadata.RemovePrefixes(metadata.Emailaddress);
        }
    }
}
