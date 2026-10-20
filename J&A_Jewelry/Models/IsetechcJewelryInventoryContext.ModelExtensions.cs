using Microsoft.EntityFrameworkCore;

namespace J_A_Jewelry.Models;

// Extends the scaffolded context with two things it was missing:
//
// 1. Explicit foreign keys mirroring the constraints that exist in the
//    production database (information_schema.KEY_COLUMN_USAGE). The original
//    scaffold only emitted indexes, so without this every generated migration
//    would try to strip the real constraints. DeleteBehavior.Restrict matches
//    the live ibfk_ constraints.
//
// 2. Reference data required by the app on a fresh database (warehouses,
//    payment methods, price lists, categories). IDs are stable and MUST match
//    the live database — see DATABASE.md before changing any row here.
public partial class IsetechcJewelryInventoryContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Diagnostics LogEvents table — created and owned by the MySQL log
        // sink (see Serilog/MySqlLogEventSink.cs), never by a migration: it
        // must not show up in migration diffs or need a migration run in prod.
        modelBuilder.Entity<LogEvent>().ToTable("LogEvents", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<LogEvent>().Property(e => e.TimeStamp).HasColumnType("datetime(3)");
        modelBuilder.Entity<LogEvent>().Property(e => e.Properties).HasColumnType("json");
        // No DbSet declaration: query with context.Set<LogEvent>() so a future
        // scaffold/migration diff never churns on this read-only table.

        // salespersonName column added by the AddSalespersonName migration;
        // checkout previously stored this value inside notes.
        modelBuilder.Entity<SalesOrderHeader>()
            .Property(e => e.SalespersonName)
            .HasMaxLength(100)
            .HasColumnName("salespersonName");

        ConfigureRelationships(modelBuilder);
        ConfigureSeedData(modelBuilder);
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // Identity (users/roles) — domain tables WITH a migration (unlike
        // the diagnostics LogEvents table excluded below).
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime(6)");
        });
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(64);
            entity.Property(e => e.Permissions).HasMaxLength(1000);
        });
        // Many-to-many with an explicit join entity (composite key) — EF can't
        // invent the skip navigation shape the API contract needs.
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity<AppUserRole>(
                join => join.HasOne(row => row.Role)
                    .WithMany()
                    .HasForeignKey(row => row.RoleId)
                    .OnDelete(DeleteBehavior.Cascade),
                join => join.HasOne(row => row.User)
                    .WithMany()
                    .HasForeignKey(row => row.UserId)
                    .OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey(row => new { row.UserId, row.RoleId }));

        // CustomerPayments
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(cp => cp.Customer)
            .WithMany()
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPayment>()
            .HasOne<SalesOrderHeader>()
            .WithMany()
            .HasForeignKey(cp => cp.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerPayment>()
            .HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(cp => cp.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inventory
        modelBuilder.Entity<Inventory>()
            .HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Inventory>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // InventoryMovements
        modelBuilder.Entity<InventoryMovement>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(im => im.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>()
            .HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(im => im.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>()
            .HasOne<PurchaseOrderHeader>()
            .WithMany()
            .HasForeignKey(im => im.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryMovement>()
            .HasOne<SalesOrderHeader>()
            .WithMany()
            .HasForeignKey(im => im.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // PriceListDetails
        modelBuilder.Entity<PriceListDetail>()
            .HasOne<PriceList>()
            .WithMany()
            .HasForeignKey(pld => pld.PriceListId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PriceListDetail>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(pld => pld.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductCategories
        modelBuilder.Entity<ProductCategory>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductCategory>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductImages
        modelBuilder.Entity<ProductImage>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // PurchaseOrderDetail
        modelBuilder.Entity<PurchaseOrderDetail>()
            .HasOne<PurchaseOrderHeader>()
            .WithMany()
            .HasForeignKey(pod => pod.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PurchaseOrderDetail>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(pod => pod.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // PurchaseOrderHeader
        modelBuilder.Entity<PurchaseOrderHeader>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(poh => poh.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // SalesOrderDetail
        modelBuilder.Entity<SalesOrderDetail>()
            .HasOne<SalesOrderHeader>()
            .WithMany()
            .HasForeignKey(sod => sod.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderDetail>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(sod => sod.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // SalesOrderHeader
        modelBuilder.Entity<SalesOrderHeader>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(soh => soh.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SalesOrderHeader>()
            .HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(soh => soh.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // SalesTaxDetail
        modelBuilder.Entity<SalesTaxDetail>()
            .HasOne<SalesOrderHeader>()
            .WithMany()
            .HasForeignKey(st => st.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // SupplierProducts
        modelBuilder.Entity<SupplierProduct>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(sp => sp.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupplierProduct>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(sp => sp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing category hierarchy
        modelBuilder.Entity<Category>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.IdParentCategory)
            .OnDelete(DeleteBehavior.Restrict);

        // TaxRate: a null CategoryId marks the fallback rate for
        // uncategorized products.
        modelBuilder.Entity<TaxRate>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(tr => tr.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Description = "Cash" },
            new PaymentMethod { Id = 2, Description = "Credit Card" },
            new PaymentMethod { Id = 3, Description = "Debit Card" },
            new PaymentMethod { Id = 4, Description = "Bank Transfer" },
            new PaymentMethod { Id = 5, Description = "Check" },
            new PaymentMethod { Id = 6, Description = "multiple payments" }
        );

        modelBuilder.Entity<Warehouse>().HasData(
            new Warehouse { Id = 1, Name = "Main Store" },
            new Warehouse { Id = 2, Name = "Warehouse A" },
            new Warehouse { Id = 3, Name = "Warehouse B" }
        );

        modelBuilder.Entity<PriceList>().HasData(
            new PriceList { Id = 1, Name = "Retail" },
            new PriceList { Id = 2, Name = "Wholesale" }
        );

        // Categories use a self-referencing hierarchy (IdParentCategory).
        // Parents must exist before children reference them — seed order
        // is handled by EF, but keep flat->nested ordering obvious here.
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, CategoryNumber = "CAT001", Description = "Rings" },
            new Category { Id = 2, CategoryNumber = "CAT002", Description = "Necklaces" },
            new Category { Id = 3, CategoryNumber = "CAT003", Description = "Bracelets" },
            new Category { Id = 4, CategoryNumber = "CAT004", Description = "Earrings" },
            new Category { Id = 5, CategoryNumber = "CAT005", Description = "Watches" }
        );

        // Default tax rate: the fallback for products with no category
        // (TaxRate.CategoryId is null there). Mexico IVA.
        modelBuilder.Entity<TaxRate>().HasData(
            new TaxRate { Id = 1, CategoryId = null, Name = "IVA 16%", Rate = 0.16m, IsActive = true }
        );
    }
}