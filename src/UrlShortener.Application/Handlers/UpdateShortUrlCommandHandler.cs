using Application.Commands;
using Application.Interfaces;
using Domain.Custom_Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class UpdateShortUrlCommandHandler : IRequestHandler<UpdateShortUrlCommand>
    {
        private readonly IUrlRepository _urlRepository;

        public UpdateShortUrlCommandHandler(IUrlRepository urlRepository)
        {
            _urlRepository = urlRepository;
        }

        public async Task Handle(UpdateShortUrlCommand request, CancellationToken cancellationToken)
        {
            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);
            if (url == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            //[cite_start]// Validation for expiration date 
            if (request.NewExpirationDate.HasValue && request.NewExpirationDate.Value < DateTime.UtcNow)
            {
                throw new ValidationException("New expiration date cannot be in the past.");
            }
           // [cite_start]// Validation for new original URL format 
            if (!Uri.TryCreate(request.NewOriginalUrl, UriKind.Absolute, out _))
            {
                throw new ValidationException("Invalid new original URL format.");
            }

            url.Update(request.NewOriginalUrl, request.NewExpirationDate);
            await _urlRepository.UpdateUrlAsync(url);
        }
    }
}
