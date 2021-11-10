using System;
using System.Threading.Tasks;

namespace Idempotency
{
    public interface IIdempotentStorage
    {
        Task<IdempotentResponse> MasterExecution(string key, string ownerId, TimeSpan timeToLive);
        Task FinishExecution(string key, string ownerId, int statusCode, string body, TimeSpan timeToLive);
    }
}