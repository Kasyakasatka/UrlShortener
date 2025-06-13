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
    public class DeleteShortUrlCommandHandler : IRequestHandler<DeleteShortUrlCommand>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly ILogger<DeleteShortUrlCommandHandler> _logger;

        public DeleteShortUrlCommandHandler(IUrlRepository urlRepository, ILogger<DeleteShortUrlCommandHandler> logger)
        {
            _urlRepository = urlRepository;
            _logger = logger;
        }

        public async Task Handle(DeleteShortUrlCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to delete URL with short code: {ShortCode}", request.ShortCode);

            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);
            if (url == null)
            {
                _logger.LogWarning("URL with short code '{ShortCode}' not found for deletion.", request.ShortCode);
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            await _urlRepository.DeleteUrlAsync(request.ShortCode);
            _logger.LogInformation("Successfully deleted URL with short code: {ShortCode}", request.ShortCode);
        }
    }
}
