using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Stores;

namespace Nop.Web.Infrastructure.Thesis;

public partial class ThesisReviewDatasetService : IThesisReviewDatasetService
{
    private readonly ICustomerService _customerService;
    private readonly IMeasurementWriter _measurementWriter;
    private readonly IProductReviewService _productReviewService;
    private readonly IProductService _productService;
    private readonly IRepository<ProductReview> _productReviewRepository;
    private readonly IStoreService _storeService;
    private readonly ThesisProfilingSettings _thesisProfilingSettings;

    public ThesisReviewDatasetService(ICustomerService customerService,
        IMeasurementWriter measurementWriter,
        IProductReviewService productReviewService,
        IProductService productService,
        IRepository<ProductReview> productReviewRepository,
        IStoreService storeService,
        ThesisProfilingSettings thesisProfilingSettings)
    {
        _customerService = customerService;
        _measurementWriter = measurementWriter;
        _productReviewService = productReviewService;
        _productService = productService;
        _productReviewRepository = productReviewRepository;
        _storeService = storeService;
        _thesisProfilingSettings = thesisProfilingSettings;
    }

    public virtual async Task<ThesisDatasetGenerationResult> GenerateAsync(ThesisDatasetGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productIds = request.ProductIds.Distinct().OrderBy(id => id).ToArray();
        if (!productIds.Any())
            return new ThesisDatasetGenerationResult { Message = "No product IDs were provided." };

        var products = (await _productService.GetProductsByIdsAsync(productIds))
            .Where(product => product != null && !product.Deleted && product.Published && product.AllowCustomerReviews)
            .OrderBy(product => product.Id)
            .ToList();

        if (!products.Any())
            return new ThesisDatasetGenerationResult { Message = "No suitable products were found for generation." };

        var availableStores = (await _storeService.GetAllStoresAsync())
            .OrderBy(store => store.Id)
            .ToList();

        var storeIds = request.StoreIds?.Any() == true
            ? availableStores.Where(store => request.StoreIds.Contains(store.Id)).Select(store => store.Id).Distinct().OrderBy(id => id).ToArray()
            : availableStores.Select(store => store.Id).Distinct().OrderBy(id => id).ToArray();

        if (!storeIds.Any())
            return new ThesisDatasetGenerationResult { Message = "No suitable stores were found for generation." };

        var maxCustomerPoolSize = request.CustomerPoolSize > 0
            ? request.CustomerPoolSize
            : _thesisProfilingSettings.MaxCustomerPoolSize;

        var customers = (await _customerService.GetAllCustomersAsync(isActive: true, pageIndex: 0, pageSize: int.MaxValue))
            .Where(customer => !customer.Deleted && customer.Active && !customer.IsSystemAccount)
            .OrderBy(customer => customer.Id)
            .Take(Math.Max(maxCustomerPoolSize, 1))
            .ToList();

        if (!customers.Any())
            return new ThesisDatasetGenerationResult { Message = "No suitable customers were found for generation." };

        var generatedReviews = new List<ProductReview>();
        var createdByProduct = new Dictionary<int, int>();
        var deterministicStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var product in products)
        {
            var existingReviews = await _productReviewService.GetAllProductReviewsAsync(productId: product.Id, approved: null, showHidden: true);
            var existingGeneratedReviews = existingReviews
                .Where(review => IsGeneratedReview(review, request.DatasetTier))
                .OrderBy(review => review.Id)
                .ToList();

            var reviewsToCreate = Math.Max(request.ReviewsPerProduct - existingGeneratedReviews.Count, 0);
            createdByProduct[product.Id] = 0;

            for (var i = 0; i < reviewsToCreate; i++)
            {
                var reviewIndex = existingGeneratedReviews.Count + i;
                var customer = customers[reviewIndex % customers.Count];
                var storeId = storeIds[reviewIndex % storeIds.Length];
                var rating = (reviewIndex % 5) + 1;

                var titleBucket = reviewIndex % 3;
                var reviewTextBucket = reviewIndex % 4;

                generatedReviews.Add(new ProductReview
                {
                    ProductId = product.Id,
                    CustomerId = customer.Id,
                    StoreId = storeId,
                    IsApproved = true,
                    Rating = rating,
                    HelpfulYesTotal = 0,
                    HelpfulNoTotal = 0,
                    CreatedOnUtc = deterministicStartDate.AddMinutes(product.Id * 10_000L + reviewIndex),
                    Title = BuildDeterministicTitle(product.Id, request.DatasetTier, reviewIndex, titleBucket),
                    ReviewText = BuildDeterministicReviewText(product.Id, request.DatasetTier, reviewIndex, reviewTextBucket),
                    ReplyText = string.Empty,
                    CustomerNotifiedOfReply = false
                });

                createdByProduct[product.Id]++;
            }
        }

        if (generatedReviews.Any())
            await _productReviewRepository.InsertAsync(generatedReviews);

