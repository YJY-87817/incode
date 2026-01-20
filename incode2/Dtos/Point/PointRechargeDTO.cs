using incode2.Models;

namespace incode2.DTO
{
    public class PointRechargeDTO : PointRecharge
    {
        public int PointRechargeId { get; set; }

        public int UserId { get; set; }

        public decimal CashAmount { get; set; }

        public int PointsGranted { get; set; }

        public string PaymentMethod { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}