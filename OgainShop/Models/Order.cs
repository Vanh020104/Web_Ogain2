namespace OgainShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }  // Trạng thái đơn hàng (đã xác nhận hay chưa)
        public string IsPaid { get; set; }
        // Navigation properties
        public User User { get; set; }
        public ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
