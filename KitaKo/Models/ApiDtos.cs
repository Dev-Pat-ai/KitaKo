using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class SaleRequest
    {
        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Profit { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class ExpenseRequest
    {
        [Required]
        [StringLength(200)]
        public string? Name { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        [Range(1, 5)]
        public int Priority { get; set; }

        public bool Paid { get; set; }
    }

    public class UtangRequest
    {
        [Required]
        [StringLength(200)]
        public string? CustomerName { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }
        public bool Paid { get; set; }
    }

    public class FinancialSettingsRequest
    {
        [Range(0, 9999999999999999.99)]
        public decimal AvailableBudget { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal DailySalesGoal { get; set; }
    }

    public class StoredProductRequest
    {
        [Required]
        [StringLength(200)]
        public string? ProductName { get; set; }

        [Required]
        [StringLength(100)]
        public string? Category { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal DefaultPrice { get; set; }

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(50)]
        public string? UnitType { get; set; }

        [StringLength(200)]
        public string? Supplier { get; set; }

        [StringLength(500)]
        public string? ProductImage { get; set; }
    }

    public class InventoryItemRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal CostPrice { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Price { get; set; }

        public DateTime? ExpirationDate { get; set; }
    }

    public class InventorySaleRequest
    {
        [Range(1, int.MaxValue)]
        public int QuantitySold { get; set; }
    }
}
