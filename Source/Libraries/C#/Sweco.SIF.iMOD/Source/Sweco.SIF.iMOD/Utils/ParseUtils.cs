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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Utils
{
    /// <summary>
    /// Class with utilities for string and column parsing or searching
    /// </summary>
    public static class ParseUtils
    {
        /// <summary>
        /// Formatting and other cultureInfo of English (UK) language
        /// </summary>
        public static CultureInfo EnglishCultureInfo { get; set; } = new CultureInfo("en-GB", false);

        /// <summary>
        /// Parse string expression with references to columnnames or numbers
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="expressionString"></param>
        /// <param name="columnValues"></param>
        /// <param name="isExceptionThrown">if true, errors will lead to an exception, otherwise a null string is returned</param>
        /// <param name="objectRef">name/id of object that contains columns to use in error messages</param>
        /// <returns>string value based on specified columnvalues and string expression</returns>
        public static string EvaluateStringExpression(List<string> columnNames, string expressionString, List<string> columnValues, bool isExceptionThrown = true, string objectRef = null)
        {
            string resultString = string.Empty;
            string columnExpression = Environment.ExpandEnvironmentVariables(expressionString);
            int idx1 = columnExpression.IndexOf('{');
            int idx2 = -1;
            while (idx1 >= 0)
            {
                resultString += columnExpression.Substring(idx2 + 1, idx1 - idx2 - 1);
                idx2 = columnExpression.IndexOf('}', idx1 + 1);
                if (idx2 < 0)
                {
                    throw new ToolException("Missing closing bracket '}' in expression string: " + expressionString);
                }
                string colString = columnExpression.Substring(idx1 + 1, idx2 - idx1 - 1);

                // Check for string manipulations
                bool isSubStringDefined = false;
                int subStringIdx = 0;
                int subStringLength = 0;
                bool isReplaceDefined = false;
                string searchString = null;
                string replaceString = null;
                if (colString.Contains(":"))
                {
                    // Retrieve part before and after ':'
                    string[] colStringParts = colString.Split(new char[] { ':' });
                    colString = colStringParts[0];
                    string stringExp = colStringParts[1];

                    if (stringExp.StartsWith("~"))
                    {
                        isSubStringDefined = true;
                        ParseSubStringExp(stringExp.Substring(1), out subStringIdx, out subStringLength);
                    }
                    else if (stringExp.Contains("="))
                    {
                        isReplaceDefined = true;
                        string[] stringExpParts = stringExp.Split(new char[] { '=' });
                        searchString = stringExpParts[0];
                        replaceString = stringExpParts[1];
                    }
                }

                // Start retrieving column index and value for specifed column string
                int colIdx = ParseColRefString(columnNames, colString, isExceptionThrown, objectRef);
                if (colIdx < 0)
                {
                    if (isExceptionThrown)
                    {
                        throw new ToolException("Column reference string '" + colString + "' does not match existing column: " + ((objectRef != null) ? objectRef : CommonUtils.ToString(columnNames)));
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    string colValue = columnValues[colIdx];

                    if (isSubStringDefined)
                    {
                        if (subStringIdx < 0)
                        {
                            subStringIdx = colValue.Length + subStringIdx;
                        }
                        if (subStringLength <= 0)
                        {
                            subStringLength = (colValue.Length - subStringLength) - subStringIdx;
                        }

                        if (subStringIdx < 0)
                        {
                            // Similarly to string substitution batchfiles, start with first symbol is start index is smaller than 0
                            subStringIdx = 0;
                        }

                        if (subStringIdx < colValue.Length)
                        {
                            if ((subStringIdx + subStringLength) > colValue.Length)
                            {
                                subStringLength = colValue.Length - subStringIdx;
                            }

                            colValue = colValue.Substring(subStringIdx, subStringLength);
                        }
                        else
                        {
                            colValue = string.Empty;
                        }
                    }
                    if (isReplaceDefined)
                    {
                        colValue = colValue.Replace(searchString, replaceString);
                    }

                    // Add resulting column value to result
                    resultString += colValue;
                }

                // Continue with possible next ColRef-expression
                idx1 = columnExpression.IndexOf('{', idx2 + 1);
            }
            resultString += columnExpression.Substring(idx2 + 1);

            // Check for [,] or [;] substrings, remove when no other characters are present before them
            resultString = ApplyConditionalSeperator(resultString, "[,]", ",");
            resultString = ApplyConditionalSeperator(resultString, "[;]", ";");

            return resultString.Trim();
        }

        private static void ParseSubStringExp(string subStringExp, out int subStringIdx, out int subStringLength)
        {
            string[] subStringExpParts = subStringExp.Split(new char[] { ',' });
            if (!int.TryParse(subStringExpParts[0], out subStringIdx))
            {
                throw new Exception("Invalid substring expression: " + subStringExp);
            }
            if (subStringExpParts.Length > 1)
            {
                if (!int.TryParse(subStringExpParts[1], out subStringLength))
                {
                    throw new Exception("Invalid substring expression: " + subStringExp);
                }
            }
            else
            {
                subStringLength = 0;
            }
        }

        private static string ApplyConditionalSeperator(string someString, string sepSearchString, string sepString)
        {
            int sepIdx = someString.IndexOf(sepSearchString);
            while (sepIdx >= 0)
            {
                if (sepIdx == 0)
                {
                    // ignore seperator at beginning of string
                    someString = someString.Substring(sepSearchString.Length);
                }
                else if (sepIdx == (someString.Length - sepSearchString.Length))
                {
                    // ignore seperator at end of string
                    someString = someString.Substring(0, sepIdx);
                }
                else
                {
                    // first seperator is after/before other characters
                    someString = someString.Substring(0, sepIdx) + sepString + someString.Substring(sepIdx + sepSearchString.Length);
                }

                // Remove double occurances of seperator string (do this only when user uses [sep]-notation
                while (someString.Contains(sepString + sepString))
                {
                    someString.Replace(sepString + sepString, sepString);
                }

                sepIdx = someString.IndexOf(sepSearchString);
            }
            return someString;
        }

        /// <summary>
        /// Parse string expression with references to columnnames or numbers
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="colString"></param>
        /// <param name="isExceptionThrown"></param>
        /// <param name="objectRef">name/id of object that contains columns</param>
        /// <returns>column index or Exception/-1 if not found</returns>
        private static int ParseColRefString(List<string> columnNames, string colString, bool isExceptionThrown = true, string objectRef = null)
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
                    colIdx = FindColumnName(columnNames, colString, true, false);
                }

                if ((colIdx < 0) || (colIdx >= columnNames.Count))
                {
                    if (isExceptionThrown)
                    {
                        throw new ToolException("Column reference string '" + colString + "' does not match existing column in: " + ((objectRef != null) ? objectRef : CommonUtils.ToString(columnNames)));
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

        /// <summary>
        /// Finds zero-based columnindex of specified columnname. If not found -1 is returned.
        /// </summary>
        /// <param name="ColumnNames"></param>
        /// <param name="columnName"></param>
        /// <param name="isMatchWhole">use true to match only whole words</param>
        /// <param name="isMatchCase">use true to match case</param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        public static int FindColumnName(List<string> ColumnNames, string columnName, bool isMatchWhole = true, bool isMatchCase = false)
        {
            int colIdx = -1;

            for (colIdx = 0; colIdx < ColumnNames.Count(); colIdx++)
            {
                string currentColumnname = ColumnNames[colIdx];
                if (!isMatchCase)
                {
                    currentColumnname = currentColumnname.ToLower();
                    columnName = columnName.ToLower();
                }
                if (isMatchWhole)
                {
                    if (currentColumnname.Equals(columnName))
                    {
                        return colIdx;
                    }
                }
                else if (currentColumnname.Contains(columnName))
                {
                    return colIdx;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find unique name for specified columnname. If it already exists in specified columnnames, a new name is made unique by adding a sequencenumber starting with 2
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="initialColumnName"></param>
        /// <param name="initialPostfix">added postfix if columnname already exists</param>
        /// <param name="isMatchWhole"></param>
        /// <param name="isMatchCase"></param>
        /// <returns></returns>
        public static string FindUniqueColumnName(List<string> columnNames, string initialColumnName, string initialPostfix = null, bool isMatchWhole = true, bool isMatchCase = false)
        {
            int idx = 1;
            string colname = initialColumnName;
            while (FindColumnIndex(columnNames, colname, isMatchWhole, isMatchCase) >= 0)
            {
                colname = initialColumnName + ((initialPostfix != null) ? initialPostfix : string.Empty) + ((idx == 1) ? string.Empty : idx.ToString());
                idx++;
            }
            return colname;
        }

        /// <summary>
        /// Finds zero-based columnindex of specified column string, which is either a columnname or a column index. 
        /// If the given string contains an integer number, this number is returned as integer index.
        /// If not found -1 is returned.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="columnNameOrIdx"></param>
        /// <param name="isMatchWhole"></param>
        /// <param name="isMatchCase"></param>
        /// <param name="isNumber">if true, a numeric <paramref name="columnNameOrIdx"/> string is treated as a columnumber and decreased by one to return a columnindex</param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        public static int FindColumnIndex(List<string> columnNames, string columnNameOrIdx, bool isMatchWhole = true, bool isMatchCase = false, bool isNumber = true)
        {
            if (int.TryParse(columnNameOrIdx, out int colIdx))
            {
                if (isNumber)
                {
                    return ((colIdx >= 1) && (colIdx <= columnNames.Count)) ? (colIdx - 1) : -1;
                }
                else
                {
                    return ((colIdx >= 0) && (colIdx < columnNames.Count)) ? colIdx : -1;
                }
            }

            return FindColumnName(columnNames, columnNameOrIdx, isMatchWhole, isMatchCase);
        }

        /// <summary>
        /// Finds one-based columnnumber of specified columnname. 
        /// If the given string contains an integer number, the integer number is returned.
        /// If not found 0 is returned.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="columnNameOrNr"></param>
        /// <param name="isMatchWhole"></param>
        /// <param name="isMatchCase"></param>
        /// <returns>one-based columnnumber or 0 if not found</returns>
        public static int FindColumnNumber(List<string> columnNames, string columnNameOrNr, bool isMatchWhole = true, bool isMatchCase = false)
        {
            if (int.TryParse(columnNameOrNr, out int colNr))
            {
                return ((colNr >= 1) && (colNr <= columnNames.Count)) ? colNr : -1;
            }

            return FindColumnName(columnNames, columnNameOrNr, isMatchWhole, isMatchCase) + 1;
        }

        /// <summary>
        /// Parse a string to a specified fieldtype
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public static object ParseStringValue(string value, FieldType fieldType)
        {
            try
            {
                switch (fieldType)
                {
                    case FieldType.Boolean:
                        return bool.Parse(value);
                    case FieldType.DateTime:
                        return DateTime.Parse(value);
                    case FieldType.Long:
                        return long.Parse(value);
                    case FieldType.Double:
                        return double.Parse(value, EnglishCultureInfo);
                    case FieldType.String:
                        return value;
                    default:
                        throw new Exception("Unexpected fieldType: " + fieldType.ToString());
                }
            }
            catch (Exception)
            {
                throw new Exception("Unexpected value type for value " + value + ". Expected type: " + fieldType.ToString());
            }
        }

        /// <summary>
        /// Retieves field type (boolean, integer, long, double, string or date) for specified value string
        /// </summary>
        /// <param name="valueString"></param>
        /// <returns></returns>
        public static FieldType GetFieldType(string valueString = null)
        {
            // Check what type the specified value has
            DateTime dateValue;
            bool boolValue;
            double doubleValue;
            long longValue;
            if (bool.TryParse(valueString, out boolValue))
            {
                return FieldType.Boolean;
            }
            else if (long.TryParse(valueString, out longValue))
            {
                return FieldType.Long;
            }
            else if (double.TryParse(valueString, NumberStyles.Float, EnglishCultureInfo, out doubleValue))
            {
                return FieldType.Double;
            }
            else if (DateTime.TryParse(valueString, out dateValue))
            {
                return FieldType.DateTime;
            }
            else
            {
                return FieldType.String;
            }
        }

        /// <summary>
        /// For each index in/ first list the value at the index position in the second list is retrieved, the resulting list is returned
        /// No checks are done, an exception will be thrown of indices don't match value list
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<T> RetrieveIndexItems<T>(List<int> indices, List<T> items)
        {
            List<T> results = new List<T>();
            for (int i = 0; i < indices.Count; i++)
            {
                results.Add(items[indices[i]]);
            }
            return results;
        }
    }

    /// <summary>
    /// Enumeration of possible fieldtypes
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// Specifies unknown, not recognized or undefined column values
        /// </summary>
        Undefined,

        /// <summary>
        /// Specifies boolean column values
        /// </summary>
        Boolean,

        /// <summary>
        /// Specifies long column values
        /// </summary>
        Long,

        /// <summary>
        /// Specifies double column values
        /// </summary>
        Double,

        /// <summary>
        /// Specifies string column values
        /// </summary>
        String,

        /// <summary>
        /// Specifies DateTime column values
        /// </summary>
        DateTime
    }
}
