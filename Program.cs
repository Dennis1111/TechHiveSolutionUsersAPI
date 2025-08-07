using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your services
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORRECT MIDDLEWARE ORDER - CRITICAL FOR PROPER FUNCTIONING!
app.UseMiddleware<ErrorHandlingMiddleware>();    // FIRST - Catch all exceptions
app.UseMiddleware<AuthenticationMiddleware>();   // SECOND - Validate tokens  
app.UseMiddleware<RequestLoggingMiddleware>();   // LAST - Log everything

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();