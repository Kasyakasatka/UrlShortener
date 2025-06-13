using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ClickAnalytic
    {
        public string ShortCode { get; set; }
        public DateTimeOffset ClickTimestamp { get; set; } 
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        public ClickAnalytic(string shortCode, string userAgent, string ipAddress)
        {
            ShortCode = shortCode;
            ClickTimestamp = DateTimeOffset.UtcNow;
            IpAddress = ipAddress ?? "";
            UserAgent = userAgent ?? "";
        }

        public ClickAnalytic() { }
    }
}
