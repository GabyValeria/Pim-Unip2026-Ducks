using Ducks.Api.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi; // Namespace ajustado para o Microsoft.OpenApi 2.0+ (.NET 10)
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Serilog para logs estruturados
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Adicionar serviços ao container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ducks API Principal",
        Version = "v1",
        Description = "API RESTful responsável pela comunicação entre os clientes Web, Desktop e Mobile e o banco de dados do ecossistema Ducks."
    });
});

// Adicionar Health Checks
builder.Services.AddHealthChecks();

// Configuração de CORS para suportar Web, Desktop e Mobile
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar o pipeline de requisições HTTP
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ducks API Principal v1");
        c.RoutePrefix = string.Empty; // Abre o Swagger diretamente na raiz (ex: http://localhost:5000/)
    });
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

// Endpoint de Health Check
app.MapHealthChecks("/health");

app.Run();