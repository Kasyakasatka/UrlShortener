using Application.Interfaces;
using Cassandra; // Для ISession, SimpleStatement, RowSet, Row
using Cassandra.Mapping; // Для IMapper
using Domain.Entities;
using System; // Для Guid?
using System.Linq; // Для .Any() и .FirstOrDefault()
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UrlRepository : IUrlRepository
    {
        private readonly ISession _session;
        private readonly IMapper _mapper; // IMapper все еще используется для UpdateAsync и DeleteUrlAsync
        private const string KeyspaceName = "url_shortener"; // Определяем имя keyspace как константу

        public UrlRepository(ISession session, IMapper mapper)
        {
            _session = session;
            _mapper = mapper;
        }

        public async Task CreateUrlAsync(Url url)
        {
            // ЯВНАЯ ВСТАВКА ДЛЯ ГАРАНТИИ, ЧТО ID СОХРАНЯЕТСЯ
            var statement = new SimpleStatement(
                $"INSERT INTO {KeyspaceName}.urls " +
                "(short_code, id, original_url, creation_timestamp, expiration_date, is_active) " + // Явно указываем id
                "VALUES (?, ?, ?, ?, ?, ?)",
                url.ShortCode,
                url.Id, // Используем nullable Id (который всегда будет Guid.NewGuid() при создании)
                url.OriginalUrl,
                url.CreationTimestamp,
                url.ExpirationDate,
                url.IsActive);

            await _session.ExecuteAsync(statement);
        }

        public async Task<Url> GetUrlByShortCodeAsync(string shortCode)
        {
            // !!! КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: РУЧНОЕ СОПОСТАВЛЕНИЕ ДЛЯ НАДЕЖНОСТИ !!!
            // Это обходит проблемы с автоматической десериализацией Guid? из NULL.
            var statement = new SimpleStatement(
                $"SELECT id, short_code, original_url, creation_timestamp, expiration_date, is_active " +
                $"FROM {KeyspaceName}.urls WHERE short_code = ?", shortCode);

            // 1. АВТИТИЧЕСКИ ЖДЕМ ЗАВЕРШЕНИЯ Task<RowSet>
            var rowSet = await _session.ExecuteAsync(statement);
            // 2. Затем получаем ПЕРВУЮ строку ИЗ RowSet
            var row = rowSet.FirstOrDefault();

            if (row == null)
            {
                return null; // Если строка не найдена, возвращаем null
            }

            // 3. Ручное сопоставление столбцов с свойствами сущности Url
            return new Url
            {
                Id = row.GetValue<Guid?>("id"), // Использование GetValue<Guid?> для безопасной обработки null
                ShortCode = row.GetValue<string>("short_code"),
                OriginalUrl = row.GetValue<string>("original_url"),
                CreationTimestamp = row.GetValue<DateTimeOffset>("creation_timestamp"),
                ExpirationDate = row.GetValue<DateTimeOffset?>("expiration_date"), // Использование GetValue<DateTimeOffset?> для обработки null
                IsActive = row.GetValue<bool>("is_active")
            };
        }

        public async Task<bool> ShortCodeExistsAsync(string shortCode)
        {
            var query = new SimpleStatement($"SELECT short_code FROM {KeyspaceName}.urls WHERE short_code = ?", shortCode);
            var rowSet = await _session.ExecuteAsync(query);
            return rowSet.Any();
        }

        public async Task UpdateUrlAsync(Url url)
        {
            // !!! ИСПРАВЛЕНО: Явно указываем KeyspaceName для UpdateAsync !!!
            await _mapper.UpdateAsync(url, KeyspaceName);
        }

        public async Task DeleteUrlAsync(string shortCode) // !!! ИСПРАВЛЕНО: имя метода на DeleteUrlAsync !!!
        {
            // !!! ИСПРАВЛЕНО: Явно указываем KeyspaceName для DeleteAsync !!!
            await _mapper.DeleteAsync<Url>($"WHERE short_code = ?", shortCode, KeyspaceName);
        }
    }
}
