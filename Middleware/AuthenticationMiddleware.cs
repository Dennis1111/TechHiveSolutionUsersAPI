using System.Text;

namespace UserManagementAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private const string ValidToken = "Bearer your-secret-api-token-12345";

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
                _logger.LogWarning("[AUTH {RequestId}] Missing Authorization header for {Path}", 
                    requestId, context.Request.Path);
                await RespondUnauthorized(context, "Missing authorization header");
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("[AUTH {RequestId}] Empty Authorization header for {Path}", 
                    requestId, context.Request.Path);
                await RespondUnauthorized(context, "Empty authorization header");
                return;
            }

            // Validate token
            if (!IsValidToken(authHeader))
            {
                _logger.LogWarning("[AUTH {RequestId}] Invalid token for {Path}: {Token}", 
                    requestId, context.Request.Path, authHeader);
                await RespondUnauthorized(context, "Invalid or expired token");
                return;
            }

            _logger.LogInformation("[AUTH {RequestId}] Authentication successful for {Path}", 
                requestId, context.Request.Path);

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
            // Simple token validation (in production, use JWT validation)
            return authHeader.Equals(ValidToken, StringComparison.Ordinal);
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

            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}