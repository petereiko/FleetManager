using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class DriverDocument : BaseEntity
    {
        public long? DriverId { get; set; }
        public virtual Driver Driver { get; set; }

        public DriverDocumentType DocumentType { get; set; }
        public string FileName { get; set; }

        public string FilePath { get; set; }
    }
}
