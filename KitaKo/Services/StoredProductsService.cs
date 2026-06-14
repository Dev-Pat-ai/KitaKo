using KitaKo.Data;
using KitaKo.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace KitaKo.Services
{
    public class StoredProductsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        public StoredProductsService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _environment = environment;
        }

        public async Task<List<StoredProduct>> GetProductsAsync(int userId, string? search = null)
        {
            var existingCount = await _dbContext.StoredProducts
                .CountAsync(p => p.UserId == userId && !p.IsArchived);

            // Lazy-seed: if user has no products, seed defaults automatically
            if (existingCount == 0)
            {
                await SeedDefaultStoredProductsAsync(userId);
            }

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

        private async Task SeedDefaultStoredProductsAsync(int userId)
        {
            var defaultProducts = GetDefaultStoredProducts(userId).ToList();
            if (!defaultProducts.Any())
            {
                return;
            }

            _dbContext.StoredProducts.AddRange(defaultProducts);
            await _dbContext.SaveChangesAsync();
        }

        private IEnumerable<StoredProduct> GetDefaultStoredProducts(int userId)
        {
            var productsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
            if (!Directory.Exists(productsFolder))
            {
                return Enumerable.Empty<StoredProduct>();
            }

            var productFiles = Directory.EnumerateFiles(productsFolder, "*.*", SearchOption.AllDirectories)
                .Where(path => AllowedImageExtensions.Contains(Path.GetExtension(path)))
                .Select(path => Path.GetRelativePath(productsFolder, path).Replace('\\', '/'))
                .Where(relativePath => !string.IsNullOrWhiteSpace(relativePath))
                .Select(relativePath => new
                {
                    RelativePath = relativePath,
                    ProductKey = Path.GetFileNameWithoutExtension(relativePath)
                        .Replace('-', ' ')
                        .Replace('_', ' ')
                        .Replace("  ", " ")
                        .Trim()
                        .ToLowerInvariant()
                })
                .GroupBy(item => item.ProductKey)
                .Select(group => group.OrderBy(item => item.RelativePath).First().RelativePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path);

            return productFiles.Select(relativePath =>
            {
                var productName = Path.GetFileNameWithoutExtension(relativePath)
                    .Replace('-', ' ')
                    .Replace('_', ' ')
                    .Replace("  ", " ")
                    .Trim();

                var defaultPrice = DetermineProductPrice(productName);
                return new StoredProduct
                {
                    UserId = userId,
                    ProductName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(productName),
                    Category = DetermineProductCategory(productName),
                    DefaultPrice = defaultPrice,
                    CostPrice = DetermineCostPrice(defaultPrice),
                    Barcode = null,
                    UnitType = DetermineProductUnitType(productName),
                    Supplier = "Local Supplier",
                    ProductImage = $"/uploads/products/{relativePath}",
                    DateCreated = DateTime.UtcNow,
                    IsArchived = false
                };
            });
        }

        private static string DetermineProductCategory(string productName)
        {
            var lowerName = productName.ToLowerInvariant();

            if (lowerName.Contains("water") || lowerName.Contains("tea") || lowerName.Contains("cola") || lowerName.Contains("gatorade") || lowerName.Contains("sprite") || lowerName.Contains("milk") || lowerName.Contains("royal tru") || lowerName.Contains("yakult"))
                return "Beverages";

            if (lowerName.Contains("noodles") || lowerName.Contains("bread") || lowerName.Contains("cookies") || lowerName.Contains("crackers") || lowerName.Contains("chips") || lowerName.Contains("candy") || lowerName.Contains("snack") || lowerName.Contains("sari sari") || lowerName.Contains("cornetto"))
                return "Snacks";

            if (lowerName.Contains("toothpaste") || lowerName.Contains("soap") || lowerName.Contains("patch") || lowerName.Contains("paracetamol") || lowerName.Contains("pain"))
                return "Toiletries";

            if (lowerName.Contains("battery") || lowerName.Contains("lighter"))
                return "Supplies";

            if (lowerName.Contains("ham") || lowerName.Contains("hotdog") || lowerName.Contains("pork") || lowerName.Contains("fish") || lowerName.Contains("kikiam") || lowerName.Contains("siomai") || lowerName.Contains("chicken") || lowerName.Contains("tocino") || lowerName.Contains("corned beef") || lowerName.Contains("burger") || lowerName.Contains("fish balls") || lowerName.Contains("nuggets"))
                return "Meat & Seafood";

            return "General";
        }

        private static decimal DetermineProductPrice(string productName)
        {
            var lowerName = productName.ToLowerInvariant();

            if (lowerName.Contains("water") || lowerName.Contains("tea") || lowerName.Contains("cola") || lowerName.Contains("gatorade") || lowerName.Contains("sprite"))
                return 25.00m;

            if (lowerName.Contains("milk") || lowerName.Contains("yakult") || lowerName.Contains("cereal") || lowerName.Contains("nescafe") || lowerName.Contains("milo"))
                return 55.00m;

            if (lowerName.Contains("noodles") || lowerName.Contains("bread") || lowerName.Contains("cookies") || lowerName.Contains("crackers") || lowerName.Contains("chips") || lowerName.Contains("candy") || lowerName.Contains("cornetto"))
                return 25.00m;

            if (lowerName.Contains("ham") || lowerName.Contains("hotdog") || lowerName.Contains("pork") || lowerName.Contains("fish") || lowerName.Contains("kikiam") || lowerName.Contains("siomai") || lowerName.Contains("chicken") || lowerName.Contains("tocino") || lowerName.Contains("burger"))
                return 120.00m;

            if (lowerName.Contains("toothpaste") || lowerName.Contains("soap") || lowerName.Contains("patch") || lowerName.Contains("paracetamol") || lowerName.Contains("battery") || lowerName.Contains("lighter"))
                return 30.00m;

            return 50.00m;
        }

        private static decimal DetermineCostPrice(decimal defaultPrice)
        {
            return Math.Round(defaultPrice * 0.60m, 2);
        }

        private static string DetermineProductUnitType(string productName)
        {
            var lowerName = productName.ToLowerInvariant();

            if (lowerName.Contains("bottle") || lowerName.Contains("milk") || lowerName.Contains("water") || lowerName.Contains("coke") || lowerName.Contains("cola") || lowerName.Contains("sprite") || lowerName.Contains("gatorade") || lowerName.Contains("yakult") || lowerName.Contains("tea"))
                return "bottle";

            if (lowerName.Contains("pack") || lowerName.Contains("sachet") || lowerName.Contains("pcs") || lowerName.Contains("piece") || lowerName.Contains("pieces") || lowerName.Contains("box") || lowerName.Contains("roll") || lowerName.Contains("bag"))
                return "pack";

            if (lowerName.Contains("kg") || lowerName.Contains("g") || lowerName.Contains("250") || lowerName.Contains("500") || lowerName.Contains("1kg") || lowerName.Contains("450g") || lowerName.Contains("100g") || lowerName.Contains("200g") || lowerName.Contains("150g") || lowerName.Contains("90g"))
                return "pack";

            return "piece";
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
                CostPrice = request.CostPrice,
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
            product.CostPrice = request.CostPrice;
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
