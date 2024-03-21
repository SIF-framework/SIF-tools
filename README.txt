SIF-tools
---------
SIF-tools is part of SIF-basis and contains the executable tools which can also be used seperately.
SIF-basis is a framework by Sweco for working with iMOD groundwater models.

This tool-set is open source, and free to use and distribute under the 
GNU General Public License v3.0 (GPLv3) or later. See below for the full 
license notice. See LICENSE.txt for the full text of the GPLv3. 

Contents of this README:
1.  Licensing
2.  Documentation
3.  Build
4.  Contacts


1. Licensing
------------
Copyright (c) 2013 Sweco Nederland B.V.

1a. SIF-tools
All rights to this software and documentation, including intellectual
property rights, are owned by Sweco Nederland B.V., except for third
party code or libraries which are governed by their own license.

This SIF-toolset is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This SIF-toolset is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with SIF-toolset. If not, see <https://www.gnu.org/licenses/>.

1b. Third party libraries
The following third party libraries are used in some of the SIF-tools:
- The EPPlus library, version 4.5.3.3 for handling Excel xlsx-files. 
  It has the following license: GNU Lesser General Public License v3.0. 
  The dll is included seperately in the tools directory.
  See:
  https://www.nuget.org/packages/EPPlus/4.5.3.3
  https://github.com/JanKallman/EPPlus.  
- Newtonsoft.Json/Json.NET library, version 13.0.1 (22-03-2021), for 
  serialization to XML. It has the following license: The MIT License (MIT).
  The dll is merged with the iMODValidator.exe tool.
  See:
  https://www.nuget.org/packages/Newtonsoft.Json/13.0.1
  https://www.newtonsoft.com/json
- MIConvexHull for retrieving a convex hull aorund points
  It has the following license: The MIT License (MIT). 
  The dll is used in and merged with IDFGENconvert.exe 
  See: https://github.com/DesignEngrLab/MIConvexHull
- ClipperLib, version 6.4.2 (27 February 2017), for clipping polygons
  It has the following license: Boost Software License - Version 1.0 (August 17th, 2003)
  The source code is used in Sweco.SIF.GIS.dll
  See: https://angusj.com/clipper2/Docs/Overview.htm
- EGIS.ShapeFileLib (Easy GIS .NET), Desktop Edition v4.5.6, 
  for reading/writing of shapefiles. 
  It has the following license: GNU Lesser General Public License v2.1
  The dll is included seperately in the tools directory.
  See: https://www.easygisdotnet.com
- ZedGraph v5.1.7, a charting library for .NET by John Champion
  It has the following license: GNU Lesser General Public License v2.1
  Note: Some changes were applied to fix some issues, see Source\Changes.txt
  The dll is included seperately in the tools directory. 
  See: 
  https://github.com/ZedGraph/ZedGraph/tree/v5.1.7
  https://www.nuget.org/packages/ZedGraph

  
Some of these third party libraries are downloaded/installed via Visual Studio's 
NuGet Packager Manager. Check above NuGet webpages for more information.


2. Documentation
----------------
The SIF-tools are part of SIF-basis, a framework by Sweco for working with 
iMOD-groundwatermodels and processing iMOD-files (i.e. IDF, IPF, GEN, etc.).
For more information about SIF, see: https://github.com/SIF-framework
For more information about iMOD, see: http://oss.deltares.nl/web/imod/home

No installation is needed for the SIF-tools. The tools should be run via the 
command-line. Each SIF-tool executable shows basic instructions for syntax 
and use when run without parameters. Currently the following tools are available:

