// ReplaceText is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ReplaceText.
// 
// ReplaceText is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ReplaceText is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ReplaceText. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sweco.SIF.ReplaceText
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
            SIFToolSettings settings = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                settings = new SIFToolSettings(args);

                tool = new SIFTool(settings);

                if (settings.HasEnoughArguments())
                {
                    settings.ParseArguments();
                    if (settings.HasLogfile)
                    {
                        // Replace log-object with a log-object without the defaultlistener
                        tool.Log = new Log();
                        tool.Log.IsWriteFileAppend = true;
                        tool.Log.Filename = settings.LogFilename;
                    }

                    exitcode = tool.Run(false, true, false);
                }
                else
                {
                    tool.ShowUsage();
                    tool.HandleZeroArguments();
                    exitcode = 0;
                }
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = ((settings != null) && settings.IsMatchCountReturned) ? -1 : 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = ((settings != null) && settings.IsMatchCountReturned) ? -1 : 1;
            }
            finally
            {
                try
                {
                    if (tool.Log != null)
                    {
                        tool.Log.WriteLogFile();
                    }
                }
                catch (Exception)
                {
                    // ignore error
                }
            }

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for replacing text inside one or more files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            int resultCode = -1; // negative for errors, otherwise the total number of matches is returned

            Initialize(settings, Log);

            ToolSuccessMessage = null;
            List<string> matchedFilenames = new List<string>();
            List<string> selectedFilenames = new List<string>();
            Dictionary<string, List<string>> matchedPatterns = new Dictionary<string, List<string>>();
            resultCode = ReplaceText(settings.BasePath, selectedFilenames, matchedFilenames, matchedPatterns, settings, Log);
            if (settings.Filter.Contains("*") || settings.Filter.Contains("?"))
            {
                if (matchedFilenames.Count > 0)
                {
                    Log.AddInfo();
                    if (settings.IsFindOnly)
                    {
                        Log.AddInfo("Summary of matched files:");
                    }
                    else
                    {
                        Log.AddInfo("Summary of modified files:");
                    }
                    foreach (string modifiedFilename in matchedFilenames)
                    {
                        Log.AddInfo(modifiedFilename, 1);
                    }
                }
            }

            if (settings.IsMatchesShown)
            {
                Log.AddInfo();
                Log.AddInfo("Summary of matched patterns:", 0, false);
                if (matchedPatterns.Keys.Count > 0)
                {
                    Log.AddInfo();
                    foreach (string filename in matchedPatterns.Keys)
                    {
                        List<string> matches = matchedPatterns[filename];
                        foreach (string match in matches)
                        {
                            Log.AddInfo(Path.GetFileName(filename) + ": " + match, 1);
                        }
                    }
                }
                else
                {
                    Log.AddInfo(" no matches found");
                }
            }

            if (selectedFilenames.Count == 0)
            {
                Log.AddWarning("No files found in path with specified filter: " + Path.Combine(settings.BasePath, settings.Filter));
            }

            if (settings.IsMatchCountReturned)
            {
                // Return resultcode as it is (number of matches or -1 for errors)
                return resultCode;
            }
            else
            {
                // Return errorcode: 1 for errors, 0 for no errors
                return (resultCode < 0) ? 1 : 0;
            }
        }

        /// <summary>
        /// Handle tool initialization based on specified settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        protected virtual void Initialize(SIFToolSettings settings, Log log)
        {
            if (!settings.IsOnlyEnvVarsExpanded)
            {
                // Correct parameters for @-character: see tool usage.
                if (settings.Text1.StartsWith("@"))
                {
                    settings.Text1 = ParseCommandLineString(settings.Text1.Substring(1, settings.Text1.Length - 1));
                    string message = "Corrected for escape characters in search string";
                    log.AddInfo(message);
                }
                if (settings.Text2.StartsWith("@"))
                {
                    settings.Text2 = ParseCommandLineString(settings.Text2.Substring(1, settings.Text2.Length - 1));
                    string message = "Corrected for escape characters in replacement string";
                    log.AddInfo(message);
                }

                if (settings.IsFindOnly)
                {
                    // Ensure text2 is different from text1
                    settings.Text2 = string.Empty;
                }
            }
        }

        /// <summary>
        /// Replace texts for files in specified path according to settings 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="matchedFilenames">list of processed files that have a match for text1</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected virtual int ReplaceText(string path, List<string> selectedFilenames, List<string> matchedFilenames, Dictionary<string, List<string>> matchedPatterns, SIFToolSettings settings, Log log)
        {
            string message;
            int resultCode = 0; // negative for errors, otherwise the number of matches is returned

            // Retrieve files that match filter that is specified in settings
            string[] filenames = SelectFiles(path, settings, log);
            selectedFilenames.AddRange(filenames);

            // Ensure basepath ends with a directory seperator character, when NOT empty (to prevent processing the root path)
            if (!settings.BasePath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !settings.BasePath.Equals(string.Empty))
            {
                settings.BasePath += Path.DirectorySeparatorChar;
            }

            // Process all selected files
            foreach (string filename in filenames)
            {
                string text = null;
                string relativeFilenamePath = settings.BasePath.Equals(string.Empty) ? filename : filename.Replace(settings.BasePath, string.Empty);
                if (settings.IsOnlyEnvVarsExpanded)
                {
                    message = "Expanding enviroment variables in '" + relativeFilenamePath + "' ... ";
                }
                else
                {
                    if (settings.IsFindOnly)
                    {
                        message = "Searching text1 in '" + relativeFilenamePath + "' ... ";
                    }
                    else
                    {
                        message = "Replacing text1 in '" + relativeFilenamePath + "' ... ";
                    }
                }

                log.AddInfo(message, 0, false);

                // Read complete text from source file
                StreamReader sr = null;
                try
                {
                    FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    text = sr.ReadToEnd();
                }
                catch (UnauthorizedAccessException ex)
                {
                    HandleIOException(filename, ex, log);
                    text = null;
                    resultCode = -1;
                    return resultCode;
                }
                catch (IOException ex)
                {
                    HandleIOException(filename, ex, log);
                    text = null;
                    resultCode = -1;
                    return resultCode;
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }
                }

                if (settings.IsBinarySkipped && (text.IndexOf("\0") >= 0))
                {
                    message = " file contains nul-characters and is skipped";
                    log.AddInfo(message);
                    continue;
                }

                // Now replace text1 with text2 according to settings
                ReplaceText(filename, relativeFilenamePath, ref text, ref resultCode, matchedPatterns, matchedFilenames, settings, log);
                if (resultCode == -1)
                {
                    // An error occured, stop and return
                    return resultCode;
                }
            }
            if (settings.IsRecursive)
            {
                foreach (string recursivePath in Directory.GetDirectories(path.Equals(string.Empty) ? "." : path))
                {
                    int tmpResultcode = ReplaceText(recursivePath, selectedFilenames, matchedFilenames, matchedPatterns, settings, log);
                    // report last error, otherwise the resultcode is used to report the number of matches
                    resultCode = (tmpResultcode < 0) ? tmpResultcode : (resultCode + tmpResultcode);
                }
            }

            return resultCode;
        }

        protected virtual void ReplaceText(string filename, string relativeFilenamePath, ref string text, ref int resultCode, Dictionary<string, List<string>> matchedPatterns, List<string> matchedFilenames, SIFToolSettings settings, Log log)
        {
            string message;
            string newText = null;
            List<string> matches = new List<string>();
            int matchCount = 0;
            int excludeCount = 0;
            if (text != null)
            {
                if (settings.IsOnlyEnvVarsExpanded)
                {
                    // Expand environment variables
                    newText = Environment.ExpandEnvironmentVariables(text);
                }
                else
                {
                    if (settings.IsCaseSensitive)
                    {
                        // Process case sensitivity
                        if (settings.IsRegExp)
                        {
                            // Process case sensitivity with regular expressions
                            newText = Regex.Replace(text, settings.Text1, (match) =>
                            {
                                bool isExcluded = IsSkipped(match.Value, settings.ExcludePatterns, settings.IsRegExp, settings.IsCaseSensitive);
                                if (isExcluded || (settings.IsFirstOnly && (matchCount > 0)))
                                {
                                    excludeCount++;
                                    return match.Value;
                                }
                                else
                                {
                                    matches.Add(match.Value);
                                    matchCount++;
                                    return match.Result(settings.Text2);
                                }
                            }, RegexOptions.Multiline | RegexOptions.Compiled);
                        }
                        else
                        {
                            // Process case sensitivity without regular expressions
                            bool isExcluded = IsSkipped(settings.Text1, settings.ExcludePatterns, false, true);
                            if (isExcluded || (settings.IsFirstOnly && (matchCount > 0)))
                            {
                                newText = text;
                            }
                            else
                            {
                                newText = CustomReplace(text, settings.Text1, settings.Text2, true, out matchCount, ref matches, settings.IsFirstOnly);
                            }
                        }
                    }
                    else
                    {
                        // Process case insensitivity
                        if (settings.IsRegExp)
                        {
                            // Process case insensitivity with regular expressions
                            newText = Regex.Replace(text, settings.Text1, (match) =>
                            {
                                bool isExcluded = IsSkipped(match.Value, settings.ExcludePatterns, settings.IsRegExp, settings.IsCaseSensitive);
                                if (isExcluded || (settings.IsFirstOnly && (matchCount > 0)))
                                {
                                    excludeCount++;
                                    return match.Value;
                                }
                                else
                                {
                                    matches.Add(match.Value);
                                    matchCount++;
                                    return match.Result(settings.Text2);
                                }
                            }, (!settings.IsCaseSensitive ? RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled : RegexOptions.Multiline | RegexOptions.Compiled));
                        }
                        else
                        {
                            // Process case insensitivity without regular expressions
                            bool isExcluded = IsSkipped(settings.Text1, settings.ExcludePatterns, false, false);
                            if (isExcluded || (settings.IsFirstOnly && (matchCount > 0)))
                            {
                                newText = text;
                            }
                            else
                            {
                                newText = CustomReplace(text, settings.Text1, settings.Text2, false, out matchCount, ref matches, settings.IsFirstOnly);
                            }
                        }
                    }
                }

                // Check if source file has been modified because of replacement (note: also for option findonly the source text is replaced in memory)
                if (!text.Equals(newText))
                {
                    resultCode += matchCount;
                    message = matchCount.ToString() + " matches";
                    if (excludeCount > 0)
                    {
                        message += " (" + excludeCount + " matches were excluded)";
                    }
                    matchedFilenames.Add(relativeFilenamePath);
                    log.AddInfo(message);

                    // Show modifications
                    string matchedTextString = "for '" + settings.Text1 + "' ";
                    message = "\t" + matchCount.ToString() + " matches " + (settings.IsMatchedStringShown ? matchedTextString : string.Empty) + "in " + relativeFilenamePath + ((excludeCount > 0) ? " (" + excludeCount + " exclusions)" : string.Empty);
                    log.AddInfo(message);
                    if (log.Listeners.Count == 0)
                    {
                        System.Console.WriteLine(message);
                    }
                    if (settings.IsMatchesShown)
                    {
                        if (!matchedPatterns.ContainsKey(filename))
                        {
                            matchedPatterns.Add(filename, matches);
                        }
                        else
                        {
                            matchedPatterns[filename].AddRange(matches);
                        }
                    }

                    if (!settings.IsFindOnly)
                    {
                        // Save access datetimes in case of reset
                        DateTime creationTime = File.GetCreationTime(filename);
                        DateTime lastAccessTime = File.GetLastAccessTime(filename);
                        DateTime lastWriteTime = File.GetLastWriteTime(filename);

                        // Write modified file
                        StreamWriter sw = null;
                        try
                        {
                            sw = new StreamWriter(filename, false);
                            sw.Write(newText);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            HandleIOException(filename, ex, log);
                            text = null;
                            resultCode = -1;
                        }
                        catch (IOException ex)
                        {
                            HandleIOException(filename, ex, log);
                            text = null;
                            resultCode = -1;
                        }
                        finally
                        {
                            if (sw != null)
                            {
                                sw.Close();
                            }
                        }

                        if (settings.IsDateTimeReset)
                        {
                            try
                            {
                                File.SetCreationTime(filename, creationTime);
                                File.SetLastAccessTime(filename, lastAccessTime);
                                File.SetLastWriteTime(filename, lastWriteTime);
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine("Warning: could not reset file datetime for '" + Path.GetFileName(filename) + "': " + ex.GetBaseException().Message);
                            }
                        }
                    }
                }
                else
                {
                    if (settings.IsErrorNoMatch && !settings.Text1.Equals(settings.Text2))
                    {
                        log.AddInfo("No match for '" + settings.Text1 + "'");
                        throw new ToolException("ERROR: No match for '" + settings.Text1 + "'");
                    }

                    message = (settings.IsFindOnly) ? "no match" : "no modifications needed";
                    if (excludeCount > 0)
                    {
                        message += " (" + excludeCount + " matches were excluded)";
                    }
                    log.AddInfo(message);
                }
            }
        }

        protected virtual string[] SelectFiles(string path, SIFToolSettings settings, Log log)
        {
            string[] filenames = null;
            if (path.Equals(string.Empty))
            {
                filenames = new string[1];
                filenames[0] = settings.Filter;
                if (settings.Filter.Contains("*") || settings.Filter.Contains("?"))
                {
                    path = Path.GetDirectoryName(settings.Filter);
                    filenames = Directory.GetFiles(path, Path.GetFileName(settings.Filter));
                }
            }
            else
            {
                filenames = Directory.GetFiles(path, settings.Filter);
            }

            return filenames;
        }

        /// <summary>
        /// Check if specified string matches one of the specified patterns
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="skipPatterns">list of pattersn that is checked</param>
        /// <param name="isRegExp">if true, patterns are assumed to be regular expressions</param>
        /// <param name="isCaseSensitive"></param>
        /// <returns></returns>
        protected bool IsSkipped(string someString, List<string> skipPatterns, bool isRegExp, bool isCaseSensitive = false)
        {
            if (skipPatterns == null)
            {
                return false;
            }

            // Check string is not matching an exclusion pattern
            int skipPatternIdx = 0;
            bool isSkipped = false;
            while ((skipPatternIdx < skipPatterns.Count) && !isSkipped)
            {
                string skipPattern = skipPatterns[skipPatternIdx];
                if (!isRegExp)
                {
                    // replace wildcard by corresponding RegExp symbol
                    skipPattern = skipPattern.Replace("?", ".");
                    skipPattern = skipPattern.Replace("*", ".*");
                }
                Regex exp = new Regex(skipPattern, (isCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase));
                isSkipped = exp.IsMatch(someString);
                skipPatternIdx++;
            }

            return isSkipped;
        }

        /// <summary>
        /// Custom replace method that allows a maximum number of replacements
        /// </summary>
        /// <param name="srcText"></param>
        /// <param name="toFind"></param>
        /// <param name="toReplace"></param>
        /// <param name="matchCase"></param>
        /// <param name="count"></param>
        /// <param name="replaceOnce"></param>
        /// <returns></returns>
        public static string CustomReplace(string srcText, string toFind, string toReplace, bool matchCase, out int count, ref List<string> matches, bool replaceOnce = false)
        {
            StringBuilder sb = new StringBuilder();
            count = 0;
            StringComparison sc = StringComparison.OrdinalIgnoreCase;
            if (matchCase)
            {
                sc = StringComparison.Ordinal; //Ordinal;
            }

            if (toReplace == null)
            {
                toReplace = string.Empty;
            }

            int pos = 0 - toReplace.Length;
            int prevPos = 0;
            while ((pos = srcText.IndexOf(toFind, prevPos, sc)) > -1)
            {
                sb.Append(srcText.Substring(prevPos, pos - prevPos));
                //                srcText = srcText.Remove(pos, toFind.Length);
                sb.Append(toReplace);
                //                srcText = srcText.Insert(pos, toReplace);
                count++;

                prevPos = pos + toFind.Length;

                if (replaceOnce)
                {
                    break;
                }
            }
            sb.Append(srcText.Substring(prevPos, srcText.Length - prevPos));

            if (count > 0)
            {
                matches.Add(toFind);
            }

            return sb.ToString();
        }

        protected static void HandleIOException(string filename, Exception ex, Log log = null)
        {
            string message = "Could not access " + filename + ". File is skipped" + "\r\n" + GetExceptionChainString(ex);
            if (log != null)
            {
                log.AddInfo(message);
                // Always show errors
                System.Console.WriteLine("Could not access " + filename + ". File is skipped.");
            }
            else
            {
                System.Console.WriteLine(message);
            }
        }

        protected static string GetExceptionChainString(Exception ex)
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
                    tabs += "\t";
                    msg += "\r\n" + tabs + innerex.Message;
                    prevEx = innerex;
                    innerex = innerex.InnerException;
                }
            }
            return msg;
        }

        /// <summary>
        /// Corrects a command line argument string for escape characters
        /// From: http://stackoverflow.com/questions/11433977/passing-new-lines-in-command-line-argument
        /// Based on: https://markyourfootsteps.wordpress.com/tag/codedom/
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ParseCommandLineString(string input)
        {
            var provider = new Microsoft.CSharp.CSharpCodeProvider();
            var parameters = new System.CodeDom.Compiler.CompilerParameters()
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
            };

            var code = @"
                        public class TmpClass
                        {
                            public static string GetValue()
                            {
                                return """ + input + @""";
                            }
                        }";

            var compileResult = provider.CompileAssemblyFromSource(parameters, code);

            if (compileResult.Errors.HasErrors)
            {
                // string codeLine = "                              return \"" + input + "\";";
                // System.Console.WriteLine(codeLine);
                for (int errorIdx = 0; errorIdx < compileResult.Errors.Count; errorIdx++)
                {
                    System.Console.WriteLine("Error " + (errorIdx + 1) + " in column " + compileResult.Errors[errorIdx].Column + ": " + compileResult.Errors[errorIdx].ErrorText); // + ": " + codeLine.Substring(compileResult.Errors[errorIdx].Column, 5)
                }
                System.Console.WriteLine("Code to compile for parsing command-line string:");
                System.Console.WriteLine(code);
                throw new ArgumentException("Interal error while parsing @-string, check quotes and backslashes: " + compileResult.Errors.Cast<System.CodeDom.Compiler.CompilerError>().First(e => !e.IsWarning).ErrorText);
            }

            var asmb = compileResult.CompiledAssembly;
            var method = asmb.GetType("TmpClass").GetMethod("GetValue");

            return method.Invoke(null, null) as string;
        }
    }
}
