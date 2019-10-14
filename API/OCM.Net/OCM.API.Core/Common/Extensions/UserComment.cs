namespace OCM.API.Common.Model.Extensions
{
    public class UserComment
    {
        public static Model.UserComment FromDataModel(Core.Data.UserComment source, bool isVerboseMode)
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

            if (isVerboseMode && source.UserCommentType != null)
            {
                userComment.CommentType = UserCommentType.FromDataModel(source.UserCommentType);
                userComment.CommentTypeID = source.UserCommentTypeId;
            }
            else
            {
                userComment.CommentTypeID = source.UserCommentTypeId;
            }

            if (isVerboseMode && source.CheckinStatusType != null)
            {
                userComment.CheckinStatusType = CheckinStatusType.FromDataModel(source.CheckinStatusType);
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