SIF-tool                Description
--------                -----------
Del2Bin                 For deleting files or subdirectories to the recycle bin      
ExcelMapper             For mapping data from Excel rows to textfile, based on a template file
ExcelSelect             For selection of rows and/or sheets in Excel file(s)
GENbuffer               For buffering GEN-lines or GEN-polygons
GENcreate               For creating GEN-files for specified extent coordinates
GENselect               For selection of features in GEN-files based and modification of column values
GENSHPconvert           For converting GEN-files to shapefiles or vice versa
GENsplit                For splitting GEN-features on column values or IPF-points (for lines)
GeoTOPscale             For adapting kv-values based on (MODFLOW 6) calculated flow through GeoTOP-layers
HFBmanager              For manipulating or retrieving info (e.g. resistance) about GEN-files for HFB-package 
HydroMonitorIPFconvert  For converting HydroMonitor Excel-files to IPF-files
IDFbnd                  For correcting boundary- condition around selected IDF-cells
IDFexp                  For evaluating multiple (nested) IDF-expressions on IDF-files
IDFGENconvert           For IDF-GEN (convex hull) or GEN-IDF (polygons/lines) conversion
IDFinfo                 For retrieving info about IDF-file (e.g. extent, cellsize, nr of values)
IDFmath                 For simple math operations on IDF-files (use IDFexp if complex)
IDFmerge                For (groupwise) aggregation of IDF-files
IDFresample             For resampling values in IDF-file with nearest neighbor method or IDW
IDFselect               For selection of cells in IDF-files
IDFvoxel                For manipulation of voxel IDF-files or creation from GeoTOP CSV-files
IFFselect               For selecting IFF-pathlines in relation to specified volume
IFFSHPconvert           For converting IFF-files to shapefiles
IMFcreate               For creating iMOD IMF-files with specified iMOD-files, legends, etc.
iMODclip                For clipping iMOD-files (IDF/ASC/IPF) to specified extent
iMODdel                 For selective deletion of iMOD-files (IDF/IPF/GEN) (e.g. empty files)
iMODmetadata            For adding or merging metadata to iMOD .MET-files
iMODstats               For creating Excelfile with statistics of IDF/IPF-file(s)
iMODValidator           For checking iMOD-models (RUN-file) for some known modelissues
iMODWBalFormat          For formatting CSV-output of iMOD-batchfunction WBALANCE
IPFcolidx               For retrieving a column number for specified column name in an IPF-file
IPFGENconvert           For converting IPF- to GEN-files or vice versa
IPFjoin                 For joining columnvalues of IPF-file(s) to columnvalues of another IPF-file
IPFmerge                For merging rows from different IPF-files with equal columns
IPFplot                 For plotting IPF-files
IPFreorder              For reordering columns from IPF-file(s) with simple column expressions
IPFsample               For sampling values from IDF-files at points from specified IPF-files
IPFselect               For selection and modification of points/rows in IPF-files
IPFSHPconvert           For converting IPF-file(s) to shapefile(s) or vice versa
IPFsplit                For splitting IPF-files based on column values
ISDcreate               For creating ISD-files from IPF- or GEN-files
ISGinfo                 For retrieving information (extent or number of segments) from an ISG-file
LayerManager            For checking REGIS/iMOD-layermodel for inconsistencies or kD/c-calculation
NumberRounder           For rounding numeric values in textfiles
ReplaceLine             For replacing line at specified linenumber within a text file
ReplaceText             For replacing text in one or more files, optionally using regular expressions
ResidualAnalysis        For comparison of residuals in IPF-files and formatting in Excelsheet
Tee                     For teeing standard output of a command to both standard output and file
URLdownload             For downloading files via somne URL
WorkflowViz             For visualisation of SIF-workflows with GraphViz-graphs

A release can be downloaded via: https://github.com/SIF-framework/SIF-tools/releases


3. Build
--------
The tools are build and tested for Windows 10, 64bit (x64) with .NET Framework 4.5.
For building the SIF-tools, Visual Studio Express 2017 was used. 
For each tool there is a subdirectory Source with the solution file for the tool.

0. For VSE2017, see: https://visualstudio.microsoft.com/vs/express
   Note: Microsoft removed VS2017 Express from its server, VS2013 Express is 
         still available and (except for some new language constructs) should
         work as well. Otherwise try the a Visual Studio Community version.
1. The SIF-source can be downloaded from: https://github.com/SIF-framework/SIF-tools
2. Build the library solutions Sweco.SIF.* under the Libraries\C# directory:
   - Build both the 'Any CPU' and 'x64' configurations.
   - Build the Common, GIS and Statistics libraries first. 
   - Build the iMOD library which depends on the previous ones.
   - Build the Spreadsheets library. This library depends on the third party 
     library EPPlus for handling Excel sheets. It will be added automatically
     via the Nuget Package Manager if a network connection is available. 
     Otherwise add EPPlus version 4.5.3.3 manually, e.g. via NuGet.
3. Build the SIF-tools
   - Most tools can be build for the 'x64' platform.
   - All used libraries are copied automatically via a preprocess Build Event.
   - iMODValidator depends on the third party library Newtonsoft.JSon for 
     handling the settings XML-file. It will be added automatically
     via the Nuget Package Manager if a network connection is available. 
     Otherwise add a version later than 13.0.1 manually, e.g. via NuGet.
4. For the SIF-tools release, SIF- and non-LGPL dll's are merged with the
   executable via ILRepack. See: https://github.com/gluck/il-repack.


4. Usage
--------
All tools can be used via the command-line. The input is specified via command-line parameters. 
Just start the executable without any parameters to get a description of the syntax and available options. 
Some tools also have a GUI (like the iMODValidator). Run these tools with an additional 'info' or 'help' 
keyword at the command-line to get the syntax info.

The tools are developed to work within the SIF-framework and to be started via a corresponding SIF-batchfile. 
This batchfile acts as a wrapper around the SIF-tool and adds logging, error-handling and support to be used in a workflow.
The SIF-batchfiles can be downloaded from the SIF-basis section in GitHub: https://github.com/SIF-framework/SIF-basis


5. Contacts
-----------
If you have a bug, other feedback or a question please contact via:
- https://github.com/SIF-framework/SIF-tools/issues
- https://github.com/SIF-framework/SIF-tools/discussions
