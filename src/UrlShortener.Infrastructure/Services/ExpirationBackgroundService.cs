using Application.Interfaces;
using Cassandra;
using Cassandra.Mapping;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExpirationBackgroundService : BackgroundService
    {
        private readonly ILogger<ExpirationBackgroundService> _logger;
        private Cassandra.ISession _session;
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

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                try
                {
                    await HandleExpiredUrlsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while handling expired URLs.");
                }
            }

            _logger.LogInformation("Expiration Background Service stopping.");
        }

        private async Task HandleExpiredUrlsAsync()
        {
            _logger.LogInformation("Checking for expired URLs...");

            var expiredUrls = await _mapper.FetchAsync<Url>(
                $"SELECT * FROM {KeyspaceName}.urls WHERE is_active = true AND expiration_date <= ?",
                DateTimeOffset.UtcNow);

            foreach (var url in expiredUrls)
            {
                _logger.LogInformation($"Deactivating expired URL: {url.ShortCode}");
                url.IsActive = false;
                await _mapper.UpdateAsync(url);
            }

            _logger.LogInformation($"Finished checking for expired URLs. Deactivated {expiredUrls.Count()} URLs.");
        }
    }
}
