using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OCM.Web.Models
{
    /// <summary>
    /// Model for Step 1: User submits their OCPI feed details
    /// </summary>
    public class OCPISubmitModel
    {
        [Required(ErrorMessage = "Company name is required")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Representative name is required")]
        [Display(Name = "Representative Name")]
        public string RepresentativeName { get; set; }

        [Required(ErrorMessage = "Contact email is required")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Display(Name = "Country")]
        public int CountryID { get; set; }

        [Required(ErrorMessage = "Provider name is required")]
        [Display(Name = "Provider Name")]
        public string ProviderName { get; set; }

        [Required(ErrorMessage = "OCPI Locations endpoint URL is required")]
        [Url(ErrorMessage = "Must be a valid URL")]
        [Display(Name = "OCPI Locations Endpoint URL")]
        public string LocationsEndpointUrl { get; set; }

        [Display(Name = "Website URL")]
        [Url(ErrorMessage = "Must be a valid URL")]
        public string WebsiteUrl { get; set; }

        [Display(Name = "Authorization Header Value (if required)")]
        public string AuthorizationHeaderValue { get; set; }

        [Display(Name = "Custom Authorization Header Key")]
        public string AuthorizationHeaderKey { get; set; }

        public string AuthorizationHeaderValuePrefix { get; set; }

        public string DataFeedType { get; set; } = "OCPI";

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the open data license")]
        [Display(Name = "I agree to license this data for re-distribution under the CC-0 license for open data.")]
        public bool AcceptedOpenDataLicense { get; set; }
    }

    /// <summary>
    /// Result of validating an OCPI feed
    /// </summary>
    public class OCPIValidationResult
    {
        public bool IsValid { get; set; }
        public int LocationCount { get; set; }
        public int EvseCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> DiscoveryLog { get; set; } = new List<string>();

        /// <summary>
        /// If the user submitted a versions endpoint, this is the resolved locations URL
        /// </summary>
        public string ResolvedLocationsEndpointUrl { get; set; }

        public string ResolvedAuthHeaderKey { get; set; }

        public string ResolvedAuthHeaderValuePrefix { get; set; }

        public string ResolvedAuthHeaderDisplayValue { get; set; }

        /// <summary>
        /// Distinct operator names/codes found in the OCPI data
        /// </summary>
        public List<DiscoveredOperator> DiscoveredOperators { get; set; } = new List<DiscoveredOperator>();

        /// <summary>
        /// Countries found in the data
        /// </summary>
        public List<string> DiscoveredCountries { get; set; } = new List<string>();

        /// <summary>
        /// Sample location titles for preview
        /// </summary>
        public List<string> SampleLocations { get; set; } = new List<string>();
    }

    /// <summary>
    /// An operator discovered from the OCPI feed data
    /// </summary>
    public class DiscoveredOperator
    {
        public string Name { get; set; }
        public int LocationCount { get; set; }

        /// <summary>
        /// If mapped to an existing OCM operator, the ID
        /// </summary>
        public int? MappedOperatorId { get; set; }

        /// <summary>
        /// If true, this is a new operator that needs to be created
        /// </summary>
        public bool IsNewOperator { get; set; }
    }

    /// <summary>
    /// Model for Step 2: validation results and operator mapping configuration
    /// </summary>
    public class OCPIValidateModel
    {
        public OCPISubmitModel SubmitDetails { get; set; }
        public OCPIValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Operator mappings: OCPI operator name -> selected OCM Operator ID (or 0 for new)
        /// </summary>
        public Dictionary<string, int> OperatorMappings { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Available OCM operators for the mapping dropdown
        /// </summary>
        public List<OCM.API.Common.Model.OperatorInfo> AvailableOperators { get; set; } = new List<OCM.API.Common.Model.OperatorInfo>();

        /// <summary>
        /// Default operator ID if there is only one operator
        /// </summary>
        public int? DefaultOperatorId { get; set; }
    }

    /// <summary>
    /// Model for Step 3: confirmation and data sharing agreement
    /// </summary>
    public class OCPIConfirmModel
    {
        public OCPISubmitModel SubmitDetails { get; set; }
        public OCPIValidationResult ValidationResult { get; set; }
        public Dictionary<string, int> OperatorMappings { get; set; } = new Dictionary<string, int>();
        public int? DefaultOperatorId { get; set; }

        [Required(ErrorMessage = "You must accept the data sharing agreement")]
        [Display(Name = "I agree to share this data under the specified license")]
        public bool AcceptedDataSharingAgreement { get; set; }
    }

    /// <summary>
    /// Final model after successful submission
    /// </summary>
    public class OCPISubmissionResultModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int? DataProviderId { get; set; }
        public string ProviderName { get; set; }
    }
}
