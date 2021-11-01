using System;

namespace OCM.Core.Data
{
    public partial class UserComment
    {
        public int Id { get; set; }
        public int ChargePointId { get; set; }
        public int UserCommentTypeId { get; set; }
        public string UserName { get; set; }
        public string Comment { get; set; }
        public byte? Rating { get; set; }
        public string RelatedUrl { get; set; }
        public DateTime DateCreated { get; set; }
        public byte? CheckinStatusTypeId { get; set; }
        public int? UserId { get; set; }
        public bool? IsActionedByEditor { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string AttachedData { get; set; }

        public virtual ChargePoint ChargePoint { get; set; }
        public virtual CheckinStatusType CheckinStatusType { get; set; }
        public virtual User User { get; set; }
        public virtual UserCommentType UserCommentType { get; set; }
    }
}
