using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class Country
    {
        public Country()
        {
            this.AddressInfoes = new List<AddressInfo>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string ISOCode { get; set; }
        public virtual ICollection<AddressInfo> AddressInfoes { get; set; }
    }
}
