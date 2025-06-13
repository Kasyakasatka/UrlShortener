using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class DeleteShortUrlCommand : IRequest
    {
        public string ShortCode { get; set; }
    }
}
