using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.UserModule
{
    public interface IAuthUser
    {
        string Email { get; }
        string UserId { get; }
        string Roles { get; }
        string FullName { get; }
        string BaseUrl { get; }

        //Added newly
        long? CompanyId { get; }
        long? CompanyBranchId { get; }
    }
}
