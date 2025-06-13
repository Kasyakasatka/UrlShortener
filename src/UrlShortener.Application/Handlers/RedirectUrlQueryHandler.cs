using Application.Interfaces;
using Application.Queries;
using Domain.Custom_Exceptions;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class RedirectUrlQueryHandler : IRequestHandler<RedirectUrlQuery, string>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IClickAnalyticRepository _clickAnalyticRepository;

        public RedirectUrlQueryHandler(IUrlRepository urlRepository, IClickAnalyticRepository clickAnalyticRepository)
        {
            _urlRepository = urlRepository;
            _clickAnalyticRepository = clickAnalyticRepository;
        }

        public async Task<string> Handle(RedirectUrlQuery request, CancellationToken cancellationToken)
        {
            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);

            if (url == null || !url.IsActive || url.IsExpired())
            {
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            await _clickAnalyticRepository.IncrementClickCounterAsync(request.ShortCode);

            var analytic = new ClickAnalytic(request.ShortCode, request.UserAgent, request.IpAddress);
            // !!! ИСПРАВЛЕНИЕ ЗДЕСЬ !!!
            // ИЗМЕНЕНО с AddAnalyticAsync на AddAsync
            await _clickAnalyticRepository.AddAsync(analytic);

            return url.OriginalUrl;
        }
    }
}
