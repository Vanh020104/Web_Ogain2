﻿namespace OgainShop.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Thumbnail { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal Total => Qty * Price;
    }
}
