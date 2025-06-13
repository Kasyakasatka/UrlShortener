using System;
using Domain.Entities;

namespace Application.DTOs
{
    public class ClickAnalyticDto
    {
        public string ShortCode { get; set; }
        public DateTimeOffset ClickTimestamp { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }

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
