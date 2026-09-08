using FCG.Catalog.Api.Extensions;
using FCG.Catalog.Infra.Context;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.AddDatabase();
builder.AddJwt();
builder.AddSwagger();
builder.Services.AddDependencies();
builder.Services.AddMassTransitWithRabbitMQ(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogContext>();

var app = builder.Build();

app.MigrateDatabase();
app.UseErrorHandling();
app.UseSwaggerConfig();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapMetrics();

app.Run();
