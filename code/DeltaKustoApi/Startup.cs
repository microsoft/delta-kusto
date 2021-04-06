using DeltaKustoApi.Controllers.ClientVersion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaKustoApi
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
            services.AddMemoryCache();
            services.AddControllers();
            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeltaKustoApi", Version = "v1" });
            //});

            //  Dependency injection
            //  Serilog
            string? connectionString = GetEnvironmentVariable("storageConnectionString");
            var container = GetEnvironmentVariable("telemetryContainerName");
            var environment = GetEnvironmentVariable("env");
            var logger = new LoggerConfiguration()
                .WriteTo
                .Async(c => c.AzureBlobStorage(
                    connectionString,
                    LogEventLevel.Verbose,
                    container,
                    $"raw-telemetry/{environment}/{{yyyy}}-{{MM}}-{{dd}}-log.txt",
                    blobSizeLimitBytes: 200 * 1024 * 1024,
                    writeInBatches: true,
                    period: TimeSpan.FromSeconds(10)))
                .CreateLogger();

            services.TryAddSingleton(new TelemetryWriter(logger));
            services.TryAddSingleton<ClientVersionCacheProxy>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseSwagger();
                //app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeltaKustoApi v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static string GetEnvironmentVariable(string variable)
        {
            var variableValue = Environment.GetEnvironmentVariable(variable);

            if (string.IsNullOrWhiteSpace(variableValue))
            {
                throw new ArgumentNullException(variable);
            }

            return variableValue;
        }
    }
}