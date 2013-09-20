using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class OperatorMap : EntityTypeConfiguration<Operator>
    {
        public OperatorMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.Title)
                .HasMaxLength(250);

            this.Property(t => t.WebsiteURL)
                .HasMaxLength(500);

            this.Property(t => t.PhonePrimaryContact)
                .HasMaxLength(100);

            this.Property(t => t.PhoneSecondaryContact)
                .HasMaxLength(100);

            this.Property(t => t.BookingURL)
                .HasMaxLength(500);

            this.Property(t => t.ContactEmail)
                .HasMaxLength(500);

            this.Property(t => t.FaultReportEmail)
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("Operator");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Title).HasColumnName("Title");
            this.Property(t => t.WebsiteURL).HasColumnName("WebsiteURL");
            this.Property(t => t.Comments).HasColumnName("Comments");
            this.Property(t => t.PhonePrimaryContact).HasColumnName("PhonePrimaryContact");
            this.Property(t => t.PhoneSecondaryContact).HasColumnName("PhoneSecondaryContact");
            this.Property(t => t.IsPrivateIndividual).HasColumnName("IsPrivateIndividual");
            this.Property(t => t.AddressInfoID).HasColumnName("AddressInfoID");
            this.Property(t => t.BookingURL).HasColumnName("BookingURL");
            this.Property(t => t.ContactEmail).HasColumnName("ContactEmail");
            this.Property(t => t.FaultReportEmail).HasColumnName("FaultReportEmail");
            this.Property(t => t.IsRestrictedEdit).HasColumnName("IsRestrictedEdit");

            // Relationships
            this.HasOptional(t => t.AddressInfo)
                .WithMany(t => t.Operators)
                .HasForeignKey(d => d.AddressInfoID);

        }
    }
}
