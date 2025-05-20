using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleDocumentDto
    {
        public long Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public VehicleDocumentType DocumentType { get; set; }
    }
}
