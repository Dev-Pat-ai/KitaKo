using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class UtangsService
    {
        private readonly ApplicationDbContext _dbContext;

        public UtangsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Utang>> GetUtangsAsync(int userId)
        {
            return await _dbContext.Utangs
                .Where(u => u.UserId == userId)
                .OrderBy(u => u.Paid)
                .ThenBy(u => u.DueDate)
                .ToListAsync();
        }

        public async Task<Utang?> GetUtangAsync(int userId, int id)
        {
            return await _dbContext.Utangs
                .FirstOrDefaultAsync(u => u.Id == id && u.UserId == userId);
        }

        public async Task<Utang> CreateUtangAsync(int userId, UtangRequest request)
        {
            var utang = new Utang
            {
                UserId = userId,
                CustomerName = request.CustomerName?.Trim(),
                Amount = request.Amount,
                DueDate = EnsureUtc(request.DueDate),
                Paid = request.Paid,
                CreatedDate = DateTime.UtcNow
            };

            _dbContext.Utangs.Add(utang);
            await _dbContext.SaveChangesAsync();
            return utang;
        }

        public async Task<bool> UpdateUtangAsync(int userId, int id, UtangRequest request)
        {
            var utang = await GetUtangAsync(userId, id);
            if (utang == null)
            {
                return false;
            }

            utang.CustomerName = request.CustomerName?.Trim();
            utang.Amount = request.Amount;
            utang.DueDate = EnsureUtc(request.DueDate);
            utang.Paid = request.Paid;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUtangAsync(int userId, int id)
        {
            var utang = await GetUtangAsync(userId, id);
            if (utang == null)
            {
                return false;
            }

            _dbContext.Utangs.Remove(utang);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
