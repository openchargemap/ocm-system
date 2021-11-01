using System.Collections.Generic;

namespace OCM.API.Common.Model.Extended
{
    public class DataQualityReport
    {
        public List<POIDataQualityReport> POIReports { get; set; }

        public DataQualityReport()
        {
            POIReports = new List<POIDataQualityReport>();
        }
    }

    public class POIDataQualityReport
    {
        public double DataQualityScore { get; set; }

        public ChargePoint POI { get; set; }

        public List<DataQualityReportItem> ReportItems { get; set; }

        public POIDataQualityReport()
        {
            ReportItems = new List<DataQualityReportItem>();
        }
    }

    public class DataQualityReportItem
    {
        public double Weighting { get; set; }

        public string Category { get; set; }

        public string Comment { get; set; }

        public bool IsPositive { get; set; }
    }
}