using FleetManager.Business.GoogleRoutesApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.GoogleViewModels
{
    public class RouteViewModel
    {
        [BindProperty]
        public string OriginAddress { get; set; } = "";

        [BindProperty]
        public string DestinationAddress { get; set; } = "";

        public DirectionsResponse? Response { get; set; }
    }
}
