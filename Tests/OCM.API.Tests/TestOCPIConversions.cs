using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OCM.API.Common;
using Xunit;

namespace OCM.API.Tests
{
    public class TestOCPIConversions
    {
        [Fact]
        void CanConvertFromOCPI()
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var json = System.IO.File.ReadAllText(path + "\\Assets\\ocpi_location_example.json");

            ReferenceDataManager refDataManager = new ReferenceDataManager();
            var coreRefData = refDataManager.GetCoreReferenceData();

            Common.Model.OCPI.OCPIDataAdapter adapter = new Common.Model.OCPI.OCPIDataAdapter(coreRefData);

            var ocpiList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.Location>>(json);
            var poiResults = adapter.FromOCPI(ocpiList);

            Assert.Equal(1, poiResults.Count());
        }
    }
}
