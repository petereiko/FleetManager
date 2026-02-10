using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.RedisConfiguration
{
    public class RedisSettings
    {
        public string ConnectionString { get; set; } = "localhost:7008";
        public int DatabaseId { get; set; } = 0;
        public bool AbortOnConnectFail { get; set; } = false;
        public int ConnectTimeout { get; set; } = 5000;
        public int SyncTimeout { get; set; } = 5000;
        public string InstanceName { get; set; } = "FleetManager:";
    }
}
