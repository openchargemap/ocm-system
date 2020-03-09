using OCM.API.Common.Model;
using OCM.Import;
using OCM.Import.Manager.UI.Properties;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;

namespace Import
{
    public partial class AppMain : Form
    {

        List<IImportProvider> _providers;

        ImportManager _importManager;

        public AppMain()
        {
            InitializeComponent();

            lstOutputType.SelectedItem = "JSON Export";

            //load settings
            this.txtDataFolderPath.Text = Settings.Default.Import_DataFolder;
            this.txtGeonamesAPIUserID.Text = Settings.Default.Geonames_API_Username;
            this.txtAPIIdentifier.Text = Settings.Default.OCM_API_Identitifer;
            this.txtAPISessionToken.Text = Settings.Default.OCM_API_SessionToken;
            this.txtAPIKey_Coulomb.Text = Settings.Default.APIKey_Coulomb;
            this.txtAPIPwd_Coulomb.Text = Settings.Default.APIPwd_Coulomb;

            this.txtImportJSONPath.Text = ConfigurationManager.AppSettings["ImportBasePath"];

            var settings = new ImportSettings
            {
                GeolocationShapefilePath = ConfigurationManager.AppSettings["GeolocationShapefilePath"],
                ImportUserAPIKey = ConfigurationManager.AppSettings["APIKey"],
                MasterAPIBaseUrl = ConfigurationManager.AppSettings["APIBaseUrl"],
                TempFolderPath = ConfigurationManager.AppSettings["ImportBasePath"]
            };
            
            _importManager = new ImportManager(settings);

            //populate provider list
            _providers = new List<IImportProvider>();
            _providers = _importManager.GetImportProviders(new List<DataProvider>());

            lstProvider.DataSource = _providers.ToArray().Select(s => s.GetProviderName()).ToList();

        }

        private async void btnProcessImports_Click(object sender, EventArgs e)
        {
            ExportType exportType = ExportType.JSON;

            if (lstOutputType.SelectedItem.ToString().StartsWith("XML")) exportType = ExportType.XML;
            if (lstOutputType.SelectedItem.ToString().StartsWith("API")) exportType = ExportType.API;
            if (lstOutputType.SelectedItem.ToString().StartsWith("CSV")) exportType = ExportType.CSV;
            if (lstOutputType.SelectedItem.ToString().StartsWith("JSON")) exportType = ExportType.JSON;

            _importManager.GeonamesAPIUserName = txtGeonamesAPIUserID.Text;
            _importManager.ImportUpdatesOnly = chkUpdatesOnly.Checked;

            this.btnProcessImports.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            await _importManager.PerformImportProcessing(exportType, txtDataFolderPath.Text, txtAPIIdentifier.Text, txtAPISessionToken.Text, chkFetchLiveData.Checked, fetchExistingFromAPI: true, lstProvider.SelectedValue.ToString());
            this.Cursor = Cursors.Default;
            this.btnProcessImports.Enabled = true;
            MessageBox.Show("Processing Completed");
        }

        private void AppMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //save settings

            Settings.Default.Import_DataFolder = this.txtDataFolderPath.Text;
            Settings.Default.Geonames_API_Username = this.txtGeonamesAPIUserID.Text;
            Settings.Default.OCM_API_Identitifer = this.txtAPIIdentifier.Text;
            Settings.Default.OCM_API_SessionToken = this.txtAPISessionToken.Text;
            Settings.Default.APIKey_Coulomb = this.txtAPIKey_Coulomb.Text;
            Settings.Default.APIPwd_Coulomb = this.txtAPIPwd_Coulomb.Text;

            Settings.Default.Save();
        }

        private void btnServiceTest1_Click(object sender, EventArgs e)
        {
            //ServiceManager networkSVCManager = new ServiceManager();
            //string jsonResult = networkSVCManager.GetAllResultsAsJSON(ServiceProvider.CoulombChargePoint, txtAPIKey_Coulomb.Text, txtAPIPwd_Coulomb.Text);

            _importManager.GeocodingTest();
        }

        private async void BtnUpload_Click(object sender, EventArgs e)
        {

            string json = System.IO.File.ReadAllText(txtImportJSONPath.Text);

            string result = await _importManager.UploadPOIList(json);

            MessageBox.Show(result);
        }
    }
}