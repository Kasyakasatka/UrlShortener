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
        // Переименовано для ясности и соответствия другим методам
        Task<Url> GetUrlByShortCodeAsync(string shortCode);
        // Переименовано для ясности
        Task CreateUrlAsync(Url url);
        // Исправление названия метода
        Task UpdateUrlAsync(Url url);

        // Оставляем эти методы, если они используются в других местах
        Task DeleteUrlAsync(string shortCode);
        Task<bool> ShortCodeExistsAsync(string shortCode);
    }
}
