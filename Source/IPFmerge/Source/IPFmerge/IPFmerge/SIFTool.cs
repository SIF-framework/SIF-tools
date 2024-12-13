// IPFmerge is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFmerge.
// 
// IPFmerge is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFmerge is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFmerge. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFmerge
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
            ToolPurpose = "SIF-tool for merging multiple IPF-files to a single IPF-file";
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

            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            CommonUtils.SortAlphanumericStrings(inputFilenames);

            Log.AddInfo("Processing input files ...");
            int fileCount = inputFilenames.Length;
            if (fileCount == 0)
            {
                Log.AddWarning("No IPF-files found for filter '" + settings.InputFilter + "' in path: " + settings.InputPath);
                return 0;
            }

            Dictionary<string, List<string>> groupDictionary = RetrieveGroups(inputFilenames, settings);
            foreach (string prefix in groupDictionary.Keys)
            {
                string outputFilename = null;
                if (groupDictionary.Keys.Count > 1)
                {
                    if (!prefix.Equals(string.Empty))
                    {
                        Log.AddInfo("Merging group '" + prefix + "' ...");
                    }
                    else
                    {
                        Log.AddInfo("Merging leftover group ...");
                    }

                    if (prefix.Equals(string.Empty))
                    {
                        outputFilename = (settings.OutputFilename == null) ? "mergedFile.IPF" : settings.OutputFilename;
                    }
                    else
                    {
                        outputFilename = prefix + ".IPF";
                    }
                }
                else
                {
                    outputFilename = (settings.OutputFilename == null) ? "mergedFile.IPF" : settings.OutputFilename;
                }

                IPFFile targetIPFFile = MergeIPFFiles(groupDictionary[prefix].ToArray(), outputFilename, settings);
                outputFilename = Path.Combine(settings.OutputPath, outputFilename);

                Log.AddInfo("Writing result IPF-file " + Path.GetFileName(outputFilename) + " ...", 1);
                targetIPFFile.WriteFile(outputFilename, null, !settings.IsTSSkipped);

            }
            System.Console.WriteLine();

            ToolSuccessMessage = "Finished merging " + fileCount + " IPF-files" + ((settings.GroupSpecifier != null) ? " in " + groupDictionary.Keys.Count + " group(s)" : string.Empty);

            return exitcode;
        }

        private Dictionary<string, List<string>> RetrieveGroups(string[] filenames, SIFToolSettings settings)
        {
            Dictionary<string, List<string>> groupDictionary = new Dictionary<string, List<string>>();

            if (settings.GroupSpecifier != null)
            {
                foreach (string filename in filenames)
                {
                    string name = Path.GetFileName(filename);
                    int prefixIdx = name.ToUpper().IndexOf(settings.GroupSpecifier.ToUpper());
                    if (prefixIdx >= 0)
                    {
                        string prefix = name.Substring(0, prefixIdx);
                        if (!groupDictionary.ContainsKey(prefix))
                        {
                            groupDictionary.Add(prefix, new List<string>());
                        }

                        groupDictionary[prefix].Add(filename);
                    }
                    else
                    {
                        if (!groupDictionary.ContainsKey(string.Empty))
                        {
                            groupDictionary.Add(string.Empty, new List<string>());
                        }
                        groupDictionary[string.Empty].Add(filename);
                    }
                }
            }
            else
            {
                groupDictionary.Add(string.Empty, filenames.ToList());
            }

            if ((settings.GroupSpecifier != null) && (groupDictionary.Keys.Count == 1) && groupDictionary.Keys.ToList()[0].Equals(string.Empty))
            {
                Log.AddWarning("No groups found for group specifier: " + settings.GroupSpecifier);
            }

            return groupDictionary;
        }

        protected virtual IPFFile MergeIPFFiles(string[] inputFilenames, string outputFilename, SIFToolSettings settings)
        {
            if (inputFilenames.Length == 0)
            {
                throw new Exception("No IPF-files found to merge");
            }

            // Read first IPF-file
            Log.AddInfo("Reading IPF-file " + Path.GetFileName(inputFilenames[0] + " ..."), 1);
            IPFFile sourceIPFFile = IPFFile.ReadFile(inputFilenames[0]);

            if (!settings.IsTSSkipped)
            {
                // Force timeseries to be loaded into memory since new file location could be different
                LoadAllTimeseries(sourceIPFFile);
            }

            // Copy definition and points from source IPF-file
            IPFFile resultIPFFile = new IPFFile();
            resultIPFFile.ColumnNames = sourceIPFFile.ColumnNames;
            resultIPFFile.AssociatedFileColIdx = sourceIPFFile.AssociatedFileColIdx;
            resultIPFFile.AssociatedFileExtension= sourceIPFFile.AssociatedFileExtension;
            resultIPFFile.Filename = outputFilename;
            resultIPFFile.AddPoints(sourceIPFFile.Points);

            int mergedFileCount = 1;
            for (int idx = 1; idx < inputFilenames.Length; idx++)
            {
                sourceIPFFile = IPFFile.ReadFile(inputFilenames[idx]);
                if (!settings.IsTSSkipped)
                {
                    // Force timeseries to be loaded into memory since new file location could be different
                    LoadAllTimeseries(sourceIPFFile);
                }

                if (sourceIPFFile.ColumnCount == resultIPFFile.ColumnCount)
                {
                    Log.AddInfo("Adding IPF-file " + Path.GetFileName(inputFilenames[idx] + " ..."), 1);
                    foreach (IPFPoint ipfPoint in sourceIPFFile.Points)
                    {
                        resultIPFFile.AddPoint(ipfPoint);
                    }
                    mergedFileCount++;
                }
                else
                {
                    Log.AddWarning("Different columncount, skipped IPF-file " + Path.GetFileName(inputFilenames[idx] + " ..."), 1);
                }
            }

            return resultIPFFile;
        }

        protected virtual void LoadAllTimeseries(IPFFile ipfFile)
        {
            foreach (IPFPoint ipfPoint in ipfFile.Points)
            {
                if (ipfPoint.HasAssociatedFile())
                {
                    ipfPoint.LoadTimeseries();
                }
            }
        }
    }
}
