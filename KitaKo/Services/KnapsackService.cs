using KitaKo.Models;

namespace KitaKo.Services
{
    public class KnapsackService
    {
        /// <summary>
        /// Optimizes expense payments using a modified knapsack algorithm that considers:
        /// - Priority (importance)
        /// - Urgency (days until due)
        /// - Amount (cost)
        /// </summary>
        public ExpenseOptimizationResult OptimizeExpenses(List<Expenses> expenses, decimal budget)
        {
            var unpaidExpenses = expenses.Where(e => !e.Paid).OrderByDescending(e => CalculateScore(e)).ToList();
            
            if (unpaidExpenses.Count == 0 || budget <= 0)
            {
                return new ExpenseOptimizationResult
                {
                    RecommendedExpenses = new List<Expenses>(),
                    TotalOptimizedCost = 0,
                    RemainingBudget = budget
                };
            }

            // Use greedy algorithm with scoring instead of knapsack for decimal precision
            var selectedExpenses = new List<Expenses>();
            decimal remainingBudget = budget;

            foreach (var expense in unpaidExpenses)
            {
                if (expense.Amount <= remainingBudget)
                {
                    selectedExpenses.Add(expense);
                    remainingBudget -= expense.Amount;
                }
            }

            decimal totalCost = selectedExpenses.Sum(e => e.Amount);

            return new ExpenseOptimizationResult
            {
                RecommendedExpenses = selectedExpenses,
                TotalOptimizedCost = totalCost,
                RemainingBudget = remainingBudget
            };
        }

        /// <summary>
        /// Calculates a composite score for an expense based on priority and urgency.
        /// Higher score = higher priority to pay
        /// </summary>
        private decimal CalculateScore(Expenses expense)
        {
            // Priority weight: 1-5 (higher = more important)
            decimal priorityScore = expense.Priority * 10;

            // Urgency weight: based on days until due
            decimal urgencyScore = CalculateUrgencyScore(expense.DaysUntilDue);

            // Combined score (60% priority, 40% urgency)
            return (priorityScore * 0.6m) + (urgencyScore * 0.4m);
        }

        /// <summary>
        /// Calculates urgency score based on days until due.
        /// Overdue items get highest urgency (50), then decreasing as deadline approaches.
        /// </summary>
        private decimal CalculateUrgencyScore(int daysUntilDue)
        {
            return daysUntilDue switch
            {
                < 0 => 50,      // Overdue: maximum urgency
                0 => 48,        // Due today: second highest
                1 => 40,        // Due tomorrow
                2 => 35,
                3 => 30,
                4 => 25,
                5 => 20,
                6 => 15,
                7 => 10,
                8 or 9 or 10 => 5,
                _ => 1           // More than 10 days: low urgency
            };
        }
    }
}