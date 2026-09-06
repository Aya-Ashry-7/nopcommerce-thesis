using Nop.Core.Caching;
using Nop.Core.Configuration;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// No-op implementation of IStaticCacheManager used to disable server caches for experiments.
    /// It simply executes the acquire function and never stores results.
    /// </summary>
    public class NoopStaticCacheManager : CacheKeyService, IStaticCacheManager
    {
        public NoopStaticCacheManager(AppSettings appSettings) : base(appSettings) { }

        public Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire) => acquire();

        public Task<T> GetAsync<T>(CacheKey key, Func<T> acquire) => Task.FromResult(acquire());

        public Task<T> GetAsync<T>(CacheKey key, T defaultValue = default) => Task.FromResult(defaultValue);

        public Task<object> GetAsync(CacheKey key) => Task.FromResult<object>(null);

        public Task RemoveAsync(CacheKey cacheKey, params object[] cacheKeyParameters) => Task.CompletedTask;

        public Task SetAsync<T>(CacheKey key, T data) => Task.CompletedTask;

        public Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters) => Task.CompletedTask;

        public Task ClearAsync() => Task.CompletedTask;

        public void Dispose() { /* nothing to dispose */ }
    }
}