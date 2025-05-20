using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VehicleDto
{
    public class VehicleEditViewModel : VehicleDto
    {
        public VehicleEditViewModel() { }
        public VehicleEditViewModel(VehicleDto m)
        {
            // copy all properties
            foreach (var prop in typeof(VehicleDto).GetProperties())
                prop.SetValue(this, prop.GetValue(m));
            ExistingImages = m.ExistingImages;
            ExistingDocuments = m.ExistingDocuments;
        }

        public IEnumerable<SelectListItem> Branches { get; set; }
        public IEnumerable<SelectListItem> FuelTypes { get; set; }
        public IEnumerable<SelectListItem> TransmissionTypes { get; set; }
        public IEnumerable<SelectListItem> Statuses { get; set; }
        public IEnumerable<SelectListItem> VehicleTypes { get; set; }
    }
}
