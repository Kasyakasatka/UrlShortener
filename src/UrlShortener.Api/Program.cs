using Cassandra;
using Infrastructure.CassandraConnectionManagement;
using Microsoft.OpenApi.Models;
using System.Reflection;
using UrlShortener.Api.Extensions;
using UrlShortener.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Сокращения URL", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddApplicationServices(builder.Configuration);


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var contactPoints = app.Configuration.GetSection("Cassandra:ContactPoints").Get<string[]>() ?? new[] { "cassandra" };
    var cassandraPort = app.Configuration.GetValue<int>("Cassandra:Port", 9042);
    var keyspaceName = app.Configuration.GetValue<string>("Cassandra:Keyspace") ?? "url_shortener";

    Cassandra.ISession keyspaceCreationSession = null;
    try
    {
        logger.LogInformation("Attempting to create a temporary Cassandra connection for keyspace and table migrations...");

        int maxMigrationRetries = 20;
        int migrationRetryDelayMs = 5000;

        for (int i = 0; i < maxMigrationRetries; i++)
        {
            try
            {
                keyspaceCreationSession = Cluster.Builder()
                                                .AddContactPoints(contactPoints)
                                                .WithPort(cassandraPort)
                                                .Build()
                                                .Connect();

                logger.LogInformation($"Successfully connected to Cassandra for migrations (attempt {i + 1}).");
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Failed to connect to Cassandra for migrations (attempt {i + 1}/{maxMigrationRetries}): {ex.Message}");
                if (i < maxMigrationRetries - 1)
                {
                    Thread.Sleep(migrationRetryDelayMs);
                }
                else
                {
                    logger.LogError(ex, "Exceeded max retries for Cassandra migration connection. Application will terminate.");
                    throw;
                }
            }
        }

        if (keyspaceCreationSession == null)
        {
            throw new InvalidOperationException("Failed to establish Cassandra connection for migrations.");
        }


        logger.LogInformation($"Попытка создать keyspace '{keyspaceName}' если он не существует.");
        await keyspaceCreationSession.ExecuteAsync(new SimpleStatement(
            $"CREATE KEYSPACE IF NOT EXISTS {keyspaceName} " +
            "WITH replication = {'class': 'SimpleStrategy', 'replication_factor' : 1};"));
        logger.LogInformation($"Keyspace '{keyspaceName}' создан или уже существует.");
        var dedicatedMigrationSession = keyspaceCreationSession.Cluster.Connect(keyspaceName);
        var cassandraContextLogger = serviceProvider.GetRequiredService<ILogger<CassandraContext>>();
        var cassandraContext = new CassandraContext(dedicatedMigrationSession, cassandraContextLogger);

        await cassandraContext.ApplyMigrations();
        logger.LogInformation("Миграции Cassandra успешно применены.");
        await dedicatedMigrationSession.ShutdownAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка во время создания keyspace Cassandra или миграции: {ErrorMessage}", ex.Message);
        throw;
    }
    finally
    {
        if (keyspaceCreationSession != null)
        {
            await keyspaceCreationSession.ShutdownAsync();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "URL Shortener API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
