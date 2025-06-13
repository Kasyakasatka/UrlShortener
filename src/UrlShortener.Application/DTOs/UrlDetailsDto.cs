using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.DTOs
{
    public class UrlDetailsDto
    {
       
        public string ShortCode { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public DateTimeOffset CreationTimestamp { get; set; }
        public DateTimeOffset? ExpirationDate { get; set; }
        public long ClickCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public List<ClickAnalyticDto> ClickAnalytics { get; set; } = new List<ClickAnalyticDto>(); 

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
