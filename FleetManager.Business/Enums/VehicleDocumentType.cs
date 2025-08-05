using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum VehicleDocumentType
    {
        [Name("Photo")]
        Photo = 1,

        [Name("Document")]
        Document = 2

        //Registration = 1,   // Vehicle registration document
        //Insurance,      // Vehicle insurance document
        //Roadworthiness,     // Vehicle inspection certificate
        //CustomsDocument,// Documents for imported or exported vehicles
    }
}
