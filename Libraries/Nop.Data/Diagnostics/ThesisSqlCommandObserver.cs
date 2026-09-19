using System.Threading;

namespace Nop.Data.Diagnostics;

/// <summary>
/// Collects per-request SQL command counts for thesis profiling.
/// </summary>
public static class ThesisSqlCommandObserver
{
    private sealed class ScopeState
    {
        public int TotalCommandCount;
        public int ReviewCommandCount;
    }

    private sealed class ScopeStateContainer
    {
        public ScopeState State { get; set; }
    }

    private static readonly AsyncLocal<ScopeStateContainer> _scopeState = new();

    public static bool IsActive => _scopeState.Value?.State != null;

    public static IDisposable BeginScope()
    {
        var previousState = _scopeState.Value;
        _scopeState.Value = new ScopeStateContainer
        {
            State = new ScopeState()
        };

        return new ScopeDisposable(previousState);
    }

    public static void ObserveCommand(string sqlText)
    {
        var state = _scopeState.Value?.State;
        if (state == null)
            return;

        Interlocked.Increment(ref state.TotalCommandCount);

        if (!string.IsNullOrEmpty(sqlText) &&
            (sqlText.Contains("ProductReview", StringComparison.InvariantCultureIgnoreCase) ||
             sqlText.Contains("productreview", StringComparison.InvariantCultureIgnoreCase)))
        {
            Interlocked.Increment(ref state.ReviewCommandCount);
        }
    }

    public static ThesisSqlCommandSnapshot GetSnapshot()
    {
        var state = _scopeState.Value?.State;
        if (state == null)
            return ThesisSqlCommandSnapshot.Empty;

        return new ThesisSqlCommandSnapshot(state.TotalCommandCount, state.ReviewCommandCount);
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

public readonly record struct ThesisSqlCommandSnapshot(int TotalCommandCount, int ReviewCommandCount)
{
    public static ThesisSqlCommandSnapshot Empty { get; } = new(0, 0);
}
