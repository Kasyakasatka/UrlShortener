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
        public void Update(string newOriginalUrl, DateTimeOffset? newExpirationDate)
        {
            if (!string.IsNullOrWhiteSpace(newOriginalUrl))
            {
                OriginalUrl = newOriginalUrl;
            }
          
            if (newExpirationDate.HasValue)
            {
                ExpirationDate = newExpirationDate.Value;
             
                if (newExpirationDate.Value > DateTimeOffset.UtcNow)
                {
                    IsActive = true;
                }
            }
           
        }

        public bool IsExpired()
        {
            return ExpirationDate.HasValue && ExpirationDate.Value <= DateTimeOffset.UtcNow;
        }
    }
}
