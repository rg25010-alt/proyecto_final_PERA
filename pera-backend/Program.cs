using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;
using Pera.Backend.GraphQL;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Default");

builder.Services.AddDbContext<PeraDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());


builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularLocal", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();


app.UseCors("AngularLocal");

app.MapGraphQL();

app.Run();
