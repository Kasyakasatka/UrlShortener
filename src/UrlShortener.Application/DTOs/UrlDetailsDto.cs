using Domain.Entities; // Убедитесь, что это using присутствует
using System;
using System.Collections.Generic;
using System.Linq; // Добавлено для LINQ-методов

namespace Application.DTOs
{
    public class UrlDetailsDto
    {
        // Инициализируем не-nullable свойства, чтобы избежать CS8618 warnings
        public string ShortCode { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public DateTimeOffset CreationTimestamp { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public long ClickCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public List<ClickAnalyticDto> ClickAnalytics { get; set; } = new List<ClickAnalyticDto>(); // Инициализация списка

        // Конструктор без параметров: требуется для десериализации (например, из JSON).
        public UrlDetailsDto() { }

        // ЭТА ВЕРСИЯ FromEntity используется для получения ДЕТАЛЕЙ (например, в GetUrlDetailsQueryHandler)
        public static UrlDetailsDto FromEntity(Url url, long clickCount, IEnumerable<ClickAnalytic> analytics)
        {
            return new UrlDetailsDto
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                CreationTimestamp = url.CreationTimestamp,
                ExpirationDate = url.ExpirationDate,
                ClickCount = clickCount,
                IsActive = url.IsActive,
                IsExpired = url.IsExpired(), // Вызываем метод IsExpired() на сущности URL
                // Преобразуем IEnumerable<ClickAnalytic> в List<ClickAnalyticDto>
                // Учитываем, что analytics может быть null
                ClickAnalytics = analytics?.Select(ClickAnalyticDto.FromEntity).ToList() ?? new List<ClickAnalyticDto>()
            };
        }

        // ЭТА НОВАЯ ПЕРЕГРУЗКА FromEntity ИСПОЛЬЗУЕТСЯ ПРИ СОЗДАНИИ (в CreateShortUrlCommandHandler)
        public static UrlDetailsDto FromEntity(Url url)
        {
            return new UrlDetailsDto
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                CreationTimestamp = url.CreationTimestamp,
                ExpirationDate = url.ExpirationDate,
                ClickCount = 0, // При создании URL счетчик кликов равен 0
                IsActive = url.IsActive,
                IsExpired = url.IsExpired(), // Вызываем метод IsExpired() на сущности URL
                ClickAnalytics = new List<ClickAnalyticDto>() // При создании URL аналитика кликов пуста
            };
        }
    }
}
