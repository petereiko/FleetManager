using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace FleetManager.Business.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Id {  get; set; }
        public string ResetToken { get;set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

    }
}
