// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweco.SIF.Common
{
    /// Note: all messages will be written to the logfile. When a log message is written with AddMessage() the minimum level can be defined for showing that message. 

    /// <summary>
    /// LogLevel defines the type/level of logmessages that can be written by a tool and will be reported by some provider (logfile or listener (e.g. console)). 
    /// Each log request R (the point where an application reports/writes a logmessage) has an associated log level that gives an indication of the importance and urgency of the message.
    /// A log request is handled by a provider P (logfile and/or listener). For each provider the LogLevel can be defined which indicates the type of messages that are actually shown or saved to the logfile
    /// A log request with level R is enabled (or actually shown) for a Log provider with LogLevel P, if R >= P. 
    /// For example: Log.AddInfo("Some message") is only shown in the console, if the LogLevel of the Console provider (ConsoleLogLevel) is Info or Debug
    /// Levels are based on log4j: https://en.wikipedia.org/wiki/Log4j#Log4j_log_levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// The highest possible rank and is intended to turn off logging
        /// </summary>
        Off = 1000,

        /// <summary>
        /// Severe errors that cause premature termination. Expect these to be immediately visible on a status console.
        /// </summary>
        Fatal = 500,

        /// <summary>
        /// Non-fatal errors or unexpected conditions. Expect these to be immediately visible on a status console.
        /// </summary>
        Error = 400,

        /// <summary>
        /// Issues that could be or lead to errors or other runtime situations that are undesirable or unexpected, but not necessarily an error. Expect these to be immediately visible on a status console.
        /// </summary>
        Warning = 300,

        /// <summary>
        /// Interesting runtime events (startup/shutdown), but not necessary to know under normal circumstances. Expect these to be immediately visible on a console, so be conservative and keep to a minimum.
        /// </summary>
        Info = 200,

        /// <summary>
        /// Detailed information on the flow through the system. Expect these to be written to logs only. Generally speaking, most lines logged by an application should have level debug.
        /// </summary>
        Debug = 100,

        /// <summary>
        /// Most detailed information. Expect these to be written to logs only.
        /// </summary>
        Trace = 50,

        /// <summary>
        /// Includes all LogLevels. Used for showing all log messages.
        /// </summary>
        All = 0

    }

    /// <summary>
    /// Class for handling log messages
    /// </summary>
    public class Log
    {
        private const string ErrorPrefix = "ERROR: ";
        private const string WarningPrefix = "WARNING: ";

        /// <summary>
        /// String to use for indentation in logfiles, e.g. for tab use "\t"
        /// </summary>
        public static string IndentString = "  ";

        /// <summary>
        /// Filename of logfile
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// LogLevel for the logfile. Each log request with level R is compared with the LogLevel of the logfile L. It is send/written to the logfile if R >= L.
        /// </summary>
        public LogLevel FileLogLevel { get; set; }

        /// <summary>
        /// List of all warnings
        /// </summary>
        public List<LogMessage> Warnings { get; protected set; }

        /// <summary>
        /// List of all errors
        /// </summary>
        public List<LogMessage> Errors { get; protected set; }

        /// <summary>
        /// Listeners (providers) for log message requests
        /// </summary>
        public List<AddMessageDelegate> Listeners { get; set; }

        /// <summary>
        /// LogLevels for each of the added listeners/providers. Each log request with level R is compared with the LogLevel of the listener L. It is send to the listener if R >= L.
        /// </summary>
        public List<LogLevel> ListenerLogLevels { get; set; }

        /// <summary>
        /// Complete log message with the combination of all log messages as will be written to the logfile
        /// </summary>
        public StringBuilder LogString { get; protected set; }

        /// <summary>
        /// Part of logString that has not yet been written to file
        /// </summary>
        protected StringBuilder pendingLogStringBuilder;

        /// <summary>
        /// Specifies if log messages are sent to listeners or not
        /// </summary>
        protected bool isListenerActive;

        /// <summary>
        /// Defines if log should be appended to current logfile when writing log with WriteFile. The default is false, and will overwrite an existing logfile
        /// </summary>
        public bool IsWriteFileAppend { get; set; }

        /// <summary>
        /// Creates Log instance without listener. Default LogLevel for the logfile is 'Debug', meaning all log request with level 'Debug' or higher will be written to the logfile.
        /// </summary>
        public Log()
        {
            Listeners = new List<AddMessageDelegate>();
            ListenerLogLevels = new List<LogLevel>();
            isListenerActive = true;
            FileLogLevel = LogLevel.Info;
            LogString = new StringBuilder();
            pendingLogStringBuilder = new StringBuilder();
            Warnings = new List<LogMessage>();
            Errors = new List<LogMessage>();
            IsWriteFileAppend = false;
        }

        /// <summary>
        /// Creates Log instance with specified listener. All log requests will also be forwarded to this listener. 
        /// Default LogLevels are 'Debug' for the logfile and 'Info' for the listener, meaning all log request with level Info or higher will be written to the listener.
        /// </summary>
        /// <param name="listener"></param>
        public Log(AddMessageDelegate listener) : this()
        {
            AddListener(listener, LogLevel.Info);
        }

        /// <summary>
        /// Creates Log instance with specified listeners. All log requests will also be forwarded to these listeners. 
        /// Default LogLevels are 'Debug' for the logfile and 'Info' for the listeners, meaning all log request with level Info or higher will be written to the listeners.
        /// </summary>
        /// <param name="listeners"></param>
        public Log(List<AddMessageDelegate> listeners) : this()
        {
            AddListeners(listeners, LogLevel.Info);
        }

        /// <summary>
        /// Adds a listener for the specified LogLevel to this logfile instance
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="listenerLogLevel">level for which messages (equal or above to this level) are sent to the listener</param>
        public void AddListener(AddMessageDelegate listener, LogLevel listenerLogLevel)
        {
            Listeners.Add(listener);
            ListenerLogLevels.Add(listenerLogLevel);
        }

        /// <summary>
        /// Adds multiple listeners for the specified LogLevel to this logfile instance
        /// </summary>
        /// <param name="listeners"></param>
        /// <param name="listenerLogLevel">level for which messages (equal or above to this level) are sent to the listener</param>
        public void AddListeners(List<AddMessageDelegate> listeners, LogLevel listenerLogLevel)
        {
            Listeners.AddRange(listeners);
            foreach (AddMessageDelegate listener in listeners)
            {
                // For each listener use same loglevel
                ListenerLogLevels.Add(listenerLogLevel);
            }
        }

        /// <summary>
        /// Clear the lists with warnings and errors
        /// </summary>
        public void ResetCounts()
        {
            Warnings.Clear();
            Errors.Clear();
        }

        /// <summary>
        /// Add an error message request for a specific category and file
        /// </summary>
        /// <param name="category">some category string</param>
        /// <param name="filename">some filename</param>
        /// <param name="message">the error message</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        public void AddError(string category, string filename, string message, int indentLevel = 0)
        {
            Errors.Add(new LogMessage(category, filename, message));
            AddMessage(LogLevel.Error, ErrorPrefix + message, indentLevel);
        }

        /// <summary>
        /// request an error message request
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        public void AddError(string message, int indentLevel = 0)
        {
            Errors.Add(new LogMessage(message));
            AddMessage(LogLevel.Error, ErrorPrefix + message, indentLevel);
        }

        /// <summary>
        /// Add a warning message request for a specific category and file
        /// </summary>
        /// <param name="category">some category string</param>
        /// <param name="filename">some filename</param>
        /// <param name="message">the warning message</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        public void AddWarning(string category, string filename, string message, int indentLevel = 0)
        {
            Warnings.Add(new LogMessage(category, filename, message));
            AddMessage(LogLevel.Warning, WarningPrefix + message, indentLevel);
        }

        /// <summary>
        /// Add a warning message request
        /// </summary>
        /// <param name="message">the warning message</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        public void AddWarning(string message, int indentLevel = 0)
        {
            Warnings.Add(new LogMessage(message));
            AddMessage(LogLevel.Warning, WarningPrefix + message, indentLevel);
        }

        /// <summary>
        /// Add an info message request
        /// </summary>
        /// <param name="message">the info message, or null to write empty line</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        /// <param name="isEolAdded">true if an end-of-line should be added after the message</param>
        public void AddInfo(string message = null, int indentLevel = 0, bool isEolAdded = true)
        {
            if (message == null)
            {
                message = string.Empty;
            }
            AddMessage(LogLevel.Info, message, indentLevel, isEolAdded);
        }

        /// <summary>
        /// Add a message request with some specified LogLevel
        /// </summary>
        /// <param name="logLevel">the loglevel of this message</param>
        /// <param name="message">the error message</param>
        /// <param name="indentLevel">number of IndentStrings before message</param>
        /// <param name="isEolAdded">true if an end-of-line should be added after the message</param>
        public void AddMessage(LogLevel logLevel, string message, int indentLevel = 0, bool isEolAdded = true)
        {
            // Add indentation
            for (int i = 0; i < indentLevel; i++)
            {
                message = IndentString + message;
            }

            // Process log request for each of the providers

            // Process log request for the logfile
            if ((LogString != null) && (logLevel >= FileLogLevel))
            {
                if (isEolAdded)
                {
                    LogString.AppendLine(message);
                    pendingLogStringBuilder.AppendLine(message);
                }
                else
                {
                    LogString.Append(message);
                    pendingLogStringBuilder.Append(message);
                }
            }

            // Process log request for each of the listeners
            if (isListenerActive)
            {
                for (int listenerIdx = 0; listenerIdx < Listeners.Count; listenerIdx++)
                {
                    AddMessageDelegate listener = Listeners[listenerIdx];
                    LogLevel listenerLogLevel = ListenerLogLevels[listenerIdx];
                    if ((listener != null) && (logLevel >= listenerLogLevel))
                    {
                        listener(message, isEolAdded);
                    }
                }
            }
        }

        /// <summary>
        /// If Filename has been set, the part of the current logstring is flushed to it
        /// </summary>
        public void Flush()
        {
            if (Filename != null)
            {
                WriteLogFile(Filename, false);
            }
        }

        /// <summary>
        /// Delete file that has been specified with the Filename-property. If Filename is not defined (null), no file is deleted and no error is raised.
        /// </summary>
        public void DeleteLogFile()
        {
            if ((Filename != null) && File.Exists(Filename))
            {
                try
                {
                    File.Delete(Filename);
                }
                catch (Exception ex)
                {
                    throw new Exception("Logfile " + Filename + " could not be deleted", ex);
                }
            }
        }

        /// <summary>
        /// Append current log to file that has been specified with the Filename-property. If Filename is not defined (null), no log is written and no error is raised.
        /// Note: existing logfiles are always appended. Use DeleteLogFile method to delete an existing logfile.
        /// </summary>
        /// <param name="isWritingLogged">if true, the writing of the logfile itself is also logged</param>
        /// <param name="logIndentLevel"></param>
        public void WriteLogFile(bool isWritingLogged = true, int logIndentLevel = 0)
        {
            if (Filename != null)
            {
                WriteLogFile(Filename, isWritingLogged);
            }
        }

        /// <summary>
        /// Append current log to file that has been specified with the Filename-property. If Filename is not defined (null), no log is written and no error is raised.
        /// Note: existing logfiles are always appended. Use DeleteLogFile method to delete an existing logfile.
        /// </summary>
        /// <param name="logfilename">path to logfile</param>
        /// <param name="isWritingLogged">if true, the writing of the logfile itself is also logged</param>
        /// <param name="logIndentLevel"></param>
        public void WriteLogFile(string logfilename, bool isWritingLogged = true, int logIndentLevel = 0)
        {
            if (logfilename != null)
            {
                Stream stream = null;
                StreamWriter sw = null;
                try
                {
                    string directory = Path.GetDirectoryName(logfilename);
                    if ((directory != null) && !directory.Equals(string.Empty))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(logfilename)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(logfilename));
                        }
                    }
                    stream = File.Open(logfilename, IsWriteFileAppend ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
                    sw = new StreamWriter(stream);
                    sw.Write(pendingLogStringBuilder);
                    pendingLogStringBuilder = new StringBuilder();
                    if (isWritingLogged)
                    {
                        AddInfo("\r\nLogfile has been written to " + Path.GetFileName(logfilename), logIndentLevel);
                    }
                }
                catch (Exception ex)
                {
                    AddError("\r\nCould not write logfile to '" + logfilename + "': " + ex.Message, logIndentLevel);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Parse a LogLevel string into LogLevel enum
        /// </summary>
        /// <param name="logLevelString"></param>
        /// <returns></returns>
        public static LogLevel ParseLogLevelString(string logLevelString)
        {
            if (logLevelString.ToLower().Equals(LogLevel.Trace.ToString().ToLower()))
            {
                return LogLevel.Trace;
            }
            else if (logLevelString.ToLower().Equals(LogLevel.Debug.ToString().ToLower()))
            {
                return LogLevel.Debug;
            }
            else if (logLevelString.ToLower().Equals(LogLevel.Info.ToString().ToLower()))
            {
                return LogLevel.Info;
            }
            else if (logLevelString.ToLower().Equals(LogLevel.Warning.ToString().ToLower()))
            {
                return LogLevel.Warning;
            }
            else if (logLevelString.ToLower().Equals(LogLevel.Error.ToString().ToLower()))
            {
                return LogLevel.Error;
            }
            else if (logLevelString.ToLower().Equals(LogLevel.Off.ToString().ToLower()))
            {
                return LogLevel.Off;
            }
            else
            {
                return LogLevel.All;
            }
        }

        /// <summary>
        /// Create listener that will write log requests to the console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isEolAdded"></param>
        public static void ConsoleListener(string message, bool isEolAdded = true)
        {
            if (isEolAdded)
            {
                System.Console.WriteLine(message);
            }
            else
            {
                System.Console.Write(message);
            }
            Application.DoEvents();
        }

        /// <summary>
        /// Deactivate all listeners: new log messages will not be sent to listeners
        /// </summary>
        public void DeactivateListener()
        {
            isListenerActive = false;
        }

        /// <summary>
        /// Activate all listeners: new log messages will be sent to listeners
        /// </summary>
        public void ActivateListener()
        {
            isListenerActive = true;
        }


        /// <summary>
        /// Retrieve number of processed items between logmessages given specified percentage of total number of items to process
        /// </summary>
        /// <param name="count"></param>
        /// <param name="logMessagePercentage"></param>
        /// <returns></returns>
        public static int GetLogMessageFrequency(int count, int logMessagePercentage = 5)
        {
            int logSnapPointMessageFrequency = (int)(count * logMessagePercentage / 100);
            if (logSnapPointMessageFrequency > 100000)
            {
                return 100000;
            }
            if (logSnapPointMessageFrequency > 50000)
            {
                return 50000;
            }
            if (logSnapPointMessageFrequency > 25000)
            {
                return 25000;
            }
            if (logSnapPointMessageFrequency > 7500)
            {
                return 10000;
            }
            if (logSnapPointMessageFrequency > 2500)
            {
                return 5000;
            }
            if (logSnapPointMessageFrequency > 750)
            {
                return 1000;
            }
            if (logSnapPointMessageFrequency > 250)
            {
                return 500;
            }
            if (logSnapPointMessageFrequency > 75)
            {
                return 100;
            }
            else
            {
                return 50;
            }
        }
    }

    /// <summary>
    /// Delegate method for handling log messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="isEolAdded"></param>
    public delegate void AddMessageDelegate(string message, bool isEolAdded = true);

    /// <summary>
    /// Class for storing log messages
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// An optional category that this log message specific to
        /// </summary>
        public string Category;

        /// <summary>
        /// An optional filename that this log message is specific to
        /// </summary>
        public string Filename;

        /// <summary>
        /// The log message
        /// </summary>
        public string Message;

        /// <summary>
        /// Optional details related to the log message
        /// </summary>
        public string Details;

        /// <summary>
        /// Create a log message instance
        /// </summary>
        /// <param name="message"></param>
        public LogMessage(string message)
        {
            this.Category = null;
            this.Filename = null;
            this.Message = message;
            this.Details = null;
        }

        /// <summary>
        /// Create a log message instance for a specific category
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        public LogMessage(string category, string message)
        {
            this.Category = category;
            this.Filename = null;
            this.Message = message;
            this.Details = null;
        }

        /// <summary>
        /// Create a log message instance for a specific category and file
        /// </summary>
        /// <param name="category"></param>
        /// <param name="filename"></param>
        /// <param name="message"></param>
        /// <param name="details"></param>
        public LogMessage(string category, string filename, string message, string details = null)
        {
            this.Category = category;
            this.Filename = filename;
            this.Message = message;
            this.Details = details;
        }

    }
}
