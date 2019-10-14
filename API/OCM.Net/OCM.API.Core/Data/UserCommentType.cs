using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class UserCommentType
    {
        public UserCommentType()
        {
            UserComments = new HashSet<UserComment>();
        }

        public int Id { get; set; }
        public string Title { get; set; }

        public virtual ICollection<UserComment> UserComments { get; set; }
    }
}
