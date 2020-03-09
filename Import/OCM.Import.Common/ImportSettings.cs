using System;
using System.Collections.Generic;
using System.Text;

namespace OCM.Import
{
    public class ImportSettings
    {
        public string MasterAPIBaseUrl { get; set; }= "https://api-01.openchargemap.io/v3";
        public int ImportRunFrequencyMinutes { get; set; } = 24 * 60;
        public string ImportUserAPIKey { get; set; }

        public string TempFolderPath { get; set; }
        public string GeolocationShapefilePath { get; set; }
        public string ImportUserAgent { get; set; } = "OCM.Import";
    }
}
