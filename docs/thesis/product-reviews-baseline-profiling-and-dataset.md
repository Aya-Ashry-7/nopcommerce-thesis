# Product Reviews Baseline Profiling and Dataset

## Scope
This workflow provides development-only tooling for:

- timing instrumentation on Product Details review-related paths,
- Linq2DB SQL command counting,
- machine-readable measurement output,
- deterministic Product Reviews dataset generation and cleanup.

It intentionally excludes caching and lazy-loading implementation.

## Prerequisites
- Use `Development` or `ThesisTest` environment.
- Ensure `Presentation/Nop.Web/App_Data/appsettings.Development.json` has `ThesisProfiling.Enabled=true`.
- Use explicit product IDs for dataset generation.

## Dataset generation
Use the admin endpoint:

- `POST /Admin/ThesisProfiling/GenerateDataset`

Request body example:

```json
{
  "productIds": [12, 19, 41],
  "datasetTier": "Tier1",
  "reviewsPerProduct": 50,
  "customerPoolSize": 40,
  "storeIds": [1]
}
```

Behavior:
- Generates deterministic approved reviews only for selected products.
- Adds thesis marker to title/text.
- Avoids duplicates by checking existing marker reviews.
- Recomputes product review totals once per affected product.
- Writes `App_Data/ThesisProfiling/dataset-manifest.json`.

## Dataset cleanup
Use:

- `POST /Admin/ThesisProfiling/CleanupDataset`

Request body example:

```json
{
  "productIds": [12, 19, 41],
  "datasetTier": "Tier1"
}
```

Behavior:
- Deletes only generated thesis reviews (marker + tier + selected products).
- Idempotent on rerun.
- Recomputes product review totals once per affected product.

## Measurements output
Measurements are written to:

- `App_Data/ThesisProfiling/measurements.jsonl`

Each line stores a JSON record with timestamp, scenario, dataset tier, product ID, method timings, and SQL command counts.

## Recommended calibration tiers
- Tier0: existing data
- Tier1: 50 reviews/product
- Tier2: 250 reviews/product
- Tier3: 1000 reviews/product

Use the same selected product IDs, customer pool, store IDs, marker, and random seed across all scenarios.
