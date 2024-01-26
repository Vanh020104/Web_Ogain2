using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OgainShop.Models;
using BCrypt.Net;

namespace OgainShop.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new OgainShopContext(
                serviceProvider.GetRequiredService<DbContextOptions<OgainShopContext>>()))
            {
                // Look for any existing data.
                if (context.Category.Any() || context.Product.Any() || context.Order.Any() || context.OrderProduct.Any() || context.User.Any())
                {
                    return;   // Database has been seeded
                }

                // Seed data for Category
                var random = new Random();
                var categoryNames = new List<string> { "Electronics", "Clothing", "Books", "Furniture", "Toys", "Sports", "Beauty", "Food", "Home Decor" };

                foreach (var categoryName in categoryNames)
                {
                    var category = new Category
                    {
                        CategoryName = categoryName
                    };
                    context.Category.Add(category);
                }
                context.SaveChanges();

                // Seed data for Product
                var categories = context.Category.ToList();
                var thumbnailPaths = Enumerable.Range(1, 10).Select(i => $"/img/product/product-{i}.jpg").ToList();

                // Danh sách tên và mô tả sản phẩm
                var productNames = new List<string> { "Fresh Apple", "Organic Carrot", "Juicy Orange", "Green Spinach", "Sweet Mango", "Crispy Broccoli", "Red Tomato", "Healthy Avocado", "Colorful Bell Pepper" };
                var descriptions = new List<string> { "Fresh and crisp apple", "Organically grown carrots", "Juicy and vitamin-rich orange", "Nutrient-packed green spinach", "Sweet and tropical mango", "Crispy and nutritious broccoli", "Red and flavorful tomato", "Healthy and creamy avocado", "Colorful and crunchy bell pepper" };


                for (int i = 1; i <= 100; i++)
                {
                    var productName = productNames[random.Next(productNames.Count)];
                    var description = descriptions[random.Next(descriptions.Count)];

                    var product = new Product
                    {
                        ProductName = productName,
                        Description = description,
                        Price = random.Next(1, 101),
                        Thumbnail = thumbnailPaths[i % 10], // Lặp lại đường dẫn ảnh sau mỗi 10 sản phẩm
                        CategoryId = categories[random.Next(categories.Count)].CategoryId,
                        Qty = random.Next(1, 20)
                    };
                    context.Product.Add(product);
                }
                context.SaveChanges();

                // Seed data for User
                var users = new User[]
                {
                    new User { Username = "admin", Password = BCrypt.Net.BCrypt.HashPassword("admin123"), Email = "admin@gmail.com", Role = "Admin" },
                    new User { Username = "user1", Password = BCrypt.Net.BCrypt.HashPassword("123456789"), Email = "user1@gmail.com", Role = "User" },
                    new User { Username = "user2", Password = BCrypt.Net.BCrypt.HashPassword("123456789"), Email = "user2@gmail.com", Role = "User" }
                };
                foreach (var user in users)
                {
                    context.User.Add(user);
                }
                context.SaveChanges();

                // Seed data for Order
                var orderStatuses = new string[] { "Chờ xác nhận", "Đã xác nhận", "Đang giao hàng", "Đã giao hàng", "Hoàn thành", "Huỷ" };
                var isPaidOptions = new string[] { "Chưa thanh toán", "Đã thanh toán" };

                // Seed data for Order
                for (int i = 1; i <= 10; i++)
                {
                    var order = new Order
                    {
                        UserId = random.Next(1, users.Length + 1),
                        // Làm tròn giây cho thời gian đơn hàng
                        OrderDate = DateTime.Now.AddSeconds(-i).AddMilliseconds(-DateTime.Now.Millisecond),
                        TotalAmount = random.Next(1, 101),
                        Status = orderStatuses[random.Next(orderStatuses.Length)],
                        IsPaid = isPaidOptions[random.Next(isPaidOptions.Length)]
                    };
                    context.Order.Add(order);
                }
                context.SaveChanges();


                // Seed data for OrderProduct
                for (int i = 1; i <= 10; i++)
                {
                    var orderProduct = new OrderProduct
                    {
                        OrderId = random.Next(1, 10),
                        ProductId = random.Next(1, 10),
                        Quantity = random.Next(1, 5),
                        UnitPrice = random.Next(1, 101)
                    };
                    context.OrderProduct.Add(orderProduct);
                }
                context.SaveChanges();
            }
        }
    }
}
