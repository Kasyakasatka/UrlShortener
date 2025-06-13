// src/UrlShortener.Infrastructure/Repositories/ClickAnalyticRepository.cs
using Application.Interfaces;
using Cassandra; // Для ISession, SimpleStatement, RowSet, Row
using Cassandra.Mapping; // IMapper все еще нужен для ClickAnalyticRepository (если его используют другие методы)
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq; // Для FirstOrDefault()
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ClickAnalyticRepository : IClickAnalyticRepository
    {
        private readonly ISession _session;
        private readonly IMapper _mapper; // Оставляем, если другие операции (например, Update/Delete с маппером) используют
        private const string KeyspaceName = "url_shortener";

        public ClickAnalyticRepository(ISession session, IMapper mapper)
        {
            _session = session;
            _mapper = mapper;
        }

        public async Task IncrementClickCounterAsync(string shortCode)
        {
            var statement = new SimpleStatement(
                $"UPDATE {KeyspaceName}.url_clicks SET count = count + 1 WHERE short_code = ?",
                shortCode);
            await _session.ExecuteAsync(statement);
        }

        public async Task AddClickAnalyticAsync(string shortCode, string ipAddress, string userAgent)
        {
            var analytic = new ClickAnalytic(shortCode, userAgent, ipAddress);
            await AddAsync(analytic);
        }

        public async Task AddAsync(ClickAnalytic analytic)
        {
            // !!! КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: ПРЯМАЯ ВСТАВКА ЧЕРЕЗ SIMPLESTATEMENT !!!
            // Это обходит все проблемы с IMapper и Execution Profiles.
            var statement = new SimpleStatement(
                $"INSERT INTO {KeyspaceName}.click_analytics " +
                "(short_code, click_timestamp, ip_address, user_agent) " +
                "VALUES (?, ?, ?, ?)",
                analytic.ShortCode,
                analytic.ClickTimestamp,
                analytic.IpAddress,
                analytic.UserAgent);

            await _session.ExecuteAsync(statement);
        }

        public async Task<IEnumerable<ClickAnalytic>> GetByShortCodeAsync(string shortCode)
        {
            var analytics = new List<ClickAnalytic>();
            var statement = new SimpleStatement(
                $"SELECT short_code, click_timestamp, ip_address, user_agent " +
                $"FROM {KeyspaceName}.click_analytics WHERE short_code = ?",
                shortCode);

            var rowSet = await _session.ExecuteAsync(statement);

            foreach (var row in rowSet)
            {
                analytics.Add(new ClickAnalytic
                {
                    ShortCode = row.GetValue<string>("short_code"),
                    ClickTimestamp = row.GetValue<DateTimeOffset>("click_timestamp"),
                    IpAddress = row.GetValue<string>("ip_address"),
                    UserAgent = row.GetValue<string>("user_agent")
                });
            }
            return analytics;
        }

        public async Task<long> GetClickCountAsync(string shortCode)
        {
            var statement = new SimpleStatement(
                $"SELECT count FROM {KeyspaceName}.url_clicks WHERE short_code = ?",
                shortCode);
            var row = (await _session.ExecuteAsync(statement)).FirstOrDefault();

            if (row == null)
            {
                return 0;
            }

            return row.GetValue<long>("count");
        }
    }
}
