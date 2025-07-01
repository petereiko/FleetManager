using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class EmailLogDto
    {
        public long? CompanyId { get; set; }
        public long? CompanyBranchId { get; set; }
        public long? VendorId { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
        public string Subject { get; set; }
        public string CC { get; set; }
        public string BCC { get;set; }
        public bool hasAttachment { get; set; }
        public List<EmailLogAttachementDto> attachmentModelCol { get; set; } = new();
    }
}
