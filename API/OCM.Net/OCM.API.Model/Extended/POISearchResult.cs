using System.Runtime.Serialization;

namespace OCM.API.Common.Model
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class POISearchResult
    {
        [DataMember(Name = "id")]
        public int ID { get; set; }

        [DataMember(Name = "t")]
        public string Title { get; set; }

        [DataMember(Name = "u")]
        public int UsageTypeID { get; set; }

        [DataMember(Name = "s")]
        public int StatusTypeID { get; set; }

        [DataMember(Name = "a")]
        public string Address { get; set; }

        [DataMember(Name = "p")]
        public string Postcode { get; set; }

        [DataMember(Name = "lt")]
        public double Latitude { get; set; }

        [DataMember(Name = "lg")]
        public double Longitude { get; set; }

        [DataMember(Name = "d")]
        public double? Distance { get; set; }
    }
}
