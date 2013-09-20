using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UserCommentType
    {
        public UserCommentType()
        {
            this.UserComments = new List<UserComment>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
