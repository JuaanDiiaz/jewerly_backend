using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace J_A_Jewelry.Models;

public partial class IsetechcJewelryInventoryContext : DbContext
{
    public IsetechcJewelryInventoryContext()
    {
    }

    public IsetechcJewelryInventoryContext(DbContextOptions<IsetechcJewelryInventoryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerPayment> CustomerPayments { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryMovement> InventoryMovements { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<PriceList> PriceLists { get; set; }

    public virtual DbSet<PriceListDetail> PriceListDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    public virtual DbSet<PurchaseOrderHeader> PurchaseOrderHeaders { get; set; }

    public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }

    public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }

    public virtual DbSet<SalesTaxDetail> SalesTaxDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierProduct> SupplierProducts { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CategoryNumber)
                .HasMaxLength(50)
                .HasColumnName("categoryNumber");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.ExtraInformation)
                .HasColumnType("text")
                .HasColumnName("extraInformation");
            entity.Property(e => e.IdParentCategory)
                .HasColumnType("int(11)")
                .HasColumnName("id_parentCategory");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postalCode");
        });

        modelBuilder.Entity<CustomerPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.CustomerId, "customerId");

            entity.HasIndex(e => e.PaymentMethodId, "paymentMethodId");

            entity.HasIndex(e => e.SalesOrderId, "salesOrderId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10)
                .HasColumnName("amount");
            entity.Property(e => e.CustomerId)
                .HasColumnType("int(11)")
                .HasColumnName("customerId");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("date")
                .HasColumnName("paymentDate");
            entity.Property(e => e.PaymentMethodId)
                .HasColumnType("int(11)")
                .HasColumnName("paymentMethodId");
            entity.Property(e => e.SalesOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("salesOrderId");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("Inventory");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.HasIndex(e => e.WarehouseId, "warehouseId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .HasColumnName("location");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouseId");
            entity.Property(e => e.Weight)
                .HasPrecision(10)
                .HasColumnName("weight");

        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.HasIndex(e => e.PurchaseOrderId, "purchaseOrderId");

            entity.HasIndex(e => e.SalesOrderId, "salesOrderId");

            entity.HasIndex(e => e.WarehouseId, "warehouseId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ManualEntryReason)
                .HasMaxLength(255)
                .HasColumnName("manualEntryReason");
            entity.Property(e => e.MovementDate)
                .HasColumnType("datetime")
                .HasColumnName("movementDate");
            entity.Property(e => e.MovementType)
                .HasColumnType("enum('IN','OUT')")
                .HasColumnName("movementType");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.PurchaseOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("purchaseOrderId");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.SalesOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("salesOrderId");
            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouseId");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<PriceListDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.PriceListId, "priceListId");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Price)
                .HasPrecision(10)
                .HasColumnName("price");
            entity.Property(e => e.PriceListId)
                .HasColumnType("int(11)")
                .HasColumnName("priceListId");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.ValidFrom)
                .HasColumnType("date")
                .HasColumnName("validFrom");
            entity.Property(e => e.ValidTo)
                .HasColumnType("date")
                .HasColumnName("validTo");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.CategoryId, "categoryId");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CategoryId)
                .HasColumnType("int(11)")
                .HasColumnName("categoryId");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");

        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("imageUrl");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");

        });

        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("PurchaseOrderDetail");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.HasIndex(e => e.PurchaseOrderId, "purchaseOrderId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.PurchaseOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("purchaseOrderId");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.Total)
                .HasPrecision(10)
                .HasColumnName("total");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10)
                .HasColumnName("unitPrice");
        });

        modelBuilder.Entity<PurchaseOrderHeader>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("PurchaseOrderHeader");

            entity.HasIndex(e => e.SupplierId, "supplierId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.OrderDate)
                .HasColumnType("date")
                .HasColumnName("orderDate");
            entity.Property(e => e.ReceptionDate)
                .HasColumnType("date")
                .HasColumnName("receptionDate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.SupplierId)
                .HasColumnType("int(11)")
                .HasColumnName("supplierId");
            entity.Property(e => e.Total)
                .HasPrecision(10)
                .HasColumnName("total");
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("SalesOrderDetail");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.HasIndex(e => e.SalesOrderId, "salesOrderId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");
            entity.Property(e => e.SalesOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("salesOrderId");
            entity.Property(e => e.Total)
                .HasPrecision(10)
                .HasColumnName("total");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10)
                .HasColumnName("unitPrice");
        });

        modelBuilder.Entity<SalesOrderHeader>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("SalesOrderHeader");

            entity.HasIndex(e => e.CustomerId, "customerId");

            entity.HasIndex(e => e.PaymentMethodId, "paymentMethodId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CustomerId)
                .HasColumnType("int(11)")
                .HasColumnName("customerId");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.PaymentMethodId)
                .HasColumnType("int(11)")
                .HasColumnName("paymentMethodId");
            entity.Property(e => e.SaleDate)
                .HasColumnType("date")
                .HasColumnName("saleDate");
            entity.Property(e => e.Total)
                .HasPrecision(10)
                .HasColumnName("total");
        });

        modelBuilder.Entity<SalesTaxDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("SalesTaxDetail");

            entity.HasIndex(e => e.SalesOrderId, "salesOrderId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.SalesOrderId)
                .HasColumnType("int(11)")
                .HasColumnName("salesOrderId");
            entity.Property(e => e.TaxAmount)
                .HasPrecision(10)
                .HasColumnName("taxAmount");
            entity.Property(e => e.TaxType)
                .HasMaxLength(50)
                .HasColumnName("taxType");

        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ClientNumber)
                .HasMaxLength(50)
                .HasColumnName("clientNumber");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(100)
                .HasColumnName("shipVia");
        });

        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ProductId, "productId");

            entity.HasIndex(e => e.SupplierId, "supplierId");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.ArtCode)
                .HasMaxLength(50)
                .HasColumnName("artCode");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Price)
                .HasPrecision(10)
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .HasColumnType("int(11)")
                .HasColumnName("productId");
            entity.Property(e => e.StyleCode)
                .HasMaxLength(50)
                .HasColumnName("styleCode");
            entity.Property(e => e.SupplierId)
                .HasColumnType("int(11)")
                .HasColumnName("supplierId");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
