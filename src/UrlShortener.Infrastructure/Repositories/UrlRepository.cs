using Application.Interfaces;
using Cassandra;
using Cassandra.Mapping;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UrlRepository : IUrlRepository
    {
        private readonly ISession _session;
        private readonly IMapper _mapper;
        private readonly ILogger<UrlRepository> _logger;
        private const string KeyspaceName = "url_shortener";

        public UrlRepository(ISession session, IMapper mapper, ILogger<UrlRepository> logger)
        {
            _session = session;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task CreateUrlAsync(Url url)
        {
            _logger.LogInformation("Attempting to create URL for short code: {ShortCode}, original URL: {OriginalUrl}", url.ShortCode, url.OriginalUrl);
            try
            {
                var statement = new SimpleStatement(
                    $"INSERT INTO {KeyspaceName}.urls " +
                    "(short_code, id, original_url, creation_timestamp, expiration_date, is_active) " +
                    "VALUES (?, ?, ?, ?, ?, ?)",
                    url.ShortCode,
                    url.Id,
                    url.OriginalUrl,
                    url.CreationTimestamp,
                    url.ExpirationDate,
                    url.IsActive);

                await _session.ExecuteAsync(statement);
                _logger.LogInformation("Successfully created URL with short code: {ShortCode}", url.ShortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating URL for short code: {ShortCode}", url.ShortCode);
                throw;
            }
        }

        public async Task<Url> GetUrlByShortCodeAsync(string shortCode)
        {
            _logger.LogInformation("Retrieving URL by short code: {ShortCode}", shortCode);
            try
            {
                var statement = new SimpleStatement(
                    $"SELECT id, short_code, original_url, creation_timestamp, expiration_date, is_active " +
                    $"FROM {KeyspaceName}.urls WHERE short_code = ?", shortCode);

                var rowSet = await _session.ExecuteAsync(statement);
                var row = rowSet.FirstOrDefault();

                if (row == null)
                {
                    _logger.LogInformation("URL with short code '{ShortCode}' not found.", shortCode);
                    return null;
                }

                var url = new Url
                {
                    Id = row.GetValue<Guid?>("id"),
                    ShortCode = row.GetValue<string>("short_code"),
                    OriginalUrl = row.GetValue<string>("original_url"),
                    CreationTimestamp = row.GetValue<DateTimeOffset>("creation_timestamp"),
                    ExpirationDate = row.GetValue<DateTimeOffset?>("expiration_date"),
                    IsActive = row.GetValue<bool>("is_active")
                };
                _logger.LogInformation("Successfully retrieved URL for short code: {ShortCode}", shortCode);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving URL by short code: {ShortCode}", shortCode);
                throw;
            }
        }

        public async Task<bool> ShortCodeExistsAsync(string shortCode)
        {
            _logger.LogInformation("Checking if short code '{ShortCode}' exists.", shortCode);
            try
            {
                var query = new SimpleStatement($"SELECT short_code FROM {KeyspaceName}.urls WHERE short_code = ?", shortCode);
                var rowSet = await _session.ExecuteAsync(query);
                bool exists = rowSet.Any();
                _logger.LogInformation("Short code '{ShortCode}' exists: {Exists}", shortCode, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if short code '{ShortCode}' exists.", shortCode);
                throw;
            }
        }

        public async Task UpdateUrlAsync(Url url)
        {
            _logger.LogInformation("Attempting to update URL for short code: {ShortCode}", url.ShortCode);
            try
            {
                var statement = new SimpleStatement(
                    $"UPDATE {KeyspaceName}.urls SET " +
                    "original_url = ?, expiration_date = ?, is_active = ? " +
                    "WHERE short_code = ?",
                    url.OriginalUrl,
                    url.ExpirationDate,
                    url.IsActive,
                    url.ShortCode 
                );

                await _session.ExecuteAsync(statement);
                _logger.LogInformation("Successfully updated URL for short code: {ShortCode}", url.ShortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating URL for short code: {ShortCode}", url.ShortCode);
                throw;
            }
        }

        public async Task DeleteUrlAsync(string shortCode)
        {
            _logger.LogInformation("Attempting to delete URL with short code: {ShortCode}", shortCode);
            try
            {
                await _mapper.DeleteAsync<Url>("WHERE short_code = ?", shortCode);
                _logger.LogInformation("Successfully deleted URL with short code: {ShortCode}", shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting URL with short code: {ShortCode}", shortCode);
                throw;
            }
        }
    }
}
