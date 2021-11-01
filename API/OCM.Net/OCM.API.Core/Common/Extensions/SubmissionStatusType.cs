namespace OCM.API.Common.Model.Extensions
{
    public class SubmissionStatusType
    {
        public static Model.SubmissionStatusType FromDataModel(Core.Data.SubmissionStatusType s)
        {
            if (s == null) return null;

            return new Model.SubmissionStatusType
            {
                ID = s.Id,
                Title = s.Title,
                IsLive = s.IsLive
            };
        }
    }
}