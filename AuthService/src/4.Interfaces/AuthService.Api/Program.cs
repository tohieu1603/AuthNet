using AuthService.Api.Config;
using AuthService.Api.Middleware;
using AuthService.Api.Support;
using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Layer DI ──────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── API Services ──────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Auth Service API",
        Version = "v1",
        Description = "Authentication & authorization microservice — JWT + refresh-token rotation, Google SSO, RBAC."
    });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter 'Bearer {token}' or rely on HttpOnly ACCESS_TOKEN cookie."
    });
    opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc, null)] = []
    });
});

// ── Options binding ───────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection(CookieSettings.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));

// ── Support services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<AuthCookieWriter>();
builder.Services.AddScoped<DataSeeder>();

// ── Authentication & Authorization policy ─────────────────────────────────
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, AuthService.Api.Security.DynamicPermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AuthService.Api.Security.PermissionAuthorizationHandler>();

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── Middleware pipeline (order matters) ────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1"));
}

app.UseCors();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Prometheus metrics ────────────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok(new { status = "UP", timestamp = DateTimeOffset.UtcNow }))
   .ExcludeFromDescription();

// ── Database init + Data seeder on startup ─────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
