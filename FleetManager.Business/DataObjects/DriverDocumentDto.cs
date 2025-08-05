using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class DriverDocumentDto
    {
        public long Id { get; set; }
        public IFormFile FileData { get; set; }
        public string? FileName { get; set; }
        public DriverDocumentType DocumentType { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}
