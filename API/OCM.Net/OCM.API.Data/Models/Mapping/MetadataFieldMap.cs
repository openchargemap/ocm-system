using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class MetadataFieldMap : EntityTypeConfiguration<MetadataField>
    {
        public MetadataFieldMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("MetadataField");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.MetadataGroupID).HasColumnName("MetadataGroupID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.DataTypeID).HasColumnName("DataTypeID");

            // Relationships
            this.HasRequired(t => t.DataType)
                .WithMany(t => t.MetadataFields)
                .HasForeignKey(d => d.DataTypeID);
            this.HasRequired(t => t.MetadataGroup)
                .WithMany(t => t.MetadataFields)
                .HasForeignKey(d => d.MetadataGroupID);

        }
    }
}
