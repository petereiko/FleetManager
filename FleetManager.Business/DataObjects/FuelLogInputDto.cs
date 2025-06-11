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
    public class FuelLogInputDto
    {
        [Required] public long DriverId { get; set; }
        [Required] public long VehicleId { get; set; }
        [Required, DataType(DataType.Date)] public DateTime Date { get; set; }
        [Range(0, int.MaxValue)] public int? Odometer { get; set; }
        [Required][Range(0, double.MaxValue)] public decimal Volume { get; set; }
        [Required][Range(0, double.MaxValue)] public decimal Cost { get; set; }
        [Required] public FuelType FuelType { get; set; }
        public IFormFile? ReceiptFile { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

}
