using NotificationsAPI.IoC;
using NotificationsAPI.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar RabbitMQ
builder.Services.AddRabbitMQMessaging(builder.Configuration);

// Configurar Serilog (opcional)
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Inicializar RabbitMQ
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


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
