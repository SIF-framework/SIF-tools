// IFFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IFFselect.
// 
// IFFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IFFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IFFselect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.IFFSelect;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IFF;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IFFselect
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
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for selecting IFF-pathlines in relation to specified volume";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int logIndentLevel;
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            // Create output path if not yet existing
            string outputPath = settings.OutputPath;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            int fileCount = inputFilenames.Length;

            Metadata metadata = new Metadata();
            metadata.Description = "IFF-selection";
            metadata.ProcessDescription = "Created with " + ToolName + " version " + ToolVersion;

            // Read input files
            logIndentLevel = 1;
            GENFile genFile = null;
            if (settings.GENFilename != null)
            {
                Log.AddInfo("Reading GEN-file " + Path.GetFileName(settings.GENFilename) + " ...", logIndentLevel);
                genFile = GENFile.ReadFile(settings.GENFilename);
                metadata.ProcessDescription += "GEN-file: " + genFile.Filename + "; ";
            }

            IDFFile topIDFFile = null;
            IDFFile botIDFFile = null;
            if (settings.TopLevelString != null)
            {
                if (float.TryParse(settings.TopLevelString, NumberStyles.Float, EnglishCultureInfo, out float topValue))
                {
                    topIDFFile = new ConstantIDFFile(topValue);
                }
                else
                {
                    Log.AddInfo("Reading top IDF-file " + Path.GetFileName(settings.TopLevelString) + " ...", logIndentLevel);
                    topIDFFile = IDFFile.ReadFile(settings.TopLevelString);
                    metadata.ProcessDescription += "TOP IDF-file: " + topIDFFile.Filename + "; ";
                }
            }
            if (settings.BotLevelString != null)
            {
                if (float.TryParse(settings.BotLevelString, NumberStyles.Float, EnglishCultureInfo, out float botValue))
                {
                    botIDFFile = new ConstantIDFFile(botValue);
                }
                else
                {
                    Log.AddInfo("Reading bottom IDF-file " + Path.GetFileName(settings.BotLevelString) + " ...", logIndentLevel);
                    botIDFFile = IDFFile.ReadFile(settings.BotLevelString);
                    metadata.ProcessDescription += "BOT IDF-file: " + botIDFFile.Filename + "; ";
                }
            }

            Log.AddInfo("Processing " + fileCount + " input IFF-files ...");
            foreach (string inputFilename in inputFilenames)
            {
                logIndentLevel = 1;
                string relativeFilename = FileUtils.GetRelativePath(inputFilename, settings.InputPath);
                Log.AddInfo("Processing IFF-file " + relativeFilename + " ...", logIndentLevel);
                logIndentLevel = 2;

                // Retrieve full output filename
                string outputFilename = null;
                // If a single output name has been specified, use that absolute path; otherwise, use path of current input file relative to specified input path (to allowed recursive output subdirectories)
                if (settings.OutputFilename != null)
                {
                    outputFilename = Path.GetFullPath(Path.Combine(settings.OutputPath, settings.OutputFilename));
                }
                else
                {
                    outputFilename = Path.GetFullPath(Path.Combine(settings.OutputPath, FileUtils.GetRelativePath(inputFilename, settings.InputPath)));
                }
                if (settings.OutputFilePostfix != null)
                {
                    outputFilename = FileUtils.AddFilePostFix(outputFilename, settings.OutputFilePostfix);
                }

                Log.AddInfo("Reading IFF-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
                IFFFile iffFile = IFFFile.ReadFile(inputFilename);

                metadata.Source = inputFilename;
                ProcessIFFFile(iffFile, genFile, topIDFFile, botIDFFile, outputFilename, metadata, settings, Log, logIndentLevel);
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected virtual void ProcessIFFFile(IFFFile iffFile, GENFile genFile, IDFFile topIDFFile, IDFFile botIDFFile, string outputFilename, Metadata metadata, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            List<int> inputFileParticleNumbers = iffFile.SelectParticles();
            long inputFileParticlePointCount = iffFile.ParticlePoints.Count;

            if (settings.ReverseMethod == ReverseMethodEnum.Before)
            {
                Log.AddInfo("Reversing traveltime ...", logIndentLevel + 1);
                iffFile.ReverseTravelTime();
            }

            // Process selection: first distinguish between point selection (and selection of complete flowlines) or flowline selection (and clipped flowlines)
            IFFFile newIFFFile = iffFile;
            Log.AddInfo("Selecting flowlines ...", logIndentLevel);
            if (settings.SelectPointType == SelectPointType.Undefined)
            {
                // Flowlines will be clipped in relation to the specified volume
                SelectFlowLinesMethod selectFlowLinesMethod = settings.SelectFlowLinesMethod;
                if (selectFlowLinesMethod == SelectFlowLinesMethod.Undefined)
                {
                    // The complete pathlines that pass through the volume are requested
                    // first select flowlines by clipping and afterwards the whole flowline is selected
                    selectFlowLinesMethod = SelectFlowLinesMethod.Inside;
                }

                // Distinguish between a) selection before/beforeinside and b) inside or outside
                // For the first two, also the flowline points before selected volume have to be stored and the contraints have to be evaluated together
                // For the second two, only the flowline points inside/outside have to be stored and the constraints can be evaluated one by one, which can be faster if only one or two contraints are specified
                if ((selectFlowLinesMethod == SelectFlowLinesMethod.Before) || (selectFlowLinesMethod == SelectFlowLinesMethod.BeforeAndInside))
                {
                    if (settings.Extent != null)
                    {
                        // Select all flowlines that pass through extent
                        Log.AddInfo("Selecting flowlines passing through extent ...", logIndentLevel + 1);
                        ParticleList extentParticles = newIFFFile.SelectParticles(settings.Extent, SelectPointType.All);
                        newIFFFile = newIFFFile.SelectFlowLines(extentParticles);
                    }

                    // Now select flowlines until just before (and inside) specified volume, evaluating all constraints together
                    Log.AddInfo("Selecting flowlines before " + ((selectFlowLinesMethod == SelectFlowLinesMethod.BeforeAndInside) ? "and inside " : string.Empty) + "selected volume ...", logIndentLevel + 1);
                    newIFFFile = newIFFFile.SelectFlowLines(genFile, topIDFFile, botIDFFile, settings.MinTravelTime, settings.MaxTravelTime, settings.MinVelocity, settings.MaxVelocity, selectFlowLinesMethod);
                }
                else
                {
                    // For each option clip flowlines incrementally
                    if (!settings.MinTravelTime.Equals(float.NaN))
                    {
                        Log.AddInfo("Selecting flowlines by travel time ...", logIndentLevel + 1);
                        newIFFFile = newIFFFile.SelectFlowLinesByTravelTime(settings.MinTravelTime, settings.MaxTravelTime);
                    }
                    if (!settings.MinVelocity.Equals(float.NaN))
                    {
                        Log.AddInfo("Selecting flowlines by velocity ...", logIndentLevel + 1);
                        newIFFFile = newIFFFile.SelectFlowLinesByVelocity(settings.MinVelocity, settings.MaxVelocity);
                    }
                    if (settings.TopLevelString != null)
                    {
                        Log.AddInfo("Selecting flowlines by TOP- and BOT-levels ...", logIndentLevel + 1);
                        newIFFFile = newIFFFile.SelectFlowLines(topIDFFile, botIDFFile, selectFlowLinesMethod);
                    }
                    if (settings.Extent != null)
                    {
                        Log.AddInfo("Selecting flowlines with an extent ...", logIndentLevel + 1);
                        newIFFFile = newIFFFile.SelectFlowLines(settings.Extent, selectFlowLinesMethod);
                    }
                    if (settings.GENFilename != null)
                    {
                        Log.AddInfo("Selecting flowlines with polygon(s) ...", logIndentLevel + 1);
                        newIFFFile = newIFFFile.SelectFlowLines(genFile, selectFlowLinesMethod);
                    }
                }

                if (settings.SelectFlowLinesMethod == SelectFlowLinesMethod.Undefined)
                {
                    // The complete pathlines that pass through the volume are requested
                    Log.AddInfo("Selecting complete flowlines ...", logIndentLevel + 1);
                    ParticleList particles = newIFFFile.SelectParticles();
                    newIFFFile = iffFile.SelectFlowLines(particles);
                }
            }
            else
            {
                // Flowlines will be selected completely, in relation to the specified volume and specified points
                newIFFFile = iffFile;

                if (settings.Extent != null)
                {
                    ParticleList particles1 = iffFile.SelectParticles(settings.Extent, settings.SelectPointType, settings.SelectPointMethod);
                    newIFFFile = newIFFFile.SelectFlowLines(particles1);
                }

                ParticleList particles2 = newIFFFile.SelectParticles(genFile, topIDFFile, botIDFFile, settings.MinTravelTime, settings.MaxTravelTime, settings.SelectPointType, settings.SelectPointMethod);
                newIFFFile = iffFile.SelectFlowLines(particles2);
            }

            // Calculate number of selected flowlines/points
            List<int> particleNumbers = newIFFFile.SelectParticles();
            Log.AddInfo("Resulting IFF-file has " + particleNumbers.Count() + " (out of " + inputFileParticleNumbers.Count + ") particles and " + newIFFFile.ParticlePoints.Count() + " (out of " + inputFileParticlePointCount + ") IFF-points", logIndentLevel);
            if ((newIFFFile.ParticlePoints.Count > 0) && (particleNumbers.Count == newIFFFile.ParticlePoints.Count))
            {
                Log.AddWarning("Number of particles is equal to number of IFF-points, no flowlines found.", logIndentLevel + 1);
                if (settings.SelectFlowLinesMethod == SelectFlowLinesMethod.Before)
                {
                    Log.AddWarning("Possibly flowlines start within specified volume. Use option Inside instead.", logIndentLevel + 1);
                }
            }

            if (settings.ReverseMethod == ReverseMethodEnum.After)
            {
                Log.AddInfo("Reversing traveltime ...", logIndentLevel);
                newIFFFile.ReverseTravelTime();
            }

            // Create IPF-points for current flowlines in IFF-file
            Log.AddInfo("Creating IPF-file for resulting IFF-flowlines ...", logIndentLevel);
            IPFFile newIPFFile = newIFFFile.SelectPoints();

            // Write results
            Log.AddInfo("Writing resulting IFF-file '" + Path.GetFileName(outputFilename) + "' ...", logIndentLevel);
            newIFFFile.Filename = outputFilename;
            newIFFFile.WriteFile();

            outputFilename = Path.ChangeExtension(outputFilename, ".IPF");
            Log.AddInfo("Writing resulting IPF-file '" + Path.GetFileName(outputFilename) + "' ...", logIndentLevel);
            newIPFFile.WriteFile(outputFilename, metadata);
        }
    }
}
