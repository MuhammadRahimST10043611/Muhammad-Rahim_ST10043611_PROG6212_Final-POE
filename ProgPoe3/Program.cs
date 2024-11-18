// Name & Surname: Muhammad Rahim
// Student Number: ST10043611
// Group: Group 3

// Description: This file contains the entry point of the application. It configures and runs the web application.
// References: 
// - https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-9.0
// - https://www.postman.com/
// - https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide
// - https://learn.microsoft.com/en-us/ef/core/
// - https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/
// - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-9.0
// - https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-9.0
// - https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-9.0
// - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-9.0
// - https://learn.microsoft.com/en-us/aspnet/core/razor-pages/?view=aspnetcore-9.0&tabs=visual-studio
// - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-9.0&tabs=windows
// - https://getbootstrap.com/docs/5.3/getting-started/introduction/
// - https://www.w3schools.com/cssref/index.php
// - https://api.jquery.com/

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using ProgPoe3.Data;

namespace ProgPoe3
{
    public class Program
    {
        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Entry point of the application. Configures and runs the web application.
        /// </summary>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            ConfigureServices(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            Configure(app);

            app.Run();
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Configures services for the application.
        /// </summary>
        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Adding controllers and views (MVC support)
            builder.Services.AddControllersWithViews();

            // Adding support for API controllers
            builder.Services.AddControllers();

            // Session configuration
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // DbContext configuration for SQL Server
            builder.Services.AddDbContext<ClaimDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // File upload limits configuration
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5 MB
            });
        }

        //------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Configures the HTTP request pipeline for the application.
        /// </summary>
        private static void Configure(WebApplication app)
        {
            // Error handling for production
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // HTTPS redirection and static files
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Request routing
            app.UseRouting();

            // Authorization middleware
            app.UseAuthorization();

            // Enabling session management
            app.UseSession();

            // Error handling for large file uploads
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error is BadHttpRequestException exception &&
                        exception.Message.Contains("Multipart body length limit"))
                    {
                        context.Response.StatusCode = StatusCodes.Status413RequestEntityTooLarge;
                        await context.Response.WriteAsync("The uploaded file is too large. Maximum allowed size is 5 MB.");
                    }
                });
            });

            // Mapping API controllers
            app.MapControllers();

            // Default routing for MVC
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Login}/{id?}");
        }
    }
}
//------------------------------------------...ooo000 END OF FILE 000ooo...------------------------------------------------------//
