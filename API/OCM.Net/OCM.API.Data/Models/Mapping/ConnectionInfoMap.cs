using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class ConnectionInfoMap : EntityTypeConfiguration<ConnectionInfo>
    {
        public ConnectionInfoMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Reference)
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("ConnectionInfo");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.ChargePointID).HasColumnName("ChargePointID");
            this.Property(t => t.ConnectionTypeID).HasColumnName("ConnectionTypeID");
            this.Property(t => t.Reference).HasColumnName("Reference");
            this.Property(t => t.StatusTypeID).HasColumnName("StatusTypeID");
            this.Property(t => t.Amps).HasColumnName("Amps");
            this.Property(t => t.Voltage).HasColumnName("Voltage");
            this.Property(t => t.PowerKW).HasColumnName("PowerKW");
            this.Property(t => t.LevelTypeID).HasColumnName("LevelTypeID");
            this.Property(t => t.Quantity).HasColumnName("Quantity");
            this.Property(t => t.Comments).HasColumnName("Comments");
            this.Property(t => t.CurrentTypeID).HasColumnName("CurrentTypeID");

            // Relationships
            this.HasRequired(t => t.ChargePoint)
                .WithMany(t => t.Connections)
                .HasForeignKey(d => d.ChargePointID);
            this.HasOptional(t => t.ChargerType)
                .WithMany(t => t.ConnectionInfoes)
                .HasForeignKey(d => d.LevelTypeID);
            this.HasOptional(t => t.CurrentType)
                .WithMany(t => t.ConnectionInfoes)
                .HasForeignKey(d => d.CurrentTypeID);
            this.HasRequired(t => t.ConnectionType)
                .WithMany(t => t.ConnectionInfoes)
                .HasForeignKey(d => d.ConnectionTypeID);
            this.HasOptional(t => t.StatusType)
                .WithMany(t => t.ConnectionInfoes)
                .HasForeignKey(d => d.StatusTypeID);

        }
    }
}
