using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class UserComment
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public UserCommentType CommentType { get; set; }
        public string UserName { get; set; }
        public string Comment { get; set; }
        public byte? Rating { get; set; }
        public string RelatedURL { get; set; }
        public DateTime DateCreated { get; set; }
        public User User { get; set; }
        public CheckinStatusType CheckinStatusType { get; set; }
    }
}