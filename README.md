URL Shortener (C# .NET, Cassandra, Docker)
Project Description
This project is a URL shortening service built with C# (.NET), Cassandra, and Docker Compose. It provides functionalities to shorten URLs, redirect, and track clicks.

Setup and Run
Ensure you have Docker and Docker Compose installed.

1. Clone the repository:

git clone <YOUR_REPOSITORY_URL>
cd Dina1 # Navigate to the project root

2. Build and run the application:
Execute the following command in your terminal from the project root (Dina1 folder):

docker-compose -f docker-compose.yml up --build --force-recreate

This command will:

Build the .NET API image.

Pull the Cassandra image.

Start both cassandra and api services.

Apply necessary database migrations.

3. Wait for application startup:
Monitor the terminal logs. The application is ready when you see: Application started.

API Usage
Access the API documentation via Swagger UI:

Swagger UI: http://localhost:8080/swagger

Use the Swagger UI to:

POST /api/urls: Create a new short URL.

GET /{shortCode}: Redirect from a short code to the original URL.

GET /api/urls/{shortCode}: Get details and analytics for a short URL.