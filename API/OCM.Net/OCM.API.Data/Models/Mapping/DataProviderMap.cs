using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class DataProviderMap : EntityTypeConfiguration<DataProvider>
    {
        public DataProviderMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(250);

            this.Property(t => t.WebsiteURL)
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("DataProvider");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.WebsiteURL).HasColumnName("WebsiteURL");
            this.Property(t => t.Comments).HasColumnName("Comments");
            this.Property(t => t.DataProviderStatusTypeID).HasColumnName("DataProviderStatusTypeID");
            this.Property(t => t.IsRestrictedEdit).HasColumnName("IsRestrictedEdit");

            // Relationships
            this.HasOptional(t => t.DataProviderStatusType)
                .WithMany(t => t.DataProviders)
                .HasForeignKey(d => d.DataProviderStatusTypeID);

        }
    }
}
