using AuthService.Application;
using AuthService.Grpc.Services;
using AuthService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Layer DI ──────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── gRPC ──────────────────────────────────────────────────────────────────
builder.Services.AddGrpc();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────
app.MapGrpcService<AuthGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
