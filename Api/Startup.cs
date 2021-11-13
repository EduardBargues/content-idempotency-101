using System;
using System.Threading.Tasks;
using Idempotency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service;

namespace Api
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
            services.AddTransient<IImplementation, Implementation>();
            services.AddTransient<IdempotencyFilter>();
            services.AddSingleton<IIdempotentStorage, InMemoryStorage>();
            services.Configure<IdempotencyConfiguration>(config =>
            {
                // Defines the header from where the idempotency-key is read.
                config.HeaderName = "x-idem-key";
                // Defines how much time a cache response is valid.
                config.TimeToLiveDeprecation = TimeSpan.FromSeconds(10);
                // We need to specify a time-to-live in case the api fails to process a request.
                config.TimeToLiveMaster = TimeSpan.FromSeconds(15);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
