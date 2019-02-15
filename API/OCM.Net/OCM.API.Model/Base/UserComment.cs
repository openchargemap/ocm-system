using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class UserComment
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }

        public int? CommentTypeID { get; set; }

        [DisplayName("Comment Type")]
        public UserCommentType CommentType { get; set; }

        [DisplayName("Name"), StringLength(100)]
        public string UserName { get; set; }

        [DisplayName("Comment"), StringLength(2000), DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        public string Comment { get; set; }

        [DisplayName("Rating")]
        public byte? Rating { get; set; }

        [DisplayName("Web link"), StringLength(500), DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string RelatedURL { get; set; }

        [DisplayName("Date Created")]
        public DateTime DateCreated { get; set; }

        [DisplayName("Comment By")]
        public User User { get; set; }

        public int? CheckinStatusTypeID { get; set; }

        [DisplayName("Check-In Status")]
        public CheckinStatusType CheckinStatusType { get; set; }

        [DisplayName("Actioned By Editor")]
        public bool? IsActionedByEditor { get; set; }
    }
}