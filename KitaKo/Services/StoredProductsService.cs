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
            await EnsureDefaultStoredProductsAsync(userId);

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
                .ToListAsync();
        }

        private async Task EnsureDefaultStoredProductsAsync(int userId)
        {
            var defaultProducts = GetDefaultStoredProducts(userId).ToList();
            if (!defaultProducts.Any())
            {
                return;
            }

            var existingProductNames = new HashSet<string>(await _dbContext.StoredProducts
                .Where(p => p.UserId == userId && !p.IsArchived)
                .Select(p => p.ProductName.ToLowerInvariant())
                .ToListAsync(), StringComparer.OrdinalIgnoreCase);

            var missingProducts = defaultProducts
                .Where(p => !existingProductNames.Contains(p.ProductName.ToLowerInvariant()))
                .ToList();

            if (!missingProducts.Any())
            {
                return;
            }

            _dbContext.StoredProducts.AddRange(missingProducts);
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

            var imageProducts = productFiles.Select(relativePath =>
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

            var staticProducts = GetStaticDefaultProducts(userId);

            return imageProducts
                .Concat(staticProducts)
                .GroupBy(p => p.ProductName.ToLowerInvariant())
                .Select(group => group.First());
        }

        private IEnumerable<StoredProduct> GetStaticDefaultProducts(int userId)
        {
            var staticProducts = new[]
            {
                (Name: "Nescafe 3-in-1 Original", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 12.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nescafe 3-in-1 Strong", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 12.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nescafe 3-in-1 Decaf", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 14.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Great Taste Original", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 11.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Great Taste White", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 11.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Great Taste Brown", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 11.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Kopiko Brown Coffee", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Kopiko Black (Sugar-Free)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 15.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Kopiko 78°C (Ready-to-Drink)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Café Puro Instant Coffee", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Barako Coffee Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Milo 3-in-1", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 12.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Milo Choco (Hot)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 12.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ovaltine Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 11.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Bear Brand Choco Malt", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nescafe Creamy White", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Boss Coffee (Canned, Imported)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "UCC Black Coffee (Canned)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 48.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Salabat (Ginger Tea Sachet)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 8.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lipton Tea Sachet (Yellow Label)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 9.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Lipton Green Tea Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 9.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Ginger Lemon Tea Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 9.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chamomile Tea Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 9.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Calamansi Tea Sachet", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 9.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nestea Iced Tea Sachet (Lemon)", Category: "Instant Coffee & Hot Drinks", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),

                (Name: "Lucky Me! Instant Pancit Canton (Original)", Category: "Instant Noodles & Soups", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Pancit Canton (Calamansi)", Category: "Instant Noodles & Soups", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Pancit Canton (Chilimansi)", Category: "Instant Noodles & Soups", DefaultPrice: 9.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Pancit Canton (Extra Hot Chili)", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Chicken Mami (Soup)", Category: "Instant Noodles & Soups", DefaultPrice: 9.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Beef Mami (Soup)", Category: "Instant Noodles & Soups", DefaultPrice: 9.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Spicy La Paz Batchoy", Category: "Instant Noodles & Soups", DefaultPrice: 10.50m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Beefy Sabaw", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Supreme Bulalo", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lucky Me! Supreme Chicken", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Payless Pancit Canton (Original)", Category: "Instant Noodles & Soups", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Payless Beef Broth Noodles", Category: "Instant Noodles & Soups", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nissin Cup Noodles (Chicken)", Category: "Instant Noodles & Soups", DefaultPrice: 14.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Nissin Cup Noodles (Seafood)", Category: "Instant Noodles & Soups", DefaultPrice: 14.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Nissin Cup Noodles (Spicy)", Category: "Instant Noodles & Soups", DefaultPrice: 14.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Nissin Chow Top (Chicken)", Category: "Instant Noodles & Soups", DefaultPrice: 16.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Shin Ramyun (Korean Spicy)", Category: "Instant Noodles & Soups", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Samyang Ramen (2x Spicy)", Category: "Instant Noodles & Soups", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Samyang Ramen (Buldak Cheese)", Category: "Instant Noodles & Soups", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ottogi Jin Ramen (Mild)", Category: "Instant Noodles & Soups", DefaultPrice: 17.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Maggi Magic Sarap Noodles", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Fantastic Noodles (Chicken)", Category: "Instant Noodles & Soups", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Cup Soup (Chicken Noodle)", Category: "Instant Noodles & Soups", DefaultPrice: 13.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Cup Soup (Cream of Mushroom)", Category: "Instant Noodles & Soups", DefaultPrice: 13.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Cup Soup (Tomato)", Category: "Instant Noodles & Soups", DefaultPrice: 13.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),

                (Name: "Coca-Cola (Regular, Sakto Size)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Coca-Cola (Zero Sugar)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Pepsi (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Pepsi (Black / Zero)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Sprite (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Sprite (Green)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "7-Up (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Mountain Dew (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Royal Orange (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sarsi (Root Beer)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mug Root Beer", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "RC Cola", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "C2 Green Tea (Apple)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "C2 Green Tea (Lemon)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "C2 Green Tea (Peach)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nestea Iced Tea (Lemon)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Nestea Iced Tea (Green Apple)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Minute Maid Orange Juice", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Minute Maid Pulpy", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Del Monte Pineapple Juice", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Sun Kist Orange Juice", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Zesto Juice (Orange)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Zesto Juice (Pineapple)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Zesto Juice (Buco Pandan)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gatorade Lemon Lime", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Gatorade Orange", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Gatorade Cool Blue", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Gatorade Berry", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Pocari Sweat (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Pocari Sweat (Ion Water)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Powerade (Mountain Blast)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Powerade (Berry Ice)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Red Bull Energy Drink", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 80.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Monster Energy (Green)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 90.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Monster Energy (Ultra White)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 90.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Cobra Energy Drink", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "can", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sting Energy Drink (Red)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "can", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sting Energy Drink (Gold)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "can", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Extra Joss (Regular)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Extra Joss (Active)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Summit Mineral Water (Small)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 12.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Summit Mineral Water (500ml)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 18.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Wilkins Mineral Water", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nature Spring Mineral Water", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Absolute Distilled Water", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Nestlé Pure Life Water", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Sparkling Water (San Pellegrino)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 90.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Bear Brand Milk (RTD)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Milo RTD (Tetra Pack)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "tetra pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chuckie (Chocolate Milk, Kids)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "tetra pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Yakult (Probiotic Drink)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 50.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Vitamilk Soy Milk", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 28.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Soymilk (Sanitarium)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Buko Juice (Zico Coconut Water)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 45.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Hydro Coconut Water", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Marigold Chocolate Milk", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 30.00m, UnitType: "tetra pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Selecta Choco Milk", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Alaska Choco Milk (Tetra)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 25.00m, UnitType: "tetra pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nescafe Ready-to-Drink (Latte)", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 50.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Kopiko Ready-to-Drink Coffee", Category: "Beverages (Ready-to-Drink)", DefaultPrice: 50.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),

                (Name: "Nova Multigrain Snacks (Original)", Category: "Chips & Snacks", DefaultPrice: 16.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nova Multigrain Snacks (Cheese)", Category: "Chips & Snacks", DefaultPrice: 16.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Piattos Potato Crisps (Cheese)", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Piattos Potato Crisps (Sour Cream)", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Piattos Potato Crisps (BBQ)", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Oishi Prawn Crackers (Original)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Oishi Prawn Crackers (Garlic)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Oishi Bread Pan (Garlic)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Oishi Pillows (Chocolate)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Chiz Curls (Original)", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chippy (Barbecue)", Category: "Chips & Snacks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chippy (Cheese)", Category: "Chips & Snacks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mr. Chips (Barbecue)", Category: "Chips & Snacks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mr. Chips (Cheese)", Category: "Chips & Snacks", DefaultPrice: 13.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Boy Bawang (Garlic)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Boy Bawang (Spicy)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lays Classic Salted", Category: "Chips & Snacks", DefaultPrice: 22.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Lays Sour Cream & Onion", Category: "Chips & Snacks", DefaultPrice: 22.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Lays Cheese", Category: "Chips & Snacks", DefaultPrice: 22.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Pringles (Original)", Category: "Chips & Snacks", DefaultPrice: 90.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Pringles (Sour Cream & Onion)", Category: "Chips & Snacks", DefaultPrice: 90.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Pringles (Cheddar Cheese)", Category: "Chips & Snacks", DefaultPrice: 90.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Doritos (Nacho Cheese)", Category: "Chips & Snacks", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Doritos (Cool Ranch)", Category: "Chips & Snacks", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Cheetos (Crunchy Cheese)", Category: "Chips & Snacks", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ruffles (Cheddar & Sour Cream)", Category: "Chips & Snacks", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Jack 'n Jill Potato Chips (Original)", Category: "Chips & Snacks", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tostitos Tortilla Chips", Category: "Chips & Snacks", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Cornick (Garlic)", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cornick (Cheese)", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Popcorn (Regular Salted)", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Popcorn (Butter)", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Popcorn (Caramel)", Category: "Chips & Snacks", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Microwave Popcorn (Jolly Time)", Category: "Chips & Snacks", DefaultPrice: 40.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Rice Crackers (Original)", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Skyflakes Crackers", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hi-Ho Crackers", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Graham Crackers (Honey)", Category: "Chips & Snacks", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Rebisco Assorted", Category: "Chips & Snacks", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cream-O (Chocolate Cookies)", Category: "Chips & Snacks", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Cloud 9 Chocolate Bar", Category: "Chocolates & Candies", DefaultPrice: 18.00m, UnitType: "bar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Choco Mucho", Category: "Chocolates & Candies", DefaultPrice: 16.00m, UnitType: "bar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Flat Tops (Milk Chocolate)", Category: "Chocolates & Candies", DefaultPrice: 18.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Knoppers Wafer", Category: "Chocolates & Candies", DefaultPrice: 20.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Kit Kat (2-finger)", Category: "Chocolates & Candies", DefaultPrice: 18.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Kit Kat (4-finger)", Category: "Chocolates & Candies", DefaultPrice: 30.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Snickers", Category: "Chocolates & Candies", DefaultPrice: 22.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Mars Bar", Category: "Chocolates & Candies", DefaultPrice: 22.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Toblerone (Mini)", Category: "Chocolates & Candies", DefaultPrice: 40.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Ferrero Rocher (3-pc Box)", Category: "Chocolates & Candies", DefaultPrice: 65.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Hershey's Kisses (Small Pack)", Category: "Chocolates & Candies", DefaultPrice: 50.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "M&Ms Peanut (Small Pack)", Category: "Chocolates & Candies", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "M&Ms Plain (Small Pack)", Category: "Chocolates & Candies", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Reese's Peanut Butter Cups (Mini)", Category: "Chocolates & Candies", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Cadbury Dairy Milk", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Cadbury Crunchie", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Hansel (Biscuit Sticks, Chocolate)", Category: "Chocolates & Candies", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hansel (Peanut Butter)", Category: "Chocolates & Candies", DefaultPrice: 18.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "White Rabbit Candy", Category: "Chocolates & Candies", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Chupa Chups Lollipop (Strawberry)", Category: "Chocolates & Candies", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Chupa Chups Lollipop (Cola)", Category: "Chocolates & Candies", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Mentos (Mint Roll)", Category: "Chocolates & Candies", DefaultPrice: 12.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Mentos (Fruit Roll)", Category: "Chocolates & Candies", DefaultPrice: 12.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Tic Tac (Orange)", Category: "Chocolates & Candies", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Tic Tac (Mint)", Category: "Chocolates & Candies", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ricola (Swiss Herb Candy)", Category: "Chocolates & Candies", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Halls (Mentho-Lyptus)", Category: "Chocolates & Candies", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Gummy Bears (Haribo)", Category: "Chocolates & Candies", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Gummy Worms", Category: "Chocolates & Candies", DefaultPrice: 28.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Sour Patch Kids", Category: "Chocolates & Candies", DefaultPrice: 28.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Skittles (Fruit)", Category: "Chocolates & Candies", DefaultPrice: 28.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Starburst (Fruit Chews)", Category: "Chocolates & Candies", DefaultPrice: 28.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Twizzlers (Strawberry)", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ring Pop (Strawberry)", Category: "Chocolates & Candies", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Push Pop", Category: "Chocolates & Candies", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Pop Rocks (Strawberry)", Category: "Chocolates & Candies", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Nerds (Grape & Strawberry)", Category: "Chocolates & Candies", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Airheads (Watermelon)", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Hershey's Milk Chocolate Bar", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Butterfinger", Category: "Chocolates & Candies", DefaultPrice: 25.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),

                (Name: "Gardenia White Bread (Loaf / Slice Pack)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 60.00m, UnitType: "loaf", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gardenia Wheat Bread", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 60.00m, UnitType: "loaf", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gardenia Pandesal (Small Pack)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tipas Pan de Sal", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Bread & Butter (Ready-to-Eat)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cheese Roll (Convenience Store Style)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hotdog Roll (Ready-to-Eat)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ham & Cheese Sandwich", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Egg Sandwich", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tuna Sandwich", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "BLT Sandwich (Pre-packed)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Club Sandwich (Pre-packed)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 45.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Subway-style Wrap (Convenience Store Brand)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 55.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Maya Cupcake (Chocolate)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 18.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Maya Cupcake (Vanilla)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 18.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Brownie (Pre-packed)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mamon (Spanish-style Cake)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ensaymada (Small)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pan de Coco", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Monay", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cheese Bread (Small)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 18.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Red Ribbon Cake Slice (Chocolate)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 80.00m, UnitType: "slice", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Red Ribbon Cake Slice (Mocha)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 80.00m, UnitType: "slice", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Goldilocks Polvoron", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Broas (Ladyfinger Biscuits)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Barquillos", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Otap", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Egg Pie Slice", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Banana Muffin (Pre-packed)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Blueberry Muffin (Pre-packed)", Category: "Bread, Pastries & Snack Cakes", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Adobo (Chicken/Pork)", Category: "Ready-to-Eat Meals", DefaultPrice: 80.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Garlic Rice", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sinangag (Fried Rice)", Category: "Ready-to-Eat Meals", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Fried Chicken (1 pc)", Category: "Ready-to-Eat Meals", DefaultPrice: 50.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Corned Beef with Rice", Category: "Ready-to-Eat Meals", DefaultPrice: 65.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Longganisa with Rice", Category: "Ready-to-Eat Meals", DefaultPrice: 65.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tocino with Rice", Category: "Ready-to-Eat Meals", DefaultPrice: 65.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pork BBQ (Stick)", Category: "Ready-to-Eat Meals", DefaultPrice: 25.00m, UnitType: "stick", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chicken BBQ (Stick)", Category: "Ready-to-Eat Meals", DefaultPrice: 25.00m, UnitType: "stick", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hotdog on a Stick (Corndog)", Category: "Ready-to-Eat Meals", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Fish Balls (per skewer)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "skewer", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Squid Balls (per skewer)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "skewer", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Kikiam (per skewer)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "skewer", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Kwek-Kwek (Orange Coated Quail Eggs)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Isaw (Chicken Intestine BBQ)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "stick", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Siomai (4 pcs)", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "box", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Siopao (Asado)", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Siopao (Bola-bola)", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Fried Lumpiang Shanghai (4 pcs)", Category: "Ready-to-Eat Meals", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pancit Canton (Ready-to-Eat Pack)", Category: "Ready-to-Eat Meals", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cup of Ramen (Prepared)", Category: "Ready-to-Eat Meals", DefaultPrice: 50.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Grilled Corn (Mais)", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Boiled Egg (Hard-boiled, Packed)", Category: "Ready-to-Eat Meals", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Scrambled Egg Pack", Category: "Ready-to-Eat Meals", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Goto (Rice Porridge with Beef Tripe)", Category: "Ready-to-Eat Meals", DefaultPrice: 55.00m, UnitType: "bowl", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Arroz Caldo (Chicken Rice Porridge)", Category: "Ready-to-Eat Meals", DefaultPrice: 50.00m, UnitType: "bowl", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Lugaw (Plain Rice Porridge)", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "bowl", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mac & Cheese (Pre-packed, Heated)", Category: "Ready-to-Eat Meals", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hotdog (Pre-cooked, Ready-to-Eat)", Category: "Ready-to-Eat Meals", DefaultPrice: 18.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Caramel Popcorn (Freshly Popped)", Category: "Ready-to-Eat Meals", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Selecta Ice Cream (Vanilla Cup)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 40.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Selecta Ice Cream (Chocolate Cup)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 40.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Selecta Ice Cream (Ube Macapuno Cup)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 45.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Selecta Ice Cream (Mango Cup)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 45.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Magnolia Ice Cream (Chocolate Bar)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 50.00m, UnitType: "bar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Magnolia Ice Cream (Vanilla Bar)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 50.00m, UnitType: "bar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nestle Drumstick (Vanilla)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 55.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Nestle Drumstick (Chocolate)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 55.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Popsicle (Orange Flavored)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Popsicle (Buko Pandan)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Halo-Halo Bar", Category: "Ice Cream & Frozen Treats", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Dirty Kitchen Ice Cream (Dirty Ice Cream Style)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 50.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Frozen Yogurt Bar (Strawberry)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 50.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Frozen Yogurt Bar (Mango)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 50.00m, UnitType: "bar", Supplier: "Imported", Image: (string?)null),
                (Name: "Ice Scramble (Pinipig Flavored)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 45.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Creamy Delight Sundae (Vanilla)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 55.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Creamy Delight Sundae (Chocolate)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 55.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mr. Softy Ice Cream (Soft Serve)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 45.00m, UnitType: "cup", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gelato Cup (Various Flavors)", Category: "Ice Cream & Frozen Treats", DefaultPrice: 60.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Frozen Buko Bar", Category: "Ice Cream & Frozen Treats", DefaultPrice: 45.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Canned Sardines in Tomato Sauce (Ligo)", Category: "Canned Goods", DefaultPrice: 30.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Sardines in Oil (Mega)", Category: "Canned Goods", DefaultPrice: 30.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Sardines (Spicy / Sili)", Category: "Canned Goods", DefaultPrice: 30.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Mackerel (555)", Category: "Canned Goods", DefaultPrice: 28.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Tuna (in Water, Century)", Category: "Canned Goods", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Tuna (in Oil, Century)", Category: "Canned Goods", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Corned Beef (Purefoods)", Category: "Canned Goods", DefaultPrice: 55.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Corned Beef (Argentina)", Category: "Canned Goods", DefaultPrice: 55.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Corned Tuna (Purefoods)", Category: "Canned Goods", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Luncheon Meat (Spam, Small)", Category: "Canned Goods", DefaultPrice: 70.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Vienna Sausage (Libby's)", Category: "Canned Goods", DefaultPrice: 35.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Chicken All-Purpose", Category: "Canned Goods", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Canned Liver Spread (CDO)", Category: "Canned Goods", DefaultPrice: 50.00m, UnitType: "can", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Canned Pork & Beans (Hunt's)", Category: "Canned Goods", DefaultPrice: 45.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Baked Beans (Heinz)", Category: "Canned Goods", DefaultPrice: 55.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Tomato Sauce (Del Monte)", Category: "Canned Goods", DefaultPrice: 30.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Tomato Paste (Del Monte)", Category: "Canned Goods", DefaultPrice: 35.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Fruit Cocktail (Del Monte)", Category: "Canned Goods", DefaultPrice: 55.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Pineapple Juice (Dole)", Category: "Canned Goods", DefaultPrice: 55.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),
                (Name: "Canned Coconut Juice/Water", Category: "Canned Goods", DefaultPrice: 40.00m, UnitType: "can", Supplier: "Imported", Image: (string?)null),

                (Name: "Jufran Banana Ketchup (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 8.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Heinz Tomato Ketchup (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 8.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "UFC Banana Ketchup (Bottle)", Category: "Condiments & Seasonings", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Mang Tomas All-Purpose Sauce", Category: "Condiments & Seasonings", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Knorr Liquid Seasoning (Small)", Category: "Condiments & Seasonings", DefaultPrice: 15.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Maggi Liquid Seasoning (Small)", Category: "Condiments & Seasonings", DefaultPrice: 15.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Soy Sauce (Datu Puti, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 5.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Vinegar (Datu Puti, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 5.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Chili Garlic Sauce (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Mayonnaise (Lady's Choice, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Mayonnaise (Hellmann's, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Mustard (Yellow, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 8.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Hot Sauce (Tabasco, Mini)", Category: "Condiments & Seasonings", DefaultPrice: 15.00m, UnitType: "mini bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Hot Sauce (Sriracha, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Oyster Sauce (Lee Kum Kee, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Teriyaki Sauce (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sweet Chili Sauce (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Ranch Dressing (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Caesar Dressing (Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sinigang Mix (Knorr, Sachet)", Category: "Condiments & Seasonings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),

                (Name: "Safeguard Soap (Bar, Travel Size)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Dove Soap (Bar, Small)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Palmolive Soap (Bar, Small)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Kojic Acid Soap (Single Bar)", Category: "Personal Care", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Papaya Soap (Single Bar)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pantene Shampoo (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Head & Shoulders Shampoo (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sunsilk Shampoo (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Dove Shampoo (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Cream Silk Conditioner (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Dove Conditioner (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Colgate Toothpaste (Travel Size)", Category: "Personal Care", DefaultPrice: 12.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Close-Up Toothpaste (Travel Size)", Category: "Personal Care", DefaultPrice: 12.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Toothbrush (Single, Plastic Pack)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Dental Floss (Travel Pack)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Mouthwash (Listerine, Small Bottle)", Category: "Personal Care", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Dove Body Wash (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Palmolive Body Wash (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Nivea Body Lotion (Travel Size)", Category: "Personal Care", DefaultPrice: 35.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Vaseline Body Lotion (Small)", Category: "Personal Care", DefaultPrice: 35.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Belo Whitening Lotion (Small)", Category: "Personal Care", DefaultPrice: 35.00m, UnitType: "tube", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sunscreen SPF 50 (Small Tube)", Category: "Personal Care", DefaultPrice: 40.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Rexona Deodorant (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sure Deodorant Spray (Mini)", Category: "Personal Care", DefaultPrice: 25.00m, UnitType: "mini bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Pond's Facial Wash (Sachet)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Cetaphil Facial Wash (Travel Size)", Category: "Personal Care", DefaultPrice: 40.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Feminine Wash (Sachet)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sanitary Napkin (Single / 2-pc Pack)", Category: "Personal Care", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Panty Liner (Single Pack)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cotton Pads (Small Pack)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Q-Tips (Small Pack)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Wet Wipes (Travel Pack)", Category: "Personal Care", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Alcohol (70% Isopropyl, Small Bottle)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hand Sanitizer (Gel, Small Bottle)", Category: "Personal Care", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cologne / Body Mist (Small)", Category: "Personal Care", DefaultPrice: 45.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Hair Ties / Ikat Buhok (Pack)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Comb (Plastic, Single)", Category: "Personal Care", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nail Cutter (Single)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Bobby Pins (Small Pack)", Category: "Personal Care", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Band-Aid / Adhesive Bandage (Small Pack)", Category: "Personal Care", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),

                (Name: "Biogesic (Paracetamol 500mg)", Category: "Over-the-Counter Medicines", DefaultPrice: 22.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tempra (Paracetamol, Children's)", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Alaxan (Ibuprofen + Paracetamol)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ibuprofen 200mg (Generic)", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mefenamic Acid 500mg (Generic)", Category: "Over-the-Counter Medicines", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Flanax (Naproxen Sodium)", Category: "Over-the-Counter Medicines", DefaultPrice: 38.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Bioflu (Flu + Cold)", Category: "Over-the-Counter Medicines", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Neozep Forte (Cold)", Category: "Over-the-Counter Medicines", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Decolgen (Cold & Sinusitis)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Tuseran Forte (Cough)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Dimetapp (Children's Cold)", Category: "Over-the-Counter Medicines", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Benadryl (Cough Syrup, Small)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Robitussin (Cough Syrup, Small)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Ascorbic Acid 500mg (Vitamin C)", Category: "Over-the-Counter Medicines", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Enervon (Multivitamins)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Centrum (Multivitamins, Single)", Category: "Over-the-Counter Medicines", DefaultPrice: 50.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Stresstabs (B-Complex)", Category: "Over-the-Counter Medicines", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Berocca (Effervescent Tab)", Category: "Over-the-Counter Medicines", DefaultPrice: 50.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Conzace (Vitamins A, C, E, Zinc)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Ceelin (Vitamin C, Kids, Sachet)", Category: "Over-the-Counter Medicines", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Kremil-S (Antacid)", Category: "Over-the-Counter Medicines", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mylanta (Antacid Liquid, Small)", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Buscopan (Stomach Cramps)", Category: "Over-the-Counter Medicines", DefaultPrice: 50.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Immodium (Anti-Diarrhea)", Category: "Over-the-Counter Medicines", DefaultPrice: 50.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Diatabs (Anti-Diarrhea)", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Loperamide (Generic, Anti-Diarrhea)", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hydrite (Oral Rehydration Salts)", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gatorade (used as ORS alternative)", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Efficascent Oil (Analgesic Oil)", Category: "Over-the-Counter Medicines", DefaultPrice: 35.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "White Flower Embrocation Oil", Category: "Over-the-Counter Medicines", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Salonpas Pain Relief Patch", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Tiger Balm (Regular)", Category: "Over-the-Counter Medicines", DefaultPrice: 35.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Counterpain (Analgesic Cream)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Betadine (Povidone Iodine, Small)", Category: "Over-the-Counter Medicines", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Hydrogen Peroxide (Small Bottle)", Category: "Over-the-Counter Medicines", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Claritin (Antihistamine, Single Dose)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Zyrtec (Antihistamine, Single Dose)", Category: "Over-the-Counter Medicines", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Doxylamine (Sleepy / Antihistamine)", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Antabine (Motion Sickness)", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Bonamine (Motion Sickness)", Category: "Over-the-Counter Medicines", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Ariel Detergent Powder (Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Tide Detergent Powder (Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Surf Detergent Powder (Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Downy Fabric Conditioner (Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Comfort Fabric Conditioner (Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Zonrox Bleach (Small Bottle)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Joy Dishwashing Liquid (Sachet)", Category: "Household Essentials", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Dawn Dishwashing Liquid (Small)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Sponge Scrubber (Single)", Category: "Household Essentials", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Garbage Bag (Small Roll / 10-pc Pack)", Category: "Household Essentials", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cling Wrap (Small Roll)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Aluminum Foil (Small Roll)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Ziplock Bags (Small Pack)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Paper Bag (for takeout)", Category: "Household Essentials", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Plastic Spoon & Fork Set", Category: "Household Essentials", DefaultPrice: 20.00m, UnitType: "set", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Plastic Cup (Single)", Category: "Household Essentials", DefaultPrice: 5.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Styrofoam Box (Takeout)", Category: "Household Essentials", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Paper Plates (Small Pack)", Category: "Household Essentials", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tissue / Napkin (Small Pack)", Category: "Household Essentials", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Wet Wipes (Household, Small Pack)", Category: "Household Essentials", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Insect Repellent (OFF! Sachet)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Katol (Mosquito Coil, 2-pc Pack)", Category: "Household Essentials", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Air Freshener Spray (Glade, Mini)", Category: "Household Essentials", DefaultPrice: 40.00m, UnitType: "mini bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Candle (White, Single)", Category: "Household Essentials", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Matchstick / Lighter", Category: "Household Essentials", DefaultPrice: 10.00m, UnitType: "box", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Batteries (AA, 2-pc Pack)", Category: "Household Essentials", DefaultPrice: 80.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Batteries (AAA, 2-pc Pack)", Category: "Household Essentials", DefaultPrice: 90.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Masking Tape (Small Roll)", Category: "Household Essentials", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Scotch Tape (Small Roll)", Category: "Household Essentials", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Ballpen (Blue / Black, Single)", Category: "Household Essentials", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Globe Prepaid Load (₱10)", Category: "Telco & E-Services", DefaultPrice: 10.00m, UnitType: "service", Supplier: "Globe", Image: (string?)null),
                (Name: "Globe Prepaid Load (₱20)", Category: "Telco & E-Services", DefaultPrice: 20.00m, UnitType: "service", Supplier: "Globe", Image: (string?)null),
                (Name: "Globe Prepaid Load (₱50)", Category: "Telco & E-Services", DefaultPrice: 50.00m, UnitType: "service", Supplier: "Globe", Image: (string?)null),
                (Name: "Globe Prepaid Load (₱100)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "service", Supplier: "Globe", Image: (string?)null),
                (Name: "Smart Prepaid Load (₱10)", Category: "Telco & E-Services", DefaultPrice: 10.00m, UnitType: "service", Supplier: "Smart", Image: (string?)null),
                (Name: "Smart Prepaid Load (₱20)", Category: "Telco & E-Services", DefaultPrice: 20.00m, UnitType: "service", Supplier: "Smart", Image: (string?)null),
                (Name: "Smart Prepaid Load (₱50)", Category: "Telco & E-Services", DefaultPrice: 50.00m, UnitType: "service", Supplier: "Smart", Image: (string?)null),
                (Name: "Smart Prepaid Load (₱100)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "service", Supplier: "Smart", Image: (string?)null),
                (Name: "DITO Prepaid Load (₱20)", Category: "Telco & E-Services", DefaultPrice: 20.00m, UnitType: "service", Supplier: "DITO", Image: (string?)null),
                (Name: "DITO Prepaid Load (₱50)", Category: "Telco & E-Services", DefaultPrice: 50.00m, UnitType: "service", Supplier: "DITO", Image: (string?)null),
                (Name: "Globe GoSURF Data Pack", Category: "Telco & E-Services", DefaultPrice: 50.00m, UnitType: "service", Supplier: "Globe", Image: (string?)null),
                (Name: "Smart GigaSurf Pack", Category: "Telco & E-Services", DefaultPrice: 50.00m, UnitType: "service", Supplier: "Smart", Image: (string?)null),
                (Name: "Beep Card (for MRT / LRT)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Beep Card Reload", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "service", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "GCash QR Payment (cashless)", Category: "Telco & E-Services", DefaultPrice: 1.00m, UnitType: "service", Supplier: "GCash", Image: (string?)null),
                (Name: "PayMaya QR Payment", Category: "Telco & E-Services", DefaultPrice: 1.00m, UnitType: "service", Supplier: "PayMaya", Image: (string?)null),
                (Name: "SIM Card (Globe)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "piece", Supplier: "Globe", Image: (string?)null),
                (Name: "SIM Card (Smart)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "piece", Supplier: "Smart", Image: (string?)null),
                (Name: "SIM Card (DITO)", Category: "Telco & E-Services", DefaultPrice: 100.00m, UnitType: "piece", Supplier: "DITO", Image: (string?)null),
                (Name: "Bills Payment (Electric, Water, etc.)", Category: "Telco & E-Services", DefaultPrice: 0.00m, UnitType: "service", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Marlboro Red (per stick)", Category: "Tobacco", DefaultPrice: 15.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Marlboro Lights (per stick)", Category: "Tobacco", DefaultPrice: 15.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Philip Morris (per stick)", Category: "Tobacco", DefaultPrice: 15.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Winston (per stick)", Category: "Tobacco", DefaultPrice: 15.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Fortune (per stick)", Category: "Tobacco", DefaultPrice: 12.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Hope (per stick)", Category: "Tobacco", DefaultPrice: 12.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Camel (per stick)", Category: "Tobacco", DefaultPrice: 20.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Lucky Strike (per stick)", Category: "Tobacco", DefaultPrice: 20.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "L&M (per stick)", Category: "Tobacco", DefaultPrice: 12.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Mighty (per stick)", Category: "Tobacco", DefaultPrice: 10.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),

                (Name: "Boiled Egg (Ready-to-Eat, Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hard-Boiled Quail Eggs (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Salted Egg (Itlog na Maalat)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Century Egg (Pidan)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ready-to-Eat Oatmeal Cup (Plain)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 25.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Ready-to-Eat Oatmeal Cup (Honey)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 25.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Granola Bar (Quaker Chewy)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Granola Bar (Nature Valley)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 25.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Quaker Oats Sachet (Original)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Quaker Oats Sachet (Honey & Almond)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Fiber Biscuits (Liga)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Nutri-Grain Bar (Cereal Bar)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Special K Bar", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Breakfast Bar (Kellogg's)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Banana (per piece)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Apple (per piece)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Orange / Mandarin (per piece)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Boiled Peanuts (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Roasted Peanuts (Salted, Small)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cashew Nuts (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Dried Mango (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mixed Nuts (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Raisins (Small Box)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 30.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Dried Cranberries (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Trail Mix (Small Pack)", Category: "Breakfast / On-The-Go Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),

                (Name: "Knorr SavorRich Powder (Chicken)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr SavorRich Powder (Pork)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr SavorRich Liquid (All-Purpose)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Sinigang Mix (Original Tamarind)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Sinigang Mix (Miso)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Sinigang Mix (Gabi)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Knorr Sinigang Mix (Calamansi)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Mama Sita's Kare-Kare Mix", Category: "Instant / Easy-Cook Foods", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mama Sita's Caldereta Mix", Category: "Instant / Easy-Cook Foods", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mama Sita's Menudo Mix", Category: "Instant / Easy-Cook Foods", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Mama Sita's Adobo Mix", Category: "Instant / Easy-Cook Foods", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Del Monte Sweet Style Spaghetti Sauce", Category: "Instant / Easy-Cook Foods", DefaultPrice: 55.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Del Monte Italian Style Sauce", Category: "Instant / Easy-Cook Foods", DefaultPrice: 55.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Clara Ole Spaghetti Sauce", Category: "Instant / Easy-Cook Foods", DefaultPrice: 60.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Barilla Pasta (Quick Cook)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 40.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Payless Pancit Canton", Category: "Instant / Easy-Cook Foods", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Monde Instant Noodles", Category: "Instant / Easy-Cook Foods", DefaultPrice: 9.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "MyJo Ramen (Various Flavors)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Tatlong Bala na Pandesal Mix", Category: "Instant / Easy-Cook Foods", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Maya Hotcake Mix (Small Pack)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Crispy Fry (Breading Mix, Sachet)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Breader (All-Purpose, Sachet)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Knorr Cream of Mushroom Mix (Sachet)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Maggi Magic Sarap Seasoning (Sachet)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Ajinomoto / MSG (Sachet)", Category: "Instant / Easy-Cook Foods", DefaultPrice: 8.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),

                (Name: "Nutella (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 120.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Skippy Peanut Butter (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 110.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Lady's Choice Peanut Butter (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 100.00m, UnitType: "jar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Maya Ube Halaya Spread", Category: "Spreads & Toppings", DefaultPrice: 80.00m, UnitType: "jar", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Baguio Strawberry Jam (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 70.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Del Monte Mango Jam (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 60.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Smucker's Strawberry Jam (Sachet)", Category: "Spreads & Toppings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Lily's Peanut Butter (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 100.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Magnolia Butter (Single Serve)", Category: "Spreads & Toppings", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Anchor Butter (Portion Pack)", Category: "Spreads & Toppings", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Cottage Cheese (Individual Cup)", Category: "Spreads & Toppings", DefaultPrice: 35.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Cream Cheese (Small Tub)", Category: "Spreads & Toppings", DefaultPrice: 45.00m, UnitType: "tub", Supplier: "Imported", Image: (string?)null),
                (Name: "Cheese Whiz (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 50.00m, UnitType: "jar", Supplier: "Imported", Image: (string?)null),
                (Name: "Eden Cheese Spread (Small)", Category: "Spreads & Toppings", DefaultPrice: 40.00m, UnitType: "tub", Supplier: "Imported", Image: (string?)null),
                (Name: "Quickmelt Cheese (Sliced Pack)", Category: "Spreads & Toppings", DefaultPrice: 45.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Honey (Single Serve Stick)", Category: "Spreads & Toppings", DefaultPrice: 10.00m, UnitType: "stick", Supplier: "Imported", Image: (string?)null),
                (Name: "Maple Syrup (Sachet)", Category: "Spreads & Toppings", DefaultPrice: 15.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Condensed Milk (Sachet)", Category: "Spreads & Toppings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Sweetened Creamer (Nestle, Sachet)", Category: "Spreads & Toppings", DefaultPrice: 10.00m, UnitType: "sachet", Supplier: "Imported", Image: (string?)null),
                (Name: "Coconut Jam / Matamis na Bao (Small Jar)", Category: "Spreads & Toppings", DefaultPrice: 60.00m, UnitType: "jar", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Fresh Milk (250ml, Tetra)", Category: "Chilled / Refrigerated Items", DefaultPrice: 35.00m, UnitType: "tetra pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Low-Fat Milk (250ml, Tetra)", Category: "Chilled / Refrigerated Items", DefaultPrice: 35.00m, UnitType: "tetra pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Chocolate Milk (250ml, Tetra)", Category: "Chilled / Refrigerated Items", DefaultPrice: 35.00m, UnitType: "tetra pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Yogurt Drink (Regular)", Category: "Chilled / Refrigerated Items", DefaultPrice: 30.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Yakult (5-pc Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 55.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Yogurt Cup (Flavored)", Category: "Chilled / Refrigerated Items", DefaultPrice: 30.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Greek Yogurt (Single Cup)", Category: "Chilled / Refrigerated Items", DefaultPrice: 45.00m, UnitType: "cup", Supplier: "Imported", Image: (string?)null),
                (Name: "Cheese (Eden Block, Small)", Category: "Chilled / Refrigerated Items", DefaultPrice: 40.00m, UnitType: "block", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Quickmelt Cheese (Small Block)", Category: "Chilled / Refrigerated Items", DefaultPrice: 45.00m, UnitType: "block", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Butter (Anchor, Individual)", Category: "Chilled / Refrigerated Items", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Cream (All-Purpose, Small)", Category: "Chilled / Refrigerated Items", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Eggs (Per Piece / 6-pc Tray)", Category: "Chilled / Refrigerated Items", DefaultPrice: 12.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Salted Egg (Chilled, Ready Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pre-packed Tofu (Tokwa)", Category: "Chilled / Refrigerated Items", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ready-to-Cook Siomai (Frozen)", Category: "Chilled / Refrigerated Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Ready-to-Cook Lumpia (Frozen)", Category: "Chilled / Refrigerated Items", DefaultPrice: 35.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hotdog (Small Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Bacon (Small Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 60.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Longganisa (Small Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 55.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Pre-sliced Ham (Small Pack)", Category: "Chilled / Refrigerated Items", DefaultPrice: 55.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),

                (Name: "Plaster / Band-Aid (Small Pack)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Wound Dressing / Gauze (Small)", Category: "First Aid & Hygiene", DefaultPrice: 30.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Medical Tape (Small Roll)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Betadine Solution (Small Bottle)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Hydrogen Peroxide (Small Bottle)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "bottle", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Cotton Balls (Small Pack)", Category: "First Aid & Hygiene", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Sterile Cotton (Small Roll)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Thermometer (Mercury-Free, Oral)", Category: "First Aid & Hygiene", DefaultPrice: 150.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Disposable Mask (Single / Small Pack)", Category: "First Aid & Hygiene", DefaultPrice: 15.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Face Shield (Single)", Category: "First Aid & Hygiene", DefaultPrice: 60.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Latex Gloves (Single Pair)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "pair", Supplier: "Imported", Image: (string?)null),
                (Name: "Surgical Gloves (Pair)", Category: "First Aid & Hygiene", DefaultPrice: 40.00m, UnitType: "pair", Supplier: "Imported", Image: (string?)null),
                (Name: "Rubbing Alcohol Wipes (Small Pack)", Category: "First Aid & Hygiene", DefaultPrice: 20.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Eye Drops (Saline / Visine)", Category: "First Aid & Hygiene", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Ear Drops (Small)", Category: "First Aid & Hygiene", DefaultPrice: 40.00m, UnitType: "bottle", Supplier: "Imported", Image: (string?)null),
                (Name: "Lip Balm (ChapStick / Burt's Bees)", Category: "First Aid & Hygiene", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Petroleum Jelly (Vaseline, Small)", Category: "First Aid & Hygiene", DefaultPrice: 35.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Antifungal Cream (Clotrimazole, Small)", Category: "First Aid & Hygiene", DefaultPrice: 40.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),
                (Name: "Hydrocortisone Cream (Small Tube)", Category: "First Aid & Hygiene", DefaultPrice: 40.00m, UnitType: "tube", Supplier: "Imported", Image: (string?)null),

                (Name: "Ballpen (BIC, Blue)", Category: "School & Office Supplies", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Ballpen (BIC, Black)", Category: "School & Office Supplies", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Pencil (No. 2)", Category: "School & Office Supplies", DefaultPrice: 5.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Permanent Marker (Black)", Category: "School & Office Supplies", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Highlighter (Yellow)", Category: "School & Office Supplies", DefaultPrice: 15.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Scotch Tape (Small Roll)", Category: "School & Office Supplies", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Masking Tape (Small Roll)", Category: "School & Office Supplies", DefaultPrice: 20.00m, UnitType: "roll", Supplier: "Imported", Image: (string?)null),
                (Name: "Rubber Band (Pack)", Category: "School & Office Supplies", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Paper Clips (Box)", Category: "School & Office Supplies", DefaultPrice: 20.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Staple Wire (Small Box)", Category: "School & Office Supplies", DefaultPrice: 25.00m, UnitType: "box", Supplier: "Imported", Image: (string?)null),
                (Name: "Correction Tape (Small)", Category: "School & Office Supplies", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Sticky Notes (Small Pad)", Category: "School & Office Supplies", DefaultPrice: 30.00m, UnitType: "pad", Supplier: "Imported", Image: (string?)null),
                (Name: "Index Cards (Small Pack)", Category: "School & Office Supplies", DefaultPrice: 25.00m, UnitType: "pack", Supplier: "Imported", Image: (string?)null),
                (Name: "Envelope (White, Individual)", Category: "School & Office Supplies", DefaultPrice: 5.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Small Notebook / Pad Paper", Category: "School & Office Supplies", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Pencil Case (Small, Single)", Category: "School & Office Supplies", DefaultPrice: 50.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Scissors (Small)", Category: "School & Office Supplies", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Ruler (30cm, Plastic)", Category: "School & Office Supplies", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Folder (Pre-packed, Single)", Category: "School & Office Supplies", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Calculator (Basic, Pocket Size)", Category: "School & Office Supplies", DefaultPrice: 120.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),

                (Name: "Phone Charging Cable (Generic USB)", Category: "Misc / Impulse Buy Items", DefaultPrice: 120.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Earphones (Budget, Single-Use Type)", Category: "Misc / Impulse Buy Items", DefaultPrice: 100.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "USB Charger Adapter (Budget)", Category: "Misc / Impulse Buy Items", DefaultPrice: 120.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Power Bank (Disposable / Budget Type)", Category: "Misc / Impulse Buy Items", DefaultPrice: 250.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Lighter (Disposable)", Category: "Misc / Impulse Buy Items", DefaultPrice: 10.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Umbrella (Foldable, Budget)", Category: "Misc / Impulse Buy Items", DefaultPrice: 150.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Poncho / Raincoat (Disposable)", Category: "Misc / Impulse Buy Items", DefaultPrice: 50.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Socks (Single Pair, Cotton)", Category: "Misc / Impulse Buy Items", DefaultPrice: 30.00m, UnitType: "pair", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Hankerchief (Single, Cotton)", Category: "Misc / Impulse Buy Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Tote Bag (Small, Eco Bag)", Category: "Misc / Impulse Buy Items", DefaultPrice: 60.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Playing Cards (Deck)", Category: "Misc / Impulse Buy Items", DefaultPrice: 30.00m, UnitType: "deck", Supplier: "Imported", Image: (string?)null),
                (Name: "Lotto Ticket (EZ2 / Swertres)", Category: "Misc / Impulse Buy Items", DefaultPrice: 10.00m, UnitType: "ticket", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Stamp / Stamp Pad (Ink)", Category: "Misc / Impulse Buy Items", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gift Wrapper (Small Sheet)", Category: "Misc / Impulse Buy Items", DefaultPrice: 20.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Greeting Card (Birthday)", Category: "Misc / Impulse Buy Items", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Gift Bag (Small)", Category: "Misc / Impulse Buy Items", DefaultPrice: 35.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Scotch Tape Dispenser (Small)", Category: "Misc / Impulse Buy Items", DefaultPrice: 40.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Padlock (Small, Combination)", Category: "Misc / Impulse Buy Items", DefaultPrice: 120.00m, UnitType: "piece", Supplier: "Imported", Image: (string?)null),
                (Name: "Key Chain / Key Ring", Category: "Misc / Impulse Buy Items", DefaultPrice: 30.00m, UnitType: "piece", Supplier: "Local Supplier", Image: (string?)null),
                (Name: "Safety Pins (Small Pack)", Category: "Misc / Impulse Buy Items", DefaultPrice: 10.00m, UnitType: "pack", Supplier: "Local Supplier", Image: (string?)null),
            };

            foreach (var product in staticProducts)
            {
                yield return new StoredProduct
                {
                    UserId = userId,
                    ProductName = product.Name,
                    Category = product.Category,
                    DefaultPrice = product.DefaultPrice,
                    CostPrice = DetermineCostPrice(product.DefaultPrice),
                    Barcode = null,
                    UnitType = product.UnitType,
                    Supplier = product.Supplier,
                    ProductImage = product.Image,
                    DateCreated = DateTime.UtcNow,
                    IsArchived = false
                };
            }
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
