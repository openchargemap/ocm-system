namespace OCM.API.Common.Model
{
    public class StatusType : SimpleReferenceDataType
    {
        public bool? IsOperational { get; set; }
        public bool IsUserSelectable { get; set; }
    }
}