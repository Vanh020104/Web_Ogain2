namespace OgainShop.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // Thêm cột Role

        // Navigation property
        public ICollection<Order> Orders { get; set; }
    }
}
