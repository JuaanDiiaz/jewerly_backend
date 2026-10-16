using System.Collections.Generic;

namespace J_A_Jewelry.Models
{
    // Row DTOs for /api/Reports/* endpoints. Plain classes (no navigation
    // properties) so ReferenceHandler.Preserve never emits $ref cycles.
    // All money values are rounded server-side to 2 decimals.

    // GET api/Reports/SalesSummary?from_date&to_date&group_by=day|month
    public class SalesSummaryRow
    {
        public string Period { get; set; } = "";
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageTicket { get; set; }
    }

    // GET api/Reports/TopProducts?from_date&to_date&limit
    public class TopProductRow
    {
        public int ProductId { get; set; }
        public string Description { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    // GET api/Reports/CategoryShare?from_date&to_date
    public class CategoryShareRow
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    // GET api/Reports/StockLevels?warehouse_id&below_quantity
    public class StockLevelRow
    {
        public int ProductId { get; set; }
        public string Description { get; set; } = "";
        public string? Picture { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int Quantity { get; set; }
    }

    // GET api/Reports/CustomerBalances?only_with_balance
    public class CustomerBalanceRow
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = "";
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
    }

    // GET api/Reports/PurchasesBySupplier?from_date&to_date
    public class PurchaseBySupplierRow
    {
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = "";
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    // GET api/Reports/TaxTotals?from_date&to_date
    public class TaxTotalRow
    {
        public string TaxType { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }

    // GET api/Reports/SalespersonPerformance?from_date&to_date
    public class SalespersonRow
    {
        public string Name { get; set; } = "";
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}