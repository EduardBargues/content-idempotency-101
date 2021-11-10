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
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly IImplementation _service;

        public MainController(ILogger<MainController> logger, IImplementation service)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [ServiceFilter(typeof(IdempotencyFilter))]
        public async Task<IActionResult> Get()
        {
            var result = await _service.DoAsync(HttpContext.Request.Path);

            var response = result == "" ? NotFound() : (IActionResult)Ok(new { dependencyMessage = result });

            return response;
        }
    }
}
