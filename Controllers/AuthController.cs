using Microsoft.AspNetCore.Mvc;

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

            // Demo users for testing middleware
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "manager", "manager123" },
                { "developer", "dev123" }
            };

            if (validUsers.ContainsKey(request.Username) && 
                validUsers[request.Username] == request.Password)
            {
                var response = new LoginResponse
                {
                    Token = "your-secret-api-token-12345",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    Username = request.Username,
                    Role = GetUserRole(request.Username)
                };

                _logger.LogInformation("Successful login for user: {Username}", request.Username);
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
}