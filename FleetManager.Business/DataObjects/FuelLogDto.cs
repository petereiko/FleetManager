using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class FuelLogDto
    {
        public long? Id { get; set; }
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = "";
        public DateTime Date { get; set; }
        public int? Odometer { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public FuelType FuelType { get; set; }
        public string? ReceiptPath { get; set; }
        public string? Notes { get; set; }
        public string CreatedBy { get; set; }
        public string LicenseNo { get; set; } = "";

    }


}
