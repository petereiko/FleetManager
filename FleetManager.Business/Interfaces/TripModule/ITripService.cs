using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels.TripsViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.TripModule
{
    public interface ITripService
    {
        // Trip CRUD Operations
        Task<MessageResponse<TripDto>> CreateTripAsync(CreateTripDto dto);
        Task<MessageResponse<TripDto>> UpdateTripAsync(UpdateTripDto dto);
        Task<MessageResponse<TripDto>> GetTripByIdAsync(long id);
        Task<MessageResponse<PaginatedResult<TripListDto>>> GetTripsAsync(TripFilterDto filter);
        Task<MessageResponse> DeleteTripAsync(long id);

        // Trip Assignment & Management
        Task<MessageResponse<TripDto>> AssignTripToDriverAsync(AssignTripDto dto);
        Task<MessageResponse<TripDto>> UnassignTripAsync(long tripId);
        Task<MessageResponse<TripDto>> StartTripAsync(StartTripDto dto);
        Task<MessageResponse<TripDto>> CompleteTripAsync(CompleteTripDto dto);
        Task<MessageResponse<TripDto>> CancelTripAsync(CancelTripDto dto);

        // Approval Workflow
        Task<MessageResponse<TripDto>> ApproveTripAsync(ApproveTripDto dto);
        Task<MessageResponse<PaginatedResult<TripListDto>>> GetPendingApprovalTripsAsync(int page, int pageSize);

        // Trip Expenses
        Task<MessageResponse<TripExpenseDto>> AddTripExpenseAsync(CreateTripExpenseDto dto);
        Task<MessageResponse<List<TripExpenseDto>>> GetTripExpensesAsync(long tripId);
        Task<MessageResponse> DeleteTripExpenseAsync(long expenseId);
        Task<MessageResponse<TripExpenseDto>> VerifyExpenseAsync(long expenseId);

        // Reports & Analytics
        Task<MessageResponse<TripStatistics>> GetTripStatisticsAsync(DateTime? startDate, DateTime? endDate);
        Task<MessageResponse<TripDashboardViewModel>> GetDashboardDataAsync();
        Task<MessageResponse<List<TripListDto>>> GetDriverTripsAsync(long driverId, int page, int pageSize);
        Task<MessageResponse<List<TripListDto>>> GetVehicleTripsAsync(long vehicleId, int page, int pageSize);

        // Validation & Business Rules
        Task<MessageResponse<bool>> ValidateTripAvailabilityAsync(long vehicleId, long? driverId, DateTime startDate, DateTime endDate, long? excludeTripId = null);

        Task<MessageResponse<List<SimpleVehicleDto>>> GetVehiclesForDriverAsync(long driverId, DateTime? scheduledStart = null, DateTime? scheduledEnd = null, bool excludeVehiclesOnTripOverlap = true);
        Task<MessageResponse<List<SimpleDriverDto>>> GetDriversForBranchAsync();
    }
}
