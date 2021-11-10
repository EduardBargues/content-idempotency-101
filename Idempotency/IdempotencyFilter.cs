using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Idempotency
{
    public class IdempotencyFilter : Attribute, IAsyncResourceFilter
    {
        private readonly ILogger<IdempotencyFilter> _logger;
        private readonly IIdempotentStorage _db;
        private readonly string _headerName;
        private readonly TimeSpan _timeToLiveMaster;
        private readonly TimeSpan _timeToLiveDeprecation;

        public IdempotencyFilter(IIdempotentStorage db, IOptions<IdempotencyConfiguration> conf, ILogger<IdempotencyFilter> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _headerName = conf.Value?.HeaderName ?? throw new ArgumentNullException("idempotency header name");
            _timeToLiveMaster = conf.Value?.TimeToLiveMaster ?? throw new ArgumentNullException("idempotency parent TTL");
            _timeToLiveDeprecation = conf.Value?.TimeToLiveDeprecation ?? throw new ArgumentNullException("idempotency deprecation TTL");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var key = context.HttpContext.Request.Headers.TryGetValue(_headerName, out StringValues keys)
                ? keys[0]
                : null;

            if (key == null)
            {
                context.Result = new BadRequestObjectResult($"Missing idempotency key. method: {context.HttpContext.Request.Method} path: {context.HttpContext.Request.Path} header: {_headerName}");
                return;
            }

            var ownerId = context.HttpContext.TraceIdentifier;
            var response = await _db.MasterExecution(key, ownerId, _timeToLiveMaster);
            if (response.Finished)
            {
                context.Result = new ObjectResult(response.Body)
                {
                    StatusCode = response.StatusCode,
                };
            }
            else
            {
                if (response.OwnerId != ownerId)
                {
                    context.Result = new ConflictObjectResult($"Request with idempotency-key: {key} is in progress.");
                }
                else
                {
                    await ProcessContext(context, next, key, ownerId);
                }
            }
        }

        private async Task ProcessContext(ResourceExecutingContext context, ResourceExecutionDelegate next, string key, string ownerId)
        {
            var originalBody = context.HttpContext.Response.Body;
            using (var memStream = new MemoryStream())
            {
                context.HttpContext.Response.Body = memStream;

                var executedContext = await next();

                memStream.Position = 0;
                string responseBody = new StreamReader(memStream).ReadToEnd();

                memStream.Position = 0;
                await memStream.CopyToAsync(originalBody);

                var statusCode = executedContext.HttpContext.Response.StatusCode;
                await _db.FinishExecution(key, ownerId, statusCode, responseBody, _timeToLiveDeprecation);
            }
        }
    }
}