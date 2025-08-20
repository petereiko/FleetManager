using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class ContactRatingDto
    {
        public long ContactId { get; set; }
        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
    }

    public class ContactRatingResultDto
    {
        public long ContactId { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        // optional: distribution
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }
}
