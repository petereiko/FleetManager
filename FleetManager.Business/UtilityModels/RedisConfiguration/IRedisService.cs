using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.RedisConfiguration
{
    public interface IRedisService
    {
        Task<T?> GetAsync<T>(string key);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<long> ListPushAsync<T>(string key, T value);
        Task<List<T>> ListRangeAsync<T>(string key, long start = 0, long stop = -1);
        Task<long> ListLengthAsync(string key);
        Task<long> ListRemoveAsync<T>(string key, T value);
        Task<bool> ListTrimAsync(string key, long start, long stop);
        Task<bool> SetAddAsync<T>(string key, T value);
        Task<List<T>> SetMembersAsync<T>(string key);
        Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
        Task<bool> SetRemoveAsync<T>(string key, T value);
    }
}
