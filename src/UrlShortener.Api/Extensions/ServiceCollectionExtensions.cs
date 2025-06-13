using Application.Interfaces;
using Infrastructure.CassandraConnectionManagement;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Cassandra;
using Cassandra.Mapping;
using System; 
using System.Threading;

namespace UrlShortener.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            var contactPoints = configuration.GetSection("Cassandra:ContactPoints").Get<string[]>() ?? new[] { "cassandra" };
            var cassandraPort = configuration.GetValue<int>("Cassandra:Port", 9042);

            services.AddSingleton<ICluster>(sp =>
            {
                return Cluster.Builder()
                    .AddContactPoints(contactPoints)
                    .WithPort(cassandraPort)
                    .Build();
            });

            var keyspaceName = configuration.GetValue<string>("Cassandra:Keyspace") ?? "url_shortener";
            services.AddSingleton<Cassandra.ISession>(sp =>
            {
                var cluster = sp.GetRequiredService<ICluster>();
                var logger = sp.GetRequiredService<ILogger<Program>>();

                int maxRetries = 10;
                int retryDelayMs = 5000;

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        logger.LogInformation($"Attempting to connect to Cassandra keyspace '{keyspaceName}' (attempt {i + 1}/{maxRetries})...");
                        return cluster.Connect(keyspaceName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, $"Failed to connect to Cassandra (attempt {i + 1}/{maxRetries}): {ex.Message}");
                        if (i < maxRetries - 1)
                        {
                            Thread.Sleep(retryDelayMs);
                        }
                        else
                        {
                            logger.LogError(ex, "Exceeded max retries for Cassandra connection. Application will terminate.");
                            throw;
                        }
                    }
                }
                throw new InvalidOperationException("Failed to connect to Cassandra after multiple retries.");
            });

            services.AddSingleton<CassandraContext>();

            services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<Cassandra.ISession>(),
                new MappingConfiguration().Define(
                    new Map<Domain.Entities.Url>()
                        .TableName("urls")
                        .PartitionKey(u => u.ShortCode)
                        .Column(u => u.Id, cm => cm.WithName("id"))
                        .Column(u => u.ShortCode, cm => cm.WithName("short_code"))
                        .Column(u => u.OriginalUrl, cm => cm.WithName("original_url"))
                        .Column(u => u.CreationTimestamp, cm => cm.WithName("creation_timestamp"))
                        .Column(u => u.ExpirationDate, cm => cm.WithName("expiration_date"))
                        .Column(u => u.IsActive, cm => cm.WithName("is_active")),
                    new Map<Domain.Entities.ClickAnalytic>()
                        .TableName("click_analytics")
                        .PartitionKey(ca => ca.ShortCode)
                        .ClusteringKey(ca => ca.ClickTimestamp)
                        .Column(ca => ca.ShortCode, cm => cm.WithName("short_code"))
                        .Column(ca => ca.ClickTimestamp, cm => cm.WithName("click_timestamp"))
                        .Column(ca => ca.IpAddress, cm => cm.WithName("ip_address"))
                        .Column(ca => ca.UserAgent, cm => cm.WithName("user_agent"))
                )
            ));

            services.AddScoped<IUrlRepository, UrlRepository>();
            services.AddScoped<IClickAnalyticRepository, ClickAnalyticRepository>();
            services.AddTransient<IShortCodeGenerator, Base62ShortCodeGenerator>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                typeof(Application.Interfaces.IUrlRepository).Assembly,
                typeof(Application.Handlers.RedirectUrlQueryHandler).Assembly
            ));

            services.AddHostedService<ExpirationBackgroundService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ExpirationBackgroundService>>();
                var session = sp.GetRequiredService<Cassandra.ISession>();
                var mapper = sp.GetRequiredService<IMapper>();
                return new ExpirationBackgroundService(logger, session, mapper);
            });

            return services;
        }
    }
}
