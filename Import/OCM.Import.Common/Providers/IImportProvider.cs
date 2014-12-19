using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;

namespace OCM.Import.Providers
{
    public interface IImportProvider
    {
        string GetProviderName();
        List<ChargePoint> Process(CoreReferenceData coreRefData);
    }

    public interface IImportProviderWithInit: IImportProvider
    {
        void InitImportProvider();
    }
}
