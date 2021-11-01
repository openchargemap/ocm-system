using System.ComponentModel.DataAnnotations;

namespace OCM.API.Common.Model
{
    public class PasswordResetRequestModel
    {
        [Required, Display(Name = "Your Email Address"), DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        public bool IsUnknownAccount { get; set; }

        public bool IsObsoleteLoginProvider { get; set; }

        public bool ResetInitiated { get; set; }
    }
}