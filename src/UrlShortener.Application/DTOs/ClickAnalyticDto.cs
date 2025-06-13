using System;
using Domain.Entities; // Добавляем, чтобы иметь доступ к ClickAnalytic entity

namespace Application.DTOs
{
    public class ClickAnalyticDto
    {
        public string ShortCode { get; set; }
        public DateTimeOffset ClickTimestamp { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }

        // Конструктор без параметров для десериализации (нужен для некоторых сценариев)
        public ClickAnalyticDto()
        {
            ShortCode = string.Empty; // Инициализируем не-nullable строки
            UserAgent = string.Empty;
            IpAddress = string.Empty;
        }

        // Конструктор для инициализации всех свойств (если понадобится)
        public ClickAnalyticDto(string shortCode, DateTimeOffset clickTimestamp, string userAgent, string ipAddress)
        {
            ShortCode = shortCode;
            ClickTimestamp = clickTimestamp;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }

        // !!! НОВЫЙ СТАТИЧЕСКИЙ МЕТОД: FromEntity !!!
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
