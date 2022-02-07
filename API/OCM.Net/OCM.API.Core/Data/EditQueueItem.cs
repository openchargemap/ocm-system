using System;
using System.Collections.Generic;

namespace OCM.Core.Data
{
    public partial class EditQueueItem
    {
        public int Id { get; set; }
        /// <summary>
        /// User who submitted this edit
        /// </summary>
        public int? UserId { get; set; }
        public string Comment { get; set; }
        public bool IsProcessed { get; set; }
        /// <summary>
        /// Editor who approved/processed this edit
        /// </summary>
        public int? ProcessedByUserId { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime? DateProcessed { get; set; }
        public string EditData { get; set; }
        public string PreviousData { get; set; }
        public int? EntityId { get; set; }
        public short? EntityTypeId { get; set; }

        public virtual EntityType EntityType { get; set; }
        public virtual User ProcessedByUser { get; set; }
        public virtual User User { get; set; }
    }
}
