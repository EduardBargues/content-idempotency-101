using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Idempotency
{
    public interface IIdempotentStorage
    {
        Task<IdempotentResponse> LinkRequestAndKey(string key, string ownerId, TimeSpan timeToLive);
        Task CacheResponse(string key, string ownerId, int statusCode, string body, TimeSpan timeToLive);
    }
    public class InMemoryStorage : IIdempotentStorage
    {
        private readonly Dictionary<string, IdempotentResponse> _db;
        private readonly AsyncLock _mutex;
        private readonly ILogger<InMemoryStorage> _logger;

        public InMemoryStorage(ILogger<InMemoryStorage> logger)
        {
            _db = new Dictionary<string, IdempotentResponse>();
            _mutex = new AsyncLock();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IdempotentResponse> LinkRequestAndKey(string key, string ownerId, TimeSpan timeToLive)
        {
            using (await _mutex.LockAsync())
            {
                var alreadyStored = _db.ContainsKey(key);
                if (!alreadyStored)
                {
                    _logger.LogInformation($"----> ADDING IDEMPOTENT RESPONSE - key: {key} / ownerId: {ownerId}");
                    _db.Add(key, new IdempotentResponse(key, ownerId, timeToLive));
                    SetMasterTimeToLive(key, ownerId, timeToLive);
                }

                return _db[key];
            }
        }
        public async Task CacheResponse(string key, string ownerId, int statusCode, string body, TimeSpan timeToLive)
        {
            using (await _mutex.LockAsync())
            {
                var dbResponse = _db[key];
                if (dbResponse.OwnerId == ownerId && !dbResponse.Finished)
                {
                    _logger.LogInformation($"----> FINISHING IDEMPOTENT RESPONSE - key: {key} / ownerId: {ownerId}");
                    dbResponse.Finished = true;
                    dbResponse.StatusCode = statusCode;
                    dbResponse.Body = body;
                    dbResponse.TimeToLive = timeToLive;
                    SetDeprecationTimeToLive(key, timeToLive);
                }
            }
        }

        private void SetDeprecationTimeToLive(string key, TimeSpan ttl)
        {
            Task.Run(async () =>
            {
                await Task.Delay(ttl);
                using (await _mutex.LockAsync())
                {
                    if (_db.ContainsKey(key))
                    {
                        _logger.LogInformation($"----> DEPRECATING IDEMPOTENT RESPONSE - key: {key}");
                        _db.Remove(key);
                    }
                }
            });
        }
        private void SetMasterTimeToLive(string key, string ownerId, TimeSpan ttl)
        {
            Task.Run(async () =>
            {
                await Task.Delay(ttl);
                using (await _mutex.LockAsync())
                {
                    if (_db.ContainsKey(key) && !_db[key].Finished)
                    {
                        _logger.LogInformation($"----> DETACHING IDEMPOTENT RESPONSE FROM OWNER - key: {key} / ownerId: {ownerId}");
                        _db.Remove(key);
                    }
                }
            });
        }
    }
}