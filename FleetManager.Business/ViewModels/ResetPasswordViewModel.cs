using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Id {  get; set; }
        public string ResetToken { get;set; }
        public string Password { get; set; }
    }
}
