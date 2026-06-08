using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class ExpensesService
    {
        private readonly ApplicationDbContext _dbContext;

        public ExpensesService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Expenses>> GetExpensesAsync(int userId)
        {
            return await _dbContext.Expenses
                .Where(e => e.UserId == userId)
                .OrderBy(e => e.Paid)
                .ThenBy(e => e.DueDate)
                .ToListAsync();
        }

        public async Task<Expenses?> GetExpenseAsync(int userId, int id)
        {
            return await _dbContext.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }

        public async Task<Expenses> CreateExpenseAsync(int userId, ExpenseRequest request)
        {
            var expense = new Expenses
            {
                UserId = userId,
                Name = request.Name?.Trim(),
                Amount = request.Amount,
                DueDate = EnsureUtc(request.DueDate),
                Priority = request.Priority,
                Paid = request.Paid,
                CreatedDate = DateTime.UtcNow
            };

            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();
            return expense;
        }

        public async Task<bool> UpdateExpenseAsync(int userId, int id, ExpenseRequest request)
        {
            var expense = await GetExpenseAsync(userId, id);
            if (expense == null)
            {
                return false;
            }

            expense.Name = request.Name?.Trim();
            expense.Amount = request.Amount;
            expense.DueDate = EnsureUtc(request.DueDate);
            expense.Priority = request.Priority;
            expense.Paid = request.Paid;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteExpenseAsync(int userId, int id)
        {
            var expense = await GetExpenseAsync(userId, id);
            if (expense == null)
            {
                return false;
            }

            _dbContext.Expenses.Remove(expense);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task ClearExpensesAsync(int userId)
        {
            var expenses = await _dbContext.Expenses
                .Where(e => e.UserId == userId)
                .ToListAsync();

            _dbContext.Expenses.RemoveRange(expenses);
            await _dbContext.SaveChangesAsync();
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
