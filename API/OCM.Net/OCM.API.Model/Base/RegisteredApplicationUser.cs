using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class RegisteredApplicationUser : SimpleReferenceDataType
    {
        public int UserID { get; set; }
        public string APIKey { get; set; }
        public bool IsWriteEnabled { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime DateCreated { get; set; }
    }
}