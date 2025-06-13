using Application.DTOs;
using Application.Interfaces;
using Application.Queries;
using Domain.Custom_Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetUrlDetailsQueryHandler : IRequestHandler<GetUrlDetailsQuery, UrlDetailsDto>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IClickAnalyticRepository _clickAnalyticRepository;
        private readonly ILogger<GetUrlDetailsQueryHandler> _logger;

        public GetUrlDetailsQueryHandler(IUrlRepository urlRepository, IClickAnalyticRepository clickAnalyticRepository, ILogger<GetUrlDetailsQueryHandler> logger)
        {
            _urlRepository = urlRepository;
            _clickAnalyticRepository = clickAnalyticRepository;
            _logger = logger;
        }

        public async Task<UrlDetailsDto> Handle(GetUrlDetailsQuery request, CancellationToken cancellationToken)
        {
            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);

            if (url == null || !url.IsActive || url.IsExpired())
            {
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            var analytics = await _clickAnalyticRepository.GetByShortCodeAsync(request.ShortCode);

            long clickCount = 0;
            try
            {
                clickCount = await _clickAnalyticRepository.GetClickCountAsync(request.ShortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not retrieve click count for short code {ShortCode}", request.ShortCode);
            }

            return UrlDetailsDto.FromEntity(url, clickCount, analytics);
        }
    }
}
