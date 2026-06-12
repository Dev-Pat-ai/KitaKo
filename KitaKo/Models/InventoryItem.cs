using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal CostPrice { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Price { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public StoredProduct? Product { get; set; }
    }
}
