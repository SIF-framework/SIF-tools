// GENcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENcreate.
// 
// GENcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENcreate. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENcreate
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
            Log log = null;
            try
            {
                // Call SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);
                log = tool.Log;

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, log);
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
            ToolPurpose = "SIF-tool for creating GEN-files";
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

            // Create output path if not yet existing
            string outputPath = Path.GetDirectoryName(settings.OutputFilename);
            if ((outputPath != null) && !outputPath.Equals(string.Empty))
            {
                outputPath = Path.GetFullPath(outputPath);
            }
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            Log.AddInfo("Creating GEN-feature(s) ...");
            GENFile genFile = CreateFeatures(settings, Log, 1);
            Log.AddInfo("Writing GEN-file " + Path.GetFileName(settings.OutputFilename) + " ...");
            genFile.WriteFile(settings.OutputFilename);

            ToolSuccessMessage = "GEN-file has been successfully created";

            return exitcode;
        }

        protected virtual GENFile CreateFeatures(SIFToolSettings settings, Log log, int v)
        {
            GENFile genFile = new GENFile();

            if (settings.Extent == null)
            {
                throw new ToolException("Please define extent to create GEN-feature for");
            }
            if (!settings.Extent.IsValidExtent())
            {
                throw new ToolException("Specified extent has negative/zero areasize and is not valid: " + settings.Extent.ToString());
            }

            genFile.AddFeature(new GENPolygon(genFile, 1, new List<Point>(new Point[] {
                        new FloatPoint(settings.Extent.llx, settings.Extent.ury),
                        new FloatPoint(settings.Extent.urx, settings.Extent.ury),
                        new FloatPoint(settings.Extent.urx, settings.Extent.lly),
                        new FloatPoint(settings.Extent.llx, settings.Extent.lly),
                        new FloatPoint(settings.Extent.llx, settings.Extent.ury) })));

            return genFile;
        }
    }
}
