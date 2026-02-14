using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class MaintenanceTicketResponse
    {
        public long Id { get; set; }
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? AdminNotes { get; set; }
        public List<MaintenanceTicketItemResponse> Items { get; set; } = new();
        public InvoiceResponse? Invoice { get; set; }
    }

    // Models/API/Maintenance/MaintenanceTicketItemResponse.cs
    public class MaintenanceTicketItemResponse
    {
        public long Id { get; set; }
        public string PartCategoryName { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? CustomDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class InvoiceResponse
    {
        public long Id { get; set; }
        public long TicketId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<InvoiceItemResponse> Items { get; set; } = new();
    }

    // Models/API/Maintenance/InvoiceItemResponse.cs
    public class InvoiceItemResponse
    {
        public long Id { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string? PartCategory { get; set; }
        public string? CustomPartDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
