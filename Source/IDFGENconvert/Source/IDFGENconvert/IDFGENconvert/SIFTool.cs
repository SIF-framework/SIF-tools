// IDFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFGENconvert.
// 
// IDFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFGENconvert
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

        #region Instance
    
        /// <summary>
        /// Retrieve static tool instance 
        /// </summary>
        public static SIFToolBase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SIFTool(new SIFToolSettings(null));
                }
                return instance;
            }
        }
        protected static SIFToolBase instance = null;

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
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for IDF-GEN or GEN-IDF conversion.";
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

            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            if (inputFilenames.Length > 1)
            {
                // Outputfilename is only allowed when a single input file was selected
                settings.OutputFilename = null;
            }

            int fileCount = 0;
            GENFile genFile = null;
            Metadata metadata = null;
            GENIDFConverter genIDFConverter = CreateGENIDFConverter(settings, Log);
            IDFGENConverter idfGENConverter = CreateIDFGENConverter(settings, Log);
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Processing file '" + Path.GetFileName(inputFilename) + "' ...");

                bool isConverted = false;
                try
                {
                    if (Path.GetExtension(inputFilename).ToLower().Equals(".idf"))
                    {
                        // Initialize GEN-file to store result. Depending on the merge setting features are added to the existing GENFile or to a new GENFile object
                        idfGENConverter.InitializeGENFile(ref genFile, ref metadata, settings, inputFilename);

                        // Convert IDF-file to GEN-file
                        isConverted = idfGENConverter.Convert(inputFilename, ref genFile, settings.OutputPath, 1);

                        // When not merged, write GEN-file for this input IDF-file now
                        if (!settings.IsMerged && (genFile.Count > 0))
                        {
                            genFile.WriteFile(metadata);
                        }
                    }
                    else if (Path.GetExtension(inputFilename).ToLower().Equals(".gen"))
                    {
                        // Convert GEN-file to IDF-file
                        isConverted = genIDFConverter.Convert(inputFilename, settings.OutputPath, (settings.OutputFilename != null) ? settings.OutputFilename : inputFilename, 1);
                    }
                    else
                    {
                        Log.AddWarning("Skipping unknown file '" + Path.GetFileName(inputFilename) + "' ...", 1);
                    }

                    if (isConverted)
                    {
                        fileCount++;
                    }
                }
                catch (ToolException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new Exception("Unexpected error while converting file: " + Path.GetFileName(inputFilename), ex);
                }
            }

            if (settings.IsMerged && (genFile.Count > 0))
            {
#if DEBUG
                string genIDs = string.Empty;
                for (int featureIdx = 0; featureIdx < genFile.Features.Count; featureIdx++)
                {
                    genIDs += genFile.Features[featureIdx].ID + ": " + genFile.Features[featureIdx].Points.Count + "\n";
                }
                FileUtils.WriteFile(Path.ChangeExtension(genFile.Filename, ".txt"), genIDs);
#endif
                genFile.WriteFile(metadata);
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Create IDFGENConverter object with specified settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual IDFGENConverter CreateIDFGENConverter(SIFToolSettings settings, Log log)
        {
            return new IDFGENConverter(settings, log);
        }

        /// <summary>
        /// Create GENIDFConverter object with specified settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual GENIDFConverter CreateGENIDFConverter(SIFToolSettings settings, Log log)
        {
            return new GENIDFConverter(settings, Log);
        }
    }
}
