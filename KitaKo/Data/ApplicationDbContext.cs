using Microsoft.EntityFrameworkCore;
using KitaKo.Models;

namespace KitaKo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Expenses> Expenses { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Utang> Utangs { get; set; }
        public DbSet<UserFinancialSettings> UserFinancialSettings { get; set; }
        public DbSet<StoredProduct> StoredProducts { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<InventorySale> InventorySales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User table
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure Expenses table
            modelBuilder.Entity<Expenses>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Priority).HasDefaultValue(1);
                entity.Property(e => e.Paid).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.DueDate });
                entity.HasIndex(e => new { e.UserId, e.Paid });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Sale table
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Profit).HasPrecision(18, 2);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.Date });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Utang table
            modelBuilder.Entity<Utang>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerName).HasMaxLength(200);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.DueDate });
                entity.HasIndex(e => new { e.UserId, e.Paid });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserFinancialSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AvailableBudget).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.DailySalesGoal).HasPrecision(18, 2).HasDefaultValue(1000);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StoredProduct>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DefaultPrice).HasPrecision(18, 2);
                entity.Property(e => e.Barcode).HasMaxLength(100);
                entity.Property(e => e.UnitType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Supplier).HasMaxLength(200);
                entity.Property(e => e.ProductImage).HasMaxLength(500);
                entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsArchived).HasDefaultValue(false);
                entity.HasIndex(e => new { e.UserId, e.ProductName, e.IsArchived });
                entity.HasIndex(e => new { e.UserId, e.Barcode, e.IsArchived });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CostPrice).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.DateAdded });
                entity.HasIndex(e => new { e.UserId, e.ProductId });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InventorySale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.CostPrice).HasPrecision(18, 2);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Profit).HasPrecision(18, 2);
                entity.Property(e => e.DateSold).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => new { e.UserId, e.DateSold });
                entity.HasIndex(e => new { e.UserId, e.ProductId });
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
