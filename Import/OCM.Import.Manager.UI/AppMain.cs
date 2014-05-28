using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OCM.Import.Providers;
using OCM.Import;
using OCM.API.NetworkServices;

namespace Import
{
    public partial class AppMain : Form
    {
        public AppMain()
        {
            InitializeComponent();

            lstOutputType.SelectedItem = "JSON Export";

            //load settings
            this.txtDataFolderPath.Text = Properties.Settings.Default.Import_DataFolder;
            this.txtGeonamesAPIUserID.Text = Properties.Settings.Default.Geonames_API_Username;
            this.txtAPIIdentifier.Text = Properties.Settings.Default.OCM_API_Identitifer;
            this.txtAPISessionToken.Text = Properties.Settings.Default.OCM_API_SessionToken;
            this.txtAPIKey_Coulomb.Text = Properties.Settings.Default.APIKey_Coulomb;
            this.txtAPIPwd_Coulomb.Text = Properties.Settings.Default.APIPwd_Coulomb;
        }

        private async void btnProcessImports_Click(object sender, EventArgs e)
        {
            ImportManager importManager = new ImportManager();
            importManager.IsSandboxedAPIMode = false;

            ExportType exportType = ExportType.JSON;
            
            if (lstOutputType.SelectedItem.ToString().StartsWith("XML")) exportType = ExportType.XML;
            if (lstOutputType.SelectedItem.ToString().StartsWith("API")) exportType = ExportType.API;
            if (lstOutputType.SelectedItem.ToString().StartsWith("CSV")) exportType = ExportType.CSV;
            if (lstOutputType.SelectedItem.ToString().StartsWith("JSON")) exportType = ExportType.JSON;
            
            importManager.GeonamesAPIUserName = txtGeonamesAPIUserID.Text;
            importManager.ImportUpdatesOnly = chkUpdatesOnly.Checked;

            this.btnProcessImports.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            
            await importManager.PerformImportProcessing(exportType, txtDataFolderPath.Text, txtAPIIdentifier.Text, txtAPISessionToken.Text, chkFetchLiveData.Checked);
            this.Cursor = Cursors.Default;
            this.btnProcessImports.Enabled = true;
            MessageBox.Show("Processing Completed");

        }

        private void AppMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //save settings

            Properties.Settings.Default.Import_DataFolder = this.txtDataFolderPath.Text;
            Properties.Settings.Default.Geonames_API_Username = this.txtGeonamesAPIUserID.Text;
            Properties.Settings.Default.OCM_API_Identitifer = this.txtAPIIdentifier.Text;
            Properties.Settings.Default.OCM_API_SessionToken = this.txtAPISessionToken.Text;
            Properties.Settings.Default.APIKey_Coulomb = this.txtAPIKey_Coulomb.Text;
            Properties.Settings.Default.APIPwd_Coulomb = this.txtAPIPwd_Coulomb.Text;

            Properties.Settings.Default.Save();
        }

        private void btnServiceTest1_Click(object sender, EventArgs e)
        {
            //ServiceManager networkSVCManager = new ServiceManager();
            //string jsonResult = networkSVCManager.GetAllResultsAsJSON(ServiceProvider.CoulombChargePoint, txtAPIKey_Coulomb.Text, txtAPIPwd_Coulomb.Text);


            new ImportManager().GeocodingTest();
        }
    }
}
