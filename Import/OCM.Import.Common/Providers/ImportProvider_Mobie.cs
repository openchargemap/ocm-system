using Amazon.Runtime.Internal.Transform;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Import.Providers
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

        List<ChargePoint> IImportProvider.Process(CoreReferenceData coreRefData)
        {
            var submissionStatus = coreRefData.SubmissionStatusTypes.First(s => s.ID == 100);//imported and published
            var status_operational = coreRefData.StatusTypes.First(os => os.ID == 50);
            var status_notoperational = coreRefData.StatusTypes.First(os => os.ID == 100);

            var status_operationalMixed = coreRefData.StatusTypes.First(os => os.ID == 75);
            var status_available = coreRefData.StatusTypes.First(os => os.ID == 10);
            var status_inuse = coreRefData.StatusTypes.First(os => os.ID == 20);
            var status_unknown = coreRefData.StatusTypes.First(os => os.ID == 0);
            var usageTypePublic = coreRefData.UsageTypes.First(u => u.ID == 1);
            var usageTypePrivate = coreRefData.UsageTypes.First(u => u.ID == 2);

            // Mappings based on https://mobie.pt/en/mobienetwork/finding-charging-points
            OperatorMappings = new Dictionary<string, int>()
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
                { "HLX",3645 },
                { "EML",2247},
                { "ION",3299 },
                { "MOO",3644 },
                { "EVP",3643 },
                { "GRC",3648 },
                { "MOB",21 },
                { "KLS",3588 },
                { "CPS",3584 },
                { "NRG",3646 },
                { "ECI",3647 }
            };

            var outputList = base.Process(coreRefData);

            return outputList;
        }
    }
}