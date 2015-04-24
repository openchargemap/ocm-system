using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model
{
    public class LoginModel
    {
        [Display(Name = "Email"), Required, DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        [Display(Name = "Password"), Required, MinLength(6), DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }
    }
}