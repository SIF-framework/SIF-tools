// IDFexp is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFexp.
// 
// IDFexp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFexp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFexp. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFexp
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
            catch (QuietException ex)
            {
                tool.Log.AddInfo(ex.GetBaseException().Message);
                tool.Log.AddInfo("Stopping quietly ...");
                exitcode = 0;
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
            ToolPurpose = "SIF-tool for evaluating expressions on IDF-files";
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

            // Initialize general settings of ExpFileParser
            Initialize(settings);

            // Create script interpreter and process input file
            Interpreter interpreter = CreateInterpreter();
            exitcode = interpreter.ProcessFile(settings.InputFilename);

            ToolSuccessMessage = null;

            return exitcode;
        }

        /// <summary>
        /// Create Interpreter instance for parsing and evaluating expressions in a script
        /// </summary>
        /// <returns></returns>
        protected virtual Interpreter CreateInterpreter()
        {
            return new Interpreter();
        }

        /// <summary>
        /// Propagate tool settings and initialize Interpreter with specified tool settings
        /// </summary>
        /// <param name="settings"></param>
        protected virtual void Initialize(SIFToolSettings settings)
        {
            Interpreter.UseNoDataAsValue = settings.UseNodataAsValue;
            Interpreter.Extent = settings.Extent;
            Interpreter.NoDataValue = settings.NoDataValue;
            Interpreter.IsMetadataAdded = settings.IsMetadataAdded;
            Interpreter.IsQuietMode = settings.IsQuietMode;
            Interpreter.IsResultRounded = settings.IsResultRounded;
            Interpreter.DecimalCount = settings.DecimalCount;
            Interpreter.BasePath = Path.GetDirectoryName(settings.InputFilename);
            Interpreter.IsDebugMode = settings.IsDebugMode;
            Interpreter.IsIntermediateResultWritten = settings.IsIntermediateResultWritten;

            Interpreter.OutputPath = settings.OutputPath;
            Interpreter.Log = Log ?? new Log();

            Interpreter.Initialize();
        }

    }
}
