using Application.Commands;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Custom_Exceptions;

namespace Application.Handlers
{
    public class CreateShortUrlCommandHandler : IRequestHandler<CreateShortUrlCommand, UrlDetailsDto>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IShortCodeGenerator _shortCodeGenerator;
        private readonly ILogger<CreateShortUrlCommandHandler> _logger;

        public CreateShortUrlCommandHandler(IUrlRepository urlRepository, IShortCodeGenerator shortCodeGenerator, ILogger<CreateShortUrlCommandHandler> logger)
        {
            _urlRepository = urlRepository;
            _shortCodeGenerator = shortCodeGenerator;
            _logger = logger;
        }

        public async Task<UrlDetailsDto> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
        {
            string finalShortCode;

            if (!string.IsNullOrWhiteSpace(request.CustomAlias))
            {
                _logger.LogInformation("Custom alias '{CustomAlias}' provided. Validating and checking uniqueness.", request.CustomAlias);

                if (!_shortCodeGenerator.IsValidShortCode(request.CustomAlias))
                {
                    _logger.LogWarning("Provided custom alias '{CustomAlias}' is not valid or has invalid length/characters. Expected length: {ExpectedLength}.", request.CustomAlias, _shortCodeGenerator.ShortCodeLength);
                    throw new ValidationException($"Custom alias '{request.CustomAlias}' is not in a valid format or has incorrect length. Expected length is {_shortCodeGenerator.ShortCodeLength}.");
                }

                if (await _urlRepository.ShortCodeExistsAsync(request.CustomAlias))
                {
                    _logger.LogWarning("Provided custom alias '{CustomAlias}' already exists.", request.CustomAlias);
                    throw new DuplicateAliasException($"Custom alias '{request.CustomAlias}' is already in use.");
                }

                finalShortCode = request.CustomAlias;
                _logger.LogInformation("Using custom alias '{CustomAlias}' as short code.", finalShortCode);
            }
            else
            {
                _logger.LogInformation("No custom alias provided. Generating a random short code.");
                int maxAttempts = 5;
                string generatedCode = string.Empty;

                for (int i = 0; i < maxAttempts; i++)
                {
                    generatedCode = _shortCodeGenerator.GenerateShortCode();
                    bool shortCodeExists = await _urlRepository.ShortCodeExistsAsync(generatedCode);

                    if (!shortCodeExists)
                    {
                        _logger.LogInformation("Generated unique short code '{ShortCode}' on attempt {Attempt}.", generatedCode, i + 1);
                        break;
                    }

                    _logger.LogWarning("Generated short code '{ShortCode}' already exists. Attempt {Attempt} of {MaxAttempts}. Retrying...", generatedCode, i + 1, maxAttempts);

                    if (i == maxAttempts - 1)
                    {
                        _logger.LogError("Failed to generate a unique short code after {MaxAttempts} attempts for originalUrl: {OriginalUrl}.", maxAttempts, request.OriginalUrl);
                        throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts.");
                    }
                }
                finalShortCode = generatedCode;
            }

            var url = new Url(finalShortCode, request.OriginalUrl, request.ExpirationDate);

            await _urlRepository.CreateUrlAsync(url);
            _logger.LogInformation("Successfully created URL entity for '{ShortCode}' with original URL '{OriginalUrl}'.", finalShortCode, request.OriginalUrl);

            return UrlDetailsDto.FromEntity(url);
        }
    }
}
