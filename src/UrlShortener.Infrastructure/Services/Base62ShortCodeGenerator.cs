using Application.Interfaces;
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
        private const int ShortCodeLength = 7; // Recommended length 

        public string GenerateShortCode()
        {
            // For a real application, you might use a more robust unique identifier
            // like a distributed counter or a GUID-based approach to ensure uniqueness
            // across distributed systems. For simplicity, we'll use a random approach here.
            // A deterministic approach using an auto-incrementing counter or timestamp-based method
            //[cite_start]// then encoding using Base62 conversion is recommended 
            // We'll simulate this with a random number for demonstration.

            long uniqueNumber = GenerateUniqueNumber(); // Simulate unique number

            var sb = new StringBuilder();
            while (uniqueNumber > 0)
            {
                sb.Insert(0, Alphabet[(int)(uniqueNumber % Base)]);
                uniqueNumber /= Base;
            }

            // Pad with 'a' if too short, or truncate if too long (though with 7 chars it's unlikely to be too long with this approach)
            while (sb.Length < ShortCodeLength)
            {
                sb.Insert(0, 'a');
            }

            //[cite_start]// Ensure generated codes meet length constraints 
            return sb.ToString().Substring(0, ShortCodeLength);
        }

        public bool IsValidShortCode(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode) || shortCode.Length != ShortCodeLength)
            {
                return false;
            }
            foreach (char c in shortCode)
            {
                if (!Alphabet.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        private long GenerateUniqueNumber()
        {
            // In a production system, this would be a real unique ID, e.g., from a sequence generator,
            // or based on a timestamp to avoid collisions in a distributed environment.
            // For demonstration, we'll use a simple time-based approach for variety, though
            // a global unique counter is ideal for this.
            return DateTime.UtcNow.Ticks;
        }
    }
}
