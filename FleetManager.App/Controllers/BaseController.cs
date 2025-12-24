using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels.CommonSecurity;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class BaseController : Controller
    {
        private readonly IAuthUser _authUser;
        private readonly IIdProtector _idProtector;

        public BaseController(IAuthUser authUser, IIdProtector idProtector)
        {
            _authUser = authUser;
            _idProtector = idProtector;
        }

        protected RedirectToActionResult RedirectToActionWithProtectedId(string action, string controller, long id)
        {
            return RedirectToAction(action, controller, new { id = _idProtector.ProtectId(id) });
        }
    }
}
