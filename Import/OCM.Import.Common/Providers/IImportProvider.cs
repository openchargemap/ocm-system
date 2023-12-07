using OCM.API.Common.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OCM.Import.Providers
{
    public interface IImportProvider
    {
        string GetProviderName();
        List<ChargePoint> Process(CoreReferenceData coreRefData);

        Task<bool> LoadInputFromURL(string url);
    }

    public interface IImportProviderWithInit : IImportProvider
    {
        void InitImportProvider();
    }
}
