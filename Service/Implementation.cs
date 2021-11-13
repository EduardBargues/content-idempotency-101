using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Service
{
    public interface IImplementation
    {
        Task<Guid> ProcessAsync(Transaction transaction);
    }

    public class Implementation : IImplementation
    {
        private readonly ILogger<Implementation> _logger;

        public Implementation(ILogger<Implementation> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Guid> ProcessAsync(Transaction transaction)
        {
            _logger.LogInformation($"----> PROCESSING TRANSACTION. amount:{transaction.Amount} origin:{transaction.OriginId} destination:{transaction.DestinationId}");
            await Task.Delay(1000);
            return Guid.NewGuid();
        }
    }

    public class Transaction
    {
        public decimal Amount { get; set; }
        public string OriginId { get; set; }
        public string DestinationId { get; set; }
    }
}
