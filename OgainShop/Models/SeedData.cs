using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OgainShop.Models;

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
                var categories = new Category[]
                {
                    new Category { CategoryName = "Electronics" },
                    new Category { CategoryName = "Clothing" },
                    
                    // Add more categories as needed
                };
                foreach (var category in categories)
                {
                    context.Category.Add(category);
                }
                context.SaveChanges();

                // Seed data for Product
                var products = new Product[]
                {
                 new Product { ProductName = "Product 1", Description = "Description 1", Price = 10.99m, Thumbnail = "/img/latest-product/lp-1.jpg", CategoryId = 1,Qty=10 },
new Product { ProductName = "Product 2", Description = "Description 2", Price = 20.99m, Thumbnail = "/img/latest-product/lp-2.jpg", CategoryId = 2,Qty=10  }

                };
                foreach (var product in products)
                {
                    context.Product.Add(product);
                }
                context.SaveChanges();

                // Seed data for User
                var users = new User[]
                {
                    new User { Username = "admin", Password = "admin123", Email = "admin@example.com", Role = "Admin" },
                    new User { Username = "user1", Password = "123456789", Email = "user1@example.com", Role = "User" },
                    new User { Username = "user2", Password = "123456789", Email = "user2@example.com", Role = "User" }
                    // Add more users as needed
                };
                foreach (var user in users)
                {
                    context.User.Add(user);
                }
                context.SaveChanges();

                // Seed data for Order
                var orders = new Order[]
                {
                                   new Order { UserId = 1, OrderDate = DateTime.Now, TotalAmount = 30.99m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
new Order { UserId = 2, OrderDate = DateTime.Now, TotalAmount = 40.99m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
new Order { UserId = 1, OrderDate = DateTime.Now.AddDays(-1), TotalAmount = 25.50m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
new Order { UserId = 3, OrderDate = DateTime.Now.AddDays(-2), TotalAmount = 55.75m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
new Order { UserId = 2, OrderDate = DateTime.Now.AddDays(-3), TotalAmount = 60.25m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
new Order { UserId = 3, OrderDate = DateTime.Now.AddDays(-4), TotalAmount = 75.00m, Status = "Chờ xác nhận", IsPaid = "Chưa thanh toán" },
                // Add more orders as needed
                };
                foreach (var order in orders)
                {
                    context.Order.Add(order);
                }
                context.SaveChanges();

                // Seed data for OrderProduct
                var orderProducts = new OrderProduct[]
                {
                    new OrderProduct { OrderId = 1, ProductId = 1, Quantity = 2, UnitPrice = 10.99m },
                    new OrderProduct { OrderId = 2, ProductId = 2, Quantity = 3, UnitPrice = 20.99m }
                    // Add more order products as needed
                };
                foreach (var orderProduct in orderProducts)
                {
                    context.OrderProduct.Add(orderProduct);
                }
                context.SaveChanges();
            }
        }
    }
}