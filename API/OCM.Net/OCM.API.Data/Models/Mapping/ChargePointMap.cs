using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class ChargePointMap : EntityTypeConfiguration<ChargePoint>
    {
        public ChargePointMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.UUID)
                .IsRequired()
                .HasMaxLength(100);

            this.Property(t => t.DataProvidersReference)
                .HasMaxLength(100);

            this.Property(t => t.OperatorsReference)
                .HasMaxLength(100);

            this.Property(t => t.UsageCost)
                .HasMaxLength(200);

            // Table & Column Mappings
            this.ToTable("ChargePoint");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.UUID).HasColumnName("UUID");
            this.Property(t => t.ParentChargePointID).HasColumnName("ParentChargePointID");
            this.Property(t => t.DataProviderID).HasColumnName("DataProviderID");
            this.Property(t => t.DataProvidersReference).HasColumnName("DataProvidersReference");
            this.Property(t => t.OperatorID).HasColumnName("OperatorID");
            this.Property(t => t.OperatorsReference).HasColumnName("OperatorsReference");
            this.Property(t => t.UsageTypeID).HasColumnName("UsageTypeID");
            this.Property(t => t.AddressInfoID).HasColumnName("AddressInfoID");
            this.Property(t => t.NumberOfPoints).HasColumnName("NumberOfPoints");
            this.Property(t => t.GeneralComments).HasColumnName("GeneralComments");
            this.Property(t => t.DatePlanned).HasColumnName("DatePlanned");
            this.Property(t => t.DateLastConfirmed).HasColumnName("DateLastConfirmed");
            this.Property(t => t.StatusTypeID).HasColumnName("StatusTypeID");
            this.Property(t => t.DateLastStatusUpdate).HasColumnName("DateLastStatusUpdate");
            this.Property(t => t.DataQualityLevel).HasColumnName("DataQualityLevel");
            this.Property(t => t.DateCreated).HasColumnName("DateCreated");
            this.Property(t => t.SubmissionStatusTypeID).HasColumnName("SubmissionStatusTypeID");
            this.Property(t => t.UsageCost).HasColumnName("UsageCost");
            this.Property(t => t.ContributorUserID).HasColumnName("ContributorUserID");

            // Relationships
            this.HasOptional(t => t.AddressInfo)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.AddressInfoID);
            this.HasOptional(t => t.ParentChargePoint)
                .WithMany(t => t.ChildChargePoints)
                .HasForeignKey(d => d.ParentChargePointID);
            this.HasRequired(t => t.DataProvider)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.DataProviderID);
            this.HasOptional(t => t.Operator)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.OperatorID);
            this.HasOptional(t => t.StatusType)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.StatusTypeID);
            this.HasOptional(t => t.SubmissionStatusType)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.SubmissionStatusTypeID);
            this.HasOptional(t => t.UsageType)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.UsageTypeID);
            this.HasOptional(t => t.Contributor)
                .WithMany(t => t.ChargePoints)
                .HasForeignKey(d => d.ContributorUserID);

        }
    }
}
