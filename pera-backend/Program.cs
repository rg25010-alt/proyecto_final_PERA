using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Pera.Backend.Datos;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "La API usa datos de prueba y solo se puede iniciar en un entorno de desarrollo.");
}

builder.Services.AddControllers();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme).AddBearerToken();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IRepositorioTutorias, RepositorioTutoriasEnMemoria>();
builder.Services.AddSingleton<IRepositorioExpedientes, RepositorioExpedientesEnMemoria>();

var app = builder.Build();

if (args.Contains("--token-coordinacion"))
{
    Console.WriteLine("Token local de Coordinación PERA (vigente durante una hora):");
    Console.WriteLine(AccesoLocalCoordinacion.Emitir(app.Services));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
