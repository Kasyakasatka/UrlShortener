using System;

namespace Domain.Entities
{
    public class Url
    {
        public Guid? Id { get; set; }
        public string ShortCode { get; set; }
        public string OriginalUrl { get; set; }
        public DateTimeOffset CreationTimestamp { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public bool IsActive { get; set; }

        public Url()
        {
            // Этот конструктор нужен для Cassandra.Mapping и инициализаторов объектов.
        }

        public Url(string shortCode, string originalUrl, DateTimeOffset? expirationDate)
        {
            Id = Guid.NewGuid();
            ShortCode = shortCode;
            OriginalUrl = originalUrl;
            CreationTimestamp = DateTimeOffset.UtcNow;
            ExpirationDate = expirationDate?.ToUniversalTime();
            IsActive = true;
        }

        // !!! ВОЗВРАЩЕН МЕТОД UPDATE !!!
        public void Update(string newOriginalUrl, DateTimeOffset? newExpirationDate)
        {
            if (!string.IsNullOrWhiteSpace(newOriginalUrl))
            {
                OriginalUrl = newOriginalUrl;
            }
            // Обновляем ExpirationDate, если оно предоставлено
            if (newExpirationDate.HasValue)
            {
                ExpirationDate = newExpirationDate.Value;
                // Если новая дата в будущем, делаем URL активным
                if (newExpirationDate.Value > DateTimeOffset.UtcNow)
                {
                    IsActive = true;
                }
            }
            // Если newExpirationDate = null, мы не меняем текущую ExpirationDate
            // и не меняем IsActive на основе ExpirationDate.
        }

        public bool IsExpired()
        {
            return ExpirationDate.HasValue && ExpirationDate.Value <= DateTimeOffset.UtcNow;
        }
    }
}
