using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class EditQueueItemMap : EntityTypeConfiguration<EditQueueItem>
    {
        public EditQueueItemMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("EditQueueItem");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.UserID).HasColumnName("UserID");
            this.Property(t => t.Comment).HasColumnName("Comment");
            this.Property(t => t.IsProcessed).HasColumnName("IsProcessed");
            this.Property(t => t.ProcessedByUserID).HasColumnName("ProcessedByUserID");
            this.Property(t => t.DateSubmitted).HasColumnName("DateSubmitted");
            this.Property(t => t.DateProcessed).HasColumnName("DateProcessed");
            this.Property(t => t.EditData).HasColumnName("EditData");
            this.Property(t => t.PreviousData).HasColumnName("PreviousData");
            this.Property(t => t.EntityID).HasColumnName("EntityID");
            this.Property(t => t.EntityTypeID).HasColumnName("EntityTypeID");

            // Relationships
            this.HasOptional(t => t.User)
                .WithMany(t => t.EditQueueItems)
                .HasForeignKey(d => d.UserID);
            this.HasOptional(t => t.ProcessedByUser)
                .WithMany(t => t.EditQueueItems1)
                .HasForeignKey(d => d.ProcessedByUserID);
            this.HasOptional(t => t.EntityType)
                .WithMany(t => t.EditQueueItems)
                .HasForeignKey(d => d.EntityTypeID);

        }
    }
}
