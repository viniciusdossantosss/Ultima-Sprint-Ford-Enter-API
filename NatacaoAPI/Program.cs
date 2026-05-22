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
    cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODA4Njk3NjAwIiwiaWF0IjoiMTc3NzIzMTE2MSIsImFjY291bnRfaWQiOiIwMTlkY2IzYWU5NTI3ZDk4YTA5MWJkZmIzYzc2ZDBjZSIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa3E1a3B0N2VjOHBkZGNqbWQ5aHYwN3ZmIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.WUc5NBu39ZbF2a2inQjF1wbaRSVX9u5o8R6zlXomLxa3CvS-gLp6O_J3b64PDiFpvTXJMeW-XW2Gvrg6YO9-_a7CpBO8jOWucFpp1e8_fnqE3aIpf-XC5LEeMCRKtJBCxOb2RrkFklFpfrZY9EfQLjsFO6QSR5mt9wnlFDkouV72g_DuC4ktbgfJIPk5eOlYASKiDx3SNY31oJAvoIwa86x027RyTznvy6LQ_gtiiMbNJxCtoCQK26EUH6xdbOj_EF1quxaCD3shos0ZnUuu1oRbm6rlVbtpu0xKcyhq8AUGevBp1hkMxxIXuPCCN6In14gMX3QQoUIBzJCGb-A-Dw";
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
    options.RequireHttpsMetadata = false; // Dev only
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
