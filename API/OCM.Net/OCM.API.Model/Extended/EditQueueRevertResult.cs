namespace OCM.API.Common.Model
{
    /// <summary>
    /// Outcome of an attempt to revert a previously approved edit queue item
    /// </summary>
    public class EditQueueRevertResult
    {
        public int EditQueueItemID { get; set; }

        public int? POIID { get; set; }

        public bool IsSuccess { get; set; }

        public string Message { get; set; }
    }
}
