using Application.Commands;
using Application.DTOs;
using Application.Queries;
using Domain.Custom_Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Api.Controllers
{
    [ApiController]
    
    [Route("api/[controller]s")] // api/urls 
    public class UrlsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UrlsController> _logger; // Add logger

        public UrlsController(IMediator mediator, ILogger<UrlsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new short URL.
        /// </summary>
        /// <param name="command">The command containing original URL, optional expiration date, and optional custom alias.</param> 
                    /// <returns>Details of the created short URL.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(UrlDetailsDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> CreateShortUrl([FromBody] CreateShortUrlCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetUrlDetails), new { shortCode = result.ShortCode }, result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred during URL creation.");
                return BadRequest(ex.Message);
            }
            catch (DuplicateAliasException ex)
            {
                _logger.LogWarning(ex, "Duplicate alias error occurred during URL creation.");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during URL creation.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific short URL. 
                    /// </summary>
                    /// <param name="shortCode">The short code of the URL.</param>
                    /// <returns>Detailed information about the short URL.</returns>
        [HttpGet("{shortCode}")]
        [ProducesResponseType(typeof(UrlDetailsDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUrlDetails(string shortCode)
        {
            try
            {
                var query = new GetUrlDetailsQuery { ShortCode = shortCode };
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation(ex, "URL details not found for short code: {ShortCode}", shortCode);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while retrieving URL details for short code: {ShortCode}", shortCode);
                return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Updates an existing short URL. 
                    /// </summary>
                    /// <param name="shortCode">The short code of the URL to update.</param>
                    /// <param name="command">The command containing the new original URL or expiration date.</param>
                    /// <returns>No content if successful.</returns>
        [HttpPut("{shortCode}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateShortUrl(string shortCode, [FromBody] UpdateShortUrlCommand command)
        {
            try
            {
                if (shortCode != command.ShortCode)
                {
                    return BadRequest("Short code in URL does not match short code in body.");
                }
                await _mediator.Send(command);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation(ex, "URL not found for update: {ShortCode}", shortCode);
                return NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred during URL update.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating URL: {ShortCode}", shortCode);
                return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Deletes a short URL. 
                    /// </summary>
                    /// <param name="shortCode">The short code of the URL to delete.</param>
                    /// <returns>No content if successful.</returns>
        [HttpDelete("{shortCode}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteShortUrl(string shortCode)
        {
            try
            {
                var command = new DeleteShortUrlCommand { ShortCode = shortCode };
                await _mediator.Send(command);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogInformation(ex, "URL not found for deletion: {ShortCode}", shortCode);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while deleting URL: {ShortCode}", shortCode);
                return StatusCode((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }
    }
}
