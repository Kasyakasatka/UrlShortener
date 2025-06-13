using Cassandra;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CassandraConnectionManagement
{
    public class CassandraContext
    {
        private readonly Cassandra.ISession _session;
        private readonly ILogger<CassandraContext> _logger;

        public CassandraContext(Cassandra.ISession session, ILogger<CassandraContext> logger)
        {
            _session = session;
            _logger = logger;
        }

        public async Task ApplyMigrations()
        {
            _logger.LogInformation("Применение миграций Cassandra...");

            // --- Таблица для URL-адресов ---
            _logger.LogInformation("Удаление таблицы 'urls', если она существует, для обеспечения актуальной схемы...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS urls;")); // !!! ИСПРАВЛЕНИЕ ОПЕЧАТКИ: EXISs -> EXISTS !!!
            _logger.LogInformation("Таблица 'urls' удалена, если существовала.");

            _logger.LogInformation("Создание или проверка существования таблицы 'urls'...");
            await _session.ExecuteAsync(new SimpleStatement(
                "CREATE TABLE IF NOT EXISTS urls (" +
                "short_code text PRIMARY KEY," + // short_code теперь PRIMARY KEY
                "id uuid," + // id теперь обычный столбец
                "original_url text," +
                "creation_timestamp timestamp," +
                "expiration_date timestamp," +
                "is_active boolean)"));
            _logger.LogInformation("Таблица 'urls' создана или уже существует.");

            // --- Таблица для счетчиков кликов ---
            _logger.LogInformation("Удаление таблицы 'url_clicks', если она существует, для обеспечения актуальной схемы...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS url_clicks;")); // Добавлено для консистентности
            _logger.LogInformation("Таблица 'url_clicks' удалена, если существовала.");

            _logger.LogInformation("Создание или проверка существования таблицы 'url_clicks' для счетчиков кликов...");
            await _session.ExecuteAsync(new SimpleStatement(
                "CREATE TABLE IF NOT EXISTS url_clicks (" +
                "short_code text PRIMARY KEY," +
                "count counter)"));
            _logger.LogInformation("Таблица 'url_clicks' создана или уже существует.");

            // --- Таблица для аналитики кликов ---
            _logger.LogInformation("Удаление таблицы 'click_analytics', если она существует, для обеспечения актуальной схемы...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS click_analytics;")); // Добавлено для консистентности
            _logger.LogInformation("Таблица 'click_analytics' удалена, если существовала.");

            _logger.LogInformation("Создание или проверка существования таблицы 'click_analytics'...");
            await _session.ExecuteAsync(new SimpleStatement(
                 "CREATE TABLE IF NOT EXISTS click_analytics (" +
                 "short_code text," +
                 "click_timestamp timestamp," + // Используем timestamp для DateTimeOffset
                 "ip_address text," +
                 "user_agent text," +
                 "PRIMARY KEY (short_code, click_timestamp)" + // СОСТАВНОЙ ПЕРВИЧНЫЙ КЛЮЧ
                 ") WITH CLUSTERING ORDER BY (click_timestamp DESC);"));
            _logger.LogInformation("Таблица 'click_analytics' создана или уже существует.");
        }
    }
}

    

