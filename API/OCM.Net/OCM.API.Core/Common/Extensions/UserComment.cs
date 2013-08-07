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

            if (isVerboseMode && source.UserCommentType != null)
            {
                userComment.CommentType = UserCommentType.FromDataModel(source.UserCommentType);
                userComment.CommentTypeID = source.UserCommentTypeID;
            }
            else
            {
                userComment.CommentTypeID = source.UserCommentTypeID;
            }


            if (isVerboseMode && source.CheckinStatusType!=null)
            {
                userComment.CheckinStatusType = CheckinStatusType.FromDataModel(source.CheckinStatusType);
                userComment.CheckinStatusTypeID = source.CheckinStatusTypeID;
            }
            else
            {
                userComment.CheckinStatusTypeID = source.CheckinStatusTypeID;
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