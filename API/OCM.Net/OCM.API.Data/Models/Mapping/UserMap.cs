using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class UserMap : EntityTypeConfiguration<User>
    {
        public UserMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.IdentityProvider)
                .IsRequired()
                .HasMaxLength(100);

            this.Property(t => t.Identifier)
                .IsRequired()
                .HasMaxLength(200);

            this.Property(t => t.Username)
                .HasMaxLength(100);

            this.Property(t => t.CurrentSessionToken)
                .HasMaxLength(100);

            this.Property(t => t.Location)
                .HasMaxLength(500);

            this.Property(t => t.WebsiteURL)
                .HasMaxLength(500);

            this.Property(t => t.EmailAddress)
                .HasMaxLength(500);

            this.Property(t => t.APIKey)
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("User");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.IdentityProvider).HasColumnName("IdentityProvider");
            this.Property(t => t.Identifier).HasColumnName("Identifier");
            this.Property(t => t.Username).HasColumnName("Username");
            this.Property(t => t.CurrentSessionToken).HasColumnName("CurrentSessionToken");
            this.Property(t => t.Profile).HasColumnName("Profile");
            this.Property(t => t.Location).HasColumnName("Location");
            this.Property(t => t.WebsiteURL).HasColumnName("WebsiteURL");
            this.Property(t => t.ReputationPoints).HasColumnName("ReputationPoints");
            this.Property(t => t.Permissions).HasColumnName("Permissions");
            this.Property(t => t.PermissionsRequested).HasColumnName("PermissionsRequested");
            this.Property(t => t.DateCreated).HasColumnName("DateCreated");
            this.Property(t => t.DateLastLogin).HasColumnName("DateLastLogin");
            this.Property(t => t.EmailAddress).HasColumnName("EmailAddress");
            this.Property(t => t.IsEmergencyChargingProvider).HasColumnName("IsEmergencyChargingProvider");
            this.Property(t => t.IsProfilePublic).HasColumnName("IsProfilePublic");
            this.Property(t => t.IsPublicChargingProvider).HasColumnName("IsPublicChargingProvider");
            this.Property(t => t.APIKey).HasColumnName("APIKey");
            this.Property(t => t.Latitude).HasColumnName("Latitude");
            this.Property(t => t.Longitude).HasColumnName("Longitude");
        }
    }
}
