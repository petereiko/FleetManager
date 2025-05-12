using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class AuditFilterModel
    {
        public int? PassOrFail { get; set; }
        public string ResultConclusion { get; set; }
        public int? Gender { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Nationalitiy { get; set; }
    }
}
