using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common.Model;
using System.Collections;
using System.Configuration;
using Newtonsoft.Json;

namespace OCM.API.Common
{


    /// <summary>
    /// Used to perform a charge point submission.
    /// </summary>
    public class SubmissionManager
    {
        public bool AllowUpdates = false;
        public bool RequireSubmissionReview = true;

        public SubmissionManager()
        {
            AllowUpdates = false;
        }

        /// <summary>
        /// Consumers should prepare a new/updated ChargePoint with as much info populated as possible
        /// </summary>
        /// <param name="submission">ChargePoint info for submission, if ID and UUID set will be treated as an update</param>
        /// <returns>false on error or not enough data supplied</returns>
        public bool PerformSubmission(Model.ChargePoint poi, Model.User user)
        {
            try
            {
                var cpManager = new POIManager();
                bool enableEditQueue = bool.Parse(ConfigurationManager.AppSettings["EnableEditQueue"]);
                bool addToQueueAndDB = true; //if true item is entered in edit queue and to the ChargePoint list in db
                bool isUpdate = false;
                bool userCanEdit = false;
                bool userCanApprove = false;

                //if user signed in, check if they have required permission to perform an edit/approve (if required)
                if (user != null)
                {
                    int? countryId = (poi.AddressInfo != null && poi.AddressInfo.Country != null) ? (int?)poi.AddressInfo.Country.ID : null;

                    if (UserManager.IsUserAdministrator(user)
                        || UserManager.HasUserPermission(user, StandardPermissionAttributes.CountryLevel_Editor, "All")
                        || (countryId != null && UserManager.HasUserPermission(user, StandardPermissionAttributes.CountryLevel_Editor, countryId.ToString()))
                        )
                    {
                        userCanEdit = true;
                        userCanApprove = true;
                    }
                    else
                    {

                        if (countryId != null && UserManager.HasUserPermission(user, StandardPermissionAttributes.CountryLevel_Approver, countryId.ToString()))
                        {
                            userCanApprove = true;
                        }
                    }

                    //if user is system user, edits/updates are not recorded in edit queue

                    if (user.ID == (int) StandardUsers.System)
                    {
                        enableEditQueue = false;
                    }
                    
                }

                var dataModel = new Core.Data.OCMEntities();

                if (poi.ID > 0 && !String.IsNullOrEmpty(poi.UUID))
                {
                    //poi is an update, validate
                    if (dataModel.ChargePoints.Any(c => c.ID == poi.ID && c.UUID == poi.UUID))
                    {
                        //update is valid poi, check if user has permission to perform an update
                        isUpdate = true;
                        if (userCanEdit) AllowUpdates = true;
                        if (!AllowUpdates && !enableEditQueue)
                        {
                            return false; //valid update requested but updates not allowed
                        }
                    }
                    else
                    {
                        //update does not correctly identify an existing poi
                        return false;
                    }
                }

                //if user cannot edit, add to edit queue for approval
                var editQueueItem = new Core.Data.EditQueueItem { DateSubmitted = DateTime.UtcNow };
                if (enableEditQueue)
                {
                    editQueueItem.EntityID = poi.ID;
                    editQueueItem.EntityType = dataModel.EntityTypes.FirstOrDefault(t => t.ID == 1);
                    //charging point location entity type id

                    //serialize cp as json
                    var jsonOutput = new OutputProviders.JSONOutputProvider();

                    //null extra data we don't want to serialize
                    poi.UserComments = null;
                    poi.MetadataTags = null;

                    string editData = jsonOutput.PerformSerialisationToString(poi, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    editQueueItem.EditData = editData;

                    if (poi.ID > 0)
                    {
                        //get json snapshot of current cp data to store as 'previous'
                        var currentPOI = cpManager.Get(poi.ID);

                        //check if poi will change with this edit, if not we discard it completely
                        if (!cpManager.HasDifferences(currentPOI, poi))
                        {
                            return false;
                        }
                        else
                        {
                            editQueueItem.PreviousData = jsonOutput.PerformSerialisationToString(currentPOI,
                                                                                                 new JsonSerializerSettings
                                                                                                     {
                                                                                                         NullValueHandling =
                                                                                                             NullValueHandling
                                                                                                     .Ignore
                                                                                                     });
                        }
                    }

                    if (user != null) editQueueItem.User = dataModel.Users.FirstOrDefault(u => u.ID == user.ID);
                    editQueueItem.IsProcessed = false;
                    editQueueItem = dataModel.EditQueueItems.Add(editQueueItem);
                    //TODO: send notification of new item for approval

                    dataModel.SaveChanges();
                }

                //prepare and save changes POI changes/addition

                int newID = 0;


                if (isUpdate && !AllowUpdates)
                {
                    //user has submitted an edit but is not an approved editor

                    try
                    {
                        string approvalStatus = "Edit Submitted for approval";

                        //send notification
                        var notification = new NotificationManager();
                        var msgParams = new Hashtable();
                        msgParams.Add("Description", "OCM-" + poi.ID + " : " + poi.AddressInfo.Title);
                        msgParams.Add("SubmissionStatusType", approvalStatus);

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
                 
                    //item is in edit queue for approval.
                    return true;
                }

                if (addToQueueAndDB)
                {


                    //updates allowed for this user, go ahead and store the update
                    var cpData = new Core.Data.ChargePoint();
                    if (isUpdate) cpData = dataModel.ChargePoints.First(c => c.ID == poi.ID);

                    //set/update cp properties

                    PopulateChargePoint_SimpleToData(poi, cpData, dataModel);

                    //if item has no submission status and user permitted to approve or edit, set to published
                    if (userCanApprove || userCanEdit)
                    {
                        if (cpData.SubmissionStatusTypeID == null)
                        {
                            cpData.SubmissionStatusType =
                                dataModel.SubmissionStatusTypes.First(
                                    s => s.ID == (int)StandardSubmissionStatusTypes.Submitted_Published);
                        }
                    }

                    //TODO: prevent update if no changes have been made to target item (other than date modified etc)

                    if (!isUpdate)
                    {
                        //new data objects need added to data model before save
                        if (cpData.AddressInfo != null) dataModel.AddressInfoList.Add(cpData.AddressInfo);

                        dataModel.ChargePoints.Add(cpData);
                    }


                    //save poi
                    dataModel.SaveChanges();
                    newID = cpData.ID;

                    //is an authorised update, reflect change in edit queue item
                    if (enableEditQueue && user != null && user.ID>0)
                    {
                        var editUser = dataModel.Users.FirstOrDefault(u => u.ID == user.ID);
                        editQueueItem.User = editUser;
                      
                        if (newID > 0) editQueueItem.EntityID = newID;

                        if (userCanApprove || userCanEdit)
                        {
                            editQueueItem.ProcessedByUser = editUser;
                            editQueueItem.DateProcessed = DateTime.UtcNow;
                            editQueueItem.IsProcessed = true;
                        }

                        dataModel.SaveChanges();
                    }

               

                    System.Diagnostics.Debug.WriteLine("Added/Updated CP:" + cpData.ID);

                    if (user != null)
                    {
                        AuditLogManager.Log(user, isUpdate ? AuditEventType.UpdatedItem : AuditEventType.CreatedItem,
                                            "Modified OCM-" + cpData.ID, null);
                        //add reputation points
                        new UserManager().AddReputationPoints(user, 1);
                    }

                    if (!isUpdate)
                    {
                        try
                        {
                            string approvalStatus = cpData.SubmissionStatusType.Title;

                            //send notification
                            NotificationManager notification = new NotificationManager();
                            Hashtable msgParams = new Hashtable();
                            msgParams.Add("Description", "OCM-" + cpData.ID + " : " + poi.AddressInfo.Title);
                            msgParams.Add("SubmissionStatusType", approvalStatus);

                            msgParams.Add("UserName", user != null ? user.Username : "Anonymous");
                            msgParams.Add("MessageBody",
                                          "New Location " + approvalStatus + " OCM-" + cpData.ID + " Submitted: " +
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

                }

               
                return true;
            }
            catch (Exception exp)
            {

                System.Diagnostics.Debug.WriteLine(exp.ToString());
                //throw exp;
                //error performing submission
                return false;
            }

        }

        /// <summary>
        /// Populate AddressInfo data from settings in a simple AddressInfo object
        /// </summary>
        private Core.Data.AddressInfo PopulateAddressInfo_SimpleToData(Model.AddressInfo simpleAddressInfo, Core.Data.AddressInfo dataAddressInfo)
        {
            if (simpleAddressInfo != null && dataAddressInfo == null) dataAddressInfo = new Core.Data.AddressInfo();

            if (simpleAddressInfo != null && dataAddressInfo != null)
            {
                dataAddressInfo.Title = simpleAddressInfo.Title;
                dataAddressInfo.AddressLine1 = simpleAddressInfo.AddressLine1;
                dataAddressInfo.AddressLine2 = simpleAddressInfo.AddressLine2;
                dataAddressInfo.Town = simpleAddressInfo.Town;
                dataAddressInfo.StateOrProvince = simpleAddressInfo.StateOrProvince;
                dataAddressInfo.Postcode = simpleAddressInfo.Postcode;
                if (simpleAddressInfo.Country != null) dataAddressInfo.CountryID = simpleAddressInfo.Country.ID;
                dataAddressInfo.Latitude = simpleAddressInfo.Latitude;
                dataAddressInfo.Longitude = simpleAddressInfo.Longitude;
                dataAddressInfo.ContactTelephone1 = simpleAddressInfo.ContactTelephone1;
                dataAddressInfo.ContactTelephone2 = simpleAddressInfo.ContactTelephone2;
                dataAddressInfo.ContactEmail = simpleAddressInfo.ContactEmail;
                dataAddressInfo.AccessComments = simpleAddressInfo.AccessComments;
                dataAddressInfo.GeneralComments = simpleAddressInfo.GeneralComments;
                dataAddressInfo.RelatedURL = simpleAddressInfo.RelatedURL;
            }

            return dataAddressInfo;
        }

        private void PopulateChargePoint_SimpleToData(Model.ChargePoint simpleChargePoint, Core.Data.ChargePoint dataChargePoint, OCM.Core.Data.OCMEntities dataModel)
        {
            if (String.IsNullOrEmpty(dataChargePoint.UUID)) dataChargePoint.UUID = Guid.NewGuid().ToString().ToUpper();

            if (simpleChargePoint.DataProvider != null)
            {
                dataChargePoint.DataProvider = dataModel.DataProviders.First(d => d.ID == simpleChargePoint.DataProvider.ID);
            }
            else
            {
                //set to ocm contributor by default
                dataChargePoint.DataProvider = dataModel.DataProviders.First(d => d.ID == (int)StandardDataProviders.OpenChargeMapContrib);
            }

            dataChargePoint.DataProvidersReference = simpleChargePoint.DataProvidersReference;

            if (simpleChargePoint.OperatorInfo != null)
            {
                dataChargePoint.Operator = dataModel.Operators.First(o => o.ID == simpleChargePoint.OperatorInfo.ID);
            }
            dataChargePoint.OperatorsReference = simpleChargePoint.OperatorsReference;

            if (simpleChargePoint.UsageType != null) dataChargePoint.UsageType = dataModel.UsageTypes.First(u => u.ID == simpleChargePoint.UsageType.ID);

            dataChargePoint.AddressInfo = PopulateAddressInfo_SimpleToData(simpleChargePoint.AddressInfo, dataChargePoint.AddressInfo);

            dataChargePoint.NumberOfPoints = simpleChargePoint.NumberOfPoints;
            dataChargePoint.GeneralComments = simpleChargePoint.GeneralComments;
            dataChargePoint.DatePlanned = simpleChargePoint.DatePlanned;
            dataChargePoint.UsageCost = simpleChargePoint.UsageCost;

            if (simpleChargePoint.DateLastStatusUpdate != null)
            {
                dataChargePoint.DateLastStatusUpdate = DateTime.UtcNow;
            }
            else
            {
                dataChargePoint.DateLastStatusUpdate = DateTime.UtcNow;
            }


            if (simpleChargePoint.DataQualityLevel != null)
            {
                dataChargePoint.DataQualityLevel = simpleChargePoint.DataQualityLevel;
            }
            else
            {
                dataChargePoint.DataQualityLevel = 1;
            }

            if (dataChargePoint.DateCreated == null)
            {
                dataChargePoint.DateCreated = DateTime.UtcNow;
            }

            var updateConnectionList = new List<OCM.Core.Data.ConnectionInfo>();
            var deleteList = new List<OCM.Core.Data.ConnectionInfo>();

            if (simpleChargePoint.Connections != null)
            {

                foreach (var c in simpleChargePoint.Connections)
                {
                    var connectionInfo = new Core.Data.ConnectionInfo();

                    //edit existing, if required
                    if (c.ID > 0) connectionInfo = dataChargePoint.Connections.FirstOrDefault(con => con.ID == c.ID && con.ChargePointID == dataChargePoint.ID);
                    if (connectionInfo == null)
                    {
                        //connection is stale info, start new
                        c.ID = 0;
                        connectionInfo = new Core.Data.ConnectionInfo();
                    }

                    connectionInfo.Reference = c.Reference;
                    connectionInfo.Amps = c.Amps;
                    connectionInfo.Voltage = c.Voltage;
                    connectionInfo.Quantity = c.Quantity;
                    connectionInfo.PowerKW = c.PowerKW;

                    if (c.ConnectionType != null)
                    {
                        connectionInfo.ConnectionType = dataModel.ConnectionTypes.First(ct => ct.ID == c.ConnectionType.ID);
                    }
                    else
                    {
                        connectionInfo.ConnectionType = null;
                    }

                    if (c.Level != null)
                    {
                        connectionInfo.ChargerType = dataModel.ChargerTypes.First(chg => chg.ID == c.Level.ID);
                    }
                    else
                    {
                        connectionInfo.ChargerType = null;
                    }

                    if (c.CurrentType != null)
                    {
                        connectionInfo.CurrentType = dataModel.CurrentTypes.First(chg => chg.ID == c.CurrentType.ID);
                    }
                    else
                    {
                        connectionInfo.ChargerType = null;
                    }

                    if (c.StatusType != null)
                    {
                        connectionInfo.StatusType = dataModel.StatusTypes.First(s => s.ID == c.StatusType.ID);
                    }
                    else
                    {
                        connectionInfo.StatusType = null;
                    }

                    bool addConnection = false;

                    //detect if connection details are non-blank/unknown before adding
                    if (
                        !String.IsNullOrEmpty(connectionInfo.Reference)
                        || connectionInfo.Amps != null
                        || connectionInfo.Voltage != null
                        || connectionInfo.PowerKW != null
                        || (connectionInfo.ConnectionType != null && connectionInfo.ConnectionType.ID > 0)
                        || (connectionInfo.StatusType != null && connectionInfo.StatusTypeID > 0)
                        || (connectionInfo.ChargerType != null && connectionInfo.ChargerType.ID > 1)
                        || (connectionInfo.CurrentType != null && connectionInfo.CurrentType.ID > 0)
                        || (connectionInfo.Quantity != null && connectionInfo.Quantity > 1)
                    )
                    {
                        addConnection = true;
                        if (connectionInfo.ChargePoint == null) connectionInfo.ChargePoint = dataChargePoint;
                    }

                    if (addConnection)
                    {
                        //if adding a new connection (not an update) add to model
                        if (c.ID <= 0)
                        {
                            dataChargePoint.Connections.Add(connectionInfo);
                        }
                        //track final list of connections being added/updated  -- will then be used to delete by difference
                        updateConnectionList.Add(connectionInfo);
                    }
                    else
                    {
                        //remove an existing connection no longer required
                        //if (c.ID > 0)
                        {
                            if (connectionInfo.ChargePoint == null) connectionInfo.ChargePoint = dataChargePoint;
                            deleteList.Add(connectionInfo);
                            //dataChargePoint.Connections.Remove(connectionInfo);
                        }
                    }
                }


            }


            //find existing connections not in updated/added list, add to delete
            if (dataChargePoint.Connections != null)
            {
                foreach (var con in dataChargePoint.Connections)
                {
                    if (!updateConnectionList.Contains(con))
                    {
                        if (!deleteList.Contains(con))
                        {
                            deleteList.Add(con);
                        }
                    }
                }
            }

            //finally clean up deleted items
            foreach (var item in deleteList)
            {
                dataModel.ConnectionInfoList.Remove(item);
            }


            if (simpleChargePoint.StatusType != null)
            {
                dataChargePoint.StatusType = dataModel.StatusTypes.First(s => s.ID == simpleChargePoint.StatusType.ID);
            }

            if (simpleChargePoint.SubmissionStatus != null)
            {
                dataChargePoint.SubmissionStatusType = dataModel.SubmissionStatusTypes.First(s => s.ID == simpleChargePoint.SubmissionStatus.ID);
            }
            else
            {
                dataChargePoint.SubmissionStatusType = dataModel.SubmissionStatusTypes.First(s => s.ID == (int)StandardSubmissionStatusTypes.Submitted_UnderReview);
            }

        }

        /// <summary>
        /// Submit a new comment against a given charge equipment id
        /// </summary>
        /// <param name="comment"></param>
        /// <returns>ID of new comment, -1 for invalid cp, -2 for general error saving comment</returns>
        public int PerformSubmission(Common.Model.UserComment comment, Model.User user)
        {
            //populate data model comment from simple comment object

            var dataModel = new Core.Data.OCMEntities();
            int cpID = comment.ChargePointID;
            var dataComment = new Core.Data.UserComment();
            dataComment.ChargePoint = dataModel.ChargePoints.FirstOrDefault(c => c.ID == cpID);

            if (dataComment.ChargePoint == null) return -1; //invalid charge point specified

            dataComment.Comment = comment.Comment;
            int commentTypeID = 10; //default to General Comment
            if (comment.CommentType != null) commentTypeID = comment.CommentType.ID;
            dataComment.UserCommentType = dataModel.UserCommentTypes.FirstOrDefault(t => t.ID == commentTypeID);
            if (comment.CheckinStatusType!=null) dataComment.CheckinStatusType = dataModel.CheckinStatusTypes.FirstOrDefault(t => t.ID == comment.CheckinStatusType.ID);
            dataComment.UserName = comment.UserName;
            dataComment.Rating = comment.Rating;
            dataComment.RelatedURL = comment.RelatedURL;
            dataComment.DateCreated = DateTime.UtcNow;

            try
            {
                dataModel.UserComments.Add(dataComment);
                dataModel.SaveChanges();

                if (user != null)
                {
                    AuditLogManager.Log(user, AuditEventType.CreatedItem, "Added Comment " + dataComment.ID + " to OCM-" + cpID, null);
                    //add reputation points
                    new UserManager().AddReputationPoints(user, 1);
                }

                try
                {
                    //prepare notification
                    NotificationManager notification = new NotificationManager();
                    Hashtable msgParams = new Hashtable();
                    msgParams.Add("Description", "");
                    msgParams.Add("ChargePointID", comment.ChargePointID);
                    msgParams.Add("ItemURL", "http://openchargemap.org/site/poi/details/" + comment.ChargePointID);
                    msgParams.Add("UserName", user != null ? user.Username : comment.UserName);
                    msgParams.Add("MessageBody", "Comment (" + dataComment.UserCommentType.Title + ") added to OCM-" + comment.ChargePointID + ": " + dataComment.Comment);

                    //if fault report, attempt to notify operator
                    if (dataComment.UserCommentType.ID == (int)StandardCommentTypes.FaultReport)
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

                return dataComment.ID;
            }
            catch (Exception)
            {
                return -2; //error saving
            }

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
                notification.SendNotification(NotificationType.ContactUsMessage);
                return true;
            }
            catch (Exception)
            {
                return false; //failed
            }
        }

        public void TestSubmission()
        {
            Model.ChargePoint cp = new Model.ChargePoint();
            cp.ID = 0;

            cp.AddressInfo = new Model.AddressInfo();
            PerformSubmission(cp, null);
        }
    }
}