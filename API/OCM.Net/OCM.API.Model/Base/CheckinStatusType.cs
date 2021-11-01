namespace OCM.API.Common.Model
{
    public class CheckinStatusType : SimpleReferenceDataType
    {
        public bool? IsPositive { get; set; }
        public bool IsAutomatedCheckin { get; set; }
    }
}