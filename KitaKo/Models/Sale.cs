using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal Profit { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime Date { get; set; }
    }
}
