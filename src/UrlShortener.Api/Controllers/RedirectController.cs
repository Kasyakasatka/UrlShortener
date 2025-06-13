using Application.Queries;
using Domain.Custom_Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc; // Для ControllerBase, HttpGet, ProducesResponseType, IActionResult
using Microsoft.Extensions.Logging; // Для ILogger
using System.Net; // Для HttpStatusCode
using System.Threading.Tasks; // Для Task

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

        /// <summary>
        /// Redirects from a short code to the original URL.
        /// </summary>
        /// <param name="shortCode">The short code to redirect from.</param>
        /// <returns>A 302 redirect response to the original URL or 404 if not found/expired.</returns>
        [HttpGet("/{shortCode}")] // Маршрут остается таким же
        [ProducesResponseType((int)HttpStatusCode.RedirectKeepVerb)] // HTTP 302
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        // !!! ИЗМЕНЕНИЕ ЗДЕСЬ: Переименован метод Redirect в HandleRedirect !!!
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

                // Используем базовый метод Redirect из ControllerBase
                // Теперь нет конфликта имен.
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
