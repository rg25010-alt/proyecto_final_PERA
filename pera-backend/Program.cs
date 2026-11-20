using Pera.Backend.Datos;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "La API usa datos de prueba y solo se puede iniciar en un entorno de desarrollo.");
}

builder.Services.AddControllers();
builder.Services.AddSingleton<IRepositorioExpedientes, RepositorioExpedientesEnMemoria>();

var app = builder.Build();

app.MapControllers();
app.Run();
