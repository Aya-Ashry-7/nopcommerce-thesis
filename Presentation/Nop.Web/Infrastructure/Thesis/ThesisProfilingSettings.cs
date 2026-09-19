namespace Nop.Web.Infrastructure.Thesis;

public partial class ThesisProfilingSettings
{
    public bool Enabled { get; set; }

    public bool EnableSqlCounting { get; set; } = true;

    public bool EnableDatasetTools { get; set; }

    public string[] AllowedEnvironments { get; set; } = ["Development", "ThesisTest"];

    public string Scenario { get; set; } = "Baseline";

    public string DatasetTier { get; set; } = "Tier0";

    public string OutputDirectory { get; set; } = "App_Data/ThesisProfiling";

    public string MeasurementsFileName { get; set; } = "measurements.jsonl";

    public string DatasetManifestFileName { get; set; } = "dataset-manifest.json";

    public string DatasetMarker { get; set; } = "THESIS-REV-2026-A";

    public int RandomSeed { get; set; } = 2026;

    public int MaxCustomerPoolSize { get; set; } = 64;
}
