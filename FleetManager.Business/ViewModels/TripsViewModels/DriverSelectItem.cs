using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class DriverSelectItem
    {
        public long Id { get; set; }    // Identity user id string or driver unique id depending on your schema
        public string Name { get; set; }
    }
}
