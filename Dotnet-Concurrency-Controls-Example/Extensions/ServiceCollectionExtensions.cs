using Dotnet_Concurrency_Controls.Data;
using Dotnet_Concurrency_Controls.Services.Contract;
using Dotnet_Concurrency_Controls.Services.Implementation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Dotnet_Concurrency_Controls.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Configuraion(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalR();

            // Database
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<IdentityUser>(options => 
                options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 3;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 2;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(300);
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
            });

            // Redis
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")));

            // Choose locking mechanism
            services.AddTransient<IBookingLockService, SqlLockService>();
            //services.AddScoped<IBookingLockService, RedisLockService>();
            //services.AddScoped<IBookingLockService, SemaphoreLockService>();

            services.AddControllersWithViews();
            return services;
        }
    }
}
