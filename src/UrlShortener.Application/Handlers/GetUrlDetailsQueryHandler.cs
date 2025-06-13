using Application.DTOs;
using Application.Interfaces;
using Application.Queries;
using Domain.Custom_Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetUrlDetailsQueryHandler : IRequestHandler<GetUrlDetailsQuery, UrlDetailsDto>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IClickAnalyticRepository _clickAnalyticRepository;

        public GetUrlDetailsQueryHandler(IUrlRepository urlRepository, IClickAnalyticRepository clickAnalyticRepository)
        {
            _urlRepository = urlRepository;
            _clickAnalyticRepository = clickAnalyticRepository;
        }

        public async Task<UrlDetailsDto> Handle(GetUrlDetailsQuery request, CancellationToken cancellationToken)
        {
            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);

            // Если URL не найден, неактивен или истёк, выбрасываем NotFoundException.
            if (url == null || !url.IsActive || url.IsExpired())
            {
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            // !!! ВАЖНО: Получаем детальные записи кликов !!!
            var analytics = await _clickAnalyticRepository.GetByShortCodeAsync(request.ShortCode);

            // !!! ВАЖНО: Получаем общий счетчик кликов из репозитория !!!
            long clickCount = 0;
            try
            {
                // Предполагается, что метод GetClickCountAsync существует в IClickAnalyticRepository
                clickCount = await _clickAnalyticRepository.GetClickCountAsync(request.ShortCode);
            }
            catch (Exception)
            {
                // Можно логировать ошибку, если счетчик не найден
                // _logger.LogError(ex, "Could not retrieve click count for short code {ShortCode}", request.ShortCode);
            }

            // !!! ВАЖНО: Передаём ВСЕ ТРИ обязательных аргумента в FromEntity !!!
            return UrlDetailsDto.FromEntity(url, clickCount, analytics);
        }
    }
}
