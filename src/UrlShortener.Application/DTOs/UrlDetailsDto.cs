using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.DTOs
{
    public record UrlDetailsDto
    {
       
        public string ShortCode { get; init; } = string.Empty;
        public string OriginalUrl { get; init; } = string.Empty;
        public DateTimeOffset CreationTimestamp { get; init; }
        public DateTimeOffset? ExpirationDate { get; init; }
        public long ClickCount { get; init; }
        public bool IsActive { get; init; }
        public bool IsExpired { get; init; }
        public List<ClickAnalyticDto> ClickAnalytics { get; init; } = new List<ClickAnalyticDto>(); 

        public UrlDetailsDto() { }

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
                IsExpired = url.IsExpired(), 
                
                ClickAnalytics = analytics?.Select(ClickAnalyticDto.FromEntity).ToList() ?? new List<ClickAnalyticDto>()
            };
        }

        public static UrlDetailsDto FromEntity(Url url)
        {
            return new UrlDetailsDto
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                CreationTimestamp = url.CreationTimestamp,
                ExpirationDate = url.ExpirationDate,
                ClickCount = 0,
                IsActive = url.IsActive,
                IsExpired = url.IsExpired(),
                ClickAnalytics = new List<ClickAnalyticDto>()
            };
        }
    }
}
