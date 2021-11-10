using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Service
{
    public interface IImplementation
    {
        Task<string> DoAsync(string path);
    }

    public class Implementation : IImplementation
    {
        private readonly ILogger<Implementation> _logger;

        public Implementation(ILogger<Implementation> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<string> DoAsync(string path)
        {
            _logger.LogInformation("answering!");
            await Task.Delay(1000);
            return $"Result at path: {path}";
        }
    }
}
