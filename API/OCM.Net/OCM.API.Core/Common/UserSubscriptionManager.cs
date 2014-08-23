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

    public class SubscriptionMatch
    {
        public SubscriptionMatchCategory Category { get; set; }

        public List<Object> ItemList { get; set; }

        public string Description { get; set; }

        public SubscriptionMatch()
        {
            ItemList = new List<Object>();
        }
    }

    public class SubscriptionMatchGroup
    {
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
            var list = dataModel.UserSubscriptions.Where(u => u.UserID == userId);

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
            var item = dataModel.UserSubscriptions.FirstOrDefault(u => u.ID == subscriptionId && u.UserID == userId);
            return OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(item, true);
        }

        public Model.UserSubscription UpdateUserSubscription(Model.UserSubscription subscription)
        {
            var dataModel = new OCM.Core.Data.OCMEntities();
            Core.Data.UserSubscription update = new Core.Data.UserSubscription();
            if (subscription.ID > 0)
            {
                //edit
                update = dataModel.UserSubscriptions.FirstOrDefault(u => u.ID == subscription.ID && u.UserID == subscription.UserID);
            }
            else
            {
                //new item
                update.User = dataModel.Users.FirstOrDefault(u => u.ID == subscription.UserID);
                update.DateCreated = DateTime.UtcNow;
                update.DateLastNotified = update.DateCreated;
            }
            if (update != null)
            {
                update.Title = subscription.Title;
                update.IsEnabled = subscription.IsEnabled;
                update.Country = dataModel.Countries.FirstOrDefault(c => c.ID == subscription.CountryID);
                update.DistanceKM = subscription.DistanceKM;
                update.FilterSettings = subscription.FilterSettings;
                update.Latitude = subscription.Latitude;
                update.Longitude = subscription.Longitude;
                update.NotificationFrequencyMins = subscription.NotificationFrequencyMins;
                update.NotifyComments = subscription.NotifyComments;
                update.NotifyEmergencyChargingRequests = subscription.NotifyEmergencyChargingRequests;
                update.NotifyGeneralChargingRequests = subscription.NotifyGeneralChargingRequests;
                update.NotifyMedia = subscription.NotifyMedia;
                update.NotifyPOIAdditions = subscription.NotifyPOIAdditions;
                update.NotifyPOIEdits = subscription.NotifyPOIEdits;
                update.NotifyPOIUpdates = subscription.NotifyPOIUpdates;
            }

            if (update.ID == 0)
            {
                dataModel.UserSubscriptions.Add(update);
            }
            dataModel.SaveChanges();

            return OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(update, true);
        }

        public SubscriptionMatchGroup GetSubscriptionMatches(OCM.Core.Data.OCMEntities dataModel, OCM.Core.Data.UserSubscription subscription)
        {
            SubscriptionMatchGroup subscriptionMatchGroup = new SubscriptionMatchGroup();

            var checkFromDate = DateTime.UtcNow.AddMinutes(-CHECK_INTERVAL_MINS); //check from last 5 mins etc
            if (subscription.DateLastNotified != null) checkFromDate = subscription.DateLastNotified.Value; //check from date last notified
            //            checkFromDate = checkFromDate.AddMonths(-1);
            System.Data.Entity.Spatial.DbGeography searchPos = null;
            if (subscription.Latitude != null && subscription.Longitude != null) searchPos = System.Data.Entity.Spatial.DbGeography.PointFromText("POINT(" + subscription.Longitude + " " + subscription.Latitude + ")", 4326);

            //check if any new comments match this subscription
            if (subscription.NotifyComments)
            {
                var newComments = dataModel.UserComments.Where(c => c.DateCreated >= checkFromDate &&
                    (searchPos == null ||
                        (searchPos != null &&
                            c.ChargePoint.AddressInfo.SpatialPosition.Distance(searchPos) / 1000 < subscription.DistanceKM
                        ))
                      );
                if (newComments.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewComment, Description = "New Comments Added" };
                    foreach (var c in newComments)
                    {
                        subscriptionMatch.ItemList.Add(OCM.API.Common.Model.Extensions.UserComment.FromDataModel(c, true));
                    }
                    subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any new Media uploads match this subscription
            if (subscription.NotifyMedia)
            {
                var newMedia = dataModel.MediaItems.Where(c => c.DateCreated >= checkFromDate &&
                     (searchPos == null ||
                        (searchPos != null &&
                            c.ChargePoint.AddressInfo.SpatialPosition.Distance(searchPos) / 1000 < subscription.DistanceKM
                        ))
                      );
                if (newMedia.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewMediaUpload, Description = "New Photos Added" };
                    foreach (var c in newMedia)
                    {
                        subscriptionMatch.ItemList.Add(OCM.API.Common.Model.Extensions.MediaItem.FromDataModel(c));
                    }
                    subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any new POIs
            if (subscription.NotifyPOIAdditions)
            {
                var newPOIs = dataModel.ChargePoints.Where(c => c.DateCreated >= checkFromDate &&
                     (searchPos == null ||
                        (searchPos != null &&
                            c.AddressInfo.SpatialPosition.Distance(searchPos) / 1000 < subscription.DistanceKM
                        ))
                      );

                if (newPOIs.Any())
                {
                    var subscriptionMatch = new SubscriptionMatch { Category = SubscriptionMatchCategory.NewPOI, Description = "New POIs Added" };
                    foreach (var p in newPOIs)
                    {
                        subscriptionMatch.ItemList.Add(OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(p));
                    }
                    subscriptionMatchGroup.SubscriptionMatches.Add(subscriptionMatch);
                }
            }

            //check if any POI Edits (pending approval) match this subscription
            if (subscription.NotifyPOIEdits)
            {
            }

            //check if any POI Updates match this subscription
            if (subscription.NotifyPOIUpdates)
            {
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

            //TODO: performance/optimisation
            //for each subscription, check if any changes match the criteria
            var dataModel = new OCM.Core.Data.OCMEntities();
            var currentTime = DateTime.UtcNow;
            var allsubscriptions = dataModel.UserSubscriptions.Where(s => s.IsEnabled == true);
            foreach (var subscription in allsubscriptions)
            {
                if (!excludeRecentlyNotified || (subscription.DateLastNotified == null || subscription.DateLastNotified.Value < currentTime.AddMinutes(-subscription.NotificationFrequencyMins)))
                {
                    var subscriptionMatchGroup = GetSubscriptionMatches(dataModel, subscription);
                    if (subscriptionMatchGroup.SubscriptionMatches.Any())
                    {
                        subscriptionMatchGroup.Subscription = OCM.API.Common.Model.Extensions.UserSubscription.FromDataModel(subscription, true);
                        allMatches.Add(subscriptionMatchGroup);
                    }
                }
            }
            return allMatches;
        }

        public int SendAllPendingSubscriptionNotifications()
        {
            int notificationsSent = 0;
            var allSubscriptionMatches = GetAllSubscriptionMatches(true);
            var userManager = new UserManager();
            NotificationManager notificationManager = new NotificationManager();
            foreach (var subscriptionMatch in allSubscriptionMatches)
            {
                var summaryHTML = "";
                foreach (var match in subscriptionMatch.SubscriptionMatches)
                {
                    if (match.ItemList != null)
                    {
                        if (match.Category == SubscriptionMatchCategory.NewPOI || match.Category == SubscriptionMatchCategory.UpdatedPOI)
                        {
                            //POI list
                            summaryHTML += "<h3>" + match.Description + "</h3>";
                            summaryHTML += "<div class='results-list'>";

                            foreach (var p in match.ItemList)
                            {
                                var poi = (ChargePoint)p;
                                summaryHTML += "<div class='result'><h3>" + poi.AddressInfo.Title + "</h3><p>" + poi.AddressInfo.ToString() + " <a href='http://openchargemap.org/site/poi/details/" + poi.ID + "'>View Details</a></p></div>";
                            }
                            summaryHTML += "</div>";
                        }

                        if (match.Category == SubscriptionMatchCategory.NewComment)
                        {
                            //comment list
                            summaryHTML += "<h3>" + match.Description + "</h3>";
                            summaryHTML += "<div class='results-list'>";

                            foreach (var i in match.ItemList)
                            {
                                var comment = (UserComment)i;
                                summaryHTML += "<div class='result'><h3>" + comment.UserName + " - " + comment.DateCreated.ToShortDateString() + "</h3><p>" + comment.Comment + " <a href='http://openchargemap.org/site/poi/details/" + comment.ChargePointID + "'>View Details</a></p></div>";
                            }
                            summaryHTML += "</div>";
                        }

                        if (match.Category == SubscriptionMatchCategory.NewMediaUpload)
                        {
                            //media list
                            summaryHTML += "<h3>" + match.Description + "</h3>";
                            summaryHTML += "<div class='results-list'>";

                            foreach (var i in match.ItemList)
                            {
                                var item = (MediaItem)i;
                                summaryHTML += "<div class='result'><h3>" + item.User.Username + " - " + item.DateCreated.ToShortDateString() + "</h3><p><img src='" + item.ItemThumbnailURL + "'/> <a href='http://openchargemap.org/site/poi/details/" + item.ChargePointID + "'>View Details</a></p></div>";
                            }
                            summaryHTML += "</div>";
                        }
                    }
                }

                List<int> subscriptionsNotified = new List<int>();
                //prepare and send email
                Hashtable msgParams = new Hashtable();
                msgParams.Add("SummaryContent", summaryHTML);
                msgParams.Add("SubscriptionTitle", subscriptionMatch.Subscription.Title);

                var userDetails = userManager.GetUser(subscriptionMatch.Subscription.UserID);
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

                //mark all subscriptions notified where sent ok
                var dataModel = new OCM.Core.Data.OCMEntities();
                foreach (var subscriptionId in subscriptionsNotified)
                {
                    var s = dataModel.UserSubscriptions.Find(subscriptionId);
                    s.DateLastNotified = DateTime.UtcNow;
                }
                dataModel.SaveChanges();
            }
            return notificationsSent;
        }
    }
}