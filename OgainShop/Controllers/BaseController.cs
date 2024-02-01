using Microsoft.AspNetCore.Mvc;
using OgainShop.Models;
using OgainShop.Data;
using Microsoft.AspNetCore.Http;
using OgainShop.Models.Authentication;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System;
using System.Text;
using OgainShop.Heplers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace OgainShop.Controllers
{
    public class BaseController : Controller
    {
        protected readonly OgainShopContext _context;

        public BaseController(OgainShopContext context)
        {
            _context = context;
        }

        protected int GetFavoriteCount(int userId)
        {
            return _context.Favorite
                .Where(f => f.UserId == userId)
                .Count();
        }
        protected async Task SetCommonViewData()
        {
            var cartItems = HttpContext.Session.Get<List<CartItem>>("cart");
            ViewData["CartItemCount"] = cartItems != null ? cartItems.Count : 0;

            var usernameFromSession = HttpContext.Session.GetString("Username");
            var currentUser = await _context.User.SingleOrDefaultAsync(u => u.Username == usernameFromSession);

            if (currentUser == null)
            {
                RedirectToAction("login", "Page");
            }

            ViewBag.FavoriteCount = GetFavoriteCount(currentUser.UserId);
        }

    }

}

