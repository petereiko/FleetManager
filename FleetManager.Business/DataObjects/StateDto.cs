using FleetManager.Business.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class StateDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public ICollection<LGA> Lgas { get; set; } = new HashSet<LGA>();

    }

}