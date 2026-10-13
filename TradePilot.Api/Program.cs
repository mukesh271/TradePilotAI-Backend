using System;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.Middleware;
using TradePilot.Api.Services;
using TradePilot.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<TradePilotDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TradePilotDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// Add application services
builder.Services.AddScoped<IRiskManagementService, RiskManagementService>();

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TradePilot API v1.0");
        c.RoutePrefix = string.Empty; // Serve at root
    });
    app.UseHttpsRedirection();
}

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradePilotDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

app.MapControllers();

app.Run();

