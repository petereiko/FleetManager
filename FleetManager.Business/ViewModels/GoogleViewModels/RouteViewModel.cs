using FleetManager.Business.GoogleRoutesApi.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.GoogleViewModels
{
    public class RouteViewModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Please enter the origin address.")]
        public string OriginAddress { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Please enter the destination address.")]
        public string DestinationAddress { get; set; } = "";

        public DirectionsResponse? Response { get; set; }
    }
}
