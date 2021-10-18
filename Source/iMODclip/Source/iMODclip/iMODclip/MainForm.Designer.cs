namespace Sweco.SIF.iMODclip
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.isOverwriteCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.outputFolderTextBox = new System.Windows.Forms.TextBox();
            this.outputFolderButton = new System.Windows.Forms.Button();
            this.recursiveClipCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.boundaryURYTextBox = new System.Windows.Forms.TextBox();
            this.boundaryURXTextBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.boundaryLLYTextBox = new System.Windows.Forms.TextBox();
            this.boundaryLLXTextBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.startButton = new System.Windows.Forms.Button();
            this.resultTextBox = new System.Windows.Forms.TextBox();
            this.inputFolderButton = new System.Windows.Forms.Button();
            this.inputFolderTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // isOverwriteCheckBox
            // 
            this.isOverwriteCheckBox.AutoSize = true;
            this.isOverwriteCheckBox.Location = new System.Drawing.Point(247, 41);
            this.isOverwriteCheckBox.Name = "isOverwriteCheckBox";
            this.isOverwriteCheckBox.Size = new System.Drawing.Size(130, 17);
            this.isOverwriteCheckBox.TabIndex = 58;
            this.isOverwriteCheckBox.Text = "&Overwrite existing files";
            this.isOverwriteCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.outputFolderTextBox);
            this.groupBox1.Controls.Add(this.outputFolderButton);
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(18, 151);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(546, 57);
            this.groupBox1.TabIndex = 57;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Output specifications";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Output folder:";
            // 
            // outputFolderTextBox
            // 
            this.outputFolderTextBox.Location = new System.Drawing.Point(91, 21);
            this.outputFolderTextBox.Name = "outputFolderTextBox";
            this.outputFolderTextBox.Size = new System.Drawing.Size(413, 20);
            this.outputFolderTextBox.TabIndex = 3;
            // 
            // outputFolderButton
            // 
            this.outputFolderButton.ForeColor = System.Drawing.Color.Black;
            this.outputFolderButton.Location = new System.Drawing.Point(513, 19);
            this.outputFolderButton.Name = "outputFolderButton";
            this.outputFolderButton.Size = new System.Drawing.Size(24, 23);
            this.outputFolderButton.TabIndex = 4;
            this.outputFolderButton.Text = "...";
            this.outputFolderButton.UseVisualStyleBackColor = true;
            // 
            // recursiveClipCheckBox
            // 
            this.recursiveClipCheckBox.AutoSize = true;
            this.recursiveClipCheckBox.Checked = true;
            this.recursiveClipCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.recursiveClipCheckBox.Location = new System.Drawing.Point(89, 41);
            this.recursiveClipCheckBox.Name = "recursiveClipCheckBox";
            this.recursiveClipCheckBox.Size = new System.Drawing.Size(143, 17);
            this.recursiveClipCheckBox.TabIndex = 56;
            this.recursiveClipCheckBox.Text = "Clip files in &subdirectories";
            this.recursiveClipCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.boundaryURYTextBox);
            this.groupBox3.Controls.Add(this.boundaryURXTextBox);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.boundaryLLYTextBox);
            this.groupBox3.Controls.Add(this.boundaryLLXTextBox);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.ForeColor = System.Drawing.Color.White;
            this.groupBox3.Location = new System.Drawing.Point(18, 64);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(546, 80);
            this.groupBox3.TabIndex = 49;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Clip boundary definition";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(513, 45);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(18, 13);
            this.label9.TabIndex = 11;
            this.label9.Text = "m.";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(513, 19);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(18, 13);
            this.label10.TabIndex = 10;
            this.label10.Text = "m.";
            // 
            // boundaryURYTextBox
            // 
            this.boundaryURYTextBox.Location = new System.Drawing.Point(406, 42);
            this.boundaryURYTextBox.Name = "boundaryURYTextBox";
            this.boundaryURYTextBox.Size = new System.Drawing.Size(100, 20);
            this.boundaryURYTextBox.TabIndex = 9;
            // 
            // boundaryURXTextBox
            // 
            this.boundaryURXTextBox.Location = new System.Drawing.Point(406, 16);
            this.boundaryURXTextBox.Name = "boundaryURXTextBox";
            this.boundaryURXTextBox.Size = new System.Drawing.Size(100, 20);
            this.boundaryURXTextBox.TabIndex = 8;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(284, 42);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(120, 13);
            this.label11.TabIndex = 7;
            this.label11.Text = "Y-coordinate upper right";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(284, 20);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(120, 13);
            this.label12.TabIndex = 6;
            this.label12.Text = "X-coordinate upper right";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(246, 45);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(18, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "m.";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(246, 19);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(18, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "m.";
            // 
            // boundaryLLYTextBox
            // 
            this.boundaryLLYTextBox.Location = new System.Drawing.Point(139, 42);
            this.boundaryLLYTextBox.Name = "boundaryLLYTextBox";
            this.boundaryLLYTextBox.Size = new System.Drawing.Size(100, 20);
            this.boundaryLLYTextBox.TabIndex = 3;
            // 
            // boundaryLLXTextBox
            // 
            this.boundaryLLXTextBox.Location = new System.Drawing.Point(139, 16);
            this.boundaryLLXTextBox.Name = "boundaryLLXTextBox";
            this.boundaryLLXTextBox.Size = new System.Drawing.Size(100, 20);
            this.boundaryLLXTextBox.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 45);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(112, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Y-coordinate lower left";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "X-coordinate lower left";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 175);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(0, 13);
            this.label8.TabIndex = 51;
            // 
            // closeButton
            // 
            this.closeButton.ForeColor = System.Drawing.Color.Black;
            this.closeButton.Location = new System.Drawing.Point(99, 390);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 54;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // helpButton
            // 
            this.helpButton.ForeColor = System.Drawing.Color.Black;
            this.helpButton.Location = new System.Drawing.Point(490, 390);
            this.helpButton.Name = "helpButton";
            this.helpButton.Size = new System.Drawing.Size(75, 23);
            this.helpButton.TabIndex = 55;
            this.helpButton.Text = "&Help";
            this.helpButton.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 222);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 50;
            this.label3.Text = "Resultaat:";
            // 
            // startButton
            // 
            this.startButton.ForeColor = System.Drawing.Color.Black;
            this.startButton.Location = new System.Drawing.Point(18, 390);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 23);
            this.startButton.TabIndex = 53;
            this.startButton.Text = "&Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // resultTextBox
            // 
            this.resultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultTextBox.Location = new System.Drawing.Point(18, 242);
            this.resultTextBox.Multiline = true;
            this.resultTextBox.Name = "resultTextBox";
            this.resultTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.resultTextBox.Size = new System.Drawing.Size(547, 142);
            this.resultTextBox.TabIndex = 52;
            this.resultTextBox.WordWrap = false;
            // 
            // inputFolderButton
            // 
            this.inputFolderButton.ForeColor = System.Drawing.Color.Black;
            this.inputFolderButton.Location = new System.Drawing.Point(541, 9);
            this.inputFolderButton.Name = "inputFolderButton";
            this.inputFolderButton.Size = new System.Drawing.Size(24, 23);
            this.inputFolderButton.TabIndex = 48;
            this.inputFolderButton.Text = "...";
            this.inputFolderButton.UseVisualStyleBackColor = true;
            // 
            // inputFolderTextBox
            // 
            this.inputFolderTextBox.Location = new System.Drawing.Point(90, 12);
            this.inputFolderTextBox.Name = "inputFolderTextBox";
            this.inputFolderTextBox.Size = new System.Drawing.Size(432, 20);
            this.inputFolderTextBox.TabIndex = 47;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 46;
            this.label1.Text = "Input folder:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(32)))), ((int)(((byte)(78)))));
            this.ClientSize = new System.Drawing.Size(574, 423);
            this.Controls.Add(this.isOverwriteCheckBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.recursiveClipCheckBox);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.resultTextBox);
            this.Controls.Add(this.inputFolderButton);
            this.Controls.Add(this.inputFolderTextBox);
            this.Controls.Add(this.label1);
            this.ForeColor = System.Drawing.Color.White;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "iMODclip";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox isOverwriteCheckBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox outputFolderTextBox;
        private System.Windows.Forms.Button outputFolderButton;
        private System.Windows.Forms.CheckBox recursiveClipCheckBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox boundaryURYTextBox;
        private System.Windows.Forms.TextBox boundaryURXTextBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox boundaryLLYTextBox;
        private System.Windows.Forms.TextBox boundaryLLXTextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.TextBox resultTextBox;
        private System.Windows.Forms.Button inputFolderButton;
        private System.Windows.Forms.TextBox inputFolderTextBox;
        private System.Windows.Forms.Label label1;
    }
}