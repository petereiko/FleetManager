using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class PartCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Models/API/Maintenance/PartResponse.cs
    public class PartResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }

    // Models/API/Maintenance/PriorityOption.cs
    public class PriorityOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty; // For UI color coding
    }
}
