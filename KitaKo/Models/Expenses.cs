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
            // Only validate due date for NEW expenses (not updates)
            // If this is an update context, allow any date
            var instance = context.ObjectInstance as Expenses;
            if (instance?.Id > 0)
            {
                // This is an update, allow any date (user may be marking overdue expense as paid)
                return ValidationResult.Success;
            }
            
            // For new expenses, due date cannot be in the past
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
