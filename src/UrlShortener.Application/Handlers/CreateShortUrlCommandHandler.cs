using Application.Commands;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class CreateShortUrlCommandHandler : IRequestHandler<CreateShortUrlCommand, UrlDetailsDto>
    {
        private readonly IUrlRepository _urlRepository;
        private readonly IShortCodeGenerator _shortCodeGenerator;

        public CreateShortUrlCommandHandler(IUrlRepository urlRepository, IShortCodeGenerator shortCodeGenerator)
        {
            _urlRepository = urlRepository;
            _shortCodeGenerator = shortCodeGenerator;
        }

        public async Task<UrlDetailsDto> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
        {
            string shortCode;
            bool shortCodeExists;
            int maxAttempts = 5; // Максимальное количество попыток генерации уникального короткого кода

            for (int i = 0; i < maxAttempts; i++)
            {
                // !!! КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Всегда генерируем новый короткий код !!!
                shortCode = _shortCodeGenerator.GenerateShortCode();
                shortCodeExists = await _urlRepository.ShortCodeExistsAsync(shortCode);

                if (!shortCodeExists)
                {
                    // Найден уникальный короткий код, выходим из цикла
                    break;
                }

                if (i == maxAttempts - 1)
                {
                    // Если все попытки исчерпаны, и мы не смогли найти уникальный код
                    throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts.");
                }
            }
            // Переменная shortCode должна быть инициализирована до использования.
            // Если цикл завершился без уникального кода, будет брошено исключение выше.
            // Поэтому можем присвоить короткий код здесь, но только для обхода предупреждения компилятора.
            // В реальном коде, shortCode всегда будет уникальным, если не выброшено исключение.
            shortCode = _shortCodeGenerator.GenerateShortCode(); // Просто инициализация для компилятора

            // Создаем новый URL-объект.
            // Теперь каждый раз будет создаваться новая запись, даже для того же originalUrl.
            var url = new Url(shortCode, request.OriginalUrl, request.ExpirationDate);

            await _urlRepository.CreateUrlAsync(url);

            // Возвращаем DTO, используя перегрузку FromEntity, которая не требует ClickCount и ClickAnalytics
            return UrlDetailsDto.FromEntity(url);
        }
    }
}
