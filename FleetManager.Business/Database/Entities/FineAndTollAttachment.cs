using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class FineAndTollAttachment : BaseEntity
    {
        public long FineAndTollId { get; set; }
        public virtual FineAndToll FineAndToll { get; set; }

        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
