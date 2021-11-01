using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCM.API.Common.Model;
using OCM.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public class EditQueueManager : ManagerBase
    {
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

        public void CleanupRedundantEditQueueitems()
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
                var p = cpManager.Get((int)item.EntityId);
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
                var editItem = GetItemWithDifferences(item, cpManager, true);
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

        public Model.EditQueueItem GetItemWithDifferences(Core.Data.EditQueueItem item, POIManager cpManager, bool loadCurrentItem)
        {
            var queueItem = Model.Extensions.EditQueueItem.FromDataModel(item);

            //get diff between previous and edit

            Model.ChargePoint poiA = DeserializePOIFromJSON(queueItem.PreviousData);

            if (loadCurrentItem && poiA != null)
            {
                poiA = new POIManager().Get(poiA.ID);
            }
            Model.ChargePoint poiB = DeserializePOIFromJSON(queueItem.EditData);

            queueItem.Differences = cpManager.CheckDifferences(poiA, poiB, useObjectCompare: true);

            return queueItem;
        }

        public List<Model.EditQueueItem> GetEditQueueItems(EditQueueFilter filter)
        {
            var sourceList =
                DataModel.EditQueueItems.Where(
                    i => (
                        (filter.ID == null || (filter.ID != null && i.EntityId == filter.ID))
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
                outputList.Add(GetItemWithDifferences(editQueueItem, cpManager, false));
            }

            return outputList.Where(i => i.Differences.Count >= filter.MinimumDifferences).Take(filter.MaxResults).ToList();
        }

        public void ProcessEditQueueItem(int id, bool publishEdit, int userId, bool enableCacheRefresh = true, string comment = null)
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
                    if (userProfile != null)
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
                                poiA = poiManager.Get(poiA.ID);
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

                            //set status type to published if previously unset
                            if (poiData.SubmissionStatusTypeId == null)
                            {
                                poiData.SubmissionStatusType = DataModel.SubmissionStatusTypes.First(s => s.Id == (int)StandardSubmissionStatusTypes.Submitted_Published);
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