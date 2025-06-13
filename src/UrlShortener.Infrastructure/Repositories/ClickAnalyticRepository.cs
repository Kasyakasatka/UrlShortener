using Application.Interfaces;
using Cassandra;
using Cassandra.Mapping;
using Domain.Entities;
using Microsoft.Extensions.Logging; // Added for logging
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ClickAnalyticRepository : IClickAnalyticRepository
    {
        private readonly ISession _session;
        private readonly IMapper _mapper;
        private readonly ILogger<ClickAnalyticRepository> _logger; // Added for logging
        private const string KeyspaceName = "url_shortener";

        public ClickAnalyticRepository(ISession session, IMapper mapper, ILogger<ClickAnalyticRepository> logger) // Injected logger
        {
            _session = session;
            _mapper = mapper;
            _logger = logger; // Initialized logger
        }

        public async Task IncrementClickCounterAsync(string shortCode)
        {
            _logger.LogInformation("Attempting to increment click counter for short code: {ShortCode}", shortCode);
            try
            {
                var statement = new SimpleStatement(
                    $"UPDATE {KeyspaceName}.url_clicks SET count = count + 1 WHERE short_code = ?",
                    shortCode);
                await _session.ExecuteAsync(statement);
                _logger.LogInformation("Successfully incremented click counter for short code: {ShortCode}", shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing click counter for short code: {ShortCode}", shortCode);
                throw;
            }
        }

        public async Task AddClickAnalyticAsync(string shortCode, string ipAddress, string userAgent)
        {
            _logger.LogInformation("Attempting to add click analytic for short code: {ShortCode}", shortCode);
            try
            {
                var analytic = new ClickAnalytic(shortCode, userAgent, ipAddress);
                await AddAsync(analytic);
                _logger.LogInformation("Successfully added click analytic for short code: {ShortCode}", shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding click analytic for short code: {ShortCode}", shortCode);
                throw;
            }
        }

        public async Task AddAsync(ClickAnalytic analytic)
        {
            _logger.LogInformation("Inserting click analytic record for short code: {ShortCode}, timestamp: {Timestamp}", analytic.ShortCode, analytic.ClickTimestamp);
            try
            {
                var statement = new SimpleStatement(
                    $"INSERT INTO {KeyspaceName}.click_analytics " +
                    "(short_code, click_timestamp, ip_address, user_agent) " +
                    "VALUES (?, ?, ?, ?)",
                    analytic.ShortCode,
                    analytic.ClickTimestamp,
                    analytic.IpAddress,
                    analytic.UserAgent);

                await _session.ExecuteAsync(statement);
                _logger.LogInformation("Click analytic record inserted successfully for short code: {ShortCode}", analytic.ShortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting click analytic record for short code: {ShortCode}", analytic.ShortCode);
                throw;
            }
        }

        public async Task<IEnumerable<ClickAnalytic>> GetByShortCodeAsync(string shortCode)
        {
            _logger.LogInformation("Retrieving click analytics for short code: {ShortCode}", shortCode);
            try
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
                _logger.LogInformation("Retrieved {Count} click analytic records for short code: {ShortCode}", analytics.Count, shortCode);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving click analytics for short code: {ShortCode}", shortCode);
                throw;
            }
        }

        public async Task<long> GetClickCountAsync(string shortCode)
        {
            _logger.LogInformation("Retrieving click count for short code: {ShortCode}", shortCode);
            try
            {
                var statement = new SimpleStatement(
                    $"SELECT count FROM {KeyspaceName}.url_clicks WHERE short_code = ?",
                    shortCode);
                var row = (await _session.ExecuteAsync(statement)).FirstOrDefault();

                if (row == null)
                {
                    _logger.LogInformation("Click count not found for short code: {ShortCode}. Returning 0.", shortCode);
                    return 0;
                }

                long count = row.GetValue<long>("count");
                _logger.LogInformation("Retrieved click count {Count} for short code: {ShortCode}", count, shortCode);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving click count for short code: {ShortCode}", shortCode);
                throw;
            }
        }
    }
}
