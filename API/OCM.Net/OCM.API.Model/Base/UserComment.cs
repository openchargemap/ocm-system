using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class UserComment
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }

        [DisplayName("Comment/Checkin Type")]
        public UserCommentType CommentType { get; set; }

        [DisplayName("Name"),StringLength(100)]
        public string UserName { get; set; }

        [DisplayName("Comment"), StringLength(100)]
        public string Comment { get; set; }

        [DisplayName("Rating"), Range(0,5)]
        public byte? Rating { get; set; }

        [DisplayName("Related Website"), StringLength(500), DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string RelatedURL { get; set; }

        [DisplayName("Date Created")]
        public DateTime DateCreated { get; set; }

        [DisplayName("Comment By")]
        public User User { get; set; }

        [DisplayName("Check-In Status")]
        public CheckinStatusType CheckinStatusType { get; set; }
    }
}