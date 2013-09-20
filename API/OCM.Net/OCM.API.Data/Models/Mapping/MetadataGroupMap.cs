using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class MetadataGroupMap : EntityTypeConfiguration<MetadataGroup>
    {
        public MetadataGroupMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("MetadataGroup");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.IsRestrictedEdit).HasColumnName("IsRestrictedEdit");
            this.Property(t => t.DataProviderID).HasColumnName("DataProviderID");
            this.Property(t => t.IsPublicInterest).HasColumnName("IsPublicInterest");

            // Relationships
            this.HasRequired(t => t.DataProvider)
                .WithMany(t => t.MetadataGroups)
                .HasForeignKey(d => d.DataProviderID);

        }
    }
}
