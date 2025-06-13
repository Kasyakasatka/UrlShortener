using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IClickAnalyticRepository
    {
        Task IncrementClickCounterAsync(string shortCode);
        Task AddClickAnalyticAsync(string shortCode, string ipAddress, string userAgent);
        Task AddAsync(ClickAnalytic analytic);
        Task<IEnumerable<ClickAnalytic>> GetByShortCodeAsync(string shortCode);
        Task<long> GetClickCountAsync(string shortCode);
    }
}
