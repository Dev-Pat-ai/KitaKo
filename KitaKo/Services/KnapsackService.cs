using KitaKo.Models;

namespace KitaKo.Services
{
    public class KnapsackService
    {
        public ExpenseOptimizationResult OptimizeExpenses(List<Expenses> expenses, decimal budget)
        {
            var unpaidExpenses = expenses.Where(e => !e.Paid).ToList();
            int n = unpaidExpenses.Count;
            int W = (int)budget;

            if (n == 0 || W == 0)
            {
                return new ExpenseOptimizationResult
                {
                    RecommendedExpenses = new List<Expenses>(),
                    TotalOptimizedCost = 0,
                    RemainingBudget = budget
                };
            }

            // Create DP table
            int[,] dp = new int[n + 1, W + 1];

            // Fill DP table
            for (int i = 1; i <= n; i++)
            {
                var expense = unpaidExpenses[i - 1];
                int weight = (int)expense.Amount;
                int value = expense.Priority * 100;

                for (int w = 0; w <= W; w++)
                {
                    if (weight <= w)
                    {
                        dp[i, w] = Math.Max(dp[i - 1, w], dp[i - 1, w - weight] + value);
                    }
                    else
                    {
                        dp[i, w] = dp[i - 1, w];
                    }
                }
            }

            // Backtrack to find which expenses to include
            List<Expenses> selectedExpenses = new List<Expenses>();
            int budgetRemaining = W;

            for (int i = n; i > 0 && budgetRemaining > 0; i--)
            {
                if (dp[i, budgetRemaining] != dp[i - 1, budgetRemaining])
                {
                    var expense = unpaidExpenses[i - 1];
                    selectedExpenses.Add(expense);
                    budgetRemaining -= (int)expense.Amount;
                }
            }

            decimal totalCost = selectedExpenses.Sum(e => e.Amount);

            return new ExpenseOptimizationResult
            {
                RecommendedExpenses = selectedExpenses,
                TotalOptimizedCost = totalCost,
                RemainingBudget = budget - totalCost
            };
        }
    }
}