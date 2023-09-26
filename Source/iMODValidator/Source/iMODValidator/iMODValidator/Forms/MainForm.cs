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
using Sweco.SIF.GIS;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Exceptions;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweco.SIF.iMODValidator.Forms
{
    public partial class MainForm : Form
    {
        protected string ProductVersionDate = "31-05-2018";

        protected SIFTool ToolInstance { get; set; }
        protected string SettingsFilename { get; }

        protected bool isMultipleDataGridCheckActive = false;
        protected DataGridViewSelectedRowCollection selectedDataGridRows = null;

        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(SIFTool sifTool, string settingsFilename = null)
        {
            InitializeComponent();

            this.ToolInstance = sifTool;
            this.SettingsFilename = settingsFilename;

            Text = Application.ProductName + " " + sifTool.ToolVersion;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            resultTextBox.Clear();

            // Retrieve loglevel
            Log log = null;
            Validator validator = null;
            try
            {
                validator = new Validator(log);
                validator.CultureInfo = SIFTool.EnglishCultureInfo;

                // Retrieve and check settings
                if (!RetrieveValidatorSettings(validator))
                {
                    return;
                }

                log = new Log(LogMessageListener);
                log.ListenerLogLevels.Add(Log.ParseLogLevelString(logLevelComboBox.SelectedItem.ToString()));
                log.Filename = Path.Combine(validator.OutputPath, Application.ProductName + "_" + DateTime.Now.ToString("dd-MM-yyyy HH:mm").Replace(":", ".") + ".log");
                validator.Log = log;

                // Show first tab page, with output window, during run
                tabControl1.SelectedTab = tabControl1.TabPages[0];

                // Start validation
                validator.Run();
            }
            catch (AbortException)
            {
                ExceptionUtils.HandleAbortException(log);
            }
            catch (ToolException ex)
            {
                ExceptionUtils.HandleValidatorException(ex, log);
            }
            catch (OutOfMemoryException ex)
            {
                log.AddMessage(LogLevel.Debug, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.");
                ExceptionUtils.HandleUnexpectedException(ex, log);
            }
            catch (Exception ex)
            {
                ExceptionUtils.HandleUnexpectedException(ex, log);
            }
            finally
            {
                Activate();

                // try to log results
                if ((validator != null) && (validator.OutputPath != null))
                {
                    log.WriteLogFile();
                }

                resultTextBox.ScrollToCaret();
                validator = null;
            }
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            HelpForm helpForm = new HelpForm();
            helpForm.SetHelpText(
                "iMODValidator\r\n" +
                "Version " + SIFTool.Instance.ToolVersion + ", " + ProductVersionDate + "\r\n\r\n" +
                "With this tool the inputfiles of a given runfile can be checked for consistency and other errors.\r\n" +
                "Some settings (such as default in- or outputfolder) can be modified in the iMODValidator.xml file.\r\n\r\n" +
                "For questions or remarks:\r\n" +
                "Koen van der Hauw\r\n" +
                "Adviseur Waterbeheer\r\n" +
                "koen.vanderhauw@sweco.nl\r\n" +
                "Sweco Nederland B.V.\r\n" +
                "www.sweco.nl\r\n" +
                "Copyright © 2013-2018, Sweco Nederland B.V.\r\n" +
                "\r\n" +
                "\r\n" +
                "Credits go to:\r\n" +
                "- AZURE Consortium for contributing in the development of the tool:\r\n" +
                "- Json.NET open source library for JSON serialization\r\n" +
                "- NPOI open source library for manipulating Excelfiles \r\n" +
                "");
            helpForm.ShowDialog();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LogMessageListener(string message, bool isEolAdded = true)
        {
            if (isEolAdded)
            {
                message += "\r\n";
            }
            resultTextBox.AppendText(message);
            Application.DoEvents();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Activate();
            if (!DesignMode)
            {
                // Initialize MainForm with settings if not being edited in Visual C# Designer
                InitializeIMODValidator();
            }
        }

        private void InitializeIMODValidator()
        {
            Log log = ToolInstance.Log;

            // Load settings from file or create new settings file if not existing in default location
            try
            {
                iMODValidatorSettingsManager.LoadMainSettings(SettingsFilename);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex);
                resultTextBox.Text += ex.GetBaseException().Message;
            }

            // Set default runfiles
            runfileTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultInputRunfile;
            if ((iMODValidatorSettingsManager.Settings.DefaultOutputFolder != null) && (!iMODValidatorSettingsManager.Settings.DefaultOutputFolder.Equals(string.Empty)))
            {
                outputPathTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultOutputFolder;
            }
            else
            {
                if (!runfileTextBox.Text.Equals(string.Empty))
                {
                    outputPathTextBox.Text = Path.GetDirectoryName(runfileTextBox.Text);
                }
            }

            // Set default surface level file and method
            try
            {
                string surfaceLevelFilename = iMODValidatorSettingsManager.Settings.DefaultSurfaceLevelFilename;
                if (!Path.IsPathRooted(surfaceLevelFilename))
                {
                    surfaceLevelFilename = Path.Combine(Directory.GetCurrentDirectory(), surfaceLevelFilename);
                }
                if (File.Exists(surfaceLevelFilename))
                {
                    surfacelevelFileTextBox.Text = surfaceLevelFilename;
                }
            }
            catch (Exception)
            {
                surfacelevelFileTextBox.Text = "<please enter a valid surfacelevel filename>";
            }
            if (iMODValidatorSettingsManager.Settings.UseSmartSurfaceLevelMethod)
            {
                surfaceLevelSmartRadioButton.Checked = true;
            }
            else if (iMODValidatorSettingsManager.Settings.UseMetaSWAPSurfaceLevelMethod)
            {
                surfacelevelMetaSWAPradioButton.Checked = true;
            }
            else if (iMODValidatorSettingsManager.Settings.UseOLFSurfaceLevelMethod)
            {
                surfacelevelOLFRadioButton.Checked = true;
            }
            else if (iMODValidatorSettingsManager.Settings.UseFileSurfaceLevelMethod)
            {
                if ((surfacelevelFileTextBox.Text != null) && (surfacelevelFileTextBox.Text != string.Empty))
                {
                    surfacelevelFileRadioButton.Checked = true;
                }
                else
                {
                    surfaceLevelSmartRadioButton.Checked = true;
                }
            }
            else
            {
                surfaceLevelSmartRadioButton.Checked = true;
            }

            // Load available checks and check-settings
            try
            {
                CheckManager.Instance.SortChecks();

                foreach (Check check in CheckManager.Instance.Checks)
                {
                    // First try to load settings from file
                    try
                    {
                        iMODValidatorSettingsManager.LoadCheckSettings(check, null, SettingsFilename);
                    }
                    catch (Exception ex)
                    {
                        // Log exeption, but dont show a messagebox
                        ExceptionUtils.HandleValidatorException(ex, log, 0, false);
                        try
                        {
                            // Settingsfile could not be loaded, create default settings for this check in settingsfile
                            log.AddMessage(LogLevel.Debug, "Defaultsettings are created for " + check.Name);
                            iMODValidatorSettingsManager.SaveCheckSettings(check);
                        }
                        catch
                        {
                            ExceptionUtils.HandleValidatorException(ex, log, 0, true);
                        }
                        resultTextBox.Text += log.LogString;
                    }

                    // Show check and check-settings in table
                    AddDataGridCheckRow(check);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read settings: " + ex.GetBaseException(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Set default extent
            urxTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultCustomExtentURX.ToString();
            uryTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultCustomExtentURY.ToString();
            llxTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultCustomExtentLLX.ToString();
            llyTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultCustomExtentLLY.ToString();
            if (iMODValidatorSettingsManager.Settings.UseCustomExtentMethod)
            {
                customExtentRadioButton.Checked = true;
            }
            else if (iMODValidatorSettingsManager.Settings.UsePackageFileExtentMethod)
            {
                packageExtentRadioButton.Checked = true;
            }
            else
            {
                modelExtentRadioButton.Checked = true;
            }

            // Set default outputmethod
            openIMODCheckBox.Checked = iMODValidatorSettingsManager.Settings.IsIMODOpened;
            openExcelCheckBox.Checked = iMODValidatorSettingsManager.Settings.IsExcelOpened;

            // Set default error margin
            float levelErrorMargin = iMODValidatorSettingsManager.Settings.LevelErrorMargin;
            levelErrorMarginTextBox.Text = (levelErrorMargin > 0) ? levelErrorMargin.ToString() : "0";

            // Set default first/maximum timestep
            int firstTimeStep = -1;
            if (int.TryParse(iMODValidatorSettingsManager.Settings.MinTimestep, out firstTimeStep))
            {
                firstTimeStepTextBox.Text = iMODValidatorSettingsManager.Settings.MinTimestep;
            }
            int maxTimeStep = -1;
            if (int.TryParse(iMODValidatorSettingsManager.Settings.MaxTimestep, out maxTimeStep))
            {
                maxTimeStepTextBox.Text = iMODValidatorSettingsManager.Settings.MaxTimestep;
            }

            // Set default logdetail and options
            foreach (string option in SplitValidationrunSettings.RetrieveOptionStrings())
            {
                splitValidationrunOptionComboBox.Items.Add(option);
            }
            splitValidationrunOptionComboBox.SelectedIndex = (int)iMODValidatorSettingsManager.Settings.SplitValidationrunOption;

            // Set default maximum timestep
            int minILAYValue = 1;
            if (int.TryParse(iMODValidatorSettingsManager.Settings.MinILAY, out minILAYValue))
            {
                firstLayerNumberTextBox.Text = iMODValidatorSettingsManager.Settings.MinILAY;
            }
            int maxILAYValue = 999;
            if (int.TryParse(iMODValidatorSettingsManager.Settings.MaxILAY, out maxILAYValue))
            {
                maxLayerNumberTextBox.Text = iMODValidatorSettingsManager.Settings.MaxILAY;
            }

            // Set default minimum summary-IDF cell size
            summaryMinCellSizeTextBox.Text = iMODValidatorSettingsManager.Settings.DefaultSummaryMinCellSize.ToString();

            // Set default IPF-settings
            useSparseMatrixCheckBox.Checked = iMODValidatorSettingsManager.Settings.UseSparseMatrix;
            useIPFWarningForExistingPointCheckBox.Checked = iMODValidatorSettingsManager.Settings.UseIPFWarningForExistingPoints;
            useIPFWarningForColumnMismatchCheckBox.Checked = iMODValidatorSettingsManager.Settings.UseIPFWarningForColumnMismatch;
            addRelativePathIMFCheckBox.Checked = iMODValidatorSettingsManager.Settings.IsRelativePathIMFAdded;

            // Set default logdetail and options
            logLevelComboBox.Items.Add(LogLevel.Error.ToString());
            logLevelComboBox.Items.Add(LogLevel.Warning.ToString());
            logLevelComboBox.Items.Add(LogLevel.Info.ToString());
            logLevelComboBox.Items.Add(LogLevel.Debug.ToString());
            logLevelComboBox.Items.Add(LogLevel.Trace.ToString());
            logLevelComboBox.SelectedIndex = 1;
        }

        private void AddDataGridCheckRow(Check check)
        {
            DataGridViewRow row = new DataGridViewRow();
            DataGridViewCell checkBoxCell = new DataGridViewCheckBoxCell();
            DataGridViewCell nameTextBoxCell = new DataGridViewTextBoxCell();
            nameTextBoxCell.Value = check.Name;
            DataGridViewCell descriptionTextBoxCell = new DataGridViewTextBoxCell();
            descriptionTextBoxCell.Value = check.Description;
            check.IsActive = ((check.Settings != null) && check.Settings.IsActiveDefault);
            checkBoxCell.Value = check.IsActive;
            nameTextBoxCell.Style.ForeColor = Color.Black;
            descriptionTextBoxCell.Style.ForeColor = Color.Black;
            row.Cells.Add(checkBoxCell);
            row.Cells.Add(nameTextBoxCell);
            row.Cells.Add(descriptionTextBoxCell);
            row.Tag = check;
            checkDataGridView.Rows.Add(row);
        }

        protected virtual bool IsActionSelected()
        {
            return isModelValidatedCheckBox.Checked;
        }

        protected virtual bool RetrieveValidatorSettings(Validator validator)
        {
            CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

            validator.NoDataValue = iMODValidatorSettingsManager.Settings.DefaultNoDataValue;

            // Retrieve and check selected runfile filename
            try
            {
                if (!IsActionSelected())
                {
                    MessageBox.Show("Please specify an action to perform");
                    return false;
                }

                if (File.Exists(runfileTextBox.Text))
                {
                    validator.RUNFilename = runfileTextBox.Text;
                }
                else
                {
                    runfileTextBox.Focus();
                    MessageBox.Show("Runfile doesn't exist, please specifiy an existing runfilename");
                    return false;
                }
            }
            catch (Exception)
            {
                runfileTextBox.Focus();
                MessageBox.Show("Please enter a valid runfilename");
                return false;
            }

            // Retrieve defined outputpath
            try
            {
                validator.OutputPath = Path.Combine(outputPathTextBox.Text, iMODValidatorSettingsManager.Settings.TooloutputSubfoldername);
            }
            catch (Exception)
            {
                outputPathTextBox.Focus();
                MessageBox.Show("Please enter a valid outputpath");
                return false;
            }

            // Retrieve and check output settings
            validator.IsIMODOpened = openIMODCheckBox.Checked;

            validator.IsResultSheetOpened = openExcelCheckBox.Checked;
            // Retrieve and check extent settings
            if (customExtentRadioButton.Checked)
            {
                float llx = float.NaN;
                float lly = float.NaN;
                float urx = float.NaN;
                float ury = float.NaN;
                try
                {
                    llx = float.Parse(llxTextBox.Text, englishCultureInfo);
                    lly = float.Parse(llyTextBox.Text, englishCultureInfo);
                    urx = float.Parse(urxTextBox.Text, englishCultureInfo);
                    ury = float.Parse(uryTextBox.Text, englishCultureInfo);
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid extent coordinate");
                    tabControl1.SelectTab(1);
                    llxTextBox.Focus();
                    return false;
                }
                validator.ExtentType = ExtentMethod.CustomExtent;
                validator.Extent = new Extent(llx, lly, urx, ury);
            }
            else if (modelExtentRadioButton.Checked)
            {
                validator.ExtentType = ExtentMethod.ModelExtent;
            }
            else
            {
                validator.ExtentType = ExtentMethod.PackageFileExtent;
            }

            // Retrieve and check surfacelevel settings
            if (surfaceLevelSmartRadioButton.Checked)
            {
                validator.SurfaceLevelMethod = SurfaceLevelMethod.Smart;
                validator.SurfaceLevelFilename = null;
            }
            else if (surfacelevelMetaSWAPradioButton.Checked)
            {
                validator.SurfaceLevelMethod = SurfaceLevelMethod.UseMetaSWAP;
                validator.SurfaceLevelFilename = null;
            }
            else if (surfacelevelOLFRadioButton.Checked)
            {
                validator.SurfaceLevelMethod = SurfaceLevelMethod.UseOLF;
                validator.SurfaceLevelFilename = null;
            }
            else if (surfacelevelFileRadioButton.Checked)
            {
                if (surfacelevelFileTextBox.Text.Trim().Equals(string.Empty) || File.Exists(surfacelevelFileTextBox.Text))
                {
                    validator.SurfaceLevelMethod = SurfaceLevelMethod.UseFilename;
                    validator.SurfaceLevelFilename = surfacelevelFileTextBox.Text;
                }
                else
                {
                    throw new Exception("Specified surface level file doesn't exist: " + surfacelevelFileTextBox.Text);
                }
            }
            else
            {
                // default: code should actually never be reached
                validator.SurfaceLevelMethod = SurfaceLevelMethod.Smart;
                validator.SurfaceLevelFilename = null;
            }

            // Retrieve and check error margin settings
            float levelErrorMargin = float.NaN;
            if (float.TryParse(levelErrorMarginTextBox.Text.Replace(",", "."), NumberStyles.Any, validator.CultureInfo, out levelErrorMargin))
            {
                if (levelErrorMargin < 0)
                {
                    MessageBox.Show("Level error margin should be positive");
                    tabControl1.SelectTab(2);
                    levelErrorMarginTextBox.Focus();
                    return false;
                }
                validator.LevelErrorMargin = levelErrorMargin;
            }
            else
            {
                MessageBox.Show("Level error margin is incorrect");
                tabControl1.SelectTab(2);
                levelErrorMarginTextBox.Focus();
                return false;
            }

            // Retrieve and check min/max timestep setting
            int firstTimeStep = 0;
            if (!firstTimeStepTextBox.Text.Equals(string.Empty))
            {
                if (int.TryParse(firstTimeStepTextBox.Text, out firstTimeStep))
                {
                    if (firstTimeStep < 0)
                    {
                        MessageBox.Show("Maximum timestep should be positive or zero");
                        tabControl1.SelectTab(2);
                        firstTimeStepTextBox.Focus();
                        return false;
                    }
                    validator.MinKPER = firstTimeStep;
                }
                else
                {
                    MessageBox.Show("Maximum timestep value is incorrect");
                    tabControl1.SelectTab(2);
                    firstTimeStepTextBox.Focus();
                    return false;
                }
            }
            int maxTimeStep = int.MaxValue;
            if (!maxTimeStepTextBox.Text.Equals(string.Empty))
            {
                if (int.TryParse(maxTimeStepTextBox.Text, out maxTimeStep))
                {
                    if (maxTimeStep <= 0)
                    {
                        MessageBox.Show("Maximum timestep should be positive (larger than 0)");
                        tabControl1.SelectTab(2);
                        maxTimeStepTextBox.Focus();
                        return false;
                    }
                    validator.MaxKPER = maxTimeStep;
                }
                else
                {
                    MessageBox.Show("Maximum timestep value is incorrect");
                    tabControl1.SelectTab(2);
                    maxTimeStepTextBox.Focus();
                    return false;
                }
            }

            try
            {
                validator.SplitValidationrunOption = SplitValidationrunSettings.ParseOptionString(splitValidationrunOptionComboBox.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("SplitValidationrun option is incorrect:" + ex.GetBaseException().Message);
                tabControl1.SelectTab(2);
                splitValidationrunOptionComboBox.Focus();
                return false;
            }

            // Retrieve and check ilay settings
            int minILAY = 1;
            if (!firstLayerNumberTextBox.Text.Equals(string.Empty))
            {
                if (int.TryParse(firstLayerNumberTextBox.Text, out minILAY))
                {
                    if (minILAY < 1)
                    {
                        MessageBox.Show("First layer number should be one or higher");
                        tabControl1.SelectTab(2);
                        firstLayerNumberTextBox.Focus();
                        return false;
                    }
                    validator.MinILAY = minILAY;
                }
                else
                {
                    MessageBox.Show("First layer number value is incorrect");
                    tabControl1.SelectTab(2);
                    firstLayerNumberTextBox.Focus();
                    return false;
                }
            }

            // Retrieve and check ilay settings
            int maxILAY = 1;
            if (!maxLayerNumberTextBox.Text.Equals(string.Empty))
            {
                if (int.TryParse(maxLayerNumberTextBox.Text, out maxILAY))
                {
                    if (maxILAY < minILAY)
                    {
                        MessageBox.Show("Last layer number should be above or equal to first layer number");
                        tabControl1.SelectTab(2);
                        maxLayerNumberTextBox.Focus();
                        return false;
                    }
                    validator.MaxILAY = maxILAY;
                }
                else
                {
                    MessageBox.Show("Last layer number value is incorrect");
                    tabControl1.SelectTab(2);
                    maxLayerNumberTextBox.Focus();
                    return false;
                }
            }

            validator.UseSparseGrids = useSparseMatrixCheckBox.Checked;
            validator.IsRelativePathIMFAdded = addRelativePathIMFCheckBox.Checked;
            iMODValidatorSettingsManager.Settings.UseIPFWarningForExistingPoints = useIPFWarningForExistingPointCheckBox.Checked;
            iMODValidatorSettingsManager.Settings.UseIPFWarningForColumnMismatch = useIPFWarningForColumnMismatchCheckBox.Checked;


            // Retrieve and check summary min cellsize settings
            float summaryMinCellSize = float.NaN;
            if (float.TryParse(summaryMinCellSizeTextBox.Text.Replace(",", "."), NumberStyles.Any, validator.CultureInfo, out summaryMinCellSize))
            {
                if (summaryMinCellSize < 0)
                {
                    MessageBox.Show("Minimum summary-IDF cellsize should be positive");
                    tabControl1.SelectTab(2);
                    summaryMinCellSizeTextBox.Focus();
                    return false;
                }
                validator.SummaryMinCellsize = summaryMinCellSize;
            }
            else
            {
                MessageBox.Show("Minimum summary-IDF cellsize is incorrect");
                tabControl1.SelectTab(2);
                summaryMinCellSizeTextBox.Focus();
                return false;
            }

            // Retrieve other settings
            validator.IsModelValidated = isModelValidatedCheckBox.Checked;

            return true;
        }

        private void runfileButton_Click(object sender, EventArgs e)
        {
            String filename = null;
            FileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Choose a runfile";
            fileDialog.CheckFileExists = true;
            if (runfileTextBox.Text.Length > 0)
            {
                try
                {
                    fileDialog.InitialDirectory = Path.GetDirectoryName(runfileTextBox.Text);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            else
            {
                try
                {
                    if (File.Exists(iMODValidatorSettingsManager.Settings.DefaultInputRunfile))
                    {
                        fileDialog.InitialDirectory = Path.GetDirectoryName(iMODValidatorSettingsManager.Settings.DefaultInputRunfile);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            fileDialog.Filter = "Runfiles (*.RUN)|*.RUN";
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (fileDialog.FileName != null)
                {
                    filename = fileDialog.FileName;
                }
                runfileTextBox.Text = filename;
                outputPathTextBox.Text = Path.GetDirectoryName(filename);
                runfileTextBox.Focus();
            }
        }

        private void outputFolderButton_Click(object sender, EventArgs e)
        {
            String foldername = null;
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            if (outputPathTextBox.Text.Equals(String.Empty))
            {
                folderDialog.SelectedPath = iMODValidatorSettingsManager.Settings.DefaultOutputFolder;
            }
            else
            {
                if (Directory.Exists(outputPathTextBox.Text))
                {
                    folderDialog.SelectedPath = Path.GetDirectoryName(outputPathTextBox.Text);
                }
            }
            DialogResult result = folderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (folderDialog.SelectedPath != null)
                {
                    foldername = folderDialog.SelectedPath;
                }
                outputPathTextBox.Text = foldername;
                outputPathTextBox.Focus();
            }
        }

        private void surfacelevelFileButton_Click(object sender, EventArgs e)
        {
            String filename = null;
            FileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Choose a surface level file";
            fileDialog.CheckFileExists = true;
            if (surfacelevelFileTextBox.Text.Length > 0)
            {
                try
                {
                    fileDialog.InitialDirectory = Path.GetDirectoryName(surfacelevelFileTextBox.Text);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            else
            {
                fileDialog.InitialDirectory = iMODValidatorSettingsManager.Settings.DefaultInputRunfile;
            }
            fileDialog.Filter = "IDF grid (*.IDF)|*.IDF|ASCI grid (*.ASC)|*.ASC|All files (*.*)|*.*";
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (fileDialog.FileName != null)
                {
                    filename = fileDialog.FileName;
                }
                surfacelevelFileTextBox.Text = filename;
                surfacelevelFileRadioButton.Checked = true;
            }
        }

        private void customExtentRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            lllabel.Enabled = customExtentRadioButton.Checked;
            urlabel.Enabled = customExtentRadioButton.Checked;
            llxLabel.Enabled = customExtentRadioButton.Checked;
            llyLabel.Enabled = customExtentRadioButton.Checked;
            urxLabel.Enabled = customExtentRadioButton.Checked;
            uryLabel.Enabled = customExtentRadioButton.Checked;
            llxTextBox.Enabled = customExtentRadioButton.Checked;
            llyTextBox.Enabled = customExtentRadioButton.Checked;
            urxTextBox.Enabled = customExtentRadioButton.Checked;
            uryTextBox.Enabled = customExtentRadioButton.Checked;
        }

        private void checkDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            Check selectedCheck = (Check)checkDataGridView.CurrentRow.Tag;

            checkPropertyGrid.SelectedObject = selectedCheck.Settings;

            // Store selected rows, but prevent single row selection when clicked on a checkbox
            if (!isMultipleDataGridCheckActive)
            {
                selectedDataGridRows = checkDataGridView.SelectedRows;
            }
        }

        private void checkDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Check if a checkbox cell was changed
            if (e.ColumnIndex == 0 && e.RowIndex != -1)
            {
                bool isChecked = (bool)checkDataGridView.Rows[e.RowIndex].Cells[0].Value;

                if (selectedDataGridRows != null)
                {
                    // Apply checkvalue to all selected rows if the changed row was one of the selected rows
                    if (selectedDataGridRows.Contains(checkDataGridView.Rows[e.RowIndex]))
                    {
                        // Set all selected rows
                        foreach (DataGridViewRow row in selectedDataGridRows)
                        {
                            row.Cells[0].Value = isChecked;
                            row.Selected = true;
                            ((Check)row.Tag).IsActive = isChecked;
                        }
                    }
                    else
                    {
                        ((Check)checkDataGridView.CurrentRow.Tag).IsActive = isChecked;
                    }
                }
                else
                {
                    ((Check)checkDataGridView.CurrentRow.Tag).IsActive = isChecked;
                }
            }
        }

        private void checkDataGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Fire EndEdit event to force CellValueChanged event for a click on column of checkbox
            if (e.ColumnIndex == 0 && e.RowIndex != -1)
            {
                checkDataGridView.EndEdit();
            }
            isMultipleDataGridCheckActive = false;
        }

        private void checkDataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Save current rowselection when the user clicks in a checkbox cell
            if (e.ColumnIndex == 0 && e.RowIndex != -1)
            {
                selectedDataGridRows = checkDataGridView.SelectedRows;
                isMultipleDataGridCheckActive = true;
            }
            else
            {
                selectedDataGridRows = null;
            }
        }

        private void checkDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Save current rowselection when the user clicks in a checkbox cell
            if ((checkDataGridView.CurrentCell.ColumnIndex == 0) && (checkDataGridView.CurrentCell.RowIndex != -1))
            {
                checkDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        protected void AbortChecks()
        {
            if (!CheckManager.Instance.IsAbortRequested)
            {
                DialogResult result = MessageBox.Show("Escape was pressed. Do you wish to abort?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.Yes)
                {
                    resultTextBox.Text += "\r\nWaiting for current check to stop ...\r\n";
                    CheckManager.Instance.AbortActions();
                }
            }
        }

        private void checkPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Check selectedCheck = (Check)checkDataGridView.CurrentRow.Tag;
            iMODValidatorSettingsManager.SaveCheckSettings(selectedCheck, null, SettingsFilename);
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)27)) // ESC-key
            {
                AbortChecks();
            }
        }

        private void resultTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)27)) // ESC-key
            {
                AbortChecks();
            }
        }

        private void tabControl1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)27)) // ESC-key
            {
                AbortChecks();
            }
        }

        private void startButton_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)27)) // ESC-key
            {
                AbortChecks();
            }
        }
    }
}
