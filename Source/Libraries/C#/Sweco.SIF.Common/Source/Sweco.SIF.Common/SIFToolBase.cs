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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// Abstract base class for SIF tools
    /// </summary>
    public abstract class SIFToolBase
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern IntPtr GetCommandLineW();

        /// <summary>
        /// Language definition for english culture as used in most tools
        /// </summary>
        public static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// SIFToolSettings object with used command-line arguments
        /// </summary>
        public SIFToolSettingsBase Settings { get; set; }

        /// <summary>
        /// Log instance for this tool
        /// </summary>
        public Log Log { get; set; }

        /// <summary>
        /// The authors of this tool as shown in the tool info header
        /// </summary>
        public string[] Authors { get; protected set; }

        /// <summary>
        /// Copyright notice for this tool, as shown in the first line of the tool header.  
        /// </summary>
        public string CopyrightNotice { get; protected set; }

        /// <summary>
        /// Message that is written when the tool finishes sucessfully, or null for no message.
        /// </summary>
        protected string ToolSuccessMessage { get; set; }

        /// <summary>
        /// A short description of tool purpose, as shown in the tool header, after the tool version
        /// </summary>
        protected string ToolPurpose { get; set; }

        /// <summary>
        /// A more detailed description about the tool to show after the tool purpose and before the tool syntax, or null to skip
        /// </summary>
        protected string ToolDescription { get; set; }

        /// <summary>
        /// SIFLicense object for this SIF-tool
        /// </summary>
        protected SIFLicense SIFLicense { get; set; }

        // A list with all defined license strings, as shown in the tool info header, for example license strings of used open source packages
        private List<string> headerLicenseLines;

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener.
        /// Then calls DefineToolProperties method to allow subclass to define tool properties.
        /// </summary>
        public SIFToolBase(SIFToolSettingsBase settings)
        {
            if (settings == null)
            {
                throw new Exception("settings parameter cannot be null for SIFToolBase() constructor");
            }

            this.Log = new Log(Log.ConsoleListener);
            this.ToolPurpose = null;
            this.Authors = null;
            this.headerLicenseLines = null;
            this.SIFLicense = new SIFLicense(this);
            this.Settings = settings;
            this.CopyrightNotice = "Copyright Sweco Nederland B.V.";
            this.ToolSuccessMessage = "Finished processing successfully";

            // Allow the subclass to define the tool properties
            DefineToolProperties();
        }

        /// <summary>
        /// Name of this tool
        /// </summary>
        public virtual string ToolName
        {
            get { return (Assembly.GetEntryAssembly() != null) ? Assembly.GetEntryAssembly().GetName().Name : Assembly.GetExecutingAssembly().GetName().Name; }
        }

        /// <summary>
        /// Version string for this tool, including postfix that specifies SIF variant
        /// </summary>
        public string ToolVersion
        {
            get
            {
                string versionString = (Assembly.GetEntryAssembly() != null) ? Assembly.GetEntryAssembly().GetName().Version.ToString() : Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return versionString + ((SIFLicense.SIFTypeVersionPostfix != null ? "." + SIFLicense.SIFTypeVersionPostfix : string.Empty));
            }
        }

        /// <summary>
        /// Defines SIF-license for this SIF-tool
        /// </summary>
        /// <param name="sifLicense"></param>
        protected void SetLicense(SIFLicense sifLicense)
        {
            this.SIFLicense = sifLicense;
        }

        /// <summary>
        /// Check license, write toolinfo and used arguments, parse and check tool arguments and run tool
        /// </summary>
        /// <param name="isExceptionHandled">if true, exceptions are handled within this method</param>
        /// <param name="isToolDetailWritten">if true, a line with the toolname, version and license details and lines with used tool arguments are written to the console</param>
        /// <param name="isSettingsParsed">if false, settings are assumed to be parsed already</param>
        /// <returns>exitcode, 0 for succes, other values for errors</returns>
        public virtual int Run(bool isExceptionHandled = false, bool isToolDetailWritten = true, bool isSettingsParsed = true)
        {
            if (Settings == null)
            {
                throw new Exception("Settings should be specifed to run SwecoTool object");
            }

            if (Log == null)
            {
                throw new Exception("Log should be non-null to run SwecoTool object");
            }

            CheckLicense();

            if (Settings.HasEnoughArguments())
            {
                int exitcode = -1;

                if (isToolDetailWritten)
                {
                    WriteToolHeader();
                }

                if (!isExceptionHandled)
                {
                    // Run tool and Let exceptions pass to calling method
                    exitcode = DoRun(Settings, isToolDetailWritten, isSettingsParsed);
                }
                else
                {
                    // Run tool and catch and handle exceptions here
                    try
                    {
                        exitcode = DoRun(Settings, isToolDetailWritten, isSettingsParsed);
                    }
                    catch (ToolException ex)
                    {
                        ExceptionHandler.HandleToolException(ex, Log);
                        exitcode = 1;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.HandleException(ex, Log);
                        exitcode = 1;
                    }
                    finally
                    {
                        try
                        {
                            if (Log != null)
                            {
                                Log.WriteLogFile();
                            }
                        }
                        catch (Exception)
                        {
                            // ignore error
                        }
                    }
                }

                return exitcode;
            }
            else
            {
                ShowUsage();
                HandleZeroArguments();
                return 0;
            }
        }

        /// <summary>
        /// Returns string with toolname and version
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToolName + " (v" + ToolVersion + ")";
        }

        /// <summary>
        /// Write information about how to call this SIF tool and about its parameters and options to console
        /// </summary>
        public virtual void ShowUsage()
        {
            WriteToolHeader();
            WriteToolPurpose();
            WriteToolDescription();
            WriteToolSyntax();
        }

        /// <summary>
        /// Checks if current user still needs to accept the SIF-license. 
        /// </summary>
        protected void CheckLicense()
        {
            if (SIFLicense == null)
            {
                throw new Exception("SIFLicense is not defined, please define via SIFTool.SetLicense() method");
            }

            SIFLicense.CheckLicense();
        }

        /// <summary>
        /// Add a line to show in the tool header that defines an applied license (e.g. extra detail for this tool or use of third party library)
        /// </summary>
        /// <param name="licenseLine"></param>
        protected void AddHeaderLicenseLine(string licenseLine)
        {
            if (this.headerLicenseLines == null)
            {
                this.headerLicenseLines = new List<string>();
            }
            this.headerLicenseLines.Add(licenseLine);
        }

        /// <summary>
        /// Add one or more lines to show in the tool header that define applied licenses (e.g. extra detail for this tool or use of third party library)
        /// </summary>
        /// <param name="licenseLines"></param>
        protected void AddHeaderLicenseLine(string[] licenseLines)
        {
            if (this.headerLicenseLines == null)
            {
                this.headerLicenseLines = new List<string>();
            }
            this.headerLicenseLines.AddRange(licenseLines);
        }

        internal void InitializeHeaderLicenseLine(string licenseLine)
        {
            this.headerLicenseLines = new List<string>();
            this.headerLicenseLines.Add(licenseLine);
        }

        /// <summary>
        /// Add specified author to list of authors
        /// </summary>
        /// <param name="author"></param>
        protected void AddAuthor(string author)
        {
            if (Authors == null)
            {
                Authors = new string[] { author };
            }
            else
            {
                if (!Authors.Contains(author))
                {
                    List<string> authorList = new List<string>(Authors);
                    authorList.Add(author);
                    Authors = authorList.ToArray();
                }
            }
        }

        /// <summary>
        /// Write a message to the logfile 
        /// </summary>
        protected virtual void WriteToolSuccessMessage()
        {
            if (ToolSuccessMessage != null)
            {
                if (Log != null)
                {
                    Log.AddInfo(ToolSuccessMessage);
                }
                else
                {
                    Console.WriteLine(ToolSuccessMessage);
                }
            }
        }

        /// <summary>
        /// Writes tool properties (name, version, author and Sweco copyright) to log (if defined) or console
        /// </summary>
        private void WriteToolHeader()
        {
            string authorString = null;
            if (this.Authors != null)
            {
                authorString = CommonUtils.ToString(new List<string>(this.Authors), ", ");
            }

            if (Assembly.GetEntryAssembly() != null)
            {
                if (System.Console.Title.ToLower().Equals(Assembly.GetEntryAssembly().Location.ToLower())) // || System.Console.Title.ToLower().Equals(System.Reflection.Assembly.GetExecutingAssembly().Location.ToLower()))
                {
                    // When the default console title is present replace it by the toolname, otherwise leave the title that might have been set from a batchfile
                    System.Console.Title = ToolName + " console";
                }
            }

            string message = null;
            if (authorString != null)
            {
                message = ToolName + ", version " + ToolVersion + ", a SIF-tool by " + authorString + ", " + CopyrightNotice;
            }
            else
            {
                message = ToolName + ", version " + ToolVersion + ", a SIF-tool, " + CopyrightNotice;
            }

            if (Log != null)
            {
                Log.AddInfo(message);
            }
            else
            {
                System.Console.WriteLine(message);
            }

            // Write additional license lines
            WriteLicenseLines(0);
        }

        /// <summary>
        /// Writes the obligatory line defined via the ToolPurpose-property to the log (if defined) or console
        /// </summary>
        private void WriteToolPurpose()
        {
            if (ToolPurpose != null)
            {
                Log.AddInfo(ToolPurpose);
            }
            else
            {
                throw new Exception("A ToolPurpose should be defined for all SIF-tools, use SIFTool.ToolPurpose-property to define a short, single line description");
            }
        }

        /// <summary>
        /// Writes the optional, more detailed ToolDescription-property to the log (if defined) or console
        /// </summary>
        private void WriteToolDescription()
        {
            if (ToolDescription != null)
            {
                Log.AddInfo(ToolDescription);
            }
        }

        /// <summary>
        /// Write all defined license strings to the log (if defined) or console, for example license strings of used open source packages
        /// </summary>
        /// <param name="indentLevel"></param>
        private void WriteLicenseLines(int indentLevel = 0)
        {
            if (headerLicenseLines != null)
            {
                foreach (string licenseString in headerLicenseLines)
                {
                    if (Log != null)
                    {
                        Log.AddInfo(licenseString, indentLevel);
                    }
                    else
                    {
                        for (int indentIdx = 0; indentIdx < indentLevel; indentIdx++)
                        {
                            System.Console.Write("\t");
                        }
                        Log.AddInfo(licenseString);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves full filename for resultfile depending on relative path of input filename and specified settings
        /// </summary>
        /// <param name="inputFilename">the full filename of the input file, including path</param>
        /// <param name="outputBasePath">the base output path</param>
        /// <param name="inputBasePath">the base input path; the input file should be somewhere under this path</param>
        /// <param name="outputFilename">if not null, this filename (excluding path) will be used for the output filename</param>
        /// <param name="extension">if not null, this extension is forced to the output filename; an exception is thrown is different from optionally specified output filename</param>
        protected virtual string RetrieveOutputFilename(string inputFilename, string outputBasePath, string inputBasePath = null, string outputFilename = null, string extension = null)
        {
            if (outputFilename != null)
            {
                // IF an extension was defined check that specified extension is the same as the output filename that the user has specified
                if ((extension != null) && !Path.GetExtension(outputFilename).Replace(".", string.Empty).ToLower().Equals(extension.ToLower()))
                {
                    throw new ToolException("Specified output filename should have extension '" + extension + "'");
                }

                // If a specific output filename was specified, simply use that filename and add it to the specified output path
                outputFilename = Path.Combine(outputBasePath, outputFilename);
            }
            else
            {
                // If no output filename was specified, use input filename with output extension and same relative path of input file under input path
                outputFilename = Path.Combine(outputBasePath, FileUtils.GetRelativePath(Path.ChangeExtension(inputFilename, extension), inputBasePath));
            }
            return Path.GetFullPath(outputFilename);
        }

        /// <summary>
        /// Show lines with possible tool syntax and explanation of options and parameters. And optionally show some example command-lines.
        /// </summary>
        private void WriteToolSyntax()
        {
            Settings.ShowToolSyntax(Log);
        }

        /// <summary>
        /// Do actual run by calling subclass implementation of abstract methods
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="isToolDetailWritten"></param>
        /// <param name="isSettingsParsed">set to false if settings do not have to be parsed</param>
        /// <returns></returns>
        private int DoRun(SIFToolSettingsBase settings, bool isToolDetailWritten, bool isSettingsParsed)
        {
            // Parse and check tool arguments
            if (isSettingsParsed)
            {
                settings.ParseArguments();
            }

            if (isToolDetailWritten)
            {
                // Call abstract method that is overriden in subclass
                settings.LogSettings(Log);
            }

            // Check specified settings by calling virtual Check method that can be override in subclass
            settings.CheckSettings();

            // Call abstract method that is overriden in subclass and that actually starts the tool process
            int exitCode = StartProcess();
            WriteToolSuccessMessage();
            return exitCode;
        }

        /// <summary>
        /// Handle situation with no arguments on command-line: wait for user keypress
        /// </summary>
        protected void HandleZeroArguments()
        {
            if ((Settings == null) || (Settings.Args.Length == 0))
            {
                // No arguments where given
                System.Console.WriteLine("Press any key to close this window.");
                System.Console.ReadKey();
            }
        }

        /// <summary>
        /// Retrieve orginal command-line arguments before formatting by Environment.CommandLine for Windows .NET Framework
        /// </summary>
        /// <returns>array with arguments (after toolname) that werecomma-seperated</returns>
        public static string[] GetFullArgs()
        {
            // code from: https://learn.microsoft.com/en-us/answers/questions/1179400/command-line-arguments-being-reformatted 
            IntPtr command_line0 = GetCommandLineW();
            string command_line = Marshal.PtrToStringUni(command_line0);
            string[] args = command_line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> argList = new List<string>(args);
            argList.RemoveAt(0);

            return argList.ToArray();
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected abstract void DefineToolProperties();

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected abstract int StartProcess();
    }
}
