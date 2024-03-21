# Documentation iMODValidator SIF-tool

# Introduction

Many of the Dutch regional groundwater models are built to be run with
iMOD[^1]. These regional models are also used to built smaller, local
models. Because of the complexity of the models and the large number of
model files, a complete, manual check for the validity of these models
is no longer feasible. Therefore the risk of unnoticed issues, like
inconsistencies in the layer model, is large.
[Sweco](http://www.sweco.nl) has been working for many years with these
kind of models and started in 2013 with the development of a tool to
automatically check iMOD-models for a number of known issues and to have
an efficient method to test and increase the quality of these
iMOD-models. This tool is known as the iMODValidator.

In the current version two main types of functionality are available:
- Validation of model input via a single RUN- or PRJ-file
- Comparison of model input of two iMOD-models via two RUN- or
    PRJ-files.

![Figure 1.1](/Source/iMODValidator/Doc/media/image1.png)

Figure 1.1 Screenshot of the main window of the current tool

# Tool description

## Introduction

The tool can be started in GUI mode by starting the executable without
command-line parameters. In the screen shown, a RUN- or PRJ-file can
then be specified (this is a model file with the file locations of all
input files of the model). This is sufficient to start the tool. If
desired, various settings can be changed to tailor the controls to
regional or local conditions. The tool then reads the RUN/PRJ-file,
performs all checks on known file types and reports the results in both
iMOD and Excel.

The RUN/PRJ-file(s) and the settings can also be specified via the
command line. Starting the tool with 'help' or 'info' on the command
line displays the syntax screen with instructions for starting via the
command line (e.g. iMODValidator.exe info):

![](/Source/iMODValidator/Doc/media/image2.png)

Figure 1.1 shows a screenshot of the main window that is displayed when
the tool is launched via GUI mode. Here the RUN/PRJ-file can be defined
and log messages are displayed during the execution of the tool.

The available functionality is broadly as follows:
- A RUN- or PRJ-file (for iMOD v5.x) can be read and validated or
  compared with another RUN- or PRJ-file.
- Both steady-state and transient models can be read
- Validation checks are available for many of the existing
  iMOD-packages. See section 2.3.2 for an overview of the supported
  iMOD-packages  and see the Dutch ‘Achtergrondrapportage’ of 
  the iMODValidator for details of the checks.
- Differences between two iMOD models can be determined via
  comparison. Not only the paths and filenames that are present in the
  RUN/PRJ-file are compared, but also the actual data of known files
  (IDF, IPF, GEN, ISG) is compared.
- Results are presented spatially and in tabular form via:
   a.  iMOD: IMF-file (a GIS project file) with all data, legends, lines, etc.
   b.  Excel: formatted table with overview of issues or differences that are found.
- Extensive logging is available for all checks that are performed,
- The various settings can be adjusted flexibly via the GUI and via an XML-file.

## Settings

The checks can be made specific for an area via settings in an XML-file
(default this is iMODValidator.XML in the same folder as the tool). This
concerns, for example, acceptable permeabilities, threshold levels or a
selection of certain checks. Most numerical settings can also be varied
spatially by specifying an IDF-file.

If too many invalid issues are reported, it may be worthwhile to adjust
the iMODValidator settings. This can be done directly in the XML-file,
but it is easier in the iMODValidator GUI mode, since for each setting a
short description is given and a check for valid input is provided. See
figure 2.1 for an example of the 2nd tab, in which settings can be
adjusted per check. In the 3rd tab (Advanced Settings) a number of
general settings are available, such as a range of checked model layers,
see figure 2.2. Optionally, you can refer to another XML-file via the /s
option. Every adjustment to settings from the 2nd tab is immediately
saved, without notification, in the used settings file. Settings from
the 3rd tab must be manually adjusted in the XML-file to have them
available as default for next time.

By deleting the XML-file iMODValidator.XML, which is located in the same
directory as the iMODValidator.exe tool, a new XML-file is automatically
created with default settings.

### File locations

There are two settings that indicate the location of files that cannot
necessarily be derived from the RUN/PRJ-file. These must be adjusted
manually in the XML-file:
- iMODExecutablePath: Full file name of the iMOD executable;
- DefaultSurfaceLevelFilename: Full file name of a surface level grid.

### Transient settings

Checks for transient models can take a long time and require a
relatively large amount of disk space. This can be influenced via a
number of settings:

- The number of stress periods that are checked can be limited via
    MaxTimestep (see figure 2.2). For example 365 can be specified as
    the maximum number of stress period or time step, to only check the
    first year with a (first) summer and winter period. By default the
    field is blank and all time steps are checked. If a maximum time
    step is specified and there are more stress periods, a warning will
    be given in the log file and resulting Excel file, see Figure 2.6.

- Checking ISG-files is handled by gridding them via iMOD. Each time
    step that refers to an ISG-file, as indicated in the RUN/PRJ file,
    is only gridded once. However, it can still take a long time for
    ISG-files with daily river levels. For the RIV-check, DRN-RIV-check
    and MetaSWAP-check, you can indicate via the IsISGConverted setting
    that the gridding of ISG files should be skipped for that specific
    check.

- To prevent the resulting IMF- and Excel-file from becoming too large
    and unworkable for a check of several years, you can specify in the
    Advanced Settings that the results must be split per year:
    
    ![](/Source/iMODValidator/Doc/media/image3.png)

### GEN-files shown in iMOD

GEN-files with lines or polygons can also be included in the XML-file to
be shown in iMOD as background features. The file name, line size, line
colour and selection can be specified per GEN-file. See the default
XML-file for examples that can be modified and copied. Non-existent
files are skipped with a warning.

![](/Source/iMODValidator/Doc/media/image4.png)

Note: if the XML tag \<IsSelected\> or \<Thickness\> does not have a
valid value, the XML-file is corrupt and the tool will not start. The
same applies if the XML-file becomes corrupt for another reason. In that
case, the XML file can be deleted to try again.

## Validation

### General description

With the validation functionality, input files of the model can be
checked for various possible issues. Automatic checks will not find all
issues in the model that are problematic. There are many issues that can
only be identified with knowledge about the area and/or a visual
inspection. Nevertheless, the iMODValidator-tool will save a significant
amount of time and the tool keeps being developed to further improve the
checks.

Below are some examples of checks:

- check RCH values to be within a plausible range;
- check for inconsistencies between layer thickness and k-values;
- check for incorrect filter settings in the WEL-package;
- check for NoData-values in the OLF-package at the location of RIV
cells.

A distinction is made between consistency checks and plausibility
checks:
- Consistency checks check for technical consistency and "hard" errors
    in the model. In principle, these checks result in error messages.
    These errors indicate inaccuracies in the model schematization.
- Plausibility checks are less hard, are based on rules of thumb and
    usually use user-adjustable check parameters. In principle, these
    checks result in warnings. Warnings are not necessarily a problem,
    it depends on the specific situation and model schematization.

Given the size and complexity of current regional models and the nature
of automatic checks, there will also be false reports of potential
issues. Likewise, sometimes no mention of an existing issue is made.
This is especially important with plausibility checks. To avoid
producing a huge list of incorrectly reported results, several settings
are available that can be adjusted to initially focus on the largest
problems. See figure 2.1 and section 2.2.

Most currently available checks systematically loop through all grid
cells and analyse the model cell-by-cell. Related model cells are
checked in the vertical direction, possibly in other model files, but
most check to do analyse surrounding cells.

![Figure 2.1](/Source/iMODValidator/Doc/media/image5.png)

Figure 2.1 Screenshot of 2nd tab in which selected checks and settings per check can be defined

![Figure 2.2](/Source/iMODValidator/Doc/media/image6.png)

Figure 2.2 3rd tab with general settings. The red marked setting defines the maximally checked stress period in transient checks.

### Supported iMOD-packages

Currently, for the following iMOD-packages checks are available in the
tool:

- BND, SHD, CHD
- TOP, BOT
- VCW (C-waarden), KDW (kD-values)
- KVV, KHV
- KVA
- ANI
- OLF
- DRN
- RIV
- ISG
- WEL
- MetaSWAP
- STO
- Model results: HEAD, BDGFLF

### Analysis of results

In figure 2.3 a screenshot of a generated IMF-file with various
warnings, is shown, opened in iMOD. At the top of the IMF-file is two
IDF-files are present with the total number of errors and warnings found
per cell. Below these two IDF-files, are the iMOD-files per check with
the errors and warnings found per layer/system. Below these are the
associated model input files that relate to the issues found. By
selecting these iMOD-files together and analysing them with the iMOD
Value Inspector (F3 key), you can check what an issue is about. A brief
description of the issue can be seen in the legend. Further explanation
can be found in Chapter 3.

Figure 2.3 shows that there are also issue legend classes with a
\'Combined result\'. This applies to cells with multiple issues. Because
all issue values are an exponent of 2, they can be added together and
traced back afterwards. For example, the combined value 11 indicates
that the issues with values 1, 2 and 8 are present.

In addition, a summary table is created in an Excel-file with the number
of issues found per check (see figure 2.4). This makes it possible to
see which issues occur most often and in which input files they occur.
Always check warnings in the 2nd Excel tab (see figure 2.5), for errors
in the RUN/PRJ-file or files that were not found, which will influence
the issues found.

![Figure 2.3](/Source/iMODValidator/Doc/media/image10.png)

Figure 2.3 Screenshot with iMODValidator warnings for three layers: KVV_L1, RIV_SYS1 and RIV_SYS3

![Figure 2.4](/Source/iMODValidator/Doc/media/image11.png)

Figure 2.4 Screenshot iMODValidator Excel summary with issues found per check

![Figure 2.5](/Source/iMODValidator/Doc/media/image12.png)

Figure 2.5 Example of errors and warnings in log and Excel-file which may influence results, but are not spatial and cannot be shown via the IMF-file.

## Comparison

With the comparison functionality, the model input of two iMOD models
can be compared via the corresponding RUN- or PRJ-files. The contents of
both files are also compared. Not only IDF-files, but also the IPF-files
of the WEL-package, the GEN-files of the HFB-package and the ISG-files
of the ISG-package are compared. For ISG-files, only the whole segments
are currently compared. and for segments that one or more differences, a
GEN-file with the whole segments is written.

![](/Source/iMODValidator/Doc/media/image13.png)

For the WEL-package, for each IPF-point it
is indicated with a specific colour whether there has been a change in
the data or that is was deleted or new.

For a number of packages, no specific order is defined for the entries
in the RUN/PRJ-file. The method to find a corresponding entry in the
other RUN/PRJ-file is as follows:

- If the number of entries in both models for a particular package is
  exactly the same, the corresponding entry is found by order.
- If the number of entries is different for a package, first entries
  are matched by file name. If any unmatched files remain, they will
  be matched by content and, for transient models, when they are
  present for the same time step and layer number.
- Entries with constant values are always matched by order.
- Remaining files are matched based on position in the package
  definition. Files that are at the same position, but have different
  filename/content will be marked as removed and/or added in the results.

It is possible to compare PRJ-files with RUN-files, but the definition
of time steps (stress periods) differs for both formats and the current
iMODValidator version does not offer the functionality to map time steps
of both formats. For time-dependent RUN-files this can cause many
differences and is not recommended.

An IMF-file and an Excel-file with the differences are also created and
shown for the comparison function.

## Hardware and software specifications

The tool requires a 64-bit processor, but otherwise there are no
specific hardware constraints. Many memory usage optimizations have been
included in the tool so that memory is released as soon as it can. In
this way, checking relatively large models is still possible.

For very large models, large input files, and time-dependent checks,
tool runs on 8 or 16 Gb computers can become very slow (and may appear
to be stuck) due to the large amount of virtual memory required via
permanent storage (hard drive). It is therefore recommended that you use
a system with at least 32 GB of memory and a fast drive (like an SSD
drive).

The tool works under Windows. No specific installation is required. To
start iMOD automatically to shown the results, the location of an iMOD
executable must be included in the settings. To show Excel files
automatically, an Excel file viewer (such as Excel, Open Office or Libre
Office) must be installed and linked to xlsx files. It is not necessary
to have Excel installed to generate Excel-files. This requires that the
EPPlus dll-file is present in the directory with the iMODValidator
executable.

The iMODValidator is available open source from version 2.x under the
GPL v3 license via <https://github.com/SIF-framework/SIF-tools>. Updates
can also be downloaded there.

# iMODValidator License

iMODValidator is an open source tool that can be used and distributed as
specified in the GNU General Public License v3.0[^2] (GPLv3) or later.
iMODValidator makes use of EPPlus, but is not linked to it[^3]. The
licences of Graphviz can be found under the Bin-directory.

Below the licence notice of iMODValidator conform the GPLv3 is shown.
See the file LICENSE.txt for the full text of the GPLv3.

All rights to this software and documentation, including intellectual
property rights, are owned by Sweco Nederland B.V. See the related
subdirectories for license notices of used external code or libraries.

iMODValidator is free software: you can redistribute it and/or modify it
under the terms of the GNU General Public License as published by the
Free Software Foundation, either version 3 of the License, or (at your
option) any later version.

iMODValidator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with iMODValidator. If not, see https://www.gnu.org/licenses.

[^1]: <https://oss.deltares.nl/web/imod>
[^2]: <https://www.gnu.org/licenses/gpl-3.0.nl.html>
[^3]: EPPlus is licensed under the GNU LGPL v3.
