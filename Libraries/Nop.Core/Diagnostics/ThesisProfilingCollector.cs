using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Nop.Core.Diagnostics;

/// <summary>
/// Collects per-request thesis profiling metrics in async-local storage.
/// </summary>
public static class ThesisProfilingCollector
{
    private sealed class ScopeState
    {
        public ScopeState(string scenario, string datasetTier, int? productId)
        {
            Scenario = scenario;
            DatasetTier = datasetTier;
            ProductId = productId;
        }

        public string Scenario { get; }
        public string DatasetTier { get; }
        public int? ProductId { get; }
        public ConcurrentDictionary<string, long> TimingsMs { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public ConcurrentDictionary<string, long> Metrics { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    }

    private sealed class ScopeStateContainer
    {
        public ScopeState State { get; set; }
    }

    private static readonly AsyncLocal<ScopeStateContainer> _scopeState = new();

    public static IDisposable BeginScope(string scenario, string datasetTier, int? productId)
    {
        var previousState = _scopeState.Value;
        _scopeState.Value = new ScopeStateContainer
        {
            State = new ScopeState(scenario, datasetTier, productId)
        };

        return new ScopeDisposable(previousState);
    }

    public static bool IsActive => _scopeState.Value?.State != null;

    public static void RecordTiming(string metricName, long elapsedMilliseconds)
    {
        var state = _scopeState.Value?.State;
        if (state == null || elapsedMilliseconds < 0)
            return;

        state.TimingsMs.AddOrUpdate(metricName, elapsedMilliseconds, (_, current) => current + elapsedMilliseconds);
    }

    public static void SetMetric(string metricName, long metricValue)
    {
        var state = _scopeState.Value?.State;
        if (state == null)
            return;

        state.Metrics[metricName] = metricValue;
    }

    public static ThesisProfilingSnapshot GetSnapshot()
    {
        var state = _scopeState.Value?.State;
        if (state == null)
            return ThesisProfilingSnapshot.Empty;

        return new ThesisProfilingSnapshot(
            state.Scenario,
            state.DatasetTier,
            state.ProductId,
            new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(state.TimingsMs, StringComparer.InvariantCultureIgnoreCase)),
            new ReadOnlyDictionary<string, long>(new Dictionary<string, long>(state.Metrics, StringComparer.InvariantCultureIgnoreCase)));
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly ScopeStateContainer _previousState;
        private bool _disposed;

        public ScopeDisposable(ScopeStateContainer previousState)
        {
            _previousState = previousState;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scopeState.Value = _previousState;
            _disposed = true;
        }
    }
}

public sealed record ThesisProfilingSnapshot(
    string Scenario,
    string DatasetTier,
    int? ProductId,
    IReadOnlyDictionary<string, long> TimingsMs,
    IReadOnlyDictionary<string, long> Metrics)
{
    public static ThesisProfilingSnapshot Empty { get; } = new(string.Empty, string.Empty, null,
        new ReadOnlyDictionary<string, long>(new Dictionary<string, long>()),
        new ReadOnlyDictionary<string, long>(new Dictionary<string, long>()));
}
