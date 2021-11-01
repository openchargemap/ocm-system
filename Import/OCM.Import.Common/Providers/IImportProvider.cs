using OCM.API.Common.Model;
using System.Collections.Generic;

namespace OCM.Import.Providers
{
    public interface IImportProvider
    {
        string GetProviderName();
        List<ChargePoint> Process(CoreReferenceData coreRefData);
    }

    public interface IImportProviderWithInit : IImportProvider
    {
        void InitImportProvider();
    }
}
