using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Controllers; // ← Added using statement

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (request == null)
                return BadRequest("Login data is required");

            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "manager", "manager123" },
                { "developer", "dev123" }
            };

            if (validUsers.ContainsKey(request.Username) && 
                validUsers[request.Username] == request.Password)
            {
                var token = TokenManager.CreateToken(request.Username);

                var response = new LoginResponse
                {
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresIn = TokenManager.ExpirySeconds, // ← Get from TokenManager
                    Username = request.Username,
                    Role = GetUserRole(request.Username)
                };

                _logger.LogInformation("Token created for user: {Username}, expires in {Minutes} minutes", 
                    request.Username, TokenManager.ExpirySeconds / 60);
                return Ok(response);
            }

            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        private static string GetUserRole(string username)
        {
            return username switch
            {
                "admin" => "Administrator",
                "manager" => "Manager", 
                _ => "User"
            };
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public static class TokenManager
    {
        private static readonly Dictionary<string, DateTime> _tokenExpiry = new();
        private const int ExpiryMinutes = 1; // 1 minutes for testing

        // Add this property to expose the expiry time
        public static int ExpirySeconds => ExpiryMinutes * 60;

        public static string CreateToken(string username)
        {
            // Simple, predictable token format
            var token = $"token-{username}";
            _tokenExpiry[token] = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
            return token;
        }

        public static bool IsValidToken(string token)
        {
            if (!_tokenExpiry.TryGetValue(token, out var expiry))
                return false;

            if (DateTime.UtcNow > expiry)
            {
                _tokenExpiry.Remove(token); // Clean up expired token
                return false;
            }

            return true;
        }

        public static void ExtendToken(string token)
        {
            if (_tokenExpiry.ContainsKey(token))
            {
                _tokenExpiry[token] = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
            }
        }

        public static TimeSpan GetTimeRemaining(string token)
        {
            if (_tokenExpiry.TryGetValue(token, out var expiry))
                return expiry - DateTime.UtcNow;
            return TimeSpan.Zero;
        }
    }
}