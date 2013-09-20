using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using OCM.Core.Data.Mapping;

namespace OCM.Core.Data
{
    public partial class OCMEntities : DbContext
    {
        static OCMEntities()
        {
            Database.SetInitializer<OCMEntities>(null);
        }

        public OCMEntities()
            : base("Name=OCMModelContext")
        {
        }

        public DbSet<AddressInfo> AddressInfoList { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ChargePoint> ChargePoints { get; set; }
        public DbSet<ChargerType> ChargerTypes { get; set; }
        public DbSet<CheckinStatusType> CheckinStatusTypes { get; set; }
        public DbSet<ConnectionInfo> ConnectionInfoList { get; set; }
        public DbSet<ConnectionType> ConnectionTypes { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<CurrentType> CurrentTypes { get; set; }
        public DbSet<DataProvider> DataProviders { get; set; }
        public DbSet<DataProviderStatusType> DataProviderStatusTypes { get; set; }
        public DbSet<DataProviderUser> DataProviderUsers { get; set; }
        public DbSet<DataType> DataTypes { get; set; }
        public DbSet<EditQueueItem> EditQueueItems { get; set; }
        public DbSet<EntityType> EntityTypes { get; set; }
        public DbSet<MediaItem> MediaItems { get; set; }
        public DbSet<MetadataField> MetadataFields { get; set; }
        public DbSet<MetadataFieldOption> MetadataFieldOptions { get; set; }
        public DbSet<MetadataGroup> MetadataGroups { get; set; }
        public DbSet<MetadataValue> MetadataValues { get; set; }
        public DbSet<Operator> Operators { get; set; }
        public DbSet<StatusType> StatusTypes { get; set; }
        public DbSet<SubmissionStatusType> SubmissionStatusTypes { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<UsageType> UsageTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserComment> UserComments { get; set; }
        public DbSet<UserCommentType> UserCommentTypes { get; set; }
        public DbSet<ViewAllEquipment> ViewAllEquipments { get; set; }
        public DbSet<ViewAllLocation> ViewAllLocations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new AddressInfoMap());
            modelBuilder.Configurations.Add(new AuditLogMap());
            modelBuilder.Configurations.Add(new ChargePointMap());
            modelBuilder.Configurations.Add(new ChargerTypeMap());
            modelBuilder.Configurations.Add(new CheckinStatusTypeMap());
            modelBuilder.Configurations.Add(new ConnectionInfoMap());
            modelBuilder.Configurations.Add(new ConnectionTypeMap());
            modelBuilder.Configurations.Add(new CountryMap());
            modelBuilder.Configurations.Add(new CurrentTypeMap());
            modelBuilder.Configurations.Add(new DataProviderMap());
            modelBuilder.Configurations.Add(new DataProviderStatusTypeMap());
            modelBuilder.Configurations.Add(new DataProviderUserMap());
            modelBuilder.Configurations.Add(new DataTypeMap());
            modelBuilder.Configurations.Add(new EditQueueItemMap());
            modelBuilder.Configurations.Add(new EntityTypeMap());
            modelBuilder.Configurations.Add(new MediaItemMap());
            modelBuilder.Configurations.Add(new MetadataFieldMap());
            modelBuilder.Configurations.Add(new MetadataFieldOptionMap());
            modelBuilder.Configurations.Add(new MetadataGroupMap());
            modelBuilder.Configurations.Add(new MetadataValueMap());
            modelBuilder.Configurations.Add(new OperatorMap());
            modelBuilder.Configurations.Add(new StatusTypeMap());
            modelBuilder.Configurations.Add(new SubmissionStatusTypeMap());
            modelBuilder.Configurations.Add(new SystemConfigMap());
            modelBuilder.Configurations.Add(new UsageTypeMap());
            modelBuilder.Configurations.Add(new UserMap());
            modelBuilder.Configurations.Add(new UserCommentMap());
            modelBuilder.Configurations.Add(new UserCommentTypeMap());
            modelBuilder.Configurations.Add(new ViewAllEquipmentMap());
            modelBuilder.Configurations.Add(new ViewAllLocationMap());
        }
    }
}
