using System.Collections.Generic;

namespace OCM.API.Common.Model
{
    public enum StandardDataProviders
    {
        OpenChargeMapContrib = 1
    }

    public enum StandardStatusTypes
    {
        Unknown = 0,
        CurrentlyAvailable = 10,
        CurrentlyInUse = 20,
        TemporarilyUnavailable = 30,
        Operational = 50,
        PartlyOperational = 75,
        NotOperational = 100,
        PlannedForFutureDate = 150,
        RemovedDecomissioned = 200
    }

    public enum StandardOperators
    {
        UnknownOperator = 1,
        PrivateIndividual = 44,
        BusinessOwner = 45,
        ChargePoint = 5,
        Blink = 9,
        Tesla = 23,
        ChargeFox = 3339,
        ShellRechargeUS = 59,
        BPPulse = 32
    }

    public enum StandardUsageTypes
    {
        Unknown = 0,
        Public = 1,
        PrivateRestricted = 2,
        PrivatelyOwned_NoticeRequired = 3,
        Public_MembershipRequired = 4,
        Public_PayAtLocation = 5
    }

    public enum StandardSubmissionStatusTypes
    {
        Submitted_UnderReview = 1,
        Imported_UnderReview = 50,
        Imported_Published = 100,
        Submitted_Published = 200,
        Submission_Rejected_Incomplete = 250,
        Delisted = 1000,
        Delisted_Duplicate = 1001,
        Delisted_NoLongerActive = 1002,
        Delisted_SupersededByUpdate = 1005,
        Delisted_NotPublicInformation = 1010,
        Delisted_RemovedByDataProvider = 1020
    }

    public enum StandardCommentTypes
    {
        GeneralComment = 10,
        ImportantNotice = 50,
        SuggestedChange = 100,
        SuggestedChangeActioned = 110,
        FaultReport = 1000
    }

    public enum StandardUsers
    {
        System = 1008
    }

    public enum StandardEntityTypes
    {
        POI = 1
    }

    public enum StandardConnectionTypes
    {
        Unknown = 0,
        J1772 = 1,
        CHAdeMO = 2,
        BS1363TypeG = 3,
        MennekesType2 = 25,
        MennekesType2Tethered = 1036,
        Schuko = 28,
        CCSComboType1 = 32,
        CCSComboType2 = 33,
        Nema5_15 = 22,
        Nema5_20 = 9,
        Nema14_50 = 11,
        TeslaRoadster = 8,
        TeslaProprietary = 30,
        TeslaSupercharger = 27,
        Type3 = 26,
        Europlug = 13,
        AS3112 = 29,
        ThreePhaseAU = 1041
    }

    public enum StandardCurrentTypes
    {
        SinglePhaseAC = 10,
        ThreePhaseAC = 20,
        DC = 30
    }

    public enum StandardMetadataFields
    {
        POIType = 1,
        AccessHours = 2,
        Attribution = 4,
        VehicleType = 5
    }

    public enum StandardMetadataFieldOptions
    {
        /*Vehicle Types*/
        Car = 5,
        Motorbike = 6,
        Bus = 7,
        DeliveryVehicle = 10,
        Taxi = 11,
        Cycle = 12,
        /*POI Types*/
        Parking = 8,
        Shopping = 1,
        Hotel = 2
    }

    public class CoreReferenceData
    {
        public List<ChargerType> ChargerTypes { get; set; }
        public List<ConnectionType> ConnectionTypes { get; set; }
        public List<CurrentType> CurrentTypes { get; set; }
        public List<Country> Countries { get; set; }
        public List<DataProvider> DataProviders { get; set; }
        public List<OperatorInfo> Operators { get; set; }
        public List<StatusType> StatusTypes { get; set; }
        public List<SubmissionStatusType> SubmissionStatusTypes { get; set; }
        public List<UsageType> UsageTypes { get; set; }
        public List<UserCommentType> UserCommentTypes { get; set; }
        public List<CheckinStatusType> CheckinStatusTypes { get; set; }

        public List<DataType> DataTypes { get; set; }
        public List<MetadataGroup> MetadataGroups { get; set; }

        public User UserProfile { get; set; }

        /// <summary>
        /// Blank item used as template to populate/construct JSON object
        /// </summary>
        public ChargePoint ChargePoint { get; set; }

        public UserComment UserComment { get; set; }
    }
}