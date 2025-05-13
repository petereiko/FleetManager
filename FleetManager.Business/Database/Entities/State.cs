using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class State
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public string Code { get; set; }
        public ICollection<LGA> Lgas { get; set; } = new HashSet<LGA>();
    }
}
