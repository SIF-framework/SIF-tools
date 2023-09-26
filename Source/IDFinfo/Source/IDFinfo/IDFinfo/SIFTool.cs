// IDFinfo is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFinfo.
// 
// IDFinfo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFinfo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFinfo. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFinfo
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            this.SetLicense(new SIFGPLLicense(this));
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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for writing info about IDF-file to console (default is x-cellsize)";
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

            string inputPath = settings.InputPath;
            string filterString = Path.ChangeExtension(settings.InputFilter, ".IDF");

            // Read files that match filter from inputpath 
            string[] inputFilenames = Directory.GetFiles(inputPath, filterString);
            if (inputFilenames.Length == 0)
            {
                throw new ToolException("No files found for filter '" + filterString + "' in path: " + inputPath);
            }

            try
            {
                IDFFile idfFile = IDFFile.ReadFile(inputFilenames[0], true);

                RetrieveInfo(idfFile, settings);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error reading IDF-file", ex);
            }

            ToolSuccessMessage = null;
            return exitcode;
        }

        protected virtual void RetrieveInfo(IDFFile idfFile, SIFToolSettings settings)
        {
            if (settings.IsExtentRequested)
            {
                System.Console.Out.WriteLine(GetExtentString(idfFile, settings));
            }

            if (settings.IsXCellsizeRequested)
            {
                System.Console.Out.WriteLine(GetXCellsizeString(idfFile, settings));
            }

            if (settings.IsYCellsizeRequested)
            {
                System.Console.Out.WriteLine(GetYCellsizeString(idfFile, settings));
            }

            if (settings.IsValueCountRequested)
            {
                System.Console.Out.WriteLine(GetValueString(idfFile, settings));
            }
        }

        protected virtual string GetValueString(IDFFile idfFile, SIFToolSettings settings)
        {
            if (settings.ValueRequested == null)
            {
                return idfFile.RetrieveValueCount().ToString();
            }
            else if (settings.ValueRequested.Equals(float.NaN))
            {
                return idfFile.RetrieveValueCount(idfFile.NoDataValue).ToString();
            }
            else
            {
                return idfFile.RetrieveValueCount((float)settings.ValueRequested).ToString();
            }
        }

        protected virtual string GetXCellsizeString(IDFFile idfFile, SIFToolSettings settings)
        {
            return idfFile.XCellsize.ToString(EnglishCultureInfo);
        }

        protected virtual string GetYCellsizeString(IDFFile idfFile, SIFToolSettings settings)
        {
            return idfFile.YCellsize.ToString(EnglishCultureInfo);
        }

        protected virtual string GetExtentString(IDFFile idfFile, SIFToolSettings settings)
        {
            string extentString1 = idfFile.Extent.llx.ToString("F3", EnglishCultureInfo) + "," + idfFile.Extent.lly.ToString("F3", EnglishCultureInfo) + "," + idfFile.Extent.urx.ToString("F3", EnglishCultureInfo) + "," + idfFile.Extent.ury.ToString("F3", EnglishCultureInfo);
            string extentString2 = idfFile.Extent.llx.ToString(EnglishCultureInfo) + "," + idfFile.Extent.lly.ToString(EnglishCultureInfo) + "," + idfFile.Extent.urx.ToString(EnglishCultureInfo) + "," + idfFile.Extent.ury.ToString(EnglishCultureInfo);
            string extentString = (extentString1.Length < extentString2.Length) ? extentString1 : extentString2;

            return extentString1;
        }
    }
}
