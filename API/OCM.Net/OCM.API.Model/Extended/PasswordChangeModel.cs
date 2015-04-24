using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class PasswordChangeModel
    {
        [Display(Name = "Password"), Required, MinLength(6), DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Confirm Password"), Required, Compare("Password"), DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string ConfirmedPassword { get; set; }

        public bool IsCurrentPasswordRequired { get; set; }

        [Display(Name = "Current Password"), DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string CurrentPassword { get; set; }

        public bool PasswordResetFailed { get; set; }

        public bool PasswordResetCompleted { get; set; }
    }
}