using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries
{
    public class RedirectUrlQuery : IRequest<string>
    {
        public string ShortCode { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
    }
}
