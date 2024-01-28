﻿namespace OgainShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Thumbnail { get; set; }

        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int Qty { get; set; }
        public string Status
        {
            get
            {
                return Qty > 0 ? "Còn hàng" : "Hết hàng";
            }
        }

        // Navigation property
        public Category Category { get; set; }
        public ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
