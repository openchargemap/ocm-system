using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class ChargerTypeMap : EntityTypeConfiguration<ChargerType>
    {
        public ChargerTypeMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.ID)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            // Table & Column Mappings
            this.ToTable("ChargerType");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.Comments).HasColumnName("Comments");
            this.Property(t => t.IsFastChargeCapable).HasColumnName("IsFastChargeCapable");
            this.Property(t => t.DisplayOrder).HasColumnName("DisplayOrder");
        }
    }
}
