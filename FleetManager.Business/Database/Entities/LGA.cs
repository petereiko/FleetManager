using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class LGA
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public long? StateId { get; set; }
        public virtual State? State { get; set; }
    }
}
