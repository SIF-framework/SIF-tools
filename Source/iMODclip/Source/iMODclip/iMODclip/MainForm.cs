// iMODclip is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODclip.
// 
// iMODclip is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODclip is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODclip. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweco.SIF.iMODclip
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            outputFolderTextBox.Text = Properties.Settings.Default.DefaultOutputFolder;
            inputFolderTextBox.Text = Properties.Settings.Default.DefaultInputFolder;
            if (!Properties.Settings.Default.LLX.Equals(float.NaN))
            {
                boundaryLLXTextBox.Text = Properties.Settings.Default.LLX.ToString();
            }
            if (!Properties.Settings.Default.LLY.Equals(float.NaN))
            {
                boundaryLLYTextBox.Text = Properties.Settings.Default.LLY.ToString();
            }
            if (!Properties.Settings.Default.URX.Equals(float.NaN))
            {
                boundaryURXTextBox.Text = Properties.Settings.Default.URX.ToString();
            }
            if (!Properties.Settings.Default.URY.Equals(float.NaN))
            {
                boundaryURYTextBox.Text = Properties.Settings.Default.URY.ToString();
            }
        }

        public void ShowHelpForm(string helpString)
        {
            Form helpForm = new Form();
            System.Windows.Forms.TextBox helpTextBox;
            System.Windows.Forms.Button okButton;
            helpTextBox = new System.Windows.Forms.TextBox();
            okButton = new System.Windows.Forms.Button();
            SuspendLayout();

            // 
            // helpTextBox
            // 
            helpTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            helpTextBox.BackColor = System.Drawing.SystemColors.Window;
            helpTextBox.ForeColor = System.Drawing.Color.Black;
            helpTextBox.Location = new System.Drawing.Point(12, 12);
            helpTextBox.Multiline = true;
            helpTextBox.Name = "helpTextBox";
            helpTextBox.ReadOnly = true;
            helpTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            helpTextBox.Size = new System.Drawing.Size(581, 304);
            helpTextBox.TabIndex = 1;
            helpTextBox.WordWrap = false;
            // 
            // okButton
            // 
            okButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            okButton.Location = new System.Drawing.Point(265, 322);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += new System.EventHandler(delegate (object sender, EventArgs e)
            {
                helpForm.Hide();
            });

            okButton.KeyDown += new System.Windows.Forms.KeyEventHandler(delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    helpForm.Hide();
                }
            });

            // 
            // HelpForm
            // 
            helpForm.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            helpForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            helpForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(78)))));
            helpForm.CancelButton = okButton;
            helpForm.ClientSize = new System.Drawing.Size(605, 357);
            helpForm.MinimumSize = helpForm.Size;
            //            helpForm.Icon = (System.Drawing.Icon)Resources.ResourceManager.GetObject("SwecoIcon");

            helpForm.Controls.Add(okButton);
            helpForm.Controls.Add(helpTextBox);
            helpForm.Name = "HelpForm";
            helpForm.Text = "Help";
            helpForm.ResumeLayout(false);
            helpForm.PerformLayout();
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            SIFTool sifTool = new SIFTool(null);
            ShowHelpForm(sifTool.ToolName + " " + sifTool.ToolVersion + "\r\n\r\n" +
                "With this tool all ASC- and/or iMOD's IDF-files in a given folder can be clipped to a given boundary\r\n" +
                "It can be specified if subdirectories should be processed recursively. Only ASC- and IDF-files will be\r\n" +
                "clipped. All other files in the specified directories will be copied. If a file already exists in the\r\n" +
                "outputdirectory, it will be skipped.\r\n\r\n" +
                "Some settings (such as default in- or outputfolder) can be modified in the iMODclip.exe.config XML file.\r\n\r\n");
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void InputFolderButton_Click(object sender, EventArgs e)
        {
            String foldername = null;
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            if (inputFolderTextBox.Text.Equals(String.Empty))
            {
                folderDialog.SelectedPath = Properties.Settings.Default.DefaultInputFolder;
            }
            else
            {
                folderDialog.SelectedPath = Path.GetDirectoryName(inputFolderTextBox.Text);
            }
            DialogResult result = folderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (folderDialog.SelectedPath != null)
                {
                    foldername = folderDialog.SelectedPath;
                }
                inputFolderTextBox.Text = foldername;
            }
        }

        private void OutputFolderButton_Click(object sender, EventArgs e)
        {
            String foldername = null;
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            if (outputFolderTextBox.Text.Equals(String.Empty))
            {
                folderDialog.SelectedPath = Properties.Settings.Default.DefaultOutputFolder;
            }
            else
            {
                folderDialog.SelectedPath = Path.GetDirectoryName(outputFolderTextBox.Text);
            }
            DialogResult result = folderDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (folderDialog.SelectedPath != null)
                {
                    foldername = folderDialog.SelectedPath;
                }
                outputFolderTextBox.Text = foldername;
            }
        }

        protected void StartButton_Click(object sender, EventArgs e)
        {
            resultTextBox.Clear();
            Log log = new Log(LogMessageListener);
            try
            {
                string commandLine = Utilities.EnsureDoubleQuotes(inputFolderTextBox.Text) + " " + Utilities.EnsureDoubleQuotes(outputFolderTextBox.Text)
                    + boundaryLLXTextBox.Text + " " + boundaryLLYTextBox.Text + " " + boundaryURXTextBox.Text + " " + boundaryURYTextBox.Text;
                if (recursiveClipCheckBox.Checked)
                {
                    commandLine = "/r " + commandLine;
                }
                if (isOverwriteCheckBox.Checked)
                {
                    commandLine = "/o " + commandLine;
                }
                // settings.isClipGenCreated = true;
                string[] args = Utilities.CommandLineToArgs(commandLine);
                SIFToolSettings settings = new SIFToolSettings(args);
                SIFTool sifTool = new SIFTool(settings);
                int exitcode = sifTool.Run(false, false, true);
            }
            catch (Exception ex)
            {
                string msg = "Unexpected error: " + ex.Message;
                log.AddError("\r\n" + msg + "\r\n");
                Exception innerex = ex.InnerException;
                Exception prevEx = ex;
                while (innerex != null)
                {
                    log.AddError("Unexpected error: " + innerex.Message + "\r\n");
                    prevEx = innerex;
                    innerex = innerex.InnerException;
                }
                log.AddInfo(prevEx.StackTrace);
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                resultTextBox.ScrollToCaret();
            }
        }

        private void LogMessageListener(string message, bool isEolAdded = true)
        {
            if (isEolAdded)
            {
                resultTextBox.AppendText(message + "\r\n");
            }
            else
            {
                resultTextBox.AppendText(message);
            }
            Application.DoEvents();
        }
    }
}
