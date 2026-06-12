using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;

namespace KitaKo.Services
{
    public class StoredProductsService
    {
        private readonly ApplicationDbContext _dbContext;

        public StoredProductsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<StoredProduct>> GetProductsAsync(int userId, string? search = null)
        {
            var query = _dbContext.StoredProducts
                .Where(p => p.UserId == userId && !p.IsArchived);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(term) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                    p.Category.ToLower().Contains(term));
            }

            return await query
                .OrderBy(p => p.ProductName)
                .Take(50)
                .ToListAsync();
        }

        public async Task<StoredProduct?> GetProductAsync(int userId, int id)
        {
            return await _dbContext.StoredProducts
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && !p.IsArchived);
        }

        public async Task<StoredProduct> CreateProductAsync(int userId, StoredProductRequest request)
        {
            await EnsureProductIsUniqueAsync(userId, request, null);

            var product = new StoredProduct
            {
                UserId = userId,
                ProductName = NormalizeRequired(request.ProductName),
                Category = NormalizeRequired(request.Category),
                DefaultPrice = request.DefaultPrice,
                Barcode = NormalizeOptional(request.Barcode),
                UnitType = NormalizeRequired(request.UnitType),
                Supplier = NormalizeOptional(request.Supplier),
                ProductImage = NormalizeOptional(request.ProductImage),
                DateCreated = DateTime.UtcNow
            };

            _dbContext.StoredProducts.Add(product);
            await _dbContext.SaveChangesAsync();
            return product;
        }

        public async Task<StoredProduct?> UpdateProductAsync(int userId, int id, StoredProductRequest request)
        {
            var product = await GetProductAsync(userId, id);
            if (product == null)
            {
                return null;
            }

            await EnsureProductIsUniqueAsync(userId, request, id);

            product.ProductName = NormalizeRequired(request.ProductName);
            product.Category = NormalizeRequired(request.Category);
            product.DefaultPrice = request.DefaultPrice;
            product.Barcode = NormalizeOptional(request.Barcode);
            product.UnitType = NormalizeRequired(request.UnitType);
            product.Supplier = NormalizeOptional(request.Supplier);
            product.ProductImage = NormalizeOptional(request.ProductImage);

            await _dbContext.SaveChangesAsync();
            return product;
        }

        public async Task<bool> ArchiveProductAsync(int userId, int id)
        {
            var product = await GetProductAsync(userId, id);
            if (product == null)
            {
                return false;
            }

            product.IsArchived = true;
            product.ArchivedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private async Task EnsureProductIsUniqueAsync(int userId, StoredProductRequest request, int? currentProductId)
        {
            var productName = NormalizeRequired(request.ProductName);
            var barcode = NormalizeOptional(request.Barcode);

            var duplicateName = await _dbContext.StoredProducts.AnyAsync(p =>
                p.UserId == userId &&
                !p.IsArchived &&
                p.Id != currentProductId &&
                p.ProductName.ToLower() == productName.ToLower());

            if (duplicateName)
            {
                throw new InvalidOperationException("A product with this name already exists.");
            }

            if (barcode == null)
            {
                return;
            }

            var duplicateBarcode = await _dbContext.StoredProducts.AnyAsync(p =>
                p.UserId == userId &&
                !p.IsArchived &&
                p.Id != currentProductId &&
                p.Barcode != null &&
                p.Barcode.ToLower() == barcode.ToLower());

            if (duplicateBarcode)
            {
                throw new InvalidOperationException("A product with this barcode already exists.");
            }
        }

        private static string NormalizeRequired(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string? NormalizeOptional(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}
