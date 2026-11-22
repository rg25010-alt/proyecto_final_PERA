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

var app = builder.Build();

app.MapGraphQL();

app.Run();