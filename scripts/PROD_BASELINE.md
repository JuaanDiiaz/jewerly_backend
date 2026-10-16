# Production database baseline runbook (isetech.com.mx)

One-time steps to align the production DB with `InitialBaseline`
(`20261016004942_InitialBaseline`). Replace `$USER` and `$PASS` with the
production MySQL credentials when running.

## 0. Read-only verification first

```sql
-- migration history should have the pre-baseline state
SELECT MigrationId FROM __efmigrationshistory;

-- self-referencing FK is expected to be MISSING until step 3
SELECT CONSTRAINT_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = 'isetechc_jewelry_inventory'
  AND TABLE_NAME = 'categories';

-- all money columns must be decimal(10,2)
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'isetechc_jewelry_inventory'
  AND COLUMN_TYPE LIKE 'decimal(10,%';

-- seed IDs must match the HasData rows exactly:
--   paymentmethods: 1 Cash, 2 Credit Card, 3 Debit Card, 4 Bank Transfer,
--                   5 Check, 6 multiple payments
--   warehouses:     1 Main Store, 2 Warehouse A, 3 Warehouse B
--   pricelists:     1 Retail, 2 Wholesale
--   categories:     1 Rings, 2 Necklaces, 3 Bracelets, 4 Earrings, 5 Watches
SELECT * FROM paymentmethods;  SELECT * FROM warehouses;
SELECT * FROM pricelists;      SELECT * FROM categories;
```

If any decimal is `decimal(10,0)` or a seed ID/value differs, STOP and fix
that difference before continuing.

## 1. Backup (mandatory, before any write)

```bash
mysqldump -h isetech.com.mx -u $USER -p isetechc_jewelry_inventory > prod_backup_$(date +%F).sql
```

## 2. Nothing else to migrate

Do NOT run `dotnet ef database update` against production — the schema is
already in place; only the history table and the missing constraint below
need updating.

## 3. Add the missing self-referencing category constraint

```sql
ALTER TABLE categories ADD CONSTRAINT FK_Categories_Parent
  FOREIGN KEY (id_parentCategory) REFERENCES categories (id);
```

## 4. Re-baseline migration history

```sql
DELETE FROM __EFMigrationsHistory;
INSERT INTO __EFMigrationsHistory
  VALUES ('20261016004942_InitialBaseline', '9.0.3');
```

## 5. Rotate leaked credentials

The password that was in `appsettings.json` is public in git history.

```sql
-- pick a strong password, then rotate for the API user
ALTER USER 'isetechc_jewerly'@'%' IDENTIFIED BY '<new-password>';
FLUSH PRIVILEGES;
```

On the server that runs the API (before restarting it):

```text
# Windows: setx ASPNETCORE_CONNECTIONSTRING__myDBConn "...new-password version..."
# Linux:   export ConnectionStrings__myDBConn="..."
# (env var Configuration key: ConnectionStrings__myDBConn)
```

Then commit the placeholder in `appsettings.json` — only do this AFTER the
server env var is set and verified, or a git-pull deploy will break:

```json
"ConnectionStrings": { "myDBConn": "Server=__SET-VIA-ENV__;Database=__SET-VIA-ENV__;User=__SET-VIA-ENV__;Password=__SET-VIA-ENV__;" }
```

Verify the API answers (`/api/Warehouse` returns 200) before/after each
server change, so the failing step is always the one you just did.