using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class InventoryService
    {
        private readonly ApplicationDbContext _dbContext;

        public InventoryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<InventoryItem>> GetInventoryAsync(int userId)
        {
            return await _dbContext.InventoryItems
                .Include(i => i.Product)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<InventoryItem?> GetInventoryItemAsync(int userId, int id)
        {
            return await _dbContext.InventoryItems
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        }

        public async Task<InventoryItem> CreateInventoryItemAsync(int userId, InventoryItemRequest request)
        {
            var product = await _dbContext.StoredProducts
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsArchived);

            if (product == null)
            {
                throw new KeyNotFoundException("Product not found.");
            }

            var inventoryItem = new InventoryItem
            {
                UserId = userId,
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = request.Quantity,
                CostPrice = request.CostPrice,
                Price = request.Price,
                ExpirationDate = request.ExpirationDate.HasValue
                    ? DateTime.SpecifyKind(request.ExpirationDate.Value, DateTimeKind.Utc)
                    : null,
                DateAdded = DateTime.UtcNow
            };

            _dbContext.InventoryItems.Add(inventoryItem);
            await _dbContext.SaveChangesAsync();
            return inventoryItem;
        }

        public async Task<InventorySale> SellInventoryItemAsync(int userId, int id, InventorySaleRequest request)
        {
            var inventoryItem = await GetInventoryItemAsync(userId, id);
            if (inventoryItem == null)
            {
                throw new KeyNotFoundException("Inventory item not found.");
            }

            if (request.QuantitySold > inventoryItem.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock. Available: {inventoryItem.Quantity}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var amount = inventoryItem.Price * request.QuantitySold;
            var profit = Math.Max(0, (inventoryItem.Price - inventoryItem.CostPrice) * request.QuantitySold);
            var soldAt = DateTime.UtcNow;

            inventoryItem.Quantity -= request.QuantitySold;

            var sale = new Sale
            {
                UserId = userId,
                Amount = amount,
                Profit = profit,
                Description = $"Sold {request.QuantitySold} x {inventoryItem.ProductName}",
                Date = soldAt
            };

            var inventorySale = new InventorySale
            {
                UserId = userId,
                InventoryItemId = inventoryItem.Id,
                ProductId = inventoryItem.ProductId,
                ProductName = inventoryItem.ProductName,
                QuantitySold = request.QuantitySold,
                UnitPrice = inventoryItem.Price,
                CostPrice = inventoryItem.CostPrice,
                Amount = amount,
                Profit = profit,
                DateSold = soldAt
            };

            _dbContext.Sales.Add(sale);
            _dbContext.InventorySales.Add(inventorySale);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return inventorySale;
        }
    }
}
