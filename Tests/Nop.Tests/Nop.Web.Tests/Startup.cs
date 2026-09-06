using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Caching;
using Nop.Web.Framework.Infrastructure; // NoopStaticCacheManager namespace

namespace Nop.Tests.Nop.Web.Tests
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ---- Add the No-op cache wiring ----
            var cachingEnabled = _configuration.GetValue<bool>("FeatureToggles:ServerCacheEnabled", true);

            if (!cachingEnabled)
            {
                // If AppSettings is registered elsewhere in your test host, let DI construct NoopStaticCacheManager.
                // Otherwise use factory to supply AppSettings from DI (ensure AppSettings is registered).
                services.AddSingleton<IStaticCacheManager, NoopStaticCacheManager>();
                services.AddSingleton<ICacheKeyService>(sp => (ICacheKeyService)sp.GetRequiredService<IStaticCacheManager>());
            }

            // ... other service registrations (if any)
        }

        public void ConfigureContainer(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder application)
        {
        }
    }
}