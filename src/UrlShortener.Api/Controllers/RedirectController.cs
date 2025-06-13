using Application.Queries;
using Domain.Custom_Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RedirectController> _logger;

        public RedirectController(IMediator mediator, ILogger<RedirectController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("/{shortCode}")]
        [ProducesResponseType((int)HttpStatusCode.RedirectKeepVerb)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> HandleRedirect(string shortCode)
        {
            try
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
            catch (NotFoundException ex)
            {
                _logger.LogInformation(ex, "Short code not found or expired: {ShortCode}", shortCode);
                return NotFound("The requested URL was not found or has expired.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during redirection for short code: {ShortCode}", shortCode);
                return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred during redirection.");
            }
        }
    }
}
