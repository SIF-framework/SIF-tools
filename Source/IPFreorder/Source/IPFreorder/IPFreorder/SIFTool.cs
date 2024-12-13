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
using Sweco.SIF.GIS;
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

            if ((settings.SourceColumnReferences == null) || (settings.SourceColumnReferences.Length == 0))
            {
                Log.AddInfo("No column definitions specified, copying all source columns ...");
            }

            // Retrieve specified files in input path
            string searchPattern = Path.GetFileName(Path.Combine(settings.InputPath, settings.InputFilter));
            string[] filenames = Directory.GetFiles(inputPath, searchPattern, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            Log.AddInfo(filenames.Length + " file(s) found under input path '" + inputPath);
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
        protected virtual void ReorderIPFFile(string sourceIPFFilename, string targetIPFFilename, SIFToolSettings settings)
        {
            IPFFile sourceIPFFile = null;
            IPFFile targetIPFFile = null;

            string[] sourceColumnStrings = settings.SourceColumnReferences;
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


                Log.AddInfo("Reading IPF-file " + FileUtils.GetRelativePath(sourceIPFFilename, settings.InputPath) + " ...");
                bool isAssociatedRefRead = (settings.AssociatedFileColumnRef == null) || !settings.AssociatedFileColumnRef.Equals("0"); // settings.IsTimeseriesSkipped || 
                sourceIPFFile = ReadIPFFile(sourceIPFFilename, isAssociatedRefRead, settings);

                Log.AddInfo("Reordering IPF-file ...");
                targetIPFFile = ReorderIPFFile(sourceIPFFile, targetIPFFilename, settings);

                if (settings.TimeseriesPath != null)
                {
                    Log.AddInfo("Replacing path to associated textfile(s) in IPF-file ...", 1);
                    ResetTimeseriesPath(targetIPFFile, settings.TimeseriesPath);
                }

                Log.AddInfo("Writing target IPF-file: " + Path.GetFileName(targetIPFFilename) + " ...", 1);
                targetIPFFile.WriteFile(targetIPFFilename, null, !settings.IsTimeseriesSkipped);
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
        /// Read IPF-file with specified settings
        /// </summary>
        /// <param name="sourceIPFFilename"></param>
        /// <param name="isAssociatedRefRead"></param>
        /// <param name="basicSettings"></param>
        /// <returns></returns>
        protected virtual IPFFile ReadIPFFile(string sourceIPFFilename, bool isAssociatedRefRead, SIFToolSettings settings)
        {
            return IPFFile.ReadFile(sourceIPFFilename, false, isAssociatedRefRead);
        }

        protected virtual void ResetTimeseriesPath(IPFFile targetIPFFile, string timeseriesPath)
        {
            foreach (IPFPoint ipfPoint in targetIPFFile.Points)
            {
                if (ipfPoint.HasTimeseries())
                {
                    if (!ipfPoint.IsTimeseriesLoaded())
                    {
                        ipfPoint.LoadTimeseries();
                    }
                    ipfPoint.ColumnValues[ipfPoint.IPFFile.AssociatedFileColIdx] = Path.Combine(timeseriesPath, Path.GetFileNameWithoutExtension(ipfPoint.Timeseries.Filename));
                }
            }
        }

        protected virtual IPFFile ReorderIPFFile(IPFFile sourceIPFFile, string targetIPFFilename, SIFToolSettings settings)
        {
            IPFFile targetIPFFile = new IPFFile();
            List<string> sourceColumnReferenceList = (settings.SourceColumnReferences != null) ? settings.SourceColumnReferences.ToList() : new List<string>();
            List<string> targetColumnNameList = (settings.TargetColumnNames != null) ? settings.TargetColumnNames.ToList() : new List<string>();
            List<string> columnExpressionList = (settings.TargetColumnExpressions != null) ? settings.TargetColumnExpressions.ToList() : new List<string>();
            List<string> removedColumnNameList = (settings.RemovedColumnNames != null) ? settings.RemovedColumnNames.ToList() : new List<string>();
            ExpandRemainderExpression(sourceIPFFile, ref sourceColumnReferenceList, ref targetColumnNameList, ref columnExpressionList, ref removedColumnNameList);

            List<int> columnNumbers = RetrieveColumnNumbers(sourceIPFFile, sourceColumnReferenceList);

            // For now leave filename at source filename, to be able to determine later if the IPF-timeseries has to be written.
            targetIPFFile.Filename = sourceIPFFile.Filename;
            targetIPFFile.AssociatedFileExtension = sourceIPFFile.AssociatedFileExtension;

            // Create target path if not yet existing
            if (!Directory.Exists(Path.GetDirectoryName(targetIPFFilename)))
            {
                Log.AddInfo("Creating target directory: " + Path.GetDirectoryName(targetIPFFilename) + " ...", 1);
                Directory.CreateDirectory(Path.GetDirectoryName(targetIPFFilename));
            }

            if ((columnNumbers == null) || (columnNumbers.Count == 0))
            {
                // No reorder arguments where specified, copy all columns from source IPF-file
                targetColumnNameList = new List<string>();
                columnNumbers = new List<int>();
                columnExpressionList = new List<string>();
                for (int colIdx = 0; colIdx < sourceIPFFile.ColumnCount; colIdx++)
                {
                    columnNumbers.Add(colIdx + 1);
                    targetColumnNameList.Add(null);
                    columnExpressionList.Add(null);
                    removedColumnNameList.Add(null);
                }
            }

            // Retrieve column names for new IPF-file
            targetIPFFile.ColumnNames = new List<string>(targetColumnNameList.Count());
            int colDefIdx = 0;
            while (colDefIdx < targetColumnNameList.Count)
            {
                if (targetColumnNameList[colDefIdx] != null)
                {
                    if (removedColumnNameList[colDefIdx] != null)
                    {
                        // When a removed column name is also specified, a rename is specified: rename column(s) in current column definition with specified name
                        string renamedColName = removedColumnNameList[colDefIdx];
                        if (int.TryParse(renamedColName, out int colNr))
                        {
                            if (colNr > 0)
                            {
                                // Start from left and use value as column number
                                renamedColName = targetIPFFile.ColumnNames[colNr - 1];
                            }
                            else if (colNr <= 0)
                            {
                                // Start from right and use value as column index
                                renamedColName = targetIPFFile.ColumnNames[targetIPFFile.ColumnCount - colNr - 1]; 
                            }
                        }

                        for (int renColIdx = 0; renColIdx < colDefIdx; renColIdx++)
                        {
                            if (targetIPFFile.ColumnNames[renColIdx].Equals(renamedColName))
                            {
                                targetIPFFile.ColumnNames[renColIdx] = targetColumnNameList[colDefIdx];
                            }
                        }

                        // remove column definition from list and prevent that it is used for column value modification
                        sourceColumnReferenceList.RemoveAt(colDefIdx);
                        targetColumnNameList.RemoveAt(colDefIdx);
                        columnExpressionList.RemoveAt(colDefIdx);
                        removedColumnNameList.RemoveAt(colDefIdx);
                        columnNumbers.RemoveAt(colDefIdx);
                    }
                    else
                    {
                        // Use specified, new columnname
                        targetIPFFile.ColumnNames.Add(targetColumnNameList[colDefIdx]);
                        colDefIdx++;
                    }
                }
                else if (removedColumnNameList[colDefIdx] != null)
                {
                    // When no target column name is specified, a deletion is specified: delete column(s) in current column definition with specified name
                    string removedColName = removedColumnNameList[colDefIdx];
                    if (int.TryParse(removedColName, out int colNr))
                    {
                        if (colNr > 0)
                        {
                            // Start from left and use value as column number
                            removedColName = targetIPFFile.ColumnNames[colNr - 1];
                        }
                        else if (colNr <= 0)
                        {
                            // Start from right and use value as column index
                            removedColName = targetIPFFile.ColumnNames[targetIPFFile.ColumnCount - colNr - 1];
                        }
                    }

                    int delColIdx = 0;
                    while ((delColIdx < colDefIdx) && (delColIdx < targetIPFFile.ColumnCount))
                    {
                        if (targetIPFFile.ColumnNames[delColIdx].Equals(removedColName))
                        {
                            targetIPFFile.ColumnNames.RemoveAt(delColIdx);

                            sourceColumnReferenceList.RemoveAt(delColIdx);
                            targetColumnNameList.RemoveAt(delColIdx);
                            columnExpressionList.RemoveAt(delColIdx);
                            removedColumnNameList.RemoveAt(delColIdx);
                            columnNumbers.RemoveAt(delColIdx);
                            colDefIdx--;
                        }
                        else
                        {
                            delColIdx++;
                        }
                    }

                    // remove column definition from list and prevent that it is used for column value modification
                    sourceColumnReferenceList.RemoveAt(colDefIdx);
                    targetColumnNameList.RemoveAt(colDefIdx);
                    columnExpressionList.RemoveAt(colDefIdx);
                    removedColumnNameList.RemoveAt(colDefIdx);
                    columnNumbers.RemoveAt(colDefIdx);
                }
                else
                {
                    // Keep name of specified source column
                    if (((columnNumbers[colDefIdx] - 1) >= 0) && ((columnNumbers[colDefIdx] - 1) < sourceIPFFile.ColumnNames.Count()))
                    {
                        targetIPFFile.ColumnNames.Add(sourceIPFFile.ColumnNames[columnNumbers[colDefIdx] - 1]);
                    }
                    else
                    {
                        throw new ToolException("Specified columnnumber (" + columnNumbers[colDefIdx] + ") is not defined for input IPF-file " + Path.GetFileName(sourceIPFFile.Filename));
                    }

                    colDefIdx++;
                }
            }

            // Update text column idx 
            targetIPFFile.AssociatedFileColIdx = -1;
            if (settings.AssociatedFileColumnRef != null)
            {
                int idx = targetIPFFile.FindColumnIndex(settings.AssociatedFileColumnRef);
                if (idx < 0)
                {
                    // split option parameter string into comma seperated substrings
                    if (!int.TryParse(settings.AssociatedFileColumnRef, out int nr))
                    {
                        throw new ToolException("Invalid column reference for associated file: " + settings.AssociatedFileColumnRef);
                    }
                    idx = nr - 1;
                }

                // Set user specified column index for target file
                targetIPFFile.AssociatedFileColIdx = idx;
                if (targetIPFFile.AssociatedFileExtension == null)
                {
                    targetIPFFile.AssociatedFileExtension = IPFFile.DefaultAssociatedFileExtension;
                }
            }
            else
            {
                if (sourceIPFFile.AssociatedFileColIdx > 0)
                {
                    bool isAssociatedColumnFound = false;
                    for (int idx = 0; idx < columnNumbers.Count; idx++)
                    {
                        if (columnNumbers[idx] == sourceIPFFile.AssociatedFileColIdx + 1)
                        {
                            targetIPFFile.AssociatedFileColIdx = idx;
                            isAssociatedColumnFound = true;
                        }
                    }
                    if (!isAssociatedColumnFound)
                    {
                        // Leave target AssociatedFileColIdx -1
                        Log.AddWarning("Associated column number is reset to 0 since referenced column is not selected as result column: " + sourceIPFFile.ColumnNames[sourceIPFFile.AssociatedFileColIdx], 1);
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
                for (int idx = 0; idx < targetColumnNameList.Count; idx++)
                {
                    if (columnExpressionList[idx] != null)
                    {
                        string columnValue = EvaluateExpression(columnExpressionList[idx], sourceIPFFile, pointIdx, sourceColumnValues);
                        targetColumnValues.Add(columnValue);
                    }
                    else
                    {
                        // Add value of specified source columnindex
                        targetColumnValues.Add(sourceColumnValues[columnNumbers[idx] - 1]);
                    }
                }
                IPFPoint targetIPFPoint = new IPFPoint(targetIPFFile, ipfPoint, targetColumnValues);

                if (!settings.IsTimeseriesSkipped)
                {
                    if (targetIPFFile.AssociatedFileColIdx >= 0)
                    {
                        if (sourceIPFFile.AssociatedFileColIdx >= 0)
                        {
                            if (ipfPoint.HasTimeseries())
                            {
                                targetIPFPoint.Timeseries = ipfPoint.Timeseries;
                            }
                            else
                            {
                                Log.AddWarning("Removing TS-reference; TS-file not found for IPF-point " + ipfPoint.ToString() + ": " + targetIPFPoint.ColumnValues[targetIPFFile.AssociatedFileColIdx], 1);
                                targetIPFPoint.ColumnValues[targetIPFFile.AssociatedFileColIdx] = null;
                            }
                        }
                        else
                        {
                            if (!targetIPFPoint.HasTimeseries())
                            {
                                Log.AddWarning("Removing TS-reference; TS-file not found for IPF-point " + targetIPFPoint.ToString() + ": " + targetIPFPoint.ColumnValues[targetIPFFile.AssociatedFileColIdx], 1);
                                targetIPFPoint.ColumnValues[targetIPFFile.AssociatedFileColIdx] = null;
                            }
                        }
                    }
                }

                // Add point without checking for timeseries now
                targetIPFFile.AddPoint(targetIPFPoint);
            }

            return targetIPFFile;
        }

        /// <summary>
        /// Updates column definitions for remainder expression 'c+', with c a columnname or number
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="sourceColumnReferences"></param>
        /// <param name="targetColumnNames"></param>
        /// <param name="columnExpressions"></param>
        /// <param name="removedColumnNames"></param>
        protected virtual void ExpandRemainderExpression(IPFFile sourceIPFFile, ref List<string> sourceColumnReferences, ref List<string> targetColumnNames, ref List<string> columnExpressions, ref List<string> removedColumnNames)
        {
            if (sourceColumnReferences != null)
            {
                List<string> tmpSourceColumnReferenceList = new List<string>();
                List<string> tmpTargetColumnNamesList = new List<string>();
                List<string> tmpColumnExpressionsList = new List<string>();
                List<string> tmpRemovedColumnReferenceList = new List<string>();

                for (int colStringIdx = 0; colStringIdx < sourceColumnReferences.Count; colStringIdx++)
                {
                    string colString = sourceColumnReferences[colStringIdx];
                    if ((colString != null) && colString.EndsWith(SIFToolSettings.RemainderColumnPostfix))
                    {
                        // Add remainder of columns to column definitions
                        string baseColString = colString.Substring(0, colString.Length - 1);
                        int colIdx = ParseColRefStringIndex(sourceIPFFile, baseColString);

                        tmpSourceColumnReferenceList.Add((colIdx + 1).ToString());
                        tmpTargetColumnNamesList.Add(null);
                        tmpColumnExpressionsList.Add(null);
                        tmpRemovedColumnReferenceList.Add(null);

                        // Add remaining columns to source column strings
                        for (int colIdx2 = colIdx + 1; colIdx2 < sourceIPFFile.ColumnCount; colIdx2++)
                        {
                            tmpSourceColumnReferenceList.Add((colIdx2 + 1).ToString());
                            tmpTargetColumnNamesList.Add(null);
                            tmpColumnExpressionsList.Add(null);
                            tmpRemovedColumnReferenceList.Add(null);
                        }
                    }
                    else
                    {
                        // Copy column definition
                        tmpSourceColumnReferenceList.Add(sourceColumnReferences[colStringIdx]);
                        tmpTargetColumnNamesList.Add(targetColumnNames[colStringIdx]);
                        tmpColumnExpressionsList.Add(columnExpressions[colStringIdx]);
                        tmpRemovedColumnReferenceList.Add(removedColumnNames[colStringIdx]);
                    }
                }

                sourceColumnReferences = tmpSourceColumnReferenceList;
                targetColumnNames = tmpTargetColumnNamesList;
                columnExpressions = tmpColumnExpressionsList;
                removedColumnNames = tmpRemovedColumnReferenceList;
            }
        }

        /// <summary>
        /// Retrieve integer list with column numbers for specified string list with column names or numbers that define columns in sourceIPFFile
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="sourceColumnReferenceList"></param>
        /// <returns></returns>
        protected List<int> RetrieveColumnNumbers(IPFFile sourceIPFFile, List<string> sourceColumnReferenceList)
        {
            List<int> columnNumbers = null;
            if (sourceColumnReferenceList != null)
            {
                columnNumbers = new List<int>();
                for (int idx = 0; idx < sourceColumnReferenceList.Count; idx++)
                {
                    string colString = sourceColumnReferenceList[idx];
                    int colIdx = ParseColRefStringIndex(sourceIPFFile, colString);
                    if (colIdx >= 0)
                    {
                        columnNumbers.Add(colIdx + 1);
                    }
                    else
                    {
                        columnNumbers.Add(0);
                    }
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
            // Check for filename expression [n:A=B] or [n:-A]
            int idx = columnExpression.IndexOf("[n:");
            if (idx >= 0)
            {
                int idx2 = columnExpression.IndexOf("]", idx + 1);
                if (idx2 > 0)
                {
                    string expression = columnExpression.Substring(idx + 1, idx2 - idx - 1);
                    string subExp = expression.Substring(2);
                    string[] substrings = subExp.Split(new char[] { '=' });
                    if (substrings.Length == 2)
                    {
                        // Perse [n:A=B]

                        // Create result, first retrieve part before left bracket
                        string result = columnExpression.Substring(0, idx);
                        string filename = Path.GetFileNameWithoutExtension(sourceIPFFile.Filename);
                        int idx3 = filename.IndexOf(substrings[0], StringComparison.OrdinalIgnoreCase);
                        if (idx3 >= 0)
                        {
                            result += filename.Substring(0, idx3);
                            result += substrings[1] + filename.Substring(idx3 + substrings[0].Length);
                        }
                        else
                        {
                            result += filename;
                        }
                        // Add last part, after right bracket
                        result += columnExpression.Substring(idx2 + 1);
                        return result;
                    }
                    else if (subExp.StartsWith("-"))
                    {
                        // Parse [n:-A]: Retrieve part after A

                        // Create result, first retrieve part before left bracket
                        string result = columnExpression.Substring(0, idx);
                        string filename = Path.GetFileNameWithoutExtension(sourceIPFFile.Filename);
                        subExp = subExp.Substring(1);
                        int idx3 = filename.LastIndexOf(subExp, StringComparison.OrdinalIgnoreCase);
                        if (idx3 >= 0)
                        {
                            result += filename.Substring(idx3 + subExp.Length);
                        }
                        else
                        {
                            result += filename;
                        }
                        // Add last part, after right bracket
                        result += columnExpression.Substring(idx2 + 1);
                        return result;
                    }
                    else
                    {
                        throw new ToolException("Unknown filename expression: " + expression);
                    }
                }
                else
                {
                    throw new ToolException("Invalid filename expression, missing right ]-bracket: " + columnExpression);
                }
            }
            else
            {
                // return complete 'expression' string
                return columnExpression;
            }
        }

        /// <summary>
        /// Parse string that refers to a column by a column name or number. The (zero-based) column index of that column in the specified IPF-file is returned.
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="colString">if null, -1 is returned</param>
        /// <param name="isExceptionThrown">if true an exeception is thrown when an error occurs, otherwise -1 is returned for the column number</param>
        /// <returns>zero-based column index</returns>
        protected int ParseColRefStringIndex(IPFFile sourceIPFFile, string colString, bool isExceptionThrown = true)
        {
            int colIdx;
            if (colString != null)
            {
                if (int.TryParse(colString, out colIdx))
                {
                    colIdx--;
                }
                else
                {
                    // try finding columnname or parse numeric value as column number
                    colIdx = sourceIPFFile.FindColumnName(colString, true, false);
                }

                if ((colIdx < 0) || (colIdx >= sourceIPFFile.ColumnCount))
                {
                    if (isExceptionThrown)
                    {
                        throw new ToolException("Column reference string '" + colString + "' does not match existing columns in IPF-file: " + Path.GetFileName(sourceIPFFile.Filename));
                    }
                    else
                    {
                        colIdx = -1;
                    }
                }
            }
            else
            {
                colIdx = -1;
            }

            return colIdx;
        }
    }
}
