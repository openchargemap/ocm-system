namespace OCM.API.Common.Model
{
    public class Country : SimpleReferenceDataType
    {
        public string ISOCode { get; set; }
        public string ContinentCode { get; set; }
    }
}