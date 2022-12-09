using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using OCM.API.Common.Model;
using OCM.Import;
using OCM.Import.Manager.UI.Properties;
using OCM.Import.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Import
{
    public partial class AppMain : Form
    {

        List<IImportProvider> _providers;

        ImportManager _importManager;

        IConfigurationRoot _config;

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

            this.txtImportJSONPath.Text = System.Configuration.ConfigurationManager.AppSettings["ImportBasePath"];

            var settings = new ImportSettings
            {
                GeolocationShapefilePath = System.Configuration.ConfigurationManager.AppSettings["GeolocationShapefilePath"],
                ImportUserAPIKey = System.Configuration.ConfigurationManager.AppSettings["APIKey"],
                MasterAPIBaseUrl = System.Configuration.ConfigurationManager.AppSettings["APIBaseUrl"],
                TempFolderPath = System.Configuration.ConfigurationManager.AppSettings["ImportBasePath"]
            };



            var configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _config = new ConfigurationBuilder()
                   .SetBasePath(configPath)
                   .AddJsonFile("appsettings.json", optional: true)
                   .Build();

            var appsettings = new OCM.Core.Settings.CoreSettings();
            _config.GetSection("ImportSettings").Bind(appsettings);


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

            var client = new SecretClient(new Uri("https://import-keyvault.vault.azure.net/"), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync("IMPORT-ocm-system");


            var apiKeys = new Dictionary<string, string>();
            apiKeys.Add("IMPORT-ocm-system", secret.Value.Value);

            var importProcessSettings = new ImportProcessSettings
            {
                ExportType = exportType,
                DefaultDataPath = txtDataFolderPath.Text,
                ApiIdentifier = txtAPIIdentifier.Text,
                ApiSessionToken = txtAPISessionToken.Text,
                FetchLiveData = chkFetchLiveData.Checked,
                CacheInputData = chkCacheInputData.Checked,
                PerformDeduplication = chkDeduplication.Checked,
                FetchExistingFromAPI = true,
                ProviderName = lstProvider.SelectedValue.ToString(),
                Credentials = apiKeys
            };

            await _importManager.PerformImportProcessing(importProcessSettings);


            var providerId = (_providers.FirstOrDefault(p => p.GetProviderName() == importProcessSettings.ProviderName) as BaseImportProvider)?.DataProviderID;
            if (providerId != null)
            {
                await _importManager.UpdateLastImportDate((int)providerId);
            }

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

            _importManager.GeocodingTestCountries();
        }

        private async void BtnUpload_Click(object sender, EventArgs e)
        {

            string json = System.IO.File.ReadAllText(txtImportJSONPath.Text);

            string result = await _importManager.UploadPOIList(json);

            MessageBox.Show(result);
        }
    }
}