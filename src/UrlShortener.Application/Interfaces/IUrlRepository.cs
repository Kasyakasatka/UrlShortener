using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUrlRepository
    {
      
        Task<Url> GetUrlByShortCodeAsync(string shortCode);
        Task CreateUrlAsync(Url url);
        Task UpdateUrlAsync(Url url);
        Task DeleteUrlAsync(string shortCode);
        Task<bool> ShortCodeExistsAsync(string shortCode);
    }
}
