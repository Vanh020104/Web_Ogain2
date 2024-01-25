using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OgainShop.Models;

namespace OgainShop.Data
{
    public class OgainShopContext : DbContext
    {
        public OgainShopContext(DbContextOptions<OgainShopContext> options)
            : base(options)
        {
        }

        public DbSet<OgainShop.Models.Category> Category { get; set; } = default!;

        public DbSet<OgainShop.Models.Order> Order { get; set; } = default!;

        public DbSet<OgainShop.Models.OrderProduct> OrderProduct { get; set; } = default!;

        public DbSet<OgainShop.Models.Product> Product { get; set; } = default!;

        public DbSet<OgainShop.Models.User> User { get; set; } = default!;
    }
}
