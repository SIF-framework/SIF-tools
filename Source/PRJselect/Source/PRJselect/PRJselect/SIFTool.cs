// PRJselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of PRJselect.
// 
// PRJselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// PRJselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with PRJselect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.PRJselect
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
            ToolPurpose = "SIF-tool for selections in PRJ-files";
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

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one input file is found for current filter '" + settings.InputFilter + "'. " +
                                        "Specify just an output path or modify input filter to select only a single file.");
            }

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Reading file " + Path.GetFileName(inputFilename) + " ...", 1);
                string outputFilename = RetrieveOutputFilename(inputFilename, settings.OutputPath, settings.InputPath, settings.OutputFilename, "PRJ");

                ProcessPRJFile(inputFilename, outputFilename, settings, Log, 1);

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected virtual void ProcessPRJFile(string inputFilename, string outputFilename, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            try
            {
                string prjString = FileUtils.ReadFile(inputFilename);
                StringReader sr = new StringReader(prjString);
                StringBuilder resultPRJFileSB = new StringBuilder();
                StringBuilder currentPackageSB = new StringBuilder();
                int packakgeLineNr = 0;

                int lineCount = 0;
                string line = sr.ReadLine();
                while (line != null)
                {
                    lineCount++;

                    if (IsPackageHeader(line))
                    {
                        // New package found, process
                        lineCount += ProcessPackage(inputFilename, ref line, packakgeLineNr, sr, resultPRJFileSB, settings, Log, logIndentLevel + 1);
                    }
                    else
                    {
                        resultPRJFileSB.AppendLine(line);
                        line = sr.ReadLine();
                    }
                }

                try
                {
                    FileUtils.WriteFile(outputFilename, resultPRJFileSB.ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not write file: " + Path.GetFileName(outputFilename), ex);
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Could not read PRJ-file: " + Path.GetFileName(inputFilename), ex);
            }
        }

        protected virtual int ProcessPackage(string prjFilename, ref string line, int lineNr, StringReader sr, StringBuilder resultPRJFileSB, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            string[] lineValues = line.Trim().Split(new char[] { ',' });

            string lv2 = lineValues[1].Trim();
            string currentPackage = lv2.Substring(1, lv2.Length - 2);
            string packageHeaderLine = line;

            // Read package definition
            line = sr.ReadLine();

            log.AddInfo("Processing package '" + currentPackage + "' ...", logIndentLevel);
            int blockLineCount = 0;
            StringBuilder packageBlockSB = new StringBuilder();
            int packageLineCount = 0;
            if (!IsExcludedPackage(currentPackage))
            {
                // Read periods until empty line or another package starts
                while (!line.Trim().Equals(string.Empty) && !IsPackageHeader(line))
                {
                    blockLineCount += ProcessPackageBlock(prjFilename, currentPackage, ref line, lineNr + 1 + blockLineCount, sr, ref packageBlockSB, ref packageLineCount, settings, log, logIndentLevel);
                }
            }
            else
            {
                // Read lines and add to result PRJ without further checks
                while (!line.Trim().Equals(string.Empty) && !IsPackageHeader(line))
                {
                    packageBlockSB.AppendLine(line);
                    blockLineCount++;
                    packageLineCount++;
                    line = sr.ReadLine();
                }
            }

            if (packageLineCount > 0)
            {
                resultPRJFileSB.AppendLine(packageHeaderLine);
                resultPRJFileSB.Append(packageBlockSB);

                return 1 + blockLineCount;
            }
            else
            {
                return 0;
            }
        }

        private bool IsExcludedPackage(string currentPackage)
        {
            if (currentPackage.ToUpper().Equals("PCG"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Process block for package
        /// </summary>
        /// <param name="prjFilename"></param>
        /// <param name="package"></param>
        /// <param name="line">first line of the package block</param>
        /// <param name="lineNr"></param>
        /// <param name="sr">stream reader pointing at remaining lines of PRJ-file</param>
        /// <param name="resultPackageBlockSB">StringBuilder to add results to</param>
        /// <param name="resultPackageLineCount">number of lines read is added to current value</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns>number of lines read</returns>
        protected virtual int ProcessPackageBlock(string prjFilename, string package, ref string line, int lineNr, StringReader sr, ref StringBuilder resultPackageBlockSB, ref int resultPackageLineCount, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            int nsub = 0;
            int nsystem = 0;
            int subLineNr = 1;
            string packageDateLine = null;
            string packageNrsLine = null;
            List<string> packageLines = new List<string>();
            List<string> extraLines = new List<string>();

            // Check for an optional date, assume any single string without ',' symbols is a 'date' (which can also be STEADY-STATE).
            string[] lineValues = line.Trim().Split(new char[] { ',' });
            string lv1 = lineValues[0];
            if (lineValues.Length == 1)
            {
                packageDateLine = line;

                line = sr.ReadLine();
                subLineNr++;
                lineValues = line.Trim().Split(new char[] { ',' });
            }

            // Read line with NSUB, NSYSTEM
            if (lineValues.Length == 2)
            {
                if (!int.TryParse(lineValues[0], out nsub))
                {
                    throw new ToolException("Invalid NSUB-value for package '" + package + "': " + lineValues[0]);
                }
                if (!int.TryParse(lineValues[1], out nsystem))
                {
                    throw new ToolException("Invalid NSUB-value for package '" + package + "': " + lineValues[1]);
                }

                packageNrsLine = line;
            }
            else
            {
                throw new ToolException("Error at line " + (lineNr + subLineNr) + ", expected 'NSUB,NSYSTEM'-line for package '" + package + "', but found: " + line);
            }

            // Read all package entries of this (period) block until an empty line or a new package is found
            bool isProcessing = true;
            bool isProcessingExtraFiles = false;
            while (isProcessing && ((line = sr.ReadLine()) != null))
            {
                subLineNr++;

                if (!line.Trim().Equals(string.Empty))
                {
                    lineValues = line.Split(new char[] { ',' });
                    if ((lineValues.Length > 6))
                    {
                        // This should be a package entry
                        string resultLine = ProcessPackageLine(prjFilename, line, package, settings, log, logIndentLevel + 1);
                        if (resultLine != null)
                        {
                            packageLines.Add(resultLine);
                        }
                    }
                    else
                    {
                        // Check for new package line
                        if (IsPackageHeader(line))
                        {
                            isProcessing = false;
                        }
                        else if (line.Contains("EXTRA FILES"))
                        {
                            isProcessingExtraFiles = true;
                        }
                        else if (isProcessingExtraFiles)
                        {
                            string resultLine = ProcessPackageLine(prjFilename, line, package, settings, log, logIndentLevel + 1);
                            if (resultLine != null)
                            {
                                extraLines.Add(resultLine);
                            }
                        }
                        else if (lineValues.Length == 1)
                        {
                            // This should be the start of another period
                            isProcessing = false;
                        }
                        else
                        {
                            throw new ToolException("Error at line " + (lineNr + subLineNr) + ", unexpected line in package '" + package + "': " + line);
                        }
                    }
                }
                else
                {
                    // empty line found, end of block
                    isProcessing = false;
                }
            }

            // Add package lines
            if (packageDateLine != null)
            {
                resultPackageBlockSB.AppendLine(packageDateLine);
            }
            if (packageLines.Count == (nsub * nsystem))
            {
                // No lines have been deleted, write original line
                resultPackageBlockSB.AppendLine(packageNrsLine);
            }
            else
            {
                int nsystem2 = packageLines.Count / nsub;
                if (nsystem2 * nsub != packageLines.Count)
                {
                    throw new ToolException("Mismatch between NSUB (" + nsub + ") and package line count " + packageLines.Count + "), which should be a multiple of NSUB");
                }
                resultPackageBlockSB.AppendLine(nsub.ToString("000") + "," + nsystem2.ToString("000"));
            }
            for (int packageLineIdx = 0; packageLineIdx < packageLines.Count; packageLineIdx++)
            {
                resultPackageBlockSB.AppendLine(packageLines[packageLineIdx]);
            }
            if (extraLines.Count > 0)
            {
                resultPackageBlockSB.AppendLine(extraLines.Count.ToString("000") + ",EXTRA FILES");
            }
            for (int extraLineIdx = 0; extraLineIdx < extraLines.Count; extraLineIdx++)
            {
                resultPackageBlockSB.AppendLine(extraLines[extraLineIdx]);
            }

            resultPackageLineCount += packageLines.Count;

            return subLineNr;
        }

        protected virtual string ProcessPackageLine(string prjFilename, string line, string currentPackage, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            string resultLine = line;

            string[] lineValues = line.Split(new char[] { ',' });

            string filename = null;
            if (lineValues.Length > 6)
            {
                // Package line
                if (lineValues[1].Trim().Equals("2"))
                {
                    filename = lineValues[6].Trim();
                }
            }
            else if (lineValues.Length == 1)
            {
                filename = lineValues[0];
            }

            // Handle settings
            if (settings.ExistFilterPackage != null)
            {
                if (currentPackage.Equals(settings.ExistFilterPackage))
                {
                    // Check if line has a filename specified
                    if (filename != null)
                    {
                        filename = filename.Trim().Replace("'", string.Empty).Replace("\"", string.Empty);
                        if (File.Exists(filename))
                        {
                            resultLine = line;
                        }
                        else
                        {
                            // ignore line with non-existing filename for specified package
                            log.AddInfo("Skipping " + currentPackage + "-line with unexisting file: " + Path.GetFileName(filename), logIndentLevel);
                            resultLine = null;
                        }
                    }
                }
                else
                {
                    resultLine = line;
                }
            }

            return resultLine;
        }

        private bool IsPackageHeader(string line)
        {
            string[] lineValues = line.Split(new char[] { ',' });

            if (lineValues.Length > 1)
            {
                string lv2 = lineValues[1].Trim();
                return (lv2.StartsWith("(") && lv2.EndsWith(")"));
            }

            return false;
        }
    }
}
