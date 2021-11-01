using System.Collections.Generic;

namespace OCM.MVC.Models
{
    public class ImportManager
    {
        public List<Import.Providers.IImportProvider> ImportProviders { get; set; }
    }
}