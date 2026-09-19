using Nop.Data.Diagnostics;

namespace Nop.Web.Infrastructure.Thesis;

public partial interface ISqlCountingScope
{
    IDisposable BeginScope();

    ThesisSqlCommandSnapshot GetSnapshot();
}

public partial class SqlCountingScope : ISqlCountingScope
{
    private readonly ThesisProfilingSettings _thesisProfilingSettings;

    public SqlCountingScope(ThesisProfilingSettings thesisProfilingSettings)
    {
        _thesisProfilingSettings = thesisProfilingSettings;
    }

    public virtual IDisposable BeginScope()
    {
        if (!_thesisProfilingSettings.Enabled || !_thesisProfilingSettings.EnableSqlCounting)
            return NullScope.Instance;

        return ThesisSqlCommandObserver.BeginScope();
    }

    public virtual ThesisSqlCommandSnapshot GetSnapshot()
    {
        return ThesisSqlCommandObserver.GetSnapshot();
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
