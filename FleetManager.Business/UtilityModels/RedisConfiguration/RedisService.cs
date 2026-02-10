using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.RedisConfiguration
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisService> _logger;
        private readonly string _instanceName;

        public RedisService(
            IConnectionMultiplexer redis,
            IOptions<RedisSettings> settings,
            ILogger<RedisService> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase(settings.Value.DatabaseId);
            _logger = logger;
            _instanceName = settings.Value.InstanceName;
        }

        private string GetKey(string key) => $"{_instanceName}{key}";

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(GetKey(key));
                return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis GET error for key: {Key}", key);
                return default;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                if (expiry.HasValue)
                {
                    return await _db.StringSetAsync(GetKey(key), serialized, new StackExchange.Redis.Expiration(expiry.Value));
                }
                else
                {
                    return await _db.StringSetAsync(GetKey(key), serialized);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET error for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                return await _db.KeyDeleteAsync(GetKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis DELETE error for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _db.KeyExistsAsync(GetKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis EXISTS error for key: {Key}", key);
                return false;
            }
        }

        public async Task<long> ListPushAsync<T>(string key, T value)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                return await _db.ListRightPushAsync(GetKey(key), serialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis LIST PUSH error for key: {Key}", key);
                return 0;
            }
        }

        public async Task<List<T>> ListRangeAsync<T>(string key, long start = 0, long stop = -1)
        {
            try
            {
                var values = await _db.ListRangeAsync(GetKey(key), start, stop);
                return values.Select(v => JsonSerializer.Deserialize<T>(v!)).Where(v => v != null).ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis LIST RANGE error for key: {Key}", key);
                return new List<T>();
            }
        }

        public async Task<long> ListLengthAsync(string key)
        {
            try
            {
                return await _db.ListLengthAsync(GetKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis LIST LENGTH error for key: {Key}", key);
                return 0;
            }
        }

        public async Task<long> ListRemoveAsync<T>(string key, T value)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                return await _db.ListRemoveAsync(GetKey(key), serialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis LIST REMOVE error for key: {Key}", key);
                return 0;
            }
        }

        public async Task<bool> ListTrimAsync(string key, long start, long stop)
        {
            try
            {
                await _db.ListTrimAsync(GetKey(key), start, stop);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis LIST TRIM error for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> SetAddAsync<T>(string key, T value)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                return await _db.SetAddAsync(GetKey(key), serialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET ADD error for key: {Key}", key);
                return false;
            }
        }

        public async Task<List<T>> SetMembersAsync<T>(string key)
        {
            try
            {
                var values = await _db.SetMembersAsync(GetKey(key));
                return values.Select(v => JsonSerializer.Deserialize<T>(v!)).Where(v => v != null).ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET MEMBERS error for key: {Key}", key);
                return new List<T>();
            }
        }

        public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
        {
            try
            {
                return await _db.KeyExpireAsync(GetKey(key), expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis KEY EXPIRE error for key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> SetRemoveAsync<T>(string key, T value)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                return await _db.SetRemoveAsync(GetKey(key), serialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET REMOVE error for key: {Key}", key);
                return false;
            }
        }
    }
}
