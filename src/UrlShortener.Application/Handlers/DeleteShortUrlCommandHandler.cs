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
    public class DeleteShortUrlCommandHandler : IRequestHandler<DeleteShortUrlCommand>
    {
        private readonly IUrlRepository _urlRepository;

        public DeleteShortUrlCommandHandler(IUrlRepository urlRepository)
        {
            _urlRepository = urlRepository;
        }

        public async Task Handle(DeleteShortUrlCommand request, CancellationToken cancellationToken)
        {
            // !!! ИСПРАВЛЕНИЕ ЗДЕСЬ !!!
            // Изменено с GetByShortCodeAsync на GetUrlByShortCodeAsync, чтобы соответствовать IUrlRepository
            var url = await _urlRepository.GetUrlByShortCodeAsync(request.ShortCode);
            if (url == null)
            {
                throw new NotFoundException(nameof(Domain.Entities.Url), request.ShortCode);
            }

            await _urlRepository.DeleteUrlAsync(request.ShortCode);
        }
    }
}
