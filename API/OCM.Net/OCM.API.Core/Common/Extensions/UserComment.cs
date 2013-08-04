using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserComment
    {
        public static Model.UserComment FromDataModel(Core.Data.UserComment source, bool isVerboseMode)
        {
            var userComment = new Model.UserComment
            {
                ID = source.ID,
                Comment = source.Comment,
                Rating = source.Rating,
                RelatedURL = source.RelatedURL,
                DateCreated = source.DateCreated,
                ChargePointID = source.ChargePointID,
                User = User.BasicFromDataModel(source.User)
            };

            if (source.UserCommentTypeID != null)
            {
                userComment.CommentTypeID = source.UserCommentTypeID;
                if (isVerboseMode)
                {
                    userComment.CommentType = UserCommentType.FromDataModel(source.UserCommentType);
                }
            }

            if (source.CheckinStatusTypeID != null)
            {
                userComment.CheckinStatusTypeID = source.CheckinStatusTypeID;
                if (isVerboseMode)
                {
                    userComment.CheckinStatusType = CheckinStatusType.FromDataModel(source.CheckinStatusType);
                }
            }

            if (userComment.User != null) userComment.UserName = userComment.User.Username;
            return userComment;
        }
    }
}