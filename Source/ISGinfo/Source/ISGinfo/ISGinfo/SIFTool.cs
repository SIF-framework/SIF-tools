// ISGinfo is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ISGinfo.
// 
// ISGinfo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ISGinfo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ISGinfo. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.ISG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.ISGinfo
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

                exitcode = tool.Run(false, false);
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

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for writing info about ISG-file to console (default is extent)";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            string inputPath = settings.InputPath;
            string filterString = Path.ChangeExtension(settings.InputFilter, ".ISG");

            // Read files that match filter from inputpath 
            string[] inputFilenames = Directory.GetFiles(inputPath, filterString);
            if (inputFilenames.Length == 0)
            {
                throw new ToolException("No files found for filter '" + filterString + "' in path: " + inputPath);
            }

            try
            {
                ISGFile isgFile = ISGFile.ReadFile(inputFilenames[0], true);

                RetrieveInfo(isgFile, settings);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error reading ISG-file", ex);
            }

            ToolSuccessMessage = null;
            return exitcode;
        }

        private void RetrieveInfo(ISGFile isgFile, SIFToolSettings settings)
        {
            if (settings.IsExtentRequested)
            {
                // Force loading of segments
                int count = isgFile.Segments.Count;

                System.Console.Out.WriteLine(GetExtentString(isgFile, settings));
            }

            if (settings.IsSegmentCountRequested)
            {
                System.Console.Out.WriteLine(GetValueCountString(isgFile, settings));
            }
        }

        protected virtual string GetValueCountString(ISGFile isgFile, SIFToolSettings settings)
        {
            return isgFile.SegmentCount.ToString();
        }

        protected virtual string GetExtentString(ISGFile isgFile, SIFToolSettings settings)
        {
            Extent extent = isgFile.Extent;
            if (!settings.SnapCellSize.Equals(float.NaN))
            {
                extent = extent.Snap(settings.SnapCellSize, true);
            }

            string extentString1 = extent.llx.ToString("F3", EnglishCultureInfo) + "," + extent.lly.ToString("F3", EnglishCultureInfo) + "," + extent.urx.ToString("F3", EnglishCultureInfo) + "," + extent.ury.ToString("F3", EnglishCultureInfo);
            string extentString2 = extent.llx.ToString(EnglishCultureInfo) + "," + extent.lly.ToString(EnglishCultureInfo) + "," + extent.urx.ToString(EnglishCultureInfo) + "," + extent.ury.ToString(EnglishCultureInfo);
            string extentString = (extentString1.Length < extentString2.Length) ? extentString1 : extentString2;

            return extentString;
        }
    }
}
