using Ducks.Api.Middlewares;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Integrations.Email;
using Ducks.Infrastructure.Integrations.OpenAI;
using Ducks.Infrastructure.Persistence;
using Ducks.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;


// Carrega .env da raiz da solução (ou de uma pasta ancestral), quando existir.
// Variáveis do ambiente e User Secrets continuam funcionando normalmente.
var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
while (currentDirectory is not null && !File.Exists(Path.Combine(currentDirectory.FullName, ".env")))
    currentDirectory = currentDirectory.Parent;

// NoClobber: uma variável que já existe no ambiente (servidor, Docker, testes) não é sobrescrita pelo .env.
if (currentDirectory is not null)
    DotNetEnv.Env.NoClobber().Load(Path.Combine(currentDirectory.FullName, ".env"));

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection não foi configurada.");

builder.Services.AddDbContext<DucksDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEducacionalService, EducacionalService>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IChatConversationService, ChatConversationService>();
// Serviços acrescentados (módulos/aulas, revisão, matrículas, avaliações, desafios e senha).
builder.Services.AddScoped<IConteudoService, ConteudoService>();
builder.Services.AddScoped<IModeracaoService, ModeracaoService>();
builder.Services.AddScoped<IMatriculaService, MatriculaService>();
builder.Services.AddScoped<IBancoQuestoesService, BancoQuestoesService>();
builder.Services.AddScoped<ITentativaService, TentativaService>();
builder.Services.AddScoped<IDesafioService, DesafioService>();
builder.Services.AddScoped<IRecuperacaoSenhaService, RecuperacaoSenhaService>();
// E-mail: Email__Provedor=Smtp envia de verdade; qualquer outro valor (ou vazio) usa o Mock, que só registra no log.
builder.Services.AddSingleton<FilaEmail>();
builder.Services.AddSingleton<IFilaEmail>(sp => sp.GetRequiredService<FilaEmail>());
builder.Services.AddSingleton<MockEmailService>();
if (string.Equals(builder.Configuration["Email:Provedor"], "Smtp", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
else
    builder.Services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<MockEmailService>());
builder.Services.AddHostedService<EnvioEmailBackgroundService>();
builder.Services.AddHttpClient<IDuckBotService, OpenAIDuckBotService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("Jwt:Secret deve ser configurado fora do código e possuir no mínimo 32 bytes.");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Issuer e Jwt:Audience são obrigatórios.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(subject, out var userId)) { context.Fail("Token sem identificador de usuário válido."); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<DucksDbContext>();
                var usuario = await db.Usuarios.AsNoTracking().Where(u => u.UsuarioID == userId && u.EstaAtivo)
                    .Select(u => new { u.SenhaAlteradaEm }).FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                if (usuario is null) context.Fail("Usuário inexistente ou bloqueado.");
                // Depois de redefinir a senha, tokens emitidos antes da troca deixam de valer.
                else if (usuario.SenhaAlteradaEm is { } alteradaEm && context.SecurityToken.ValidFrom < alteradaEm)
                    context.Fail("Sessão encerrada pela troca de senha.");
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autenticado",
                    Detail = "Informe um token de acesso válido.",
                    Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Você não possui permissão para executar esta operação.",
                    Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
                });
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("ContentManagement", policy => policy.RequireRole("Administrador", "Curador"));
    options.AddPolicy("Moderation", policy => policy.RequireRole("Administrador", "Moderador"));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("DucksCors", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .AllowAnyHeader()
            .AllowCredentials();
}));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Limite de requisições excedido",
            Detail = "Aguarde antes de realizar novas tentativas.",
            Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
        }, cancellationToken: cancellationToken);
    };
    options.AddPolicy("ChatPerUser", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    // O limite pode ser ajustado por configuração (RateLimiting:Auth:PermitLimit); o padrão continua 10.
    var authPermitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
    // Acrescentado: esqueci/redefinir senha (padrão 5 por IP a cada 15 minutos).
    var recuperacaoPermitLimit = builder.Configuration.GetValue("RateLimiting:RecuperacaoSenha:PermitLimit", 5);
    options.AddPolicy("RecuperacaoSenhaPerIp", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = recuperacaoPermitLimit,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("AuthPerIp", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ducks API", Version = "v1" });
    // Acrescentado: descrições das rotas e campos a partir dos comentários /// do código.
    foreach (var xml in new[] { "Ducks.Api.xml", "Ducks.Application.xml" })
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(caminho)) c.IncludeXmlComments(caminho);
    }
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    response.ContentType = "application/problem+json";
    await response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = response.StatusCode,
        Title = response.StatusCode == StatusCodes.Status404NotFound ? "Recurso não encontrado" : "Requisição não concluída",
        Detail = response.StatusCode == StatusCodes.Status404NotFound ? "A rota ou o recurso solicitado não existe." : "Não foi possível concluir a requisição.",
        Extensions = { ["traceId"] = statusContext.HttpContext.TraceIdentifier }
    });
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ducks API v1");
        c.RoutePrefix = string.Empty;
    });
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("DucksCors");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program { }
