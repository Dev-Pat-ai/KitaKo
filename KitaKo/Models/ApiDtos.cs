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

        // Reverted back to original 1-5 scale
        [Range(1, 5)]
        public int Priority { get; set; }

        // NEW: category field
        [StringLength(50)]
        public string? Category { get; set; }

        public bool Paid { get; set; }
    }

    public class OptimizeRequest
    {
        [Range(0.01, 9999999999999999.99)]
        public decimal Budget { get; set; }
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

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class FinancialSettingsRequest
    {
        [Range(0, 9999999999999999.99)]
        public decimal AvailableBudget { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal DailySalesGoal { get; set; }
    }
}
