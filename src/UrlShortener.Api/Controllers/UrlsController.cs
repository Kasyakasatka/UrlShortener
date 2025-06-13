using Application.Commands;
using Application.DTOs;
using Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Domain.Custom_Exceptions;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class UrlsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UrlsController> _logger;

        public UrlsController(IMediator mediator, ILogger<UrlsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(UrlDetailsDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> CreateShortUrl([FromBody] CreateShortUrlCommand command)
        {
            var result = await _mediator.Send(command);
            _logger.LogInformation("Successfully created short URL for '{OriginalUrl}' with short code '{ShortCode}'.",
                command.OriginalUrl, result.ShortCode);
            return CreatedAtAction(nameof(GetUrlDetails), new { shortCode = result.ShortCode }, result);
        }

        [HttpGet("{shortCode}")]
        [ProducesResponseType(typeof(UrlDetailsDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetUrlDetails(string shortCode)
        {
            var query = new GetUrlDetailsQuery { ShortCode = shortCode };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPut("{shortCode}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UpdateShortUrl(string shortCode, [FromBody] UpdateShortUrlCommand command)
        {
            if (shortCode != command.ShortCode)
            {
                throw new ValidationException("Short code in URL does not match short code in body.");
            }
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpDelete("{shortCode}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> DeleteShortUrl(string shortCode)
        {
            var command = new DeleteShortUrlCommand { ShortCode = shortCode };
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
