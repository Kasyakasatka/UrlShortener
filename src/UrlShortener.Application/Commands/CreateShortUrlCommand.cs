using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{

    public record CreateShortUrlCommand(
        string OriginalUrl,
        string? CustomAlias = null,
        DateTimeOffset? ExpirationDate = null
    ) : IRequest<UrlDetailsDto>;
}
