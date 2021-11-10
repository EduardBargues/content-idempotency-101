
using System;

namespace Idempotency
{
    public class IdempotentResponse
    {
        public IdempotentResponse(string key, string ownerId, TimeSpan timeToLive)
        {
            Key = key;
            OwnerId = ownerId;
            TimeToLive = timeToLive;
        }

        public string Key { get; set; }
        public string OwnerId { get; set; }
        public bool Finished { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public int StatusCode { get; set; }
        public string Body { get; set; }
    }
}