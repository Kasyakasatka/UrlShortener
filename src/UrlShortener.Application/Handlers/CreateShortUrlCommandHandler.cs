using Application.Commands;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class CreateShortUrlCommandHandler : IRequestHandler<CreateShortUrlCommand, UrlDetailsDto>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IShortCodeGenerator _shortCodeGenerator;
        private readonly ILogger<CreateShortUrlCommandHandler> _logger;

        public CreateShortUrlCommandHandler(IUrlRepository urlRepository, IShortCodeGenerator shortCodeGenerator, ILogger<CreateShortUrlCommandHandler> logger) // Добавлен ILogger в конструктор
        {
            _urlRepository = urlRepository;
            _shortCodeGenerator = shortCodeGenerator;
            _logger = logger;
        }

        public async Task<UrlDetailsDto> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
        {
            string shortCode = string.Empty;
            bool shortCodeExists;
            int maxAttempts = 5;

            _logger.LogInformation("Attempting to create a short URL for originalUrl: {OriginalUrl}", request.OriginalUrl);

            for (int i = 0; i < maxAttempts; i++)
            {
                shortCode = _shortCodeGenerator.GenerateShortCode();
                shortCodeExists = await _urlRepository.ShortCodeExistsAsync(shortCode);

                if (!shortCodeExists)
                {
                    _logger.LogInformation("Generated unique short code '{ShortCode}' on attempt {Attempt}.", shortCode, i + 1);
                    break;
                }

                _logger.LogWarning("Generated short code '{ShortCode}' already exists. Attempt {Attempt} of {MaxAttempts}. Retrying...", shortCode, i + 1, maxAttempts);

                if (i == maxAttempts - 1)
                {
                    _logger.LogError("Failed to generate a unique short code after {MaxAttempts} attempts for originalUrl: {OriginalUrl}.", maxAttempts, request.OriginalUrl);
                    throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts.");
                }
            }

            var url = new Url(shortCode, request.OriginalUrl, request.ExpirationDate);

            await _urlRepository.CreateUrlAsync(url);
            _logger.LogInformation("Successfully created URL entity for '{ShortCode}' with original URL '{OriginalUrl}'.", shortCode, request.OriginalUrl);

            return UrlDetailsDto.FromEntity(url);
        }
    }
}
