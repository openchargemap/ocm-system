using System;
using System.Collections.Generic;
using System.Text;

namespace OCM.Import
{
    public class ImportSettings
    {
        public string MasterAPIBaseUrl { get; set; } = "https://api-01.openchargemap.io/v3";
        public int ImportRunFrequencyMinutes { get; set; } = 24 * 60;
        public string ImportUserAPIKey { get; set; }

        public string TempFolderPath { get; set; }
        public string GeolocationShapefilePath { get; set; }
        public string ImportUserAgent { get; set; } = "OCM.Import";
        public List<string> EnabledImports { get; set; } = new List<string>();

        public Dictionary<string, string> ApiKeys { get; set; } = new Dictionary<string, string>();
    }

    public class ImportStatus
    {
        public string LastImportedProvider { get; set; }
        public DateTime? DateLastImport { get; set; }
        public string LastImportStatus { get; set; }
        public double ProcessingTimeSeconds { get; set; } = 0;
    }
}
