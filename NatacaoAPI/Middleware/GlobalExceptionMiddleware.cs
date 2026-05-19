using System.Net;
using System.Text.Json;

namespace NatacaoAPI.Middleware
{
    /// <summary>
    /// Middleware global de tratamento de exceções.
    /// 
    /// Decisão arquitetural: centralizar o tratamento de erros aqui para:
    /// 1. Nunca vazar stack traces para o cliente (segurança)
    /// 2. Padronizar o formato de erro em toda a API
    /// 3. Manter os Controllers limpos, sem try/catch repetitivos
    /// 
    /// Mapeia tipos de exceção para códigos HTTP apropriados:
    /// - InvalidOperationException → 400 (regras de negócio violadas)
    /// - UnauthorizedAccessException → 401/403
    /// - KeyNotFoundException → 404
    /// - Qualquer outra → 500
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção não tratada: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno no servidor.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                status = (int)statusCode,
                message,
                // Em produção, nunca expor detalhes internos
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
