using System;
using System.Collections.Generic;
using System.Linq;
using KitaKo.Models;

namespace KitaKo.Services
{
    public class KnapsackService
    {
        private static readonly Dictionary<string, int> CategoryBonus = new(StringComparer.OrdinalIgnoreCase)
        {
            { "bill", 200 },
            { "subscription", 150 },
            { "stock", 100 },
            { "other", 50 },
        };

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
                    RecommendedExpenses = new List<Expenses>(),
                    SkippedExpenses = unpaid,
                    TotalOptimizedCost = 0,
                    RemainingBudget = budget,
                    TotalPriorityScore = 0,
                    BudgetWarning = warning
                };
            }

            const int MaxCapacityCentavos = 10_000_000;
            int W = (int)Math.Min(budget * 100m, MaxCapacityCentavos);

            int[] weights = unpaid.Select(e => (int)Math.Min(e.Amount * 100m, int.MaxValue / 2)).ToArray();
            int[] values = unpaid.Select(e => ComputeValue(e, now)).ToArray();

            int[] dp = new int[W + 1];
            for (int i = 0; i < n; i++)
            {
                int wi = weights[i];
                int vi = values[i];
                if (wi > W)
                {
                    continue;
                }

                for (int w = W; w >= wi; w--)
                {
                    int candidate = dp[w - wi] + vi;
                    if (candidate > dp[w])
                    {
                        dp[w] = candidate;
                    }
                }
            }

            int[,] dpFull = new int[n + 1, W + 1];
            for (int i = 1; i <= n; i++)
            {
                int wi = weights[i - 1];
                int vi = values[i - 1];
                for (int w = 0; w <= W; w++)
                {
                    dpFull[i, w] = dpFull[i - 1, w];
                    if (wi <= w)
                    {
                        int candidate = dpFull[i - 1, w - wi] + vi;
                        if (candidate > dpFull[i, w])
                        {
                            dpFull[i, w] = candidate;
                        }
                    }
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

