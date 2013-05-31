using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class AuditLog
    {
        public static Model.AuditLog FromDataModel(Core.Data.AuditLog source)
        {
            if (source == null) return null;

            return new Model.AuditLog()
            {
                ID = source.ID,
                EventDate = source.EventDate,
                UserID = source.UserID,
                EventDescription = source.EventDescription,
                Comment = source.Comment
            };
        }
    }
}