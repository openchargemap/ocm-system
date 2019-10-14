using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.MVC.Models
{
    public class ImportManager
    {
        public List<Import.Providers.IImportProvider> ImportProviders { get; set; }
    }
}