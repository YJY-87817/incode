using incode2.Models;

namespace incode2.DTO
{
    public class PointRechargeDTO : PointRecharge
    {
        public int CashAmount { get; set; }

        public string PaymentMethod { get; set; }
    }
}