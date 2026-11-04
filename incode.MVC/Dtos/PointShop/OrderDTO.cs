using System.Text.RegularExpressions;

namespace incode.MVC.Dtos.PointShop
{
    // 給使用者查看Order用
    public class OrderDTO
    {
        public int UserId { get; set; }

        public int TotalPoint { get; set; }

        public string Remark { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; }

        public int OrderId { get; set; }
    }

    // 給使用者查看OrderDetail用
    public class OrderDetailDTO
    {
        public int OrderdetailId { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public string ImageName { get; set; }

        public int Quantity { get; set; }

        public int UnitPointAtPurchase { get; set; }

        public bool RequiresShipping { get; set; }
    }

    // 給使用者查看ShippedDetail用
    public class ShippedDTO
    {
        public int ShippedDetailId { get; set; }
        public DateTime? ShippedDate { get; set; }

        public DateTime? ArrivalDate { get; set; }

        public bool IsFailed { get; set; }

        public string Remark { get; set; }

        public bool IsCompleted { get; set; }

        public int OrderId { get; set; }

        public string MaskedName { get; set; }

        public string MaskedPhone { get; set; }

        public string MaskedAddress { get; set; }

        /// <summary>
        /// 遮罩姓名,保留第一個字
        /// </summary>
        public static string MaskName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            return name.Length == 1
                ? name
                : name[0] + new string('*', name.Length - 1);
        }

        /// <summary>
        /// 遮罩電話號碼,保留前3碼和後2碼
        /// </summary>
        public static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

            if (phone.Length <= 5) return new string('*', phone.Length);

            return phone.Substring(0, 3) + new string('*', phone.Length - 5) + phone.Substring(phone.Length - 2);
        }

        /// <summary>
        /// 遮罩地址,保留縣市區路名,遮罩門牌號碼
        /// 例如: 台北市信義區忠孝東路五段123號 -> 台北市信義區忠孝東路五段***號
        /// </summary>
        public static string MaskAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;

            // 使用正則表達式匹配門牌號碼部分
            // 匹配模式: 數字、巷、弄、號、樓、之等
            var pattern = @"(\d+巷)?(\d+弄)?(\d+之?\d*)號?(.*)$";
            var match = Regex.Match(address, pattern);

            if (match.Success && match.Groups[0].Value.Length > 0)
            {
                // 取得門牌之前的部分
                var beforeNumber = address.Substring(0, match.Index);
                
                // 保留"號"及後面的部分(如樓層)
                var afterPart = match.Groups[4].Value;
                
                return beforeNumber + "***號" + afterPart;
            }

            // 如果無法匹配,則使用簡單遮罩
            return address.Length > 10
                ? address.Substring(0, 10) + "***"
                : address;
        }
    }

    // 使用者取得過去寄送資料
    public class ShipAddressDTO
    {
        public string RecipientName { get; set; }

        public string RecipientAddress { get; set; }

        public string RecipientPhone { get; set; }

    }

}

