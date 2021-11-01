using System;
using System.Collections.Generic;

namespace OCM.Import
{
    public class ExtendedAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class EVSE
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string ID { get; set; }
        public DateTime Updated { get; set; }
        public string Content { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Status { get; set; }
        public List<ExtendedAttribute> ExtendedAttributes { get; set; }
    }
}
