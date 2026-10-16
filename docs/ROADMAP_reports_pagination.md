# Roadmap: Reports & Pagination

Status/working plan — phases are independent and can ship in any order but
P1+P2 come first because reports build on them.

## Pagination (P1–P2)

Today every list endpoint returns the full table (`ToListAsync()`), the
Flutter providers load all rows into memory, and no endpoint supports search
(except SalesOrderHeader's `from_date`/`to_date`). Movement and sales history
grow unbounded, so this breaks first.

### P1 — Backend paging infrastructure

1. Shared `PagedResult<T>` envelope: `{ data, page, pageSize, totalCount }`.
   - `ApiService._handleResponse` currently unwraps `$values` into plain
     lists; teach it to also recognize the envelope (or expose
     `getPaged<T>()` next to the existing `get<T>()`).
   - Query params: `page`, `page_size` (snake_case per repo convention).
     Defaults: `page=1`, `page_size=50`, cap 200.
2. Apply to the high-volume endpoints first, one controller per commit so
   app + API ship together:
   - `/InventoryMovement` (unbounded log) — add `from_date`/`to_date` while here
   - `/SalesOrderHeader` (already has dates) + details lookup
   - `/Product`, `/Customer` (+ a `q` name-search param while touching them)
3. EF specifics: `EF.Functions`… none needed; deterministic `OrderBy`
   (Id or date DESC) **before** `Skip/Take` (MySQL requires it), a single
   `CountAsync()` for the total, `AsNoTracking()` on all reads.
4. Reference tables stay unpaginated (categories, payment methods,
   warehouses, price lists) — small and needed whole for dropdowns.

### P2 — Frontend paging

1. Provider pattern: `items` + `page` + `hasMore` + `loadMore()` +
   `refresh()` (refresh resets to page 1). Keep existing CRUD methods
   unchanged.
2. `BaseScreen` gains an optional paged list mode: `ScrollController`
   triggers `loadMore()` at ~80% scroll; pull-to-refresh maps to
   `refresh()`.
3. Search inputs in Products/Customers screens call the server (`q`)
   instead of filtering loaded rows.

## Reports (P3–P5)

Server-side aggregation only — reports never pull full tables (depends on
P1 discipline). New `ReportsController`, `/api/Reports/*`, small dedicated
DTOs (also keeps `ReferenceHandler.Preserve` `$ref` noise out of payloads).

### P3 — Core report endpoints

| Report | Query | Notes |
|---|---|---|
| Sales summary | SalesOrderHeader grouped by day/week/month, range + warehouse params | revenue, order count, avg ticket |
| Top products | SalesOrderDetail → Product grouped | qty + revenue ranks |
| Category breakdown | Top products × ProductCategory → Category | share of sales |
| Stock levels | Inventory grouped by product/warehouse | low-stock cutoff param |
| Customer balances | Sales totals vs CustomerPayment per customer | outstanding balance |
| Purchases by supplier | PurchaseOrderHeader/Detail grouped | spend, receipt status |
| Tax totals | SalesTaxDetail by period | per SalesOrderHeader range |
| Salesperson performance | SalesOrderHeader grouped by salespersonName | qty + revenue |

### P4 — Reports screens (Flutter)

- New Reports section in `ModernSidebar`; one screen with a shared
  filter bar (date range + warehouse + category), KPI tiles, result table.
- Provider per report group (e.g. `ReportsProvider` with typed fetches),
  standard `isLoading`/`error` conventions.

### P5 — Polish

- Trend charts (fl_chart) for sales summary; consider the dataviz skill.
- CSV export: generate server-side (report endpoints get `?format=csv`)
  or client-side; server-side keeps the iPad flow file-system-free.
- Rough margin report (sales revenue vs latest purchase unit cost) —
  caveated, since stock cost layers aren't tracked.

## Suggested order

P1 → P2 → P4(only sales summary + stock levels as the first two reports)
→ P3(remaining endpoints) → P5. This gets visible value on the iPad
earliest: paged lists + two working reports make the demo real.