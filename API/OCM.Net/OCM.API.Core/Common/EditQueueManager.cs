
using Newtonsoft.Json;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common
{
    public class EditQueueManager : ManagerBase
    {

        private Model.ChargePoint DeserializePOIFromJSON(string json)
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
            var sourceList = DataModel.EditQueueItems;

            var redundantEdits = new List<Model.EditQueueItem>();

            var cpManager = new POIManager();

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

            queueItem.Differences = cpManager.CheckDifferences(poiA, poiB);

            return queueItem;
        }

        public List<Model.EditQueueItem> GetEditQueueItems(EditQueueFilter filter)
        {

            var sourceList =
                DataModel.EditQueueItems.Where(
                    i => (
                        (filter.ShowProcessed || (filter.ShowProcessed == false && i.IsProcessed == false))
                        && (filter.DateFrom == null || (filter.DateFrom != null && i.DateSubmitted >= filter.DateFrom))
                        && (filter.DateTo == null || (filter.DateTo != null && i.DateSubmitted <= filter.DateTo))
                        && (filter.ShowEditsOnly == false || (filter.ShowEditsOnly == true && i.PreviousData != null))
                        )).OrderByDescending(e => e.DateSubmitted);

            var cpManager = new POIManager();
            var outputList = new List<Model.EditQueueItem>();

            //perform object level differencing on json contents of edit queue items (very expensive), used to get summary and count of differences per item
            foreach (var editQueueItem in sourceList)
            {
                outputList.Add(GetItemWithDifferences(editQueueItem, cpManager, true));
            }

            return outputList.Where(i => i.Differences.Count >= filter.MinimumDifferences).Take(filter.MaxResults).ToList();

        }

        public void ProcessEditQueueItem(int id, bool publishEdit, int userId)
        {
            //check current user is authorized to approve edits

            //prepare poi details

            var queueItem = DataModel.EditQueueItems.FirstOrDefault(e => e.ID == id);

            if (queueItem != null && queueItem.IsProcessed == false)
            {

                if (queueItem.EntityType.ID == (int)StandardEntityTypes.POI)
                {
                    //processing a POI add/edit

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
                        var poiData = new Core.Data.ChargePoint();

                        //if its an edit, load the original details before applying the change
                        if (poiUpdateRequired)
                        {
                            //updates to externally provided POIs require old version to be superseded (archived) first
                            if (poiA!=null && poiA.DataProviderID!=(int)StandardDataProviders.OpenChargeMapContrib)
                            {
                                poiManager.SupersedePOI(DataModel, poiA, poiB);
                            }
                            poiData = DataModel.ChargePoints.First(c => c.ID == poiB.ID);
                        }

                        //set/update cp properties
                        poiManager.PopulateChargePoint_SimpleToData(poiB, poiData, DataModel);

                        //set status type to published if previously unset
                        if (poiData.SubmissionStatusTypeID == null)
                        {
                            poiData.SubmissionStatusType = DataModel.SubmissionStatusTypes.First(s => s.ID == (int)StandardSubmissionStatusTypes.Submitted_Published);
                        }

                        if (poiData.ID == 0)
                        {
                            //new item, set contributor on poi details
                            poiData.Contributor = DataModel.Users.First(u => u.ID == queueItem.User.ID);
                        }

                        //publish edit
                        DataModel.SaveChanges();

                    }

                    //update edit queue item as processed
                    queueItem.IsProcessed = true;
                    queueItem.ProcessedByUser = DataModel.Users.FirstOrDefault(u => u.ID == userId);
                    queueItem.DateProcessed = DateTime.UtcNow;
                    DataModel.SaveChanges();
                }
            }
        }
    }
}
