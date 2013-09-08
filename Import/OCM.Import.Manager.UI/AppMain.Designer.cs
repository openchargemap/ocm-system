namespace Import
{
    partial class AppMain
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
            this.tabSettings = new System.Windows.Forms.TabControl();
            this.tabPageImport = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkFetchLiveData = new System.Windows.Forms.CheckBox();
            this.chkUpdatesOnly = new System.Windows.Forms.CheckBox();
            this.lstOutputType = new System.Windows.Forms.ComboBox();
            this.btnProcessImports = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageTest = new System.Windows.Forms.TabPage();
            this.btnServiceTest1 = new System.Windows.Forms.Button();
            this.tabPageSettings = new System.Windows.Forms.TabPage();
            this.txtAPIPwd_Coulomb = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtAPIKey_Coulomb = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtGeonamesAPIUserID = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtAPISessionToken = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtAPIIdentifier = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDataFolderPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabSettings.SuspendLayout();
            this.tabPageImport.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPageTest.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.tabPageImport);
            this.tabSettings.Controls.Add(this.tabPageTest);
            this.tabSettings.Controls.Add(this.tabPageSettings);
            this.tabSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSettings.Location = new System.Drawing.Point(0, 0);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.SelectedIndex = 0;
            this.tabSettings.Size = new System.Drawing.Size(431, 307);
            this.tabSettings.TabIndex = 0;
            // 
            // tabPageImport
            // 
            this.tabPageImport.Controls.Add(this.groupBox1);
            this.tabPageImport.Location = new System.Drawing.Point(4, 22);
            this.tabPageImport.Name = "tabPageImport";
            this.tabPageImport.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageImport.Size = new System.Drawing.Size(423, 281);
            this.tabPageImport.TabIndex = 0;
            this.tabPageImport.Text = "Import";
            this.tabPageImport.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkFetchLiveData);
            this.groupBox1.Controls.Add(this.chkUpdatesOnly);
            this.groupBox1.Controls.Add(this.lstOutputType);
            this.groupBox1.Controls.Add(this.btnProcessImports);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(9, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(405, 263);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Processing";
            // 
            // chkFetchLiveData
            // 
            this.chkFetchLiveData.AutoSize = true;
            this.chkFetchLiveData.Checked = true;
            this.chkFetchLiveData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFetchLiveData.Location = new System.Drawing.Point(255, 70);
            this.chkFetchLiveData.Name = "chkFetchLiveData";
            this.chkFetchLiveData.Size = new System.Drawing.Size(102, 17);
            this.chkFetchLiveData.TabIndex = 12;
            this.chkFetchLiveData.Text = "Fetch Live Data";
            this.chkFetchLiveData.UseVisualStyleBackColor = true;
            // 
            // chkUpdatesOnly
            // 
            this.chkUpdatesOnly.AutoSize = true;
            this.chkUpdatesOnly.Location = new System.Drawing.Point(255, 47);
            this.chkUpdatesOnly.Name = "chkUpdatesOnly";
            this.chkUpdatesOnly.Size = new System.Drawing.Size(90, 17);
            this.chkUpdatesOnly.TabIndex = 11;
            this.chkUpdatesOnly.Text = "Updates Only";
            this.chkUpdatesOnly.UseVisualStyleBackColor = true;
            // 
            // lstOutputType
            // 
            this.lstOutputType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstOutputType.FormattingEnabled = true;
            this.lstOutputType.Items.AddRange(new object[] {
            "CSV Export",
            "XML Export",
            "API Publish (Live)",
            "API Publish (Test)"});
            this.lstOutputType.Location = new System.Drawing.Point(255, 20);
            this.lstOutputType.Name = "lstOutputType";
            this.lstOutputType.Size = new System.Drawing.Size(121, 21);
            this.lstOutputType.TabIndex = 4;
            // 
            // btnProcessImports
            // 
            this.btnProcessImports.Location = new System.Drawing.Point(132, 20);
            this.btnProcessImports.Name = "btnProcessImports";
            this.btnProcessImports.Size = new System.Drawing.Size(75, 23);
            this.btnProcessImports.TabIndex = 1;
            this.btnProcessImports.Text = "Process";
            this.btnProcessImports.UseVisualStyleBackColor = true;
            this.btnProcessImports.Click += new System.EventHandler(this.btnProcessImports_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(118, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Process the latest data:";
            // 
            // tabPageTest
            // 
            this.tabPageTest.Controls.Add(this.btnServiceTest1);
            this.tabPageTest.Location = new System.Drawing.Point(4, 22);
            this.tabPageTest.Name = "tabPageTest";
            this.tabPageTest.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTest.Size = new System.Drawing.Size(423, 281);
            this.tabPageTest.TabIndex = 1;
            this.tabPageTest.Text = "Testing";
            this.tabPageTest.UseVisualStyleBackColor = true;
            // 
            // btnServiceTest1
            // 
            this.btnServiceTest1.Location = new System.Drawing.Point(306, 21);
            this.btnServiceTest1.Name = "btnServiceTest1";
            this.btnServiceTest1.Size = new System.Drawing.Size(75, 23);
            this.btnServiceTest1.TabIndex = 0;
            this.btnServiceTest1.Text = "Service Test";
            this.btnServiceTest1.UseVisualStyleBackColor = true;
            this.btnServiceTest1.Click += new System.EventHandler(this.btnServiceTest1_Click);
            // 
            // tabPageSettings
            // 
            this.tabPageSettings.Controls.Add(this.txtDataFolderPath);
            this.tabPageSettings.Controls.Add(this.label2);
            this.tabPageSettings.Controls.Add(this.txtAPIPwd_Coulomb);
            this.tabPageSettings.Controls.Add(this.label7);
            this.tabPageSettings.Controls.Add(this.txtAPIKey_Coulomb);
            this.tabPageSettings.Controls.Add(this.label6);
            this.tabPageSettings.Controls.Add(this.txtGeonamesAPIUserID);
            this.tabPageSettings.Controls.Add(this.label5);
            this.tabPageSettings.Controls.Add(this.txtAPISessionToken);
            this.tabPageSettings.Controls.Add(this.label4);
            this.tabPageSettings.Controls.Add(this.txtAPIIdentifier);
            this.tabPageSettings.Controls.Add(this.label3);
            this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSettings.Size = new System.Drawing.Size(423, 281);
            this.tabPageSettings.TabIndex = 2;
            this.tabPageSettings.Text = "Settings";
            this.tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // txtAPIPwd_Coulomb
            // 
            this.txtAPIPwd_Coulomb.Location = new System.Drawing.Point(135, 149);
            this.txtAPIPwd_Coulomb.Name = "txtAPIPwd_Coulomb";
            this.txtAPIPwd_Coulomb.Size = new System.Drawing.Size(230, 20);
            this.txtAPIPwd_Coulomb.TabIndex = 26;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 152);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(92, 13);
            this.label7.TabIndex = 25;
            this.label7.Text = "Coulomb API Pwd";
            // 
            // txtAPIKey_Coulomb
            // 
            this.txtAPIKey_Coulomb.Location = new System.Drawing.Point(135, 123);
            this.txtAPIKey_Coulomb.Name = "txtAPIKey_Coulomb";
            this.txtAPIKey_Coulomb.Size = new System.Drawing.Size(230, 20);
            this.txtAPIKey_Coulomb.TabIndex = 24;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 126);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Coulomb API Key";
            // 
            // txtGeonamesAPIUserID
            // 
            this.txtGeonamesAPIUserID.Location = new System.Drawing.Point(135, 96);
            this.txtGeonamesAPIUserID.Name = "txtGeonamesAPIUserID";
            this.txtGeonamesAPIUserID.Size = new System.Drawing.Size(230, 20);
            this.txtGeonamesAPIUserID.TabIndex = 22;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 99);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "Geonames API UserID";
            // 
            // txtAPISessionToken
            // 
            this.txtAPISessionToken.Location = new System.Drawing.Point(135, 70);
            this.txtAPISessionToken.Name = "txtAPISessionToken";
            this.txtAPISessionToken.Size = new System.Drawing.Size(230, 20);
            this.txtAPISessionToken.TabIndex = 20;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 73);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 13);
            this.label4.TabIndex = 19;
            this.label4.Text = "API Session Token";
            // 
            // txtAPIIdentifier
            // 
            this.txtAPIIdentifier.Location = new System.Drawing.Point(135, 44);
            this.txtAPIIdentifier.Name = "txtAPIIdentifier";
            this.txtAPIIdentifier.Size = new System.Drawing.Size(230, 20);
            this.txtAPIIdentifier.TabIndex = 18;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "API Identifier";
            // 
            // txtDataFolderPath
            // 
            this.txtDataFolderPath.Location = new System.Drawing.Point(135, 18);
            this.txtDataFolderPath.Name = "txtDataFolderPath";
            this.txtDataFolderPath.Size = new System.Drawing.Size(230, 20);
            this.txtDataFolderPath.TabIndex = 28;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 27;
            this.label2.Text = "Path to data folders:";
            // 
            // AppMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(431, 307);
            this.Controls.Add(this.tabSettings);
            this.Name = "AppMain";
            this.Text = "Import Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AppMain_FormClosing);
            this.tabSettings.ResumeLayout(false);
            this.tabPageImport.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPageTest.ResumeLayout(false);
            this.tabPageSettings.ResumeLayout(false);
            this.tabPageSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabSettings;
        private System.Windows.Forms.TabPage tabPageImport;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnProcessImports;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPageTest;
        private System.Windows.Forms.ComboBox lstOutputType;
        private System.Windows.Forms.CheckBox chkUpdatesOnly;
        private System.Windows.Forms.CheckBox chkFetchLiveData;
        private System.Windows.Forms.Button btnServiceTest1;
        private System.Windows.Forms.TabPage tabPageSettings;
        private System.Windows.Forms.TextBox txtDataFolderPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtAPIPwd_Coulomb;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtAPIKey_Coulomb;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtGeonamesAPIUserID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtAPISessionToken;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtAPIIdentifier;
        private System.Windows.Forms.Label label3;
    }
}