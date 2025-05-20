using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.Database.Entities
{
    public class VehicleDocument:BaseEntity
    {
        public long? VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
        public VehicleDocumentType DocumentType { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }


    }
}
