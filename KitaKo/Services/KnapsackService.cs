using KitaKo.Models;

namespace KitaKo.Services
{
    public class KnapsackService
    {
<<<<<<< HEAD
        // Category priority bonus: bills outrank stock at equal user-priority
        private static readonly Dictionary<string, int> CategoryBonus = new(StringComparer.OrdinalIgnoreCase)
        {
            { "bill",         200 },
            { "subscription", 150 },
            { "stock",        100 },
            { "other",         50 },
        };

        /// <summary>
        /// Compute a composite value for a single expense.
        /// Higher = more important to pay.
        ///   - User priority (1-10) × 1000  → up to 10,000
        ///   - Due-date urgency bonus        → up to 500
        ///   - Category bonus                → up to 200
        /// </summary>
        private static int ComputeValue(Expenses expense, DateTime now)
        {
            // 1. Base value from user-assigned priority
            int baseValue = expense.Priority * 1000;

            // 2. Urgency bonus based on days until due
            double daysLeft = (expense.DueDate - now).TotalDays;
            int urgency = daysLeft < 0 ? 500   // overdue
                        : daysLeft <= 1 ? 400
                        : daysLeft <= 3 ? 300
                        : daysLeft <= 7 ? 150
                        : daysLeft <= 14 ? 50
                        : 0;

            // 3. Category bonus (bills > stock > other)
            int catBonus = 0;
            if (!string.IsNullOrWhiteSpace(expense.Category)
                && CategoryBonus.TryGetValue(expense.Category, out var cb))
            {
                catBonus = cb;
            public class KnapsackService
            {
                // Category priority bonus: bills outrank stock at equal user-priority
                private static readonly Dictionary<string, int> CategoryBonus = new(StringComparer.OrdinalIgnoreCase)
                {
                    { "bill",         200 },
                    { "subscription", 150 },
                    { "stock",        100 },
                    { "other",         50 },
                };

                /// <summary>
                /// Compute a composite value for a single expense.
                /// Higher = more important to pay.
                /// </summary>
                private static int ComputeValue(Expenses expense, DateTime now)
                {
                    int baseValue = expense.Priority * 1000;

                    double daysLeft = (expense.DueDate - now).TotalDays;
                    int urgency = daysLeft < 0 ? 500
                                : daysLeft <= 1 ? 400
                                : daysLeft <= 3 ? 300
                                : daysLeft <= 7 ? 150
                                : daysLeft <= 14 ? 50
                                : 0;

                    int catBonus = 0;
                    if (!string.IsNullOrWhiteSpace(expense.Category)
                        && CategoryBonus.TryGetValue(expense.Category, out var cb))
                    {
                        catBonus = cb;
                    }

                    return baseValue + urgency + catBonus;
                }

                public ExpenseOptimizationResult OptimizeExpenses(List<Expenses> expenses, decimal budget)
                {
                    var now = DateTime.UtcNow;
                    var unpaid = expenses.Where(e => !e.Paid).ToList();

                    // Build overdue warning
                    var overdueItems = unpaid.Where(e => e.DueDate < now).ToList();
                    string? warning = null;
                    if (overdueItems.Any())
                    {
                        var overdueTotal = overdueItems.Sum(e => e.Amount);
                        warning = $"You have {overdueItems.Count} overdue expense(s) totalling ₱{overdueTotal:N2}. " +
                                  "They have been given highest priority in the optimization.";
                    }

                    int n = unpaid.Count;
                    if (n == 0 || budget <= 0)
                    {
                        return new ExpenseOptimizationResult
                        {
                            SkippedExpenses = unpaid,
                            TotalOptimizedCost = 0,
                            RemainingBudget = budget,
                            TotalPriorityScore = 0,
                            BudgetWarning = warning
                        };
                    }

                    // Work in centavos to preserve decimal precision
                    const int MaxCapacityCentavos = 10_000_000; // ₱100,000 max granularity
                    int W = (int)Math.Min(budget * 100m, MaxCapacityCentavos);

                    int[] weights = unpaid.Select(e => (int)Math.Min(e.Amount * 100m, int.MaxValue / 2)).ToArray();
                    int[] values = unpaid.Select(e => ComputeValue(e, now)).ToArray();

                    // 0/1 Knapsack DP (1-D rolling array for efficiency)
                    int[] dp = new int[W + 1];
                    for (int i = 0; i < n; i++)
                    {
                        int wi = weights[i];
                        int vi = values[i];
                        for (int w = W; w >= wi; w--)
                        {
                            if (dp[w - wi] + vi > dp[w])
                                dp[w] = dp[w - wi] + vi;
                        }
                    }

                    // Rebuild full table for backtracking
                    int[,] dpFull = new int[n + 1, W + 1];
                    for (int i = 1; i <= n; i++)
                    {
                        int wi = weights[i - 1];
                        int vi = values[i - 1];
                        for (int w = 0; w <= W; w++)
                        {
                            dpFull[i, w] = dpFull[i - 1, w];
                            if (wi <= w && dpFull[i - 1, w - wi] + vi > dpFull[i, w])
                                dpFull[i, w] = dpFull[i - 1, w - wi] + vi;
                        }
                    }

                    var selected = new List<Expenses>();
                    int remaining = W;
                    for (int i = n; i > 0 && remaining > 0; i--)
                    {
                        if (dpFull[i, remaining] != dpFull[i - 1, remaining])
                        {
                            selected.Add(unpaid[i - 1]);
                            remaining -= weights[i - 1];
                        }
                    }
                    selected.Reverse();

                    var selectedIds = selected.Select(e => e.Id).ToHashSet();
                    var skipped = unpaid.Where(e => !selectedIds.Contains(e.Id)).ToList();
                    decimal totalCost = selected.Sum(e => e.Amount);
                    int totalScore = selected.Sum(e => ComputeValue(e, now));

                    return new ExpenseOptimizationResult
                    {
                        RecommendedExpenses = selected,
                        SkippedExpenses = skipped,
                        TotalOptimizedCost = totalCost,
                        RemainingBudget = budget - totalCost,
                        TotalPriorityScore = totalScore,
                        BudgetWarning = warning
                    };
                }
            }
        }
                RemainingBudget = remainingBudget
