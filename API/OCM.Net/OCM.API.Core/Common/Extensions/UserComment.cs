using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserComment
    {
        public static Model.UserComment FromDataModel(Core.Data.UserComment source, bool isVerboseMode, Model.CoreReferenceData refData = null)
        {
            var userComment = new Model.UserComment
            {
                ID = source.Id,
                Comment = source.Comment,
                Rating = source.Rating,
                RelatedURL = source.RelatedUrl,
                DateCreated = source.DateCreated,
                ChargePointID = source.ChargePointId,
                User = User.BasicFromDataModel(source.User),
                IsActionedByEditor = source.IsActionedByEditor
            };

            if (isVerboseMode && refData != null)
            {
                userComment.CommentType = refData?.UserCommentTypes.FirstOrDefault(i => i.ID == source.UserCommentTypeId) ?? UserCommentType.FromDataModel(source.UserCommentType);
                userComment.CommentTypeID = source.UserCommentTypeId;
            }
            else
            {
                userComment.CommentTypeID = source.UserCommentTypeId;
            }

            if (isVerboseMode && (refData != null || source.CheckinStatusType != null) && source.CheckinStatusTypeId!=null)
            {
                userComment.CheckinStatusType = refData?.CheckinStatusTypes.FirstOrDefault(i => i.ID == source.CheckinStatusTypeId) ?? CheckinStatusType.FromDataModel(source.CheckinStatusType);
                userComment.CheckinStatusTypeID = source.CheckinStatusTypeId;
            }
            else
            {
                userComment.CheckinStatusTypeID = source.CheckinStatusTypeId;
            }

            if (userComment.User != null)
            {
                userComment.UserName = userComment.User.Username;
            }
            else
            {
                userComment.UserName = source.UserName;
            }

            return userComment;
        }
    }
}