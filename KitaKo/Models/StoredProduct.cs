using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class StoredProduct
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Range(0, 9999999999999999.99)]
        public decimal DefaultPrice { get; set; }

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(50)]
        public string UnitType { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Supplier { get; set; }

        [StringLength(500)]
        public string? ProductImage { get; set; }

        public DateTime DateCreated { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
    }
}
