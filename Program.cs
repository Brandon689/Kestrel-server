using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Kestrel
        builder.WebHost.UseKestrel(options =>
        {
            options.ListenLocalhost(5000);
        });

        // Add logging
        builder.Logging.AddConsole();

        // Add CORS services
        builder.Services.AddCors();

        var app = builder.Build();

        // Create a logger
        var logger = app.Logger;

        // Middleware for logging requests
        app.Use(async (context, next) =>
        {
            logger.LogInformation($"Request {context.Request.Method} {context.Request.Path}");
            await next.Invoke();
        });

        // Basic authentication middleware
        app.Use(async (context, next) =>
        {
            string authHeader = context.Request.Headers.Authorization;
            if (authHeader != null && authHeader.StartsWith("Basic"))
            {
                // Very basic auth check - in real world, use a more secure method
                if (authHeader == "Basic dXNlcjpwYXNz") // base64 of "user:pass"
                {
                    await next.Invoke();
                    return;
                }
            }

            context.Response.StatusCode = 401; //Unauthorized
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Secure Area\"");
            await context.Response.WriteAsync("Unauthorized");
        });

        // Enable CORS
        app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        // Enable serving static files
        app.UseStaticFiles();

        // Root route
        app.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("Hello from Kestrel without ASP.NET!");
        });

        // Route with parameter
        app.MapGet("/hello/{name}", async (string name, HttpContext context) =>
        {
            await context.Response.WriteAsync($"Hello, {name}!");
        });

        // JSON response
        app.MapGet("/api/data", async (HttpContext context) =>
        {
            var data = new { Message = "Hello, World!", Date = DateTime.Now };
            await context.Response.WriteAsJsonAsync(data);
        });

        // Handle POST request
        app.MapPost("/api/post", async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            await context.Response.WriteAsync($"Received: {body}");
        });

        // Query parameters
        app.MapGet("/search", async (HttpContext context) =>
        {
            var query = context.Request.Query["q"].ToString();
            await context.Response.WriteAsync($"Searching for: {query}");
        });

        // Multiple HTTP methods
        app.MapMethods("/api/resource", new[] { "GET", "POST", "PUT", "DELETE" }, async (HttpContext context) =>
        {
            var method = context.Request.Method;
            await context.Response.WriteAsync($"You sent a {method} request");
        });

        // Health check
        app.MapGet("/health", async (HttpContext context) =>
        {
            await context.Response.WriteAsync("Healthy");
        });

        await app.RunAsync();
    }
}
