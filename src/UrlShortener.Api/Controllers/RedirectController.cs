using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Domain.Custom_Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    [Route("/{shortCode}")] 
    public class RedirectController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IMediator mediator, ILogger<RedirectController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.TemporaryRedirect)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
      
        public async Task<IActionResult> HandleRedirect(string shortCode)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var query = new RedirectUrlQuery
            {
                ShortCode = shortCode,
                UserAgent = userAgent,
                IpAddress = ipAddress
            };
            var originalUrl = await _mediator.Send(query);

            return Redirect(originalUrl);
        }
    }
}
