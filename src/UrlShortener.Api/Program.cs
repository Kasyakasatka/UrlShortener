using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Application.Interfaces;
using Infrastructure.CassandraConnectionManagement;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Cassandra; 
using Cassandra.Mapping; 
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Logging;
using System.Threading; 
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var contactPoints = builder.Configuration.GetSection("Cassandra:ContactPoints").Get<string[]>() ?? new[] { "cassandra" };
var cassandraPort = builder.Configuration.GetValue<int>("Cassandra:Port", 9042);

builder.Services.AddSingleton<ICluster>(sp =>
{
    return Cluster.Builder()
        .AddContactPoints(contactPoints)
        .WithPort(cassandraPort)
        .Build();
});

var keyspaceName = builder.Configuration.GetValue<string>("Cassandra:Keyspace") ?? "url_shortener";
builder.Services.AddSingleton<Cassandra.ISession>(sp =>
{
    var cluster = sp.GetRequiredService<ICluster>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    int maxRetries = 10;
    int retryDelayMs = 5000; //5 seconds

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


builder.Services.AddSingleton<CassandraContext>();

builder.Services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<Cassandra.ISession>(),
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


builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<IClickAnalyticRepository, ClickAnalyticRepository>();
builder.Services.AddTransient<IShortCodeGenerator, Base62ShortCodeGenerator>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    Assembly.GetExecutingAssembly(),
    typeof(Application.Interfaces.IUrlRepository).Assembly
));

builder.Services.AddControllers();
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

builder.Services.AddHostedService<ExpirationBackgroundService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ExpirationBackgroundService>>();
    var session = sp.GetRequiredService<Cassandra.ISession>();
    var mapper = sp.GetRequiredService<IMapper>();
    return new ExpirationBackgroundService(logger, session, mapper);
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

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
app.UseAuthorization();

app.MapControllers();

app.Run();
