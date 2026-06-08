using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class FinancialSettingsService
    {
        private readonly ApplicationDbContext _dbContext;

        public FinancialSettingsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserFinancialSettings> GetOrCreateSettingsAsync(int userId)
        {
            var settings = await _dbContext.UserFinancialSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings != null)
            {
                return settings;
            }

            settings = new UserFinancialSettings
            {
                UserId = userId,
                AvailableBudget = 0,
                DailySalesGoal = 1000,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserFinancialSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
            return settings;
        }

        public async Task<UserFinancialSettings> UpdateSettingsAsync(int userId, FinancialSettingsRequest request)
        {
            var settings = await GetOrCreateSettingsAsync(userId);
            settings.AvailableBudget = request.AvailableBudget;
            settings.DailySalesGoal = request.DailySalesGoal;
            settings.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return settings;
        }
    }
}
