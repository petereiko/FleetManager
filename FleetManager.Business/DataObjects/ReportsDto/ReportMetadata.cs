using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ReportMetadata
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public List<string> Parameters { get; set; } = new();

        public DateTime LastGenerated { get; set; }  // Used for "Updated"
        public int EstimatedReadTimeMinutes { get; set; } // Optional
        public int CompletenessPercentage { get; set; }   // Used for progress bar

    }
}
