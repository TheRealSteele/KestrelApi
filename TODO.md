# TODO List - Immediate Next Steps

## 🚨 High Priority (This Week)

### Security Critical
- [ ] **Rate Limiting** - Prevent API abuse
  ```csharp
  // Add to Program.cs
  services.AddRateLimiter(options => {
      options.AddFixedWindowLimiter("api", options => {
          options.PermitLimit = 100;
          options.Window = TimeSpan.FromMinutes(1);
      });
  });
  ```

- [ ] **Move Auth0 Config to User Secrets**
  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "Auth0:Domain" "your-domain"
  dotnet user-secrets set "Auth0:Audience" "your-audience"
  ```

- [ ] **Add Security Headers Middleware**
  - Create `SecurityHeadersMiddleware.cs`
  - Add to pipeline in Program.cs
  - Test with securityheaders.com

### API Robustness
- [ ] **Global Exception Handler**
  ```csharp
  public class GlobalExceptionHandlingMiddleware
  {
      // Catch all exceptions
      // Return consistent ProblemDetails
      // Log with correlation ID
  }
  ```

## 📋 Medium Priority (Next Week)

### API Design
- [ ] **API Versioning**
  - Install `Microsoft.AspNetCore.Mvc.Versioning`
  - Add version to routes: `/api/v1/names`
  - Create deprecation strategy

- [ ] **Enhance Swagger**
  - Add JWT authentication UI
  - Document all status codes
  - Add request/response examples

### Code Quality
- [ ] **Add FluentValidation**
  - Replace data annotations
  - Create validator classes
  - Add custom validation rules

- [ ] **Create Constants File**
  ```csharp
  public static class ApiConstants
  {
      public const int MaxNameLength = 100;
      public const int MaxSecretLength = 500;
      public const string CorrelationIdHeader = "X-Correlation-Id";
  }
  ```

## 🔧 Low Priority (This Month)

### Infrastructure
- [ ] **Create Dockerfile**
  - Multi-stage build
  - Non-root user
  - Health check included

- [ ] **GitHub Actions CI/CD**
  - Build and test on PR
  - Security scanning
  - Auto-deploy to staging

### Documentation
- [ ] **Add XML Comments**
  ```csharp
  /// <summary>
  /// Adds a new name for the authenticated user
  /// </summary>
  /// <param name="request">The name to add</param>
  /// <returns>201 Created on success</returns>
  ```

- [ ] **Create CONTRIBUTING.md**
  - Development setup
  - Testing guidelines
  - PR process

## 🎯 Quick Wins (Can do today)

1. **Add CORS Policy**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("ApiPolicy", policy =>
       {
           policy.WithOrigins("https://localhost:3000")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
       });
   });
   ```

2. **Add Request Logging Middleware**
   - Log request method, path, and duration
   - Exclude sensitive endpoints from logging

3. **Configure Serilog Request Logging**
   ```csharp
   app.UseSerilogRequestLogging(options =>
   {
       options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
   });
   ```

## 📊 Definition of Done

For each task:
- [ ] Code implemented and tested
- [ ] Unit tests written (>90% coverage)
- [ ] Integration tests updated
- [ ] Documentation updated
- [ ] No new compiler warnings
- [ ] Security review completed
- [ ] Performance impact assessed

## 🔄 Daily Checklist

- [ ] Review and update this TODO list
- [ ] Pick top priority items
- [ ] Create feature branch
- [ ] Implement with TDD
- [ ] Update documentation
- [ ] Create PR with description
- [ ] Address review feedback

---

*Last Updated: 2025-01-18*
*Next Sprint Planning: 2025-01-25*