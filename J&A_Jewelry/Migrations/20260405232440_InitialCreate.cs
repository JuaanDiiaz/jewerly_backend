using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace J_A_Jewelry.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    categoryNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    extraInformation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    postalCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    description = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    clientNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    shipVia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesOrderHeader",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    saleDate = table.Column<DateTime>(type: "date", nullable: true),
                    customerId = table.Column<int>(type: "int(11)", nullable: true),
                    total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    paymentMethodId = table.Column<int>(type: "int(11)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "SalesOrderHeader_ibfk_1",
                        column: x => x.customerId,
                        principalTable: "Customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "SalesOrderHeader_ibfk_2",
                        column: x => x.paymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PriceListDetails",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    priceListId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    validFrom = table.Column<DateTime>(type: "date", nullable: true),
                    validTo = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "PriceListDetails_ibfk_1",
                        column: x => x.priceListId,
                        principalTable: "PriceLists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "PriceListDetails_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    categoryId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "ProductCategories_ibfk_1",
                        column: x => x.categoryId,
                        principalTable: "Categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ProductCategories_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    imageUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "ProductImages_ibfk_1",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderHeader",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    supplierId = table.Column<int>(type: "int(11)", nullable: true),
                    orderDate = table.Column<DateTime>(type: "date", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    receptionDate = table.Column<DateTime>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "PurchaseOrderHeader_ibfk_1",
                        column: x => x.supplierId,
                        principalTable: "Suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    supplierId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    artCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    styleCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "SupplierProducts_ibfk_1",
                        column: x => x.supplierId,
                        principalTable: "Suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "SupplierProducts_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    warehouseId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    weight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "Inventory_ibfk_1",
                        column: x => x.warehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Inventory_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    customerId = table.Column<int>(type: "int(11)", nullable: true),
                    salesOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    paymentDate = table.Column<DateTime>(type: "date", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    paymentMethodId = table.Column<int>(type: "int(11)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "CustomerPayments_ibfk_1",
                        column: x => x.customerId,
                        principalTable: "Customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "CustomerPayments_ibfk_2",
                        column: x => x.salesOrderId,
                        principalTable: "SalesOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "CustomerPayments_ibfk_3",
                        column: x => x.paymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesOrderDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    salesOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    quantity = table.Column<int>(type: "int(11)", nullable: true),
                    unitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "SalesOrderDetail_ibfk_1",
                        column: x => x.salesOrderId,
                        principalTable: "SalesOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "SalesOrderDetail_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesTaxDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    salesOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    taxType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    taxAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "SalesTaxDetail_ibfk_1",
                        column: x => x.salesOrderId,
                        principalTable: "SalesOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    warehouseId = table.Column<int>(type: "int(11)", nullable: true),
                    movementType = table.Column<string>(type: "enum('IN','OUT')", nullable: true),
                    quantity = table.Column<int>(type: "int(11)", nullable: true),
                    movementDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    purchaseOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    salesOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    manualEntryReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "InventoryMovements_ibfk_1",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "InventoryMovements_ibfk_2",
                        column: x => x.warehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "InventoryMovements_ibfk_3",
                        column: x => x.purchaseOrderId,
                        principalTable: "PurchaseOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "InventoryMovements_ibfk_4",
                        column: x => x.salesOrderId,
                        principalTable: "SalesOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetail",
                columns: table => new
                {
                    id = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    purchaseOrderId = table.Column<int>(type: "int(11)", nullable: true),
                    productId = table.Column<int>(type: "int(11)", nullable: true),
                    quantity = table.Column<int>(type: "int(11)", nullable: true),
                    unitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true),
                    total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "PurchaseOrderDetail_ibfk_1",
                        column: x => x.purchaseOrderId,
                        principalTable: "PurchaseOrderHeader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "PurchaseOrderDetail_ibfk_2",
                        column: x => x.productId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "customerId",
                table: "CustomerPayments",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "paymentMethodId",
                table: "CustomerPayments",
                column: "paymentMethodId");

            migrationBuilder.CreateIndex(
                name: "salesOrderId",
                table: "CustomerPayments",
                column: "salesOrderId");

            migrationBuilder.CreateIndex(
                name: "productId",
                table: "Inventory",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "warehouseId",
                table: "Inventory",
                column: "warehouseId");

            migrationBuilder.CreateIndex(
                name: "productId1",
                table: "InventoryMovements",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "purchaseOrderId",
                table: "InventoryMovements",
                column: "purchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "salesOrderId1",
                table: "InventoryMovements",
                column: "salesOrderId");

            migrationBuilder.CreateIndex(
                name: "warehouseId1",
                table: "InventoryMovements",
                column: "warehouseId");

            migrationBuilder.CreateIndex(
                name: "priceListId",
                table: "PriceListDetails",
                column: "priceListId");

            migrationBuilder.CreateIndex(
                name: "productId2",
                table: "PriceListDetails",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "categoryId",
                table: "ProductCategories",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "productId3",
                table: "ProductCategories",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "productId4",
                table: "ProductImages",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "productId5",
                table: "PurchaseOrderDetail",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "purchaseOrderId1",
                table: "PurchaseOrderDetail",
                column: "purchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "supplierId",
                table: "PurchaseOrderHeader",
                column: "supplierId");

            migrationBuilder.CreateIndex(
                name: "productId6",
                table: "SalesOrderDetail",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "salesOrderId2",
                table: "SalesOrderDetail",
                column: "salesOrderId");

            migrationBuilder.CreateIndex(
                name: "customerId1",
                table: "SalesOrderHeader",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "paymentMethodId1",
                table: "SalesOrderHeader",
                column: "paymentMethodId");

            migrationBuilder.CreateIndex(
                name: "salesOrderId3",
                table: "SalesTaxDetail",
                column: "salesOrderId");

            migrationBuilder.CreateIndex(
                name: "productId7",
                table: "SupplierProducts",
                column: "productId");

            migrationBuilder.CreateIndex(
                name: "supplierId1",
                table: "SupplierProducts",
                column: "supplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPayments");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "PriceListDetails");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetail");

            migrationBuilder.DropTable(
                name: "SalesOrderDetail");

            migrationBuilder.DropTable(
                name: "SalesTaxDetail");

            migrationBuilder.DropTable(
                name: "SupplierProducts");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "PurchaseOrderHeader");

            migrationBuilder.DropTable(
                name: "SalesOrderHeader");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "PaymentMethods");
        }
    }
}
