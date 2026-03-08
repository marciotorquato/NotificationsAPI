using Microsoft.EntityFrameworkCore;
using NotificationsAPI.Data;
using NotificationsAPI.IoC;
using NotificationsAPI.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRabbitMQMessaging(builder.Configuration);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

try
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<RabbitMQInitializer>();
    await initializer.InitializeAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro ao inicializar RabbitMQ");
    throw;
}

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
    Log.Information("Migrations aplicadas com sucesso.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro ao aplicar migrations");
    throw;
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();