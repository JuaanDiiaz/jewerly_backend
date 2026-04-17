# J&A Jewelry Backend API

ASP.NET Core REST API for the J&A Jewelry Inventory Management system.

## Requirements

- .NET 8.0 SDK or later
- MySQL 8.0 or later

## Setup

1. **Configure database connection**

   Update the connection string in `appsettings.json` or use environment variables:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=jewelry_inventory;User=root;Password=your_password;"
     }
   }
   ```

2. **Run the application**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001` or `http://localhost:5000`.

3. **Apply migrations** (if database doesn't exist)
   ```bash
   dotnet ef database update
   ```

## API Endpoints

| Resource | Controller | Methods |
|----------|-------------|---------|
| Products | ProductController | GET, POST, PUT/{id}, DELETE/{id} |
| Categories | CategoryController | GET, POST, PUT/{id}, DELETE/{id} |
| Customers | CustomerController | GET, POST, PUT/{id}, DELETE/{id} |
| Customer Payments | CustomerPaymentController | GET, POST, PUT/{id}, DELETE/{id} |
| Inventory | InventoryController | GET, POST, PUT/{id}, DELETE/{id} |
| Inventory Movements | InventoryMovementController | GET, POST, PUT/{id}, DELETE/{id} |
| Warehouses | WarehouseController | GET, POST, PUT/{id}, DELETE/{id} |
| Sales Orders | SalesOrderHeaderController | GET, POST, PUT/{id}, DELETE/{id} |
| Sales Details | SalesOrderDetailController | GET, POST, PUT/{id}, DELETE/{id} |
| Purchase Orders | PurchaseOrderHeaderController | GET, POST, PUT/{id}, DELETE/{id} |
| Purchase Details | PurchaseOrderDetailController | GET, POST, PUT/{id}, DELETE/{id} |
| Price Lists | PriceListController | GET, POST, PUT/{id}, DELETE/{id} |
| Price List Details | PriceListDetailController | GET, POST, PUT/{id}, DELETE/{id} |
| Payment Methods | PaymentMethodController | GET, POST, PUT/{id}, DELETE/{id} |
| Suppliers | SupplierController | GET, POST, PUT/{id}, DELETE/{id} |
| Product Images | ProductImageController | GET, POST, PUT/{id}, DELETE/{id} |
| Product Categories | ProductCategoryController | GET, POST, PUT/{id}, DELETE/{id} |

## Key Endpoints

### Inventory Management

- `PUT /api/Inventory/UpdateQuantity` - Update inventory quantity for a product at a warehouse
  ```json
  {
    "productId": 1,
    "warehouseId": 1,
    "quantityChange": -5
  }
  ```

### Sales Orders

1. Create header: `POST /api/SalesOrderHeader`
2. Add details: `POST /api/SalesOrderDetail` for each item

### Purchase Orders

1. Create header: `POST /api/PurchaseOrderHeader`
2. Add details: `POST /api/PurchaseOrderDetail` for each item

## Architecture

- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core with MySQL
- **Database**: MySQL with code-first migrations

## Project Structure

```
J&A_Jewelry/
├── Controllers/          # API controllers
├── Models/               # Entity models and DbContext
├── Migrations/            # EF Core migrations
├── Pages/                 # Razor pages (optional)
└── Program.cs             # Application entry point
```

## Frontend

This API powers the J&A Jewelry iPad app. See [jewerly_ipad](https://github.com/JuaanDiiaz/jewerly_ipad) for more details.

## License

Private - J&A Jewelry
