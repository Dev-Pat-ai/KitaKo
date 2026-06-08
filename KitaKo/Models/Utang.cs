using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class Utang
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string? CustomerName { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Paid { get; set; }
    }
}
