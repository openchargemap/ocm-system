namespace OCM.Core.Data
{
    public partial class MetadataValue
    {
        public int Id { get; set; }
        public int ChargePointId { get; set; }
        public int MetadataFieldId { get; set; }
        public string ItemValue { get; set; }
        public int? MetadataFieldOptionId { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual MetadataField MetadataField { get; set; }
        public virtual MetadataFieldOption MetadataFieldOption { get; set; }
    }
}
