// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for storing issues with IDF-files. A distinction is made in three levels: type, group ID and issuecode. There are three types: errors, warnings and (more general) information (Info). 
    /// The group ID identifies any group for which specific issues are stored by their issue code. A group can be anything that can be represented by a grid, i.e. e a single file, a model, all grids in a directory, etc.
    /// For each issue code, the related error values can be added to a list of values to calculate statistics for. Also general messages can be stored as handled by the Log class.
    /// </summary>
    public class IDFLog : Log
    {
        /// <summary>
        /// Base path and filename for log IDF-files, for IDF-files warnings/errors/info a corresponding postfix is added to this basefilename
        /// </summary>
        public string BaseFilename { get; set; }

        /// <summary>
        /// Base IDF-file for storing IDF-files issue locations. A copy of this file (and its extent and cellsize) is made for warnings/errors.
        /// </summary>
        public IDFFile BaseIDFFile { get; private set; }

        /// <summary>
        /// Total number of Infos
        /// </summary>
        public long InfoCount { get; private set; }

        /// <summary>
        /// Total number of warnings
        /// </summary>
        public long WarningCount { get; private set; }

        /// <summary>
        /// Total number of errors
        /// </summary>
        public long ErrorCount { get; private set; }

        /// <summary>
        /// Info statistics per group ID
        /// </summary>
        protected Dictionary<string, IDFLogStatistics> InfoStatDictionary { get; set; }

        /// <summary>
        /// Warning statistics per group ID
        /// </summary>
        protected Dictionary<string, IDFLogStatistics> WarningStatDictionary { get; set; }

        /// <summary>
        /// Error statistics per group ID
        /// </summary>
        protected Dictionary<string, IDFLogStatistics> ErrorStatDictionary { get; set; }

        /// <summary>
        /// Indicates if IDFLog contains info that has not been saved yet
        /// </summary>
        public bool HasUnsavedIDFFileInfos { get; private set; }

        /// <summary>
        /// Indicates if IDFLog contains warnings that have not been saved yet
        /// </summary>
        public bool HasUnsavedIDFFileWarnings { get; private set; }

        /// <summary>
        /// Indicates if IDFLog contains errors that have not been saved yet
        /// </summary>
        public bool HasUnsavedIDFFileErrors { get; private set; }

        /// <summary>
        /// Creates new IDFLog object for specified log object and base files
        /// </summary>
        /// <param name="log"></param>
        /// <param name="baseFilename"></param>
        /// <param name="baseIDFFile"></param>
        public IDFLog(Log log, string baseFilename, IDFFile baseIDFFile) : base()
        {
            Initialize(baseFilename, baseIDFFile);

            this.Errors = log.Errors;
            this.Warnings = log.Warnings;
            this.Listeners = log.Listeners;
            this.ListenerLogLevels = log.ListenerLogLevels;
            this.LogString = new StringBuilder(log.LogString.ToString());
        }

        /// <summary>
        /// Creates new IDFLog object for specified base files
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="baseIDFFile"></param>
        public IDFLog(string baseFilename, IDFFile baseIDFFile) : base()
        {
            Initialize(baseFilename, baseIDFFile);
        }

        /// <summary>
        /// Creates new IDFLog object for specified base files and listener
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="baseIDFFile"></param>
        /// <param name="listener"></param>
        public IDFLog(string baseFilename, IDFFile baseIDFFile, AddMessageDelegate listener) : base(listener)
        {
            Initialize(baseFilename, baseIDFFile);
        }

        /// <summary>
        /// Creates new IDFLog object for specified base files and listeners
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="baseIDFFile"></param>
        /// <param name="listeners"></param>
        public IDFLog(string baseFilename, IDFFile baseIDFFile, List<AddMessageDelegate> listeners) : base(listeners)
        {
            Initialize(baseFilename, baseIDFFile);
        }

        /// <summary>
        /// Reset log variables and statistics to initial values
        /// </summary>
        /// <param name="baseFilename"></param>
        /// <param name="baseIDFFile"></param>
        protected void Initialize(string baseFilename, IDFFile baseIDFFile)
        {
            this.BaseFilename = baseFilename;
            InfoStatDictionary = new Dictionary<string, IDFLogStatistics>();
            WarningStatDictionary = new Dictionary<string, IDFLogStatistics>();
            ErrorStatDictionary = new Dictionary<string, IDFLogStatistics>();
            HasUnsavedIDFFileInfos = false;
            HasUnsavedIDFFileWarnings = false;
            HasUnsavedIDFFileErrors = false;

            SetBaseIDFFile(baseIDFFile);
        }

        /// <summary>
        /// Define a new base IDF-file that is used to define grid properties for all IDF-files
        /// </summary>
        /// <param name="baseIDFFile"></param>
        public void SetBaseIDFFile(IDFFile baseIDFFile)
        {
            this.BaseIDFFile = baseIDFFile;
            if (baseIDFFile != null)
            {
                ClearLayerIDFFiles();
            }
        }

        /// <summary>
        /// Remove/reset all current warning and error IDF-files
        /// </summary>
        public void ClearLayerIDFFiles()
        {
            InfoStatDictionary.Clear();
            WarningStatDictionary.Clear();
            ErrorStatDictionary.Clear();
            HasUnsavedIDFFileInfos = false;
            HasUnsavedIDFFileWarnings = false;
            HasUnsavedIDFFileErrors = false;
        }

        /// <summary>
        /// Add an infomessage to log and infocode to (general) IDF-loggrid at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific info</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddInfo(string message, float x, float y, int infoCode, int indentLevel = 0)
        {
            AddInfo(message, x, y, infoCode, float.NaN, indentLevel);
        }

        /// <summary>
        /// Add an infomessage to log and infocode to (general) IDF-loggrid at specified xy-coordinates. 
        /// Also add the info value (used for creating statistics) that was related to the specified info code.
        /// </summary>
        /// <param name="message">a string message that describes the specific info</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="infoValue">infovalue that caused info and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddInfo(string message, float x, float y, int infoCode, float infoValue, int indentLevel = 0)
        {
            base.AddInfo(message, indentLevel);
            AddInfoValue(x, y, infoCode, infoValue);
        }

        /// <summary>
        /// Add an infomessage to log and infocode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific info</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the info occurred</param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddInfo(string message, float x, float y, string groupID, int infoCode, int indentLevel = 0)
        {
            AddInfo(message, x, y, groupID, infoCode, float.NaN, indentLevel);
        }

        /// <summary>
        /// Add an infomessage to log and infocode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// Also add the info value (used for creating statistics) that was related to the specified info code.
        /// </summary>
        /// <param name="message">a string message that describes the specific info</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the info occurred</param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="infoValue">infovalue that caused info and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddInfo(string message, float x, float y, string groupID, int infoCode, float infoValue, int indentLevel = 0)
        {
            base.AddInfo(message, indentLevel);
            AddInfoValue(x, y, groupID, infoCode, infoValue);
        }

        /// <summary>
        /// Add an infocode to (general) IDF-loggrid specified xy-coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        public void AddInfoValue(float x, float y, int infoCode)
        {
            AddInfoValue(x, y, infoCode, float.NaN);
        }

        /// <summary>
        /// Add an infocode and infovalue to (general) IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="infoValue">infovalue that caused info and which is used for statistics</param>
        public void AddInfoValue(float x, float y, int infoCode, float infoValue)
        {
            AddInfoValue(x, y, string.Empty, infoCode, infoValue);
        }

        /// <summary>
        /// Add an infocode to IDF-loggrid for specified group and xy-location 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the info occurred</param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        public void AddInfoValue(float x, float y, string groupID, int infoCode)
        {
            AddInfoValue(x, y, groupID, infoCode, float.NaN);
        }

        /// <summary>
        /// Add an infocode and infovalue to IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the info occurred</param>
        /// <param name="infoCode">an integer that identifies the specific info</param>
        /// <param name="infoValue">infovalue that caused info and which is used for statistics</param>
        public void AddInfoValue(float x, float y, string groupID, int infoCode, float infoValue)
        {
            InfoCount++;

            // Ensure that specified infoclass is present in info dictionary
            if (!InfoStatDictionary.ContainsKey(groupID))
            {
                string idfPath = Path.GetDirectoryName(BaseFilename);
                string filenamePrefix = Path.GetFileNameWithoutExtension(BaseFilename);
                string filename = Path.Combine(idfPath, filenamePrefix + "_infos" + (groupID.Equals(string.Empty) ? string.Empty : "_") + groupID + ".IDF");
                InfoStatDictionary.Add(groupID, new IDFLogStatistics(filename, BaseIDFFile));
            }

            // Retrieve statistics object for specified infoclass 
            IDFLogStatistics infoIDFFileStat = InfoStatDictionary[groupID];
            infoIDFFileStat.AddIssueCode(x, y, infoCode);
            HasUnsavedIDFFileInfos = true;

            // Ensure an entry is present in the counts-dictionary for specified infocode 
            if (!infoIDFFileStat.IssueCodeCountDictionary.ContainsKey(infoCode))
            {
                infoIDFFileStat.IssueCodeCountDictionary.Add(infoCode, 0);
            }
            // Update infocount for specified infoclass and infocode 
            infoIDFFileStat.IssueCodeCountDictionary[infoCode]++;

            if (!infoValue.Equals(float.NaN))
            {
                // Ensure an entry is present in the value-dictionary for specified infocode 
                if (!infoIDFFileStat.IssueCodeValuesDictionary.ContainsKey(infoCode))
                {
                    infoIDFFileStat.IssueCodeValuesDictionary.Add(infoCode, new List<float>());
                }
                // Add info value to statistics dictionary for this infoclass and infocode 
                infoIDFFileStat.IssueCodeValuesDictionary[infoCode].Add(infoValue);
            }
        }

        /// <summary>
        /// Add a warningmessage to log and warningcode to (general) IDF-loggrid at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific warning</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddWarning(string message, float x, float y, int warningCode, int indentLevel = 0)
        {
            AddWarning(message, x, y, warningCode, float.NaN, indentLevel);
        }

        /// <summary>
        /// Add a warningmessage to log and warningcode to (general) IDF-loggrid at specified xy-coordinates. 
        /// Also add the value (used for creating statistics) that was related to the specified warning code.
        /// </summary>
        /// <param name="message">a string message that describes the specific warning</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="warningValue">value that caused warning and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddWarning(string message, float x, float y, int warningCode, float warningValue, int indentLevel = 0)
        {
            base.AddWarning(message, indentLevel);
            AddWarningValue(x, y, warningCode, warningValue);
        }

        /// <summary>
        /// Add a warningmessage to log and warningcode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific warning</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the warning occurred</param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddWarning(string message, float x, float y, string groupID, int warningCode, int indentLevel = 0)
        {
            base.AddWarning(message, indentLevel);
            AddWarningValue(x, y, groupID, warningCode, float.NaN);
        }

        /// <summary>
        /// Add a warningmessage to log and warningcode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// Also add the value (used for creating statistics) that was related to the specified warning code.
        /// </summary>
        /// <param name="message">a string message that describes the specific warning</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the warning occurred</param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="warningValue">value that caused warning and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddWarning(string message, float x, float y, string groupID, int warningCode, float warningValue, int indentLevel = 0)
        {
            base.AddWarning(message, indentLevel);
            AddWarningValue(x, y, groupID, warningCode, indentLevel);
        }

        /// <summary>
        /// Add a warningcode to (general) IDF-loggrid specified xy-coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        public void AddWarningValue(float x, float y, int warningCode)
        {
            AddWarningValue(x, y, warningCode, float.NaN);
        }

        /// <summary>
        /// Add a warningcode and warningvalue to (general) IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="warningValue">value that caused warning and which is used for statistics</param>
        public void AddWarningValue(float x, float y, int warningCode, float warningValue)
        {
            AddWarningValue(x, y, string.Empty, warningCode, warningValue);
        }

        /// <summary>
        /// Add a warningcode to IDF-loggrid for specified group and xy-location 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the warning occurred</param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        public void AddWarningValue(float x, float y, string groupID, int warningCode)
        {
            AddWarningValue(x, y, groupID, warningCode, float.NaN);
        }

        /// <summary>
        /// Add a warningcode and warningvalue to IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the warning occurred</param>
        /// <param name="warningCode">an integer that identifies the specific warning</param>
        /// <param name="warningValue">value that caused warning and which is used for statistics</param>
        public void AddWarningValue(float x, float y, string groupID, int warningCode, float warningValue)
        {
            WarningCount++;

            if (!WarningStatDictionary.ContainsKey(groupID))
            {
                string idfPath = Path.GetDirectoryName(BaseFilename);
                string filenamePrefix = Path.GetFileNameWithoutExtension(BaseFilename);
                string filename = Path.Combine(idfPath, filenamePrefix + "_warnings" + (groupID.Equals(string.Empty) ? string.Empty : "_") + groupID + ".IDF");
                WarningStatDictionary.Add(groupID, new IDFLogStatistics(filename, BaseIDFFile));
            }

            IDFLogStatistics warningIDFFileStat = WarningStatDictionary[groupID];
            warningIDFFileStat.AddIssueCode(x, y, warningCode);
            HasUnsavedIDFFileWarnings = true;

            if (!warningIDFFileStat.IssueCodeCountDictionary.ContainsKey(warningCode))
            {
                warningIDFFileStat.IssueCodeCountDictionary.Add(warningCode, 0);
            }
            warningIDFFileStat.IssueCodeCountDictionary[warningCode]++;
            if (!warningValue.Equals(float.NaN))
            {
                if (!warningIDFFileStat.IssueCodeValuesDictionary.ContainsKey(warningCode))
                {
                    warningIDFFileStat.IssueCodeValuesDictionary.Add(warningCode, new List<float>());
                }
                warningIDFFileStat.IssueCodeValuesDictionary[warningCode].Add(warningValue);
            }
        }

        /// <summary>
        /// Add an errormessage to log and errorcode to (general) IDF-loggrid at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific error</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddError(string message, float x, float y, int errorCode, int indentLevel = 0)
        {
            AddError(message, x, y, errorCode, float.NaN, indentLevel);
        }

        /// <summary>
        /// Add an errormessage to log and errorcode to (general) IDF-loggrid at specified xy-coordinates. 
        /// Also add the error value (used for creating statistics) that was related to the specified error code.
        /// </summary>
        /// <param name="message">a string message that describes the specific error</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="errorValue">errorvalue that caused error and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddError(string message, float x, float y, int errorCode, float errorValue, int indentLevel = 0)
        {
            base.AddError(message, indentLevel);
            AddErrorValue(x, y, errorCode, errorValue);
        }

        /// <summary>
        /// Add an errormessage to log and errorcode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// </summary>
        /// <param name="message">a string message that describes the specific error</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the error occurred</param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddError(string message, float x, float y, string groupID, int errorCode, int indentLevel = 0)
        {
            AddError(message, x, y, groupID, errorCode, float.NaN, indentLevel);
        }

        /// <summary>
        /// Add an errormessage to log and errorcode to IDF-loggrid of specified group at specified xy-coordinates. 
        /// Also add the error value (used for creating statistics) that was related to the specified error code.
        /// </summary>
        /// <param name="message">a string message that describes the specific error</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the error occurred</param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="errorValue">errorvalue that caused error and which is used for statistics</param>
        /// <param name="indentLevel">indent level for message</param>
        public void AddError(string message, float x, float y, string groupID, int errorCode, float errorValue, int indentLevel = 0)
        {
            base.AddError(message, indentLevel);
            AddErrorValue(x, y, groupID, errorCode, errorValue);
        }

        /// <summary>
        /// Add an errorcode to (general) IDF-loggrid specified xy-coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        public void AddErrorValue(float x, float y, int errorCode)
        {
            AddErrorValue(x, y, errorCode, float.NaN);
        }

        /// <summary>
        /// Add an errorcode and errorvalue to (general) IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="errorValue">errorvalue that caused error and which is used for statistics</param>
        public void AddErrorValue(float x, float y, int errorCode, float errorValue)
        {
            AddErrorValue(x, y, string.Empty, errorCode, errorValue);
        }

        /// <summary>
        /// Add an errorcode to IDF-loggrid for specified group and xy-location 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the error occurred</param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        public void AddErrorValue(float x, float y, string groupID, int errorCode)
        {
            AddErrorValue(x, y, groupID, errorCode, float.NaN);
        }

        /// <summary>
        /// Add an errorcode and errorvalue to IDF-loggrid for specified group and xy-location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="groupID">a string that identifies the item(s) for which the error occurred</param>
        /// <param name="errorCode">an integer that identifies the specific error</param>
        /// <param name="errorValue">errorvalue that caused error and which is used for statistics</param>
        public void AddErrorValue(float x, float y, string groupID, int errorCode, float errorValue)
        {
            ErrorCount++;

            // Ensure that specified errorclass is present in error dictionary
            if (!ErrorStatDictionary.ContainsKey(groupID))
            {
                string idfPath = Path.GetDirectoryName(BaseFilename);
                string filenamePrefix = Path.GetFileNameWithoutExtension(BaseFilename);
                string filename = Path.Combine(idfPath, filenamePrefix + "_errors" + (groupID.Equals(string.Empty) ? string.Empty : "_") + groupID + ".IDF");
                ErrorStatDictionary.Add(groupID, new IDFLogStatistics(filename, BaseIDFFile));
            }

            // Retrieve statistics object for specified errorclass 
            IDFLogStatistics errorIDFFileStat = ErrorStatDictionary[groupID];
            errorIDFFileStat.AddIssueCode(x, y, errorCode);
            HasUnsavedIDFFileErrors = true;

            // Ensure an entry is present in the counts-dictionary for specified errorcode 
            if (!errorIDFFileStat.IssueCodeCountDictionary.ContainsKey(errorCode))
            {
                errorIDFFileStat.IssueCodeCountDictionary.Add(errorCode, 0);
            }
            // Update errorcount for specified errorclass and errorcode 
            errorIDFFileStat.IssueCodeCountDictionary[errorCode]++;

            if (!errorValue.Equals(float.NaN))
            {
                // Ensure an entry is present in the value-dictionary for specified errorcode 
                if (!errorIDFFileStat.IssueCodeValuesDictionary.ContainsKey(errorCode))
                {
                    errorIDFFileStat.IssueCodeValuesDictionary.Add(errorCode, new List<float>());
                }
                // Add error value to statistics dictionary for this errorclass and errorcode 
                errorIDFFileStat.IssueCodeValuesDictionary[errorCode].Add(errorValue);
            }
        }

        /// <summary>
        /// Write message-logfile to filename as defined by BaseFilename
        /// </summary>
        /// <param name="isWritingLogged">if true, the writing of the logfile itself is also logged</param>
        public void WriteLogFile(bool isWritingLogged)
        {
            base.WriteLogFile(BaseFilename, isWritingLogged);
        }

        /// <summary>
        /// Write both message-and IDF-logfiles to filenames based on currently defined BaseFilename
        /// </summary>
        /// <param name="isWritingLogged">if true, the writing of the logfile itself is also logged</param>
        /// <param name="indentLevel"></param>
        public void WriteLogFiles(bool isWritingLogged = true, int indentLevel = 0)
        {
            base.WriteLogFile(BaseFilename, isWritingLogged, indentLevel);
            WriteLogIDFFiles(isWritingLogged, indentLevel);
        }

        /// <summary>
        /// Delete all files in specified path and add message for this to logfile
        /// </summary>
        /// <param name="idfPath"></param>
        /// <param name="indentLevel"></param>
        public void DeleteLogIDFPath(string idfPath, int indentLevel = 0)
        {
            AddInfo("Deleting IDF-logfiles from " + idfPath, indentLevel);
            FileUtils.DeleteDirectory(idfPath, true);
        }

        /// <summary>
        /// Write IDF-logfiles to filenames based on currently defined BaseFilename
        /// </summary>
        /// <param name="isWritingLogged">if true, the writing of the logfile itself is also logged</param>
        /// <param name="indentLevel"></param>
        public void WriteLogIDFFiles(bool isWritingLogged = true, int indentLevel = 0)
        {
            try
            {
                string idfPath = Path.GetDirectoryName(BaseFilename);
                if (isWritingLogged)
                {
                    AddInfo("Writing log layer IDF-files ...", indentLevel);
                }

                foreach (string infoGroupID in WarningStatDictionary.Keys)
                {
                    IDFFile warningLayerIDFFile = WarningStatDictionary[infoGroupID].IssueIDFFile;
                    if (warningLayerIDFFile.HasValueLargerThan(0))
                    {
                        warningLayerIDFFile.WriteFile();
                    }
                }
                HasUnsavedIDFFileWarnings = false;

                foreach (string warningFilePostFix in WarningStatDictionary.Keys)
                {
                    IDFFile warningGroupID = WarningStatDictionary[warningFilePostFix].IssueIDFFile;
                    if (warningGroupID.HasValueLargerThan(0))
                    {
                        warningGroupID.WriteFile();
                    }
                }
                HasUnsavedIDFFileWarnings = false;

                foreach (string errorGroupID in ErrorStatDictionary.Keys)
                {
                    IDFFile errorLayerIDFFile = ErrorStatDictionary[errorGroupID].IssueIDFFile;
                    if (errorLayerIDFFile.HasDataValues())
                    {
                        errorLayerIDFFile.WriteFile();
                    }
                }
                HasUnsavedIDFFileErrors = false;
            }
            catch (Exception ex)
            {
                AddError("Could not write log layer IDF-files: " + ex.Message, indentLevel);
            }
        }

        /// <summary>
        /// Retrieve list of info groupID strings that were added to this IDFLog
        /// </summary>
        /// <returns></returns>
        public List<string> GetInfoGroupIDs()
        {
            return InfoStatDictionary.Keys.ToList();
        }

        /// <summary>
        /// Retrieve list of warning groupID strings that were added to this IDFLog
        /// </summary>
        /// <returns></returns>
        public List<string> GetWarningGroupIDs()
        {
            return WarningStatDictionary.Keys.ToList();
        }

        /// <summary>
        /// Retrieve list of error groupID strings that were added to this IDFLog
        /// </summary>
        /// <returns></returns>
        public List<string> GetErrorGroupIDs()
        {
            return ErrorStatDictionary.Keys.ToList();
        }

        /// <summary>
        /// Retrieve IDFLogStatistics for specified info groupID string
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns>null if groupID is not known</returns>
        public IDFLogStatistics GetInfoStatistics(string groupID)
        {
            if (InfoStatDictionary.ContainsKey(groupID))
            {
                return InfoStatDictionary[groupID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve IDFLogStatistics for specified warning groupID string
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns>null if groupID is not known</returns>
        public IDFLogStatistics GetWarningStatistics(string groupID)
        {
            if (WarningStatDictionary.ContainsKey(groupID))
            {
                return WarningStatDictionary[groupID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve IDFLogStatistics for specified error groupID string
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns>null if groupID is not known</returns>
        public IDFLogStatistics GetErrorStatistics(string groupID)
        {
            if (ErrorStatDictionary.ContainsKey(groupID))
            {
                return ErrorStatDictionary[groupID];
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Companion class for IDFLog which stored statistics for IDFLog-grids
    /// </summary>
    public class IDFLogStatistics
    {
        /// <summary>
        /// Filename of IDF-file that is used for storing issuecodes
        /// </summary>
        public string IDFFilename { get; protected set; }

        /// <summary>
        /// IDF-file that is used for storing (summed) issuecodes at xy-locations
        /// </summary>
        public IDFFile IssueIDFFile { get; protected set; }

        /// <summary>
        /// Dictionary with counts per issuecode
        /// </summary>
        internal Dictionary<int, long> IssueCodeCountDictionary { get; set; }

        /// <summary>
        /// Dictionary with summed values per issuecode
        /// </summary>
        internal Dictionary<int, List<float>> IssueCodeValuesDictionary { get; set; }

        /// <summary>
        /// Create new IDFLogStatistics object
        /// </summary>
        /// <param name="issueIDFFilename">filename used for storing issue grid</param>
        /// <param name="baseIDFFile">Base IDF-file that is copied for storing (summed) issue codes</param>
        public IDFLogStatistics(string issueIDFFilename, IDFFile baseIDFFile)
        {
            this.IDFFilename = issueIDFFilename;

            IssueIDFFile = (IDFFile)baseIDFFile.Copy(issueIDFFilename);
            IssueIDFFile.SetValues(0);

            IssueCodeCountDictionary = new Dictionary<int, long>();
            IssueCodeValuesDictionary = new Dictionary<int, List<float>>();
        }

        /// <summary>
        /// Add issue code to issue grid 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="issueCode"></param>
        public void AddIssueCode(float x, float y, int issueCode)
        {
            IssueIDFFile.AddValue(x, y, issueCode);
        }

        /// <summary>
        /// Retrieves issue count for specified issue code
        /// </summary>
        /// <param name="issueCode"></param>
        /// <returns>number of issues that were added to log for specified issue code</returns>
        public long GetIssueCount(int issueCode)
        {
            if (IssueCodeCountDictionary.ContainsKey(issueCode))
            {
                return IssueCodeCountDictionary[issueCode];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Retrieves value list for specified issue code
        /// </summary>
        /// <param name="issueCode"></param>
        /// <returns>null if issueCode is not known</returns>
        public List<float> GetValueList(int issueCode)
        {
            if (IssueCodeValuesDictionary.ContainsKey(issueCode))
            {
                return IssueCodeValuesDictionary[issueCode];
            }
            else
            {
                return null;
            }
        }
    }
}
