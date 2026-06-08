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

        [Range(1, 5)]
        public int Priority { get; set; }

        public bool Paid { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ExpenseOptimizationResult
    {
        public List<Expenses>? RecommendedExpenses { get; set; }
        public decimal TotalOptimizedCost { get; set; }
        public decimal RemainingBudget { get; set; }
    }
}
