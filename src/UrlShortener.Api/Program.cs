using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Application.Interfaces;
using Infrastructure.CassandraConnectionManagement;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Cassandra; // Для ICluster и ISession
using Cassandra.Mapping; // Для IMapper
using Microsoft.Extensions.DependencyInjection; // Для методов расширения AddSingleton, AddScoped и т.д.
using Microsoft.Extensions.Hosting; // Для AddHostedService
using Microsoft.Extensions.Logging;
using System.Threading; // Для Thread.Sleep
using System.Net.Sockets; // Для SocketException

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- НАЧАЛО: ПРАВИЛЬНАЯ РЕГИСТРАЦИЯ CASSANDRA И СВЯЗАННЫХ СЕРВИСОВ ---

// 1. Регистрация ICluster
var contactPoints = builder.Configuration.GetSection("Cassandra:ContactPoints").Get<string[]>() ?? new[] { "cassandra" };
var cassandraPort = builder.Configuration.GetValue<int>("Cassandra:Port", 9042);

builder.Services.AddSingleton<ICluster>(sp =>
{
    return Cluster.Builder()
        .AddContactPoints(contactPoints)
        .WithPort(cassandraPort)
        .Build();
});

// 2. Регистрация ISession с повторными попытками
var keyspaceName = builder.Configuration.GetValue<string>("Cassandra:Keyspace") ?? "url_shortener";
builder.Services.AddSingleton<Cassandra.ISession>(sp =>
{
    var cluster = sp.GetRequiredService<ICluster>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    int maxRetries = 10;
    int retryDelayMs = 5000; // 5 секунд

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


// 3. Регистрация CassandraContext (регистрируем для DI)
builder.Services.AddSingleton<CassandraContext>();

// 4. Регистрация IMapper (для Cassandra.Mapping)
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
        // !!! ИЗМЕНЕНИЕ ЗДЕСЬ: ДОБАВЛЕН МАППИНГ ДЛЯ ClickAnalytic !!!
        new Map<Domain.Entities.ClickAnalytic>()
            .TableName("click_analytics") // Явно указываем имя таблицы
            .PartitionKey(ca => ca.ShortCode) // Указываем первичный ключ
            .ClusteringKey(ca => ca.ClickTimestamp) // Указываем ключ кластеризации
            .Column(ca => ca.ShortCode, cm => cm.WithName("short_code"))
            .Column(ca => ca.ClickTimestamp, cm => cm.WithName("click_timestamp"))
            .Column(ca => ca.IpAddress, cm => cm.WithName("ip_address"))
            .Column(ca => ca.UserAgent, cm => cm.WithName("user_agent"))
    )
));


// --- КОНЕЦ: ПРАВИЛЬНАЯ РЕГИСТРАЦИЯ CASSANDRA И СВЯЗАННЫХ СЕРВИСОВ ---


// Регистрация репозиториев
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddScoped<IClickAnalyticRepository, ClickAnalyticRepository>();

// Регистрация сервисов
builder.Services.AddTransient<IShortCodeGenerator, Base62ShortCodeGenerator>();

// Добавляем MediatR
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

// Добавляем фоновый сервис
builder.Services.AddHostedService<ExpirationBackgroundService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ExpirationBackgroundService>>();
    var session = sp.GetRequiredService<Cassandra.ISession>();
    var mapper = sp.GetRequiredService<IMapper>();
    return new ExpirationBackgroundService(logger, session, mapper);
});

var app = builder.Build();

// Применяем миграции Cassandra при запуске
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    Cassandra.ISession keyspaceCreationSession = null;
    try
    {
        logger.LogInformation("Attempting to create a temporary Cassandra connection for keyspace and table migrations...");

        int maxMigrationRetries = 20; // Увеличиваем количество попыток для миграций
        int migrationRetryDelayMs = 5000; // 5 секунд

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


// Настраиваем конвейер HTTP-запросов.
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
