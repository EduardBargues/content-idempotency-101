using System;

namespace Idempotency
{
    public class IdempotencyConfiguration
    {
        public string HeaderName { get; set; }
        public TimeSpan TimeToLiveMaster { get; set; }
        public TimeSpan TimeToLiveDeprecation { get; set; }
    }
}