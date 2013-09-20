using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class CheckinStatusTypeMap : EntityTypeConfiguration<CheckinStatusType>
    {
        public CheckinStatusTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("CheckinStatusType");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.IsPositive).HasColumnName("IsPositive");
            this.Property(t => t.IsAutomatedCheckin).HasColumnName("IsAutomatedCheckin");
        }
    }
}
