using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class UpdateShortUrlCommand : IRequest
    {
        public string ShortCode { get; set; }
        public string NewOriginalUrl { get; set; }
        public DateTime? NewExpirationDate { get; set; }
    }
}
