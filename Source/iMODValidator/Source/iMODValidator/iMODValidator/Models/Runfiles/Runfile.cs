// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Runfiles
{
    /// <summary>
    /// Base class for handling a specific RUN-file version
    /// </summary>
    public abstract class Runfile
    {
        protected static CultureInfo englishCultureInfo = null;
        protected const int MaxUnknownPackageCount = 10;
        private const string RunfileCategoryString = "Runfile";

        protected string[] runfileLines;
        public string[] RunfileLines
        {
            get { return runfileLines; }
            set { runfileLines = value; }
        }

        protected string runfilename;
        public string Runfilename
        {
            get { return runfilename; }
            set { runfilename = value; }
        }
        protected string outputFoldername;
        public string OutputFoldername
        {
            get { return outputFoldername; }
            set { outputFoldername = value; }
        }

        protected Model model;
        public Model Model
        {
            get { return model; }
            set { model = value; }
        }

        protected StreamReader sr = null;
        protected int currentLineIndex;
        protected Dictionary<string, int> unknownPackageCountDictionary;

        public Runfile(string runfilename)
        {
            englishCultureInfo = new CultureInfo("en-GB", false);
            this.runfilename = runfilename;
            this.model = null;
            this.unknownPackageCountDictionary = new Dictionary<string, int>();
        }

        public string ReadOutputfolder(Log log)
        {
            StreamReader sr = null;
            string outputFolder = null;

            try
            {
                log.AddMessage(LogLevel.Trace, "Opening runfile " + runfilename);
                sr = new StreamReader(runfilename);

                log.AddMessage(LogLevel.Trace, "Reading outputfolder from runfile...", 1);

                // read model outputfolder
                outputFolder = sr.ReadLine();
            }
            catch (Exception ex)
            {
                throw new Exception("Could not parse runfile", ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            return outputFolder;
        }

        protected bool IsEndOfRunfile()
        {
            return (currentLineIndex == runfileLines.Length);
        }

        public string ReadLine()
        {
            if (currentLineIndex < runfileLines.Length)
            {
                return runfileLines[currentLineIndex++];
            }
            else
            {
                return null;
            }
        }

        public string ReadLine(long linenumber)
        {
            if (linenumber < runfileLines.Length)
            {
                return runfileLines[linenumber - 1];
            }
            else
            {
                return null;
            }
        }

        protected void ResetCurrentLinenumber()
        {
            currentLineIndex = 0;
        }

        public long GetCurrentLinenumber()
        {
            return currentLineIndex; // + 1; // Retrieve last read linenr 
        }

        public string PeekLine()
        {
            if (currentLineIndex < runfileLines.Length)
            {
                return runfileLines[currentLineIndex];
            }
            else
            {
                return null;
            }
        }

        public string RemoveWhitespace(string line)
        {
            line = line.Replace("\t", " ");
            while (line.Contains("  "))
            {
                line = line.Replace("  ", " ");
            }

            return line.Trim();
        }

        public Model ReadModel(Log log, int maxKPER = int.MaxValue)
        {
            try
            {
                log.AddMessage(LogLevel.Trace, "Opening runfile " + Path.GetFileName(runfilename) + "...");
                sr = new StreamReader(runfilename);

                log.AddInfo("Reading runfile " + Path.GetFileName(runfilename) + "...");
                string runfileString = sr.ReadToEnd();

                runfileLines = SplitLargeString(runfileString, new string[] { "\n" }, log);
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error while reading runfile " + runfilename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading runfile " + runfilename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }

            ///////////////////////////////////////
            // Start parsing runfile version 2.6 //
            ///////////////////////////////////////
            try
            {
                log.AddInfo("Parsing runfile " + Path.GetFileName(runfilename) + "...");

                ResetCurrentLinenumber();

                model = new Model();
                model.Runfilename = runfilename;

                ReadDataset1(log);
                ReadDataset2(log);
                ReadDataset3(log);
                ReadDataset4(log);
                ReadDataset5(log);
                ReadDataset6(log);
                ReadDataset7(log);
                ReadDataset8(log);
                ReadDataset9(log);
                ReadDataset10(log);
                ReadDataset12(log, maxKPER); // Timestep files
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error while parsing runfile line " + GetCurrentLinenumber(), ex);
            }

            log.AddInfo(string.Empty);

            return model;
        }

        private string[] SplitLargeString(string runfileString, string[] values, Log log = null)
        {
            DateTime prevTime = DateTime.Now;
            if (log != null)
            {
                log.AddInfo("Splitting runfile " + Path.GetFileName(runfilename) + " ..", 0, false);
            }

            List<string> lineList = new List<string>();
            runfileString += values[0];
            int length = runfileString.Length;
            int startIdx = 0;
            TimeSpan interval = new TimeSpan(0, 0, 5);
            int eolIdx = IndexOf(runfileString, values, startIdx);
            do
            {
                if ((log != null) && ((DateTime.Now - prevTime) > interval))
                {
                    // Add signs of life...
                    log.AddInfo(".", 0, false);
                    prevTime = DateTime.Now;
                }

                string nextLine = runfileString.Substring(startIdx, eolIdx - startIdx).Trim().Replace("\r", "");
                if (!nextLine.Equals(string.Empty))
                {
                    lineList.Add(nextLine);
                }
                startIdx = eolIdx + 1;
                eolIdx = IndexOf(runfileString, values, startIdx);
            } while (eolIdx > 0);
            log.AddInfo(".");
            return lineList.ToArray();
        }

        private int IndexOf(string inputString, string[] values, int startIndex)
        {
            int minIdx = int.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                int idx = inputString.IndexOf(values[i], startIndex);
                if ((idx > 0) && (idx < minIdx))
                {
                    minIdx = idx;
                }
            }
            if (minIdx == int.MaxValue)
            {
                return -1;
            }
            else
            {
                return minIdx;
            }
        }

        // Parse dataset 1: read model outputfolder
        public void ReadDataset1(Log log)
        {
            log.AddInfo("Reading dataset 1: outputfolder...");
            OutputFoldername = ReadLine();
            Model.ModelresultsPath = this.OutputFoldername.Replace("\"", string.Empty).Trim();
        }

        // Parse dataset 2:  Configuration number of model layers, stress periods, type of simulation, spatial network methodology  
        public void ReadDataset2(Log log)
        {
            log.AddInfo("Reading dataset 2: configuration ...");
            string wholeLine = RemoveWhitespace(ReadLine());
            string[] lineParts = wholeLine.Split(new char[] { ' ', ',' });

            // NLAY,MXNLAY,NPER,SDATE,NSCL,IFTEST,ICONCHK,IIPF,IUNCONF,IFVDL 
            Model.NLAY = int.Parse(lineParts[0]);
            Model.MXNLAY = int.Parse(lineParts[1]);
            Model.NPER = int.Parse(lineParts[2]);
            try
            {
                Model.SDATE = long.Parse(lineParts[3]);
            }
            catch (Exception)
            {
                Model.SDATE = 0;
                log.AddWarning(RunfileCategoryString, runfilename, "Invalid value for SDATE parameter in dataset 2, assuming default of 0");
            }
            Model.NSCL = int.Parse(lineParts[4]);
            Model.IFTEST = (int.Parse(lineParts[5]));
            Model.ICONCHK = (int.Parse(lineParts[6]));
            if (lineParts.Length > 7)
            {
                try
                {
                    Model.IIPF = (int.Parse(lineParts[7]));
                }
                catch (Exception)
                {
                    Model.IIPF = 0;
                    log.AddWarning(RunfileCategoryString, runfilename, "Invalid value for IIPF parameter in dataset 2, assuming default of 0");
                }
            }
            else
            {
                Model.IIPF = 0;
            }

            if (lineParts.Length > 8)
            {
                //                Model.IUNCONF = (int.Parse(lineParts[7]));
            }
            if (lineParts.Length > 9)
            {
                //                Model.IFDVL = (int.Parse(lineParts[7]));
            }
        }

        // Parse optional dataset 3: File to monitor time-series during a simulation ??
        public void ReadDataset3(Log log)
        {
            int ipfCount = Math.Abs(Model.IIPF);
            // Apply only whenever IIP(F)=1 or negative from Data Set 2 

            if (ipfCount > 0)
            {
                log.AddInfo("Reading dataset 3: timeseries ...");
                for (int i = 1; i <= ipfCount; i++)
                {
                    string wholeLine = ReadLine();
                    string[] lineParts = wholeLine.Split(new char[] { ',' });
                    // todo parse monitorline

                    // IPF_TS,IPFTYPE
                }
            }
        }

        // Parse dataset 4: simulation mode (submodels)
        public void ReadDataset4(Log log)
        {
            log.AddInfo("Reading dataset 4: simulation mode ...");
            string wholeLine = string.Empty;
            try
            {
                wholeLine = RemoveWhitespace(ReadLine());

                // First try splitting with comma's
                string[] lineParts = wholeLine.Split(new char[] { ',' });
                if (lineParts.Length < 5)
                {
                    // try splitting with spaces
                    string[] lineParts2 = wholeLine.Split(new char[] { ' ', ',' });
                    lineParts = (lineParts2.Length > lineParts.Length) ? lineParts2 : lineParts;
                    if (lineParts.Length < 5)
                    {
                        throw new Exception("Error for dataset 4, " + lineParts.Length + " out of 5 obligatory parameters found.");
                    }
                }

                // NMULT,IDEBUG ,IEXPORT,IPOSWEL,ISCEN,IBDG,MINKD,MINC 
                Model.NMULT = (lineParts.Length > 0) ? int.Parse(lineParts[0], englishCultureInfo) : 0;
                Model.IDEBUG = (lineParts.Length > 1) ? int.Parse(lineParts[1], englishCultureInfo) : 0;
                Model.IEXPORT = (lineParts.Length > 2) ? int.Parse(lineParts[2], englishCultureInfo) : 0;
                Model.IPOSWELL = (lineParts.Length > 3) ? int.Parse(lineParts[3], englishCultureInfo) : 0;
                Model.ISCEN = (lineParts.Length > 4) ? int.Parse(lineParts[4], englishCultureInfo) : 0;
                Model.IBDG = (lineParts.Length > 5) ? int.Parse(lineParts[5], englishCultureInfo) : 0;
                Model.MINKD = (lineParts.Length > 6) ? float.Parse(lineParts[6], englishCultureInfo) : 0.001f;
                Model.MINC = (lineParts.Length > 7) ? float.Parse(lineParts[7], englishCultureInfo) : 1f;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read dataset 4, line: " + wholeLine, ex);
            }
        }

        // Parse optional dataset 5: solver configuration
        public void ReadDataset5(Log log)
        {
            // todo line is optional? which check?
            log.AddInfo("Reading dataset 5: solver configuration ...");
            string wholeLine = RemoveWhitespace(ReadLine());
            string[] lineParts = wholeLine.Split(new char[] { ' ' });

            // OUTER,INNER,HCLOSE,QCLOSE,RELAX, MAXWBALERROR,MXCNVG,DELTCNVG 
            //  todo
        }

        // Parse optional dataset 6: Simulation window, location of the (sub)model and the chosen raster size 
        public void ReadDataset6(Log log)
        {
            string wholeLine;
            string[] lineParts;

            Model.Submodels = new SubModel[Model.NMULT];

            if ((Model.NMULT > 0) && (Model.NSCL > 0))
            {
                // read submodel extent's
                log.AddInfo("Reading dataset 6: simulation window ...");
                for (int i = 0; i < Model.NMULT; i++)
                {
                    wholeLine = ReadLine().Replace(",", " ");
                    wholeLine = RemoveWhitespace(wholeLine);
                    lineParts = wholeLine.Split(new char[] { ' ' });
                    model.Submodels[i] = new SubModel();

                    try
                    {
                        if (Model.NSCL == 1)
                        {
                            if (Model.NMULT > 1)
                            {
                                // IACT,XMIN,YMIN,XMAX,YMAX,CSIZE,BUFFER,CSUB 
                                model.Submodels[i].IACT = int.Parse(lineParts[0]);
                                if ((model.Submodels[i].IACT < -1) || (model.Submodels[i].IACT > 1))
                                {
                                    log.AddError(RunfileCategoryString, runfilename, "Invalid value for IACT in definition for submodel " + i + ": IACT = " + lineParts[0], 1);
                                }
                                model.Submodels[i].XMIN = float.Parse(lineParts[1], englishCultureInfo);
                                model.Submodels[i].YMIN = float.Parse(lineParts[2], englishCultureInfo);
                                model.Submodels[i].XMAX = float.Parse(lineParts[3], englishCultureInfo);
                                model.Submodels[i].YMAX = float.Parse(lineParts[4], englishCultureInfo);
                                model.Submodels[i].CSIZE = float.Parse(lineParts[5], englishCultureInfo);
                                model.Submodels[i].BUFFER = float.Parse(lineParts[6], englishCultureInfo);
                                model.Submodels[i].CSUB = lineParts[7];
                            }
                            else // (Model.NMULT == 1)    
                            {
                                // XMIN,YMIN,XMAX,YMAX,CSIZE,BUFFER 
                                model.Submodels[i].XMIN = float.Parse(lineParts[0], englishCultureInfo);
                                model.Submodels[i].YMIN = float.Parse(lineParts[1], englishCultureInfo);
                                model.Submodels[i].XMAX = float.Parse(lineParts[2], englishCultureInfo);
                                model.Submodels[i].YMAX = float.Parse(lineParts[3], englishCultureInfo);
                                model.Submodels[i].CSIZE = float.Parse(lineParts[4], englishCultureInfo);
                                model.Submodels[i].BUFFER = float.Parse(lineParts[5], englishCultureInfo);
                            }
                        }
                        else
                        {
                            if (Model.NSCL == 2)
                            {
                                if (Model.NMULT > 1)
                                {
                                    // IACT,XMIN,YMIN,XMAX,YMAX,CSIZE,MAXCSIZE,BUFFER,CSUB 
                                    model.Submodels[i].IACT = int.Parse(lineParts[0]);
                                    if ((model.Submodels[i].IACT < -1) || (model.Submodels[i].IACT > 1))
                                    {
                                        log.AddError(RunfileCategoryString, runfilename, "Invalid value for IACT in definition for submodel " + i + ": IACT = " + lineParts[0], 1);
                                    }
                                    model.Submodels[i].XMIN = float.Parse(lineParts[1], englishCultureInfo);
                                    model.Submodels[i].YMIN = float.Parse(lineParts[2], englishCultureInfo);
                                    model.Submodels[i].XMAX = float.Parse(lineParts[3], englishCultureInfo);
                                    model.Submodels[i].YMAX = float.Parse(lineParts[4], englishCultureInfo);
                                    model.Submodels[i].CSIZE = float.Parse(lineParts[5], englishCultureInfo);
                                    model.Submodels[i].MAXCSIZE = float.Parse(lineParts[6], englishCultureInfo);
                                    model.Submodels[i].BUFFER = float.Parse(lineParts[7], englishCultureInfo);
                                    model.Submodels[i].CSUB = lineParts[8];
                                }
                                else    // (Model.NMULT == 1)
                                {
                                    // XMIN,YMIN,XMAX,YMAX,CSIZE,MAXCSIZE,BUFFER,CSUB 
                                    model.Submodels[i].XMIN = float.Parse(lineParts[0], englishCultureInfo);
                                    model.Submodels[i].YMIN = float.Parse(lineParts[1], englishCultureInfo);
                                    model.Submodels[i].XMAX = float.Parse(lineParts[2], englishCultureInfo);
                                    model.Submodels[i].YMAX = float.Parse(lineParts[3], englishCultureInfo);
                                    model.Submodels[i].CSIZE = float.Parse(lineParts[4], englishCultureInfo);
                                    model.Submodels[i].MAXCSIZE = float.Parse(lineParts[5], englishCultureInfo);
                                    model.Submodels[i].BUFFER = float.Parse(lineParts[6], englishCultureInfo);
                                    model.Submodels[i].CSUB = lineParts[7];
                                }
                            }
                            else
                            {
                                throw new ToolException("Invalid NSCL value for dataset 6:" + Model.NSCL);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Invalid value in definition for submodel " + i + ": " + wholeLine, ex);
                    }

                }
            }
            else if ((Model.NMULT == 0) && (Model.NSCL == 0))
            {
                // use extent of boundary file from dataset 9 for modelrun
                log.AddInfo("NSCL = 0, NMULT=0: extent of boundary file from dataset 9 will be used", 1);
            }
            else
            {
                throw new ToolException("Invalid MNULT or NSCL values for dataset 6:" + Model.NMULT + ", " + Model.NSCL);
            }
        }

        // Parse optional dataset dataset 7:  Scenario file, file that describes the scenario configuration 
        public void ReadDataset7(Log log)
        {
            // Apply only whenever ISCEN=1 from Data Set 4 
            if (Model.ISCEN == 1)
            {
                log.AddInfo("Reading dataset 7: scenario file ...");
                string wholeLine = ReadLine();
                // todo parse line: SCNFILE
            }
        }

        // Parse dataset 8: Activated modules(9)/packages(9) and their corresponding output  
        public void ReadDataset8(Log log)
        {
            string packageKey;
            string wholeLine;
            string[] lineParts;

            log.AddInfo("Reading dataset 8: active modules/packages ...");
            wholeLine = ReadLine();
            if (!wholeLine.Trim().ToLower().Equals(iMODValidatorSettingsManager.Settings.RunfileActivemodulesHeader.ToLower()))
            {
                throw new ToolException("Expected '" + iMODValidatorSettingsManager.Settings.RunfileActivemodulesHeader);
            }

            // Parse all package activation lines
            wholeLine = RemoveWhitespace(PeekLine());                   // first just peek at next line
            while (!IsEndOfRunfile() && (RetrievePackageKey(wholeLine) != null))
            {
                // IPM,NLSAVE,ILSAVE(NLSAVE),KEY
                wholeLine = RemoveWhitespace(ReadLine());               // now actually read and advance to next line
                lineParts = wholeLine.Split(new char[] { ' ', ',' });

                // retrieve the package key from the line
                packageKey = RetrievePackageKey(wholeLine);

                // Retrieve the corresponding package object
                Package package = PackageManager.Instance.CreatePackageInstance(packageKey, model);

                // check if package is supported 
                if (package != null)
                {
                    // Remove (singleton) packagefiles from a previous run that may still be attacked to this model
                    package.ClearFiles();

                    // check if package is active. For the purpose of validation simply all available packages files will be validated
                    package.IsActive = lineParts[0].Equals("1");
                    // todo parse output layers?

                    Model.AddPackage(package);
                    if (package.IsActive)
                    {
                        log.AddInfo("Package " + packageKey + " is active.", 1);
                    }
                    else
                    {
                        log.AddInfo("Package " + packageKey + " is inactive.", 1);
                    }
                }
                else
                {
                    log.AddWarning(RunfileCategoryString, runfilename, "Package " + packageKey + " is currently not supported and is skipped.", 1);
                }

                // read next line
                wholeLine = RemoveWhitespace(PeekLine());
                lineParts = wholeLine.Split(new char[] { ' ' });
            }
        }

        // Parse dataset 9: Boundary
        public void ReadDataset9(Log log)
        {
            log.AddInfo("Reading dataset 9: boundary ...");
            string wholeLine = ReadLine();
            string[] lineParts = wholeLine.Split(new char[] { ',' });
            SubModel subModel = new SubModel();
            subModel.IACT = 1;
            subModel.BUFFER = 0;

            if (lineParts.Length == 1)
            {
                model.BNDFILE = wholeLine.Trim().Replace("'", string.Empty).Replace("\"", string.Empty);

                if (File.Exists(model.BNDFILE))
                {
                    IDFFile bndIDFFile = IDFFile.ReadFile(model.BNDFILE, true, null, 0, Model.GetExtent());

                    // Retrieve submodel extent from BND-file
                    subModel.XMIN = bndIDFFile.Extent.llx;
                    subModel.YMIN = bndIDFFile.Extent.lly;
                    subModel.XMAX = bndIDFFile.Extent.urx;
                    subModel.YMAX = bndIDFFile.Extent.ury;
                    subModel.CSIZE = bndIDFFile.XCellsize;
                }
                else
                {
                    log.AddWarning(RunfileCategoryString, runfilename, "BND-file does not exist: " + model.BNDFILE, 1);
                }
            }
            else if (lineParts.Length == 4)
            {
                // Retrieve submodel extent from specified extent
                subModel.XMIN = float.Parse(lineParts[1], englishCultureInfo);
                subModel.YMIN = float.Parse(lineParts[2], englishCultureInfo);
                subModel.XMAX = float.Parse(lineParts[3], englishCultureInfo);
                subModel.YMAX = float.Parse(lineParts[4], englishCultureInfo);
                // Note: CSIZE is not defined now
            }
            else
            {
                throw new ToolException("Invalid values for dataset 9: " + wholeLine.Trim());
            }

            // Store submodel extent of none was defined earlier (in dataset 6)
            if (Model.Submodels == null)
            {
                Model.Submodels = new SubModel[1];
                model.Submodels[0] = subModel;
            }
        }

        // Parse dataset 10: Modules for each layers, number of files
        public void ReadDataset10(Log log)
        {
            log.AddInfo("Reading dataset 10: module/package definitions ...");

            // Check for line with "MODULES FOR EACH LAYER"-string
            string wholeLine = ReadLine().ToLower();
            if (!wholeLine.StartsWith(iMODValidatorSettingsManager.Settings.RunfilesModulelayersHeader.ToLower()))
            {
                throw new ToolException("Unexpected text, expected '" + iMODValidatorSettingsManager.Settings.RunfilesModulelayersHeader + "'");
            }

            ReadPackages(log);
        }

        /// <summary>
        /// Is used for both timerelated and non-timerelated data to read the consequtive packages and their files
        /// </summary>
        /// <param name="log"></param>
        /// <param name="firstStressPeriod"></param>
        private void ReadPackages(Log log, StressPeriod firstStressPeriod = null, int maxKPER = int.MaxValue)
        {
            string wholeLine;
            string[] lineParts;
            int fileCount;
            string packageKey;
            StressPeriod stressPeriod;

            stressPeriod = firstStressPeriod;

            // Now start reading packages: NFILES,KEY 
            wholeLine = PeekLine(); // just peek at line, don't actually advance the line. This is necessary to stop reading the non-transient files which are followed by a a line without commma's
            lineParts = Split(wholeLine, new char[] { ',' });

            packageKey = "unknown";
            while ((lineParts != null) && (lineParts.Length > 1))
            {
                CheckManager.Instance.CheckForAbort();

                // now actually read the line and advance the cursor
                wholeLine = RemoveWhitespace(ReadLine().Trim());

                // check for a new package: the line consists of an nfiles, key pair
                if (lineParts.Length == 2)
                {
                    // Parse line for new package and read package files
                    fileCount = int.Parse(lineParts[0]);
                    packageKey = RetrievePackageKey(lineParts[1]);
                    if (packageKey == null)
                    {
                        log.AddError(RunfileCategoryString, runfilename, "Unexpected line in runfile: " + wholeLine, 2);
                    }
                    else
                    {
                        // skip -1 definitions, there will be defined as missing timesteps 
                        if (fileCount > -1)
                        {
                            // Parse dataset 11, actually read package files
                            ReadDataset11(packageKey, fileCount, log, stressPeriod);
                        }
                    }
                }
                else
                {
                    if (!IsFileDefinitionLine(lineParts))
                    {
                        // line is not a new packagedefinition and not a file definition, so it must be a new stressperiod
                        stressPeriod = RetrieveTransientStressperiod(lineParts, model, log);
                        if (stressPeriod != null)
                        {
                            // Skip optional steady-state stress period in transient runfiles
                            if ((firstStressPeriod != null) && (firstStressPeriod.DateTime == null))
                            {
                                // there's always at least one transient (e.g. the RIV) package, use it to determine the first time step when a steady-state timestep is present before the actual transient data
                                firstStressPeriod = stressPeriod;
                                model.StartDate = firstStressPeriod.DateTime;
                                // model.firstStressPeriod = firstStressPeriod;
                            }

                            log.AddMessage(LogLevel.Trace, "Reading " + stressPeriod.ToString(), 1);

                            if (stressPeriod.KPER > model.NPER)
                            {
                                log.AddWarning(RunfileCategoryString, runfilename, "Stressperiod (" + stressPeriod.KPER + ") larger than defined number of stressperiods NPER (" + model.NPER + "). Reading stopped.", 2);
                                return;
                            }

                            if (stressPeriod.KPER > maxKPER)
                            {
                                log.AddWarning(RunfileCategoryString, runfilename, "Stressperiod (" + stressPeriod.KPER + ") larger than specified maximum number of stressperiods maxKPER (" + maxKPER + "). Reading stopped.", 2);
                                return;
                            }
                        }
                        else
                        {
                            // Unexpected stressPeriodstring
                            HandleInvalidRunfileLine(wholeLine, packageKey, stressPeriod, firstStressPeriod, log);
                        }
                    }
                    else
                    {
                        // may be one of some leftover lines from unkown package
                        // skip lines until a new stressperiod or a new package is found (skipping files for unknown packages)
                        HandleInvalidRunfileLine(wholeLine, packageKey, stressPeriod, firstStressPeriod, log);
                    }
                }

                // read next line
                wholeLine = PeekLine();
                lineParts = Split(wholeLine, new char[] { ',' });
            }
        }

        private void HandleInvalidRunfileLine(string wholeLine, string packageKey, StressPeriod stressPeriod, StressPeriod firstStressPeriod, Log log)
        {
            if (stressPeriod == firstStressPeriod)
            {
                // just write error for first stress period
                string stressPeriodString = string.Empty;
                if (stressPeriod != null)
                {
                    stressPeriodString = " " + stressPeriod.ToString();
                }
                log.AddError(RunfileCategoryString, runfilename, "Unexpected line while parsing packagefile for " + packageKey + " package" + stressPeriodString + ", expected {NFILES, KEY}-pair, but found: " + wholeLine.Trim(), 1);
            }
            else
            {
                log.AddError(RunfileCategoryString, runfilename, "Unexpected line: " + wholeLine.Trim(), 1);
            }
        }

        private bool IsFileDefinitionLine(string[] lineParts)
        {
            if (lineParts.Length == 4)
            {
                try
                {
                    int intValue = int.Parse(lineParts[0]);
                    double dblValue = double.Parse(lineParts[1], englishCultureInfo);
                    dblValue = double.Parse(lineParts[2], englishCultureInfo);
                    string extension = Path.GetExtension(lineParts[3]).ToLower(); ;
                    List<string> validExtensions = new List<string>(new string[] { ".idf", ".ipf", ".isg", ".gen" });
                    if (validExtensions.Contains(extension))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return a StressPeriod object if valid and null if the line doesn't contain a new stressperiod with a valid date
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private StressPeriod RetrieveTransientStressperiod(string[] lineParts, Model model, Log log)
        {
            // if a new stressperiod is started read it: KPER,DELT,SNAME,ISAVE (, optional number)
            // Note: In some RUN-files extra info is added after the 5th parameter. Ignore this
            if (lineParts.Length < 4)
            {
                return null;
            }

            try
            {
                int KPER = int.Parse(lineParts[0]);
                float DELT = float.Parse(lineParts[1], englishCultureInfo);

                // Assume (DELT > 0), a transient modelrun
                if (!DELT.Equals(1f))
                {
                    log.AddWarning(RunfileCategoryString, runfilename, "Currently iMODValidator only works for DELT=1: Timestep string in errorfiles will not be correct!");
                }

                // If Model.SDATE=0, names will be constructed by SNAME solely (see Data 
                // Set 12), otherwise it is SDATE + DELT notated as yyyymmdd. 
                string SNAME = null;
                if (model.SDATE == 0)
                {
                    long tmpResult;
                    SNAME = lineParts[2].Trim();
                    // Check for a number with 8 digits: yyyymmdd
                    if ((SNAME.Length != 8) || !long.TryParse(SNAME, out tmpResult))
                    {
                        log.AddError(RunfileCategoryString, runfilename, "Unexpected SNAME parameter for stressperiod " + KPER + " in line " + GetCurrentLinenumber() + ": " + SNAME, 1);
                        return null;
                    }
                }
                else
                {
                    // If SDATE>0, the variable SNAME will be overruled by the computation SDATE+DELT 
                    // to form a date expressing the end date of the current simulation period
                    SNAME = Model.GetStressPeriodString(model.StartDate, KPER);
                }
                int ISAVE = int.Parse(lineParts[3]);
                return new StressPeriod(KPER, DELT, SNAME, ISAVE);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing stressperiod-definition in line " + GetCurrentLinenumber() + ": " + lineParts[0] + "," + lineParts[1] + "," + lineParts[2] + "," + lineParts[3], ex);
            }
        }

        protected string[] Split(string line, char[] seperator)
        {
            if (line == null)
            {
                return null;
            }
            else
            {
                return RemoveWhitespace(line.Trim()).Split(seperator);
            }
        }

        // Parse dataset 11:  Input file assignment, files for each module/package
        public void ReadDataset11(string packageKey, int fileCount, Log log, StressPeriod stressPeriod = null)
        {
            Package package = Model.GetPackage(packageKey);

            if (package == null)
            {
                // Package was not found in modeldefinition, add package now, since it is is used in the RUN-file; check if the package is known
                package = PackageManager.Instance.CreatePackageInstance(packageKey, model);

                // check if package is supported 
                if (package != null)
                {
                    log.AddWarning("Package " + packageKey + " is used, but not defined as (in)active package in RUN-file header", 1);
                    // Remove (singleton) packagefiles from a previous run that may still be attacked to this model
                    package.ClearFiles();

                    package.IsActive = false;
                    Model.AddPackage(package);
                }
            }

            if (package != null)
            {
                // Now start reading packagefiles
                try
                {
                    package.ParseRunfilePackageFiles(this, fileCount, log, stressPeriod);
                }
                catch (Exception ex)
                {
                    if (stressPeriod != null)
                    {
                        log.AddWarning(packageKey, runfilename, "Could not parse packagefiles for package " + packageKey + " for stressperiod " + stressPeriod.ToString() + ": " + ExceptionHandler.GetExceptionChainString(ex), 1);
                    }
                    else
                    {
                        log.AddWarning(packageKey, runfilename, "Could not parse packagefiles for package " + packageKey + ": " + ExceptionHandler.GetExceptionChainString(ex), 1);
                    }
                    log.AddInfo(ExceptionHandler.GetStacktraceString(ex, true, 1), 1);
                }
            }
            else
            {
                // if package is unknown
                HandleDataset11UnknownPackage(packageKey, fileCount, log, stressPeriod);
            }
        }

        private void HandleDataset11UnknownPackage(string packageKey, int fileCount, Log log, StressPeriod stressPeriod = null)
        {
            string stressPeriodString = string.Empty;
            if (stressPeriod != null)
            {
                stressPeriodString = " for " + stressPeriod.ToString();
            }

            if (!unknownPackageCountDictionary.ContainsKey(packageKey))
            {
                unknownPackageCountDictionary.Add(packageKey, 0);
            }
            unknownPackageCountDictionary[packageKey] = unknownPackageCountDictionary[packageKey] + 1;

            if (unknownPackageCountDictionary[packageKey] <= MaxUnknownPackageCount)
            {
                log.AddWarning(RunfileCategoryString, runfilename, "Unknown package " + packageKey + stressPeriodString + " in line " + GetCurrentLinenumber() + ", skipping " + fileCount + " lines.", 1);
            }
            else if (unknownPackageCountDictionary[packageKey] == MaxUnknownPackageCount + 1)
            {
                log.AddWarning(RunfileCategoryString, runfilename, "More than " + MaxUnknownPackageCount + " times found unknown " + packageKey + "-package, further references to missing " + packageKey + "-package are not logged");
            }
            for (int i = 1; i <= fileCount; i++)
            {
                // simply skip lines
                string wholeLine = ReadLine();
            }
        }

        private void ReadDataset12(Log log, int maxKPER = int.MaxValue)
        {
            string wholeLine;
            string[] lineParts;
            //int fileCount;
            //string packageKey;

            log.AddInfo("Reading dataset 12: packages for each layer and stress period ...");

            // Check for line with "PACKAGES FOR EACH LAYER AND STRESS-PERIOD"-string
            wholeLine = ReadLine();
            if (wholeLine != null)
            {
                wholeLine = wholeLine.ToLower();
            }
            try
            {
                if (wholeLine == null)
                {
                    // finished reading file
                    log.AddError(RunfileCategoryString, runfilename, "Unexpected end of file, expected '" + iMODValidatorSettingsManager.Settings.RunfileTimestepsHeader + "'");
                    return;
                }

                if (!wholeLine.StartsWith(iMODValidatorSettingsManager.Settings.RunfileTimestepsHeader.ToLower()))
                {
                    throw new ToolException("Unexpected text, expected '" + iMODValidatorSettingsManager.Settings.RunfileTimestepsHeader + "'");
                }

                // Now start reading packages: TS

                // KPER,DELT,SNAME,ISAVE
                wholeLine = RemoveWhitespace(ReadLine().Trim());
                lineParts = wholeLine.Split(new char[] { ',' });
                int KPER = int.Parse(lineParts[0]);
                float DELT = float.Parse(lineParts[1], englishCultureInfo);
                string SNAME = lineParts[2].Trim();
                int ISAVE = int.Parse(lineParts[3]);
                if ((DELT > 0) && !SNAME.ToLower().Equals("steady-state"))
                {
                    if (!DELT.Equals(1f))
                    {
                        log.AddWarning(RunfileCategoryString, runfilename, "Currently iMODValidator only works for DELT=1: Timestep string in errorfiles will not be correct!");
                    }

                    // Check timestep date for transient runs (DELT > 0)
                    long tmpResult;
                    if ((SNAME.Length != 8) || !long.TryParse(SNAME, out tmpResult))
                    {
                        log.AddError(RunfileCategoryString, runfilename, "Unexpected SNAME parameter for stressperiod " + KPER + " in line " + GetCurrentLinenumber() + ": " + SNAME, 1);
                    }
                }
                else
                {
                    // when DELT=0 or SNAME is "steady-state", a steady-state modelrun is specified, no special action needed, except ensuring KPER=0
                    KPER = 0;
                    model.AddSnameKperPair(SNAME, KPER);
                }

                StressPeriod firstStressPeriod = new StressPeriod(KPER, DELT, SNAME, ISAVE);
                if (model.SDATE == 0)
                {
                    model.StartDate = firstStressPeriod.DateTime;
                }

                ReadPackages(log, firstStressPeriod, maxKPER);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not parse runfile line: " + wholeLine, ex);
            }
        }

        protected string RetrievePackageKey(string line)
        {
            string packageKey = null;

            int idx1 = line.IndexOf("(");
            int idx2 = line.IndexOf(")");
            if ((idx1 >= 0) && (idx1 < idx2))
            {
                packageKey = line.Substring(idx1 + 1, idx2 - (idx1 + 1));
            }
            else if (line.IndexOf("!") >= 0)
            {
                // Try old method for specifying keys
                idx1 = line.IndexOf("!");
                if (idx1 >= 0)
                {
                    idx2 = line.IndexOf(" ", idx1);
                    if (idx2 >= idx1)
                    {
                        packageKey = line.Substring(idx1 + 1, idx2 - (idx1 + 1));
                    }
                }
            }
            else
            {
                // if string is just a single word with some whitespace around, return it as the key
                line = line.Trim();
                if (line.IndexOfAny(new char[] { ' ', ':', '\\', '/', '\t', ';', ',' }) < 0)
                {
                    return line;
                }
            }
            return packageKey;
        }
    }
}
