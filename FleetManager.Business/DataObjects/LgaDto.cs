using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class LgaDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long StateId { get; set; }
        public StateDto State { get; set; }
    }
}
