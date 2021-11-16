using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    /// <summary>
    /// Used to perform a data submission.
    /// </summary>
    public class SubmissionManager
    {
        public bool AllowUpdates = false;
        public bool RequireSubmissionReview = true;

        public SubmissionManager()
        {
            AllowUpdates = false;



        }

        //convert a simple POI to data and back again to fully populate all related properties, as submission may only have simple IDs for ref data etc
        private Model.ChargePoint PopulateFullPOI(Model.ChargePoint poi, Model.CoreReferenceData refData)
        {
            OCMEntities tempDataModel = new OCMEntities();

            //convert simple poi to fully populated db version
            var poiData = new POIManager().PopulateChargePoint_SimpleToData(poi, tempDataModel);

            //convert back to simple POI
            var modelPOI = Model.Extensions.ChargePoint.FromDataModel(poiData, false, false, true, true, refData);

            //clear temp changes from the poi
            //dataModel.Entry(poiData).Reload();
            tempDataModel.Dispose();
            return modelPOI;
        }

        private string PerformSerialisationToString(object graph, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings != null)
            {
                return JsonConvert.SerializeObject(graph, serializerSettings);
            }
            else
            {
                return JsonConvert.SerializeObject(graph);
            }
        }

        /// <summary>
        /// Consumers should prepare a new/updated ChargePoint with as much info populated as possible
        /// </summary>
        /// <param name="submission">ChargePoint info for submission, if ID and UUID set will be treated as an update</param>
        /// <returns>false on error or not enough data supplied</returns>
        public async Task<ValidationResult> PerformPOISubmission(Model.ChargePoint updatedPOI, Model.User user, bool performCacheRefresh = true, bool disablePOISuperseding = false)
        {
            try
            {
                var poiManager = new POIManager();
                bool enableEditQueueLogging = bool.Parse(ConfigurationManager.AppSettings["EnableEditQueue"]);
                bool isUpdate = false;
                bool userCanEditWithoutApproval = false;
                bool isSystemUser = false;
                int? supersedesID = null;//POI from another data provider which has been superseded by an edit

                //if user signed in, check if they have required permission to perform an edit/approve (if required)
                if (user != null)
                {
                    if (user.ID == (int)StandardUsers.System) isSystemUser = true;

                    //if user is system user, edits/updates are not recorded in edit queue
                    if (isSystemUser)
                    {
                        enableEditQueueLogging = false;
                    }

                    userCanEditWithoutApproval = POIManager.CanUserEditPOI(updatedPOI, user);
                }

                var dataModel = new Core.Data.OCMEntities();

                //if poi is an update, validate if update can be performed
                if (updatedPOI.ID > 0 && !String.IsNullOrEmpty(updatedPOI.UUID))
                {
                    if (dataModel.ChargePoints.Any(c => c.Id == updatedPOI.ID && c.Uuid == updatedPOI.UUID))
                    {
                        //update is valid poi, check if user has permission to perform an update
                        isUpdate = true;
                        if (userCanEditWithoutApproval) AllowUpdates = true;
                        if (!AllowUpdates && !enableEditQueueLogging)
                        {
                            //valid update requested but updates not allowed
                            return new ValidationResult { IsValid = false, Message = "Updates are disabled" };
                        }
                    }
                    else
                    {
                        //update does not correctly identify an existing poi
                        return new ValidationResult { IsValid = false, Message = "Update does not correctly match an existing POI" };
                    }
                }

                //validate if minimal required data is present
                if (updatedPOI.AddressInfo.Title == null || (updatedPOI.AddressInfo.Country == null && updatedPOI.AddressInfo.CountryID == null))
                {
                    return new ValidationResult { IsValid = false, Message = "Update failed basic validation" };
                }

                //convert to DB version of POI and back so that properties are fully populated
                using (var refDataManager = new ReferenceDataManager())
                {
                    var refData = await refDataManager.GetCoreReferenceDataAsync();

                    updatedPOI = PopulateFullPOI(updatedPOI, refData);

                }

                Model.ChargePoint oldPOI = null;

                if (updatedPOI.ID > 0)
                {
                    //get json snapshot of current cp data to store as 'previous'
                    oldPOI = await poiManager.Get(updatedPOI.ID);
                }

                //if user cannot edit directly, add to edit queue for approval
                var editQueueItem = new Core.Data.EditQueueItem { DateSubmitted = DateTime.UtcNow };
                if (enableEditQueueLogging)
                {
                    editQueueItem.EntityId = updatedPOI.ID;
                    editQueueItem.EntityType = dataModel.EntityTypes.FirstOrDefault(t => t.Id == 1);
                    //charging point location entity type id

                    //serialize cp as json

                    //null extra data we don't want to serialize/compare
                    updatedPOI.UserComments = null;
                    updatedPOI.MediaItems = null;

                    string editData = PerformSerialisationToString(updatedPOI, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    editQueueItem.EditData = editData;

                    if (updatedPOI.ID > 0)
                    {
                        //check if poi will change with this edit, if not we discard it completely
                        if (!poiManager.HasDifferences(oldPOI, updatedPOI))
                        {
                            System.Diagnostics.Debug.WriteLine("POI Update has no changes, discarding change.");
                            return new ValidationResult { IsValid = true, ItemId = updatedPOI.ID, Message = "No POI changes detected" };
                        }
                        else
                        {
                            editQueueItem.PreviousData = PerformSerialisationToString(oldPOI, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        }
                    }

                    if (user != null)
                    {
                        editQueueItem.User = dataModel.Users.FirstOrDefault(u => u.Id == user.ID);
                    }
                    var processedByUser = editQueueItem.User ?? dataModel.Users.FirstOrDefault(u => u.Id == (int)StandardUsers.System);

                    editQueueItem.IsProcessed = false;
                    dataModel.EditQueueItems.Add(editQueueItem);
                    //TODO: send notification of new item for approval

                    //save edit queue item
                    dataModel.SaveChanges();

                    //if previous edit queue item exists by same user for same POI, mark as processed
                    var previousEdits = dataModel.EditQueueItems.Where(e => e.UserId == editQueueItem.UserId && e.EntityId == editQueueItem.EntityId && e.EntityTypeId == editQueueItem.EntityTypeId && e.Id != editQueueItem.Id && e.IsProcessed != true);
                    foreach (var previousEdit in previousEdits)
                    {
                        previousEdit.IsProcessed = true;
                        previousEdit.ProcessedByUser = processedByUser;

                        previousEdit.DateProcessed = DateTime.UtcNow;
                    }
                    //save updated edit queue items
                    dataModel.SaveChanges();
                }

                //prepare and save changes POI changes/addition
                if (isUpdate && !AllowUpdates)
                {
                    //user has submitted an edit but is not an approved editor
                    //SendEditSubmissionNotification(updatedPOI, user);

                    //user is not an editor, item is now pending in edit queue for approval.
                    return new ValidationResult { IsValid = true, ItemId = updatedPOI.ID, Message = "Update submitted for review" };
                }

                if (isUpdate && updatedPOI.SubmissionStatusTypeID >= 1000)
                {
                    //update is a delisting, skip superseding poi
                    System.Diagnostics.Debug.WriteLine("skipping superseding of imported POI due to delisting");
                }
                else
                {
                    //if poi being updated exists from an imported source we supersede the old POI with the new version, unless we're doing a fresh import from same data provider
                    if (!disablePOISuperseding)
                    {
                        //if update by non-system user will change an imported/externally provided data, supersede old POI with new one (retain ID against new POI)
                        if (isUpdate && !isSystemUser && oldPOI.DataProviderID != (int)StandardDataProviders.OpenChargeMapContrib)
                        {
                            //move old poi to new id, set status of new item to superseded
                            supersedesID = poiManager.SupersedePOI(dataModel, oldPOI, updatedPOI);
                        }
                    }
                }

                //user is an editor, go ahead and store the addition/update
                //set/update cp properties
                var cpData = poiManager.PopulateChargePoint_SimpleToData(updatedPOI, dataModel);

                //if item has no submission status and user permitted to edit, set to published
                if (userCanEditWithoutApproval && cpData.SubmissionStatusTypeId == null)
                {

                    cpData.SubmissionStatusTypeId = (int)StandardSubmissionStatusTypes.Submitted_Published; //hack due to conflicting state change for SubmissionStatusType
                }
                else
                {
                    //no submission status, set to 'under review'
                    if (cpData.SubmissionStatusTypeId == null) cpData.SubmissionStatusTypeId = (int)StandardSubmissionStatusTypes.Submitted_UnderReview;
                }

                cpData.DateLastStatusUpdate = DateTime.UtcNow;

                if (!isUpdate)
                {
                    //new data objects need added to data model before save
                    if (cpData.AddressInfo != null) dataModel.AddressInfoes.Add(cpData.AddressInfo);

                    dataModel.ChargePoints.Add(cpData);
                }

                //finally - save poi update
                dataModel.SaveChanges();

                //get id of update/new poi
                int newPoiID = cpData.Id;

                //this is an authorised update, reflect change in edit queue item
                if (enableEditQueueLogging && user != null && user.ID > 0)
                {
                    var editUser = dataModel.Users.FirstOrDefault(u => u.Id == user.ID);
                    editQueueItem.User = editUser;

                    if (newPoiID > 0) editQueueItem.EntityId = newPoiID;

                    //if user is authorised to edit, process item automatically without review
                    if (userCanEditWithoutApproval)
                    {
                        editQueueItem.ProcessedByUser = editUser;
                        editQueueItem.DateProcessed = DateTime.UtcNow;
                        editQueueItem.IsProcessed = true;
                    }

                    //save edit queue item changes
                    dataModel.SaveChanges();
                }
                else
                {
                    //anonymous submission, update edit queue item
                    if (enableEditQueueLogging && user == null)
                    {
                        if (newPoiID > 0) editQueueItem.EntityId = newPoiID;
                        dataModel.SaveChanges();
                    }
                }

                System.Diagnostics.Debug.WriteLine("Added/Updated CP:" + cpData.Id);

                //if user is not anonymous, log their submission and update their reputation points
                if (user != null)
                {
                    AuditLogManager.Log(user, isUpdate ? AuditEventType.UpdatedItem : AuditEventType.CreatedItem, "Modified OCM-" + cpData.Id, null);
                    //add reputation points
                    new UserManager().AddReputationPoints(user, 1);
                }

                //preserve new POI Id for caller
                updatedPOI.ID = cpData.Id;

                if (performCacheRefresh)
                {
                    if (supersedesID != null)
                    {
                        await CacheManager.RefreshCachedPOI((int)supersedesID);
                    }
                    await CacheManager.RefreshCachedPOI(updatedPOI.ID);
                }

                return new ValidationResult { IsValid = true, ItemId = updatedPOI.ID, Message = "Update submitted." };
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.ToString());
                AuditLogManager.ReportWebException(true, null, AuditEventType.SystemErrorWeb, "POI Submission Failed", exp);

                //error performing submission
                return new ValidationResult { IsValid = false, Message = "Submission Failed with an Exception: " + exp.Message };
            }
        }

        private static void SendNewPOISubmissionNotification(Model.ChargePoint poi, Model.User user, Core.Data.ChargePoint cpData)
        {
            try
            {
                string approvalStatus = cpData.SubmissionStatusType.Title;

                //send notification
                NotificationManager notification = new NotificationManager();
                Hashtable msgParams = new Hashtable();
                msgParams.Add("Description", "OCM-" + cpData.Id + " : " + poi.AddressInfo.Title);
                msgParams.Add("SubmissionStatusType", approvalStatus);
                msgParams.Add("ItemURL", "https://openchargemap.org/site/poi/details/" + cpData.Id);
                msgParams.Add("ChargePointID", cpData.Id);
                msgParams.Add("UserName", user != null ? user.Username : "Anonymous");
                msgParams.Add("MessageBody",
                              "New Location " + approvalStatus + " OCM-" + cpData.Id + " Submitted: " +
                              poi.AddressInfo.Title);

                notification.PrepareNotification(NotificationType.LocationSubmitted, msgParams);

                //notify default system recipients
                notification.SendNotification(NotificationType.LocationSubmitted);
            }
            catch (Exception)
            {
                ;
                ; //failed to send notification
            }
        }

        private static void SendEditSubmissionNotification(Model.ChargePoint poi, Model.User user)
        {
            try
            {
                string approvalStatus = "Edit Submitted for approval";

                //send notification
                var notification = new NotificationManager();
                var msgParams = new Hashtable();
                msgParams.Add("Description", "OCM-" + poi.ID + " : " + poi.AddressInfo.Title);
                msgParams.Add("SubmissionStatusType", approvalStatus);
                msgParams.Add("ItemURL", "https://openchargemap.org/site/poi/details/" + poi.ID);
                msgParams.Add("ChargePointID", poi.ID);
                msgParams.Add("UserName", user != null ? user.Username : "Anonymous");
                msgParams.Add("MessageBody",
                              "Edit item for Approval: Location " + approvalStatus + " OCM-" + poi.ID + " Submitted: " +
                              poi.AddressInfo.Title);

                notification.PrepareNotification(NotificationType.LocationSubmitted, msgParams);

                //notify default system recipients
                notification.SendNotification(NotificationType.LocationSubmitted);
            }
            catch (Exception)
            {
                ;
                ; //failed to send notification
            }
        }

        /// <summary>
        /// Submit a new comment against a given charge equipment id
        /// </summary>
        /// <param name="comment"></param>
        /// <returns>ID of new comment, -1 for invalid cp, -2 for general error saving comment</returns>
        public async Task<int> PerformSubmission(Common.Model.UserComment comment, Model.User user)
        {
            //TODO: move all to UserCommentManager
            //populate data model comment from simple comment object

            var dataModel = new Core.Data.OCMEntities();
            int cpID = comment.ChargePointID;
            var dataComment = new Core.Data.UserComment();
            var dataChargePoint = dataModel.ChargePoints.FirstOrDefault(c => c.Id == cpID);

            if (dataChargePoint == null) return -1; //invalid charge point specified

            dataComment.ChargePointId = dataChargePoint.Id;

            dataComment.Comment = comment.Comment;
            int commentTypeID = comment.CommentTypeID ?? 10; //default to General Comment

            // some clients may post a CommentType object instead of just an ID
            if (comment.CommentType != null)
            {
                commentTypeID = comment.CommentType.ID;
            }

            dataComment.UserCommentTypeId = commentTypeID;

            int? checkinStatusType = comment.CheckinStatusTypeID;
            dataComment.CheckinStatusTypeId = (byte?)comment.CheckinStatusTypeID;

            // some clients may post a CheckinStatusType object instead of just an ID
            if (dataComment.CheckinStatusTypeId == null && comment.CheckinStatusType != null)
            {
                dataComment.CheckinStatusTypeId = (byte?)comment.CheckinStatusType.ID;
            }

            dataComment.UserName = comment.UserName;
            dataComment.Rating = comment.Rating;
            dataComment.RelatedUrl = comment.RelatedURL;
            dataComment.DateCreated = DateTime.UtcNow;

            if (user != null && user.ID > 0)
            {
                var ocmUser = dataModel.Users.FirstOrDefault(u => u.Id == user.ID);

                if (ocmUser != null)
                {
                    dataComment.UserId = ocmUser.Id;
                    dataComment.UserName = ocmUser.Username;
                }
            }

            try
            {
                dataChargePoint.DateLastStatusUpdate = DateTime.UtcNow;
                dataModel.UserComments.Add(dataComment);

                dataModel.SaveChanges();

                if (user != null)
                {
                    AuditLogManager.Log(user, AuditEventType.CreatedItem, "Added Comment " + dataComment.Id + " to OCM-" + cpID, null);
                    //add reputation points
                    new UserManager().AddReputationPoints(user, 1);
                }

                //SendPOICommentSubmissionNotifications(comment, user, dataComment);

                //TODO: only refresh cache for specific POI
                await CacheManager.RefreshCachedPOI(dataComment.ChargePoint.Id);

                return dataComment.Id;
            }
            catch (Exception exp)
            {
                return -2; //error saving
            }
        }

        private static void SendPOICommentSubmissionNotifications(Common.Model.UserComment comment, Model.User user, Core.Data.UserComment dataComment)
        {
            try
            {
                //prepare notification
                NotificationManager notification = new NotificationManager();

                Hashtable msgParams = new Hashtable();
                msgParams.Add("Description", "");
                msgParams.Add("ChargePointID", comment.ChargePointID);
                msgParams.Add("ItemURL", "https://openchargemap.org/site/poi/details/" + comment.ChargePointID);
                msgParams.Add("UserName", user != null ? user.Username : comment.UserName);
                msgParams.Add("MessageBody", "Comment (" + dataComment.UserCommentType.Title + ") added to OCM-" + comment.ChargePointID + ": " + dataComment.Comment);

                //if fault report, attempt to notify operator
                if (dataComment.UserCommentType.Id == (int)StandardCommentTypes.FaultReport)
                {
                    //decide if we can send a fault notification to the operator
                    notification.PrepareNotification(NotificationType.FaultReport, msgParams);

                    //notify default system recipients
                    bool operatorNotified = false;
                    if (dataComment.ChargePoint.Operator != null)
                    {
                        if (!String.IsNullOrEmpty(dataComment.ChargePoint.Operator.FaultReportEmail))
                        {
                            try
                            {
                                notification.SendNotification(dataComment.ChargePoint.Operator.FaultReportEmail, ConfigurationManager.AppSettings["DefaultRecipientEmailAddresses"].ToString());
                                operatorNotified = true;
                            }
                            catch (Exception)
                            {
                                System.Diagnostics.Debug.WriteLine("Fault report: failed to notify operator");
                            }
                        }
                    }

                    if (!operatorNotified)
                    {
                        notification.Subject += " (OCM: Could not notify Operator)";
                        notification.SendNotification(NotificationType.LocationCommentReceived);
                    }
                }
                else
                {
                    //normal comment, notification to OCM only
                    notification.PrepareNotification(NotificationType.LocationCommentReceived, msgParams);

                    //notify default system recipients
                    notification.SendNotification(NotificationType.LocationCommentReceived);
                }
            }
            catch (Exception)
            {
                ; ; // failed to send notification
            }
        }

        public int PerformSubmission(Common.Model.MediaItem mediaItem, Model.User user)
        {
            return -1;
        }

        public bool SubmitContactSubmission(ContactSubmission contactSubmission)
        {
            return SendContactUsMessage(contactSubmission.Name, contactSubmission.Email, contactSubmission.Comment);
        }

        public bool SendContactUsMessage(string senderName, string senderEmail, string comment)
        {
            try
            {
                //send notification
                NotificationManager notification = new NotificationManager();
                Hashtable msgParams = new Hashtable();
                msgParams.Add("Description", (comment.Length > 64 ? comment.Substring(0, 64) + ".." : comment));
                msgParams.Add("Name", senderName);
                msgParams.Add("Email", senderEmail);
                msgParams.Add("Comment", comment);

                notification.PrepareNotification(NotificationType.ContactUsMessage, msgParams);

                //notify default system recipients
                return notification.SendNotification(NotificationType.ContactUsMessage);
            }
            catch (Exception exp)
            {
                System.Diagnostics.Debug.WriteLine(exp.ToString());
                return false; //failed
            }
        }
    }
}