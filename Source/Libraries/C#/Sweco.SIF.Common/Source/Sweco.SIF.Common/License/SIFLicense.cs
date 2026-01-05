// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// Class for SIF-basis license
    /// </summary>
    public class SIFLicense
    {
        /// <summary>
        /// Name of SIF-instrument as used in written text
        /// </summary>
        protected const string SIFInstrumentName = "SIF";

        /// <summary>
        /// Current version of this SIF/license-type 
        /// </summary>
        protected const string SIFVersion = "2.3.0.0";

        /// <summary>
        /// Short name of license holder as used in written text
        /// </summary>
        protected const string LicenseHolderName = "Sweco";

        /// <summary>
        /// Full, format name of license holder as used in copyright notice
        /// </summary>
        protected const string LicenseHolderFullName = "Sweco Nederland B.V.";

        /// <summary>
        /// Full name of this SIF-license
        /// </summary>
        public virtual string SIFLicenseName
        {
            get { return SIFInstrumentName + "-basis"; }
        }

        /// <summary>
        /// Full name of this SIF-type
        /// </summary>
        public virtual string SIFTypeName
        {
            get { return SIFInstrumentName + "-basis"; }
        }

        /// <summary>
        /// Title of license form for this license
        /// </summary>
        public virtual string LicenseFormTitle
        {
            get { return "Licentie " + SIFLicenseName + " " + SIFVersion; }
        }

        /// <summary>
        /// Width of textbox in license form
        /// </summary>
        protected virtual int LicenseForm_TextBoxWidth
        {
            get { return 662; }
        }

        /// <summary>
        /// Height of textbox in license form
        /// </summary>
        protected virtual int LicenseForm_TextBoxHeight
        {
            get { return 360; }
        }

        /// <summary>
        /// Margin around controles in license form
        /// </summary>
        protected virtual int LicenseForm_ControlMargin
        {
            get { return 12; }
        }

        /// <summary>
        /// Width of buttons in license form
        /// </summary>
        protected virtual int LicenseForm_ButtonWidth
        {
            get { return 100; }
        }

        /// <summary>
        /// Postfix (letter) that indicates this SIF/license-type and is used as a postfix for the tool version
        /// </summary>
        public virtual string SIFTypeVersionPostfix
        {
            get { return "b"; }
        }

        /// <summary>
        /// Filename of license file (without path, including extension) for this SIF/license-type
        /// </summary>
        public string LicenseFilename 
        {
            get { return SIFLicenseName + "_" + SIFVersion + ".lic"; }
        }

        /// <summary>
        /// Filter for allowed filenames of license file (without path, including extension) for this SIF/license-type
        /// </summary>
        public string LicenseFilenameFilter
        {
            get { return SIFLicenseName + "*" + ".lic"; }
        }

        /// <summary>
        /// First part of string in license file that stores acceptance by a specific user, e.g. "SIF-license accepted by"
        /// </summary>
        protected virtual string AcceptancePrefix
        {
            get { return SIFLicenseName + " " + SIFVersion + ", licentie geaccepteerd door "; }
        }

        /// <summary>
        /// Full name string for user, including both domain and username or accountname
        /// </summary>
        protected virtual string FullUsername
        {
            get { return Environment.UserDomainName + "\\" + Environment.UserName; }
        }

        /// <summary>
        /// Full acceptance string as written to the license file: acceptance prefix, full username and date and time
        /// </summary>
        protected virtual string AcceptanceString
        {
            get { return AcceptancePrefix + FullUsername + ", " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(); }
        }

        /// <summary>
        /// First part of license file, including header and license text, until the first user acceptance line
        /// </summary>
        public virtual string LicenseFileTextPart1
        {
            get
            {
                return SIFLicenseName + " " + SIFVersion + "\r\n\r\n" + ReplaceSpecialChars(LicenseText) + "\r\n";
            }
        }

        /// <summary>
        /// Full license text of this SIF/license-type to accept by user
        /// </summary>
        public virtual string LicenseText
        {
            get
            {
                string licenseText = "Deze tools zijn onderdeel van " + SIFTypeName + ", een framework van Sweco voor iMOD-modellering.\r\n"
                             + "Copyright Sweco Nederland B.V.\r\n"
                             + "\r\n"
                             + "Hierbij wordt toestemming verleend aan iedere persoon van de organisatie (de \"Gebruiker\")\r\n"
                             + "die van Sweco (de \"Auteur\") een kopie heeft ontvangen van deze software en bijbehorende \r\n"
                             + "documentatie (de \"Software\"), om de Software te gebruiken onder de volgende voorwaarden:\r\n"
                             + "\r\n"
                             + "Bovenstaande copyright mededeling, dit bericht en andere licentiebestanden in deze \r\n"
                             + "directories, moeten worden opgenomen in alle kopie" + Convert.ToChar(235) + "n van de Software.\r\n"
                             + "\r\n"
                             + "Aanpassen, publiceren of distribueren van kopie" + Convert.ToChar(235) + "n van de Software buiten de eigen\r\n"
                             + "organisatie is niet toegestaan zonder voorafgaande toestemming van de Auteur. Bij \r\n"
                             + "commercieel gebruik van de Software voor doeleinden buiten de eigen organisatie, \r\n"
                             + "dient een referentie naar de Software te worden opgenomen in gepubliceerde teksten,\r\n"
                             + "zoals offertes, notities of rapportages.\r\n"
                             + "\r\n"
                             + "DE SOFTWARE IS BESCHIKBAAR IN DE HUIDIGE STAAT, ZONDER GARANTIE VAN WELKE SOORT DAN OOK.\r\n"
                             + "IN GEEN GEVAL KAN DE AUTEUR AANSPRAKELIJK WORDEN GEHOUDEN VOOR SCHADE DIE IS ONTSTAAN\r\n"
                             + "DOOR GEBRUIK VAN DE SOFTWARE.\r\n"
                             + "\r\n"
                             + "Alle intellectuele eigendomsrechten met betrekking tot de Software berusten bij Sweco\r\n"
                             + "Nederland B.V. en/of haar toeleveranciers, voor zover het niet gaat om open-source\r\n"
                             + "onderdelen die zijn gebruikt binnen de Software.\r\n";

                return licenseText;
            }
        }

        /// <summary>
        /// Reference to SIF-tool that this license is used for
        /// </summary>
        protected SIFToolBase Tool { get; }

        /// <summary>
        /// Constructor for SIFLicense instance
        /// </summary>
        /// <param name="tool"></param>
        public SIFLicense(SIFToolBase tool)
        {
            this.Tool = tool;
        }

        /// <summary>
        /// Check if current license for this tool has been accepted by current user, otherwise show license text and request approval. Tool is exited if not approved.
        /// </summary>
        public void CheckLicense()
        {
            // Retrieve all license paths that are checked. 
            List<string> licensePaths = new List<string>() { GetLicensePath1(), GetLicensePath2(), GetLicensePath3() };

            // Check for a valid license for current SIF-tool instance in one of the license paths
            foreach (string licensePath in licensePaths)
            {
                if (Directory.Exists(licensePath))
                {
                    string[] licenseFilenames = Directory.GetFiles(licensePath, LicenseFilenameFilter);
                    foreach (string licenseFilename in licenseFilenames)
                    {
                        if (HasValidLicense(licenseFilename, FullUsername))
                        {
                            return;
                        }
                    }
                }
            }

            // Show license form, ask user to accept license or cancel
            bool hasAccepted = ShowLicenseForm(LicenseText);

            if (!hasAccepted)
            {
                System.Console.WriteLine("License not accepted, exiting");
                Environment.Exit(-1);
            }
            else
            {
                // Try to write acceptance of this user to ALL license path(s)
                // Note: it is important to add license to all paths since Windows paths could be emptied overnight (which happens sometimes on network/virtual systems).
                // Later, the user could remove one of these paths if necessary, as long as one path remains. Otherwise the LicenseCheck is shown again.
                bool isWritten = false;
                foreach (string licensePath in licensePaths)
                {
                    string licenseFilename = Path.Combine(licensePath, LicenseFilename);
                    isWritten |= WriteLicenseFile(licenseFilename);
                }

                if (!isWritten)
                {
                    MessageBox.Show("License file could not be saved in: " + CommonUtils.ToString(licensePaths, "\n") + "\n\nEnsure write access to prevent license-check for each run.", "Write error for SIF-license", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        /// <summary>
        /// Checks if the specified license file is a valid license file for the current SIF-tool instance; 
        /// Optionally check if the specified user has accepted the license
        /// </summary>
        /// <param name="licenseFilename"></param>
        /// <param name="username">if non-empty, it is checked if this user has accepted the license</param>
        /// <returns></returns>
        protected virtual bool HasValidLicense(string licenseFilename, string username = null)
        {
            bool hasValidLicense = false;

            if (File.Exists(licenseFilename))
            {
                // Check all selected license filenames for requested license
                StringBuilder licenseFileText = null;
                StreamReader sr = null;
                licenseFileText = new StringBuilder();
                try
                {
                    // Read file until first user acceptance line is found or end-of-file is reached
                    FileStream fs = new FileStream(licenseFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sr = new StreamReader(fs);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        licenseFileText.AppendLine(line);
                        if (line.StartsWith(AcceptancePrefix))
                        {
                            // Check that first part of file starts with licensetext
                            if (licenseFileText.ToString().StartsWith(LicenseFileTextPart1))
                            {
                                if (username == null)
                                {
                                    hasValidLicense = true;
                                    break;
                                }
                                else if (line.Contains(username))
                                {
                                    // license has already been accepted by this user
                                    hasValidLicense = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore exceptions, these are threated as an invalid license file
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                    }
                }
            }

            return hasValidLicense;
        }

        /// <summary>
        /// Show license form with current license and write license to specified filename
        /// </summary>
        /// <param name="licenseText">full license text to show in form</param>
        /// <returns>true if license has been accepted by user; false otherwise</returns>
        protected bool ShowLicenseForm(string licenseText)
        {
            int buttonHeight = 23;
            Form licenseForm = new Form();
            System.Windows.Forms.TextBox textBox;
            System.Windows.Forms.Button okButton;
            System.Windows.Forms.Button cancelButton;

            textBox = new System.Windows.Forms.TextBox();
            okButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            licenseForm.SuspendLayout();

            textBox.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            textBox.Location = new System.Drawing.Point(LicenseForm_ControlMargin, LicenseForm_ControlMargin);
            textBox.Multiline = true;
            textBox.Name = "textBox";
            textBox.Size = new System.Drawing.Size(LicenseForm_TextBoxWidth, LicenseForm_TextBoxHeight);
            textBox.MinimumSize = new System.Drawing.Size(LicenseForm_TextBoxWidth, LicenseForm_TextBoxHeight);
            textBox.TabIndex = 0;
            textBox.Text = licenseText;
            textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top)));

            okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            okButton.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            okButton.Location = new System.Drawing.Point((int) (LicenseForm_TextBoxWidth / 2) - LicenseForm_ButtonWidth, LicenseForm_TextBoxHeight + 2 * LicenseForm_ControlMargin);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(LicenseForm_ButtonWidth, buttonHeight);
            okButton.BackColor = System.Drawing.SystemColors.ButtonFace;
            okButton.TabIndex = 2;
            okButton.Text = "Akkoord";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += new System.EventHandler(delegate (object sender, EventArgs e)
            {
                licenseForm.DialogResult = DialogResult.Yes;
                licenseForm.Close();
            });

            cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            cancelButton.Location = new System.Drawing.Point((int)(LicenseForm_TextBoxWidth / 2) + 2 * LicenseForm_ControlMargin, LicenseForm_TextBoxHeight + 2 * LicenseForm_ControlMargin);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(LicenseForm_ButtonWidth, buttonHeight);
            cancelButton.BackColor = System.Drawing.SystemColors.ButtonFace;
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Annuleren";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += new System.EventHandler(delegate (object sender, EventArgs e)
            {
                licenseForm.DialogResult = DialogResult.No;
                licenseForm.Close();
            });

            licenseForm.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            licenseForm.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            licenseForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(78)))));
            licenseForm.CancelButton = cancelButton;
            licenseForm.ClientSize = new System.Drawing.Size(LicenseForm_TextBoxWidth + 2 * LicenseForm_ControlMargin, LicenseForm_TextBoxHeight + 3 * LicenseForm_ControlMargin + buttonHeight);
            licenseForm.MinimumSize = licenseForm.Size;
            licenseForm.Icon = CommonUtils.SIFIcon;

            licenseForm.Controls.Add(cancelButton);
            licenseForm.Controls.Add(okButton);
            licenseForm.Controls.Add(textBox);
            licenseForm.Name = "LicenseForm";
            licenseForm.Text = LicenseFormTitle;
            licenseForm.ResumeLayout(false);
            licenseForm.PerformLayout();

            cancelButton.Select();

            DialogResult result = licenseForm.ShowDialog();
            return (result == DialogResult.Yes);
        }

        /// <summary>
        /// Add acceptance line for current user to specified license file or create new file if it does not yet exist
        /// </summary>
        /// <param name="licenseFilename"></param>
        protected virtual bool WriteLicenseFile(string licenseFilename)
        {
            StreamWriter sw = null;
            try
            {
                if (File.Exists(licenseFilename))
                {
                    // License file starts with correct licensetext, just add license acceptance of this user
                    sw = new StreamWriter(licenseFilename, true);
                    sw.WriteLine(AcceptanceString);
                }
                else
                {
                    FileUtils.EnsureFolderExists(licenseFilename);

                    sw = new StreamWriter(licenseFilename);
                    sw.Write(LicenseFileTextPart1);
                    sw.WriteLine(AcceptanceString);
                }

                return true;
            }
            catch (Exception)
            {
                // Specified license file could be not written
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Retrieve path with license file for current SIF-version, that is first checked, which is like: Users\{User}\AppData\Local\Sweco\SIF
        /// </summary>
        /// <returns></returns>
        protected string GetLicensePath1()
        {
            return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LicenseHolderName), SIFInstrumentName.Replace(" ", "-"));
        }

        /// <summary>
        /// Retrieve path with license file for current SIF-version, that is checked secondly: the path with the current tool
        /// </summary>
        /// <returns></returns>
        protected string GetLicensePath2()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        }

        /// <summary>
        /// Retrieve path with license file for current SIF-version, that is checked last: a temporary path generated by Windows
        /// </summary>
        /// <returns></returns>
        protected string GetLicensePath3()
        {
            string tmpPath = Path.GetTempPath();
            return Path.Combine(Path.Combine(tmpPath, LicenseHolderName), SIFInstrumentName.Replace(" ", "-"));
        }

        /// <summary>
        /// Replace characters that are shown correctly in a Form, but not in a textfile
        /// </summary>
        /// <param name="textString"></param>
        /// <returns></returns>
        protected string ReplaceSpecialChars(string textString)
        {
            // Replace ë with e
            textString = textString.Replace(Convert.ToChar(235), 'e');

            return textString;
        }
    }
}

