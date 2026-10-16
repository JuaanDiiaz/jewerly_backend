# Database management (EF Core + MySQL)

The database schema is versioned with **EF Core migrations**, stored in
`J&A_Jewelry/Migrations/`. The only active migration is `InitialBaseline`
(`20261016004942_InitialBaseline`), generated from the current model and
verified column-by-column against the live schema on a scratch database.

## Key facts

- Provider: **Pomelo.EntityFrameworkCore.MySql** (`UseMySql` in `Program.cs`).
  Oracle's `MySql.EntityFrameworkCore` was replaced because it does not
  implement EF Core 9 migration locking (its `database update` emits SQL
  Server statements and fails).
- Dev connection string lives in **user secrets**
  (`dotnet user-secrets list --project J&A_Jewelry`), never in source.
  `appsettings.json` still contains production credentials — **pending
  rotation** (see below).
- All foreign keys are declared explicitly in
  `Models/IsetechcJewelryInventoryContext.ModelExtensions.cs`, mirroring the
  constraints in the live database. The scaffold originally only emitted
  indexes — do not regenerate the context over it.
- Money columns use `HasPrecision(10, 2)` (the scaffold dropped the scale).

## Day-to-day workflow

```bash
git pull                                    # ALWAYS first — linear history
dotnet ef migrations add <DescriptiveName>  # review generated Up()/Down()
dotnet ef database update                   # apply to dev DB
git add -A && git commit -m "migration: <what>"
```

- One migration = one logical change. Never edit a migration that was
  already pushed; add a new one on top.
- Reference data lives in `HasData(...)` blocks in `ModelExtensions.cs`.
  Seed IDs must stay exactly as they are (`PaymentMethod`, `Warehouse`,
  `PriceList`, `Category` rows 1..n) — the app references these IDs.
  Changing any seeded value is itself a migration.

## Applying migrations to production (isetech.com.mx)

1. Make a backup first:
   `mysqldump -h isetech.com.mx -u <user> -p isetechc_jewelry_inventory > backup.sql`
2. Generate and review the script:
   `dotnet ef migrations script <LastApplied> <New> --idempotent`
3. Apply only after review.

### PENDING one-time steps (manual — do on the server)

The production DB predates `InitialBaseline`, so migration history must be
re-baselined there once:

1. Verify the schema matches a fresh baseline (types `decimal(10,2)`, etc.);
2. `ALTER TABLE categories ADD CONSTRAINT FK_Categories_Parent FOREIGN KEY
   (id_parentCategory) REFERENCES categories(id);` — the self-reference
   constraint existed only locally;
3. Confirm seed IDs in production match the `HasData` values
   (`SELECT * FROM paymentmethods; SELECT * FROM warehouses;
   SELECT * FROM pricelists; SELECT * FROM categories;`);
4. Empty and re-seed history:
   `DELETE FROM __EFMigrationsHistory;`
   `INSERT INTO __EFMigrationsHistory VALUES ('20261016004942_InitialBaseline','9.0.3');`
5. **Rotate credentials** — the password currently committed in
   `appsettings.json` is public in git history and must be changed on the
   server, then move the production connection string to environment
   variables there and replace the file value with a placeholder.

## CI

`.github/workflows/db-check.yml` fails any push/PR where the model changed
without a new migration
(`dotnet ef migrations has-pending-model-changes`).