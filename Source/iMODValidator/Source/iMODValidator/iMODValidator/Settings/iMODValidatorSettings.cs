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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Settings
{
    /// <summary>
    /// Avaiable extent types
    /// </summary>
    public enum ExtentMethod
    {
        ModelExtent,
        PackageFileExtent,
        CustomExtent
    }

    /// <summary>
    /// Available surfacelevelmethod types
    /// </summary>
    public enum SurfaceLevelMethod
    {
        Smart,
        UseMetaSWAP,
        UseOLF,
        UseFilename
    }

    /// <summary>
    /// Class for storing iMODValidator settings
    /// </summary>
    public class iMODValidatorSettings
    {
        public string TooloutputPrefix;
        public string DefaultInputRUNFile1;
        public string DefaultOutputFolder;
        public string DefaultSurfaceLevelFilename;
        public bool UseSmartSurfaceLevelMethod;
        public bool UseMetaSWAPSurfaceLevelMethod;
        public bool UseOLFSurfaceLevelMethod;
        public bool UseFileSurfaceLevelMethod;
        public bool UseCustomExtentMethod;
        public bool UsePackageFileExtentMethod;
        public float DefaultCustomExtentLLX;
        public float DefaultCustomExtentLLY;
        public float DefaultCustomExtentURX;
        public float DefaultCustomExtentURY;
        public float DefaultNoDataValue;
        public float LevelErrorMargin;
        public string MinTimestep;
        public string MaxTimestep;
        public SplitValidationrunSettings.Options SplitValidationrunOption;
        public string MinILAY;
        public string MaxILAY;
        public float DefaultSummaryMinCellSize;
        public string iMODExecutablePath;
        public bool IsIMODOpened;
        public bool IsExcelOpened;
        public bool IsRelativePathIMFAdded;
        public bool UseLazyLoading;
        public string RunfileTimestepsHeader;
        public string RunfileActivemodulesHeader;
        public string RunfilesModulelayersHeader;
        public string DefaultGENListSeperators;
        public string DefaultIPFListSeperators;
        public bool UseSparseMatrix;
        public bool UseIPFWarningForExistingPoints;
        public bool UseIPFWarningForColumnMismatch;
        public int ComparedDecimalCount;
        public List<GENFileSettings> GENFiles;
        public MetadataSettings Metadata;

        public iMODValidatorSettings()
        {
            TooloutputPrefix = "iMODValidator";
            DefaultInputRUNFile1 = "<please select or type a valid RUN/PRJ-filename>";
            DefaultOutputFolder = "<please select or type enter a valid outputfolder>";

            DefaultSurfaceLevelFilename = "<please enter a valid surfacelevel filename>";
            UseSmartSurfaceLevelMethod = true;
            UseMetaSWAPSurfaceLevelMethod = false;
            UseOLFSurfaceLevelMethod = false;
            UseFileSurfaceLevelMethod = false;

            DefaultCustomExtentLLX = 0;
            DefaultCustomExtentLLY = 0;
            DefaultCustomExtentURX = 1000000;
            DefaultCustomExtentURY = 1000000;

            DefaultNoDataValue = -9999;
            LevelErrorMargin = 0.005f;
            MinTimestep = "0";
            MaxTimestep = "";
            SplitValidationrunOption = SplitValidationrunSettings.Options.None;
            DefaultSummaryMinCellSize = 100;

            iMODExecutablePath = @"iMOD\iMOD_V5_6_1.exe";
            IsIMODOpened = true;
            IsExcelOpened = true;
            IsRelativePathIMFAdded = false;

            ComparedDecimalCount = 7;

            GENFiles = new List<GENFileSettings>();
            // Note: do not add GEN-files here, since these will be added by JsonSerializer to the GEN-files that are present in actual ssettingsfiles

            UseLazyLoading = true;
            RunfileTimestepsHeader = "PACKAGES FOR EACH LAYER AND STRESS-PERIOD";
            RunfileActivemodulesHeader = "ACTIVE MODULES";
            RunfilesModulelayersHeader = "MODULES FOR EACH LAYER";
            DefaultGENListSeperators = "	; ,";
            DefaultIPFListSeperators = "	; ,";
            UseIPFWarningForExistingPoints = false;
            UseIPFWarningForColumnMismatch = true;
            UseCustomExtentMethod = false;
            UsePackageFileExtentMethod = false;

            MinILAY = "1";
            MaxILAY = "999";

            this.Metadata = new MetadataSettings();
        }

        public iMODValidatorSettings(bool addDefaultGENFiles) : this()
        {
            // Force some default setting to show example daa in generated default settings XML-file
            GENFiles.Add(new GENFileSettings(@"C:\Tools\iMODValidator\Shapes\Provincies.GEN", "100,100,100", 2, true));
            GENFiles.Add(new GENFileSettings(@"Shapes\T250WTR.GEN", "0,128,182", 2, false));
        }
    }

    /// <summary>
    /// Class for storing settings used to define how to split a validation run in groups
    /// </summary>
    public class SplitValidationrunSettings
    {
        public enum Options
        {
            None,
            Yearly
        }

        public static string[] RetrieveOptionStrings()
        {
            return new string[] { Options.None.ToString(), Options.Yearly.ToString() };
        }

        public static Options ParseOptionString(string optionString)
        {
            if (optionString.Equals(Options.None.ToString()))
            {
                return Options.None;
            }
            else if (optionString.Equals(Options.Yearly.ToString()))
            {
                return Options.Yearly;
            }
            else
            {
                throw new Exception("Unexpected SplitRunSettings.Options string: " + optionString);
            }
        }
    }
}
