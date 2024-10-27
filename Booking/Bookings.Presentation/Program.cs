using Bookings.Application.Services;
using Bookings.Infrastructure;
using Bookings.Presentation;
using Bookings.Presentation.Middleware;
using Bookings.Presentation.Validators;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Prometheus;
using Serilog;
using Serilog.Sinks.Network;
using StackExchange.Redis;
using Stripe;
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
    StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

    services.AddControllers();

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.UseHttpClientMetrics();

    services.AddControllers()
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ReserveSeatsRequestValidator>());

    services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var connectionString = configuration["Redis:ConnectionString"];
        return ConnectionMultiplexer.Connect(connectionString);
    });
    services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
    services.AddHangfireServer();

    services.AddTransient<ReservationExpiryJob>();
    builder.Services.AddScoped<PaymentIntentService>();
    builder.Services.AddScoped<RefundService>();


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

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
        if (shouldSeedDatabase)
            AppDbContextSeeder.Seed(dbContext);
    }
    app.UseHangfireDashboard();

    // Schedule the ReservationExpiryJob to run every 5 minutes
    RecurringJob.AddOrUpdate<ReservationExpiryJob>(
        job => job.CheckAndReleaseExpiredReservationsAsync(), Cron.MinuteInterval(1));

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