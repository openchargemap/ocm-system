using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class DataProviderUser
    {
        public int ID { get; set; }
        public DataProvider DataProvider { get; set; }
        public User User { get; set; }
        public bool IsDataProviderAdmin { get; set; }
    }
}