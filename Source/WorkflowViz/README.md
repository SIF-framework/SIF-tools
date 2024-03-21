# Documentation WorkflowViz SIF-tool

# Introduction
Workflows can get complicated easily when composed of many subdirectory's and batchfiles. It will be hard then to get an overview of all individual steps. Also there's a large risk of running the batchfiles in the wrong order or of missing errors in logfiles.

WorkflowViz is a SIF-tool for visualisation van SIF-workflows, where workflows exist of one or multiple combinations of batchfiles and corresponding logfiles and where batchfiles are run in alphabetic order.

![Figure 1.1](/Source/WorkflowViz/Doc/media/Image11.png)

Figure 1.1 Example workflow BASIS0 with subworkflows

# WorkflowViz results
WorkflowViz creates graphs in HTML- and PNG-files voor each subworkflow within a specified WORKIN-directory for a model that is built with the SIF-framework of Sweco. In these graphs batchfiles and subworkflows are visualized by nodes and the order between batchfiles and/or subworkflows is visualized by arrows.

For the creation of the graphs the well-known open source package Graphviz is used, which has many possibilities for visualisation. See [http://www.graphviz.org](http://www.graphviz.org/).

See figure 2.1 for an example graph of a successfully executed workflow with two levels of subworkflows. For demonstration of the tool a few artificial issues have been created, see figure 2.2 and 2.3. The colour of the nodes and arrows shows the status of the batchfile(s) and/or subworkflow(s). See the below table for the definition of the colours.

![Table 2.1](/Source/WorkflowViz/Doc/media/Table21.png)

-   Grey items are not run. Two special cases are Settings- and Runscripts-batchfiles. The "00 Settings.bat" batchfiles contain settings, do not modify data themselves and don't generate a logfile. They are important for the overview, but because they do not manipulate data they're shown in a grey colour. The same holds for "Runscripts.bat" batchfiles. These can be used to run the batchfiles of a subworkflow, but can also be skipped, for example when another Runscripts batchfile is already executed from a higher level.
-   Green items show workflows or batchfiles that are run completely and without error messages. Also the corresponding logfiles are created after the batchfiles.
-   The colour yellow indicated that subworkflows are run partially, underlying batchfiles will be shown in grey. It depends on the specific workflow what this means. When it contains optional parts there's no problem. It is possible to define in the WorkflowViz-batchfile via the EXCLUDESTRINGS-parameter that certain batchfiles should not be shown in the graph.
-   The colour orange means that a batchfile is modified after the corresponding logfile is created in an earlier run. This earlier run might not be up to date anymore.
-   The colour red indicates that an error is found in the logfile(s) of one or more batchfiles that are part of a subworkflow.

![Figure 2.1](/Source/WorkflowViz/Doc/media/Image21.png)

Figure 2.1 Example graph of a workflow without issues in its subworkflows

![Figure 2.2](/Source/WorkflowViz/Doc/media/Image22.png)

Figure 2.2 Example graph (at highest level) of workflow with issues in its subworkflows

As described above some issues have been created to illustrate how WorkflowViz works. See the result of running the tool in figure 2.2 (level 1) and figure 2.3 (level 2). The differences between figure 2.1 and 2.2 are explained below:

-   Workflow ORG:
    -   An error occurred in a batchfile in subworkflow "02 Clipmodel". This is indicated by a red status colour of and inside workflow ORG.
-   Workflow BASIS0:
    -   Some batchfile is not run within subworkflow "02 DRN-correctie". This is indicated by a yellow subworkflow and a grey batchfile at a deeper level.
    -   A batchfile in subworkflow "04 RIV-correctie" is run later than the batchfiles in "05 OLF-correctie". Possibly the results are out of date. This is shown by a red arrow between both subworkflows and also between workflows BASIS0 and BASIS1. In a tooltip[^1] above this arrow the relevant dates can be shown.
-   Workflow BASIS1:
    -   A batchfile inside workflow BASIS1 is modified after being run earlier. The results of the earlier run are possibly out of date. This is indicated by an orange colour of the subworkflow and the batchfile.

![Figure 2.3](/Source/WorkflowViz/Doc/media/Image23.png)

Figure 2.3 Example graph (at level 2) of workflow with issues

# WorkflowViz options
WorkflowViz offers several options for visualization of the graphs:
-   *Exclude substrings* for skipping subworkflows and/or batchfiles.\
    When these strings are present in filenames of batchfiles and/or directory names of subworkflows then they're not shown in the graphs.
-   *Workflow order strings*\
    When the order of subworkflows is incorrectly shown, with this option an alternative (partial) order can be specified.
-   *Visualisation level*\
    This is the number of subworkflow levels that is shown in one graph (default 2).
-   *Recursion level*\
    This is the maximum level for which subdirectory's inside the specified WORKIN-directory will be read. As a default this is 1, for a quick overview.
-   *Runscripts modus on/off*\
    When this modus is on, the special Runscripts batchfiles of SIF will be shown above the corresponding subworkflows and determine the order of subworkflows.
-   *Ignore edge-check*\
    With this option no checks are performed for the order in which scripts are run (as defined by the logfile dates). It is still checked that the date of a batchfile is older than the date of the corresponding logfile.
-   *Show toplevel batchfiles*\
    As a default no batchfiles are shown in the first graph (at the highest level). With this option they will be shown.
-   *Show result*\
    With this option the resulting HTML-page is opened directly after finishing the tool.
-   *Dot path*\
    For directed graphs dot.exe is used, a part of Graphviz. A version of Graphviz is distributed with WorkflowViz. With this option another version of path can be defined.
-   *Dot options*\
    With this option options of Graphviz can be specified for dot.exe, for example '-Gdpi=300' for enlarging the resolution of the result. See the Graphviz website for details: [http://www.graphviz.org](http://www.graphviz.org/).

Besides the command-line options there are several settings that can be defined in the separate WorkflowViz config file (Bin\\WorkflowViz.exe.config), like:
-   DotPath: a default path for dot.exe
-   Node_FontSize: fontsize for nodes, for larger/smaller graphs
-   Node_Margin: margin around text inside nodes, for larger/smaller graphs
-   Fontname: fontname, like arial, calibri, etc.
-   LogErrorString: the text in logfiles that indicates an error occurred.

# Usage of WorkflowViz
WorkflowViz should be started from the command-line, which also allows to specify relevant parameters, like input path, result path and some options, like described above. When the WorkflowViz executable is executed without parameters, the command-line syntax, the possible options and an example command-line are described briefly. It is advised to run WorkflowViz via a batchfile. In the [SIF-basis distribution](https://github.com/SIF-framework/SIF-basis/tree/main/Scripts) such a batchfile is available in which, conform the SIF-concept, all settings are defined in the top part of this batchfile, including a short description and which will write a logfile with possible error messages.

Ensure that the environment variable DOTEXE in this batchfile refers to the right absolute or relative directory with dot.exe. Optionally use the variable EXCLUDESTRINGS to define substrings of workflows or batchfiles that should be skipped, e.g. for IMFcreate or PLOT which are often optional in workflows. Or raise the RECURSIONLEVEL when more detail is requested.

In the Test-directory two example batchfiles are present for a test model. A model variant is present with successfully executed batchfiles and a variant is present with errors and/or inconsistencies in batchfiles. Both tests can simply be run by starting the corresponding batchfiles "WorkflowViz example?.bat". When finished, automatically a browser should be opened with the resulting graphs.

The graphs can be viewed with normal browser functionality. It is then possible to zoom or pan when the graphs are too large to fit the screen. Often there is a keyboard shortcut for zooming back to 100% (Ctrl-0 in Edge).

When in an HTML-graph, the cluster of a subworkflow (outside the nodes) is clicked, a graph for this subworkflow is shown. See figure 2.3. When a node is clicked the directory with batchfiles and logfiles is opened. When the initial HTML-page is opened with Internet Explorer, Windows Explorer is opened. For other browsers, the directory is shown inside the browser.

# WorkflowViz Licence
WorkflowViz is an open source tool that can be used and distributed as specified in the GNU General Public License v3.0[^2] (GPLv3) or later. WorkflowViz makes use of Graphviz, maar is not linked to it[^3]. The licences of Graphviz can be found under the Bin-directory.

Below the licence notice of WorkflowViz conform the GPLv3 is shown. See the file LICENSE.txt for the full text of the GPLv3.

All rights to this software and documentation, including intellectual property rights, are owned by Sweco Nederland B.V. See the related subdirectories for license notices of used external code or libraries.

WorkflowViz is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

WorkflowViz is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with WorkflowViz. If not, see https://www.gnu.org/licenses.

[^1]: This tooltip is only shown when the mouse cursor is exactly above the arrow and no other objects in the HTML file are selected. Alternatively the dates can be checked in the directories themselves.
[^2]: See <https://www.gnu.org/licenses/gpl-3.0.nl.html>
[^3]: Graphviz can be distributed under the Common Public License Version 1.0 (see graphviz.org).
