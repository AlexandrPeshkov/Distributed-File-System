using DFS.Balancer.Gateway;
using DFS.Balancer.Models;
using DFS.Balancer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

namespace DFS.Journal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DFS API", Version = "v1" });
                c.IncludeXmlComments($@"{AppDomain.CurrentDomain.BaseDirectory}\DFS.Balancer.XML");
            });

            var nodeConfiguration = Configuration.GetSection(nameof(BalancerConfiguration));

            services.Configure<BalancerConfiguration>(nodeConfiguration);
            services.AddSingleton<BalancerService>();
            services.AddSingleton<NodeGateway>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BalancerService balancerService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "DFS Node API");
                c.RoutePrefix = string.Empty;
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}