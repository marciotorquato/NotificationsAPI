using NotificationsAPI.IoC;
using NotificationsAPI.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar toda infraestrutura (DbContext, Repositories, Services, RabbitMQ)
builder.Services.AddInfrastructure(builder.Configuration);

// Registrar RabbitMQ
builder.Services.AddRabbitMQMessaging(builder.Configuration);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));


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
