using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{

    public record CreateShortUrlCommand : IRequest<UrlDetailsDto>
    {
        [Required(ErrorMessage = "Original URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string OriginalUrl { get; init; }

        public DateTimeOffset? ExpirationDate { get; init; }

        [MinLength(7, ErrorMessage = "Custom alias must be exactly 7 characters long.")]
        [MaxLength(7, ErrorMessage = "Custom alias must be exactly 7 characters long.")]
        public string? CustomAlias { get; init; }
    }
}