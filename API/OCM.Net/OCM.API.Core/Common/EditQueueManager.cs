
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

            var cpManager = new ChargePointManager();

            foreach (var item in sourceList)
            {
                var editItem = GetItemWithDifferences(item, cpManager);
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

        public Model.EditQueueItem GetItemWithDifferences(Core.Data.EditQueueItem item, ChargePointManager cpManager)
        {
            var queueItem = Model.Extensions.EditQueueItem.FromDataModel(item);

            //get diff between previous and edit

            Model.ChargePoint poiA = DeserializePOIFromJSON(queueItem.PreviousData);
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
                        )).OrderByDescending(e => e.DateSubmitted);

            var cpManager = new ChargePointManager();
            var outputList = new List<Model.EditQueueItem>();

            //perform object level differencing on json contents of edit queue items (very expensive), used to get summary and count of differences per item
            foreach (var editQueueItem in sourceList)
            {
                outputList.Add(GetItemWithDifferences(editQueueItem, cpManager));
            }

            return outputList.Where(i => i.Differences.Count >= filter.MinimumDifferences).Take(filter.MaxResults).ToList();

        }
    }
}
