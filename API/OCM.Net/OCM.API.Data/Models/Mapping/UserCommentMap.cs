using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace OCM.Core.Data.Mapping
{
    public class UserCommentMap : EntityTypeConfiguration<UserComment>
    {
        public UserCommentMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            this.Property(t => t.UserName)
                .HasMaxLength(100);

            this.Property(t => t.RelatedURL)
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("UserComment");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.ChargePointID).HasColumnName("ChargePointID");
            this.Property(t => t.UserCommentTypeID).HasColumnName("UserCommentTypeID");
            this.Property(t => t.UserName).HasColumnName("UserName");
            this.Property(t => t.Comment).HasColumnName("Comment");
            this.Property(t => t.Rating).HasColumnName("Rating");
            this.Property(t => t.RelatedURL).HasColumnName("RelatedURL");
            this.Property(t => t.DateCreated).HasColumnName("DateCreated");
            this.Property(t => t.CheckinStatusTypeID).HasColumnName("CheckinStatusTypeID");
            this.Property(t => t.UserID).HasColumnName("UserID");
            this.Property(t => t.IsActionedByEditor).HasColumnName("IsActionedByEditor");

            // Relationships
            this.HasRequired(t => t.ChargePoint)
                .WithMany(t => t.UserComments)
                .HasForeignKey(d => d.ChargePointID);
            this.HasOptional(t => t.CheckinStatusType)
                .WithMany(t => t.UserComments)
                .HasForeignKey(d => d.CheckinStatusTypeID);
            this.HasOptional(t => t.User)
                .WithMany(t => t.UserComments)
                .HasForeignKey(d => d.UserID);
            this.HasRequired(t => t.UserCommentType)
                .WithMany(t => t.UserComments)
                .HasForeignKey(d => d.UserCommentTypeID);

        }
    }
}
