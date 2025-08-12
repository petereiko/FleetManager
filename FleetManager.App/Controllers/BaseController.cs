using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class BaseController : Controller
    {
        private readonly IAuthUser _authUser;

        public BaseController(IAuthUser authUser)
        {
            _authUser = authUser;
        }

    }
}
