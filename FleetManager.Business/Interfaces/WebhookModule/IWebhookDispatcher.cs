using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.WebhookModule
{
    public interface IWebhookDispatcher
    {
        Task DispatchAsync(string eventName, long entityId, object payload, CancellationToken cancellationToken = default);
    }

}
