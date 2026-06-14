using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class Expenses
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string? Name { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        // Reverted back to original 1-5 scale
        [Range(1, 5)]
        public int Priority { get; set; }

        // NEW: expense category ("bill", "stock", "subscription", "other")
        [StringLength(50)]
        public string? Category { get; set; }

        public bool Paid { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ExpenseOptimizationResult
    {
        public List<Expenses> RecommendedExpenses { get; set; } = new();
        public List<Expenses> SkippedExpenses { get; set; } = new();      // NEW: what was left out
        public decimal TotalOptimizedCost { get; set; }
        public decimal RemainingBudget { get; set; }
        public int TotalPriorityScore { get; set; }                       // NEW: value maximized
        public string? BudgetWarning { get; set; }                        // NEW: overdue alert
    }
}
