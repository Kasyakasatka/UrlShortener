# Используем образ SDK для сборки приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
# Убедитесь, что путь к вашему .csproj правильный относительно контекста сборки (Dina1)
COPY ["src/UrlShortener.Api/UrlShortener.Api.csproj", "src/UrlShortener.Api/"]
# Если у вас несколько проектов, добавьте их сюда
# COPY ["src/AnotherProject/AnotherProject.csproj", "src/AnotherProject/"]
RUN dotnet restore "src/UrlShortener.Api/UrlShortener.Api.csproj"

# Копируем весь остальной код
COPY . .
# Переходим в директорию вашего основного проекта
WORKDIR "/src/src/UrlShortener.Api"

# Собираем проект
RUN dotnet build "UrlShortener.Api.csproj" -c Release -o /app/build

# Публикуем проект
FROM build AS publish
RUN dotnet publish "UrlShortener.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Открываем порт, на котором будет слушать ваше приложение (обычно 80)
EXPOSE 80

# КОМАНДА ЗАПУСКА ВАШЕГО ПРИЛОЖЕНИЯ
# Убедитесь, что 'UrlShortener.Api.dll' - это фактическое имя вашей основной DLL
ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]