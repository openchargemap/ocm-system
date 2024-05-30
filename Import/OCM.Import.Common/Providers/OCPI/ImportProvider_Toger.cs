namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Toger : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Toger() : base()
        {
            ProviderName = "toger";
            OutputNamePrefix = "toger";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            CredentialKey = "OCPI-TOGER";

            // this feed does not specify operator
            DefaultOperatorID = 3784;

            Init(dataProviderId: 34, "https://app.toger.co/ocpi/cpo/2.2.1/locations");
        }
    }
}