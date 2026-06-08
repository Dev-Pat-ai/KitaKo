using System.ComponentModel.DataAnnotations;

namespace KitaKo.Models
{
    public class UserFinancialSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Range(0, 9999999999999999.99)]
        public decimal AvailableBudget { get; set; }

        [Range(0.01, 9999999999999999.99)]
        public decimal DailySalesGoal { get; set; } = 1000;

        public DateTime UpdatedAt { get; set; }
    }
}
