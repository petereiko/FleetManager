using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces
{
    public interface IUserService
    {
        Task SeedRoles();
    }
}
