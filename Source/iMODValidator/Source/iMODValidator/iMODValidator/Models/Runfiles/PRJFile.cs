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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Models.Packages.Files;

namespace Sweco.SIF.iMODValidator.Models.Runfiles
{
    public abstract class PRJFile : RUNFile
    {
        public override string RUNFileType
        {
            get { return "PRJ"; }
        }

        public PRJFile(string runfilename) : base(runfilename)
        {
        }

        protected override void ParseFile(Log log, int maxKPER)
        {
            string wholeLine;
            string[] lineParts;
            string packageKey;

            log.AddInfo("Parsing PRJ-file packages ...");

            wholeLine = ReadLine();

            packageKey = "unknown";
            while (wholeLine != null)
            {
                CheckManager.Instance.CheckForAbort();

                wholeLine = RemoveWhitespace(wholeLine.Trim());
                lineParts = Split(wholeLine, new char[] { ',' });

                // check for a new package: the line consists of an nfiles, key pair
                if ((lineParts.Length >= 2) && lineParts[1].Contains("("))
                {
                    // Parse line for new package
                    int n = int.Parse(lineParts[0]);
                    packageKey = lineParts[1].Replace("(", string.Empty).Replace(")", string.Empty).Trim();
                    bool isActive = true;
                    if (lineParts.Length > 2)
                    {
                        isActive = int.Parse(lineParts[2]) == 1;
                    }

                    // Retrieve/Define Package object for current package
                    Package package = model.GetPackage(packageKey);
                    if (package != null)
                    {
                        throw new ToolException("Invalid PRJ-file: " + packageKey + "-package is defined more than once");
                    }
                    package = PackageManager.Instance.CreatePackageInstance(packageKey, model);
                    package.IsActive = isActive;
                    package.Model = model;
                    model.AddPackage(package);

                    // Remove (singleton) packagefiles from a previous run that may still be attacked to this model
                    package.ClearFiles();

                    package.ParsePRJFilePackageFiles(this, maxKPER, log, 1);
                }
                else if (wholeLine.ToLower().Equals("periods"))
                {
                    List<string> stopStrings = new List<string>() { "species" };

                    while ((wholeLine != null) && (PeekLine() != null) && !stopStrings.Contains(PeekLine().Trim()))
                    {
                        string period = ReadLine().Trim();
                        string dateString = ReadLine().Trim();
                        if (!DateTime.TryParseExact(dateString, "dd-MM-yyyy hh:mm:ss", englishCultureInfo, System.Globalization.DateTimeStyles.None, out DateTime date))
                        {
                            log.AddError("Invalid period definition date: " + dateString);
                            continue;
                        }
                        model.AddPeriodDefinition(period, date);
                    }
                }

                // read next line
                wholeLine = ReadLine();
            }

            // Define boundary
            HandleBoundary(log, 1);

            Model.NLAY = model.GetMaxLayer();
            Model.MXNLAY = Model.NLAY;
            int maxPackageKPER = model.GetMaxKPER();
            if (maxPackageKPER == 0)
            {
                // For steady-state models, first period has KPER=0
                model.NPER = 1;
            }
            else
            {
                Model.NPER = maxPackageKPER;
            }
        }

        private void HandleBoundary(Log log, int logIndentLevel)
        {
            SubModel subModel = new SubModel();
            subModel.IACT = 1;
            subModel.BUFFER = 0;

            Package bndPackage = model.GetPackage(BNDPackage.DefaultKey);
            PackageFile bndPackageFile = bndPackage.GetPackageFile(0);
            if (bndPackageFile != null)
            {
                model.BNDFILE = bndPackageFile.FName;
            }

            if ((model.BNDFILE != null) && File.Exists(model.BNDFILE))
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
                if (model.BNDFILE == null)
                {
                    log.AddWarning(RUNFileCategoryString, null, "No BND-entries found, retrieving (max) extent and cellsize from other packages ...", 1);
                }
                else
                {
                    log.AddWarning(RUNFileCategoryString, runfilename, "BND-file does not exist: " + model.BNDFILE, logIndentLevel);
                    log.AddInfo("Retrieving (max) extent and cellsize from other packages ...", 1);

                    // Retrieve submodel extent from BND-file
                    Extent extent = model.GetPackageExtent();

                    subModel.XMIN = extent.llx;
                    subModel.YMIN = extent.lly;
                    subModel.XMAX = extent.urx;
                    subModel.YMAX = extent.ury;
                    subModel.CSIZE = model.GetMaxPackageCellSize();
                }

                model.GetPackageExtent();
            }

            Model.Submodels = new SubModel[1];
            model.Submodels[0] = subModel;
        }
    }
}
