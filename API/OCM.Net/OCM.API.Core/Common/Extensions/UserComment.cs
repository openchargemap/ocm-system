using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserComment
    {
        public static Model.UserComment FromDataModel(Core.Data.UserComment source)
        {
            var userComment =  new Model.UserComment { 
                ID = source.ID,
                Comment = source.Comment, 
                Rating = source.Rating, 
                
                RelatedURL = source.RelatedURL, 
                DateCreated = source.DateCreated ,
                CommentType = UserCommentType.FromDataModel(source.UserCommentType),
                ChargePointID = source.ChargePointID,
                User = User.BasicFromDataModel(source.User),
                CheckinStatusType = CheckinStatusType.FromDataModel(source.CheckinStatusType)
            };

            if (userComment.User != null) userComment.UserName = userComment.User.Username;
            return userComment;
        }
    }
}