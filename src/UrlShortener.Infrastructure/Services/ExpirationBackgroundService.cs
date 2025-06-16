using Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Cassandra;
using Cassandra.Mapping;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Infrastructure.Services
{
    public class ExpirationBackgroundService : BackgroundService
    {
        private readonly ILogger<ExpirationBackgroundService> _logger;
        private readonly Cassandra.ISession _session;
        private readonly IMapper _mapper;

        private const string KeyspaceName = "url_shortener";

        public ExpirationBackgroundService(ILogger<ExpirationBackgroundService> logger, Cassandra.ISession session, IMapper mapper)
        {
            _logger = logger;
            _session = session;
            _mapper = mapper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expiration Background Service running.");

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await HandleExpiredUrlsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while handling expired URLs.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Expiration Background Service stopping.");
        }

        private async Task HandleExpiredUrlsAsync()
        {
            _logger.LogInformation("Checking for expired URLs...");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            string currentBucket = now.ToString("yyyy-MM-dd");
            string yesterdayBucket = now.AddDays(-1).ToString("yyyy-MM-dd");
            string twoDaysAgoBucket = now.AddDays(-2).ToString("yyyy-MM-dd");

            var bucketsToQuery = new List<string> { currentBucket, yesterdayBucket, twoDaysAgoBucket };

            List<Url> urlsToDeactivate = new List<Url>();
            int urlsCheckedCount = 0;
            int urlsDeactivatedCount = 0;

            foreach (var bucket in bucketsToQuery)
            {
                _logger.LogInformation("Querying active URLs from bucket: {Bucket}", bucket);

                var selectStatement = new SimpleStatement(
                    $"SELECT short_code, original_url, creation_timestamp, expiration_date, is_active, expiration_bucket " +
                    $"FROM {KeyspaceName}.urls WHERE expiration_bucket = ? AND is_active = true ALLOW FILTERING;"
                );

                try
                {
                    var rowSet = await _session.ExecuteAsync(selectStatement.Bind(bucket));

                    foreach (var row in rowSet)
                    {
                        urlsCheckedCount++;
                        var url = new Url
                        {
                            ShortCode = row.GetValue<string>("short_code"),
                            OriginalUrl = row.GetValue<string>("original_url"),
                            CreationTimestamp = row.GetValue<DateTimeOffset>("creation_timestamp"),
                            ExpirationDate = row.GetValue<DateTimeOffset?>("expiration_date"),
                            IsActive = row.GetValue<bool>("is_active"),
                            ExpirationBucket = row.GetValue<string>("expiration_bucket")
                        };

                        if (url.IsActive && url.IsExpired())
                        {
                            urlsToDeactivate.Add(url);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error querying URLs from bucket {Bucket}: {ErrorMessage}", bucket, ex.Message);
                }
            }

            _logger.LogInformation("Found {CheckedCount} active URLs in checked buckets. {DeactivateCount} URLs for deactivation.", urlsCheckedCount, urlsToDeactivate.Count);

            foreach (var url in urlsToDeactivate)
            {
                _logger.LogInformation($"Your URL deactivation: {url.ShortCode}");

                string oldBucket = url.ExpirationBucket;
                bool oldIsActive = true;
                string shortCode = url.ShortCode;

                Url newUrlState = new Url
                {
                    ShortCode = url.ShortCode,
                    OriginalUrl = url.OriginalUrl,
                    CreationTimestamp = url.CreationTimestamp,
                    ExpirationDate = url.ExpirationDate,
                    IsActive = false,
                    ExpirationBucket = url.ExpirationBucket
                };

                try
                {
                    await _mapper.DeleteAsync<Url>(
                        $"WHERE expiration_bucket = ? AND is_active = ? AND short_code = ?",
                        oldBucket, oldIsActive, shortCode);

                    await _mapper.InsertAsync(newUrlState);

                    urlsDeactivatedCount++;
                    _logger.LogInformation("Successfully deactivated expired URL: {ShortCode}", url.ShortCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deactivating URL{ShortCode}: {ErrorMessage}", url.ShortCode, ex.Message);
                }
            }

            _logger.LogInformation($"Finished checking for expired URLs. Deactivated {urlsDeactivatedCount} URLs.");
        }
    }
}
