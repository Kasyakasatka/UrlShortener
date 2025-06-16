using Application.Interfaces;
using Application.Queries;
using Domain.Custom_Exceptions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class RedirectUrlQueryHandler : IRequestHandler<RedirectUrlQuery, string>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IClickAnalyticRepository _clickAnalyticRepository;
        private readonly ILogger<RedirectUrlQueryHandler> _logger;

        public RedirectUrlQueryHandler(IUrlRepository urlRepository, IClickAnalyticRepository clickAnalyticRepository, ILogger<RedirectUrlQueryHandler> logger)
        {
            _urlRepository = urlRepository;
            _clickAnalyticRepository = clickAnalyticRepository;
            _logger = logger;
        }

        public async Task<string> Handle(RedirectUrlQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling redirect for short code: {ShortCode}", request.ShortCode);

            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);

            if (url == null || !url.IsActive || url.IsExpired())
            {
                _logger.LogWarning("URL with short code '{ShortCode}' not found, inactive, or expired.", request.ShortCode);
                throw new NotFoundException(nameof(Url), request.ShortCode);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _clickAnalyticRepository.IncrementClickCounterAsync(request.ShortCode);
                    var analytic = new ClickAnalytic(
                        request.ShortCode,
                        request.UserAgent ?? "unknown",
                        request.IpAddress ?? "unknown"
                    );
                    await _clickAnalyticRepository.AddAsync(analytic);
                    _logger.LogInformation("Click analytics recorded for short code: {ShortCode}", request.ShortCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recording click analytics for short code: {ShortCode}", request.ShortCode);
                }
            });

            _logger.LogInformation("Redirecting short code '{ShortCode}' to original URL: {OriginalUrl}", request.ShortCode, url.OriginalUrl);
            return url.OriginalUrl;
        }
    }
}
