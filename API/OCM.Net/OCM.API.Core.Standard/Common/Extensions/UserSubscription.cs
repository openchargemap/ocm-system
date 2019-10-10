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
                ID = source.Id,
                Title = source.Title,
                CountryID = source.CountryId,
                DistanceKM = source.DistanceKm,
                
                IsEnabled = (bool)source.IsEnabled,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                NotifyComments = source.NotifyComments,
                NotifyEmergencyChargingRequests = source.NotifyEmergencyChargingRequests,
                NotifyGeneralChargingRequests = source.NotifyGeneralChargingRequests,
                NotifyMedia = source.NotifyMedia,
                NotifyPOIAdditions = source.NotifyPoiadditions,
                NotifyPOIUpdates = source.NotifyPoiupdates,
                NotifyPOIEdits = source.NotifyPoiedits,
                NotificationFrequencyMins = source.NotificationFrequencyMins,
                DateCreated = source.DateCreated,
                DateLastNotified = source.DateLastNotified,
                UserID = source.UserId
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