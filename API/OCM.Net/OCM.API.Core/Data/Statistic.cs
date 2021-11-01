using System;

namespace OCM.Core.Data
{
    public partial class Statistic
    {
        public Guid Id { get; set; }
        public string StatTypeCode { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? CountryId { get; set; }
        public int? UserId { get; set; }
        public int NumItems { get; set; }

        public virtual Country Country { get; set; }
        public virtual User User { get; set; }
    }
}
