using System;
using System.Threading.Tasks;
using Idempotency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Service;

namespace Api
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ILogger<TransactionsController> _logger;
        private readonly IImplementation _service;

        public TransactionsController(ILogger<TransactionsController> logger, IImplementation service)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpPost]
        [ServiceFilter(typeof(IdempotencyFilter))]
        public async Task<IActionResult> Post([FromBody] Transaction transaction)
        {
            var transactionId = await _service.ProcessAsync(transaction);
            var response = transactionId == null || transactionId == Guid.Empty
                ? UnprocessableEntity()
                : (IActionResult)Ok(new { transactionId = transactionId });
            return response;
        }
    }
}
