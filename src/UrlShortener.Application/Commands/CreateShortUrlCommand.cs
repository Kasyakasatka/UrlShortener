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
        string? CustomAlias = null, // nullable string для необязательных значений
        DateTimeOffset? ExpirationDate = null // DateTimeOffset? для дат
    ) : IRequest<UrlDetailsDto>; // Указываем, что это команда MediatR, которая возвращает UrlDetailsDto
}
