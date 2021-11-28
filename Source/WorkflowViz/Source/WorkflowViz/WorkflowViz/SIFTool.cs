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
using Sweco.SIF.WorkflowViz.GraphViz;
using Sweco.SIF.WorkflowViz.Visualisation;
using Sweco.SIF.WorkflowViz.Workflows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz
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

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for visualisation of SIF-workflows with GraphViz-graphs";
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

            Initialize(settings);

            Console.WriteLine("Reading workflow ...");
            Workflow workinWorkflow = Workflow.ReadPath(settings.InputPath, settings.WorkflowSettings);

            Console.WriteLine("Creating graph ...");
            GraphSettings graphSettings = CreateGraphSettings(settings);
            WFVisualizer wfVisualizer = CreateWFGraphVisualizer(graphSettings); 
            string graphFilename = wfVisualizer.Visualize(workinWorkflow, settings.OutputPath, 0);

            ToolSuccessMessage = "Finished processing " + workinWorkflow.TotalBatchfileCount + " batchfile(s) and " + workinWorkflow.TotalWorkflowCount + " (sub)workflow(s)"; 

            if (settings.IsResultOpened)
            {
                OpenFile(graphFilename);
            }

            return exitcode;
        }

        protected virtual void Initialize(SIFToolSettings settings)
        {
            // Create output path if not yet existing
            string outputPath = settings.OutputPath;
            if (!Path.GetExtension(outputPath).Equals(string.Empty))
            {
                outputPath = Path.GetDirectoryName(outputPath);
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Set dotpath
            Dot.DotPath = settings.DotPath;

            // Set dot options
            if (settings.DotOptions != null)
            {
                Dot.DotOptions = settings.DotOptions;
            }

        }

        protected virtual WFVisualizer CreateWFGraphVisualizer(GraphSettings graphSettings)
        {
            return new WFGraphVisualizer(graphSettings);
        }

        protected virtual GraphSettings CreateGraphSettings(SIFToolSettings settings = null)
        {
            GraphSettings graphSettings = new GraphSettings();

            if (settings != null)
            {
                // Copy specified SIFTool-settings to new GraphSettings object
                graphSettings.MaxWorkflowLevels = settings.MaxWorkflowLevels;
                graphSettings.MaxRecursionLevel = settings.MaxRecursionLevel;
                graphSettings.IsBatchfileShown = settings.IsBatchfileShown;
                graphSettings.IsRunScriptsMode = settings.IsRunScriptsMode;
                graphSettings.SourcePath = settings.InputPath;
                graphSettings.WorkflowOrder = settings.WorkflowSettings.OrderStrings;
                graphSettings.IsEdgeCheckSkipped = settings.WorkflowSettings.IsEdgeCheckSkipped;
                graphSettings.IsOuterBBoxShown = true;

                if (!Path.GetExtension(settings.OutputPath).Equals(string.Empty))
                {
                    graphSettings.OutputFormat = Dot.ParseOutputFormat(Path.GetExtension(settings.OutputPath));
                    if (graphSettings.OutputFormat == OutputFormat.Undefined)
                    {
                        throw new ToolException("Extension '" + Path.GetExtension(settings.OutputPath) + "' is not defined for outputfile: " + settings.OutputPath);
                    }
                }

            }

            return graphSettings;
        }

        protected virtual void OpenFile(string filename)
        {
            CommonUtils.ExecuteCommand(CommonUtils.EnsureDoubleQuotes(filename), -1, out string outputString, Path.GetDirectoryName(filename));
        }
    }
}
