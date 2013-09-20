using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class DataProviderUserMap : EntityTypeConfiguration<DataProviderUser>
    {
        public DataProviderUserMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("DataProviderUser");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.DataProviderID).HasColumnName("DataProviderID");
            this.Property(t => t.UserID).HasColumnName("UserID");
            this.Property(t => t.IsDataProviderAdmin).HasColumnName("IsDataProviderAdmin");

            // Relationships
            this.HasRequired(t => t.DataProvider)
                .WithMany(t => t.DataProviderUsers)
                .HasForeignKey(d => d.DataProviderID);
            this.HasRequired(t => t.User)
                .WithMany(t => t.DataProviderUsers)
                .HasForeignKey(d => d.UserID);

        }
    }
}
