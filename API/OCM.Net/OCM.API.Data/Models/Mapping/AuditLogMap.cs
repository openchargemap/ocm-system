using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class AuditLogMap : EntityTypeConfiguration<AuditLog>
    {
        public AuditLogMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("AuditLog");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.EventDate).HasColumnName("EventDate");
            this.Property(t => t.UserID).HasColumnName("UserID");
            this.Property(t => t.EventDescription).HasColumnName("EventDescription");
            this.Property(t => t.Comment).HasColumnName("Comment");

            // Relationships
            this.HasRequired(t => t.User)
                .WithMany(t => t.AuditLogs)
                .HasForeignKey(d => d.UserID);

        }
    }
}
