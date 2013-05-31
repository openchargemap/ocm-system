using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class UserCommentType
    {
        public static Model.UserCommentType FromDataModel(Core.Data.UserCommentType source)
        {
            if (source != null)
            {
                return new Model.UserCommentType
                {
                    ID = source.ID,
                    Title = source.Title
                };
            }
            else
            {
                return null;
            }
        }
    }
}