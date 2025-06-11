using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FleetManager.Business.Hubs
{
    
    [Authorize] // only authenticated users may connect
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Called when a client connects. We could add them to groups here if desired.
        /// </summary>
        public override Task OnConnectedAsync()
        {
            // Optionally, you can add this connection to a group based on user roles, etc.
            // e.g. await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");

            return base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects.
        /// </summary>
        public override Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Clean up group memberships if you used them
            return base.OnDisconnectedAsync(exception);
        }

        // You can define server-to-client methods here if you want clients to invoke.
        // But for our notifications, we'll just push from server via IHubContext.

        /// <summary>
        /// (Optional) a client-callable ping for connection testing.
        /// </summary>
        public Task Ping() => Clients.Caller.SendAsync("Pong");
    }
}
