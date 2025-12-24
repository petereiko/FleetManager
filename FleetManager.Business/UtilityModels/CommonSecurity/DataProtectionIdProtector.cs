using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.CommonSecurity
{

    /// <summary>
    /// DataProtection-based protector. Produces URL-safe Base64 tokens.
    /// </summary>
    public class DataProtectionIdProtector : IIdProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectionIdProtector(IDataProtectionProvider provider)
        {
            // purpose string isolates these protected payloads from other uses of data protection
            _protector = provider.CreateProtector("FleetManager.IdProtector.v1");
        }

        // -----------------------
        // numeric helpers
        // -----------------------
        public string ProtectId(long id) => ProtectString(id.ToString());

        public long? UnprotectId(string protectedId)
        {
            var s = UnprotectToString(protectedId);
            return long.TryParse(s, out var v) ? (long?)v : null;
        }

        // -----------------------
        // generic string helpers
        // -----------------------
        public string ProtectIdForAny(string idString) => ProtectString(idString);

        public string? UnprotectIdToString(string protectedId) => UnprotectToString(protectedId);

        // -----------------------
        // internal helpers
        // -----------------------
        private string ProtectString(string plain)
        {
            if (plain == null) throw new ArgumentNullException(nameof(plain));
            var bytes = Encoding.UTF8.GetBytes(plain);
            var protectedBytes = _protector.Protect(bytes);
            return WebEncoders.Base64UrlEncode(protectedBytes);
        }

        private string? UnprotectToString(string protectedId)
        {
            if (string.IsNullOrWhiteSpace(protectedId)) return null;
            try
            {
                var protectedBytes = WebEncoders.Base64UrlDecode(protectedId);
                var bytes = _protector.Unprotect(protectedBytes);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Do not leak exceptions — return null to indicate failure / invalid token
                return null;
            }
        }
    }


    //public class DataProtectionIdProtector : IIdProtector
    //{
    //    private readonly IDataProtector _protector;


    //    public DataProtectionIdProtector(IDataProtectionProvider provider)
    //    {
    //        _protector = provider.CreateProtector("FleetManager.IdProtector.v1");
    //    }


    //    public string ProtectId(long id)
    //    {
    //        var plain = Encoding.UTF8.GetBytes(id.ToString());
    //        var protectedBytes = _protector.Protect(plain);
    //        return WebEncoders.Base64UrlEncode(protectedBytes);
    //    }


    //    public long? UnprotectId(string protectedId)
    //    {
    //        if (string.IsNullOrWhiteSpace(protectedId)) return null;
    //        try
    //        {
    //            var protectedBytes = WebEncoders.Base64UrlDecode(protectedId);
    //            var bytes = _protector.Unprotect(protectedBytes);
    //            var s = Encoding.UTF8.GetString(bytes);
    //            return long.TryParse(s, out var id) ? (long?)id : null;
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }
    //}
}
