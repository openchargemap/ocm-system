using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    /// <summary>
    /// Holds custom defined meta data values for a given POI
    /// </summary>
    public partial class MetadataValue
    {
        public int Id { get; set; }
        /// <summary>
        /// ID of POI
        /// </summary>
        public int ChargePointId { get; set; }
        /// <summary>
        /// Metadata Field value relates to
        /// </summary>
        public int MetadataFieldId { get; set; }
        public string ItemValue { get; set; }
        public int? MetadataFieldOptionId { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual MetadataField MetadataField { get; set; }
        public virtual MetadataFieldOption MetadataFieldOption { get; set; }
    }
}
