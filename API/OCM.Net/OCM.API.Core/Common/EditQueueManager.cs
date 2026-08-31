using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCM.API.Common
{
    public class EditQueueManager : ManagerBase
    {
        /// <summary>
        /// Marker written to the comment of an edit queue item when an administrator has reverted that edit
        /// </summary>
        public const string RevertedCommentPrefix = "[Reverted]";

        /// <summary>
        /// True if this edit has already been reverted by an administrator
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsItemReverted(Model.EditQueueItem item)
        {
            return item?.Comment != null && item.Comment.StartsWith(RevertedCommentPrefix);
        }

        /// <summary>
        /// True if this edit can be reverted, only approved POI edits which have a previous version to restore can be reverted
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool CanItemBeReverted(Model.EditQueueItem item)
        {
            if (item == null) return false;

            //an edit which is still pending should be rejected rather than reverted
            if (!item.IsProcessed) return false;

            //a new location submission has no previous version to restore
            if (String.IsNullOrEmpty(item.PreviousData)) return false;

            if (item.EntityType != null && item.EntityType.ID != (int)StandardEntityTypes.POI) return false;

            //the revert itself is recorded as a new edit which can be reverted in turn, so an item is only reverted once
            if (IsItemReverted(item)) return false;

            return true;
        }

        public static Model.ChargePoint DeserializePOIFromJSON(string json)
        {
            Model.ChargePoint poi = null;
            try
            {
                if (json != null)
                {
                    poi = JsonConvert.DeserializeObject<Model.ChargePoint>(json);
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Failed to parse json POI data");
            }

            return poi;
        }

        public async Task CleanupRedundantEditQueueitems()
        {
            var cpManager = new POIManager();

            // cleanup new items not marked as processed but not awaiting review
            var sourceList = DataModel
                .EditQueueItems
                .Include(e => e.User)
                .Include(e => e.ProcessedByUser)
                .Include(e => e.EntityType)
                .Where(e => e.IsProcessed == false && e.PreviousData == null);

            System.Diagnostics.Debug.WriteLine($"Cleanup of added items: {sourceList.Count()}");

            var processedEdits = new List<Model.EditQueueItem>();

            foreach (var item in sourceList)
            {
                var p = await cpManager.Get((int)item.EntityId);
                if (p != null)
                {
                    if (p.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Submitted_UnderReview && p.SubmissionStatusTypeID != (int)StandardSubmissionStatusTypes.Submitted_UnderReview)
                    {
                        item.IsProcessed = true;
                        item.ProcessedByUserId = (int)StandardUsers.System;
                        item.DateProcessed = p.DateCreated;
                    }
                }
            }
            DataModel.SaveChanges();

            // cleanup edits with no differences

            sourceList = DataModel
                .EditQueueItems
                .Include(e => e.User)
                .Include(e => e.ProcessedByUser)
                .Include(e => e.EntityType)
                .AsNoTracking().Where(e => e.IsProcessed == false && e.PreviousData != null);

            var redundantEdits = new List<Model.EditQueueItem>();

            foreach (var item in sourceList)
            {
                var editItem = await GetItemWithDifferences(item, cpManager, true);
                if (editItem.Differences.Count == 0)
                {
                    redundantEdits.Add(editItem);
                }
            }

            //delete redundant edits
            foreach (var item in redundantEdits)
            {
                var delItem = DataModel.EditQueueItems.Find(item.ID);
                DataModel.EditQueueItems.Remove(delItem);
            }
            DataModel.SaveChanges();

        }

        public async Task<Model.EditQueueItem> GetItemWithDifferences(Core.Data.EditQueueItem item, POIManager cpManager, bool loadCurrentItem)
        {
            var queueItem = Model.Extensions.EditQueueItem.FromDataModel(item);

            //get diff between previous and edit

            Model.ChargePoint poiA = DeserializePOIFromJSON(queueItem.PreviousData);

            if (loadCurrentItem && poiA != null)
            {
                poiA = await new POIManager().Get(poiA.ID);
            }
            Model.ChargePoint poiB = DeserializePOIFromJSON(queueItem.EditData);

            queueItem.Differences = cpManager.CheckDifferences(poiA, poiB, useObjectCompare: true);

            return queueItem;
        }

        public async Task<List<Model.EditQueueItem>> GetEditQueueItems(EditQueueFilter filter)
        {
            var sourceList =
                DataModel.EditQueueItems.Where(
                    i => (
                        (filter.ID == null || (filter.ID != null && i.EntityId == filter.ID))
                        && (filter.UserId == null || (filter.UserId != null && i.UserId == filter.UserId))
                        && (filter.ShowProcessed || (filter.ShowProcessed == false && i.IsProcessed == false))
                        && (filter.DateFrom == null || (filter.DateFrom != null && i.DateSubmitted >= filter.DateFrom))
                        && (filter.DateTo == null || (filter.DateTo != null && i.DateSubmitted <= filter.DateTo))
                        && (filter.ShowEditsOnly == false || (filter.ShowEditsOnly == true && i.PreviousData != null))
                        )).OrderByDescending(e => e.DateSubmitted).ToList();

            var cpManager = new POIManager();
            var outputList = new List<Model.EditQueueItem>();

            //perform object level differencing on json contents of edit queue items (very expensive), used to get summary and count of differences per item
            foreach (var editQueueItem in sourceList)
            {
                outputList.Add(await GetItemWithDifferences(editQueueItem, cpManager, false));
            }

            return outputList.Where(i => i.Differences.Count >= filter.MinimumDifferences).Take(filter.MaxResults).ToList();
        }

        /// <summary>
        /// Revert one or more previously approved edits, restoring the content each edit replaced. Administrator level action.
        /// </summary>
        /// <param name="ids">edit queue items to revert</param>
        /// <param name="userId">administrator performing the revert</param>
        /// <param name="enableCacheRefresh"></param>
        /// <returns>outcome for each requested item</returns>
        public async Task<List<Model.EditQueueRevertResult>> RevertEditQueueItems(IEnumerable<int> ids, int userId, bool enableCacheRefresh = true)
        {
            var results = new List<Model.EditQueueRevertResult>();

            if (ids == null) return results;

            var distinctIds = ids.Distinct().ToList();

            var administrator = new UserManager().GetUser(userId);

            //reverting an approved edit is an administrator level action, country editors approve and reject but cannot revert
            if (administrator == null || UserManager.IsUserEditingBlocked(administrator) || !UserManager.IsUserAdministrator(administrator))
            {
                foreach (var id in distinctIds)
                {
                    results.Add(new Model.EditQueueRevertResult { EditQueueItemID = id, IsSuccess = false, Message = "Only an administrator can revert an approved edit." });
                }

                return results;
            }

            foreach (var id in distinctIds)
            {
                results.Add(await RevertEditQueueItem(id, administrator, enableCacheRefresh));
            }

            return results;
        }

        /// <summary>
        /// Revert a single approved edit by resubmitting the content which the edit replaced. The revert is itself recorded as a new (already approved) edit so it can be undone.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="administrator"></param>
        /// <param name="enableCacheRefresh"></param>
        /// <returns></returns>
        private async Task<Model.EditQueueRevertResult> RevertEditQueueItem(int id, Model.User administrator, bool enableCacheRefresh)
        {
            var result = new Model.EditQueueRevertResult { EditQueueItemID = id, IsSuccess = false };

            var queueItem = DataModel.EditQueueItems.Include(e => e.EntityType).FirstOrDefault(e => e.Id == id);

            if (queueItem == null)
            {
                result.Message = "Edit not found.";
                return result;
            }

            var item = Model.Extensions.EditQueueItem.FromDataModel(queueItem);
            result.POIID = item.EntityID;

            if (!CanItemBeReverted(item))
            {
                if (!item.IsProcessed) result.Message = "Only approved edits can be reverted, reject this edit instead.";
                else if (String.IsNullOrEmpty(item.PreviousData)) result.Message = "This is a new location submission, there is no previous version to restore.";
                else if (IsItemReverted(item)) result.Message = "This edit has already been reverted.";
                else result.Message = "This type of edit cannot be reverted.";

                return result;
            }

            //the 'before' content of the edit is the version we are restoring
            var revertPOI = DeserializePOIFromJSON(queueItem.PreviousData);

            if (revertPOI == null)
            {
                result.Message = "Could not read the previous version of this location.";
                return result;
            }

            var poiManager = new POIManager();
            var poiId = queueItem.EntityId ?? revertPOI.ID;
            var currentPOI = await poiManager.Get(poiId);

            if (currentPOI == null)
            {
                result.Message = $"Location OCM-{poiId} no longer exists.";
                return result;
            }

            result.POIID = currentPOI.ID;

            //restore the previous content but keep the current identity and publication status, a revert should not unpublish a location or send it back for review
            revertPOI.ID = currentPOI.ID;
            revertPOI.UUID = currentPOI.UUID;
            revertPOI.SubmissionStatus = null;
            revertPOI.SubmissionStatusTypeID = currentPOI.SubmissionStatusTypeID;
            revertPOI.UserComments = null;
            revertPOI.MediaItems = null;

            if (!poiManager.HasDifferences(currentPOI, revertPOI))
            {
                result.Message = $"OCM-{currentPOI.ID} already matches the version before this edit, nothing to revert.";
                return result;
            }

            //warn if the location has moved on since this edit was approved, the revert will also undo any later changes to the same values
            var editPOI = DeserializePOIFromJSON(queueItem.EditData);
            if (editPOI != null)
            {
                //normalise the values we deliberately do not roll back so they are not reported as later changes
                editPOI.ID = currentPOI.ID;
                editPOI.UUID = currentPOI.UUID;
                editPOI.SubmissionStatus = null;
                editPOI.SubmissionStatusTypeID = currentPOI.SubmissionStatusTypeID;
            }
            bool hasLaterChanges = editPOI != null && poiManager.HasDifferences(currentPOI, editPOI);

            var submissionResult = await new SubmissionManager().PerformPOISubmission(revertPOI, administrator, performCacheRefresh: enableCacheRefresh);

            if (!submissionResult.IsValid)
            {
                result.Message = submissionResult.Message;
                return result;
            }

            //record the revert against the original edit so it is not reverted a second time
            queueItem.Comment = $"{RevertedCommentPrefix} by {administrator.Username} on {DateTime.UtcNow:u}"
                + (String.IsNullOrWhiteSpace(item.Comment) ? "" : " (previous comment: " + item.Comment + ")");
            DataModel.SaveChanges();

            AuditLogManager.Log(administrator, AuditEventType.EditReverted, $"Reverted edit queue item {id} for OCM-{currentPOI.ID}", null);

            result.IsSuccess = true;
            result.Message = $"Reverted edit for OCM-{currentPOI.ID}."
                + (hasLaterChanges ? $" Note: OCM-{currentPOI.ID} did not match the content of this edit beforehand, so later changes (or a rejected edit) may also have been undone. Revert the new edit to put it back." : "");

            return result;
        }

        public async Task ProcessEditQueueItem(int id, bool publishEdit, int userId, bool enableCacheRefresh = true, string comment = null)
        {
            //prepare poi details
            int updatePOIId = 0;
            var queueItem = DataModel.EditQueueItems.FirstOrDefault(e => e.Id == id);

            if (queueItem != null && queueItem.IsProcessed == false)
            {
                if (queueItem.EntityType.Id == (int)StandardEntityTypes.POI)
                {
                    //check current user is authorized to approve edits for this POIs country
                    bool hasEditPermission = false;
                    var editPOI = DeserializePOIFromJSON(queueItem.EditData);
                    var userProfile = new UserManager().GetUser(userId);
                    if (userProfile != null && !UserManager.IsUserEditingBlocked(userProfile))
                    {
                        if (UserManager.HasUserPermission(userProfile, editPOI.AddressInfo.CountryID, PermissionLevel.Editor))
                        {
                            hasEditPermission = true;
                        }
                    }

                    //processing a POI add/edit
                    if (hasEditPermission)
                    {
                        if (publishEdit)
                        {
                            //get diff between previous and edit

                            POIManager poiManager = new POIManager();
                            Model.ChargePoint poiA = DeserializePOIFromJSON(queueItem.PreviousData);
                            Model.ChargePoint poiB = DeserializePOIFromJSON(queueItem.EditData);

                            bool poiUpdateRequired = false;

                            if (poiA != null)
                            {
                                //this is an edit, load the latest version of the POI as version 'A'
                                poiA = await poiManager.Get(poiA.ID);
                                if (poiManager.HasDifferences(poiA, poiB))
                                {
                                    poiUpdateRequired = true;
                                }
                            }

                            //save poi update
                            //if its an edit, load the original details before applying the change
                            if (poiUpdateRequired)
                            {
                                //updates to externally provided POIs require old version to be superseded (archived) first
                                if (poiA != null && poiA.DataProviderID != (int)StandardDataProviders.OpenChargeMapContrib)
                                {
                                    poiManager.SupersedePOI(DataModel, poiA, poiB);
                                }
                            }

                            //set/update cp properties from simple model to data model
                            var poiData = poiManager.PopulateChargePoint_SimpleToData(poiB, DataModel);

                            if (poiData.Id == 0 && queueItem.EntityId > 0 && poiA == null)
                            {
                                // processing an edit that is a new item, load the existing item from the database directly
                                poiData = DataModel.ChargePoints.FirstOrDefault(p => p.Id == (int)queueItem.EntityId);
                            }

                            //set status type to published if previously unset or pending review
                            if (poiData.SubmissionStatusTypeId == null || (poiData.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Submitted_UnderReview || poiData.SubmissionStatusTypeId == (int)StandardSubmissionStatusTypes.Imported_UnderReview))
                            {
                                poiData.SubmissionStatusType = DataModel.SubmissionStatusTypes.First(s => s.Id == (int)StandardSubmissionStatusTypes.Submitted_Published);
                                poiData.SubmissionStatusTypeId = poiData.SubmissionStatusType.Id;
                            }

                            poiData.DateLastStatusUpdate = DateTime.UtcNow;

                            //publish edit
                            DataModel.SaveChanges();

                            updatePOIId = poiData.Id;

                            //attribute submitter with reputation points
                            if (queueItem.UserId != null)
                            {
                                new UserManager().AddReputationPoints((int)queueItem.UserId, 1);
                            }
                        }

                        //update edit queue item as processed
                        queueItem.IsProcessed = true;
                        queueItem.ProcessedByUser = DataModel.Users.FirstOrDefault(u => u.Id == userId);
                        queueItem.Comment = comment;
                        queueItem.DateProcessed = DateTime.UtcNow;
                        DataModel.SaveChanges();

                        //TODO: also award processing editor with reputation points if they are approving someone elses edit and they are not Admin

                        //Refresh POI cache
                        if (enableCacheRefresh)
                        {
                            _ = CacheManager.RefreshCachedPOI(updatePOIId);
                        }
                    }
                }
            }
        }
    }
}