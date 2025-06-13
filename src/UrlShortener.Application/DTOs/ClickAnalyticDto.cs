using System;
using Domain.Entities;

namespace Application.DTOs
{
    public record ClickAnalyticDto
    {
        public string ShortCode { get; init; }
        public DateTimeOffset ClickTimestamp { get; init; }
        public string UserAgent { get; init; }
        public string IpAddress { get; init; }

        public ClickAnalyticDto()
        {
            ShortCode = string.Empty;
            UserAgent = string.Empty;
            IpAddress = string.Empty;
        }
        public ClickAnalyticDto(string shortCode, DateTimeOffset clickTimestamp, string userAgent, string ipAddress)
        {
            ShortCode = shortCode;
            ClickTimestamp = clickTimestamp;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }

        public static ClickAnalyticDto FromEntity(ClickAnalytic entity)
        {
            return new ClickAnalyticDto
            {
                ShortCode = entity.ShortCode,
                ClickTimestamp = entity.ClickTimestamp,
                UserAgent = entity.UserAgent,
                IpAddress = entity.IpAddress
            };
        }
    }
}
