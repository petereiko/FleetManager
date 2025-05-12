using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class EmailLogAttachementDto
    {
        public string FolderOnServer { get; set; }
        public string FileNameOnServer { get; set; }
        public string EmailFileName { get; set; }
    }
}
