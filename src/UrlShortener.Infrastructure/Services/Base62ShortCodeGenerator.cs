using Application.Interfaces;
using Microsoft.Extensions.Logging; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class Base62ShortCodeGenerator : IShortCodeGenerator
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const int Base = 62;
        private const int FixedShortCodeLength = 7; 
        private readonly Random _random = new Random(); 
        private readonly ILogger<Base62ShortCodeGenerator> _logger;

        public int ShortCodeLength => FixedShortCodeLength;

        public Base62ShortCodeGenerator(ILogger<Base62ShortCodeGenerator> logger)
        {
            _logger = logger;
        }

        public string GenerateShortCode()
        {
            _logger.LogInformation("Attempting to generate a new random fixed-length short code.");
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < FixedShortCodeLength; i++)
            {
                sb.Append(Alphabet[_random.Next(Base)]); 
            }
            string generatedCode = sb.ToString();
            _logger.LogInformation("Generated random short code: {GeneratedCode}", generatedCode);
            return generatedCode;
        }

        public bool IsValidShortCode(string shortCode)
        {
            _logger.LogInformation("Validating short code: {ShortCode}", shortCode);
            if (string.IsNullOrEmpty(shortCode) || shortCode.Length != FixedShortCodeLength) 
            {
                _logger.LogWarning("Short code '{ShortCode}' is invalid: Null, empty, or incorrect length ({Length}). Expected {ExpectedLength}.", shortCode, shortCode?.Length ?? 0, FixedShortCodeLength);
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

    }
}
