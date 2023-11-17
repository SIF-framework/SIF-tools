// IDFmerge is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFmerge.
// 
// IDFmerge is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFmerge is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFmerge. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.ASC;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFmerge
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
		/// <param name="args">command-line arguments</param>stat
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
            AddAuthor("Koen Jansen");
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for (groupwise) aggregation of IDF-files";
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

			string[] idfFilenames = GetInputFilenames(settings);
            if (idfFilenames != null)
            {
                if (idfFilenames.Length > 0)
                {
                    Log.AddInfo("Searching for '" + settings.InputFilter + "' in input path ...", 1);
                    if (settings.GroupIndices != null)
					{
                        Log.AddInfo("Grouping IDF-files ...", 1);
						Dictionary<string, List<string>> groupSubstringDictionary = CreateGroups(idfFilenames, settings);
						foreach(string groupSubstring in groupSubstringDictionary.Keys)
						{
                            Log.AddInfo("Processing group '" + groupSubstring + "' ...", 1);
                            string[] groupIDFFilenames = groupSubstringDictionary[groupSubstring].ToArray();
							ProcessIDFFiles(groupIDFFilenames, settings);
						}
					}
					else
					{
						ProcessIDFFiles(idfFilenames, settings);
					}
                }
                else
                {
                    Log.AddWarning("No IDF-files found for specified filter: " + settings.InputFilter, 1);
                }
            }

            return exitcode;
        }

		protected virtual string[] GetInputFilenames(SIFToolSettings settings)
		{
			return Directory.GetFiles(settings.InputPath, settings.InputFilter, SearchOption.TopDirectoryOnly);
		}

		private void ProcessIDFFiles(string[] idfFilenames, SIFToolSettings settings)
		{
            if ((idfFilenames != null) && (idfFilenames.Length > 0))
            {
                Log.AddInfo("Retrieving union extent of " + idfFilenames.Length + " input files ...", 2);
                Extent resultExtent = RetrieveExtentUnion(idfFilenames);

                // Process first, initial IDF-file
                Log.AddInfo("Reading " + Path.GetFileName(idfFilenames[0]) + " ...", 2);
                IDFFile resultIDFFile = ReadIDFFile(idfFilenames[0], settings);
                if (!resultIDFFile.Extent.Contains(resultExtent))
                {
                    resultIDFFile = resultIDFFile.EnlargeIDF(resultExtent);
                }

				IDFFile countIDFFile = resultIDFFile.IsNotEqual(resultIDFFile.NoDataValue);
				countIDFFile.ReplaceValues(countIDFFile.NoDataValue, 0);
				countIDFFile.Filename = "count.IDF";

				if (!settings.UseNodataCalculationValue && settings.IgnoreNoDataValue && (settings.StatFunction.Equals(StatFunction.Sum) || settings.StatFunction.Equals(StatFunction.Mean)))
				{
					// For sum, mean, ensure that NoData-values are ignored and do not result in a NoData-value
					resultIDFFile.NoDataCalculationValue = 0;
				}

                // Calculate number of points between 5% logmessages, use multiple of 50
                int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(idfFilenames.Length, 5);

                for (int fileIdx = 1; fileIdx < idfFilenames.Length; fileIdx++)
                {
                    if (fileIdx % logSnapPointMessageFrequency == 0)
                    {
                        Log.AddInfo("Processing GEN-features " + (fileIdx + 1) + "-" + (int)Math.Min(idfFilenames.Length, (fileIdx + logSnapPointMessageFrequency)) + " of " + idfFilenames.Length + " ...", 3);
                    }

                    string idfFilename = idfFilenames[fileIdx];

                    Log.AddInfo("Reading " + Path.GetFileName(idfFilename) + " ...", 2);
                    IDFFile idfFile = ReadIDFFile(idfFilename, settings);
                    MergeIDFFile(ref resultIDFFile, ref countIDFFile, idfFile, settings);
                }

                if (settings.StatFunction == StatFunction.Mean)
                {
					if (settings.IgnoreNoDataValue)
					{
						resultIDFFile /= countIDFFile;
					}
					else
					{
						resultIDFFile /= idfFilenames.Length;
					}
                }

                string fileExtension = Path.GetExtension(idfFilenames[0]).ToUpper().Substring(1);
                Metadata resultMetadata = new Metadata(settings.StatFunction.ToString() + " of " + fileExtension + "-files");
                resultMetadata.ProcessDescription = "Automatically generated with " + ToolName + " " + ToolVersion + ", " + SIFLicense;
                resultMetadata.Source = Path.Combine(settings.InputPath, settings.InputFilter);
                if (settings.GroupIndices != null)
                {
                    resultMetadata.Source += ", grouped by '" + GetSubstring(Path.GetFileName(idfFilenames[0]), settings.GroupIndices) + "'";
                }

                WriteResults(idfFilenames, resultIDFFile, resultMetadata, countIDFFile, settings);
            }
		}

        /// <summary>
        /// Retrieve union of extents of all IDF-files for which filenames are specified. 
        /// Note: filenames should refer to existing files.
        /// </summary>
        /// <param name="idfFilenames">array with filenames of IDF-files</param>
        /// <returns></returns>
        private Extent RetrieveExtentUnion(string[] idfFilenames)
        {
            Extent extent = null;

            if ((idfFilenames != null) && (idfFilenames.Length > 0))
            {
                IDFFile idfFile1 = IDFFile.ReadFile(idfFilenames[0], true);
                extent = idfFile1.Extent;

                // Calculate number of points between 5% logmessages, use multiple of 50
                int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(idfFilenames.Length, 5);

                for (int fileIdx = 1; fileIdx < idfFilenames.Length; fileIdx++)
                {
                    if (fileIdx % logSnapPointMessageFrequency == 0)
                    {
                        Log.AddInfo("Processing GEN-features " + (fileIdx + 1) + "-" + (int)Math.Min(idfFilenames.Length, (fileIdx + logSnapPointMessageFrequency)) + " of " + idfFilenames.Length + " ...", 3);
                    }

                    IDFFile idfFile = IDFFile.ReadFile(idfFilenames[fileIdx], true);
                    if (!extent.Contains(idfFile.Extent))
                    {
                        extent = extent.Enlarge(idfFile.Extent);
                    }
                }
            }

            return extent;
        }

        /// <summary>
        /// Create groups of filenames as specified in settings
        /// </summary>
        /// <param name="idfFilenames"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> CreateGroups(string[] idfFilenames, SIFToolSettings settings)
		{
			Dictionary<string, List<string>> groupSubstringDictionary = new Dictionary<string, List<string>>();
			foreach (string idfFilename in idfFilenames)
			{
				string substring = GetSubstring(Path.GetFileNameWithoutExtension(idfFilename), settings.GroupIndices);
				if (groupSubstringDictionary.ContainsKey(substring))
				{
					groupSubstringDictionary.TryGetValue(substring, out List<string> idfFilenameGroup);
					idfFilenameGroup.Add(idfFilename);
				}
				else
				{
					List<string> idfFilenameGroup = new List<string> { idfFilename };
					groupSubstringDictionary.Add(substring, idfFilenameGroup);
				}
			}
			return groupSubstringDictionary;
		}

        /// <summary>
        /// Retrieve substring from some string as defined by specified list of indices
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="groupIndices">List of arrays with two indices i1 and 2 that define each group substring</param>
        /// <returns></returns>
		private string GetSubstring(string someString, List<int[]> groupIndices)
		{
			string substring = null;

			foreach (int[] idxPair in groupIndices)
			{
                // Parse and check one-based indices and allow negative indices for backward reference
				int idx1 = idxPair[0];
				int idx2 = idxPair[1];
				if (idxPair[0] <= 0)
				{
					idx1 = someString.Length + idxPair[0];
				}
				if (idxPair[1] <= 0)
				{
					idx2 = someString.Length + idxPair[1];
				}
				if (idx1 > idx2)
				{
					throw new ToolException("Invalid substring indices for IDF-filename '" + someString + "': " + idxPair[0] + "," + idxPair[1]);
				}
				if (idx1 <= 0)
				{
					throw new ToolException("Invalid substring index 1 for IDF-filename '" + someString + "': " + idxPair[0]);
				}
				if (idx2 > someString.Length)
				{
					throw new ToolException("Invalid substring index 2 for IDF-filename '" + someString + "': " + idxPair[1]);
				}

				string newString = someString.Substring(idx1 - 1, idx2 - idx1 + 1);
				substring = substring + newString;
			}
			return substring;
		}

		protected virtual IDFFile ReadIDFFile(string idfFilename, SIFToolSettings settings)
        {
            IDFFile idfFile = null;
            if (IDFFile.HasIDFExtension(idfFilename))
            {
                idfFile = IDFFile.ReadFile(idfFilename);
            }
            else if (ASCFile.HasASCExtension(idfFilename))
            {
                ASCFile ascFile = ASCFile.ReadFile(idfFilename, EnglishCultureInfo);
                idfFile = new IDFFile(ascFile);
            }

            if (settings.UseNodataCalculationValue)
            {
                if (settings.NoDataCalculationValue.Equals(float.NaN))
                {
                    idfFile.NoDataCalculationValue = idfFile.NoDataValue;
                    if (idfFile.NoDataValue.Equals(float.MaxValue))
                    {
                        idfFile.NoDataValue = float.MinValue;
                    }
                    else
                    {
                        idfFile.NoDataValue = float.MaxValue;
                    }
                }
                else
                {
                    idfFile.NoDataCalculationValue = settings.NoDataCalculationValue;
                }
            }

            return idfFile;
        }

        /// <summary>
        /// Merge specified new IDF-file with currently merged, resulting IDF-file 
        /// </summary>
        /// <param name="resultIDFFile"></param>
        /// <param name="countIDFFile"></param>
        /// <param name="idfFile"></param>
        /// <param name="settings"></param>
        protected virtual void MergeIDFFile(ref IDFFile resultIDFFile, ref IDFFile countIDFFile, IDFFile idfFile, SIFToolSettings settings)
        {
			if (!idfFile.Extent.Contains(resultIDFFile.Extent))
			{
				idfFile = idfFile.EnlargeIDF(resultIDFFile.Extent);
			}
			IDFFile countTempIDFFile = idfFile.IsNotEqual(idfFile.NoDataValue);
			countTempIDFFile.ReplaceValues(countTempIDFFile.NoDataValue, 0);
			countIDFFile += countTempIDFFile;

            switch (settings.StatFunction)
            {
                case StatFunction.Min:
                    resultIDFFile.ReplaceValues(idfFile.IsLesser(resultIDFFile), 1, idfFile);
					if(!settings.UseNodataCalculationValue && !settings.IgnoreNoDataValue)
					{
						resultIDFFile.ReplaceValues(idfFile.IsEqual(idfFile.NoDataValue), resultIDFFile.NoDataValue);
					}
                    break;
                case StatFunction.Max:
                    resultIDFFile.ReplaceValues(idfFile.IsGreater(resultIDFFile), 1, idfFile);
					if (!settings.UseNodataCalculationValue && !settings.IgnoreNoDataValue)
					{
						resultIDFFile.ReplaceValues(idfFile.IsEqual(idfFile.NoDataValue), resultIDFFile.NoDataValue);
					}
					break;
                case StatFunction.Mean:
				case StatFunction.Sum:
                    if (!settings.UseNodataCalculationValue && settings.IgnoreNoDataValue)
                    {
						// For sum, mean, ensure that NoData-values are ignored and do not result in a NoData-value
						idfFile.NoDataCalculationValue = 0;
                    }
                    resultIDFFile = resultIDFFile + idfFile;
                    break;
            }
		}

        protected virtual void WriteResults(string[] idfFilenames, IDFFile resultIDFFile, Metadata resultMetadata, IDFFile countIDFFile, SIFToolSettings settings)
        {
            // Use first filename in group if present as initial filename (for which optional group indices still apply)
            string currentResultIDFFilename = (idfFilenames.Length > 0) ? idfFilenames[0] : resultIDFFile.Filename;

            string outputFilename = GetOutputFilename(currentResultIDFFilename, settings, settings.WritePostfix ? ("_" + settings.StatFunction.ToString().ToLower()) : null);
			Log.AddInfo("Writing " + settings.StatFunction.ToString().ToLower() + " IDF-file to: " + Path.GetFileName(outputFilename) + " ...", 1);
			resultIDFFile.WriteFile(outputFilename, resultMetadata);

            if (settings.WriteCountIDFFile)
            {
				string countFilename = GetOutputFilename(currentResultIDFFilename, settings, "_count");
                Log.AddInfo("Writing count IDF-file to: " + Path.GetFileName(countFilename), 1);
				countIDFFile.WriteFile(countFilename);
            }
        }

        protected string GetOutputFilename(string initialFilename, SIFToolSettings settings, string postfix)
        {
            string outputFilename;
            if (settings.GroupIndices != null)
            {
                string substring = GetSubstring(Path.GetFileNameWithoutExtension(initialFilename), settings.GroupIndices);
				if (postfix != null)
				{
					outputFilename = Path.Combine(settings.OutputPath, substring + postfix + ".idf");
				}
				else
				{
					outputFilename = Path.Combine(settings.OutputPath, substring + ".idf");
				}
					
            }
            else
            {
                outputFilename = (settings.OutputFilename != null) ? settings.OutputFilename : SIFToolSettings.DefaultOutputFilename;
                if (postfix != null)
                {
                    outputFilename = FileUtils.AddFilePostFix(outputFilename, postfix);
                }

                outputFilename = Path.Combine(settings.OutputPath, outputFilename);
            }

            return outputFilename;
        }
    }
}
