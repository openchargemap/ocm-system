using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class SystemConfigMap : EntityTypeConfiguration<SystemConfig>
    {
        public SystemConfigMap()
        {
            // Primary Key
            this.HasKey(t => t.ConfigKeyName);

            // Properties
            this.Property(t => t.ConfigKeyName)
                .IsRequired()
                .HasMaxLength(100);

            this.Property(t => t.ConfigValue)
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("SystemConfig");
            this.Property(t => t.ConfigKeyName).HasColumnName("ConfigKeyName");
            this.Property(t => t.ConfigValue).HasColumnName("ConfigValue");
            this.Property(t => t.DataTypeID).HasColumnName("DataTypeID");

            // Relationships
            this.HasOptional(t => t.DataType)
                .WithMany(t => t.SystemConfigs)
                .HasForeignKey(d => d.DataTypeID);

        }
    }
}
