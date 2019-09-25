using OCM.API.Common.Model;
using OCM.Import;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Import
{
    public partial class AppMain : Form
    {

        List<IImportProvider> _providers;
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


            //populate provider list
            _providers = new List<IImportProvider>();
            _providers = new ImportManager(this.txtDataFolderPath.Text).GetImportProviders(new List<DataProvider>());

            lstProvider.DataSource = _providers.ToArray().Select(s=>s.GetProviderName()).ToList();

        }

        private async void btnProcessImports_Click(object sender, EventArgs e)
        {
            ImportManager importManager = new ImportManager(txtDataFolderPath.Text);
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

            await importManager.PerformImportProcessing(exportType, txtDataFolderPath.Text, txtAPIIdentifier.Text, txtAPISessionToken.Text, chkFetchLiveData.Checked, fetchExistingFromAPI: true, lstProvider.SelectedValue.ToString());
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

            new ImportManager("").GeocodingTest();
        }

        private void BtnUpload_Click(object sender, EventArgs e)
        {
            ImportManager importManager = new ImportManager(txtDataFolderPath.Text);
            importManager.IsSandboxedAPIMode = true;
            string json = System.IO.File.ReadAllText(txtImportJSONPath.Text);

            string result = importManager.UploadPOIList(json, txtAPIIdentifier.Text, txtAPISessionToken.Text);

            MessageBox.Show(result);
        }
    }
}