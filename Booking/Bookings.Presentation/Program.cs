using Bookings.Application.Services;
using Bookings.Core.Interfaces;
using Bookings.Infrastructure.Repositories;
using Bookings.Presentation.Middleware;
using Bookings.Presentation.Validators;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services);


var app = builder.Build();
ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    services.AddControllers()
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateBookingDtoValidator>());


    services.AddScoped<BookingService>();


    services.AddScoped<IBookingRepository, BookingRepository>();
}

void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<ErrorHandlingMiddleware>();


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