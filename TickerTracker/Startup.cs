using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = "/404";
                    await next();
                }
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

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
                    "/404",
                    new { controller = "HttpError", action = "_404" }
                );

                endpoints.MapControllerRoute(
                    "rest-hello",
                    "/api/hello",
                    new { controller = "RestHello", action = "Index" }
                );

                // add more rest ctrls later
            });
        }
    }
}
