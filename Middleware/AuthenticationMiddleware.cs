using System.Text;
using System.Text.Json;
using UserManagementAPI.Controllers;

namespace UserManagementAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private const string ValidToken = "Bearer your-secret-api-token-12345";

        // Create static instance - reused for all requests
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "unknown";
            
            // Skip authentication for certain paths
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Check for Authorization header
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("[AUTH {RequestId}] Missing Authorization header", requestId);
                await RespondUnauthorized(context, "Missing authorization header");
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("[AUTH {RequestId}] Empty Authorization header", requestId);
                await RespondUnauthorized(context, "Empty authorization header");
                return;
            }

            if (!IsValidToken(authHeader))
            {
                _logger.LogWarning("[AUTH {RequestId}] Invalid or expired token", requestId);
                await RespondUnauthorized(context, "Invalid or expired token");
                return;
            }

            // 🚀 EXTEND TOKEN ON VALID REQUEST (Auto-renewal feature!)
            var token = authHeader.Substring(7);
            TokenManager.ExtendToken(token);
            
            var timeRemaining = TokenManager.GetTimeRemaining(token);
            _logger.LogInformation("[AUTH {RequestId}] Token extended, {Minutes} minutes remaining", 
                requestId, Math.Round(timeRemaining.TotalMinutes, 1));

            // Token is valid, continue to next middleware
            await _next(context);
        }

        private static bool ShouldSkipAuthentication(string path)
        {
            var publicPaths = new[]
            {
                "/swagger",
                "/api/auth/login",
                "/health"
            };

            return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsValidToken(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring(7); // Remove "Bearer " prefix
            return TokenManager.IsValidToken(token);
        }

        private static async Task RespondUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                title = "Unauthorized",
                status = 401,
                detail = message,
                timestamp = DateTime.UtcNow
            };

            // Use the static instance instead of creating new one
            var json = JsonSerializer.Serialize(errorResponse, JsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}