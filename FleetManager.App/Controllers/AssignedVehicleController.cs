using FleetManager.App.Areas.Admin.Controllers;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace FleetManager.App.Controllers
{
    public class AssignedVehicleController : Controller
    {
        
        private readonly IAuthUser _authUser;
        private readonly IDriverVehicleService _assignmentService;
        private readonly ILogger<AssignedVehicleController> _logger;

        public AssignedVehicleController(IAuthUser authUser, IDriverVehicleService assignmentService)
        {
            _authUser = authUser;
            _assignmentService = assignmentService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _authUser.UserId;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                // query assignments
                var q = _assignmentService
                    .QueryAssignmentsByDriver(driverId);

                

                var assignments = q
                    .OrderBy(a => a.StartDate)
                    .ToList();


                var vm = new AssignmentIndexViewModel
                {
                    DriverFilterId = driverId,
                    Assignments = assignments
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assignments");
                return View("Error");
            }
        }
          
    }
}
