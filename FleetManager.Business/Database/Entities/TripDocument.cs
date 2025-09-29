using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class TripDocument : BaseEntity
    {
        public long TripId { get; set; }
        public virtual Trip Trip { get; set; }

        public string DocumentName { get; set; }
        public string DocumentType { get; set; } // Waybill, Invoice, Proof of Delivery, etc.
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public long FileSize { get; set; }
        public string? Description { get; set; }
    }
}
