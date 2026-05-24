using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NatacaoAPI.Data;
using NatacaoAPI.Middleware;
using NatacaoAPI.Models;
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
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient();

// ══════════════════════════════════════════════════════════════════
// 3. MAPSTER — Configuração de mapeamento (substitui AutoMapper vulnerável)
//    CVE-2026-32933: DoS via recursão descontrolada no AutoMapper 13.0.1
// ══════════════════════════════════════════════════════════════════
NatacaoAPI.Profiles.MapsterConfig.RegisterMappings();
builder.Services.AddSingleton(Mapster.TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<MapsterMapper.IMapper, MapsterMapper.ServiceMapper>();

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

// ══════════════════════════════════════════════════════════════════
// 5. RATE LIMITING — Proteção contra força bruta
// ══════════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = builder.Environment.IsDevelopment() ? 1000 : 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// ══════════════════════════════════════════════════════════════════
// 6. CORS — Política explícita para localhost
// ══════════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ══════════════════════════════════════════════════════════════════
// 7. REQUEST SIZE LIMIT — Proteção contra DoS
// ══════════════════════════════════════════════════════════════════
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// Health Check
builder.Services.AddHealthChecks();

// ══════════════════════════════════════════════════════════════════
// 8. CONTROLLERS + SWAGGER
// ══════════════════════════════════════════════════════════════════
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NatacaoAPI",
        Version = "v2",
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
// 9. MIDDLEWARE PIPELINE
//    Ordem importa! Exception handler primeiro, depois auth, depois endpoints.
// ══════════════════════════════════════════════════════════════════

// Middleware global de exceções — primeiro no pipeline para capturar tudo
app.UseMiddleware<GlobalExceptionMiddleware>();

// HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCors();

// Rate Limiting
app.UseRateLimiter();

// Health Check Endpoint
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Define o endpoint do JSON do Swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NatacaoAPI V2");
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

// ══════════════════════════════════════════════════════════════════
// 10. SEED — Criar Usuários padrões na primeira execução (Admin, Professor, Aluno)
// ══════════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (!db.Usuarios.Any(u => u.Email == "admin@natacao.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Administrador",
            Email = "admin@natacao.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            Role = UsuarioRole.Admin,
            DataCriacao = DateTime.UtcNow
        });
        db.SaveChanges();
        logger.LogInformation("Admin seed criado: admin@natacao.com / Admin@123");
    }

    if (!db.Usuarios.Any(u => u.Email == "professor@natacao.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Professor Teste",
            Email = "professor@natacao.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Prof@123!", workFactor: 12),
            Role = UsuarioRole.Professor,
            DataCriacao = DateTime.UtcNow
        });
        db.SaveChanges();
        logger.LogInformation("Professor seed criado: professor@natacao.com / Prof@123!");
    }

    if (!db.Usuarios.Any(u => u.Email == "aluno@natacao.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Aluno Teste",
            Email = "aluno@natacao.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Aluno@123!", workFactor: 12),
            Role = UsuarioRole.Aluno,
            DataCriacao = DateTime.UtcNow
        });
        db.SaveChanges();
        logger.LogInformation("Aluno seed criado: aluno@natacao.com / Aluno@123!");
    }
}

app.Run();

// Tornar a classe Program acessível para testes de integração
public partial class Program { }