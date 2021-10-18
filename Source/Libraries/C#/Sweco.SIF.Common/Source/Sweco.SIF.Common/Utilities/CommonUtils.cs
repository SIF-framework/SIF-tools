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
using Sweco.SIF.Common.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// SIF utilities for file processing
    /// </summary>
    public class CommonUtils
    {
        /// <summary>
        /// Copies stringlist (with stringvalues) 
        /// </summary>
        /// <param name="stringList"></param>
        /// <returns></returns>
        public static List<string> CopyStringList(List<string> stringList)
        {
            if (stringList == null)
            {
                return null;
            }

            List<string> newStringList = new List<string>();
            foreach (string stringValue in stringList)
            {
                newStringList.Add(stringValue);
            }
            return newStringList;
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<string> list, string listSeperator = ",")
        {
            string listString = string.Empty;
            if (list != null)
            {
                for (int colIdx = 0; colIdx < list.Count; colIdx++)
                {
                    listString += list[colIdx];
                    if (colIdx < list.Count - 1)
                    {
                        listString += listSeperator;
                    }
                }
            }
            return listString;
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <param name="format">format string, see ToString() help</param>
        /// <param name="formatProvider"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<float> list, string format = null, IFormatProvider formatProvider = null, string listSeperator = ",")
        {
            List<string> stringList = new List<string>();
            foreach (float value in list)
            {
                if (format == null)
                {
                    stringList.Add((formatProvider != null) ? value.ToString(formatProvider) : value.ToString());
                }
                else
                {
                    stringList.Add((formatProvider != null) ? value.ToString(format, formatProvider) : value.ToString(format));
                }
            }
            return ToString(stringList, listSeperator);
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <param name="format">format string, see ToString() help</param>
        /// <param name="formatProvider"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<double> list, string format = null, IFormatProvider formatProvider = null, string listSeperator = ",")
        {
            List<string> stringList = new List<string>();
            foreach (double value in list)
            {
                if (format == null)
                {
                    stringList.Add((formatProvider != null) ? value.ToString(formatProvider) : value.ToString());
                }
                else
                {
                    stringList.Add((formatProvider != null) ? value.ToString(format, formatProvider) : value.ToString(format));
                }
            }
            return ToString(stringList, listSeperator);
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <param name="formatProvider"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<int> list, IFormatProvider formatProvider = null, string listSeperator = ",")
        {
            List<string> stringList = new List<string>();
            foreach (int value in list)
            {
                stringList.Add((formatProvider != null) ? value.ToString(formatProvider) : value.ToString());
            }
            return ToString(stringList, listSeperator);
        }

        /// <summary>
        /// Writes list items to a comma seperated string
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString<T>(List<T> list, string listSeperator = ",")
        {
            List<string> stringList = new List<string>();
            foreach (T value in list)
            {
                stringList.Add(value.ToString());
            }
            return ToString(stringList, listSeperator);
        }

        /// <summary>
        /// Sorts strings with alphanumeric substrings correctly
        /// </summary>
        /// <param name="stringArray">an array of strings</param>
        public static void SortAlphanumericStrings(string[] stringArray)
        {
            Array.Sort(stringArray, new AlphanumComparator());
        }

        /// <summary>
        /// Sorts strings with alphanumeric substrings correctly
        /// </summary>
        /// <param name="stringList">a list of strings to sort</param>
        public static void SortAlphanumericStrings(List<string> stringList)
        {
            string[] stringArray = stringList.ToArray();
            Array.Sort(stringArray, new AlphanumComparator());
            stringList.Clear();
            stringList.AddRange(stringArray);
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        /// <summary>
        /// Splits command-line in From: https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/749653#749653
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        /// <summary>
        /// Ensures that the given string is surrounded with double quotes, if not yet present these are addded.
        /// </summary>
        /// <param name="someString"></param>
        /// <returns>same string with extra double quotes if not yet present, or null for null string</returns>
        public static string EnsureDoubleQuotes(string someString)
        {
            if (someString == null)
            {
                return null;
            }

            if (someString.Equals(string.Empty))
            {
                return "\"\"";
            }

            if (!someString.StartsWith("\""))
            {
                someString = "\"" + someString;
            }
            if (!someString.EndsWith("\""))
            {
                someString += "\"";
            }
            return someString;
        }

        /// <summary>
        /// Splits a given string with the specified listseperator and excludes the specified quote symbols from the split
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="listseperator"></param>
        /// <param name="quote">quote symbol, default is double quote symbol</param>
        /// <param name="isQuoteRemoved">true if quotes should be removed from the resulting substrings</param>
        /// <param name="isWhitespaceTrimmed">true if the resulting substrings should be trimmed for whitespace, default is false</param>
        /// <returns></returns>
        public static string[] SplitQuoted(string inputString, char listseperator, char quote = '\"', bool isQuoteRemoved = false, bool isWhitespaceTrimmed = false)
        {
            // for non-whitespace listseperators, from: http://stackoverflow.com/questions/3776458/split-a-comma-separated-string-with-both-quoted-and-unquoted-strings
            // for non-whitespace listseperators, from: http://stackoverflow.com/questions/554013/regular-expression-to-split-on-spaces-unless-in-quotes
            // for whitespace listseperators, from: http://stackoverflow.com/questions/4780728/regex-split-string-preserving-quotes/4780801#4780801

            string regExString = null;
            if (listseperator.Equals(' ') || listseperator.Equals('\t'))
            {
                // List separator is whitespace, see one or more consequtive listseparators as one
                // original regExStrings: "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*)[\\s]+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"; // "([^\\s]*\"[^\"]+\"[^\\s]*)|[^\"]?\\w+.[^\"]?"; // skipping at decimal points: "(?<=\")\\w[\\w\\s]*(?=\")|\\w+|\"[\\w\\s]*\""
                regExString = "(?<=^[^" + quote + "]*(?:" + quote + "[^" + quote + "]*" + quote + "[^" + quote + "]*)*)[\\s]+(?=(?:[^" + quote + "]*" + quote + "[^" + quote + "]*" + quote + ")*[^" + quote + "]*$)";
                string[] splitArray = Regex.Split(inputString, regExString);
                if (isQuoteRemoved)
                {
                    for (int i = 0; i < splitArray.Length; i++)
                    {
                        splitArray[i] = splitArray[i].Replace("\"", "");
                    }
                }
                if (isWhitespaceTrimmed)
                {
                    for (int i = 0; i < splitArray.Length; i++)
                    {
                        splitArray[i] = splitArray[i].Replace("\"", "");
                    }
                }
                return splitArray;
            }
            else
            {
                regExString = "(?:^|" + listseperator.ToString() + ")(" + quote + "(?:[^" + quote + "]+|" + quote + quote + ")*" + quote + "|[^" + listseperator.ToString() + "]*)";

                bool isWorkaroundUsed = false;
                if (inputString.StartsWith(listseperator.ToString()))
                {
                    // Add dummy entry instead of empty entry at beginning of inputString to avoid error with missed first list separator: the regex expression doesn't allow an empty first entry
                    inputString = quote.ToString() + quote.ToString() + inputString;
                    isWorkaroundUsed = true;
                }
                Regex csvSplit = new Regex(regExString, RegexOptions.Compiled);
                List<string> list = new List<string>();
                string curr = null;
                MatchCollection matches = csvSplit.Matches(inputString);
                foreach (Match match in csvSplit.Matches(inputString))
                {
                    curr = match.Value;
                    if (curr.Length == 0)
                    {
                        list.Add("");
                    }
                    else
                    {
                        curr = curr.TrimStart(listseperator);
                        if (isQuoteRemoved)
                        {
                            curr = curr.Replace("\"", "");
                        }
                        if (isWhitespaceTrimmed)
                        {
                            curr = curr.Trim();
                        }
                        list.Add(curr);
                    }
                }
                if (isWorkaroundUsed)
                {
                    list[0] = string.Empty;
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// Retrieve maximum of two integers
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static int Max(int value1, int value2)
        {
            return (value1 > value2) ? value1 : value2;
        }

        /// <summary>
        /// Retrieve maximum of two floats
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static float Max(float value1, float value2)
        {
            return (value1 > value2) ? value1 : value2;
        }

        /// <summary>
        /// Retrieve maximum of two longs
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static long Max(long value1, long value2)
        {
            return (value1 > value2) ? value1 : value2;
        }

        /// <summary>
        /// Retrieve minimum of two integers
        /// </summary>
        public static int Min(int value1, int value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        /// <summary>
        /// Retrieve minimum of two floats
        /// </summary>
        public static float Min(float value1, float value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        /// <summary>
        /// Retrieve minimum of two longs
        /// </summary>
        public static long Min(long value1, long value2)
        {
            return (value1 < value2) ? value1 : value2;
        }

        /// <summary>
        /// The icon as used for SIF-tool forms and executables
        /// </summary>
        public static System.Drawing.Icon SIFIcon
        {
            get { return (System.Drawing.Icon)Resources.ResourceManager.GetObject("SwecoIcon"); }
        }

        /// <summary>
        /// Retrieve indices of format items that are present in string for String.Format() method
        /// </summary>
        /// <param name="logString"></param>
        /// <returns>list of found indices or empty list when no format items were found</returns>
        public static List<int> GetFormatStringIndices(string logString)
        {
            List<int> indices = new List<int>();

            int charIdx = 0;
            while (charIdx < logString.Length)
            {
                if (logString[charIdx].Equals('{'))
                {
                    charIdx++;
                    string indexString = string.Empty;
                    while ((charIdx < logString.Length) && !logString[charIdx].Equals('}') && !logString[charIdx].Equals(',') && !logString[charIdx].Equals(':'))
                    {
                        // Parse format item and index
                        indexString += logString[charIdx++];
                    }
                    if (int.TryParse(indexString, out int index))
                    {
                        indices.Add(index);
                    }
                }
                else
                {
                    charIdx++;
                }
            }

            return indices;
        }

        /// <summary>
        /// Select items from list with specified indices
        /// </summary>
        /// <param name="items">list of items</param>
        /// <param name="indices">list of indices to select</param>
        /// <param name="isInverted">invert selection</param>
        /// <returns></returns>
        public static List<T> SelectItems<T>(List<T> items, List<int> indices, bool isInverted = false)
        {
            List<T> selectedItems = new List<T>();
            for (int idx = 0; idx < items.Count; idx++)
            {
                if ((!isInverted && indices.Contains(idx)) || (isInverted && !indices.Contains(idx)))
                {
                    selectedItems.Add(items[idx]);
                }
            }

            return selectedItems;
        }

        /// <summary>
        /// Returns the common left substring, or an empty string of there is no overlap
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static string GetCommonLeftSubstring(string str1, string str2)
        {
            int strIdx = 0;
            while ((strIdx < str1.Length) && (strIdx < str2.Length) && str1[strIdx].Equals(str2[strIdx]))
            {
                strIdx++;
            }
            return (strIdx > 0) ? str1.Substring(0, strIdx) : string.Empty;
        }

        /// <summary>
        /// Returns the common left complete substring parts, seperated by some symbol, or an empty string of there is no overlap
        /// e.g. "HBB_STAT_BASIS1_BAS" and "HBB_STAT_BASIS1_BAS" and seperatorsymbol '_' will give "HBB_STAT_"
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <param name="seperatorString"></param>
        /// <param name="isSeperatorAdded">if true the last matching seperatorstring is added to the resultstring as well</param>
        /// <returns></returns>
        public static string GetCommonLeftSubstringParts(string str1, string str2, string seperatorString, bool isSeperatorAdded = false)
        {
            string match = string.Empty;
            int prevSeperatorIdx = 0;
            int seperatorIdx = 0;
            while ((seperatorIdx >= 0) && (seperatorIdx < str2.Length) && str1.Substring(0, seperatorIdx).Equals(str2.Substring(0, seperatorIdx)))
            {
                prevSeperatorIdx = seperatorIdx;
                seperatorIdx = str1.IndexOf(seperatorString, seperatorIdx + 1);
            }
            if (prevSeperatorIdx > 0)
            {
                return (isSeperatorAdded) ? str1.Substring(0, prevSeperatorIdx) + seperatorString : str1.Substring(0, prevSeperatorIdx);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the longest common substring, or an empty string of there is no overlap
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static string LongestCommonSubstring(string str1, string str2)
        {
            // source: https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Longest_common_substring
            // see also: https://en.wikipedia.org/wiki/Longest_common_substring_problem

            string sequence = string.Empty;
            if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
                return string.Empty;

            int[,] num = new int[str1.Length, str2.Length];
            int maxlen = 0;
            int lastSubsBegin = 0;
            StringBuilder sequenceBuilder = new StringBuilder();

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                            int thisSubsBegin = i - num[i, j] + 1;
                            if (lastSubsBegin == thisSubsBegin)
                            {//if the current LCS is the same as the last time this block ran
                                sequenceBuilder.Append(str1[i]);
                            }
                            else //this block resets the string builder if a different LCS is found
                            {
                                lastSubsBegin = thisSubsBegin;
                                sequenceBuilder.Length = 0; //clear it
                                sequenceBuilder.Append(str1.Substring(lastSubsBegin, (i + 1) - lastSubsBegin));
                            }
                        }
                    }
                }
            }
            sequence = sequenceBuilder.ToString();

            return sequence;
        }

        /// <summary>
        /// Executes the specified command in a DOS-box. This can be an MSDOS-command or a filename to start some executable or open some file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="Timeout">Timeout in milliseconds, 0 to wait indefinitely, or negative value for no timeout (and skip errorcheck for started process)</param>
        /// <param name="outputString"></param>
        /// <param name="workingdirectory"></param>
        /// <param name="windowsStyle"></param>
        /// <returns></returns>
        public static int ExecuteCommand(string command, int Timeout, out string outputString, string workingdirectory = null, ProcessWindowStyle windowsStyle = ProcessWindowStyle.Hidden)
        {
            int ExitCode = 0;
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/C " + command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = (Timeout < 0) ? true : false;
            if (workingdirectory == null)
            {
                ProcessInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            }
            else
            {
                ProcessInfo.WorkingDirectory = workingdirectory;
            }
            if (Timeout >= 0)
            {
                ProcessInfo.RedirectStandardError = true;
                ProcessInfo.RedirectStandardOutput = true;
            }
            ProcessInfo.WindowStyle = windowsStyle;

            outputString = string.Empty;
            ExitCode = 0;
            Process = Process.Start(ProcessInfo);
            Process.PriorityBoostEnabled = true;
            // Process.PriorityClass = ProcessPriorityClass.AboveNormal;
            //            Process.BeginOutputReadLine();
            if (Timeout >= 0)
            {
                if (Timeout == 0)
                {
                    Process.WaitForExit();
                }
                else
                {
                    Process.WaitForExit(Timeout);
                }
                if (Process.HasExited)
                {
                    outputString = Process.StandardOutput.ReadToEnd() + Process.StandardError.ReadToEnd();
                    ExitCode = Process.ExitCode;
                    Process.Close();
                }
                else
                {
                    outputString = string.Empty;
                    ExitCode = 0;
                }
            }
            return ExitCode;
        }
    }
}
