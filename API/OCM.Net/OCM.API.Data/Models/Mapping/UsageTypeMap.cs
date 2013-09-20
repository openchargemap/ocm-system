using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class UsageTypeMap : EntityTypeConfiguration<UsageType>
    {
        public UsageTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            // Table & Column Mappings
            this.ToTable("UsageType");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.IsPayAtLocation).HasColumnName("IsPayAtLocation");
            this.Property(t => t.IsMembershipRequired).HasColumnName("IsMembershipRequired");
            this.Property(t => t.IsAccessKeyRequired).HasColumnName("IsAccessKeyRequired");
        }
    }
}
