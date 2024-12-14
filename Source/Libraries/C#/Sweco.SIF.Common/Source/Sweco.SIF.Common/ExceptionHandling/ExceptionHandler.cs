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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// Class for handling ToolExceptions or other Exceptions
    /// </summary>
    public class ExceptionHandler
    {
        /// <summary>
        /// String to use for indentation in exceptions, e.g. for tab use "\t"
        /// </summary>
        public static string IndentString = "  ";

        /// <summary>
        /// Handle ToolException by writing exception details to logfile or console
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="log"></param>
        public static void HandleToolException(Exception ex, Log log = null)
        {
            // Combine messages of inner ToolExceptions
            Exception innerException = ex;
            string messages = string.Empty;
            string tabs = string.Empty;
            Exception prevException = ex;
            while (innerException != null)
            {
                if (innerException is ToolException)
                {
                    if (!messages.Equals(string.Empty))
                    {
                        messages += "\r\n";
                    }
                    messages += tabs + innerException.Message;
                    tabs += IndentString;
                }
                prevException = innerException;
                innerException = innerException.InnerException;
            }
            if (messages.Equals(string.Empty))
            {
                // Use inner exception when no messages from ToolExceptions were found
                messages = ex.GetBaseException().Message;
            }

            if (log != null)
            {
                log.AddError(messages);
                log.AddInfo("Application is exiting...");
            }
            else
            {
                System.Console.WriteLine("ERROR: " + messages);
                System.Console.WriteLine("Application is exiting...");
            }
        }

        /// <summary>
        /// Handle Exception by writing exception details to logfile or console
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="log"></param>
        public static void HandleException(Exception ex, Log log = null)
        {
            if (log != null)
            {
                log.AddError(ex.GetBaseException().Message);
            }
            if ((log == null) || (log.Listeners.Count == 0))
            {
                System.Console.WriteLine("Unexpected toolerror: " + ex.GetBaseException().Message);
            }

            // Combine messages of innerexceptions
            bool hasToolException = ex is ToolException;
            Exception innerException = ex;
            string innerExceptionMessages = string.Empty;
            string tabs = string.Empty;
            Exception prevException = ex;
            while (innerException != null)
            {
                hasToolException = hasToolException || (innerException is ToolException);
                innerExceptionMessages += tabs + innerException.Message + "\r\n";
                tabs += IndentString;
                prevException = innerException;
                innerException = innerException.InnerException;
            }
            innerException = prevException;

            if (log != null)
            {
                log.AddInfo(innerExceptionMessages);
            }
            if ((log == null) || (log.Listeners.Count == 0))
            {
                System.Console.WriteLine(innerExceptionMessages);
            }

            if (!hasToolException)
            {
                // Only write stacktrace when no ToolExceptions are part of the stacktrace, these should provide enough information to find the error
                if (log != null)
                {
                    if ((innerException != ex) && !innerException.StackTrace.Equals(ex.StackTrace))
                    {
                        log.AddMessage(LogLevel.Info, innerException.StackTrace);
                    }
                    log.AddMessage(LogLevel.Info, ex.StackTrace);
                }
                if ((log == null) || (log.Listeners.Count == 0))
                {
                    if (innerException != ex)
                    {
                        System.Console.WriteLine(innerException.StackTrace);
                        System.Console.WriteLine();
                    }
                    System.Console.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Check if the Message property of any of the exceptions in the exception chain contains the specified text
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ExceptionChainContains(Exception ex, string text)
        {
            Exception innerEx = ex;
            while (innerEx != null)
            {
                if (innerEx.Message.Contains(text))
                {
                    return true;
                }
                innerEx = innerEx.InnerException;
            }
            return false;
        }

        /// <summary>
        /// Retrieve all messages from the exception chain as a string
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetExceptionChainString(Exception ex)
        {
            string msg = null;
            if (ex != null)
            {
                msg = ex.Message;
                Exception innerex = ex.InnerException;
                Exception prevEx = ex;
                string tabs = string.Empty;
                while (innerex != null)
                {
                    tabs += IndentString;
                    msg += "\r\n" + tabs + innerex.Message;
                    prevEx = innerex;
                    innerex = innerex.InnerException;
                }
            }
            return msg;
        }

        /// <summary>
        /// Retrieves the stacktrace from the exception that is lowest in the chain.
        /// If requested tabindentation is added before each line of the stacktrace
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="isTabIndented">true, adds tabindentation</param>
        /// <param name="indentLevel">start indentation level (number of initial tabs)</param>
        /// <returns></returns>
        public static string GetStacktraceString(Exception ex, bool isTabIndented = false, int indentLevel = 0)
        {
            string stacktraceString = null;
            if (ex != null)
            {
                Exception innerex = ex.InnerException;
                Exception prevEx = ex;
                string tabs = string.Empty;
                for (int i = 0; i < indentLevel; i++)
                {
                    tabs += IndentString;
                }
                while (innerex != null)
                {
                    tabs += IndentString;
                    prevEx = innerex;
                    innerex = innerex.InnerException;
                }
                stacktraceString = prevEx.StackTrace;
                if (isTabIndented)
                {
                    stacktraceString = tabs + stacktraceString.Replace("\r\n", "\r\n" + tabs);
                }

                return stacktraceString;
            }
            return stacktraceString;
        }
    }
}
