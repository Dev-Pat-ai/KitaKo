using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class InventorySale
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int InventoryItemId { get; set; }
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int QuantitySold { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal UnitPrice { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal CostPrice { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Amount { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Profit { get; set; }

        public DateTime DateSold { get; set; }
        public InventoryItem? InventoryItem { get; set; }
    }
}
