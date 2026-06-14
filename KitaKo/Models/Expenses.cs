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

        [Range(0.01, 10000000)]
        public decimal Amount { get; set; }

        [CustomValidation(typeof(ExpenseValidations), nameof(ExpenseValidations.ValidateDueDate))]
        public DateTime DueDate { get; set; }

        // Reverted back to original 1-5 scale
        [Range(1, 5)]
        public int Priority { get; set; }

        // NEW: expense category ("bill", "stock", "subscription", "other")
        [StringLength(50)]
        public string? Category { get; set; }

        public bool Paid { get; set; }
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Calculates days remaining until due date. Negative if overdue.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int DaysUntilDue => (int)Math.Ceiling((DueDate - DateTime.UtcNow).TotalDays);

        /// <summary>
        /// Returns true if expense is past due date
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsOverdue => DaysUntilDue < 0 && !Paid;
    }

    public static class ExpenseValidations
    {
        public static ValidationResult? ValidateDueDate(DateTime dueDate, ValidationContext context)
        {
            if (dueDate < DateTime.UtcNow.Date)
            {
                return new ValidationResult("Due date cannot be in the past");
            }
            return ValidationResult.Success;
        }
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
