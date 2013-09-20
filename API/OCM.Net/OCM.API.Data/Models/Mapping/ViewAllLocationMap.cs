using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class ViewAllLocationMap : EntityTypeConfiguration<ViewAllLocation>
    {
        public ViewAllLocationMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.ID)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.DataProvider)
                .HasMaxLength(250);

            this.Property(t => t.LocationTitle)
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

            this.Property(t => t.Country)
                .HasMaxLength(100);

            this.Property(t => t.ISOCode)
                .HasMaxLength(100);

            this.Property(t => t.Usage)
                .HasMaxLength(200);

            this.Property(t => t.Operator)
                .HasMaxLength(250);

            this.Property(t => t.WebsiteURL)
                .HasMaxLength(500);

            this.Property(t => t.PhonePrimaryContact)
                .HasMaxLength(100);

            this.Property(t => t.PhoneSecondaryContact)
                .HasMaxLength(100);

            this.Property(t => t.BookingURL)
                .HasMaxLength(500);

            this.Property(t => t.DataProviderURL)
                .HasMaxLength(500);

            this.Property(t => t.DataProvidersReference)
                .HasMaxLength(100);

            this.Property(t => t.OperatorsReference)
                .HasMaxLength(100);

            this.Property(t => t.SubmissionStatus)
                .HasMaxLength(100);

            this.Property(t => t.EquipmentStatus)
                .HasMaxLength(100);

            // Table & Column Mappings
            this.ToTable("ViewAllLocations");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.DataProvider).HasColumnName("DataProvider");
            this.Property(t => t.LocationTitle).HasColumnName("LocationTitle");
            this.Property(t => t.AddressLine1).HasColumnName("AddressLine1");
            this.Property(t => t.AddressLine2).HasColumnName("AddressLine2");
            this.Property(t => t.Town).HasColumnName("Town");
            this.Property(t => t.StateOrProvince).HasColumnName("StateOrProvince");
            this.Property(t => t.Postcode).HasColumnName("Postcode");
            this.Property(t => t.Latitude).HasColumnName("Latitude");
            this.Property(t => t.Longitude).HasColumnName("Longitude");
            this.Property(t => t.ContactTelephone1).HasColumnName("ContactTelephone1");
            this.Property(t => t.ContactTelephone2).HasColumnName("ContactTelephone2");
            this.Property(t => t.ContactEmail).HasColumnName("ContactEmail");
            this.Property(t => t.AccessComments).HasColumnName("AccessComments");
            this.Property(t => t.GeneralComments).HasColumnName("GeneralComments");
            this.Property(t => t.RelatedURL).HasColumnName("RelatedURL");
            this.Property(t => t.Country).HasColumnName("Country");
            this.Property(t => t.ISOCode).HasColumnName("ISOCode");
            this.Property(t => t.Usage).HasColumnName("Usage");
            this.Property(t => t.IsPayAtLocation).HasColumnName("IsPayAtLocation");
            this.Property(t => t.IsMembershipRequired).HasColumnName("IsMembershipRequired");
            this.Property(t => t.IsAccessKeyRequired).HasColumnName("IsAccessKeyRequired");
            this.Property(t => t.Operator).HasColumnName("Operator");
            this.Property(t => t.WebsiteURL).HasColumnName("WebsiteURL");
            this.Property(t => t.Comments).HasColumnName("Comments");
            this.Property(t => t.PhonePrimaryContact).HasColumnName("PhonePrimaryContact");
            this.Property(t => t.PhoneSecondaryContact).HasColumnName("PhoneSecondaryContact");
            this.Property(t => t.IsPrivateIndividual).HasColumnName("IsPrivateIndividual");
            this.Property(t => t.BookingURL).HasColumnName("BookingURL");
            this.Property(t => t.DataProviderURL).HasColumnName("DataProviderURL");
            this.Property(t => t.DataProviderComments).HasColumnName("DataProviderComments");
            this.Property(t => t.DataProvidersReference).HasColumnName("DataProvidersReference");
            this.Property(t => t.OperatorsReference).HasColumnName("OperatorsReference");
            this.Property(t => t.NumberOfPoints).HasColumnName("NumberOfPoints");
            this.Property(t => t.EquipmentGeneralComments).HasColumnName("EquipmentGeneralComments");
            this.Property(t => t.DatePlanned).HasColumnName("DatePlanned");
            this.Property(t => t.DateLastConfirmed).HasColumnName("DateLastConfirmed");
            this.Property(t => t.DateLastStatusUpdate).HasColumnName("DateLastStatusUpdate");
            this.Property(t => t.DataQualityLevel).HasColumnName("DataQualityLevel");
            this.Property(t => t.DateCreated).HasColumnName("DateCreated");
            this.Property(t => t.SubmissionStatus).HasColumnName("SubmissionStatus");
            this.Property(t => t.IsLive).HasColumnName("IsLive");
            this.Property(t => t.EquipmentStatus).HasColumnName("EquipmentStatus");
            this.Property(t => t.IsOperational).HasColumnName("IsOperational");
        }
    }
}
