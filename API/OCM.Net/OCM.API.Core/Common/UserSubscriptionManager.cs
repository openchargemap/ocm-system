using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCM.API.Common.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public enum SubscriptionMatchCategory
    {
        NewPOI,
        NewComment,
        NewMediaUpload,
        EditedPOI,
        UpdatedPOI,
        ChargingRequestGeneral,
        ChargingRequestEmergency
    }

    public class SubscriptionMatchItem
    {
        public ChargePoint POI { get; set; }

        public object Item { get; set; }
    }

    public class SubscriptionMatch
    {
        public SubscriptionMatchCategory Category { get; set; }

        public List<SubscriptionMatchItem> ItemList { get; set; }

        public string Description { get; set; }

        public SubscriptionMatch()
        {
            ItemList = new List<SubscriptionMatchItem>();
        }
    }

    public class SubscriptionMatchGroup
    {
        public DateTime DateFrom { get; set; }

        public UserSubscription Subscription { get; set; }

        public List<SubscriptionMatch> SubscriptionMatches { get; set; }

        public SubscriptionMatchGroup()
        {
            SubscriptionMatches = new List<SubscriptionMatch>();
        }
    }

    public class UserSubscriptionManager
    {
        private const int CHECK_INTERVAL_MINS = 500;

        public List<Model.UserSubscription> GetUserSubscriptions(int userId)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            var list = dataModel.UserSubscriptions.Where(u => u.UserId == userId).AsNoTracking();

            var results = new List<Model.UserSubscription>();

            foreach (var i in list)
            {
                results.Add(OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(i, true));
            }
            return results;
        }

        public Model.UserSubscription GetUserSubscription(int userId, int subscriptionId)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            var item = dataModel.UserSubscriptions.FirstOrDefault(u => u.Id == subscriptionId && u.UserId == userId);
            return OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(item, true);
        }

        public void DeleteSubscription(int userId, int subscriptionId)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            var item = dataModel.UserSubscriptions.FirstOrDefault(u => u.Id == subscriptionId && u.UserId == userId);
            if (item != null)
            {
                dataModel.UserSubscriptions.Remove(item);
                dataModel.SaveChanges();
            }
        }

        public Model.UserSubscription UpdateUserSubscription(Model.UserSubscription subscription)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            Core.Data.UserSubscription update = new Core.Data.UserSubscription();
            if (subscription.ID > 0)
            {
                //edit
                update = dataModel.UserSubscriptions.FirstOrDefault(u => u.Id == subscription.ID && u.UserId == subscription.UserID);
            }
            else
            {
                //new item
                update.User = dataModel.Users.FirstOrDefault(u => u.Id == subscription.UserID);
                update.DateCreated = DateTime.UtcNow;
                update.DateLastNotified = update.DateCreated;
            }
            if (update != null)
            {
                update.Title = subscription.Title;
                update.IsEnabled = subscription.IsEnabled;
                if (subscription.CountryID != null)
                {
                    update.Country = dataModel.Countries.FirstOrDefault(c => c.Id == subscription.CountryID);
                }
                else
                {
                    update.Country = null;
                    update.CountryId = null;
                }

                update.DistanceKm = subscription.DistanceKM;

                if (subscription.FilterSettings != null)
                {
                    update.FilterSettings = JsonConvert.SerializeObject(subscription.FilterSettings);
                }
                else
                {
                    update.FilterSettings = null;
                }

                update.Latitude = subscription.Latitude;
                update.Longitude = subscription.Longitude;
                update.NotificationFrequencyMins = subscription.NotificationFrequencyMins;
                update.NotifyComments = subscription.NotifyComments;
                update.NotifyEmergencyChargingRequests = subscription.NotifyEmergencyChargingRequests;
                update.NotifyGeneralChargingRequests = subscription.NotifyGeneralChargingRequests;
                update.NotifyMedia = subscription.NotifyMedia;
                update.NotifyPoiadditions = subscription.NotifyPOIAdditions;
                update.NotifyPoiedits = subscription.NotifyPOIEdits;
                update.NotifyPoiupdates = subscription.NotifyPOIUpdates;
            }

            if (update.Id == 0)
            {
                dataModel.UserSubscriptions.Add(update);
            }
            dataModel.SaveChanges();

            return OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(update, true);
        }

        public SubscriptionMatchGroup GetSubscriptionMatches(int subscriptionId, int userId, DateTime? dateFrom)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            var poiManager = new POIManager();

            var subscription = dataModel.UserSubscriptions.FirstOrDefault(s => s.Id == subscriptionId && s.UserId == userId);
            if (subscription != null)
            {
                return GetSubscriptionMatches(dataModel, poiManager, subscription, dateFrom);
            }
            else
            {
                return null;
            }
        }

        private bool IsPOISubscriptionFilterMatch(ChargePoint poi, UserSubscriptionFilter filter, OCM.Core.Data.UserSubscription subscription)
        {
            if (poi == null) return false;

            bool isMatch = true;

            if (subscription.CountryId != null)
            {
                if (poi.AddressInfo.CountryID != subscription.CountryId) isMatch = false;
            }

            if (filter == null) return isMatch; //no more filtering to do

            if (filter.ConnectionTypeIDs != null && filter.ConnectionTypeIDs.Any())
            {
                //if no matching connection types, poi is not a match
                foreach (var connTypeId in filter.ConnectionTypeIDs)
                {
                    if (!poi.Connections.Any(c => c.ConnectionTypeID == connTypeId)) isMatch = false;
                }
            }

            if (filter.LevelIDs != null && filter.LevelIDs.Any())
            {
                //if no matching levels Ids, poi is not a match
                foreach (var levelTypeId in filter.LevelIDs)
                {
                    if (!poi.Connections.Any(c => c.LevelID == levelTypeId)) isMatch = false;
                }
            }

            if (filter.OperatorIDs != null && filter.OperatorIDs.Any() && !filter.OperatorIDs.Any(op => op == poi.OperatorID))
            {
                //does not match by operator ID
                isMatch = false;
            }

            if (filter.StatusTypeIDs != null && filter.StatusTypeIDs.Any() && !filter.StatusTypeIDs.Any(st => st == poi.StatusTypeID))
            {
                //does not match by status type ID
                isMatch = false;
            }

            if (filter.UsageTypeIDs != null && filter.UsageTypeIDs.Any() && !filter.UsageTypeIDs.Any(usage => usage == poi.UsageTypeID))
            {
                //does not match by usage type ID
                isMatch = false;
            }

            return isMatch;
        }

        public SubscriptionMatchGroup GetSubscriptionMatches(OCM.Core.Data.OCMEntities dataModel, POIManager poiManager, OCM.Core.Data.UserSubscription subscription, DateTime? dateFrom = null)
        {
            SubscriptionMatchGroup subscriptionMatchGroup = new SubscriptionMatchGroup();

            var checkFromDate = DateTime.UtcNow.AddMinutes(-CHECK_INTERVAL_MINS); //check from last 5 mins etc
            if (subscription.DateLastNotified != null) checkFromDate = subscription.DateLastNotified.Value; //check from date last notified
            else checkFromDate = subscription.DateCreated;

            if (dateFrom != null) checkFromDate = dateFrom.Value;

            subscriptionMatchGroup.DateFrom = checkFromDate;

            //FIXME: re-test with updated coordinate objects (nettopology etc)
            GeoCoordinatePortable.GeoCoordinate searchPos = null;

            UserSubscriptionFilter filter = null;
            if (subscription.FilterSettings != null)
            {
                try
                {
                    filter = JsonConvert.DeserializeObject<UserSubscriptionFilter>(subscription.FilterSettings);
                }
                catch (Exception)
                {
                    //failed to parse subscription filter
                }
            }

            if (subscription.Latitude != null && subscription.Longitude != null) searchPos = new GeoCoordinatePortable.GeoCoordinate((double)subscription.Latitude, (double)subscription.Longitude);

            if (subscription.NotifyEmergencyChargingRequests)
            {
                var emergencyCharging = dataModel.UserChargingRequests.Where(c => c.DateCreated >= checkFromDate && c.IsActive == true && c.IsEmergency == true);
                var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.ChargingRequestEmergency, Description = "New Emergency Charging Requests" };

                foreach (var chargingRequest in emergencyCharging)
                {
                    //filter on location
                    if (searchPos != null)
                    {
                        if (GeoManager.CalcDistance(chargingRequest.Latitude, chargingRequest.Longitude, (double)searchPos.Latitude, (double)searchPos.Longitude, DistanceUnit.KM) < subscription.DistanceKm)
                        {
                            subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = chargingRequest });
                        }
                    }
                    else
                    {
                        subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = chargingRequest });
                    }
                }
                if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
            }

            if (subscription.NotifyGeneralChargingRequests)
            {
                //TODO: subscription not filtered on lat/long will return global charging requests
                var generalCharging = dataModel.UserChargingRequests.Where(c => c.DateCreated >= checkFromDate && c.IsActive == true && c.IsEmergency == false);
                var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.ChargingRequestGeneral, Description = "New Charging Requests" };
                //filter on location
                foreach (var gc in generalCharging)
                {
                    if (searchPos != null)
                    {
                        if (GeoManager.CalcDistance(gc.Latitude, gc.Longitude, (double)searchPos.Latitude, (double)searchPos.Longitude, DistanceUnit.KM) < subscription.DistanceKm)
                        {
                            subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = gc });
                        }
                    }
                    else
                    {
                        subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = gc });
                    }
                }

                if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
            }

            //check if any POI Edits (pending approval) match this subscription
            if (subscription.NotifyPoiedits)
            {
                var poiEdits = dataModel.EditQueueItems.Where(c => c.DateSubmitted >= checkFromDate && c.PreviousData != null && c.IsProcessed == false);
                if (poiEdits.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.EditedPOI, Description = "Proposed POI Edits" };
                    foreach (var p in poiEdits)
                    {
                        try
                        {
                            var updatedPOI = JsonConvert.DeserializeObject<ChargePoint>(p.EditData);
                            if (IsPOISubscriptionFilterMatch(updatedPOI, filter, subscription))
                            {
                                if (searchPos != null)
                                {
                                    if (GeoManager.CalcDistance(updatedPOI.AddressInfo.Latitude, updatedPOI.AddressInfo.Longitude, (double)searchPos.Latitude, (double)searchPos.Longitude, DistanceUnit.KM) < subscription.DistanceKm)
                                    {
                                        subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = p, POI = updatedPOI });
                                    }
                                }
                                else
                                {
                                    subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = p, POI = updatedPOI });
                                }
                            }
                        }
                        catch (Exception)
                        {
                            ; ;
                        }
                    }
                    if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any new POIs
            if (subscription.NotifyPoiadditions)
            {
                /* var newPOIs = dataModel.ChargePoints.Where(c => c.DateCreated >= checkFromDate && c.SubmissionStatusType.IsLive == true &&
                      (searchPos == null ||
                         (searchPos != null &&
                             c.AddressInfo.SpatialPosition.Distance(searchPos) / 1000 < subscription.DistanceKM
                         ))
                       ).ToList();*/

                var filterParams = new APIRequestParams { CreatedFromDate = checkFromDate, AllowMirrorDB = true };
                if (searchPos != null)
                {
                    filterParams.DistanceUnit = DistanceUnit.KM;
                    filterParams.Distance = subscription.DistanceKm;
                    filterParams.Latitude = searchPos.Latitude;
                    filterParams.Longitude = searchPos.Longitude;
                }
                if (subscription.CountryId != null)
                {
                    filterParams.CountryIDs = new int[] { (int)subscription.CountryId };
                }
                var poiCollection = poiManager.GetPOIList(filterParams);

                if (poiCollection.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewPOI, Description = "New POIs Added" };
                    foreach (var p in poiCollection)
                    {
                        //var poi = OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(p);
                        if (IsPOISubscriptionFilterMatch(p, filter, subscription))
                        {
                            subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { POI = p });
                        }
                    }
                    if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any POI Updates match this subscription
            if (subscription.NotifyPoiupdates)
            {
                var poiUpdates = dataModel.EditQueueItems.Where(c => c.DateProcessed >= checkFromDate && c.IsProcessed == true && c.PreviousData != null);
                if (poiUpdates.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.UpdatedPOI, Description = "POIs Updated" };
                    foreach (var p in poiUpdates)
                    {
                        try
                        {
                            var updatedPOI = JsonConvert.DeserializeObject<ChargePoint>(p.EditData);
                            if (IsPOISubscriptionFilterMatch(updatedPOI, filter, subscription))
                            {
                                if (searchPos != null)
                                {
                                    if (GeoManager.CalcDistance(updatedPOI.AddressInfo.Latitude, updatedPOI.AddressInfo.Longitude, (double)searchPos.Latitude, (double)searchPos.Longitude, DistanceUnit.KM) < subscription.DistanceKm)
                                    {
                                        subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = p, POI = updatedPOI });
                                    }
                                }
                                else
                                {
                                    subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = p, POI = updatedPOI });
                                }
                            }
                        }
                        catch (Exception)
                        {
                            ; ;
                        }
                    }
                    if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any new comments match this subscription
            if (subscription.NotifyComments)
            {
                var newComments = dataModel.UserComments.Where(c => c.DateCreated >= checkFromDate &&
                    (searchPos == null ||
                        (searchPos != null &&
                            c.ChargePoint.AddressInfo.SpatialPosition.Distance(new NetTopologySuite.Geometries.Point((double)searchPos.Latitude, (double)searchPos.Longitude)) / 1000 < subscription.DistanceKm
                        ))
                      );
                if (newComments.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewComment, Description = "New Comments Added" };
                    foreach (var c in newComments)
                    {
                        var poi = OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(c.ChargePoint);
                        if (IsPOISubscriptionFilterMatch(poi, filter, subscription))
                        {
                            subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = OCM.API.Common.Model.Extensions.UserComment.FromDataModel(c, true), POI = poi });
                        }
                    }
                    if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any new Media uploads match this subscription
            if (subscription.NotifyMedia)
            {
                var newMedia = dataModel.MediaItems.Where(c => c.DateCreated >= checkFromDate &&
                     (searchPos == null ||
                        (searchPos != null &&
                            c.ChargePoint.AddressInfo.SpatialPosition.Distance(new NetTopologySuite.Geometries.Point((double)searchPos.Latitude, (double)searchPos.Longitude)) / 1000 < subscription.DistanceKm
                        ))
                      );
                if (newMedia.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewMediaUpload, Description = "New Photos Added" };
                    foreach (var c in newMedia)
                    {
                        var poi = OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(c.ChargePoint);
                        if (IsPOISubscriptionFilterMatch(poi, filter, subscription))
                        {
                            subscriptionMatch.ItemList.Add(new SubscriptionMatchItem { Item = OCM.API.Common.Model.Extensions.MediaItem.FromDataModel(c), POI = poi });
                        }
                    }
                    if (subscriptionMatch.ItemList.Any()) subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            return subscriptionMatchGroup;
        }

        /// <summary>
        /// Get all subscription matches for all active subscriptions where applicable
        /// </summary>
        /// <returns></returns>
        public List<SubscriptionMatchGroup> GetAllSubscriptionMatches(bool excludeRecentlyNotified = true)
        {
            List<SubscriptionMatchGroup> allMatches = new List<SubscriptionMatchGroup>();

            //TODO: performance/optimisation (use cache for POI queries is done)
            //for each subscription, check if any changes match the criteria
            var dataModel = new OCM.Core.Data.OCMEntities();
            //var cacheManager = new OCM.Core.Data.CacheProviderMongoDB();
            var poiManager = new POIManager();
            var currentTime = DateTime.UtcNow;

            int minHoursBetweenNotifications = 3;
            var minDateSinceNotification = currentTime.AddHours(-minHoursBetweenNotifications);
            var allsubscriptions = dataModel.UserSubscriptions.Where(s => s.IsEnabled == true && s.User.EmailAddress != null && (s.DateLastNotified == null || s.DateLastNotified < minDateSinceNotification));
            foreach (var subscription in allsubscriptions)
            {
                if (!excludeRecentlyNotified || (subscription.DateLastNotified == null || subscription.DateLastNotified.Value < currentTime.AddMinutes(-subscription.NotificationFrequencyMins)))
                {
                    var subscriptionMatchGroup = GetSubscriptionMatches(dataModel, poiManager, subscription);
                    if (subscriptionMatchGroup.SubscriptionMatches.Any())
                    {
                        subscriptionMatchGroup.Subscription = OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(subscription, true);
                        allMatches.Add(subscriptionMatchGroup);
                    }
                }
            }
            return allMatches;
        }

        public string GetSubscriptionMatchHTMLSummary(SubscriptionMatchGroup subscriptionResults)
        {
            int maxItemsShown = 20;
            var summaryHTML = "";
            string siteUrl = "https://openchargemap.org/site";
            string poiLinkText = "View POI Details";

            if (subscriptionResults.SubscriptionMatches == null || subscriptionResults.SubscriptionMatches.Count == 0)
            {
                return "<p>No Subscription Matches</p>";
            }
            summaryHTML += "<p>This subscription matches the following items which have been updated since " + subscriptionResults.DateFrom.ToString() + ": <ul>";
            foreach (var match in subscriptionResults.SubscriptionMatches)
            {
                //output a summary
                summaryHTML += "<li><a href='#" + match.Category.ToString() + "'>" + match.Description + " (" + match.ItemList.Count + ")</a></li>";
            }

            summaryHTML += "</ul>";

            foreach (var match in subscriptionResults.SubscriptionMatches)
            {
                if (match.ItemList != null && match.ItemList.Count > 0)
                {
                    summaryHTML += "<h3 id='" + match.Category.ToString() + "'>" + match.Description + " (" + match.ItemList.Count + ")</h3>";

                    if (match.Category == SubscriptionMatchCategory.ChargingRequestEmergency)
                    {
                        //emergency charging
                        foreach (var i in match.ItemList)
                        {
                            var r = (Core.Data.UserChargingRequest)i.Item;
                            summaryHTML += "<div class='result'><blockquote>" + r.Comment + "</blockquote><p>" + (r.User != null ? r.User.Username : "(Anonymous)") + " - " + r.DateCreated.ToShortDateString() + "</p></div>";
                        }
                    }

                    if (match.Category == SubscriptionMatchCategory.ChargingRequestGeneral)
                    {
                        //general charging
                        foreach (var i in match.ItemList)
                        {
                            var r = (Core.Data.UserChargingRequest)i.Item;
                            summaryHTML += "<div class='result'><blockquote>" + r.Comment + "</blockquote><p>" + (r.User != null ? r.User.Username : "(Anonymous)") + " - " + r.DateCreated.ToShortDateString() + "</p></div>";
                        }
                    }

                    if (match.Category == SubscriptionMatchCategory.EditedPOI)
                    {
                        //edits
                        summaryHTML += "<a href='" + siteUrl + "/editqueue'>View Edit Queue</a> <ul>";
                        foreach (var i in match.ItemList)
                        {
                            var item = (Core.Data.EditQueueItem)i.Item;
                            var poi = (ChargePoint)i.POI;
                            summaryHTML += "<li>OCM-" + poi.ID + ": " + poi.AddressInfo.Title + " : " + poi.AddressInfo.ToString().Replace(",", ", ") + " <a href='" + siteUrl + "/poi/details/" + poi.ID + "'>" + poiLinkText + "</a></li>";
                        }
                        summaryHTML += "</ul>";
                    }

                    if (match.Category == SubscriptionMatchCategory.NewPOI)
                    {
                        //POI list

                        summaryHTML += "<ul>";
                        foreach (var p in match.ItemList.Take(maxItemsShown))
                        {
                            var poi = (ChargePoint)p.POI;
                            summaryHTML += "<li>OCM-" + poi.ID + ": " + poi.AddressInfo.Title + " : " + poi.AddressInfo.ToString().Replace(",", ", ") + " <a href='" + siteUrl + "/poi/details/" + poi.ID + "'>" + poiLinkText + "</a></li>";
                        }
                        summaryHTML += "</ul>";

                        if (match.ItemList.Count > maxItemsShown)
                        {
                            summaryHTML += "<p>In addition there were " + (match.ItemList.Count - maxItemsShown) + " more items added.</p>";
                        }
                    }

                    if (match.Category == SubscriptionMatchCategory.UpdatedPOI)
                    {
                        //updates
                        summaryHTML += "<ul>";
                        foreach (var i in match.ItemList.Take(maxItemsShown))
                        {
                            var item = (Core.Data.EditQueueItem)i.Item;
                            var poi = (ChargePoint)i.POI;
                            summaryHTML += "<li>OCM-" + poi.ID + ": " + poi.AddressInfo.Title + " : " + poi.AddressInfo.ToString().Replace(",", ", ") + " <a href='" + siteUrl + "/poi/details/" + poi.ID + "'>" + poiLinkText + "</a></li>";
                        }
                        summaryHTML += "</ul>";

                        if (match.ItemList.Count > maxItemsShown)
                        {
                            summaryHTML += "<p>In addition there were " + (match.ItemList.Count - maxItemsShown) + " more items updated.</p>";
                        }
                    }

                    if (match.Category == SubscriptionMatchCategory.NewComment)
                    {
                        //comment list
                        summaryHTML += "<div class='results-list'>";

                        foreach (var i in match.ItemList)
                        {
                            var comment = (UserComment)i.Item;
                            var poi = (ChargePoint)i.POI;
                            summaryHTML += "<div class='result'>" + FormatPOIHeading(poi) + "<div style='padding:0.2em;'>" + (!String.IsNullOrEmpty(comment.UserName) ? comment.UserName : "(Anonymous)") + " - " + comment.DateCreated.ToShortDateString() + "<br/>";
                            if (!String.IsNullOrEmpty(comment.Comment))
                            {
                                summaryHTML += "<blockquote>";
                                summaryHTML += comment.Comment;
                                summaryHTML += "</blockquote>";
                            }
                            if (comment.Rating != null) summaryHTML += "" + comment.Rating + " out of 5";
                            if (comment.CommentType != null) summaryHTML += " <span class='label label-info'>" + comment.CommentType.Title + "</span>";
                            if (comment.CheckinStatusType != null)
                            {
                                summaryHTML += " <span class='label " + (comment.CheckinStatusType.IsPositive == true ? "label-success" : "label-danger") + "'>" + comment.CheckinStatusType.Title + "</span>";
                            }

                            summaryHTML += "</div></div>";
                        }
                        summaryHTML += "</div>";
                    }

                    if (match.Category == SubscriptionMatchCategory.NewMediaUpload)
                    {
                        //media list
                        summaryHTML += "<div class='results-list'>";

                        foreach (var i in match.ItemList)
                        {
                            var item = (MediaItem)i.Item;
                            var poi = (ChargePoint)i.POI;
                            summaryHTML += "<div class='result'>" + FormatPOIHeading(poi) + "<div style='padding:0.2em;'>" + (item.User != null ? item.User.Username : "(Anonymous)") + " - " + item.DateCreated.ToShortDateString() + "<br/><blockquote><img src='" + item.ItemThumbnailURL + "'/>" + item.Comment + "</blockquote></div></div>";
                        }
                        summaryHTML += "</div>";
                    }
                }
            }

            return summaryHTML;
        }

        private string FormatPOIHeading(ChargePoint poi)
        {
            string siteUrl = "https://openchargemap.org/site";
            var html = "<h3><a href='" + siteUrl + "/poi/details/" + poi.ID + "'>OCM-" + poi.ID + ": " + poi.AddressInfo.Title + " : " + poi.AddressInfo.ToString().Replace(",", ", ") + "</a></h3>";
            return html;
        }

        public int SendAllPendingSubscriptionNotifications(string templateFolderPath)
        {
            int notificationsSent = 0;
            List<int> subscriptionsNotified = new List<int>();
            List<int> subscriptionsSkipped = new List<int>();

            var allSubscriptionMatches = GetAllSubscriptionMatches(true);
            var userManager = new UserManager();
            NotificationManager notificationManager = new NotificationManager();

            notificationManager.TemplateFolderPath = templateFolderPath;

            foreach (var subscriptionMatch in allSubscriptionMatches)
            {
                if (subscriptionMatch.SubscriptionMatches != null && subscriptionMatch.SubscriptionMatches.Count > 0)
                {
                    bool hasItemMatches = subscriptionMatch.SubscriptionMatches.Any(m => m.ItemList.Count > 0);

                    if (hasItemMatches)
                    {
                        string summaryHTML = GetSubscriptionMatchHTMLSummary(subscriptionMatch);
                        var userDetails = userManager.GetUser(subscriptionMatch.Subscription.UserID);

                        //prepare and send email
                        Hashtable msgParams = new Hashtable();
                        msgParams.Add("SummaryContent", summaryHTML);
                        msgParams.Add("SubscriptionTitle", subscriptionMatch.Subscription.Title);

                        msgParams.Add("UserName", userDetails.Username);
                        msgParams.Add("SubscriptionEditURL", "https://openchargemap.org/site/profile/subscriptionedit?id=" + subscriptionMatch.Subscription.ID);

                        if (!String.IsNullOrEmpty(userDetails.EmailAddress))
                        {
                            notificationManager.PrepareNotification(NotificationType.SubscriptionNotification, msgParams);
                            bool sentOK = notificationManager.SendNotification(userDetails.EmailAddress);
                            if (sentOK)
                            {
                                notificationsSent++;
                                subscriptionsNotified.Add(subscriptionMatch.Subscription.ID);
                            }
                        }
                    }
                    else
                    {
                        subscriptionsSkipped.Add(subscriptionMatch.Subscription.ID);
                    }

                    //mark all subscriptions notified where sent ok
                    var dataModel = new OCM.Core.Data.OCMEntities();
                    foreach (var subscriptionId in subscriptionsNotified)
                    {
                        var s = dataModel.UserSubscriptions.Find(subscriptionId);
                        s.DateLastNotified = DateTime.UtcNow;
                    }

                    //mark all subscriptions with no matching items as processed/skipped
                    foreach (var subscriptionId in subscriptionsSkipped)
                    {
                        var s = dataModel.UserSubscriptions.Find(subscriptionId);
                        s.DateLastNotified = DateTime.UtcNow;
                    }
                    dataModel.SaveChanges();
                }
            }
            return notificationsSent;
        }
    }
}