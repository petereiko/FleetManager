using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.CommonSecurity
{

    public interface IIdProtector
    {
        // numeric long convenience (keeps existing call sites)
        string ProtectId(long id);
        long? UnprotectId(string protectedId);

        // generic string-based protect/unprotect for GUIDs or other textual ids
        string ProtectIdForAny(string idString);
        string? UnprotectIdToString(string protectedId);
    }


    //public interface IIdProtector
    //{
    //    string ProtectId(long id);
    //    long? UnprotectId(string protectedId);
    //}
}
