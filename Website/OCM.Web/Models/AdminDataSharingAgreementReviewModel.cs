using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OCM.API.Common.Model;

namespace OCM.Web.Models
{
    public class AdminDataSharingAgreementListItem
    {
        public DataSharingAgreement Agreement { get; set; }
        public DataProvider DataProvider { get; set; }
        public bool IsApproved => DataProvider?.IsApprovedImport == true;
    }

    public class AdminDataSharingAgreementReviewModel
    {
        public DataSharingAgreement Agreement { get; set; }
        public DataProvider DataProvider { get; set; }
        public bool IsApproved => DataProvider?.IsApprovedImport == true;
        public AdminDataSharingAgreementEditModel Review { get; set; } = new AdminDataSharingAgreementEditModel();
        public string ImportConfig { get; set; }
        public OCPIValidationResult ValidationPreview { get; set; }
        public List<OperatorInfo> AvailableOperators { get; set; } = new List<OperatorInfo>();
    }

    public class AdminDataSharingAgreementEditModel
    {
        [Required]
        public int AgreementId { get; set; }

        public int? DataProviderId { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public int CountryId { get; set; }

        [Required]
        public string RepresentativeName { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string ContactEmail { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string WebsiteUrl { get; set; }

        [Required]
        public string DataFeedType { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string SubmittedFeedUrl { get; set; }

        public string SubmittedCredentials { get; set; }

        [Required]
        public string ProviderName { get; set; }

        public string OutputNamePrefix { get; set; }

        public int? DataProviderOcpiId { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        public string LocationsEndpointUrl { get; set; }

        public string AuthHeaderKey { get; set; }

        public string AuthHeaderValuePrefix { get; set; }

        public string CredentialKey { get; set; }

        public int? DefaultOperatorId { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsAutoRefreshed { get; set; } = true;

        public bool IsProductionReady { get; set; } = true;

        public bool AllowDuplicatePOIWithDifferentOperator { get; set; } = true;

        public string Description { get; set; }

        public string OperatorMappingsText { get; set; }

        public string ExcludedLocationIdsText { get; set; }

        public bool ApproveImport { get; set; }
    }
}
