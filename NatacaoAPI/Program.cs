using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NatacaoAPI.Data;
using NatacaoAPI.Middleware;
using NatacaoAPI.Repositories;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services;
using NatacaoAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════
// 1. BANCO DE DADOS — MySQL via Pomelo
// ══════════════════════════════════════════════════════════════════
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ══════════════════════════════════════════════════════════════════
// 2. INJEÇÃO DE DEPENDÊNCIA — Repositories e Services
//    Decisão: usar Scoped (uma instância por requisição HTTP) para
//    garantir que o DbContext seja compartilhado dentro da mesma request.
// ══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITurmaRepository, TurmaRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITurmaService, TurmaService>();
builder.Services.AddScoped<IReservaService, ReservaService>();

// ══════════════════════════════════════════════════════════════════
// 3. AUTOMAPPER — Escaneia todos os Profiles do assembly automaticamente
// ══════════════════════════════════════════════════════════════════
builder.Services.AddAutoMapper(cfg => 
{
    var licenseKey = builder.Configuration.GetValue<string>("AutoMapper:LicenseKey");
    if (!string.IsNullOrEmpty(licenseKey))
    {
        cfg.LicenseKey = licenseKey;
    }
}, AppDomain.CurrentDomain.GetAssemblies());

// ══════════════════════════════════════════════════════════════════
// 4. AUTENTICAÇÃO JWT
//    A chave simétrica e configurações ficam no appsettings.json.
//    Em produção, usar Azure Key Vault ou variáveis de ambiente.
// ══════════════════════════════════════════════════════════════════
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Dev only = false
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Remove tolerância padrão de 5 min
    };
});

builder.Services.AddAuthorization();

// Health Check
builder.Services.AddHealthChecks();

// ══════════════════════════════════════════════════════════════════
// 5. CONTROLLERS + SWAGGER
// ══════════════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NatacaoAPI",
        Version = "v1",
        Description = "API de Agendamento e Controle para Aulas de Natação"
    });

    // Configurar Swagger para aceitar JWT Bearer Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Exemplo: 'Bearer {seu_token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ══════════════════════════════════════════════════════════════════
// 6. MIDDLEWARE PIPELINE
//    Ordem importa! Exception handler primeiro, depois auth, depois endpoints.
// ══════════════════════════════════════════════════════════════════

// Middleware global de exceções — primeiro no pipeline para capturar tudo
app.UseMiddleware<GlobalExceptionMiddleware>();

// Health Check Endpoint
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Define o endpoint do JSON do Swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NatacaoAPI V1");
        // Define a rota para a UI do Swagger para não conflitar com a raiz
        c.RoutePrefix = "swagger";
    });
}

// Arquivos estáticos do frontend (wwwroot)
app.UseDefaultFiles();  // Serve index.html como default
app.UseStaticFiles();   // Habilita servir arquivos de wwwroot

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Tornar a classe Program acessível para testes de integração
public partial class Program { }