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
            _logger.LogInformation("Applying Cassandra migrations...");
            _logger.LogInformation("Dropping 'urls' table if it exists to ensure current schema...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS urls;"));
            _logger.LogInformation("'urls' table dropped if existed.");

            _logger.LogInformation("Creating or ensuring existence of 'urls' table with new schema (expiration_bucket)...");
            await _session.ExecuteAsync(new SimpleStatement(
                "CREATE TABLE IF NOT EXISTS urls (" +
                "short_code text," +
                "original_url text," +
                "creation_timestamp timestamp," +
                "expiration_date timestamp," +
                "is_active boolean," +
                "expiration_bucket text," + 
                           
                "PRIMARY KEY ((expiration_bucket, is_active), short_code))"));
            _logger.LogInformation("'urls' table created or already exists.");

            _logger.LogInformation("Dropping 'url_clicks' table if it exists to ensure current schema...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS url_clicks;"));
            _logger.LogInformation("'url_clicks' table dropped if existed.");

            _logger.LogInformation("Creating or ensuring existence of 'url_clicks' table for click counters...");
            await _session.ExecuteAsync(new SimpleStatement(
                "CREATE TABLE IF NOT EXISTS url_clicks (" +
                "short_code text PRIMARY KEY," +
                "count counter)"));
            _logger.LogInformation("'url_clicks' table created or already exists.");

            _logger.LogInformation("Dropping 'click_analytics' table if it exists to ensure current schema...");
            await _session.ExecuteAsync(new SimpleStatement("DROP TABLE IF EXISTS click_analytics;"));
            _logger.LogInformation("'click_analytics' table dropped if existed.");

            _logger.LogInformation("Creating or ensuring existence of 'click_analytics' table...");
            await _session.ExecuteAsync(new SimpleStatement(
                   "CREATE TABLE IF NOT EXISTS click_analytics (" +
                   "short_code text," +
                   "click_timestamp timestamp," +
                   "ip_address text," +
                   "user_agent text," +
                   "PRIMARY KEY (short_code, click_timestamp)" +
                   ") WITH CLUSTERING ORDER BY (click_timestamp DESC);"));
            _logger.LogInformation("'click_analytics' table created or already exists.");
        }
    }
}
