// NumberRounder is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of NumberRounder.
// 
// NumberRounder is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// NumberRounder is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with NumberRounder. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.NumberRounder
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

        public const int CHUNK_STRING_LENGTH = 100000;

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
            AddAuthor("Koen Jansen");
            ToolPurpose = "SIF-tool for rounding all numbers in one or more textfiles to given number of decimals";
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

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            //string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );

            //Log.AddInfo("Processing input files ...");
            int modifiedCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                int decimalCount = settings.DecimalCount;
                string decimalSeperator = settings.DecimalSeperator;
                string listSeperator = settings.ListSeperator;

                CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

                if (settings.IsBackedUp)
                {
                    if ((settings.BackupPath == null) || settings.BackupPath.Equals(string.Empty))
                    {
                        settings.BackupPath = string.Empty;
                    }

                    string backupFoldername = Path.Combine(Path.GetDirectoryName(inputFilename), settings.BackupPath);
                    if (!Directory.Exists(backupFoldername))
                    {
                        Directory.CreateDirectory(backupFoldername);
                    }

                    string backupFilename = Path.Combine(backupFoldername, Path.GetFileNameWithoutExtension(inputFilename) + "_org" + Path.GetExtension(inputFilename));
                    if (File.Exists(backupFilename))
                    {
                        backupFilename = Path.Combine(Path.GetDirectoryName(backupFilename), Path.GetFileNameWithoutExtension(backupFilename) + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + Path.GetExtension(backupFilename));
                    }
                    File.Copy(inputFilename, backupFilename, true);
                }

                string formatString = "0";
                if (decimalCount > 0)
                {
                    formatString += ".";
                    for (int i = 0; i < decimalCount; i++)
                    {
                        formatString += "0";
                    }
                }

                Log.AddInfo("Reading file '" + Path.GetFileName(inputFilename) + "' ...", 1);
                double dblValue = double.NaN;
                bool isModified = false;
                StreamReader sr = null;
                string scientificSubstring = null;
                StringBuilder resultStringBuilder = new StringBuilder();
                try
                {
                    sr = new StreamReader(inputFilename);
                    while (!sr.EndOfStream)
                    {
                        StringBuilder newLineStringBuilder = new StringBuilder();
                        string wholeLine = sr.ReadLine().Trim();

                        string[] lineValues = wholeLine.Split(new string[] { listSeperator }, StringSplitOptions.None);
                        for (int i = 0; i < lineValues.Length; i++)
                        {
                            string valueString = lineValues[i];
                            if (settings.ThousandsSeperator != null)
                            {
                                valueString = valueString.Replace(settings.ThousandsSeperator, string.Empty);
                            }

                            if (valueString.Contains(decimalSeperator))
                            {
                                // Force English notation
                                valueString = valueString.Replace(decimalSeperator, ".");

                                if (double.TryParse(valueString, NumberStyles.Float, englishCultureInfo, out dblValue))
                                {
                                    if (settings.ScientificMode == ScientificMode.RemoveRound)
                                    {
                                        // Handle actual rounding (and implicitly remove and round scientific notation if present)
                                        valueString = valueString.Replace(valueString.Trim(), Math.Round(dblValue, decimalCount).ToString(formatString, englishCultureInfo));
                                    }
                                    else 
                                    {
                                        // If string is in scientific notation, keep scientific notation
                                        int idx = valueString.ToUpper().IndexOf("E");
                                        if (idx > 0) 
                                        {
                                            if (settings.ScientificMode == ScientificMode.KeepRound)
                                            {
                                                scientificSubstring = valueString.Substring(idx);
                                                valueString = valueString.Substring(0, idx);
                                                dblValue = double.Parse(valueString, NumberStyles.Float, englishCultureInfo);

                                                // Handle actual rounding
                                                valueString = valueString.Replace(valueString.Trim(), Math.Round(dblValue, decimalCount).ToString(formatString, englishCultureInfo));

                                                valueString += scientificSubstring;
                                            }
                                            else
                                            {
                                                // ignore numeric strings with scientific notation: do not round and keep notation
                                            }
                                        }
                                        else
                                        {
                                            // Handle actual rounding of numeric values without scientific notation
                                            valueString = valueString.Replace(valueString.Trim(), Math.Round(dblValue, decimalCount).ToString(formatString, englishCultureInfo));
                                        }
                                    }

                                    // Reset original decimal seperator
                                    valueString = valueString.Replace(".", decimalSeperator);
                                }
                            }
                            newLineStringBuilder.Append(valueString + listSeperator);
                        }

                        // remove last listseperator
                        if (lineValues.Length > 0)
                        {
                            newLineStringBuilder.Remove(newLineStringBuilder.Length - 1, 1);
                        }
                        resultStringBuilder.AppendLine(newLineStringBuilder.ToString());

                        if (!isModified && !newLineStringBuilder.ToString().Equals(wholeLine))
                        {
                            isModified = true;
                            modifiedCount++;
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new Exception("No admission to read file, check security settings or current file usage for: " + Path.GetFileName(inputFilename), ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error during conversion of " + Path.GetFileName(inputFilename), ex);
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }
                }

                if (IsWritten(isModified, settings))
                {
                    string outputFilename = inputFilename;
                    WriteResult(resultStringBuilder, outputFilename, inputFilename, settings);
                    
                }
                else
                {
                    Log.AddInfo("no modifications", 1);
                }
            }

            ToolSuccessMessage = "Finished processing " + inputFilenames.Count() + " file(s) and changed " + modifiedCount +  " file(s)" ;

            return exitcode;
        }

        protected virtual bool IsWritten(bool isModified, SIFToolSettings settings)
        {
            return isModified;
        }

        protected virtual void WriteResult(StringBuilder resultStringBuilder, string outputFilename, string inputFilename, SIFToolSettings settings)
        {
            // Write result file
            Log.AddInfo("writing rounded file '" + Path.GetFileName(inputFilename) + "' ...", 1);
            StreamWriter sw = null;

            try
            {
                sw = new StreamWriter(outputFilename, false);
                while (resultStringBuilder.Length > CHUNK_STRING_LENGTH)
                {
                    sw.Write(resultStringBuilder.ToString(0, CHUNK_STRING_LENGTH));
                    resultStringBuilder.Remove(0, CHUNK_STRING_LENGTH);
                }
                sw.Write(resultStringBuilder);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("No admission to write file, check security settings or current file usage for: " + Path.GetFileName(outputFilename), ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during writing of " + Path.GetFileName(outputFilename), ex);
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
}
