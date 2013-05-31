using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserComment
    {
        public static Model.UserComment FromDataModel(Core.Data.UserComment source)
        {
            return new Model.UserComment { 
                ID = source.ID,
                Comment = source.Comment, 
                Rating = source.Rating, 
                UserName = source.UserName, 
                RelatedURL = source.RelatedURL, 
                DateCreated = source.DateCreated ,
                CommentType = UserCommentType.FromDataModel(source.UserCommentType),
                ChargePointID = source.ChargePointID,
                User = User.BasicFromDataModel(source.User),
                CheckinStatusType = CheckinStatusType.FromDataModel(source.CheckinStatusType)
            };
        }
    }
}