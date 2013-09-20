using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class AddressInfoMap : EntityTypeConfiguration<AddressInfo>
    {
        public AddressInfoMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .HasMaxLength(100);

            this.Property(t => t.AddressLine1)
                .HasMaxLength(1000);

            this.Property(t => t.AddressLine2)
                .HasMaxLength(1000);

            this.Property(t => t.Town)
                .HasMaxLength(100);

            this.Property(t => t.StateOrProvince)
                .HasMaxLength(100);

            this.Property(t => t.Postcode)
                .HasMaxLength(100);

            this.Property(t => t.ContactTelephone1)
                .HasMaxLength(200);

            this.Property(t => t.ContactTelephone2)
                .HasMaxLength(200);

            this.Property(t => t.ContactEmail)
                .HasMaxLength(500);

            this.Property(t => t.RelatedURL)
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("AddressInfo");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.AddressLine1).HasColumnName("AddressLine1");
            this.Property(t => t.AddressLine2).HasColumnName("AddressLine2");
            this.Property(t => t.Town).HasColumnName("Town");
            this.Property(t => t.StateOrProvince).HasColumnName("StateOrProvince");
            this.Property(t => t.Postcode).HasColumnName("Postcode");
            this.Property(t => t.CountryID).HasColumnName("CountryID");
            this.Property(t => t.Latitude).HasColumnName("Latitude");
            this.Property(t => t.Longitude).HasColumnName("Longitude");
            this.Property(t => t.ContactTelephone1).HasColumnName("ContactTelephone1");
            this.Property(t => t.ContactTelephone2).HasColumnName("ContactTelephone2");
            this.Property(t => t.ContactEmail).HasColumnName("ContactEmail");
            this.Property(t => t.AccessComments).HasColumnName("AccessComments");
            this.Property(t => t.GeneralComments).HasColumnName("GeneralComments");
            this.Property(t => t.RelatedURL).HasColumnName("RelatedURL");

            // Relationships
            this.HasOptional(t => t.Country)
                .WithMany(t => t.AddressInfoes)
                .HasForeignKey(d => d.CountryID);

        }
    }
}