        foreach (var product in products)
            await _productReviewService.UpdateProductReviewTotalsAsync(product);

        var generationResult = new ThesisDatasetGenerationResult
        {
            Succeeded = true,
            Message = "Dataset generation completed.",
            CreatedReviewCount = generatedReviews.Count,
            ProductReviewCountMap = createdByProduct
        };

        await _measurementWriter.WriteDatasetManifestAsync(new
        {
            generatedOnUtc = DateTime.UtcNow,
            marker = _thesisProfilingSettings.DatasetMarker,
            randomSeed = _thesisProfilingSettings.RandomSeed,
            datasetTier = request.DatasetTier,
            selectedProductIds = products.Select(product => product.Id).ToArray(),
            selectedStoreIds = storeIds,
            selectedCustomerIds = customers.Select(customer => customer.Id).ToArray(),
            reviewsPerProduct = request.ReviewsPerProduct,
            createdByProduct = createdByProduct,
            totalCreated = generatedReviews.Count
        });

        return generationResult;
    }

    public virtual async Task<ThesisDatasetCleanupResult> CleanupAsync(ThesisDatasetCleanupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var productIds = request.ProductIds.Distinct().OrderBy(id => id).ToArray();
        if (!productIds.Any())
            return new ThesisDatasetCleanupResult { Message = "No product IDs were provided." };

        var products = (await _productService.GetProductsByIdsAsync(productIds))
            .Where(product => product != null)
            .OrderBy(product => product.Id)
            .ToList();

        if (!products.Any())
            return new ThesisDatasetCleanupResult { Message = "No products were found for cleanup." };

        var reviewsToDelete = new List<ProductReview>();
        var deletedByProduct = new Dictionary<int, int>();

        foreach (var product in products)
        {
            var reviews = await _productReviewService.GetAllProductReviewsAsync(productId: product.Id, approved: null, showHidden: true);
            var generatedReviews = reviews
                .Where(review => IsGeneratedReview(review, request.DatasetTier))
                .ToList();

            deletedByProduct[product.Id] = generatedReviews.Count;
            reviewsToDelete.AddRange(generatedReviews);
        }

        if (reviewsToDelete.Any())
            await _productReviewService.DeleteProductReviewsAsync(reviewsToDelete);

        foreach (var product in products)
            await _productReviewService.UpdateProductReviewTotalsAsync(product);

        return new ThesisDatasetCleanupResult
        {
            Succeeded = true,
            Message = "Dataset cleanup completed.",
            DeletedReviewCount = reviewsToDelete.Count,
            ProductReviewCountMap = deletedByProduct
        };
    }

    protected virtual bool IsGeneratedReview(ProductReview review, string datasetTier)
    {
        if (review == null)
            return false;

        var marker = _thesisProfilingSettings.DatasetMarker ?? string.Empty;
        if (string.IsNullOrWhiteSpace(marker))
            return false;

        var tierTag = string.IsNullOrWhiteSpace(datasetTier) ? string.Empty : $"[{datasetTier}]";

        return (!string.IsNullOrEmpty(review.Title) &&
                review.Title.Contains(marker, StringComparison.InvariantCultureIgnoreCase) &&
                (string.IsNullOrEmpty(tierTag) || review.Title.Contains(tierTag, StringComparison.InvariantCultureIgnoreCase)))
               || (!string.IsNullOrEmpty(review.ReviewText) &&
                   review.ReviewText.Contains(marker, StringComparison.InvariantCultureIgnoreCase) &&
                   (string.IsNullOrEmpty(tierTag) || review.ReviewText.Contains(tierTag, StringComparison.InvariantCultureIgnoreCase)));
    }

    protected virtual string BuildDeterministicTitle(int productId, string datasetTier, int index, int bucket)
    {
        var marker = _thesisProfilingSettings.DatasetMarker;

        return bucket switch
        {
            0 => $"{marker} [{datasetTier}] Product {productId} Review {index:D5}",
            1 => $"{marker} [{datasetTier}] P{productId} Deterministic Baseline Review #{index:D5}",
            _ => $"{marker} [{datasetTier}] P{productId} Controlled Review Sequence {index:D5}"
        };
    }

    protected virtual string BuildDeterministicReviewText(int productId, string datasetTier, int index, int bucket)
    {
        var marker = _thesisProfilingSettings.DatasetMarker;

        var baseText =
            $"{marker} [{datasetTier}] Deterministic synthetic review for product {productId}. Sequence {index:D5}. ";

        var targetLength = bucket switch
        {
            0 => 120,
            1 => 180,
            2 => 260,
            _ => 340
        };

        var filler = "This review content is generated for thesis calibration and remains deterministic across scenarios. ";
        while (baseText.Length < targetLength)
            baseText += filler;

        if (baseText.Length > targetLength)
            baseText = baseText[..targetLength];

        return baseText;
    }
}
