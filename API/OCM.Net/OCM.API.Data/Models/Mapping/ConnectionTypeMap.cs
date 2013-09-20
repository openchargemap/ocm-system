using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class ConnectionTypeMap : EntityTypeConfiguration<ConnectionType>
    {
        public ConnectionTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            this.Property(t => t.FormalName)
                .HasMaxLength(200);

            // Table & Column Mappings
            this.ToTable("ConnectionType");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.FormalName).HasColumnName("FormalName");
            this.Property(t => t.IsDiscontinued).HasColumnName("IsDiscontinued");
            this.Property(t => t.IsObsolete).HasColumnName("IsObsolete");
        }
    }
}
