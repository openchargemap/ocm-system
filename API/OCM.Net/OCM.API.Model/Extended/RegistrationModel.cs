using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class RegistrationModel : PasswordChangeModel
    {
        [Display(Name = "Email Address"), Required(ErrorMessage = "Required"), DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        [Display(Name = "Name or Screen Name (shown publicly)"), Required(ErrorMessage = "Required"), MinLength(3), MaxLength(20)]
        public string Username { get; set; }

        public bool RegistrationFailed { get; set; }
    }
}