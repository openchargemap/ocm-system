using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class MetadataValueMap : EntityTypeConfiguration<MetadataValue>
    {
        public MetadataValueMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("MetadataValue");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.ChargePointID).HasColumnName("ChargePointID");
            this.Property(t => t.MetadataFieldID).HasColumnName("MetadataFieldID");
            this.Property(t => t.ItemValue).HasColumnName("ItemValue");
            this.Property(t => t.MetadataFieldOptionID).HasColumnName("MetadataFieldOptionID");

            // Relationships
            this.HasRequired(t => t.ChargePoint)
                .WithMany(t => t.MetadataValues)
                .HasForeignKey(d => d.ChargePointID);
            this.HasRequired(t => t.MetadataField)
                .WithMany(t => t.MetadataValues)
                .HasForeignKey(d => d.MetadataFieldID);
            this.HasOptional(t => t.MetadataFieldOption)
                .WithMany(t => t.MetadataValues)
                .HasForeignKey(d => d.MetadataFieldOptionID);

        }
    }
}
