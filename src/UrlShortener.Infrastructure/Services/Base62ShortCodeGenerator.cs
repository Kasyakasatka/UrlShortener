using Application.Interfaces;
using Microsoft.Extensions.Logging; // Добавлено для логирования
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class Base62ShortCodeGenerator : IShortCodeGenerator
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int Base = 62;
        private readonly Random _random = new Random();
        private const int ShortCodeLength = 7;
        private readonly ILogger<Base62ShortCodeGenerator> _logger; // Добавлено для логирования

        public Base62ShortCodeGenerator(ILogger<Base62ShortCodeGenerator> logger) // Добавлен ILogger в конструктор
        {
            _logger = logger; // Инициализация логгера
        }

        public string GenerateShortCode()
        {
            _logger.LogInformation("Attempting to generate a new short code.");
            long uniqueNumber = GenerateUniqueNumber();

            var sb = new StringBuilder();
            while (uniqueNumber > 0)
            {
                sb.Insert(0, Alphabet[(int)(uniqueNumber % Base)]);
                uniqueNumber /= Base;
            }

            while (sb.Length < ShortCodeLength)
            {
                sb.Insert(0, 'a');
            }

            string generatedCode = sb.ToString().Substring(0, ShortCodeLength);
            _logger.LogInformation("Generated short code: {GeneratedCode}", generatedCode);
            return generatedCode;
        }

        public bool IsValidShortCode(string shortCode)
        {
            _logger.LogInformation("Validating short code: {ShortCode}", shortCode);
            if (string.IsNullOrEmpty(shortCode) || shortCode.Length != ShortCodeLength)
            {
                _logger.LogWarning("Short code '{ShortCode}' is invalid: Null, empty, or incorrect length ({Length}).", shortCode, shortCode?.Length ?? 0);
                return false;
            }
            foreach (char c in shortCode)
            {
                if (!Alphabet.Contains(c))
                {
                    _logger.LogWarning("Short code '{ShortCode}' contains invalid character: '{InvalidChar}'.", shortCode, c);
                    return false;
                }
            }
            _logger.LogInformation("Short code '{ShortCode}' is valid.", shortCode);
            return true;
        }

        private long GenerateUniqueNumber()
        {
            long number = DateTime.UtcNow.Ticks;
            _logger.LogDebug("Generated unique number (based on Ticks): {UniqueNumber}", number);
            return number;
        }
    }
}
