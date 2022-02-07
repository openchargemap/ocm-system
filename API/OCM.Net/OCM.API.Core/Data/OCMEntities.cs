using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OCM.Core.Data
{
    public partial class OCMEntities : DbContext
    {
        public static readonly ILoggerFactory ConsoleLogger = LoggerFactory.Create(builder => { builder.AddConsole(); });
        public OCMEntities()
        {
        }

        public OCMEntities(DbContextOptions<OCMEntities> options)
            : base(options)
        {
        }

        public virtual DbSet<AddressInfo> AddressInfoes { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<ChargePoint> ChargePoints { get; set; }
        public virtual DbSet<ChargerType> ChargerTypes { get; set; }
        public virtual DbSet<CheckinStatusType> CheckinStatusTypes { get; set; }
        public virtual DbSet<ConnectionInfo> ConnectionInfoes { get; set; }
        public virtual DbSet<ConnectionType> ConnectionTypes { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<CurrentType> CurrentTypes { get; set; }
        public virtual DbSet<DataProvider> DataProviders { get; set; }
        public virtual DbSet<DataProviderStatusType> DataProviderStatusTypes { get; set; }
        public virtual DbSet<DataSharingAgreement> DataSharingAgreements { get; set; }
        public virtual DbSet<DataType> DataTypes { get; set; }
        public virtual DbSet<EditQueueItem> EditQueueItems { get; set; }
        public virtual DbSet<EditQueueItemArchive> EditQueueItemArchives { get; set; }
        public virtual DbSet<EntityType> EntityTypes { get; set; }
        public virtual DbSet<MediaItem> MediaItems { get; set; }
        public virtual DbSet<MetadataField> MetadataFields { get; set; }
        public virtual DbSet<MetadataFieldOption> MetadataFieldOptions { get; set; }
        public virtual DbSet<MetadataGroup> MetadataGroups { get; set; }
        public virtual DbSet<MetadataValue> MetadataValues { get; set; }
        public virtual DbSet<Operator> Operators { get; set; }
        public virtual DbSet<RegisteredApplication> RegisteredApplications { get; set; }
        public virtual DbSet<RegisteredApplicationUser> RegisteredApplicationUsers { get; set; }
        public virtual DbSet<Statistic> Statistics { get; set; }
        public virtual DbSet<StatusType> StatusTypes { get; set; }
        public virtual DbSet<SubmissionStatusType> SubmissionStatusTypes { get; set; }
        public virtual DbSet<SystemConfig> SystemConfigs { get; set; }
        public virtual DbSet<UsageType> UsageTypes { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserChargingRequest> UserChargingRequests { get; set; }
        public virtual DbSet<UserComment> UserComments { get; set; }
        public virtual DbSet<UserCommentType> UserCommentTypes { get; set; }
        public virtual DbSet<UserSubscription> UserSubscriptions { get; set; }
        public virtual DbSet<ViewAllEquipment> ViewAllEquipments { get; set; }
        public virtual DbSet<ViewAllLocation> ViewAllLocations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseLoggerFactory(ConsoleLogger);
                optionsBuilder.UseLazyLoadingProxies();

                optionsBuilder.UseSqlServer(System.Configuration.ConfigurationManager.ConnectionStrings["OCMEntities"].ConnectionString, x =>
                {
                    x.UseNetTopologySuite();
                    x.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
                    x.EnableRetryOnFailure(3);

                });

#if DEBUG
                optionsBuilder.EnableDetailedErrors(true);
                optionsBuilder.EnableSensitiveDataLogging(true);
#endif
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AddressInfo>(entity =>
            {
                entity.ToTable("AddressInfo");

                entity.HasIndex(e => e.SpatialPosition, "IX_AddresLocationSpatial");

                entity.HasIndex(e => e.CountryId, "IX_AddressInfo_CountryID>")
                    .HasFillFactor(100);

                entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_AddressLocation")
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AddressLine1).HasMaxLength(1000);

                entity.Property(e => e.AddressLine2).HasMaxLength(1000);

                entity.Property(e => e.ContactEmail).HasMaxLength(500);

                entity.Property(e => e.ContactTelephone1).HasMaxLength(200);

                entity.Property(e => e.ContactTelephone2).HasMaxLength(200);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.Postcode).HasMaxLength(100);

                entity.Property(e => e.RelatedUrl)
                    .HasMaxLength(500)
                    .HasColumnName("RelatedURL");

                entity.Property(e => e.SpatialPosition).HasComputedColumnSql("([geography]::Point([Latitude],[Longitude],(4326)))", true);

                entity.Property(e => e.StateOrProvince).HasMaxLength(100);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Town).HasMaxLength(100);

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.AddressInfoes)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AddressInfo_Country");
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLog");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.EventDate).HasColumnType("datetime");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AuditLogs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AuditLog_User");
            });

            modelBuilder.Entity<ChargePoint>(entity =>
            {
                entity.ToTable("ChargePoint");

                entity.HasIndex(e => e.Uuid, "IX_ChargePoint")
                    .IsUnique();

                entity.HasIndex(e => e.AddressInfoId, "IX_ChargePointAddressID")
                    .HasFillFactor(100);

                entity.HasIndex(e => e.DateLastStatusUpdate, "IX_ChargePoint_DateLastStatusUpdate")
                    .HasFillFactor(100);

                entity.HasIndex(e => e.ParentChargePointId, "IX_ChargePoint_ParentID")
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AddressInfoId).HasColumnName("AddressInfoID");

                entity.Property(e => e.DataProviderId).HasColumnName("DataProviderID");

                entity.Property(e => e.DataProvidersReference).HasMaxLength(100);

                entity.Property(e => e.DateCreated)
                    .HasColumnType("smalldatetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateLastConfirmed).HasColumnType("smalldatetime");

                entity.Property(e => e.DateLastStatusUpdate).HasColumnType("smalldatetime");

                entity.Property(e => e.DatePlanned).HasColumnType("smalldatetime");

                entity.Property(e => e.OperatorId).HasColumnName("OperatorID");

                entity.Property(e => e.OperatorsReference).HasMaxLength(100);

                entity.Property(e => e.ParentChargePointId).HasColumnName("ParentChargePointID");

                entity.Property(e => e.StatusTypeId)
                    .HasColumnName("StatusTypeID")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.SubmissionStatusTypeId).HasColumnName("SubmissionStatusTypeID");

                entity.Property(e => e.UsageCost).HasMaxLength(200);

                entity.Property(e => e.UsageTypeId).HasColumnName("UsageTypeID");

                entity.Property(e => e.Uuid)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("UUID")
                    .HasDefaultValueSql("(newid())");

                entity.HasOne(d => d.AddressInfo)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.AddressInfoId)
                    .HasConstraintName("FK_ChargePoint_AddressInfo");

                entity.HasOne(d => d.DataProvider)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.DataProviderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ChargePoint_DataProvider");

                entity.HasOne(d => d.Operator)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.OperatorId)
                    .HasConstraintName("FK_ChargePoint_Operator");

                entity.HasOne(d => d.ParentChargePoint)
                    .WithMany(p => p.InverseParentChargePoint)
                    .HasForeignKey(d => d.ParentChargePointId)
                    .HasConstraintName("FK_ChargePoint_ChargePoint");

                entity.HasOne(d => d.StatusType)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.StatusTypeId)
                    .HasConstraintName("FK_ChargePoint_StatusType");

                entity.HasOne(d => d.SubmissionStatusType)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.SubmissionStatusTypeId)
                    .HasConstraintName("FK_ChargePoint_SubmissionStatusType");

                entity.HasOne(d => d.UsageType)
                    .WithMany(p => p.ChargePoints)
                    .HasForeignKey(d => d.UsageTypeId)
                    .HasConstraintName("FK_ChargePoint_UsageType");
            });

            modelBuilder.Entity<ChargerType>(entity =>
            {
                entity.ToTable("ChargerType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<CheckinStatusType>(entity =>
            {
                entity.ToTable("CheckinStatusType");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.IsPositive)
                    .HasDefaultValueSql("((1))")
                    .HasComment("If true, implies positive, if false, implies negative, if null implies neutral");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<ConnectionInfo>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PK_ConnectionInfo_1")
                    .IsClustered(false);

                entity.ToTable("ConnectionInfo");

                entity.HasComment("List of equipment types and specifications for a given POI");

                entity.HasIndex(e => e.ChargePointId, "IX_ConnectionInfoChargePoint")
                    .IsClustered()
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ChargePointId).HasColumnName("ChargePointID");

                entity.Property(e => e.ConnectionTypeId).HasColumnName("ConnectionTypeID");

                entity.Property(e => e.CurrentTypeId).HasColumnName("CurrentTypeID");

                entity.Property(e => e.LevelTypeId).HasColumnName("LevelTypeID");

                entity.Property(e => e.PowerKw).HasColumnName("PowerKW");

                entity.Property(e => e.Reference).HasMaxLength(100);

                entity.Property(e => e.StatusTypeId).HasColumnName("StatusTypeID");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.ConnectionInfoes)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientCascade)
                    .HasConstraintName("FK_ConnectionInfo_ChargePoint");

                entity.HasOne(d => d.ConnectionType)
                    .WithMany(p => p.ConnectionInfoes)
                    .HasForeignKey(d => d.ConnectionTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConnectionInfo_ConnectorType");

                entity.HasOne(d => d.CurrentType)
                    .WithMany(p => p.ConnectionInfoes)
                    .HasForeignKey(d => d.CurrentTypeId)
                    .HasConstraintName("FK_ConnectionInfo_CurrentType");

                entity.HasOne(d => d.LevelType)
                    .WithMany(p => p.ConnectionInfoes)
                    .HasForeignKey(d => d.LevelTypeId)
                    .HasConstraintName("FK_ConnectionInfo_ChargerType");

                entity.HasOne(d => d.StatusType)
                    .WithMany(p => p.ConnectionInfoes)
                    .HasForeignKey(d => d.StatusTypeId)
                    .HasConstraintName("FK_ConnectionInfo_StatusType");
            });

            modelBuilder.Entity<ConnectionType>(entity =>
            {
                entity.ToTable("ConnectionType");

                entity.HasIndex(e => e.Id, "IX_ConnectionType_Title")
                    .IsUnique()
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.FormalName).HasMaxLength(200);

                entity.Property(e => e.IsDiscontinued).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsObsolete).HasDefaultValueSql("((0))");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<Country>(entity =>
            {
                entity.ToTable("Country");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ContinentCode)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Isocode)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("ISOCode");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });


            modelBuilder.Entity<CurrentType>(entity =>
            {
                entity.ToTable("CurrentType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Description).HasMaxLength(500);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<DataProvider>(entity =>
            {
                entity.ToTable("DataProvider");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DataProviderStatusTypeId).HasColumnName("DataProviderStatusTypeID");

                entity.Property(e => e.DateLastImported).HasColumnType("datetime");

                entity.Property(e => e.IsApprovedImport).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsOpenDataLicensed).HasDefaultValueSql("((0))");

                entity.Property(e => e.License).HasMaxLength(250);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.WebsiteUrl)
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");

                entity.HasOne(d => d.DataProviderStatusType)
                    .WithMany(p => p.DataProviders)
                    .HasForeignKey(d => d.DataProviderStatusTypeId)
                    .HasConstraintName("FK_DataProvider_DataProviderStatus");
            });

            modelBuilder.Entity<DataProviderStatusType>(entity =>
            {
                entity.ToTable("DataProviderStatusType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.IsProviderEnabled)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<DataSharingAgreement>(entity =>
            {
                entity.ToTable("DataSharingAgreement");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ContactEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.DataFeedType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DataFeedUrl)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("DataFeedURL");

                entity.Property(e => e.DateAgreed).HasColumnType("datetime");

                entity.Property(e => e.DateCreated).HasColumnType("datetime");

                entity.Property(e => e.RepresentativeName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.WebsiteUrl)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("WebsiteURL");
            });

            modelBuilder.Entity<DataType>(entity =>
            {
                entity.ToTable("DataType");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Title).HasMaxLength(100);
            });

            modelBuilder.Entity<EditQueueItem>(entity =>
            {
                entity.ToTable("EditQueueItem");

                entity.HasIndex(e => e.DateSubmitted, "IX_EditQueueFilters")
                    .HasFillFactor(100);

                entity.HasIndex(e => new { e.UserId, e.EntityId }, "IX_UserID_EntityID")
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DateProcessed).HasColumnType("smalldatetime");

                entity.Property(e => e.DateSubmitted)
                    .HasColumnType("smalldatetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EntityId).HasColumnName("EntityID");

                entity.Property(e => e.EntityTypeId).HasColumnName("EntityTypeID");

                entity.Property(e => e.ProcessedByUserId)
                    .HasColumnName("ProcessedByUserID")
                    .HasComment("Editor who approved/processed this edit");

                entity.Property(e => e.UserId)
                    .HasColumnName("UserID")
                    .HasComment("User who submitted this edit");

                entity.HasOne(d => d.EntityType)
                    .WithMany(p => p.EditQueueItems)
                    .HasForeignKey(d => d.EntityTypeId)
                    .HasConstraintName("FK_EditQueueItem_EntityType");

                entity.HasOne(d => d.ProcessedByUser)
                    .WithMany(p => p.EditQueueItemProcessedByUsers)
                    .HasForeignKey(d => d.ProcessedByUserId)
                    .HasConstraintName("FK_EditQueue_User1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.EditQueueItemUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_EditQueue_User");
            });

            modelBuilder.Entity<EditQueueItemArchive>(entity =>
            {
                entity.ToTable("EditQueueItemArchive");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.DateProcessed).HasColumnType("smalldatetime");

                entity.Property(e => e.DateSubmitted).HasColumnType("smalldatetime");

                entity.Property(e => e.EntityId).HasColumnName("EntityID");

                entity.Property(e => e.EntityTypeId).HasColumnName("EntityTypeID");

                entity.Property(e => e.ProcessedByUserId).HasColumnName("ProcessedByUserID");

                entity.Property(e => e.UserId).HasColumnName("UserID");
            });

            modelBuilder.Entity<EntityType>(entity =>
            {
                entity.ToTable("EntityType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<MediaItem>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .IsClustered(false);

                entity.ToTable("MediaItem");

                entity.HasIndex(e => e.ChargePointId, "IX_MediaItem")
                    .IsClustered()
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ChargePointId).HasColumnName("ChargePointID");

                entity.Property(e => e.Comment).HasMaxLength(1000);

                entity.Property(e => e.DateCreated)
                    .HasColumnType("smalldatetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.IsEnabled)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ItemThumbnailUrl)
                    .HasMaxLength(500)
                    .HasColumnName("ItemThumbnailURL");

                entity.Property(e => e.ItemUrl)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("ItemURL");

                entity.Property(e => e.MetadataValue).HasMaxLength(1000);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.MediaItems)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MediaItem_ChargePoint");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.MediaItems)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MediaItem_User");
            });

            modelBuilder.Entity<MetadataField>(entity =>
            {
                entity.ToTable("MetadataField");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DataTypeId).HasColumnName("DataTypeID");

                entity.Property(e => e.MetadataGroupId).HasColumnName("MetadataGroupID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.DataType)
                    .WithMany(p => p.MetadataFields)
                    .HasForeignKey(d => d.DataTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataField_DataType");

                entity.HasOne(d => d.MetadataGroup)
                    .WithMany(p => p.MetadataFields)
                    .HasForeignKey(d => d.MetadataGroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataField_MetadataGroup");
            });

            modelBuilder.Entity<MetadataFieldOption>(entity =>
            {
                entity.ToTable("MetadataFieldOption");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.MetadataFieldId).HasColumnName("MetadataFieldID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.MetadataField)
                    .WithMany(p => p.MetadataFieldOptions)
                    .HasForeignKey(d => d.MetadataFieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataFieldOption_MetadataField");
            });

            modelBuilder.Entity<MetadataGroup>(entity =>
            {
                entity.ToTable("MetadataGroup");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DataProviderId).HasColumnName("DataProviderID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.DataProvider)
                    .WithMany(p => p.MetadataGroups)
                    .HasForeignKey(d => d.DataProviderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataGroup_DataProvider");
            });

            modelBuilder.Entity<MetadataValue>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PK_MetadataValue_1")
                    .IsClustered(false);

                entity.ToTable("MetadataValue");

                entity.HasComment("Holds custom defined meta data values for a given POI");

                entity.HasIndex(e => e.ChargePointId, "IX_MetadataValue_ChargePointID")
                    .IsClustered()
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ChargePointId)
                    .HasColumnName("ChargePointID")
                    .HasComment("ID of POI");

                entity.Property(e => e.MetadataFieldId)
                    .HasColumnName("MetadataFieldID")
                    .HasComment("Metadata Field value relates to");

                entity.Property(e => e.MetadataFieldOptionId).HasColumnName("MetadataFieldOptionID");

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.MetadataValues)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataValue_ChargePoint");

                entity.HasOne(d => d.MetadataField)
                    .WithMany(p => p.MetadataValues)
                    .HasForeignKey(d => d.MetadataFieldId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MetadataValue_MetadataField");

                entity.HasOne(d => d.MetadataFieldOption)
                    .WithMany(p => p.MetadataValues)
                    .HasForeignKey(d => d.MetadataFieldOptionId)
                    .HasConstraintName("FK_MetadataValue_MetadataFieldOption");
            });

            modelBuilder.Entity<Operator>(entity =>
            {
                entity.ToTable("Operator");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AddressInfoId).HasColumnName("AddressInfoID");

                entity.Property(e => e.BookingUrl)
                    .HasMaxLength(500)
                    .HasColumnName("BookingURL");

                entity.Property(e => e.ContactEmail).HasMaxLength(500);

                entity.Property(e => e.FaultReportEmail).HasMaxLength(500);

                entity.Property(e => e.IsRestrictedEdit).HasDefaultValueSql("((0))");

                entity.Property(e => e.PhonePrimaryContact).HasMaxLength(100);

                entity.Property(e => e.PhoneSecondaryContact).HasMaxLength(100);

                entity.Property(e => e.Title).HasMaxLength(250);

                entity.Property(e => e.WebsiteUrl)
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");

                entity.HasOne(d => d.AddressInfo)
                    .WithMany(p => p.Operators)
                    .HasForeignKey(d => d.AddressInfoId)
                    .HasConstraintName("FK_Operator_AddressInfo");
            });

            modelBuilder.Entity<RegisteredApplication>(entity =>
            {
                entity.ToTable("RegisteredApplication");

                entity.HasIndex(e => e.AppId, "IX_RegisteredApplication_AppID")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AppId)
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasColumnName("AppID");

                entity.Property(e => e.DateApikeyLastUsed)
                    .HasColumnType("datetime")
                    .HasColumnName("DateAPIKeyLastUsed");

                entity.Property(e => e.DateApikeyUpdated)
                    .HasColumnType("datetime")
                    .HasColumnName("DateAPIKeyUpdated");

                entity.Property(e => e.DateCreated).HasColumnType("datetime");

                entity.Property(e => e.DeprecatedApikey)
                    .HasMaxLength(100)
                    .HasColumnName("DeprecatedAPIKey");

                entity.Property(e => e.PrimaryApikey)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("PrimaryAPIKey");

                entity.Property(e => e.SharedSecret)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.WebsiteUrl)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RegisteredApplications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RegisteredApplication_User");
            });

            modelBuilder.Entity<RegisteredApplicationUser>(entity =>
            {
                entity.ToTable("RegisteredApplicationUser");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Apikey)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("APIKey")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.DateCreated).HasColumnType("datetime");

                entity.Property(e => e.IsEnabled)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.IsWriteEnabled)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RegisteredApplicationId).HasColumnName("RegisteredApplicationID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.RegisteredApplication)
                    .WithMany(p => p.RegisteredApplicationUsers)
                    .HasForeignKey(d => d.RegisteredApplicationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RegisteredApplicationUser_RegisteredApplication");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RegisteredApplicationUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RegisteredApplicationUser_User");
            });

            modelBuilder.Entity<SessionState>(entity =>
            {
                entity.ToTable("SessionState");

                entity.Property(e => e.Id).HasMaxLength(449);

                entity.Property(e => e.Value).IsRequired();
            });

            modelBuilder.Entity<Statistic>(entity =>
            {
                entity.ToTable("Statistic");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasDefaultValueSql("(newid())");

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.StatTypeCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Statistics)
                    .HasForeignKey(d => d.CountryId)
                    .HasConstraintName("FK_Statistics_Country");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Statistics)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Statistics_User");
            });

            modelBuilder.Entity<StatusType>(entity =>
            {
                entity.ToTable("StatusType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.IsUserSelectable)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<SubmissionStatusType>(entity =>
            {
                entity.ToTable("SubmissionStatusType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasKey(e => e.ConfigKeyName);

                entity.ToTable("SystemConfig");

                entity.Property(e => e.ConfigKeyName).HasMaxLength(100);

                entity.Property(e => e.ConfigValue).HasMaxLength(500);

                entity.Property(e => e.DataTypeId).HasColumnName("DataTypeID");

                entity.HasOne(d => d.DataType)
                    .WithMany(p => p.SystemConfigs)
                    .HasForeignKey(d => d.DataTypeId)
                    .HasConstraintName("FK_SystemConfig_DataType");
            });

            modelBuilder.Entity<UsageType>(entity =>
            {
                entity.ToTable("UsageType");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.IsPublicAccess).HasDefaultValueSql("((0))");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Apikey)
                    .HasMaxLength(100)
                    .HasColumnName("APIKey");

                entity.Property(e => e.CurrentSessionToken).HasMaxLength(100);

                entity.Property(e => e.DateCreated)
                    .HasColumnType("smalldatetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateLastLogin).HasColumnType("smalldatetime");

                entity.Property(e => e.EmailAddress).HasMaxLength(500);

                entity.Property(e => e.Identifier)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.IdentityProvider)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.IsProfilePublic)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Location).HasMaxLength(500);

                entity.Property(e => e.PasswordHash).HasMaxLength(500);

                entity.Property(e => e.ReputationPoints).HasDefaultValueSql("((0))");

                entity.Property(e => e.Username).HasMaxLength(100);

                entity.Property(e => e.WebsiteUrl)
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");
            });

            modelBuilder.Entity<UserChargingRequest>(entity =>
            {
                entity.ToTable("UserChargingRequest");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Comment).IsRequired();

                entity.Property(e => e.DateCreated).HasColumnType("datetime");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserChargingRequests)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserChargingRequest_User");
            });

            modelBuilder.Entity<UserComment>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PK_UserComment_1")
                    .IsClustered(false);

                entity.ToTable("UserComment");

                entity.HasIndex(e => e.ChargePointId, "IX_UserComment_ChargePoint")
                    .IsClustered()
                    .HasFillFactor(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ChargePointId).HasColumnName("ChargePointID");

                entity.Property(e => e.CheckinStatusTypeId).HasColumnName("CheckinStatusTypeID");

                entity.Property(e => e.DateCreated)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActionedByEditor).HasDefaultValueSql("((0))");

                entity.Property(e => e.RelatedUrl)
                    .HasMaxLength(500)
                    .HasColumnName("RelatedURL");

                entity.Property(e => e.UserCommentTypeId).HasColumnName("UserCommentTypeID");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.UserName).HasMaxLength(100);

                entity.HasOne(d => d.ChargePoint)
                    .WithMany(p => p.UserComments)
                    .HasForeignKey(d => d.ChargePointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserComment_ChargePoint");

                entity.HasOne(d => d.CheckinStatusType)
                    .WithMany(p => p.UserComments)
                    .HasForeignKey(d => d.CheckinStatusTypeId)
                    .HasConstraintName("FK_UserComment_CheckinStatusType");

                entity.HasOne(d => d.UserCommentType)
                    .WithMany(p => p.UserComments)
                    .HasForeignKey(d => d.UserCommentTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserComment_UserCommentType");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserComments)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserComment_User");
            });

            modelBuilder.Entity<UserCommentType>(entity =>
            {
                entity.ToTable("UserCommentType");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.ToTable("UserSubscription");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.DateCreated)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.DateLastNotified).HasColumnType("datetime");

                entity.Property(e => e.DistanceKm).HasColumnName("DistanceKM");

                entity.Property(e => e.IsEnabled)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.NotificationFrequencyMins).HasDefaultValueSql("((5))");

                entity.Property(e => e.NotifyPoiadditions).HasColumnName("NotifyPOIAdditions");

                entity.Property(e => e.NotifyPoiedits).HasColumnName("NotifyPOIEdits");

                entity.Property(e => e.NotifyPoiupdates).HasColumnName("NotifyPOIUpdates");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.UserSubscriptions)
                    .HasForeignKey(d => d.CountryId)
                    .HasConstraintName("FK_UserSubscription_Country");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserSubscriptions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserSubscription_User");
            });

            modelBuilder.Entity<ViewAllEquipment>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("ViewAllEquipment");

                entity.Property(e => e.AddressLine1).HasMaxLength(1000);

                entity.Property(e => e.AddressLine2).HasMaxLength(1000);

                entity.Property(e => e.BookingUrl)
                    .HasMaxLength(500)
                    .HasColumnName("BookingURL");

                entity.Property(e => e.Connection1Type)
                    .HasMaxLength(200)
                    .HasColumnName("Connection1_Type");

                entity.Property(e => e.ContactEmail).HasMaxLength(500);

                entity.Property(e => e.ContactTelephone1).HasMaxLength(200);

                entity.Property(e => e.ContactTelephone2).HasMaxLength(200);

                entity.Property(e => e.Country).HasMaxLength(100);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.DataProvider).HasMaxLength(250);

                entity.Property(e => e.DataProviderId).HasColumnName("DataProviderID");

                entity.Property(e => e.DataProviderUrl)
                    .HasMaxLength(500)
                    .HasColumnName("DataProviderURL");

                entity.Property(e => e.DataProvidersReference).HasMaxLength(100);

                entity.Property(e => e.DateCreated).HasColumnType("smalldatetime");

                entity.Property(e => e.DateLastConfirmed).HasColumnType("smalldatetime");

                entity.Property(e => e.DateLastStatusUpdate).HasColumnType("smalldatetime");

                entity.Property(e => e.DatePlanned).HasColumnType("smalldatetime");

                entity.Property(e => e.EquipmentStatus).HasMaxLength(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Isocode)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("ISOCode");

                entity.Property(e => e.LocationTitle).HasMaxLength(100);

                entity.Property(e => e.Operator).HasMaxLength(250);

                entity.Property(e => e.OperatorId).HasColumnName("OperatorID");

                entity.Property(e => e.OperatorsReference).HasMaxLength(100);

                entity.Property(e => e.PhonePrimaryContact).HasMaxLength(100);

                entity.Property(e => e.PhoneSecondaryContact).HasMaxLength(100);

                entity.Property(e => e.Postcode).HasMaxLength(100);

                entity.Property(e => e.RelatedUrl)
                    .HasMaxLength(500)
                    .HasColumnName("RelatedURL");

                entity.Property(e => e.StateOrProvince).HasMaxLength(100);

                entity.Property(e => e.SubmissionStatus).HasMaxLength(100);

                entity.Property(e => e.Town).HasMaxLength(100);

                entity.Property(e => e.Usage).HasMaxLength(200);

                entity.Property(e => e.WebsiteUrl)
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");
            });

            modelBuilder.Entity<ViewAllLocation>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("ViewAllLocations");

                entity.Property(e => e.AddressLine1).HasMaxLength(1000);

                entity.Property(e => e.AddressLine2).HasMaxLength(1000);

                entity.Property(e => e.BookingUrl)
                    .HasMaxLength(500)
                    .HasColumnName("BookingURL");

                entity.Property(e => e.ContactEmail).HasMaxLength(500);

                entity.Property(e => e.ContactTelephone1).HasMaxLength(200);

                entity.Property(e => e.ContactTelephone2).HasMaxLength(200);

                entity.Property(e => e.Country).HasMaxLength(100);

                entity.Property(e => e.DataProvider).HasMaxLength(250);

                entity.Property(e => e.DataProviderId).HasColumnName("DataProviderID");

                entity.Property(e => e.DataProviderUrl)
                    .HasMaxLength(500)
                    .HasColumnName("DataProviderURL");

                entity.Property(e => e.DataProvidersReference).HasMaxLength(100);

                entity.Property(e => e.DateCreated).HasColumnType("smalldatetime");

                entity.Property(e => e.DateLastConfirmed).HasColumnType("smalldatetime");

                entity.Property(e => e.DateLastStatusUpdate).HasColumnType("smalldatetime");

                entity.Property(e => e.DatePlanned).HasColumnType("smalldatetime");

                entity.Property(e => e.EquipmentStatus).HasMaxLength(100);

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Isocode)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("ISOCode");

                entity.Property(e => e.LocationTitle)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Operator).HasMaxLength(250);

                entity.Property(e => e.OperatorsReference).HasMaxLength(100);

                entity.Property(e => e.PhonePrimaryContact).HasMaxLength(100);

                entity.Property(e => e.PhoneSecondaryContact).HasMaxLength(100);

                entity.Property(e => e.Postcode).HasMaxLength(100);

                entity.Property(e => e.RelatedUrl)
                    .HasMaxLength(500)
                    .HasColumnName("RelatedURL");

                entity.Property(e => e.StateOrProvince).HasMaxLength(100);

                entity.Property(e => e.SubmissionStatus).HasMaxLength(100);

                entity.Property(e => e.Town).HasMaxLength(100);

                entity.Property(e => e.Usage).HasMaxLength(200);

                entity.Property(e => e.WebsiteUrl)
                    .HasMaxLength(500)
                    .HasColumnName("WebsiteURL");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
