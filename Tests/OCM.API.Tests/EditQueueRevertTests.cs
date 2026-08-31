using OCM.API.Common;
using OCM.API.Common.Model;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Unit tests for the rules governing which approved edits an administrator can revert
    /// </summary>
    public class EditQueueRevertTests
    {
        private static EditQueueItem GetApprovedEdit()
        {
            return new EditQueueItem
            {
                ID = 1,
                EntityID = 100,
                EntityType = new EntityType { ID = (int)StandardEntityTypes.POI, Title = "POI" },
                IsProcessed = true,
                PreviousData = "{\"ID\":100}",
                EditData = "{\"ID\":100}"
            };
        }

        [Fact]
        public void ApprovedPOIEditCanBeReverted()
        {
            Assert.True(EditQueueManager.CanItemBeReverted(GetApprovedEdit()));
        }

        [Fact]
        public void PendingEditCannotBeReverted()
        {
            var item = GetApprovedEdit();
            item.IsProcessed = false;

            //a pending edit should be rejected rather than reverted
            Assert.False(EditQueueManager.CanItemBeReverted(item));
        }

        [Fact]
        public void NewLocationSubmissionCannotBeReverted()
        {
            var item = GetApprovedEdit();
            item.PreviousData = null;

            //an addition has no previous version to restore
            Assert.False(EditQueueManager.CanItemBeReverted(item));
        }

        [Fact]
        public void AlreadyRevertedEditCannotBeRevertedAgain()
        {
            var item = GetApprovedEdit();
            item.Comment = EditQueueManager.RevertedCommentPrefix + " by admin on 2026-08-31 00:00:00Z";

            Assert.True(EditQueueManager.IsItemReverted(item));
            Assert.False(EditQueueManager.CanItemBeReverted(item));
        }

        [Fact]
        public void EditWithUnrelatedCommentIsNotTreatedAsReverted()
        {
            var item = GetApprovedEdit();
            item.Comment = "Approved, looks correct";

            Assert.False(EditQueueManager.IsItemReverted(item));
            Assert.True(EditQueueManager.CanItemBeReverted(item));
        }

        [Fact]
        public void NullItemCannotBeReverted()
        {
            Assert.False(EditQueueManager.CanItemBeReverted(null));
            Assert.False(EditQueueManager.IsItemReverted(null));
        }
    }
}
