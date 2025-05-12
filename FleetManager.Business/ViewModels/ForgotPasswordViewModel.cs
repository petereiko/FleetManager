using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string Email {  get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

    }
}
