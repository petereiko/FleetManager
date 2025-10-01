using FleetManager.Business;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.Enums;
using FleetManager.Business.Implementations.Webhooks;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.WebhookModule;
using iTextSharp.text.log;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels 
{
    public class NotificationWorker
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationWorker> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Entry point used by Hangfire. Example enqueue:
        /// _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripStarted", tripId, correlationId));
        /// </summary>
        public async Task ProcessEvent(string eventName, long tripId, string correlationId = null)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<FleetManagerDbContext>();
            var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var webhookDispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationWorker>>();

            try
            {
                var trip = await db.Trips
                    .AsNoTracking()
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.Id == tripId);

                if (trip == null)
                {
                    logger.LogWarning("NotificationWorker: trip {TripId} not found for event {Event}", tripId, eventName);
                    return;
                }

                var driverName = trip.Driver != null ? $"{trip.Driver.User?.FirstName} {trip.Driver.User?.LastName}" : null;
                var tripNumber = trip.TripNumber;

                // Make branchId explicitly nullable so we can safely test for presence
                long? branchId = trip.CompanyBranchId;

                switch (eventName)
                {
                    case "TripAssigned":
                        if (trip.Driver?.UserId != null)
                        {
                            var title = "New Trip Assigned";
                            var body = $"You have been assigned Trip {tripNumber} from {trip.Origin} to {trip.Destination} scheduled for {trip.ScheduledStartDate:u}.";
                            await SafeNotifyAsync(notification, trip.Driver.UserId, title, body, "TripAssigned", tripId, correlationId);
                        }

                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "Trip Assigned", $"{driverName} assigned to {tripNumber} ({trip.Origin} → {trip.Destination})", "TripAssigned", tripId, correlationId);
                        break;

                    case "TripUnassigned":
                        if (trip.Driver?.UserId != null)
                        {
                            var title = "Trip Unassigned";
                            var body = $"You have been unassigned from Trip {tripNumber} ({trip.Origin} → {trip.Destination}).";
                            await SafeNotifyAsync(notification, trip.Driver.UserId, title, body, "TripUnassigned", tripId, correlationId);
                        }

                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "Trip Unassigned", $"{driverName} unassigned from {tripNumber}", "TripUnassigned", tripId, correlationId);
                        break;

                    case "TripStarted":
                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "Trip Started", $"{driverName} started {tripNumber} to {trip.Destination} at {trip.ActualStartDate:u}", "TripStarted", tripId, correlationId);

                        if (trip.Driver?.UserId != null)
                        {
                            await SafeNotifyAsync(notification, trip.Driver.UserId, "Trip Started (Confirmed)", $"You started Trip {tripNumber} to {trip.Destination} at {trip.ActualStartDate:u}. Drive safely.", "TripStarted", tripId, correlationId);
                        }

                        await SafeDispatchWebhookAsync(webhookDispatcher, "TripStarted", tripId, new
                        {
                            @event = "TripStarted",
                            tripId = trip.Id,
                            tripNumber = trip.TripNumber,
                            driverId = trip.DriverId,
                            driverName,
                            origin = trip.Origin,
                            destination = trip.Destination,
                            startedAt = trip.ActualStartDate,
                            startOdometer = trip.StartOdometer,
                            branchId = trip.CompanyBranchId,
                            companyId = trip.CompanyId
                        }, logger);
                        break;

                    case "TripCompleted":
                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "Trip Completed", $"{driverName} completed {tripNumber}. Distance: {trip.ActualDistance} km", "TripCompleted", tripId, correlationId);

                        if (trip.Driver?.UserId != null)
                        {
                            await SafeNotifyAsync(notification, trip.Driver.UserId, "Trip Completed (Confirmed)", $"You completed Trip {tripNumber}. Distance: {trip.ActualDistance} km. Great work.", "TripCompleted", tripId, correlationId);
                        }

                        await SafeDispatchWebhookAsync(webhookDispatcher, "TripCompleted", tripId, new
                        {
                            @event = "TripCompleted",
                            tripId = trip.Id,
                            tripNumber = trip.TripNumber,
                            driverId = trip.DriverId,
                            driverName,
                            origin = trip.Origin,
                            destination = trip.Destination,
                            completedAt = trip.ActualEndDate,
                            distance = trip.ActualDistance,
                            fuelCost = trip.ActualFuelCost,
                            branchId = trip.CompanyBranchId,
                            companyId = trip.CompanyId
                        }, logger);
                        break;

                    case "TripCancelled":
                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "Trip Cancelled", $"Trip {tripNumber} was cancelled.", "TripCancelled", tripId, correlationId);

                        if (trip.Driver?.UserId != null)
                        {
                            await SafeNotifyAsync(notification, trip.Driver.UserId, "Trip Cancelled", $"Trip {tripNumber} has been cancelled. Please check the app for details.", "TripCancelled", tripId, correlationId);
                        }
                        break;

                    case "TripApproved":
                        if (trip.Driver?.UserId != null)
                        {
                            await SafeNotifyAsync(notification, trip.Driver.UserId, "Trip Approved", $"Trip {tripNumber} has been approved.", "TripApproved", tripId, correlationId);
                        }

                        if (!string.IsNullOrEmpty(trip.CreatedBy))
                        {
                            await SafeNotifyAsync(notification, trip.CreatedBy, "Trip Approved", $"Your Trip {tripNumber} was approved.", "TripApproved", tripId, correlationId);
                        }
                        break;

                    case "TripExpenseAdded":
                        if (branchId != null)
                            await NotifyAdminsAsync(db, notification, branchId.Value, "New Trip Expense", $"A new expense was added to {tripNumber}. Please review.", "TripExpenseAdded", tripId, correlationId);
                        break;

                    default:
                        logger.LogInformation("NotificationWorker: Unhandled event {Event} for trip {TripId}", eventName, tripId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationWorker.ProcessEvent error for event {Event} trip {TripId}", eventName, tripId);
                throw;
            }
        }

        // -- helper instance methods (keeps everything inside the class)
        private async Task SafeNotifyAsync(INotificationService notification, string userId, string title, string message, string eventKey, long tripId, string correlationId)
        {
            try
            {
                // Adjust this call to match your INotificationService signature if different
                await notification.CreateAsync(userId, title, message, NotificationType.Info, new { @event = eventKey, tripId, correlationId });
            }
            catch (Exception ex)
            {
                // swallow to avoid blocking other notifications, but log at debug
                _logger.LogDebug(ex, "SafeNotifyAsync failed for user {UserId} event {EventKey} trip {TripId}", userId, eventKey, tripId);
            }
        }

        private async Task NotifyAdminsAsync(FleetManagerDbContext db, INotificationService notification, long branchId, string title, string message, string eventKey, long tripId, string correlationId)
        {
            try
            {
                var admins = await db.CompanyAdmins
                    .AsNoTracking()
                    .Where(a => a.CompanyBranchId == branchId && a.IsActive)
                    .Select(a => a.UserId)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    try
                    {
                        await notification.CreateAsync(adminId, title, message, NotificationType.Info, new { @event = eventKey, tripId, correlationId });
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogDebug(innerEx, "NotifyAdminsAsync: failed to notify admin {AdminId} for trip {TripId}", adminId, tripId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotifyAdminsAsync failed for branch {BranchId} trip {TripId}", branchId, tripId);
            }
        }

        private async Task SafeDispatchWebhookAsync(IWebhookDispatcher dispatcher, string eventName, long tripId, object payload, Microsoft.Extensions.Logging.ILogger logger)
        {
            try
            {
                await dispatcher.DispatchAsync(eventName, tripId, payload);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Webhook dispatch failed for event {Event} trip {TripId}", eventName, tripId);
            }
        }
    }
}

