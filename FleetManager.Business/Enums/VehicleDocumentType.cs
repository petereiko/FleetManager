using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum VehicleDocumentType
    {
        Registration = 1,   // Vehicle registration document
        Insurance,      // Vehicle insurance document
        Roadworthiness,     // Vehicle inspection certificate
        CustomsDocument,// Documents for imported or exported vehicles
    }
}
