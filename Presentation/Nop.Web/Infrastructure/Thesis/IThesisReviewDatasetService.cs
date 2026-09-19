namespace Nop.Web.Infrastructure.Thesis;

public partial interface IThesisReviewDatasetService
{
    Task<ThesisDatasetGenerationResult> GenerateAsync(ThesisDatasetGenerationRequest request);

    Task<ThesisDatasetCleanupResult> CleanupAsync(ThesisDatasetCleanupRequest request);
}

public partial record ThesisDatasetGenerationRequest
{
    public IList<int> ProductIds { get; init; } = new List<int>();

    public string DatasetTier { get; init; }

    public int ReviewsPerProduct { get; init; }

    public int CustomerPoolSize { get; init; }

    public IList<int> StoreIds { get; init; } = new List<int>();
}

public partial record ThesisDatasetCleanupRequest
{
    public IList<int> ProductIds { get; init; } = new List<int>();

    public string DatasetTier { get; init; }
}

public partial record ThesisDatasetGenerationResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; }

    public int CreatedReviewCount { get; init; }

    public IDictionary<int, int> ProductReviewCountMap { get; init; } = new Dictionary<int, int>();
}

public partial record ThesisDatasetCleanupResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; }

    public int DeletedReviewCount { get; init; }

    public IDictionary<int, int> ProductReviewCountMap { get; init; } = new Dictionary<int, int>();
}
