using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class MetadataFieldOptionMap : EntityTypeConfiguration<MetadataFieldOption>
    {
        public MetadataFieldOptionMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("MetadataFieldOption");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.MetadataFieldID).HasColumnName("MetadataFieldID");
            this.Property(t => t.Title).HasColumnName("Title");

            // Relationships
            this.HasRequired(t => t.MetadataField)
                .WithMany(t => t.MetadataFieldOptions)
                .HasForeignKey(d => d.MetadataFieldID);

        }
    }
}
