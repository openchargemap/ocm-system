using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class MediaItemMap : EntityTypeConfiguration<MediaItem>
    {
        public MediaItemMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.ItemURL)
                .IsRequired()
                .HasMaxLength(500);

            this.Property(t => t.ItemThumbnailURL)
                .HasMaxLength(500);

            this.Property(t => t.Comment)
                .HasMaxLength(1000);

            this.Property(t => t.MetadataValue)
                .HasMaxLength(1000);

            // Table & Column Mappings
            this.ToTable("MediaItem");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.ItemURL).HasColumnName("ItemURL");
            this.Property(t => t.ItemThumbnailURL).HasColumnName("ItemThumbnailURL");
            this.Property(t => t.Comment).HasColumnName("Comment");
            this.Property(t => t.ChargePointID).HasColumnName("ChargePointID");
            this.Property(t => t.UserID).HasColumnName("UserID");
            this.Property(t => t.DateCreated).HasColumnName("DateCreated");
            this.Property(t => t.IsEnabled).HasColumnName("IsEnabled");
            this.Property(t => t.IsVideo).HasColumnName("IsVideo");
            this.Property(t => t.IsFeaturedItem).HasColumnName("IsFeaturedItem");
            this.Property(t => t.MetadataValue).HasColumnName("MetadataValue");
            this.Property(t => t.IsExternalResource).HasColumnName("IsExternalResource");

            // Relationships
            this.HasRequired(t => t.ChargePoint)
                .WithMany(t => t.MediaItems)
                .HasForeignKey(d => d.ChargePointID);
            this.HasRequired(t => t.User)
                .WithMany(t => t.MediaItems)
                .HasForeignKey(d => d.UserID);

        }
    }
}
