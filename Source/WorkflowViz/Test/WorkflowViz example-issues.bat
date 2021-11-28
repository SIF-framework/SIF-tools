@ECHO OFF
REM ******************************************
REM * SIF-basis (Sweco)                      *
REM * Version 1.2.0 November 2021            *
REM *                                        *
REM * WorkflowViz.bat                        *
REM * DESCRIPTION                            * 
REM *   For visualization of SIF-workflows   *
REM * AUTHOR(S): Koen van der Hauw (Sweco)   *
REM ******************************************
CALL :Initialization

REM ********************
REM * Script variables *
REM ********************
REM WORKINPATH:     Path to WORKIN subdirectory to create WorkflowViz-graphs for
REM EXCLUDESTRINGS: Comma-seperated list of string in subdirectories or filenames to exclude
REM WORKFLOWORDER:  Comma-seperated substrings that define workflow (partial) order (e.g. BASISDATA to ensure that if comes first, BASIS0)
REM VISUALIZELEVEL: Visualization level: maximum number of workflow levels to show in one diagram, or leave empty for default (2)
REM RECURSIONLEVEL: Recursion level: subworkflow depth to generate diagrams for, or leave empty for default (1)
REM RUNSCRIPTMODE:  Specify (with value 1) if Runscriptsmode should be enabled: show Runscripts batchfiles above toplevel workflows, otherwise Runscripts are shown like normal batchfiles
REM ISTOPLEVBATCH:  Specify (with value 1) if batchfiles should be shown for the highest level graph. As a default batchfiles are not shown in the graph at the highest level for more clarity.
REM SKIPEDGECHECK:  Specify (with value 1) if check for order inconsistencies of batchfiles and subworkflows should be skipped (and not visualized with a red arrow), or leave empty to check ordering
REM ISRESULTSHOWN:  Specify (with value 1) if result should be shown after succesful run, or leave empty to skip
REM RESULTPATH:     Path to write results
SET WORKINPATH=Input\Model-issues\WORKIN
SET EXCLUDESTRINGS=
SET WORKFLOWORDER=BASISDATA,ORG
SET VISUALIZELEVEL=2
SET RECURSIONLEVEL=1
SET RUNSCRIPTMODE=1
SET SKIPEDGECHECK=
SET ISTOPLEVBATCH=
SET ISRESULTSHOWN=1
SET RESULTPATH=Output\Model-issues

REM WORKFLOWVIZEXE: Path to WorkflowViz.exe, absolute or relative to the location of this batchfile
REM DOTEXE:         Path to dot.exe, absolute or relative to path with WorkflowViz executable
REM DOTOPTIONS:     Specify command-line options to run dot.exe, use full substring of command-line for dot.exe, e.g. -Gdpi=300. Check dot manual for all options. Do not add double quotes around options string.
SET WORKFLOWVIZEXE=..\Bin\WorkflowViz.exe
SET DOTEXE=graphviz-2.49.1\dot.exe
SET DOTOPTIONS=

REM *********************
REM * Derived variables *
REM *********************
SET SCRIPTNAME=%~n0
SET LOGFILE="%SCRIPTNAME%.log"

REM *******************
REM * Script commands *
REM *******************
SETLOCAL EnableDelayedExpansion

TITLE SIF-basis: %SCRIPTNAME%

SET MSG=Starting script '%SCRIPTNAME%' ...
ECHO %MSG%
ECHO %MSG% > %LOGFILE%

IF NOT DEFINED WORKINPATH (
  ECHO: 
  SET MSG=Please define WORKINPATH variable to specfiy WORKIN-directory to create workflow diagrams for
  ECHO !MSG!
  ECHO !MSG! >> %LOGFILE%
  GOTO error
)

IF NOT EXIST "%WORKFLOWVIZEXE%" (
  SET MSG=WorkflowViz executable not found: %WORKFLOWVIZEXE%
  ECHO !MSG!
  ECHO !MSG! >> %LOGFILE%
  GOTO error
)

SET MSG=  running WorkflowViz for '%WORKINPATH%' ...
ECHO %MSG%
ECHO %MSG% >> %LOGFILE%

SET EXOPTION=
SET WOOPTION=
SET VLOPTION=
SET RLOPTION=
SET RMOPTION=
SET BTOPTION=
SET SEOPTION=
SET OROPTION=
SET DOTOPTION=
SET DOTEXEOPTION=
IF DEFINED EXCLUDESTRINGS SET EXOPTION=/ex:%EXCLUDESTRINGS%
IF DEFINED WORKFLOWORDER SET WOOPTION=/wo:%WORKFLOWORDER%
IF DEFINED RECURSIONLEVEL SET RLOPTION=/rl:%RECURSIONLEVEL%
IF DEFINED VISUALIZELEVEL SET VLOPTION=/vl:%VISUALIZELEVEL%
IF "%RUNSCRIPTMODE%"=="1" SET RMOPTION=/rm
IF "%ISTOPLEVBATCH%"=="1" SET BTOPTION=/bt
IF "%SKIPEDGECHECK%"=="1" SET SEOPTION=/se
IF "%ISRESULTSHOWN%"=="1" SET OROPTION=/or
IF DEFINED DOTOPTIONS SET DOTOPTION=/do:"%DOTOPTIONS%"
IF DEFINED DOTEXE SET DOTEXEOPTION=/dot:"%DOTEXE%"
ECHO "%WORKFLOWVIZEXE%" %OROPTION% %VLOPTION% %RLOPTION% %RMOPTION% %BTOPTION% %EXOPTION% %SEOPTION% %WOOPTION% %DOTEXEOPTION% %DOTOPTION% "%WORKINPATH%" "%RESULTPATH%" >> %LOGFILE%
"%WORKFLOWVIZEXE%" %OROPTION% %VLOPTION% %RLOPTION% %RMOPTION% %BTOPTION% %EXOPTION% %SEOPTION% %WOOPTION% %DOTEXEOPTION% %DOTOPTION% "%WORKINPATH%" "%RESULTPATH%" >> %LOGFILE%
IF ERRORLEVEL 1 GOTO error

:success
SET MSG=Script finished, see "%~n0.log"
ECHO %MSG%
ECHO %MSG% >> %LOGFILE%
REM Set errorlevel 0 for higher level scripts
CMD /C "EXIT /B 0"
GOTO exit

:error
ECHO:
SET MSG=AN ERROR HAS OCCURRED^^! Check logfile "%~n0.log"
ECHO %MSG%
ECHO %MSG% >> %LOGFILE%
REM Set errorlevel for higher level scripts
CMD /C "EXIT /B 1"
GOTO exit

REM FUNCTION: Intialize script. To use: "CALL :Initialization", without arguments
:Initialization
  COLOR 70
  GOTO:EOF

:exit
ECHO:
IF "%NOPAUSE%"=="" PAUSE
