using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserSubscription
    {
        public static Model.UserSubscription FromDataModel(Core.Data.UserSubscription source, bool isVerboseMode)
        {
            if (source == null) return null;

            var item = new Model.UserSubscription
            {
                ID = source.ID,
                Title = source.Title,
                CountryID = source.CountryID,
                DistanceKM = source.DistanceKM,
                
                IsEnabled = source.IsEnabled,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                NotifyComments = source.NotifyComments,
                NotifyEmergencyChargingRequests = source.NotifyEmergencyChargingRequests,
                NotifyGeneralChargingRequests = source.NotifyGeneralChargingRequests,
                NotifyMedia = source.NotifyMedia,
                NotifyPOIAdditions = source.NotifyPOIAdditions,
                NotifyPOIUpdates = source.NotifyPOIUpdates,
                NotifyPOIEdits = source.NotifyPOIEdits,
                NotificationFrequencyMins = source.NotificationFrequencyMins,
                DateCreated = source.DateCreated,
                DateLastNotified = source.DateLastNotified,
                UserID = source.UserID
            };

            if (source.FilterSettings!=null){
                item.FilterSettings = JsonConvert.DeserializeObject<UserSubscriptionFilter>(source.FilterSettings);
            }

            if (isVerboseMode)
            {
                item.Country = OCM.API.Common.Model.Extensions.Country.FromDataModel(source.Country);
            }

            return item;
        }
    }
}