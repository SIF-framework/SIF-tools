// IPFreorder is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFreorder.
// 
// IPFreorder is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFreorder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFreorder. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFreorder
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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for reordering, copying or adding columns from source IPF-file(s) based on simple column expressions";
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
            string outputPath = settings.OutputPath;

            if (!Directory.Exists(inputPath))
            {
                throw new ToolException("Inputdirectory doesn't exist: " + inputPath);
            }

            string outputFilename = null;
            if ((Path.GetExtension(outputPath) != null) && !Path.GetExtension(outputPath).Equals(string.Empty))
            {
                outputFilename = Path.GetFileName(outputPath);
                outputPath = Path.GetDirectoryName(outputPath);
            }

            if ((settings.SourceColumnStrings == null) || (settings.SourceColumnStrings.Length == 0))
            {
                Log.AddInfo("No column definitions specified, copying all source columns ...");
            }

            // Retrieve specified files in input path
            string searchPattern = Path.GetFileName(Path.Combine(settings.InputPath, settings.InputFilter));
            string[] filenames = Directory.GetFiles(inputPath, searchPattern, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            Log.AddInfo(filenames.Length + " files found under input path '" + inputPath);
            if (filenames.Length > 1)
            {
                if ((Path.GetExtension(settings.OutputPath) != null) && !Path.GetExtension(settings.OutputPath).Equals(string.Empty))
                {
                    // Output path has an extension, a single file as output is not accepted for wildcard input
                    throw new ToolException("For inputPath with wildcards, outputPath has to be a directoryname");
                }
            }

            if (!Directory.Exists(outputPath))
            {
                Log.AddInfo("Outputdirectory '" + outputPath  + "' is created ...", 1);
                Directory.CreateDirectory(outputPath);
            }

            // Process files
            for (int idx = 0; idx < filenames.Length; idx++)
            {
                string sourceIPFFilename = filenames[idx];
                // string currentInputPath = Path.GetDirectoryName(sourceIPFFilename);
                string subPath = Path.GetDirectoryName(FileUtils.GetRelativePath(sourceIPFFilename, inputPath));
                string currentOutputDirectoryName = outputPath;
                if ((subPath != null) && !subPath.Equals(string.Empty))
                {
                    currentOutputDirectoryName = Path.Combine(currentOutputDirectoryName, subPath);
                }
                string outputIPFFilename = Path.Combine(currentOutputDirectoryName, Path.GetFileName(filenames[idx]));
                if ((filenames.Length == 1) && (outputFilename != null))
                {
                    outputIPFFilename = Path.Combine(currentOutputDirectoryName, outputFilename);
                }
                ReorderIPFFile(sourceIPFFilename, outputIPFFilename, settings);
            }

            ToolSuccessMessage = "Finished reordering IPF-file(s)";

            return exitcode;
        }

        /// <summary>
        /// Reorder columns of given IPF-file based on specified settings
        /// </summary>
        /// <param name="sourceIPFFilename"></param>
        /// <param name="targetIPFFilename"></param>
        /// <param name="settings"></param>
        protected void ReorderIPFFile(string sourceIPFFilename, string targetIPFFilename, SIFToolSettings settings)
        {
            IPFFile sourceIPFFile = null;
            IPFFile targetIPFFile = null;

            string[] sourceColumnStrings = settings.SourceColumnStrings;
            string[] targetColumnNames = settings.TargetColumnNames;
            string[] columnExpressions = settings.TargetColumnExpressions;

            try
            {
                if ((targetColumnNames != null) && ((columnExpressions != null) && (targetColumnNames.Length != columnExpressions.Length))
                    || ((sourceColumnStrings != null) && (targetColumnNames.Length != sourceColumnStrings.Length)))
                {
                    throw new ToolException("Unequal lengths for columnNames-, columnExpressions- and columnIndices-array: " + targetColumnNames.Length + "," + columnExpressions.Length + "," + sourceColumnStrings.Length);
                }

                if (!File.Exists(sourceIPFFilename))
                {
                    throw new ToolException("IPF-file doesn't exist: " + sourceIPFFilename);
                }

                if (File.Exists(targetIPFFilename) && !settings.IsOverwrite)
                {
                    Log.AddWarning("Targetfile is existing, reordering is skipped for " + Path.GetFileName(sourceIPFFilename), 1);
                    return;
                }

                Log.AddInfo("Reordering IPF-file " + sourceIPFFilename);
                sourceIPFFile = IPFFile.ReadFile(sourceIPFFilename, false, true);
                int[] columnNumbers = RetrieveColumnNumbers(sourceIPFFile, sourceColumnStrings);
                targetIPFFile = new IPFFile();

                // For now leave filename at source filename, to be able to determine later if the IPF-timeseries has to be written.
                targetIPFFile.Filename = sourceIPFFilename;

                // Create target path if not yet existing
                if (!Directory.Exists(Path.GetDirectoryName(targetIPFFilename)))
                {
                    Log.AddInfo("Creating target directory: " + Path.GetDirectoryName(targetIPFFilename) + " ...", 1);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetIPFFilename));
                }

                if ((columnNumbers == null) || (columnNumbers.Length == 0))
                {
                    targetColumnNames = new string[sourceIPFFile.ColumnCount];
                    columnNumbers = new int[sourceIPFFile.ColumnCount];
                    columnExpressions = new string[sourceIPFFile.ColumnCount];
                    for (int colIdx = 0; colIdx < sourceIPFFile.ColumnCount; colIdx++)
                    {
                        columnNumbers[colIdx] = colIdx + 1;
                        targetColumnNames[colIdx] = null;
                        columnExpressions[colIdx] = null;
                    }
                }

                targetIPFFile.ColumnNames = new List<string>(targetColumnNames.Count());
                for (int idx = 0; idx < targetColumnNames.Length; idx++)
                {
                    targetIPFFile.ColumnNames.Add(string.Empty);
                    if (targetColumnNames[idx] != null)
                    {
                        targetIPFFile.ColumnNames[idx] = targetColumnNames[idx];
                    }
                    else
                    {
                        if (((columnNumbers[idx] - 1) >= 0) && ((columnNumbers[idx] - 1) < sourceIPFFile.ColumnNames.Count()))
                        {
                            targetIPFFile.ColumnNames[idx] = sourceIPFFile.ColumnNames[columnNumbers[idx] - 1];
                        }
                        else
                        {
                            throw new ToolException("Specified columnnumber (" + columnNumbers[idx] + ") is not defined for input IPF-file " + Path.GetFileName(sourceIPFFilename));
                        }
                    }
                }

                // Update text column idx 
                targetIPFFile.AssociatedFileColIdx = -1;
                if (settings.AssociatedFileColumnIndex >= 0)
                {
                    // Set user specified column index
                    targetIPFFile.AssociatedFileColIdx = settings.AssociatedFileColumnIndex - 1;
                }
                else
                {
                    if (sourceIPFFile.AssociatedFileColIdx > 0)
                    {
                        for (int idx = 0; idx < columnNumbers.Length; idx++)
                        {
                            if (columnNumbers[idx] == sourceIPFFile.AssociatedFileColIdx + 1)
                            {
                                targetIPFFile.AssociatedFileColIdx = idx;
                            }
                        }
                    }
                }

                // Process points/rows of IPF-file
                for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
                {
                    IPFPoint ipfPoint = sourceIPFFile.Points[pointIdx];
                    List<string> sourceColumnValues = ipfPoint.ColumnValues;
                    List<string> targetColumnValues = new List<string>();

                    // Retrieve defined values for target columns
                    for (int idx = 0; idx < targetColumnNames.Length; idx++)
                    {
                        if (columnExpressions[idx] != null)
                        {
                            string columnValue = EvaluateExpression(columnExpressions[idx], sourceIPFFile, pointIdx, sourceColumnValues);
                            targetColumnValues.Add(columnValue);
                        }
                        else
                        {
                            // Add value of specified source columnindex
                            targetColumnValues.Add(sourceColumnValues[columnNumbers[idx] - 1]);
                        }
                    }
                    IPFPoint targetIPFPoint = new IPFPoint(targetIPFFile, ipfPoint, targetColumnValues);
                    if (targetIPFFile.AssociatedFileColIdx >= 0)
                    {
                        targetIPFPoint.Timeseries = ipfPoint.Timeseries;
                    }
                    targetIPFFile.AddPoint(targetIPFPoint);
                }

                Log.AddInfo("Writing target IPF-file: " + Path.GetFileName(targetIPFFilename) + " ...", 1);
                targetIPFFile.WriteFile(targetIPFFilename);
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error reordering " + Path.GetFileName(sourceIPFFilename), ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reordering " + Path.GetFileName(sourceIPFFilename), ex);
            }
        }

        /// <summary>
        /// Retrieve integer array with column numbers for string array with column names or numbers
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="sourceColumnStrings"></param>
        /// <returns></returns>
        protected int[] RetrieveColumnNumbers(IPFFile sourceIPFFile, string[] sourceColumnStrings)
        {
            int[] columnNumbers = null;
            if (sourceColumnStrings != null)
            {
                columnNumbers = new int[sourceColumnStrings.Length];
                for (int idx = 0; idx < sourceColumnStrings.Length; idx++)
                {
                    string colString = sourceColumnStrings[idx];
                    int colNr = ParseColumnString(sourceIPFFile, colString);
                    columnNumbers[idx] = colNr;
                }
            }
            return columnNumbers;
        }

        /// <summary>
        /// Evaluate expression for string value of specified point/row in IPF-file
        /// </summary>
        /// <param name="columnExpression"></param>
        /// <param name="sourceIPFFile"></param>
        /// <param name="rowIdx"></param>
        /// <param name="columnValues"></param>
        /// <returns></returns>
        protected virtual string EvaluateExpression(string columnExpression, IPFFile sourceIPFFile, int rowIdx, List<string> columnValues)
        {
            // Currently only a single constant value is allowed, return complete 'expression' string
            return columnExpression;
        }

        /// <summary>
        /// Parse string that refers to a column by a column name or number. The (one-based) column number of that column in the specified IPF-file is returned.
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="colString">if null, -1 is returned</param>
        /// <param name="isExceptionThrown">if true an exeception is thrown when an error occurs, otherwise -1 is returned for the column number</param>
        /// <returns></returns>
        protected int ParseColumnString(IPFFile sourceIPFFile, string colString, bool isExceptionThrown = true)
        {
            int colNr;
            if (colString != null)
            {
                if (!int.TryParse(colString, out colNr))
                {
                    // try columnname
                    colNr = sourceIPFFile.FindColumnName(colString, true, false) + 1;
                }

                if ((colNr <= 0) || (colNr > sourceIPFFile.ColumnCount))
                {
                    if (isExceptionThrown)
                    {
                        throw new ToolException("Invalid column string '" + colString + "' does not match existing column in IPF-file: " + Path.GetFileName(sourceIPFFile.Filename));
                    }
                    else
                    {
                        colNr = -1;
                    }
                }
            }
            else
            {
                colNr = -1;
            }

            return colNr;
        }

    }
}
