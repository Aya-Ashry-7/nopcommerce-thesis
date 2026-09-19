namespace Nop.Web.Infrastructure.Thesis;

public partial record MeasurementRecord
{
    public DateTime MeasuredOnUtc { get; init; }

    public string CommitSha { get; init; }

    public string Scenario { get; init; }

    public string DatasetTier { get; init; }

    public int? ProductId { get; init; }

    public long? ReviewCount { get; init; }

    public IDictionary<string, long> TimingsMs { get; init; } = new Dictionary<string, long>();

    public long TotalSqlCommandCount { get; init; }

    public long ReviewSqlCommandCount { get; init; }

    public long? ResponseSizeBytes { get; init; }

    public string MachineSpecification { get; init; }

    public string SqlServerVersion { get; init; }

    public string BrowserVersion { get; init; }

    public string ApplicationConfiguration { get; init; }
}
