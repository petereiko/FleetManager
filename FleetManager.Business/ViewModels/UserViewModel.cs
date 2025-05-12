using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class UserViewModel
    {
        public long CompanyId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public bool IsFirstLogin { get; set; }
        public string LastName { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Id {  get; set; }
        public string Phone {  get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; }
        public List<CheckBoxListItemDto> Roles { get; set; }=new List<CheckBoxListItemDto>();
    }
}
