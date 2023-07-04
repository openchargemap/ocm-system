using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers.OCPI
{
    public class ImportProvider_Mobie : ImportProvider_OCPI, IImportProvider
    {
        public ImportProvider_Mobie() : base()
        {
            ProviderName = "mobie.pt";
            OutputNamePrefix = "mobie";

            IsAutoRefreshed = true;
            IsProductionReady = true;

            DataProviderID = 7; // mobie.pt
            AutoRefreshURL = "https://ocpi.mobinteli.com/2.2/locations";

        }

        public override Dictionary<string, int> GetOperatorMappings()
        {
            // Mappings based on https://mobie.pt/en/mobienetwork/finding-charging-points

            return new Dictionary<string, int>()
            {
                    { "EDP",3276 },
                    { "GLP",3557 },
                    { "HRZ",3550 },
                    { "GLG",3557 },
                    { "MAK",3649},
                    { "MLT",3557},
                    { "REP",91 },
                    { "IBD",2247 },
                    { "PIR",200 },
                    { "PRI",200 },
                    { "HLX",3645 },
                    { "EML",2247},
                    { "ION",3299 },
                    { "MOO",3644 },
                    { "EVP",3643 },
                    { "GRC",3648 },
                    { "MOB",21 },
                    { "KLS",3588 },
                    { "KLC",3588 },
                    { "CPS",3584 },
                    { "NRG",3646 },
                    { "ECI",3647 },
                    { "MOT",3684 },
                    { "FAC",3685 },
                    { "EVC",3693 }
            };
        }

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            var outputList = Process(coreRefData);

            return outputList;
        }
    }
}