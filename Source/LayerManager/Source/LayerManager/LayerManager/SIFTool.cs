// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.LayerManager.LayerModelManagers;
using Sweco.SIF.LayerManager.LayerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager
{
    public class SIFTool : SIFToolBase
    {
        protected IDFLog log; 

        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
            log = null;
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
                exitcode = -1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = -1;
            }

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for checking REGIS/iMOD-layermodel for inconsistencies and/or calculates kD/c-files";
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

            // Define logging object
            string logFilename = Path.Combine(settings.OutputPath, ToolName + ".log");
            IDFLog log = new IDFLog(Log, logFilename, null);

            // Check for deletion of old files in output path
            if (settings.IsDeleteFiles)
            {
                try
                {
                    Log.AddInfo("Deleting old files in output path: " + settings.OutputPath);
                    DeleteOutputFolders(settings);
                }
                catch (Exception ex)
                {
                    Log.AddWarning("Could not delete outputfolder: " + ex.GetBaseException().Message);
                }
            }

            // Set global settings 
            PropagateSettings(settings);

            // Create appropiate LayerModelManager object
            LayerModelManager layerModelManager = CreateLayerModelManager(settings);

            // Read filenames of inputmodel as specified by user
            LayerModelMap inputLayerModelMap = layerModelManager.ReadLayerModelMap(log, 0);

            // Perform specified actions
            exitcode = ProcessLayerModelActions(layerModelManager, inputLayerModelMap, settings, log, 0);

            ToolSuccessMessage = "Finished processing";

            return exitcode;
        }

        /// <summary>
        /// Propagate values from settings to specific classes
        /// </summary>
        /// <param name="settings"></param>
        protected virtual void PropagateSettings(SIFToolSettings settings)
        {
            if (settings.IsDummyDefaultKDCSkipped)
            {
                Layer.IsDummyDefaultKDCSkipped = true;
            }
        }

        /// <summary>
        /// Create LayerModelManager object based on input type in specified settings
        /// </summary>
        /// <param name="layerModelInputType"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected LayerModelManager CreateLayerModelManager(SIFToolSettings settings)
        {
            return CreateLayerModelManager(settings.inputType, settings);
        }

        /// <summary>
        /// Create LayerModelManager object based on input type
        /// </summary>
        /// <param name="layerModelInputType"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual LayerModelManager CreateLayerModelManager(InputType layerModelInputType, SIFToolSettings settings)
        {
            switch (settings.inputType)
            {
                case InputType.iMOD:
                    return new IMODLayerModelManager(settings);
                case InputType.REGISII2:
                    return new REGISLayerModelManager(settings);
                default:
                    throw new Exception("Unknown layer model input type: " + settings.inputType);
            }
        }

        /// <summary>
        /// Perform actually specified actions for specified layermodel
        /// </summary>
        /// <param name="layerModelManager"></param>
        /// <param name="inputLayerModelMap"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        protected virtual int ProcessLayerModelActions(LayerModelManager layerModelManager, LayerModelMap inputLayerModelMap, SIFToolSettings settings, IDFLog log, int logIndentLevel)
        {
            int exitcode = 0;
            if (settings.IsKDCCalculated)
            {
                layerModelManager.CalculateKDC(inputLayerModelMap, settings, log, logIndentLevel);
            }
            if (settings.IsModelChecked)
            {
                // For checking KVA-factor should not be used
                string[] kvaFilenames = inputLayerModelMap.KVAFilenames;
                inputLayerModelMap.KVAFilenames = null;
                exitcode = layerModelManager.Check(inputLayerModelMap, settings, null, log, logIndentLevel) ? 0 : 1;
                inputLayerModelMap.KVAFilenames = kvaFilenames;
            }
            if (!settings.IsModelChecked && !settings.IsKDCCalculated)
            {
                throw new ToolException("Specify option /c and/or /kdc to run " + ToolName);
            }

            return exitcode;
        }

        /// <summary>
        /// Delete files and result subdirectories in output path as specified in settings
        /// </summary>
        /// <param name="settings"></param>
        protected virtual void DeleteOutputFolders(SIFToolSettings settings)
        {
            try
            {
                if (Directory.Exists(settings.OutputPath))
                {
                    foreach (string filename in Directory.GetFiles(settings.OutputPath))
                    {
                        File.Delete(filename);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.TOPSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.TOPSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.BOTSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.BOTSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, settings.OutputKDSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, settings.OutputKDSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, settings.OutputKDSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, settings.OutputCSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.KHVSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.KHVSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.KVVSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.KVVSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, settings.OutputThicknessSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, settings.OutputThicknessSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.MismatchSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.MismatchSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, Properties.Settings.Default.ModelchecksSubdirname)))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, Properties.Settings.Default.ModelchecksSubdirname), true);
                    }
                    if (Directory.Exists(Path.Combine(settings.OutputPath, "tmp")))
                    {
                        FileUtils.DeleteDirectory(Path.Combine(settings.OutputPath, "tmp"), true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ToolException("Could not delete folder", ex);
            }
        }

    }
}
