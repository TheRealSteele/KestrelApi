# KestrelApi

A secure .NET 9 Web API for managing names and secrets with Auth0 authentication, structured logging, and health monitoring.

## Project Structure

```
KestrelApi/
├── Controllers/              # API Controllers
│   ├── HealthController.cs   # Health check endpoints
│   ├── NamesController.cs    # Names management endpoints
│   └── SecretsController.cs  # Secrets management endpoints
├── HealthChecks/            # Custom health checks
│   └── Auth0HealthCheck.cs  # Auth0 connectivity check
├── Names/                   # Names domain
│   ├── INamesRepository.cs
│   ├── INamesService.cs
│   ├── InMemoryNamesRepository.cs
│   └── NamesService.cs
├── Secrets/                 # Secrets domain
│   ├── ISecretsRepository.cs
│   ├── ISecretsService.cs
│   ├── InMemorySecretsRepository.cs
│   └── SecretsService.cs
├── Security/                # Security services
│   ├── DataProtectionEncryptionService.cs
│   └── IEncryptionService.cs
├── Models/                  # Request/Response models
│   ├── NameRequest.cs
│   └── SecretRequest.cs
├── KestrelApi.IntegrationTests/  # Integration tests
├── .editorconfig            # Code style configuration
├── .gitignore              # Git ignore rules
├── Directory.Build.props    # MSBuild global properties
├── appsettings.json        # Application configuration
├── KestrelApi.csproj       # Project file
└── Program.cs              # Application entry point
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Auth0 Account](https://auth0.com/) (for authentication)
- Optional: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## Configuration

### Auth0 Setup

1. Create an Auth0 application
2. Update `appsettings.json` with your Auth0 configuration:

```json
{
  "Auth0": {
    "Domain": "https://your-tenant.auth0.com/",
    "Audience": "https://your-api-identifier"
  }
}
```

### Environment-Specific Configuration

For development, create `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information"
      }
    }
  }
}
```

## Building the Application

### Using .NET CLI

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Using Visual Studio

1. Open `KestrelApi.sln` in Visual Studio
2. Press `Ctrl+Shift+B` or go to Build → Build Solution

## Running the Application

### Using .NET CLI

```bash
# Run in Development mode
dotnet run

# Run in Production mode
dotnet run --environment Production

# Run with specific URLs
dotnet run --urls "http://localhost:5000;https://localhost:5001"
```

### Using Visual Studio

1. Press `F5` to run with debugging
2. Press `Ctrl+F5` to run without debugging

### Using Docker

```bash
# Build the Docker image
docker build -t kestrelapi .

# Run the container
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production kestrelapi
```

## API Endpoints

### Public Endpoints

- `GET /health` - Overall health status
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

### Protected Endpoints (Require JWT Authentication)

#### Names
- `GET /names` - Get all names for the authenticated user
- `POST /names` - Add a new name
- `GET /names/{index}` - Get a specific name by index

#### Secrets
- `GET /secrets` - Get all secrets for the authenticated user
- `POST /secrets` - Add a new encrypted secret
- `GET /secrets/{index}` - Get a specific secret by index

## Running Tests

### Unit Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test KestrelApi.Tests/KestrelApi.Tests.csproj
```

### Integration Tests

```bash
# Run integration tests
dotnet test KestrelApi.IntegrationTests/KestrelApi.IntegrationTests.csproj

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Test Categories

Run specific test categories:

```bash
# Run only unit tests
dotnet test --filter Category=Unit

# Run only integration tests
dotnet test --filter Category=Integration

# Run tests for a specific feature
dotnet test --filter FullyQualifiedName~SecretsController
```

## Development

### Code Quality

The project enforces code quality through:

- **EditorConfig** - Consistent code formatting
- **Code Analysis** - Static analysis with .NET analyzers
- **Treat Warnings as Errors** - Ensures clean builds

### Logging

The application uses Serilog for structured logging:

- Console output in JSON format
- File output with daily rotation
- Logs stored in `logs/` directory
- Configurable log levels per namespace

### Health Checks

Health endpoints provide:

- **Overall Health** (`/health`) - All health checks
- **Readiness** (`/health/ready`) - Ready to accept traffic
- **Liveness** (`/health/live`) - Application is running

## Troubleshooting

### Common Issues

1. **Auth0 Configuration Error**
   - Ensure Auth0 domain includes `https://` and trailing `/`
   - Verify audience matches your API identifier

2. **Port Already in Use**
   - Change ports in `launchSettings.json` or use `--urls` parameter

3. **Certificate Issues**
   - Run `dotnet dev-certs https --trust` for local HTTPS

### Debug Logging

Enable debug logging by setting environment variable:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Serilog__MinimumLevel__Default=Debug
```

## Contributing

1. Follow the coding standards in `.editorconfig`
2. Ensure all tests pass before submitting PR
3. Add tests for new features
4. Update documentation as needed

## License

This project is licensed under the MIT License.