using FleetManager.Business.GoogleRoutesApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FleetManager.Business.GoogleRoutesApi.Interfaces
{
    public interface IGoogleRoutesService
    {
        Task<DirectionsResponse> ComputeRoutesAsync(ComputeRoutesRequest request, CancellationToken cancellationToken = default);
    }
}
