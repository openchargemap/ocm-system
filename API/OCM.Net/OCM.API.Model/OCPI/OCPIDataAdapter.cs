using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model.OCPI
{
    public class OCPIDataAdapter
    {
        CoreReferenceData _coreReferenceData { get; set; }

        private List<RegionInfo> _countries = new List<RegionInfo>();
        public OCPIDataAdapter(CoreReferenceData coreReferenceData)
        {
            _coreReferenceData = coreReferenceData;

            // create lookup list for countries
            _countries = new List<RegionInfo>();
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                RegionInfo country = new RegionInfo(culture.LCID);
                if (_countries.Where(p => p.Name == country.Name).Count() == 0)
                    _countries.Add(country);
            }
        }

        private string GetCountryCodeFromISO3(string srcISO)
        {
          
            return _countries.FirstOrDefault(c => c.ThreeLetterISORegionName == srcISO).TwoLetterISORegionName;

        }
        public IEnumerable<OCM.API.Common.Model.ChargePoint> FromOCPI(IEnumerable<OCM.Model.OCPI.Location> source)
        {
            var output = new List<ChargePoint>();

            foreach(var i in source) {

                var iso2Code = GetCountryCodeFromISO3(i.Country);

                var cp = new ChargePoint
                {
                    DataProvidersReference = i.Id,
                    AddressInfo = new AddressInfo
                    {
                        Title = i.Name,
                        AddressLine1 = i.Address,
                        Town = i.City,
                        // i.Charging_when_closed
                        //i.AdditionalProperties
                        Latitude = double.Parse(i.Coordinates.Latitude),
                        Longitude = double.Parse(i.Coordinates.Longitude),
                        CountryID = _coreReferenceData.Countries.FirstOrDefault(c => c.ISOCode == iso2Code)?.ID,
                        AccessComments = i.Directions?.Select(d => d.Text).ToString()
                    }
                };

              if (i.AdditionalProperties.ContainsKey("evses"))
                {
                    List<OCM.Model.OCPI.EVSE> evse = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OCM.Model.OCPI.EVSE>>(i.AdditionalProperties["evses"].ToString());

                    foreach(var e in evse)
                    {
                       //TODO:
                    }
                }
                output.Add(cp);
            }
            return output;
        }
    }
}
