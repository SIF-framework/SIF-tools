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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
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
        /// Converts list items to a single string with specified seperator
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listSeperator"></param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<string> list, string listSeperator = ",")
        {
            return ToString(list, listSeperator, false);
        }

        /// <summary>
        /// Converts trimmed strings in list to a single string with specified seperator
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listSeperator">list seperator to add beteen strings</param>
        /// <param name="isTrimmed">if true, strings in list are trimmed for whitespace</param>
        /// <returns>empty string for empty or null list</returns>
        public static string ToString(List<string> list, string listSeperator, bool isTrimmed)
        {
            string listString = string.Empty;
            if (list != null)
            {
                for (int colIdx = 0; colIdx < list.Count; colIdx++)
                {
                    listString += list[colIdx];
                    if (isTrimmed)
                    {
                        listString = listString.Trim();
                    }
                    if (colIdx < list.Count - 1)
                    {
                        listString += listSeperator;
                    }
                }
            }
            return listString;
        }

        /// <summary>
        /// Converts list items to a single string with specified seperator
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
        /// Converts list items to a single string with specified format and seperator
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
        /// Converts list items to a single string with specified format and seperator
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
        /// Converts list items to a single string with specified seperator
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
        public static string[] SplitQuotedDeprecated(string inputString, char listseperator, char quote = '\"', bool isQuoteRemoved = false, bool isWhitespaceTrimmed = false)
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
            int inputStringLength = inputString.Length;
            bool isWhiteSpaceListSeperator = listseperator.Equals(' ') || listseperator.Equals('\t');

            // Create empty list to store temporary result
            List<string> values = new List<string>();

            // Loop through input string and find/process substrings between list seperator
            int charIdx = 0;
            StringBuilder sb = new StringBuilder();
            while (charIdx < inputStringLength)
            {
                // Read current substring
                sb.Clear();

                if (inputString[charIdx] == quote)
                {
                    if (!isQuoteRemoved)
                    {
                        sb.Append(quote);
                    }
                    // Skip first quote
                    charIdx++;

                    // Substring starts with a quote, read string until next quote (followed by list seperator or end-of-string), skipping all intermediate list seperators and quotes
                    while ((charIdx < inputStringLength) 
                        && ((inputString[charIdx] != quote) || ((inputString[charIdx] == quote) && ((charIdx == inputStringLength - 1) || (inputString[charIdx + 1] != listseperator)))))
                    {
                        if (inputString[charIdx] == quote)
                        {
                            // Add intermediate quote when it is not an ending quote (before a listseperator or as the last character of the line)
                            if (((charIdx < inputStringLength - 1) && (inputString[charIdx + 1] != listseperator)))
                            {
                                sb.Append(quote);
                            }
                            charIdx++;
                        }

                        // a quote was found, read string until next quote, skipping all intermediate list seperators
                        while ((charIdx < inputStringLength) && (inputString[charIdx] != quote))
                        {
                            sb.Append(inputString[charIdx++]);
                        }
                    }

                    // Check that a quote was found
                    if (((charIdx < inputStringLength) && (inputString[charIdx] != quote)) || ((charIdx == inputStringLength) && (inputString[charIdx - 1] != quote)))
                    {
                        throw new ToolException("Missing quote character (" + quote  + ") after initially quoted substring and before next list seperator (" + listseperator + "): " + sb.ToString() 
                            + "\nNote: Ensure closing quote is the last symbol of the line or is followed immediately by the list seperator (" + listseperator + ")");
                    }

                    if (!isQuoteRemoved)
                    {
                        sb.Append(quote);
                    }

                    // Skip last quote
                    charIdx++;

                    if (isWhitespaceTrimmed && !isWhiteSpaceListSeperator)
                    {
                        // Skip whitespace
                        while ((charIdx < inputStringLength) && !inputString[charIdx].Equals(listseperator) && (inputString[charIdx].Equals('\t') || inputString[charIdx].Equals(' ')))
                        {
                            charIdx++;
                        }
                    }

                    // check for list seperator or end-of-string and continue with next character
                    if ((charIdx < inputStringLength) && (inputString[charIdx++] != listseperator))
                    {
                        throw new ToolException("Unexpected character '" + inputString[charIdx] + "' after quoted substring: " + sb.ToString());
                    }
                }
                else
                {
                    // Substring does not start with a quote, read string until next list seperator
                    while ((charIdx < inputStringLength) && (inputString[charIdx] != listseperator))
                    {
                        sb.Append(inputString[charIdx++]);
                    }

                    // Skip list seperator
                    charIdx++;

                    if (isWhiteSpaceListSeperator)
                    {
                        // Skip consequtive whitespace list seperators
                        while ((charIdx < inputStringLength) && (inputString[charIdx] == listseperator))
                        {
                            charIdx++;
                        }

                        if (sb.Length == 0)
                        {
                            // No substring has yet been found, just whitespace listseperators, so now read string until next list seperator
                            while ((charIdx < inputStringLength) && (inputString[charIdx] != listseperator))
                            {
                                sb.Append(inputString[charIdx++]);
                            }
                        }
                    }
                }

                string value = sb.ToString();
                if (isWhitespaceTrimmed)
                {
                    value = value.Trim();
                }
                values.Add(value);
            }

            // When string ends with a listseperator, add an empty substring
            if (inputString[inputStringLength - 1] == listseperator)
            {
                values.Add(string.Empty);
            }

            return values.ToArray();
        }

        /// <summary>
        /// Splits a given string with the specified listseperator and excludes the specified quote symbols from the split
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="listseperator"></param>
        /// <param name="startQuote">starting quote symbol</param>
        /// <param name="endQuote">ending quote symbol</param>
        /// <param name="isQuoteRemoved">true if quotes should be removed from the resulting substrings</param>
        /// <param name="isWhitespaceTrimmed">true if the resulting substrings should be trimmed for whitespace, default is false</param>
        /// <returns></returns>
        public static string[] SplitQuoted(string inputString, char listseperator, char startQuote, char endQuote, bool isQuoteRemoved = false, bool isWhitespaceTrimmed = false)
        {
            int inputStringLength = inputString.Length;
            bool isWhiteSpaceListSeperator = listseperator.Equals(' ') || listseperator.Equals('\t');

            // Create empty list to store temporary result
            List<string> values = new List<string>();

            // Loop through input string and find/process substrings between list seperator
            int charIdx = 0;
            StringBuilder sb = new StringBuilder();
            while (charIdx < inputStringLength)
            {
                // Read current substring
                sb.Clear();

                if (inputString[charIdx] == startQuote)
                {
                    if (!isQuoteRemoved)
                    {
                        sb.Append(startQuote);
                    }
                    // Skip first quote
                    charIdx++;

                    // Substring starts with a quote, read string until next quote (followed by list seperator or end-of-string), skipping all intermediate list seperators and quotes
                    while ((charIdx < inputStringLength)
                        && ((inputString[charIdx] != endQuote) || ((inputString[charIdx] == endQuote) && ((charIdx == inputStringLength - 1) || (inputString[charIdx + 1] != listseperator)))))
                    {
                        if (inputString[charIdx] == endQuote)
                        {
                            // Add intermediate quote when it is not an ending quote (before a listseperator or as the last character of the line)
                            if (((charIdx < inputStringLength - 1) && (inputString[charIdx + 1] != listseperator)))
                            {
                                sb.Append(endQuote);
                            }
                            charIdx++;
                        }

                        // a quote was found, read string until next end quote, skipping all intermediate list seperators
                        while ((charIdx < inputStringLength) && (inputString[charIdx] != endQuote))
                        {
                            sb.Append(inputString[charIdx++]);
                        }
                    }

                    // Check that a quote was found
                    if (((charIdx < inputStringLength) && (inputString[charIdx] != endQuote)) || ((charIdx == inputStringLength) && (inputString[charIdx - 1] != endQuote)))
                    {
                        throw new ToolException("Missing end quote character (" + endQuote + ") after initially quoted substring and before next list seperator (" + listseperator + "): " + sb.ToString()
                            + "\nNote: Ensure end quote is the last symbol of the line or is followed immediately by the list seperator (" + listseperator + ")");
                    }

                    if (!isQuoteRemoved)
                    {
                        sb.Append(endQuote);
                    }

                    // Skip last quote
                    charIdx++;

                    if (isWhitespaceTrimmed && !isWhiteSpaceListSeperator)
                    {
                        // Skip whitespace
                        while ((charIdx < inputStringLength) && !inputString[charIdx].Equals(listseperator) && (inputString[charIdx].Equals('\t') || inputString[charIdx].Equals(' ')))
                        {
                            charIdx++;
                        }
                    }

                    // check for list seperator or end-of-string and continue with next character
                    if ((charIdx < inputStringLength) && (inputString[charIdx++] != listseperator))
                    {
                        throw new ToolException("Unexpected character '" + inputString[charIdx] + "' after quoted substring: " + sb.ToString());
                    }
                }
                else
                {
                    // Substring does not start with a quote, read string until next list seperator
                    while ((charIdx < inputStringLength) && (inputString[charIdx] != listseperator))
                    {
                        sb.Append(inputString[charIdx++]);
                    }

                    // Skip list seperator
                    charIdx++;

                    if (isWhiteSpaceListSeperator)
                    {
                        // Skip consequtive whitespace list seperators
                        while ((charIdx < inputStringLength) && (inputString[charIdx] == listseperator))
                        {
                            charIdx++;
                        }
                    }
                }

                string value = sb.ToString();
                if (isWhitespaceTrimmed)
                {
                    value = value.Trim();
                }
                values.Add(value);
            }

            // When string ends with a listseperator, add an empty substring
            if (inputString[inputStringLength - 1] == listseperator)
            {
                values.Add(string.Empty);
            }

            return values.ToArray();
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
        /// Retrieve minimum value in list
        /// </summary>
        public static float Min(List<float> values)
        {
            if ((values == null) || (values.Count == 0))
            {
                return float.NaN;
            }

            float min = float.MaxValue;
            foreach(float value in values)
            {
                if (value < min)
                {
                    min = value;
                }
            }

            return min;
        }

        /// <summary>
        /// Retrieve maximum value in list
        /// </summary>
        public static float Max(List<float> values)
        {
            if ((values == null) || (values.Count == 0))
            {
                return float.NaN;
            }

            float max = float.MinValue;
            foreach (float value in values)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        /// <summary>
        /// Retrieve median value in list
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Median(List<float> values)
        {
            values.Sort();
            int n = values.Count;
            if (n % 2 == 0)
            {
                return (values[n / 2 - 1] + values[n / 2]) / 2;
            }
            else
            {
                return values[n / 2];
            }
        }

        /// <summary>
        /// Retrieve median value in list; note: for an even number of elements the rounded average of the two middle elements is used.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static int Median(List<int> values)
        {
            values.Sort();
            int n = values.Count;
            if (n % 2 == 0)
            {
                return (values[n / 2 - 1] + values[n / 2]) / 2;
            }
            else
            {
                return values[n / 2];
            }
        }

        /// <summary>
        /// The icon as used for SIF-tool forms and executables
        /// </summary>
        public static System.Drawing.Icon SIFIcon
        {
            get { return (System.Drawing.Icon)Resources.ResourceManager.GetObject("SwecoIcon"); }
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
        /// Starts the specified executable
        /// </summary>
        /// <param name="cmdPath"></param>
        /// <param name="parameterString">parameters to add to cmdPath, use null to skip</param>
        /// <param name="log"></param>
        /// <param name="indentLevel"></param>
        /// <param name="timeout">Timeout in milliseconds, 0 to wait indefinitely for process to finish (default), or negative value for no timeout and return immediately</param>
        /// <returns>exitcode of executed command; when a timeout occurs -1 is returned and a Timeout message is added to specified log</returns>
        public static int ExecuteCommand(string cmdPath, string parameterString, Log log, int indentLevel = 0, int timeout = 0)
        {
            string outputString;
            string fullCmdPath = cmdPath;
            if (!Path.IsPathRooted(cmdPath))
            {
                fullCmdPath = Path.Combine(Directory.GetCurrentDirectory(), cmdPath);
            }

            if (File.Exists(fullCmdPath))
            {
                try
                {
                    string command = "\"" + fullCmdPath + "\"" + ((parameterString == null) ? string.Empty : (" " + "\"" + parameterString + "\""));
                    int exitCode = ExecuteCommand(command, timeout, out outputString, Path.GetDirectoryName(fullCmdPath));
                    LogLevel logLevel = LogLevel.Trace;
                    if (exitCode != 0)
                    {
                        logLevel = LogLevel.Error;
                    }
                    if ((log != null) && (outputString.Length > 0))
                    {
                        log.AddMessage(logLevel, outputString);
                    }
                    return exitCode;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while starting " + Path.GetFileName(fullCmdPath), ex);
                }
            }
            else
            {
                if (log != null)
                {
                    log.AddWarning("Specified command file does not exist: " + cmdPath);
                }
            }
            return -1;
        }

        /// <summary>
        /// Executes the specified command in a DOS-box. This can be an MSDOS-command or a filename to start some executable or open some file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="timeout">Timeout in milliseconds, 0 to wait indefinitely for process to finish, or negative value for no timeout and return immediately (and skip errorcheck for started process)</param>
        /// <param name="outputString">output string of executed command; when a timeout occurs outputstring is set to "Timeout occurred"</param>
        /// <param name="workingdirectory"></param>
        /// <param name="windowsStyle"></param>
        /// <returns>exitcode of executed command; when a timeout occurs, -1 is returned and outputstring is set to "Timeout occurred"</returns>
        public static int ExecuteCommand(string command, int timeout, out string outputString, string workingdirectory = null, ProcessWindowStyle windowsStyle = ProcessWindowStyle.Hidden)
        {
            int exitCode = 0;
            ProcessStartInfo processInfo;
            Process process;

            processInfo = new ProcessStartInfo("cmd.exe", "/C " + command);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = (timeout < 0) ? true : false;
            if (workingdirectory == null)
            {
                processInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            }
            else
            {
                processInfo.WorkingDirectory = workingdirectory;
            }
            if (timeout >= 0)
            {
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;
            }
            processInfo.WindowStyle = windowsStyle;

            outputString = string.Empty;
            exitCode = 0;
            process = Process.Start(processInfo);
            process.PriorityBoostEnabled = true;
            // Process.PriorityClass = ProcessPriorityClass.AboveNormal;
            //            Process.BeginOutputReadLine();

            try
            {
                if (timeout >= 0)
                {
                    if (timeout == 0)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        process.WaitForExit(timeout);
                    }
                    if (process.HasExited)
                    {
                        outputString = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                        exitCode = process.ExitCode;
                        process.Close();
                    }
                    else
                    {
                        outputString = "Timeout occurred";
                        exitCode = -1;
                        KillProcessAndChildren(process.Id); // Process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                KillProcessAndChildren(process.Id);
                throw ex;
            }
            return exitCode;
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            // Code from: https://stackoverflow.com/questions/5901679/kill-process-tree-programmatically-in-c-sharp

            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        /// <summary>
        /// Starts the specified batch file
        /// </summary>
        /// <param name="batchFilename"></param>
        /// <param name="arguments">space seperated command-line arguments for batchfile, ensure arguments with spaces are surrounded with double quotes</param>
        /// <param name="outputString">output string that is written to console by batchfile. This string is also logged as Info when log is defined and an error occurs</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="timeout">Timeout in milliseconds, 0 to wait indefinitely, or negative value for no timeout</param>
        /// <returns>iMOD-exitcode</returns>
        public static int RunBatchfile(string batchFilename, string arguments, out string outputString, Log log, int logIndentLevel = 0, int timeout = 0)
        {
            outputString = null;
            if (File.Exists(batchFilename))
            {
                if (log != null)
                {
                    log.AddInfo("Running batchfile " + Path.GetFileName(batchFilename) + " ...", logIndentLevel);
                }

                try
                {
                    string command = EnsureDoubleQuotes(Path.GetFileName(batchFilename)) + " " + arguments;
                    outputString = string.Empty;
                    // int exitCode = ExecuteCommand(batchFilename, arguments, log, timeout);
                    int exitCode = CommonUtils.ExecuteCommand(command, timeout, out outputString, Path.GetDirectoryName(batchFilename));

                    LogLevel logLevel = LogLevel.Trace;
                    if (exitCode != 0)
                    {
                        logLevel = LogLevel.Info;
                    }
                    if ((log != null) && (outputString.Length > 0))
                    {
                        log.AddMessage(logLevel, outputString);
                    }
                    return exitCode;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while starting iMOD", ex);
                }
            }
            else
            {
                if (log != null)
                {
                    log.AddWarning("Specified batchfile does not exist: " + batchFilename);
                }
                else
                {
                    throw new Exception("Specified batchfile does not exist: " + batchFilename);
                }
            }
            return -1;
        }

        /// <summary>
        /// Convert the color from a Color object to an RGB long value (as used for colors in the iMOD IMF-file)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static long Color2Long(Color color)
        {
            return color.R + color.G * 256 + color.B * 65536;
        }

        /// <summary>
        /// Convert an RGB long value (as used for colors in the iMOD IMF-file) to a Color object
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color Long2Color(long color)
        {
            return Color.FromArgb((int)color % 256, (int)(color % 65536) / 256, (int)color / 65536);
        }

        /// <summary>
        /// Removes all specified substrings from a string
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="stringList"></param>
        /// <returns></returns>
        public static string RemoveStrings(string someString, List<string> stringList)
        {
            for (int stringIdx = 0; stringIdx < stringList.Count; stringIdx++)
            {
                someString = someString.Replace(stringList[stringIdx], string.Empty);
            }
            return someString;
        }
    }
}
