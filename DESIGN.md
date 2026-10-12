# DESIGN

## Key decisions

- **PostgreSQL + binary COPY seeding:** The assignment dataset (~50k rows) is small enough for fast relational querying with indexes. Binary COPY minimizes seed time and avoids row-by-row insert overhead.
- **Layered backend (`Controllers -> Services -> Repositories`):** Keeps API contracts, validation, business rules, and data access separated but lightweight.
- **Shared filter model and query pipeline:** `RowsRequestValidator` + `AgGridFilterParser` + `FilterCriteria` + `PositionQueryBuilder` are reused by both rows and aggregation endpoints so totals and rows stay consistent.
- **AG Grid Community Infinite Row Model:** Fully server-backed paging/sorting/filtering; no Enterprise features required.
- **Seeding over EF migrations (for this scope):** Database schema/data initialization is handled by startup DDL + CSV seeding. EF Core migrations were intentionally not used because they are not required by the assignment and the schema is simple/stable for this time-boxed delivery.

## Handling large datasets smoothly

- Backend does filtering/sorting/paging in SQL.
- Stable ordering includes a secondary `trade_id` sort to keep pagination deterministic.
- Requests are parameterized and columns are whitelisted to prevent SQL injection.
- Frontend datasource cancels stale in-flight requests on fast user interactions.

## Position-units rule

Position is a signed quantity but units differ by commodity. To avoid misleading totals:

- Position totals are only returned when `groupBy=commodity`.
- For other groupings, position is omitted (`null`) and the response includes a note that mixed units are not comparable.
- Frontend summary mirrors this behavior and never shows position in grand totals.

## If backed by OpenSearch (future)

- Index risk positions with keyword fields for `book`, `region`, `instrumentType`, `currency` and date/number mappings for market fields.
- Use text analyzers (plus n-gram edge indexing) for `counterparty` and `commodity` fuzzy search.
- Implement filters with `bool.filter` clauses; text search with `multi_match`/`query_string` as needed.
- Aggregations: `terms` on selected group key + `sum` for `pnl` and `fxExposure`; conditional position aggregate only for commodity.
- Deep paging: `search_after` instead of large from/size offsets.

## Improvements with more time

- Integration tests against a disposable PostgreSQL container.
- Persisted column/filter view state.
- Summary chart and code-splitting in frontend to reduce bundle size warnings.
