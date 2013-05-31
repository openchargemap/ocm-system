using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web.Script.Serialization;
using OCM.Import.NetworkServices.ThirdPartyServices;

namespace OCM.API.NetworkServices
{
    public class ReservationStatus
    {

    }

    public class ReservationRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime DurationMinutes { get; set; }
        public string[] Credentials { get; set; }
        public string Reference { get; set; }
    }

    public class EquipmentStatus
    {
        public int OCM_ID { get; set; }
        public string Status { get; set; }
    }


    public enum ServiceProvider
    {
        CoulombChargePoint = 10,
        SemaConnect = 20,
        BlinkNetwork = 30
    }

    public class ServiceManager
    {
        
        public string GetAllResultsAsJSON(ServiceProvider serviceProvider, string apiKey, string apiPwd)
        {
            if (serviceProvider == ServiceProvider.CoulombChargePoint)
            {
                //Coulomb notes: logdata.endTime field in Reference.cs autocreated as DateTime but needs to be serialized as string as value is often 24:00, change property type to string
                OCM.Import.NetworkServices.ThirdPartyServices.Coulomb.coulombservicesClient svc = new Import.NetworkServices.ThirdPartyServices.Coulomb.coulombservicesClient();
                svc.ClientCredentials.UserName.UserName = apiKey;
                svc.ClientCredentials.UserName.Password = apiPwd;

                string output = "";
                OCM.Import.NetworkServices.ThirdPartyServices.Coulomb.logdata[] stationData = { new OCM.Import.NetworkServices.ThirdPartyServices.Coulomb.logdata() { } };
                string result = svc.getAllUSStations(new OCM.Import.NetworkServices.ThirdPartyServices.Coulomb.stationSearchRequest()
                {
                   // Geo = new Import.NetworkServices.ThirdPartyServices.Coulomb.stationSearchRequestGeo { lat = "38.5846", @long = "-121.4961" },
                    Country = "USA",
                    Proximity = 50000,
                    postalCode = "95816"
                },
                out output, out stationData);

                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = 10000000;
                string outputJS = js.Serialize(stationData);
                System.Diagnostics.Debug.WriteLine("<json>"+outputJS+"</json>");
                return outputJS;
            }
            return null;
        }

        public int CheckAvailability(int id, DateTime startDate, int durationMins, string[] credentials)
        {
            return 0;
        }

        public bool PerformReservation(int id, DateTime startDate, int durationMins, string[] credentials)
        {
            return false;
        }

        public bool CancelReservation(int id, DateTime startDate, int durationMins, string[] credentials)
        {
            return false;
        }

        public bool UpdateOCMStatus(int id, string connectionRef, int statusTypeID)
        {
            return true;
        }

        public EquipmentStatus GetEquipmentStatus(int id)
        {
            //looking relevant network provider for equipment

            //if network provider supported, query status

            return new EquipmentStatus() { };
        }

    }
}
