using System.Diagnostics;
using System.Text;

namespace UserManagementAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly string _logFilePath;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            
            // Create logs directory if it doesn't exist
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(logsDir);
            _logFilePath = Path.Combine(logsDir, $"api-requests-{DateTime.Now:yyyy-MM-dd}.log");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            context.Items["RequestId"] = requestId;

            // Log incoming request
            await LogRequest(context, requestId);

            // Capture response body
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                // Log outgoing response
                await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);
            }
        }

        private async Task LogRequest(HttpContext context, string requestId)
        {
            var request = context.Request;
            var headerString = string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value.ToArray())}"));
            
            var requestInfo = $"[REQUEST {requestId}]\n" +
                              $"Method: {request.Method}\n" +
                              $"Path: {request.Path}\n" +
                              $"Query: {request.QueryString}\n" +
                              $"Headers: {headerString}\n";

            // Log request body for POST/PUT requests
            if (request.Method == "POST" || request.Method == "PUT")
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                requestInfo += $"Body: {body}\n";
                request.Body.Position = 0;
            }

            _logger.LogInformation(requestInfo);
            
            // Also write to file
            await File.AppendAllTextAsync(_logFilePath, 
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {requestInfo}\n");
        }

        private async Task LogResponse(HttpContext context, string requestId, long elapsedMs)
        {
            var response = context.Response;
            var headerString = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value.ToArray())}"));
            
            var responseInfo = $"[RESPONSE {requestId}]\n" +
                               $"Status Code: {response.StatusCode}\n" +
                               $"Elapsed Time: {elapsedMs} ms\n" +
                               $"Headers: {headerString}\n";

            // Log response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            
            if (!string.IsNullOrEmpty(body))
            {
                responseInfo += $"Body: {body}\n";
            }

            _logger.LogInformation(responseInfo);
            
            // Also write to file
            await File.AppendAllTextAsync(_logFilePath, 
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {responseInfo}\n");
        }
    }
}