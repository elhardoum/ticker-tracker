using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TickerTracker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Hangfire cron jobs
            var connStr = Models.Database.Instance().getConnectionString();
            GlobalConfiguration.Configuration.UseSqlServerStorage(connStr);

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connStr, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            // Add the processing server as IHostedService
            services.AddHangfireServer();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            var errPath = "/" + System.Guid.NewGuid().ToString("N");

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = errPath;
                    await next();
                }
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.Use(async (context, next) => {
                context.Request.Cookies.TryGetValue("sid", out string sid);

                if ( string.IsNullOrEmpty(sid) )
                {
                    context.Items["user"] = null;
                } else
                {
                    context.Items["user"] = await Models.Users.findOne("SessionId", sid);
                }

                await next();
            });

            // auth protection
            app.Use(async (context, next) => {
                if (null == context.Items["user"])
                {
                    if (context.Request.RouteValues.TryGetValue("authProtected", out object authProtected))
                    {
                        if (Boolean.Parse(authProtected.ToString()) && null == context.Items["user"])
                        {
                            context.Response.Redirect( Models.Util.Url("/auth/notice", context.Request) );
                            return;
                        }
                    }
                }

                await next();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "home",
                    "/",
                    new { controller = "Home", action = "Index" }
                );

                endpoints.MapControllerRoute(
                    "privacy",
                    "/privacy",
                    new { controller = "Home", action = "Privacy" }
                );

                endpoints.MapControllerRoute(
                    "404",
                    errPath,
                    new { controller = "HttpError", action = "_404" }
                );

                endpoints.MapControllerRoute(
                    "About",
                    "/about",
                    new { controller = "Home", action = "About" }
                );

                endpoints.MapControllerRoute(
                    "auth-redirect",
                    "/auth/redirect",
                    new { controller = "TwitterAuth", action = "Redirect" }
                );

                endpoints.MapControllerRoute(
                    "auth-callback",
                    "/auth/callback",
                    new { controller = "TwitterAuth", action = "Callback" }
                );

                endpoints.MapControllerRoute(
                    "logout",
                    "/auth/logout",
                    new { controller = "TwitterAuth", action = "Logout" }
                );

                endpoints.MapControllerRoute(
                    "auth-notice",
                    "/auth/notice",
                    new { controller = "HttpError", action = "_401" }
                );

                endpoints.MapControllerRoute(
                    "portfolio",
                    "/portfolio",
                    new { controller = "Portfolio", action = "Index", authProtected = true }
                );

                endpoints.MapControllerRoute(
                    "portfolio-create",
                    "/portfolio/create",
                    new { controller = "Portfolio", action = "Create", authProtected = true }
                );

                endpoints.MapControllerRoute(
                    "portfolio-edit",
                    "/portfolio/edit/{editId}",
                    new { controller = "Portfolio", action = "Update", authProtected = true },
                    constraints: new { editId = "\\d+" }
                );

                endpoints.MapControllerRoute(
                    "supported-tickers",
                    "/about/supported-ticker-symbols/stocks-etfs",
                    new { controller = "SupportedTickers", action = "Stocks" }
                );

                endpoints.MapControllerRoute(
                    "supported-tickers",
                    "/about/supported-ticker-symbols/cryptocurrencies",
                    new { controller = "SupportedTickers", action = "Crypto" }
                );
            });

            // cron jobs
            var cron = new Models.Cron();

            // cleanup
            RecurringJob.RemoveIfExists("fetch-stocks");
            RecurringJob.RemoveIfExists("fetch-crypto");

            // initial run
            Task.Run(() => cron.FetchStocks());
            Task.Run(() => cron.FetchCrypto());

            RecurringJob.AddOrUpdate("fetch-stocks", () => cron.FetchStocks(), "*/2 * * * *");
            RecurringJob.AddOrUpdate("fetch-crypto", () => cron.FetchCrypto(), "*/2 * * * *");
        }
    }
}
