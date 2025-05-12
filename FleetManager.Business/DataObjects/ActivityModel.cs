
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class ActivityModel
    {
        public long Id { get; set; }
        public string BusinessName { get; set; }
        public string Applicant { get; set; }
        public string Nationality { get; set; }
        public string ResultServiceType { get; set; }
        //public string Status { get; set; }
        public string ResultConclusion { get; set; }
        public string Gender { get; set; }
        //public string PassOrFail { get; set; }
        public DateTime? TestDate { get; set; }
        public string Age { get; set; }
    }
}
