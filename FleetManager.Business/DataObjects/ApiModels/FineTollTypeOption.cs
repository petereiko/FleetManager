using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FineTollTypeOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class FineTollStatusOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
