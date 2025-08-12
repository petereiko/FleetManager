using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class CompanyOwnerDashboardViewModel
    {
        public int BranchCount { get; set; }
        public int AdminCount { get; set; }
        public int DriverCount { get; set; }
        public int VehicleCount { get; set; }

        public List<CompanyBranchItem> BranchItems { get; set; } = new List<CompanyBranchItem>();
    }

    public class CompanyBranchItem
    {
        public int AdminCount { get; set; }
        public int DriverCount { get; set; } = 0;
        public int VehicleCount { get; set; } = 0;
        public string Branch { get; set; }
        public bool IsHeadquarter { get; set; }

    }
}
