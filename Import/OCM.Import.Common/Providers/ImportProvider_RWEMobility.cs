using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Common.Model;
using System.Web.Script.Serialization;

namespace OCM.Import.Providers
{
    public class ImportProvider_RWEMobility : BaseImportProvider, IImportProvider
    {
        public ImportProvider_RWEMobility()
        {
            ProviderName = "RWE-Mobility.com";
            OutputNamePrefix = "rwe-mobility";
            AutoRefreshURL = "https://www.rwe-mobility.com/emobilityfrontend/rs/chargingstations/";
            IsAutoRefreshed = true;
            IsProductionReady = true;
        }

        public List<API.Common.Model.ChargePoint> Process(CoreReferenceData coreRefData)
        {
            ImportCommonReferenceData importRefData = new ImportCommonReferenceData(coreRefData);
            List<ChargePoint> outputList = new List<ChargePoint>();

            string jsString = InputData;// "{ \"data\": " + InputData + "}";
            JavaScriptSerializer jss = new JavaScriptSerializer();
            jss.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });
            dynamic parsedList = jss.Deserialize(jsString, typeof(object)) as dynamic;
          
            var dataList = parsedList.chargingstations;
          
            int itemCount = 0;
            foreach (var item in dataList)
            {
                /*
                 * {
               "chargingstations":[
                  {
                     "id":"14249",
                     "oid":"EM-NL-00000363-06-00006",
                     "brand":"Station City of Amsterdam",
                     "location_name":null,
                     "location_name_lang":null,
                     "latitude":52.401097,
                     "longitude":4.932926,
                     "street":"Buikslotermeerplein",
                     "house_number":"2000",
                     "postal_code":"1025 XL",
                     "city":"Amsterdam",
                     "country":"Netherlands",
                     "country_iso":"NLD",
                     "phone":"00318003773683",
                     "ac":true,
                     "dc":false,
                     "free":true,
                     "occupied":false,
                     "spotIds":"BA-3665-1, BA-3066-8"
                  },
                 * */
                ChargePoint cp = new ChargePoint();
                cp.DataProvider = new DataProvider() { ID = 21 }; //rwe-mobility
                cp.DataProvidersReference = item["id"].ToString();
                cp.OperatorsReference = item["oid"].ToString();
                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo = new AddressInfo();

                try
                {
                    
                    cp.AddressInfo.Title = item["location_name"] != null ? item["location_name"].ToString() : item["brand"].ToString();
                    cp.AddressInfo.Title = cp.AddressInfo.Title.Trim().Replace("&amp;", "&");
                }
                catch (Exception)
                {
                    //could not get a location title
                    System.Diagnostics.Debug.WriteLine("No title for item: "+cp.DataProvidersReference);
                }
                //cp.AddressInfo.RelatedURL = item["url"].ToString();

                cp.DateLastStatusUpdate = DateTime.Now;
                cp.AddressInfo.AddressLine1 = item["house_number"] != null ? item["house_number"]+" " +item["street"].ToString():item["street"].ToString().Trim();
                cp.AddressInfo.Town = item["city"].ToString().Trim();
                cp.AddressInfo.Postcode = item["postal_code"].ToString().Trim();
                if (String.IsNullOrEmpty(cp.AddressInfo.Title)) cp.AddressInfo.Title = cp.AddressInfo.Town;

                cp.AddressInfo.Latitude = double.Parse(item["latitude"].ToString());
                cp.AddressInfo.Longitude = double.Parse(item["longitude"].ToString());

                //default to norway
                if (item["phone"] != null)
                {
                    cp.AddressInfo.ContactTelephone1 = item["phone"].ToString();
                }

                if (!String.IsNullOrEmpty(item["country"].ToString()))
                {
                    string country = item["country"].ToString();
                    int? countryID = null;

                    var countryVal = coreRefData.Countries.FirstOrDefault(c => c.Title.ToLower() == country.Trim().ToLower());
                    if (countryVal == null)
                    {
                        country = country.ToUpper();
                        //match country
                        if (country == "UNITED STATES" || country == "US" || country == "USA" || country == "U.S." || country == "U.S.A.") countryID = 2;

                        if (country == "UK" || country == "GB" || country == "GREAT BRITAIN" || country == "UNITED KINGDOM") countryID = 1;
                    }
                    else
                    {
                        countryID = countryVal.ID;
                    }

                    if (countryID == null)
                    {
                        this.Log("Country Not Matched, will require Geolocation:" + item["country"].ToString());

                    }
                    else
                    {
                        cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == countryID);
                    }
                }
                else
                {
                    //default to US if no country identified
                    //cp.AddressInfo.Country = cp.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(cy => cy.ID == 2);
                }

                cp.UsageType = null;
                //TODO:identify usage type, number of points, connections

                cp.NumberOfPoints = 1;
                cp.StatusType = importRefData.Status_Operational;


                if (cp.DataQualityLevel == null) cp.DataQualityLevel = 2;

                cp.SubmissionStatus = importRefData.SubmissionStatus_ImportedAndPublished;

                outputList.Add(cp);
                itemCount++;
            }

            return outputList;
        }
  
    }
}
