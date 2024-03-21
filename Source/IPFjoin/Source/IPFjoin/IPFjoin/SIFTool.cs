// IPFjoin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFjoin.
// 
// IPFjoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFjoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFjoin. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFjoin
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
            ToolPurpose = "SIF-tool for joining columnvalues of IPF-file(s) to columnvalues other IPF-file";
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

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (settings.TSDecimalCount != -1)
            {
                // Apply specified setting for decimal count of timeseries
                IPFTimeseries.DecimalCount = settings.TSDecimalCount;
            }
            if (settings.IgnoreTSDateErrors)
            {
                // Apply specified setting for ignoring date errors when reading timeseries
                IPFTimeseries.IsInvalidDateIgnored = true;
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );

            Log.AddInfo("Applying " + settings.JoinType.ToString() + "-join for points ...");
            Log.AddInfo("Applying " + settings.TSJoinType.ToString() + "-join for dates in timeseries ...");
            Log.AddInfo();

            // Retrieve file 2 and create datstructures for efficient handling
            Log.AddInfo("Reading file '" + Path.GetFileName(settings.JoinFilename) + "' ...");
            Log.AddInfo();

            JoinFile joinFile2 = ReadJoinFile(settings.JoinFilename);

            List<int> keyIndices2 = null;
            IDictionary<string, List<int>> keyDictionary2 = null;
            if (settings.KeyString2 != null)
            {
                keyIndices2 = RetrieveKeyIndices(joinFile2, settings.KeyString2);
                keyDictionary2 = RetrieveKeyDictionary(joinFile2, keyIndices2);
            }
            List<int> selectedColIndices2 = RetrieveSelectedIndices2(joinFile2, keyIndices2, settings);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                // Retrieve source IPF-file and create datstructures for efficient handling
                Log.AddInfo("Processing file '" + Path.GetFileName(inputFilename) + "' ...", 1);
                IPFFile ipfFile1 = IPFFile.ReadFile(inputFilename);
                JoinFile joinFile1 = CreateJoinFile(ipfFile1);

                List<int> keyIndices1 = null;
                IDictionary<string, List<int>> keyDictionary1 = null;
                if (settings.KeyString1 != null)
                {
                    keyIndices1 = RetrieveKeyIndices(joinFile1, settings.KeyString1);
                    keyDictionary1 = RetrieveKeyDictionary(joinFile1, keyIndices1);
                }
                else
                {
                    string keyString = string.Empty;

                    // Keys are defined automatically from common columns in file 1 and 2
                    foreach (string columnName1 in joinFile1.ColumnNames)
                    {
                        if ((joinFile1.FindColumnName(columnName1) >= 0) && (joinFile2.FindColumnName(columnName1) >= 0))
                        {
                            keyString += columnName1 + ",";
                        }
                    }
                    keyString = keyString.Substring(0, keyString.Length - 1);
                    keyIndices1 = RetrieveKeyIndices(joinFile1, keyString);
                    keyIndices2 = RetrieveKeyIndices(joinFile2, keyString);
                    keyDictionary1 = RetrieveKeyDictionary(joinFile1, keyIndices1);
                    keyDictionary2 = RetrieveKeyDictionary(joinFile2, keyIndices2);
                    Log.AddInfo("Automatically retrieved key(s) for file 1 and 2: " + keyString, 2);
                }

                string outputFilename = GetOutputFilename(inputFilename, inputFilenames, settings);
                IPFFile resultIPFFile = JoinIPFFile(ipfFile1, keyDictionary1, joinFile2, keyDictionary2, selectedColIndices2, outputFilename, settings);
                Log.AddInfo("Joining file 1 (" + ipfFile1.Points.Count + " rows) to file 2 (" + joinFile2.Rows.Count + " rows) resulted in " + resultIPFFile.Points.Count + " rows", 2);

                Log.AddInfo("Writing resulting IPF-file '" + Path.GetFileName(outputFilename) + "' ...", 2);
                resultIPFFile.WriteFile();

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Retrieves columns for file 2 that should be added to file 1
        /// </summary>
        /// <param name="joinFile2"></param>
        /// <param name="keyIndices2"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual List<int> RetrieveSelectedIndices2(JoinFile joinFile2, List<int> keyIndices2, SIFToolSettings settings)
        {
            List<int> selectedColIndices = new List<int>();

            for (int colIdx2 = 0; colIdx2 < joinFile2.ColumnNames.Count; colIdx2++)
            {
                // Add all columns from file 2 (except for natural joins where key-columns are skipped)
                if ((settings.JoinType != JoinType.Natural) || ((keyIndices2 != null) && !keyIndices2.Contains(colIdx2)))
                {
                    selectedColIndices.Add(colIdx2);
                }
            }

            return selectedColIndices;
        }

        protected virtual JoinFile CreateJoinFile(IPFFile ipfFile)
        {
            return new JoinFile(ipfFile);
        }

        protected virtual JoinFile ReadJoinFile(string filename)
        {
            return new JoinFile(filename);
        }

        /// <summary>
        /// Create a dictionary with unique keys that are mapped to lists of indices with all rows (in specified JoinFile) with the same key.
        /// Each key is a string with columnvalues, seperated by a semicolon ';', specified by the given keyIndices list.
        /// </summary>
        /// <param name="joinFile"></param>
        /// <param name="keyIndices"></param>
        /// <param name="isSorted">if true a SortedDictionary is used, resulting in sorted keys, but slower lookup speed</param>
        /// <returns></returns>
        protected IDictionary<string, List<int>> RetrieveKeyDictionary(JoinFile joinFile, List<int> keyIndices, bool isSorted = false)
        {
            if ((keyIndices == null) || (keyIndices.Count == 0))
            {
                throw new Exception("keyIndices cannot be empty when retrieving KeyDictionary");
            }

            IDictionary<string, List<int>> keyDictionary = isSorted ? (IDictionary<string, List<int>>) new SortedDictionary<string, List<int>>() : (IDictionary<string, List<int>>) (new Dictionary<string, List<int>>());
            for (int pointIdx = 0; pointIdx < joinFile.Rows.Count; pointIdx++)
            {
                List<string> columnValues = joinFile.Rows[pointIdx];

                string keyString = string.Empty;
                foreach (int keyIndex in keyIndices)
                {
                    if (keyIndex >= columnValues.Count)
                    {
                        throw new ToolException("Invalid key columnnumber " + (keyIndex + 1) + " for joinFile row " + ( pointIdx + 1) + " with " + columnValues.Count + " value(s): " + CommonUtils.ToString(columnValues));
                    }
                    keyString += columnValues[keyIndex] + ";";
                }
                keyString = keyString.Substring(0, keyString.Length - 1);
                
                if (!keyDictionary.ContainsKey(keyString))
                {
                    // Add empty list for this key
                    keyDictionary.Add(keyString, new List<int>());
                }

                // Add this point index to list of indices of this key
                keyDictionary[keyString].Add(pointIdx);
            }

            return keyDictionary;
        }

        /// <summary>
        /// Retrieve a list of column indices as defined by the specified string with comma-seperated column references (name or number)
        /// </summary>
        /// <param name="joinFile"></param>
        /// <param name="keyStringsString">string with comma-seperated key strings</param>
        /// <returns></returns>
        protected virtual List<int> RetrieveKeyIndices(JoinFile joinFile, string keyStringsString)
        {
            if ((keyStringsString == null) || keyStringsString.Equals(string.Empty))
            {
                throw new Exception("Join key cannot be empty");
            }
            List<int> keyIndices = new List<int>();

            string[] keyStrings = keyStringsString.Split(new char[] { ',' });
            foreach (string keyString in keyStrings)
            {
                int colIndex = joinFile.FindColumnIndex(keyString);
                if (colIndex == -1)
                {
                    throw new ToolException("Columnnumber or -name not defined for IPF-file '" + Path.GetFileName(joinFile.Filename) + "': " + keyString);
                }

                keyIndices.Add(colIndex);
            }

            return keyIndices;
        }

        /// <summary>
        /// Join columns of joinFile2 to columns of ipfFile1, including timeseries if present, as specified by settings. Points are matched by specified key dictionaries.
        /// </summary>
        /// <param name="ipfFile1"></param>
        /// <param name="keyDictionary1">list of key-values for each point in ipfFile1</param>
        /// <param name="joinFile2"></param>
        /// <param name="keyDictionary2">list of key-values for each point in joinFile2</param>
        /// <param name="selectedColIndices2"></param>
        /// <param name="outputIPFFilename"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected IPFFile JoinIPFFile(IPFFile ipfFile1, IDictionary<string, List<int>> keyDictionary1, JoinFile joinFile2, IDictionary<string, List<int>> keyDictionary2, List<int> selectedColIndices2, string outputIPFFilename, SIFToolSettings settings)
        {
            IPFFile resultIPFFile = new IPFFile();
            resultIPFFile.CopyProperties(ipfFile1);
            resultIPFFile.Filename = outputIPFFilename;

            // Add selected columns and create column mapping from IPF-file 2 to result IPF-file columns
            List<int> columnMapping2 = new List<int>();
            for (int colIdx2 = 0; colIdx2 < joinFile2.ColumnNames.Count; colIdx2++)
            {
                if (selectedColIndices2.Contains(colIdx2))
                {
                    columnMapping2.Add(resultIPFFile.ColumnCount);

                    string columnName2 = joinFile2.ColumnNames[colIdx2];
                    string corrColumnName2 = resultIPFFile.FindUniqueColumnName(columnName2);
                    resultIPFFile.AddColumn(corrColumnName2);
                }
            }

            // Create Copy of keyDictionary2 to use for Contains-check and which can be updated during process (for speed optimalization)
            List<string> keyDictionary2KeyList = keyDictionary2.Keys.ToList();

            // Walk through keys and points of IPF-file 1 and check for corresponding row in file 2
            foreach (string keyString in keyDictionary1.Keys)
            {
                List<int> pointIndices1 = keyDictionary1[keyString];

                // Loop through all source IPF-points that have this key
                foreach (int pointIndex1 in pointIndices1)
                {
                    IPFPoint ipfPoint1 = ipfFile1.Points[pointIndex1];

                    // Search matching rows in file 2
                    if (!keyString.Equals(string.Empty) && keyDictionary2KeyList.Contains(keyString))
                    {
                        // For each row in file 2 with this key, create a resulting IPF-point
                        List<int> pointIndices2 = keyDictionary2[keyString];
                        foreach (int pointIndex2 in pointIndices2)
                        {
                            // Key is present in file 2: now retrieve source IPF-point and copy as base for result IPF-file
                            IPFPoint resultIPFPoint = ipfPoint1.CopyIPFPoint();

                            // Ensure timeseries is loaded if present
                            if (resultIPFPoint.HasTimeseries() && !resultIPFPoint.IsTimeseriesLoaded())
                            {
                                resultIPFPoint.LoadTimeseries();
                            }

                            // Now, assign copied IPF-point to result IPF-file
                            resultIPFPoint.IPFFile = resultIPFFile;

                            // Add column values from file 2
                            JoinRow joinRow = joinFile2.Rows[pointIndex2];
                            for (int colIdx2 = 0; colIdx2 < joinRow.Count; colIdx2++)
                            {
                                if (selectedColIndices2.Contains(colIdx2))
                                {
                                    resultIPFPoint.ColumnValues.Add(joinRow[colIdx2]);
                                }
                            }

                            if (settings.IsTSJoined && joinRow.HasTimeseries)
                            {
                                IPFTimeseries ipfTimeseries1 = ipfPoint1.Timeseries;
                                Log.AddInfo("Joining TXT-files for point '" + keyString.Replace(';', ',') + "' ...", 2);
                                string tsRelSubdirPath = FileUtils.GetRelativePath(Path.GetDirectoryName(ipfTimeseries1.Filename), Path.GetDirectoryName(ipfFile1.Filename), false);
                                string resultFilename = Path.Combine(Path.GetDirectoryName(outputIPFFilename), Path.Combine(tsRelSubdirPath, Path.GetFileNameWithoutExtension(ipfTimeseries1.Filename) + ".TXT"));

                                JoinTimeseries(resultIPFPoint, ipfPoint1.Timeseries, joinRow.Timeseries, resultFilename, settings, Log);
                            }

                            resultIPFFile.AddPoint(resultIPFPoint);
                        }
                    }
                    else
                    {
                        // Key is empty or not found in file2
                        switch (settings.JoinType)
                        {
                            case JoinType.Natural:
                            case JoinType.Inner:
                            case JoinType.RightOuter:
                                // Skip unmatched point
                                break;
                            case JoinType.FullOuter:
                            case JoinType.LeftOuter:
                                // Add unmatched point with NULL-values for file 2
                                IPFPoint resultIPFPoint = ipfPoint1.CopyIPFPoint();

                                // Ensure timeseries is loaded if present
                                if (resultIPFPoint.HasTimeseries() && !resultIPFPoint.IsTimeseriesLoaded())
                                {
                                    resultIPFPoint.LoadTimeseries();
                                }

                                // Now, assign copied IPF-point to result IPF-file
                                resultIPFPoint.IPFFile = resultIPFFile;

                                // Add column values from file 2
                                for (int colIdx2 = 0; colIdx2 < joinFile2.ColumnNames.Count; colIdx2++)
                                {
                                    if (selectedColIndices2.Contains(colIdx2))
                                    {
                                        resultIPFPoint.ColumnValues.Add(string.Empty);
                                    }
                                }

                                resultIPFFile.AddPoint(resultIPFPoint);

                                break;
                            default:
                                throw new Exception("Unknown Join-type: " + settings.JoinType);
                        }
                    }
                }

                // Remove key from dictionary 2 list to speed-up search, but also to keep track of unmatched rows
                keyDictionary2KeyList.Remove(keyString);
            }

            switch (settings.JoinType)
            {
                case JoinType.FullOuter:
                case JoinType.RightOuter:
                    // For full or right outer join also add remaing points from file 2
                    foreach (string keyString2 in keyDictionary2KeyList)
                    {
                        List<int> pointIndices2 = keyDictionary2[keyString2];

                        // Loop through all source IPF-points that have this key
                        foreach (int pointIndex2 in pointIndices2)
                        {
                            IPFPoint ipfPoint2 = ipfFile1.Points[pointIndex2];
                            List<string> columnValues = new List<string>() { ipfPoint2.XString, ipfPoint2.YString };
                            for (int colIdx1 = 2; colIdx1 < ipfFile1.ColumnCount; colIdx1++)
                            {
                                columnValues.Add("");
                            }
                            for (int colIdx2 = 0; colIdx2 < joinFile2.ColumnNames.Count; colIdx2++)
                            {
                                if (selectedColIndices2.Contains(colIdx2))
                                {
                                    columnValues.Add(ipfPoint2.ColumnValues[colIdx2]);
                                }
                            }
                            IPFPoint resultingIPFPoint = new IPFPoint(resultIPFFile, ipfPoint2, columnValues);
                            // Note: optional timeseries data from IPF-file 2 is ignored
                            resultIPFFile.AddPoint(resultingIPFPoint);
                        }
                    }
                    break;
                default:
                    // ignore 
                    break;
            }
            return resultIPFFile;
        }

        /// <summary>
        /// Retrieve outputfilename as defined by settings. IF an OutputFilename is defined in settings it is used, otherwise the input filename will be used.
        /// For the output path the relative path from the input file is concatenated to the OutputPath as specified in settings.
        /// </summary>
        /// <param name="inputFilename"></param>
        /// <param name="inputFilenames"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual string GetOutputFilename(string inputFilename, string[] inputFilenames, SIFToolSettings settings)
        {
            // Retrieve path for input filename relatieve to specified global input path in settings
            string relativeInputFilename = FileUtils.GetRelativePath(inputFilename, settings.InputPath);

            string outputFilename = null;
            if (settings.OutputFilename != null)
            {
                // An outputfilename is specified, which is only valid when a single inputfilename is specified
                if (inputFilenames.Length > 1)
                {
                    throw new ToolException("An outputfilename cannot be specified when input filter results in multiple inputfiles");
                }
                else
                {
                    outputFilename = settings.OutputFilename;
                }
            }
            else
            {
                // Use same filename as input filename
                outputFilename = Path.GetFileName(inputFilename);
            }

            outputFilename = Path.Combine(Path.Combine(settings.OutputPath, Path.GetDirectoryName(relativeInputFilename)), outputFilename);

            return outputFilename;
        }

        /// <summary>
        /// Join IPFTimeseries2 to IPFTimeseries1. Assume both textfiles are sorted ascending on the first column
        /// </summary>
        /// <param name="ipfPoint"></param>
        /// <param name="ipfTimeseries1"></param>
        /// <param name="ipfTimeseries2"></param>
        /// <param name="resultTSFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        protected void JoinTimeseries(IPFPoint ipfPoint, IPFTimeseries ipfTimeseries1, IPFTimeseries ipfTimeseries2, string resultTSFilename, SIFToolSettings settings, Log log)
        {
            int dateTS1Idx = 0;

            IPFTimeseries selIPFtimeseries1 = RetrieveSelectedTimeseries(JoinOperand.Left, ipfTimeseries1, settings);
            IPFTimeseries selIPFtimeseries2 = RetrieveSelectedTimeseries(JoinOperand.Right, ipfTimeseries2, settings);

            try
            {
                // Create column definitions for result timeseries
                List<string> resultColumnNames = new List<string>();
                List<float> resultNoDataValues = new List<float>();

                // Retrieve resulting timeseries columns
                for (int colIdx = 0; colIdx < selIPFtimeseries1.ColumnNames.Count; colIdx++)
                {
                    resultColumnNames.Add(selIPFtimeseries1.ColumnNames[colIdx]);
                    resultNoDataValues.Add(selIPFtimeseries1.NoDataValues[colIdx]);
                }
                for (int colIdx = 0; colIdx < selIPFtimeseries2.ColumnNames.Count; colIdx++)  // skip first (date) column
                {
                    string columnName = selIPFtimeseries2.ColumnNames[colIdx];
                    if (resultColumnNames.Contains(columnName))
                    {
                        columnName += "2";
                    }

                    resultColumnNames.Add(columnName);
                    resultNoDataValues.Add(selIPFtimeseries2.NoDataValues[colIdx]);
                }

                // Initialize resulting timeseries
                List<DateTime> dateList1 = selIPFtimeseries1.Timestamps;
                List<DateTime> dateList2 = selIPFtimeseries2.Timestamps;
                List<DateTime> resultDateList = new List<DateTime>();
                List<List<float>> resultValueLists = new List<List<float>>();
                for (int colIdx = 0; colIdx < resultColumnNames.Count; colIdx++)
                {
                    resultValueLists.Add(new List<float>());
                }
                float noDataValue1 = selIPFtimeseries1.NoDataValue;
                float noDataValue2 = selIPFtimeseries2.NoDataValue;
                IPFTimeseries resultTimeseries = new IPFTimeseries(resultDateList, resultValueLists, resultColumnNames, resultNoDataValues);

                // Retrieve first date from timeseries2 to start with
                int dateTS2Idx = 0;
                int prevDateTS2Idx = 0;
                DateTime? dateTS2 = null;
                if (dateTS2Idx < dateList2.Count)
                {
                    dateTS2 = dateList2[dateTS2Idx];
                }

                // Loop through dates of timeseries1 and try to find a matching date in timeseries2
                int dateCount = 0;
                DateTime? dateTS1 = null;
                for (dateTS1Idx = 0; dateTS1Idx < dateList1.Count; dateTS1Idx++)
                {
                    dateTS1 = dateList1[dateTS1Idx];
                    if (((settings.TSPeriodStartDate == null) || (dateTS1 >= settings.TSPeriodStartDate)) && ((settings.TSPeriodEndDate == null) || (dateTS1 <= settings.TSPeriodEndDate)))
                    {
                        // try to find corresponding dateTS2 (equal to or later than dateTS1)
                        while ((dateTS2Idx < dateList2.Count) && (dateTS2 < dateTS1))
                        {
                            if (((settings.TSPeriodStartDate == null) || (dateTS2 >= settings.TSPeriodStartDate)) && ((settings.TSPeriodEndDate == null) || (dateTS2 <= settings.TSPeriodEndDate)))
                            {
                                if ((settings.TSJoinType == JoinType.FullOuter) || (settings.TSJoinType == JoinType.RightOuter))
                                {
                                    // This date from TS2 doesn't match a date from TS1, so write only the TS2-date and value (and use NoData for TS1-value)
                                    AddTimeseriesDate(resultTimeseries, (DateTime)dateTS2, selIPFtimeseries1, -1, selIPFtimeseries2, prevDateTS2Idx, dateTS2Idx, settings);
                                    dateCount++;
                                }
                            }

                            prevDateTS2Idx = dateTS2Idx;
                            dateTS2Idx++;
                            if (dateTS2Idx < dateList2.Count)
                            {
                                dateTS2 = dateList2[dateTS2Idx];
                            }
                        }

                        if ((dateTS2Idx < dateList2.Count) && (dateTS2.Equals(dateTS1)))
                        {
                            // date from TS2 is equal to date from TS1, no interpolation is necessary. Always add date, for all JoinTypes
                            AddTimeseriesDate(resultTimeseries, (DateTime)dateTS2, selIPFtimeseries1, dateTS1Idx, selIPFtimeseries2, prevDateTS2Idx, dateTS2Idx, settings);
                            dateCount++;
                            prevDateTS2Idx = dateTS2Idx;
                            dateTS2Idx++;
                            if (dateTS2Idx < dateList2.Count)
                            {
                                dateTS2 = dateList2[dateTS2Idx];
                            }
                        }
                        else if (dateTS2Idx < dateList2.Count)
                        {
                            if ((settings.TSJoinType == JoinType.FullOuter) || (settings.TSJoinType == JoinType.LeftOuter))
                            {
                                // date from TS2 is later than date from TS1
                                if (settings.IsTS2Interpolated)
                                {
                                    // Interpolate inbetween dates from TS2
                                    AddTimeseriesDate(resultTimeseries, (DateTime)dateTS1, selIPFtimeseries1, dateTS1Idx, selIPFtimeseries2, prevDateTS2Idx, dateTS2Idx, settings);
                                    dateCount++;
                                }
                                else
                                {
                                    // Copy TS1-date and value, use NoData for TS2
                                    // WriteLogMessage("\tDate " + basisDateStr + " not found in timeseries of IPF-file #2: skipped");
                                    AddTimeseriesDate(resultTimeseries, (DateTime)dateTS1, selIPFtimeseries1, dateTS1Idx, selIPFtimeseries2, -1, -1, settings);
                                    dateCount++;
                                }
                            }
                        }
                        else
                        {
                            // corresponding date not found in ipf2 and interpolation is not possible
                            if ((settings.TSJoinType == JoinType.FullOuter) || (settings.TSJoinType == JoinType.LeftOuter))
                            {
                                // Copy TS1-date and value, use NoData for TS2
                                AddTimeseriesDate(resultTimeseries, (DateTime)dateTS1, selIPFtimeseries1, dateTS1Idx, selIPFtimeseries2, -1, -1, settings);
                                dateCount++;
                            }
                        }
                    }
                }

                if ((settings.TSJoinType == JoinType.FullOuter) || (settings.TSJoinType == JoinType.RightOuter))
                {
                    // Write remaining dates from timeseries2
                    while ((dateTS2Idx < ipfTimeseries2.Timestamps.Count))
                    {
                        // This date from TS2 doesn't match a date from TS1, so write only the TS2-date (TS1-value will be NoData)
                        prevDateTS2Idx = dateTS2Idx;
                        dateTS2Idx++;
                        if (dateTS2Idx < dateList2.Count)
                        {
                            dateTS2 = dateList2[dateTS2Idx];
                            if (((settings.TSPeriodStartDate == null) || (dateTS2 >= settings.TSPeriodStartDate)) && ((settings.TSPeriodEndDate == null) || (dateTS2 <= settings.TSPeriodEndDate)))
                            {
                                AddTimeseriesDate(resultTimeseries, (DateTime)dateTS2, selIPFtimeseries1, -1, selIPFtimeseries2, prevDateTS2Idx, dateTS2Idx, settings);
                                dateCount++;
                            }
                        }
                    }
                }

                resultTimeseries.Filename = resultTSFilename;
                ipfPoint.Timeseries = resultTimeseries;
            }
            catch (Exception ex)
            {
                if (dateTS1Idx < selIPFtimeseries1.Timestamps.Count)
                {
                    throw new Exception("Unexpected error while joining date " + selIPFtimeseries1.Timestamps[dateTS1Idx], ex);
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Retrieve selected part of timeseries according to tool settings
        /// </summary>
        /// <param name="joinOperand"></param>
        /// <param name="ipfTimeseries"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IPFTimeseries RetrieveSelectedTimeseries(JoinOperand joinOperand, IPFTimeseries ipfTimeseries, SIFToolSettings settings)
        {
            IPFTimeseries selIPFTimeseries = ipfTimeseries.Select(settings.TSPeriodStartDate, settings.TSPeriodEndDate, -1, true);

            return selIPFTimeseries;
        }

        /// <summary>
        /// Convert a list of indices to numbers by increasing each index with one
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        protected List<int> IndicesToNumbers(List<int> indices)
        {
            List<int> colNrs = new List<int>();
            foreach (int idx in indices)
            {
                colNrs.Add(idx + 1);
            }
            return colNrs;
        }

        /// <summary>
        /// Add specified resultdate with values from both timeseries. If indices for previous and/or later dates of timeseries2 are specified, 
        /// inbetween dates are added and if specified these values are interpolated.
        /// </summary>
        /// <param name="resultTimeseries"></param>
        /// <param name="resultDate"></param>
        /// <param name="timeseries1"></param>
        /// <param name="dateTS1Idx"></param>
        /// <param name="timeseries2"></param>
        /// <param name="prevDateTS2Idx">index of previously added date from TS2; use -1 to ignore and not add inbetween dates</param>
        /// <param name="nextDateTS2Idx">index of next added date from TS2; use -1 to ignore and not add inbetween dates</param>
        /// <param name="settings"></param>
        protected void AddTimeseriesDate(IPFTimeseries resultTimeseries, DateTime resultDate, Timeseries timeseries1, int dateTS1Idx, Timeseries timeseries2, int prevDateTS2Idx, int nextDateTS2Idx, SIFToolSettings settings)   // Statistics textFileResStatistics, Statistics textFileVal1Statistics, Statistics textFileVal2Statistics, Statistics totalStatistics
        {
            resultTimeseries.Timestamps.Add(resultDate);

            // Add base columnvalues
            for (int colIdx = 0; colIdx < timeseries1.ValueColumns.Count; colIdx++)
            {
                if (dateTS1Idx >= 0)
                {
                    resultTimeseries.ValueColumns[colIdx].Add(timeseries1.ValueColumns[colIdx][dateTS1Idx]);
                }
                else
                {
                    resultTimeseries.ValueColumns[colIdx].Add(resultTimeseries.NoDataValues[colIdx]);
                }
            }

            DateTime? prevDateTS2 = ((prevDateTS2Idx >= 0) && (prevDateTS2Idx < timeseries2.Timestamps.Count)) ? timeseries2.Timestamps[prevDateTS2Idx] : (DateTime?)null;
            DateTime? nextDateTS2 = ((nextDateTS2Idx >= 0) && (nextDateTS2Idx < timeseries2.Timestamps.Count)) ? timeseries2.Timestamps[nextDateTS2Idx] : (DateTime?)null;

            // Add joined columnvalues
            if (nextDateTS2 != null)
            {
                for (int colIdx = 0; colIdx < timeseries2.ValueColumns.Count; colIdx++)
                {
                    float resultValue = float.NaN;

                    float prevValueTS2 = timeseries2.ValueColumns[colIdx][prevDateTS2Idx];
                    float nextValueTS2 = timeseries2.ValueColumns[colIdx][nextDateTS2Idx];
                    if (resultDate.Equals(nextDateTS2))
                    {
                        if (!nextValueTS2.Equals(timeseries2.NoDataValues[colIdx]))
                        {
                            resultValue = nextValueTS2;
                        }
                        else
                        {
                            resultValue = timeseries2.NoDataValues[colIdx];
                        }
                    }
                    else if (prevValueTS2.Equals(timeseries2.NoDataValues[colIdx]) || nextValueTS2.Equals(timeseries2.NoDataValues[colIdx]))
                    {
                        // No interpolation possible
                        resultValue = timeseries2.NoDataValues[colIdx];
                    }
                    else
                    {
                        // Interpolate joined columnvalues
                        try
                        {
                            if (settings.TSMaxInterpolationDistance.Equals(float.NaN) || (((DateTime)nextDateTS2).Subtract((DateTime)prevDateTS2).TotalDays < settings.TSMaxInterpolationDistance))
                            {
                                long prevDateTicks = ((DateTime)prevDateTS2).Ticks;
                                long nextDateTicks = ((DateTime)nextDateTS2).Ticks;
                                long dateTicks = resultDate.Ticks;

                                resultValue = prevValueTS2 + (nextValueTS2 - prevValueTS2) * (dateTicks - prevDateTicks) / (nextDateTicks - prevDateTicks);
                                if (resultValue.Equals(float.NaN))
                                {
                                    throw new Exception("Interpolation error: NaN result");
                                }
                            }
                            else
                            {
                                // No interpolation possible, interpolation distance is too large
                                resultValue = timeseries2.NoDataValues[colIdx];
                            }
                        }
                        catch
                        {
                            // no number (column might contain non-value strings): copy previous value
                            resultValue = timeseries2.ValueColumns[colIdx][prevDateTS2Idx];
                        }
                    }
                    resultTimeseries.ValueColumns[timeseries1.ValueColumns.Count + colIdx].Add(resultValue);
                }
            }
            else
            {
                for (int colIdx = 0; colIdx < timeseries2.ValueColumns.Count; colIdx++)
                {
                    resultTimeseries.ValueColumns[timeseries1.ValueColumns.Count + colIdx].Add(timeseries2.NoDataValues[colIdx]);
                }
            }
        }
    }

    /// <summary>
    /// Indicator of join operand, which is left for ipf1 and right for the file that joined to ipf1
    /// </summary>
    public enum JoinOperand
    {
        Left,
        Right
    }
}
