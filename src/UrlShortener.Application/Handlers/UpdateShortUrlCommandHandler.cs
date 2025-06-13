using Application.Commands;
using Application.Interfaces;
using Domain.Custom_Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class UpdateShortUrlCommandHandler : IRequestHandler<UpdateShortUrlCommand>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly ILogger<UpdateShortUrlCommandHandler> _logger;

        public UpdateShortUrlCommandHandler(IUrlRepository urlRepository, ILogger<UpdateShortUrlCommandHandler> logger)
        {
            _urlRepository = urlRepository;
            _logger = logger;
        }

        public async Task Handle(UpdateShortUrlCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to update URL with short code: {ShortCode}", request.ShortCode);

            try
            {
                var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);
                if (url == null)
                {
                    _logger.LogWarning("URL with short code '{ShortCode}' not found for update.", request.ShortCode);
                    throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
                }

                if (request.NewExpirationDate.HasValue && request.NewExpirationDate.Value < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Invalid update attempt for short code '{ShortCode}': New expiration date '{NewExpirationDate}' is in the past.", request.ShortCode, request.NewExpirationDate.Value);
                    throw new ValidationException("New expiration date cannot be in the past.");
                }

                if (string.IsNullOrWhiteSpace(request.NewOriginalUrl) || !Uri.TryCreate(request.NewOriginalUrl, UriKind.Absolute, out _))
                {
                    _logger.LogWarning("Invalid update attempt for short code '{ShortCode}': Invalid new original URL format '{NewOriginalUrl}'.", request.ShortCode, request.NewOriginalUrl);
                    throw new ValidationException("Invalid new original URL format.");
                }

                url.Update(request.NewOriginalUrl, request.NewExpirationDate);
                await _urlRepository.UpdateUrlAsync(url);
                _logger.LogInformation("Successfully updated URL with short code: {ShortCode}", request.ShortCode);
            }
            catch (NotFoundException ex)
            {
                throw;
            }
            catch (ValidationException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while updating URL with short code: {ShortCode}", request.ShortCode);
                throw;
            }
        }
    }
}
