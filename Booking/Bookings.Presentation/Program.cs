using Bookings.Infrastructure;
using Bookings.Presentation;
using Bookings.Presentation.Middleware;
using Bookings.Presentation.Validators;
using BookingService.Infrastructure;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Sinks.Network;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.UDPSink("http://logstash", 5000)
    .CreateLogger();

builder.Host.UseSerilog();

ConfigureServices(builder.Services);


var app = builder.Build();
ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.UseHttpClientMetrics();

    services.AddControllers()
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateBookingRequestValidator>());

    services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    services.RegisterServicesFromAssemblies(
    Assembly.Load("Bookings.Core"),
    Assembly.Load("Bookings.Application"),
    Assembly.Load("Bookings.Infrastructure")
);
}


void ConfigureMiddleware(WebApplication app)
{
    bool shouldSeedDatabase = builder.Configuration.GetValue<bool>("SeedDatabase");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Database.Migrate();
        if (shouldSeedDatabase)
            AppDbContextSeeder.Seed(dbContext);
    }

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseRouting();
    app.UseMetricServer();
    app.UseHttpMetrics();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthorization();

    app.MapControllers();
}

public partial class Program { }