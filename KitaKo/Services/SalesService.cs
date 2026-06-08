using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class SalesService
    {
        private readonly ApplicationDbContext _dbContext;

        public SalesService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Sale>> GetSalesAsync(int userId)
        {
            return await _dbContext.Sales
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Date)
                .ToListAsync();
        }

        public async Task<Sale?> GetSaleAsync(int userId, int id)
        {
            return await _dbContext.Sales
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        }

        public async Task<Sale> CreateSaleAsync(int userId, SaleRequest request)
        {
            var sale = new Sale
            {
                UserId = userId,
                Amount = request.Amount,
                Profit = request.Profit,
                Description = string.IsNullOrWhiteSpace(request.Description) ? "Sale" : request.Description.Trim(),
                Date = DateTime.UtcNow
            };

            _dbContext.Sales.Add(sale);
            await _dbContext.SaveChangesAsync();
            return sale;
        }

        public async Task<bool> UpdateSaleAsync(int userId, int id, SaleRequest request)
        {
            var sale = await GetSaleAsync(userId, id);
            if (sale == null)
            {
                return false;
            }

            sale.Amount = request.Amount;
            sale.Profit = request.Profit;
            sale.Description = request.Description?.Trim();
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSaleAsync(int userId, int id)
        {
            var sale = await GetSaleAsync(userId, id);
            if (sale == null)
            {
                return false;
            }

            _dbContext.Sales.Remove(sale);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
